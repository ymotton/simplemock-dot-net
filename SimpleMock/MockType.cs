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

            var methods = _methodParameterMocks.Where(
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
            
            if (methods.Any())
            {
                CreateMethodBranch(il, methods);
                EmitDefault(il, methodInfo);
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
        /// <param name="methodParameterMocks"></param>
        private void CreateMethodBranch(ILGenerator il, IEnumerable<Mock<T>.IMethodParameterMock> methodParameterMocks)
        {
            foreach (var methodParameterMock in methodParameterMocks)
            {
                var method = (MethodCallExpression)((LambdaExpression)methodParameterMock.MethodExpression).Body;
                foreach (Expression argumentExpression in method.Arguments)
                {
                    Console.WriteLine(argumentExpression);
                }
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
        private void EmitBranch(ILGenerator il, Expression argumentExpression)
        {
            if (argumentExpression is ConstantExpression)
            {
                var constant = (ConstantExpression) argumentExpression;
                Console.WriteLine(constant);
                //constant.
            }
        }
    }
}
