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
        private bool _cancelBuildUp;
        private Exception _cancelBuildUpException;

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

            try
            {
                if (methodsMocks.Any() && !_cancelBuildUp)
                {
                    EmitMocks(il, methodsMocks);
                }
            }
            catch (Exception exception)
            {
                _cancelBuildUpException = exception;
                _cancelBuildUp = true;
            }

            il.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Throw);

            typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
        }

        /// <summary>
        /// Parses the expression tree and converts it to stub implementations in IL.
        /// </summary>
        /// <param name="il"></param>
        /// <param name="methodInfo"></param>
        /// <param name="methodParameterMocks"></param>
        private static void EmitMocks(ILGenerator il, IEnumerable<Mock<T>.IMethodParameterMock> methodParameterMocks)
        {
            foreach (var mock in methodParameterMocks)
            {
                EmitMock(il, mock);
            }
        }
        private static void EmitMock(ILGenerator il, Mock<T>.IMethodParameterMock methodParameterMock)
        {
            var lambdaExpression = methodParameterMock.MethodExpression as LambdaExpression;
            if (lambdaExpression == null)
            {
                throw new ArgumentException("Invalid expression, expecting a lambda", "methodParameterMock");
            }
            
            var methodExpression = lambdaExpression.Body as MethodCallExpression;
            if (methodExpression == null)
            {
                throw new ArgumentException("Invalid expression, expecting a method call", "methodParameterMock");
            }
            
            Label exitLabel = il.DefineLabel();

            int argumentIndex = 1;
            foreach (var expression in methodExpression.Arguments)
            {
                EmitBranch(il, expression, exitLabel, argumentIndex);

                argumentIndex++;
            }

            if (methodParameterMock.MethodReturnsMock is Mock<T>.IMethodReturnsMock)
            {
                var methodReturnsMock = ((Mock<T>.IMethodReturnsMock)methodParameterMock.MethodReturnsMock);
                EmitConstant(il, methodReturnsMock.ReturnValue, methodReturnsMock.ReturnType);
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
        private static void EmitBranch(ILGenerator il, Expression expression, Label exitLabel, int argumentIndex)
        {
            Type argumentType;
            object argumentValue;
            GetArgumentValueAndType(expression, out argumentValue, out argumentType);

            var defaultMethod = typeof(EqualityComparer<>).MakeGenericType(new[] { argumentType }).GetMethod("get_Default");
            il.Emit(OpCodes.Call, defaultMethod);
            il.Emit(OpCodes.Ldarg, argumentIndex);
            EmitConstant(il, argumentValue, argumentType);
        
            var equalsMethod = defaultMethod.ReturnType.GetMethods(BindingFlags.Instance | BindingFlags.Public).First(m => m.Name == "Equals" && m.GetParameters().Count() == 2);
            il.Emit(OpCodes.Callvirt, equalsMethod);
            il.Emit(OpCodes.Brfalse_S, exitLabel);
        }
        private static void EmitConstant(ILGenerator il, object value, Type type)
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
        private static void EmitDefault(ILGenerator il, Type type)
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
        private static void EmitClassConstant(ILGenerator il, object value, Type type)
        {
            if (type == typeof(string))
            {
                il.Emit(OpCodes.Ldstr, (string)value);
            }
            else
            {
                throw new NotImplementedException("Other reference types are not supported");
            }
        }
        private static void EmitValueTypeConstant(ILGenerator il, object value, Type type)
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
        private static void EmitPrimitive(ILGenerator il, object value, Type baseType)
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
        private static void EmitEnum(ILGenerator il, object value, Type baseType)
        {
            if (!baseType.IsEnum)
            {
                throw new ArgumentException("Expecting an enum", "baseType");
            }

            il.Emit(OpCodes.Ldc_I4, (Int32)value);
        }
        private static void EmitStruct(ILGenerator il, object value, Type baseType)
        {
            if (baseType.IsEnum || baseType.IsPrimitive || !baseType.IsValueType)
            {
                throw new ArgumentException("Expecting a struct", "baseType");
            }

            throw new InvalidOperationException("Structs are currently unsupported");
        }

        private static void GetArgumentValueAndType(Expression expression, out object value, out Type type)
        {
            if (expression is ConstantExpression)
            {
                GetConstant(
                    expression,
                    out value,
                    out type
                );
            }
            else if (expression is UnaryExpression)
            {
                GetUnary(
                    expression,
                    out value,
                    out type
                );
            }
            else if (expression is MemberExpression)
            {
                GetMember(
                    expression,
                    out value,
                    out type
                );
            }
            else
            {
                throw new InvalidOperationException("Expression Not supported");
            }

        }
        private static void GetConstant(Expression expression, out object value, out Type type)
        {
            var constantExpression = expression as ConstantExpression;
            
            if (constantExpression == null)
            {
                throw new ArgumentException("expression");
            }

            type = constantExpression.Type;
            value = constantExpression.Value;
        }
        private static void GetUnary(Expression expression, out object value, out Type type)
        {
            var unaryExpression = expression as UnaryExpression;

            if (unaryExpression == null)
            {
                throw new ArgumentException("expression");
            }

            var constantExpression = unaryExpression.Operand as ConstantExpression;

            if (constantExpression == null)
            {
                throw new NotSupportedException(
                    string.Format(
                        "This type of Operand is not supported for UnaryExpressions ({0})",
                        unaryExpression.Operand.GetType())
                );
            }

            type = typeof(Nullable<>).MakeGenericType(constantExpression.Type);
            value = constantExpression.Value;
        }
        private static void GetMember(Expression expression, out object value, out Type type)
        {
            var memberExpression = expression as MemberExpression;

            if (memberExpression == null)
            {
                throw new ArgumentException("expression");
            }

            var objectMember = Expression.Convert(memberExpression, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();

            value = getter();
            type = memberExpression.Type;
        }
    }
}
