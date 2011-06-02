using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq.Expressions;

namespace SimpleMock
{
    internal class MockType<T>
        where T : class
    {
        private IEnumerable<Mock<T>.IMethodParameterMock> _methodParameterMocks;
        public MockType(IEnumerable<Mock<T>.IMethodParameterMock> methodParameterMocks)
        {
            _methodParameterMocks = methodParameterMocks;
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

            if (interfaceType != null)
            {
                typeBuilder.AddInterfaceImplementation(interfaceType);
            }

            CreateStubImplementation(typeBuilder);

            mockType = typeBuilder.CreateType();

            return (T)Activator.CreateInstance(mockType, new object[0]);
        }

        private void CreateStubImplementation(TypeBuilder typeBuilder)
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
                CreateMethodStub(typeBuilder, methodInfo);
            }
        }
        private void CreateMethodStub(TypeBuilder typeBuilder, MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters().Select(pi => pi.ParameterType).ToArray();

            var methodBuilder = typeBuilder.DefineMethod(
                                            methodInfo.Name,
                                            MethodAttributes.Public | MethodAttributes.Virtual,
                                            methodInfo.ReturnType,
                                            parameters);
            
            var il = methodBuilder.GetILGenerator();

            var methodsMocks = _methodParameterMocks.Where(
                m =>
                    {
                        var method = ((MethodCallExpression) ((LambdaExpression) m.MethodExpression).Body).Method;

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
            
            if (methodsMocks.Any())
            {
                EmitMethodBranch(il, methodInfo, methodsMocks);
                il.Emit(OpCodes.Ret);
            }
            else
            {
                il.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(Type.EmptyTypes));
                il.Emit(OpCodes.Throw);
            }
            
            typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
        }
        /// <summary>
        /// TODO: Parse the expression tree, and convert it to IL manually
        /// </summary>
        /// <param name="il"></param>
        /// <param name="methodInfo"></param>
        /// <param name="methodParameterMocks"></param>
        private void EmitMethodBranch(ILGenerator il, MethodInfo methodInfo, IEnumerable<Mock<T>.IMethodParameterMock> methodParameterMocks)
        {
            foreach (var methodParameterMock in methodParameterMocks)
            {
                EmitBranch(il, methodInfo, methodParameterMock);
            }
        }
        private void EmitDefault(ILGenerator il, MethodInfo methodInfo)
        {
            if (methodInfo.ReturnType.IsValueType && methodInfo.ReturnType != typeof(void))
            {
                if (methodInfo.ReturnType.IsPrimitive || methodInfo.ReturnType.IsEnum)
                {
                    il.Emit(OpCodes.Ldc_I4_0);
                }
                else
                {
                    LocalBuilder local = il.DeclareLocal(methodInfo.ReturnType);
                    il.Emit(OpCodes.Ldloca_S, local);
                    il.Emit(OpCodes.Initobj, methodInfo.ReturnType);
                    il.Emit(OpCodes.Ldloc_0);
                }
            }
            else if (methodInfo.ReturnType.IsClass)
            {
                il.Emit(OpCodes.Ldnull);
            }
        }
        private void EmitBranch(ILGenerator il, MethodInfo methodInfo, Mock<T>.IMethodParameterMock methodParameterMock)
        {
            var references = new List<object>();

            var method = (MethodCallExpression)((LambdaExpression)methodParameterMock.MethodExpression).Body;

            var pairs = from parameter in methodInfo.GetParameters()
                        from argument in method.Arguments
                        select new
                                   {
                                       Parameter = parameter,
                                       Expression = (ConstantExpression)argument
                                   };

            var exitLabel = il.DefineLabel();
            
            int index = 1;
            foreach (var pair in pairs)
            {
                var argumentExpression = pair.Expression;

                if (argumentExpression.Type.IsClass)
                {
                    var defaultMethod = typeof (EqualityComparer<>).MakeGenericType(new[] {argumentExpression.Type}).GetMethod("get_Default");
                    il.Emit(OpCodes.Call, defaultMethod);
                    il.Emit(OpCodes.Ldarg, index);
                    LoadConstant(references, il, argumentExpression.Value, argumentExpression.Value.GetType());
                    var equalsMethod = defaultMethod.ReturnType.GetMethods(BindingFlags.Instance | BindingFlags.Public).First(m => m.Name == "Equals" && m.GetParameters().Count() == 2);
                    il.Emit(OpCodes.Callvirt, equalsMethod);
                    il.Emit(OpCodes.Brfalse_S, exitLabel);
                }

                index++;
            }

            if (methodParameterMock.MethodReturnsMock is Mock<T>.IMethodReturnsMock)
            {
                object returnValue = ((Mock<T>.IMethodReturnsMock)methodParameterMock.MethodReturnsMock).ReturnValue;
                LoadConstant(references, il, returnValue, returnValue.GetType());
                il.Emit(OpCodes.Ret);
            }
            else if (methodParameterMock.MethodReturnsMock is Mock<T>.IMethodThrowsMock)
            {
                Type exceptionType = ((Mock<T>.IMethodThrowsMock)methodParameterMock.MethodReturnsMock).ExceptionType;
                il.Emit(OpCodes.Newobj, exceptionType.GetConstructor(Type.EmptyTypes));
                il.Emit(OpCodes.Throw);
            }
            il.MarkLabel(exitLabel);
        }
        private void LoadConstant(List<object> references, ILGenerator il, object constantValue, Type constantType)
        {
            int index = references.IndexOf(constantValue);

            if (index < 0)
            {
                index = references.Count;
                references.Add(constantValue);
            }

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4_4, index);
            il.Emit(OpCodes.Ldelem_Ref);
            if (constantType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, constantType);
            }
            else
            {
                il.Emit(OpCodes.Castclass, constantType);
            }
        }
    }
}
