using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

namespace SimpleMock
{
    internal class MockType<T>
        where T : class
    {
        private readonly IEnumerable<MethodMock> _methodMocks;
        private readonly IDictionary<FieldBuilder, object> _references;
        private TypeBuilder _typeBuilder;

        /// <summary>
        /// Creates an instance of a MockType given a set of method mock definitions.
        /// </summary>
        /// <param name="methodMocks">Definitions of a method mock.</param>
        public MockType(IEnumerable<MethodMock> methodMocks)
        {
            _methodMocks = methodMocks;
            _references = new Dictionary<FieldBuilder, object>();
        }

        /// <summary>
        /// Creates a type that either derives from T if it's a class, either implements T if it's an interface.
        /// And supplies stub implementations for all the methods in T
        /// </summary>
        /// <returns></returns>
        public T BuildUp()
        {
            Type mockType = typeof(T);

            Type baseType = mockType.IsClass ? mockType : typeof(object);
            Type interfaceType = mockType.IsInterface ? mockType : null;

            string assemblyName = baseType.Name + "Mock";
            string assemblyFileName = assemblyName + ".dll";
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyFileName);

            string randomString = Guid.NewGuid().ToString().Split(new char[] { '-' })[0];

            _typeBuilder = moduleBuilder.DefineType(
                baseType.Name + randomString,
                TypeAttributes.Class | TypeAttributes.Public,
                baseType,
                interfaceType != null ? new[] { interfaceType } : new Type[0]);

            if (interfaceType != null)
            {
                _typeBuilder.AddInterfaceImplementation(interfaceType);
            }

            EmitMethodStubs();

            EmitConstructor();

            mockType = _typeBuilder.CreateType();

            return (T)Activator.CreateInstance(mockType, _references.Select(r => r.Value).ToArray());
        }

        /// <summary>
        /// Creates a constructor that takes all the references as parameters and assigns them to private fields.
        /// </summary>
        private void EmitConstructor()
        {
            // Create a constructor that takes all the references as parameters
            ConstructorBuilder constructorBuilder = _typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                _references.Select(r => r.Value.GetType()).ToArray());

