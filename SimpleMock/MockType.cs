using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq.Expressions;
using System.Security;

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

            var typeBuilder = moduleBuilder.DefineType(
                baseType.Name + randomString,
                TypeAttributes.Class | TypeAttributes.Public,
                baseType,
                interfaceType != null ? new[] { interfaceType } : new Type[0]);
            
            _typeBuilder = typeBuilder;

            if (interfaceType != null)
            {
                typeBuilder.AddInterfaceImplementation(interfaceType);
            }

            CreateMethodStubs();

            DefineConstructor();

            mockType = typeBuilder.CreateType();

            return (T)Activator.CreateInstance(mockType, _references.Select(r => r.Value).ToArray());
        }

        /// <summary>
        /// Creates a constructor that takes all the references as parameters and assigns them to private fields.
        /// </summary>
        private void DefineConstructor()
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
        private void CreateMethodStubs()
        {
            Type mockType = typeof(T);

            var propertiesToImplement =
                    mockType.IsInterface
                    ? mockType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList()
                    : new List<PropertyInfo>();

            var methodsToOverride =
                    mockType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                            .Where(mi => mi.IsAbstract || mi.IsVirtual)
                            .ToList();

            foreach (var propertyInfo in propertiesToImplement)
            {
                // TODO: Implement CreatePropertyStub(...)
                Console.WriteLine(propertyInfo);
            }
            foreach (var methodInfo in methodsToOverride)
            {
                CreateMethodStub(methodInfo);
            }
        }

        /// <summary>
        /// Creates a stub implementation for the method if it's abstract. Otherwise it creates the defined behavior.
        /// </summary>
        /// <param name="methodInfo"></param>
        private void CreateMethodStub(MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters().Select(pi => pi.ParameterType).ToArray();

            var methodBuilder = _typeBuilder.DefineMethod(
                                            methodInfo.Name,
                                            MethodAttributes.Public | MethodAttributes.Virtual,
                                            methodInfo.ReturnType,
                                            parameters);

            var il = methodBuilder.GetILGenerator();

            var methodMocks = _methodMocks.Where(
                m =>
                {
                    var method = ((MethodCallExpression)((LambdaExpression)m.MethodExpression).Body).Method;

                    bool matches = method.ReturnType == methodInfo.ReturnType;
                    matches &= method.Name == methodInfo.Name;

                    var leftEnumerator = method.GetParameters().GetEnumerator();
                    var rightEnumerator = methodInfo.GetParameters().GetEnumerator();

                    bool leftNext = leftEnumerator.MoveNext();
                    bool rightNext = rightEnumerator.MoveNext();
                    while (leftNext && rightNext)
                    {
                        matches &= ((ParameterInfo)leftEnumerator.Current).ParameterType == ((ParameterInfo)rightEnumerator.Current).ParameterType;
                        leftNext = leftEnumerator.MoveNext();
                        rightNext = rightEnumerator.MoveNext();
                    }

                    return matches;
                });

            if (methodMocks.Any())
            {
                foreach (var mock in methodMocks)
                {
                    EmitMock(methodInfo, il, mock);
                }
            }

            il.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Throw);

            _typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
        }

        private void EmitMock(MethodInfo methodInfo, ILGenerator il, MethodMock methodMock)
        {
            var lambdaExpression = methodMock.MethodExpression as LambdaExpression;
            if (lambdaExpression == null)
            {
                throw new ArgumentException("Invalid expression, expecting a lambda", "methodMock");
            }

            var methodExpression = lambdaExpression.Body as MethodCallExpression;
            if (methodExpression == null)
            {
                throw new ArgumentException("Invalid expression, expecting a method call", "methodMock");
            }

            if (methodMock is MethodImplementationMockBase)
            {
                var methodImplementationMock = (MethodImplementationMockBase)methodMock;

                EmitCustomImplementation(methodInfo, il, methodImplementationMock);
            }
            else if (methodMock is MethodParameterMockBase)
            {
                var methodParameterMock = (MethodParameterMockBase)methodMock;

                EmitGeneratedImplementation(il, methodExpression, methodParameterMock);
            }
        }
        private void EmitGeneratedImplementation(ILGenerator il, MethodCallExpression methodExpression, MethodParameterMockBase methodParameterMock)
        {
            Label exitLabel = il.DefineLabel();

            int argumentIndex = 1;
            foreach (var expression in methodExpression.Arguments)
            {
                EmitBranch(il, expression, exitLabel, argumentIndex);

                argumentIndex++;
            }

            if (methodParameterMock.MethodCompletesMock is MethodReturnsMockBase)
            {
                var methodReturnsMock = ((MethodReturnsMockBase)methodParameterMock.MethodCompletesMock);

                if (methodReturnsMock.Callback != null)
                {
                    EmitReference(il, methodReturnsMock.Callback);

                    EmitInvokeAction(il, methodReturnsMock.Callback);
                }

                EmitConstant(il, methodReturnsMock.ReturnValue, methodReturnsMock.ReturnType);

                il.Emit(OpCodes.Ret);
            }
            else if (methodParameterMock.MethodCompletesMock is MethodThrowsMockBase)
            {
                Type exceptionType = ((MethodThrowsMockBase)methodParameterMock.MethodCompletesMock).ExceptionType;
                il.Emit(OpCodes.Newobj, exceptionType.GetConstructor(Type.EmptyTypes));
                il.Emit(OpCodes.Throw);
            }

            il.MarkLabel(exitLabel);            
        }
        private void EmitCustomImplementation(MethodInfo methodInfo, ILGenerator il, MethodImplementationMockBase methodImplementationMock)
        {
            var implementation = methodImplementationMock.CustomImplemenation;
            if (implementation == null)
            {
                return;
            }

            EmitReference(il, implementation);

            EmitInvokeAction(il, implementation, methodInfo);

            il.Emit(OpCodes.Ret);
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
                for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
                {
                    il.Emit(OpCodes.Ldarg, parameterIndex + 1);
                }

                parameterTypes = parameters.Select(pi => pi.ParameterType).ToArray();
            }

            var invokeMethod = actionType.GetMethod("Invoke", parameterTypes);
            il.Emit(OpCodes.Callvirt, invokeMethod);
        }
        private void EmitBranch(ILGenerator il, Expression expression, Label exitLabel, int argumentIndex)
        {
            Type argumentType;
            object argumentValue;
            ExpressionHelpers.GetArgumentValueAndType(expression, out argumentValue, out argumentType);

            var defaultMethod = typeof(EqualityComparer<>).MakeGenericType(new[] { argumentType }).GetMethod("get_Default");
            il.Emit(OpCodes.Call, defaultMethod);
            il.Emit(OpCodes.Ldarg, argumentIndex);
            EmitConstant(il, argumentValue, argumentType);

            var equalsMethod = defaultMethod.ReturnType.GetMethods(BindingFlags.Instance | BindingFlags.Public).First(m => m.Name == "Equals" && m.GetParameters().Count() == 2);
            il.Emit(OpCodes.Callvirt, equalsMethod);
            il.Emit(OpCodes.Brfalse_S, exitLabel);
        }
        private void EmitConstant(ILGenerator il, object value, Type type)
        {
            if (value == null)
            {
                EmitDefault(il, type);
            }
            else if (type.IsClass)
            {
                EmitClassConstant(il, value, type);
            }
            else if (type.IsValueType)
            {
                EmitValueTypeConstant(il, value, type);
            }
            else
            {
                throw new NotSupportedException("This type is not supported!");
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
        private void EmitClassConstant(ILGenerator il, object value, Type type)
        {
            EmitReference(il, value, type);
        }
        private void EmitValueTypeConstant(ILGenerator il, object value, Type type)
        {
            if (!type.IsValueType)
            {
                throw new ArgumentException("Expecting a ValueType", "type");
            }

            Type baseType = type;
            bool isNullable = false;
            if (Nullable.GetUnderlyingType(type) != null)
            {
                baseType = type.GetGenericArguments().First();
                isNullable = true;
            }

            if (baseType.IsPrimitive)
            {
                EmitPrimitive(il, value, baseType);
            }
            else if (baseType.IsEnum)
            {
                EmitEnum(il, value, baseType);
            }
            else
            {
                // Then it must be a struct
                EmitStruct(il, value, baseType);
            }

            if (isNullable)
            {
                var constructor = type.GetConstructor(new[] { baseType });
                il.Emit(OpCodes.Newobj, constructor);
            }
        }
        private void EmitPrimitive(ILGenerator il, object value, Type baseType)
        {
            if (!baseType.IsPrimitive)
            {
                throw new ArgumentException("Expecting a primitive", "baseType");
            }

            if (baseType == typeof(bool))
            {
                if ((bool)value)
                {
                    il.Emit(OpCodes.Ldc_I4_1);
                }
                else
                {
                    il.Emit(OpCodes.Ldc_I4_0);
                }
            }
            else if (baseType == typeof(Char))
            {
                il.Emit(OpCodes.Ldc_I4, (Char)value);
                il.Emit(OpCodes.Conv_I2);
            }
            else if (baseType == typeof(byte))
            {
                il.Emit(OpCodes.Ldc_I4, (byte)value);
                il.Emit(OpCodes.Conv_I1);
            }
            else if (baseType == typeof(Int16))
            {
                il.Emit(OpCodes.Ldc_I4, (Int16)value);
                il.Emit(OpCodes.Conv_I2);
            }
            else if (baseType == typeof(Int32))
            {
                il.Emit(OpCodes.Ldc_I4, (Int32)value);
            }
            else if (baseType == typeof(Int64))
            {
                il.Emit(OpCodes.Ldc_I8, (Int64)value);
            }
            else if (baseType == typeof(UInt16))
            {
                il.Emit(OpCodes.Ldc_I4, (UInt16)value);
                il.Emit(OpCodes.Conv_U2);
            }
            else if (baseType == typeof(UInt32))
            {
                il.Emit(OpCodes.Ldc_I4, (UInt32)value);
                il.Emit(OpCodes.Conv_U4);
            }
            else if (baseType == typeof(UInt64))
            {
                il.Emit(OpCodes.Ldc_I8, (UInt64)value);
                il.Emit(OpCodes.Conv_U8);
            }
            else if (baseType == typeof(Single))
            {
                il.Emit(OpCodes.Ldc_R4, (Single)value);
            }
            else if (baseType == typeof(Double))
            {
                il.Emit(OpCodes.Ldc_R8, (Double)value);
            }
        }
        private void EmitEnum(ILGenerator il, object value, Type baseType)
        {
            if (!baseType.IsEnum)
            {
                throw new ArgumentException("Expecting an enum", "baseType");
            }

            il.Emit(OpCodes.Ldc_I4, (Int32)value);
        }
        private void EmitStruct(ILGenerator il, object value, Type baseType)
        {
            if (baseType.IsEnum || baseType.IsPrimitive || !baseType.IsValueType)
            {
                throw new ArgumentException("Expecting a struct", "baseType");
            }

            EmitReference(il, value, baseType);
        }
    }
}