            // Call object's parameterless constructor
            var il = constructorBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));

            // Load all of the arguments into fields for the references
            int argumentIndex = 1;
            foreach (var reference in _references)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg, argumentIndex);
                il.Emit(OpCodes.Stfld, reference.Key);

                argumentIndex++;
            }

            il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Creates stub implementations for all the non implemented abstract methods / properties in the base class or interface.
        /// The methods / properties that have been defined are implemented accordingly.
        /// </summary>
        private void EmitMethodStubs()
        {
            Type mockType = typeof(T);

            foreach (var propertyInfo in mockType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // TODO: Implement CreatePropertyStub(...)
                Console.WriteLine(propertyInfo);
            }
            foreach (var methodInfo in mockType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                               .Where(mi => mi.IsAbstract || mi.IsVirtual))
            {
                EmitMethodStub(methodInfo);
            }
        }

        /// <summary>
        /// Creates a stub implementation for the method if it's abstract. Otherwise it creates the defined behavior.
        /// </summary>
        /// <param name="methodInfo"></param>
        private void EmitMethodStub(MethodInfo methodInfo)
        {
            var methodBuilder = _typeBuilder.DefineMethod(
                                            methodInfo.Name,
                                            MethodAttributes.Public | MethodAttributes.Virtual,
                                            methodInfo.ReturnType,
                                            methodInfo.GetParameters().Select(pi => pi.ParameterType).ToArray());

            var il = methodBuilder.GetILGenerator();

            if (methodInfo.IsGenericMethodDefinition)
            {
                methodBuilder.DefineGenericParameters(methodInfo.GetGenericArguments().Select(p => p.Name).ToArray());

                // Match method mocks to the method we're implementing
                // And group on method signature, so generic method instances can be separated
                var methodMockGroups = from methodMock in _methodMocks
                                       let expressionMethod = methodMock.Expression.Method
                                       where expressionMethod.IsGenericMethod
                                       where methodInfo.Equals(expressionMethod.GetGenericMethodDefinition())
                                       group methodMock by expressionMethod into g
                                       select g;

                // Iterate through the groups of methods in case multiple generic method instances have been defined
                foreach (var methodMockGroup in methodMockGroups)
                {
                    var exitLabel = il.DefineLabel();

                    var groupMethod = methodMockGroup.First().Expression.Method;
                    var groupMethodDefinition = groupMethod.GetGenericMethodDefinition();

                    foreach (var argument in groupMethodDefinition.GetGenericArguments().Zip(groupMethod.GetGenericArguments(), (l, r) => new { l, r }))
                    {
                        EmitTypeOf(il, argument.l);
                        EmitTypeOf(il, argument.r);

                        il.Emit(OpCodes.Call, typeof(Type).GetMethod("op_Equality", new[] { typeof(Type), typeof(Type) }));
                        il.Emit(OpCodes.Brfalse_S, exitLabel);
                    }

                    EmitMethodMocks(il, methodMockGroup);

                    il.MarkLabel(exitLabel);
                }
            }
            else
            {
                // Match method mocks to the method we're implementing
                var methodMocks = _methodMocks.Where(m => methodInfo.Equals(m.Expression.Method));

                EmitMethodMocks(il, methodMocks);
            }

            // By default throw a NotImplementedException
            il.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Throw);

            _typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
        }
        private void EmitMethodMocks(ILGenerator il, IEnumerable<MethodMock> methodMocks)
        {
            // Emit either a custom implementation or a generated implementation based on the defined mock
            foreach (var mock in methodMocks)
            {
                if (mock is MethodImplementationMockBase)
                {
                    EmitCustomImplementation(mock.Expression.Method, il, (MethodImplementationMockBase)mock);
                }
                else if (mock is MethodParameterMockBase)
                {
                    EmitGeneratedImplementation(mock.Expression.Method, il, (MethodParameterMockBase)mock);
                }
            }
        }

        private void EmitGeneratedImplementation(MethodInfo methodInfo, ILGenerator il, MethodParameterMockBase methodParameterMock)
        {
            Label exitLabel = il.DefineLabel();

            EmitBranches(methodInfo, il, methodParameterMock.Expression, exitLabel);

            if (methodParameterMock.MethodCompletesMock is MethodReturnsMockBase)
            {
                var methodReturnsMock = (MethodReturnsMockBase)methodParameterMock.MethodCompletesMock;

                EmitReturnsImplementation(il, methodReturnsMock);
            }
            else if (methodParameterMock.MethodCompletesMock is MethodThrowsMockBase)
            {
                var methodThrowsMock = (MethodThrowsMockBase)methodParameterMock.MethodCompletesMock;

                EmitThrowsImplementation(il, methodThrowsMock);
            }

            il.MarkLabel(exitLabel);
        }
        private void EmitReturnsImplementation(ILGenerator il, MethodReturnsMockBase methodReturnsMock)
        {
            if (methodReturnsMock.Callback != null)
            {
                EmitReference(il, methodReturnsMock.Callback);
                EmitInvokeAction(il, methodReturnsMock.Callback);
            }
            
            EmitConstant(il, methodReturnsMock.ReturnValue, methodReturnsMock.ReturnType);
            il.Emit(OpCodes.Ret);
        }
        private void EmitThrowsImplementation(ILGenerator il, MethodThrowsMockBase methodThrowsMock)
        {
            if (methodThrowsMock.ExceptionInitializer != null)
            {
                EmitReference(il, methodThrowsMock.ExceptionInitializer);
                EmitInvokeAction(il, methodThrowsMock.ExceptionInitializer);
            }
            else
            {
                Type exceptionType = methodThrowsMock.ExceptionType;
                il.Emit(OpCodes.Newobj, exceptionType.GetConstructor(Type.EmptyTypes));
            }

            il.Emit(OpCodes.Throw);
        }
        private void EmitCustomImplementation(MethodInfo methodInfo, ILGenerator il, MethodImplementationMockBase methodImplementationMock)
        {
            if (methodImplementationMock.CustomImplemenation == null)
            {
                return;
            }

            EmitReference(il, methodImplementationMock.CustomImplemenation);
            EmitInvokeAction(il, methodImplementationMock.CustomImplemenation, methodInfo);

            il.Emit(OpCodes.Ret);
        }
        private void EmitBranches(MethodInfo methodInfo, ILGenerator il, MethodCallExpression methodExpression, Label exitLabel)
        {
            int argumentIndex = 1;
            foreach (var argumentExpression in methodExpression.Arguments)
            {
                Type argumentType;
                object argumentValue;
                ExpressionHelpers.GetArgumentValueAndType(argumentExpression, out argumentValue, out argumentType);

                var defaultMethod = typeof(EqualityComparer<>).MakeGenericType(new[] { argumentType }).GetMethod("get_Default");
                il.Emit(OpCodes.Call, defaultMethod);

                EmitLdArgument(methodInfo, il, argumentIndex);
                EmitConstant(il, argumentValue, argumentType);

                var equalsMethod = defaultMethod.ReturnType.GetMethods(BindingFlags.Instance | BindingFlags.Public).First(m => m.Name == "Equals" && m.GetParameters().Count() == 2);
                il.Emit(OpCodes.Callvirt, equalsMethod);

                il.Emit(OpCodes.Brfalse_S, exitLabel);

                argumentIndex++;
            } 
        }
        private void EmitLdArgument(MethodInfo methodInfo, ILGenerator il, int argumentIndex)
        {
            if (methodInfo.IsGenericMethod)
            {
                var methodDefinitionInfo = methodInfo.GetGenericMethodDefinition();
                var typeMapper = methodDefinitionInfo.GetGenericArguments()
                                     .Zip(methodInfo.GetGenericArguments(), (l, r) => new { l, r })
                                     .ToDictionary(x => x.l, x => x.r);

                var genericParameters = methodDefinitionInfo.GetParameters();

                il.Emit(OpCodes.Ldarg, argumentIndex);

                ParameterInfo parameterInfo = genericParameters[argumentIndex - 1];
                il.Emit(OpCodes.Box, parameterInfo.ParameterType);
                il.Emit(OpCodes.Unbox_Any, ConstructGenericParameter(parameterInfo, typeMapper));
            }
            else
            {
                il.Emit(OpCodes.Ldarg, argumentIndex);
            }
        }
        private void EmitConstant(ILGenerator il, object value, Type type)
        {
            // If the value is null, just emit the type's default value
            if (value == null)
            {
                EmitDefault(il, type);
            }
            // If it's a nullable valuetype initialize it accordingly
            else if (type.IsValueType && Nullable.GetUnderlyingType(type) != null)
            {
                Type baseType = type.GetGenericArguments().First();

                EmitReference(il, value, baseType);

                var constructor = type.GetConstructor(new[] { baseType });
                il.Emit(OpCodes.Newobj, constructor);
            }
            // If it's a non-nullable valuetype or a reference type, just emit the reference
            else
            {
                EmitReference(il, value, type);
            }
        }
        private void EmitDefault(ILGenerator il, Type type)
        {
            if (type.IsValueType && type != typeof(void))
            {
                if (type.IsPrimitive || type.IsEnum)
                {
                    il.Emit(OpCodes.Ldc_I4_0);
                }
                else
                {
                    LocalBuilder local = il.DeclareLocal(type);
                    il.Emit(OpCodes.Ldloca_S, local);
                    il.Emit(OpCodes.Initobj, type);
                    il.Emit(OpCodes.Ldloc_0);
                }
            }
            else if (type.IsClass)
            {
                il.Emit(OpCodes.Ldnull);
            }
        }
        private void EmitReference(ILGenerator il, object reference, Type referenceType = null)
        {
            if (referenceType == null)
            {
                referenceType = reference.GetType();
            }

            var fieldBuilder = _typeBuilder.DefineField(
                "_reference" + Guid.NewGuid().ToString().Split(new char[] { '-' })[0],
                referenceType,
                FieldAttributes.Private);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fieldBuilder);

            _references.Add(fieldBuilder, reference);
        }
        private void EmitInvokeAction(ILGenerator il, object action, MethodInfo methodInfo = null)
        {
            Type actionType = action.GetType();
            Type[] parameterTypes = Type.EmptyTypes;

            if (methodInfo != null)
            {
                var parameters = methodInfo.GetParameters();
                
                for (int parameterIndex = 1; parameterIndex <= parameters.Length; parameterIndex++)
                {
                    EmitLdArgument(methodInfo, il, parameterIndex);
                }

                parameterTypes = parameters.Select(pi => pi.ParameterType).ToArray();
            }

            var invokeMethod = actionType.GetMethod("Invoke", parameterTypes);
            il.Emit(OpCodes.Callvirt, invokeMethod);
        }
        private void EmitTypeOf(ILGenerator il, Type type)
        {
            il.Emit(OpCodes.Ldtoken, type);
            il.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
        }
        private Type ConstructGenericParameter(ParameterInfo parameterInfo, IDictionary<Type, Type> typeMapper)
        {
            return ConstructGenericType(parameterInfo.ParameterType, typeMapper);
        }
        private Type ConstructGenericType(Type type, IDictionary<Type, Type> typeMapper)
        {
            if (type.IsArray)
            {
                // TODO: Figure out a better way to construct the type of an array of a given type
                return new ArrayList().ToArray(ConstructGenericType(type.GetElementType(), typeMapper)).GetType();
            }
            else if (type.IsGenericParameter)
            {
                return typeMapper[type];
            }
            else if (type.IsGenericTypeDefinition)
            {
                var genericTypes = type.GetGenericArguments().Select(t => typeMapper[t]).ToArray();
                return type.MakeGenericType(genericTypes);
            }

            return type;
        }
    }
}