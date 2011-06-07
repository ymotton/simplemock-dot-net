using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SimpleMock
{
    #region Fluent API Nested Type Baseclasses

    public abstract class MethodMock
    {
        protected MethodMock(Expression expresison)
        {
            MethodExpression = expresison;
        }

        internal Expression MethodExpression { get; set; }
    }
    public abstract class MethodImplementationMockBase : MethodMock
    {
        protected MethodImplementationMockBase(Expression expresison, object implementation)
            : base(expresison)
        {
            CustomImplemenation = implementation;
        }

        internal object CustomImplemenation { get; set; }
    }
    public abstract class MethodParameterMockBase : MethodMock
    {
        protected MethodParameterMockBase(Expression expresison)
            : base(expresison)
        {
        }

        internal MethodCompletesMockBase MethodCompletesMock { get; set; }
    }
    public abstract class MethodCompletesMockBase
    {
        
    }
    public abstract class MethodReturnsMockBase : MethodCompletesMockBase
    {
        internal object ReturnValue { get; set; }
        internal Type ReturnType { get; set; }
        internal Action Callback { get; set; }
    }
    public abstract class MethodThrowsMockBase : MethodCompletesMockBase
    {
        internal Type ExceptionType { get; set; }
        internal Func<Exception> ExceptionInitializer { get; set; }
        internal Action Callback { get; set; }
    }

    #endregion

    /// <summary>
    /// A class that helps define and generate a mock implementation of a class or an interface.
    /// </summary>
    /// <typeparam name="T">The class or interface to be mocked.</typeparam>
    public class Mock<T> : IInvisibleSystemObjectMethods
        where T : class
    {
        #region Properties

        /// <summary>
        /// Lazily creates an instance of the (interface) type that has been defined.
        /// </summary>
        public T Instance
        {
            get
            {
                if (EqualityComparer<T>.Default.Equals(_instance, default(T)))
                {
                    _instance = new MockType<T>(MethodMocks).BuildUp();
                }

                return _instance;
            }
        }
        private T _instance;

        protected IList<MethodMock> MethodMocks
        {
            get
            {
                if (_methodMocks == null)
                {
                    _methodMocks = new List<MethodMock>();
                }
                
                return _methodMocks;
            }
        }
        private IList<MethodMock> _methodMocks;

        #endregion

        /// <summary>
        /// Defines a mock implementation for given method with given argument.
        /// </summary>
        /// <typeparam name="TReturn">The returntype of the method that is being mocked.</typeparam>
        /// <param name="methodExpression">The method to be mocked with a specific set of arguments.</param>
        /// <returns></returns>
        public MethodParameterMock<TReturn> HasMethod<TReturn>(Expression<Func<T, TReturn>> methodExpression)
        {
            var methodParameterMock = new MethodParameterMock<TReturn>(methodExpression);
            AddAndReturnMethodMock(methodExpression, methodParameterMock);
            return methodParameterMock;
        }

        /// <summary>
        /// Defines a mock implementation for given method with given argument.
        /// </summary>
        /// <typeparam name="TReturn">The returntype of the method that is being mocked.</typeparam>
        /// <param name="methodExpression">The method to be mocked with arguments to disambiguate between overloads.</param>
        /// <param name="implementation">A delegate that matches the method signature of the method to be mocked, to provide a custom implementation.</param>
        /// <remarks>The arguments will be ignored as the custom implementation defines what will be returned.</remarks>
        /// <returns></returns>
        public MethodImplementationMock<TReturn> HasMethod<TReturn>(Expression<Func<T, TReturn>> methodExpression, Func<TReturn> implementation)
        {
            return AddAndReturnMethodMock(methodExpression, new MethodImplementationMock<TReturn>(methodExpression, implementation));
        }
        /// <summary>
        /// Defines a mock implementation for given method with given argument.
        /// </summary>
        /// <typeparam name="TArg1">The first argument of the method that is being mocked.</typeparam>
        /// <typeparam name="TReturn">The returntype of the method that is being mocked.</typeparam>
        /// <param name="methodExpression">The method to be mocked with arguments to disambiguate between overloads.</param>
        /// <param name="implementation">A delegate that matches the method signature of the method to be mocked, to provide a custom implementation.</param>
        /// <remarks>The arguments will be ignored as the custom implementation defines what will be returned.</remarks>
        /// <returns></returns>
        public MethodImplementationMock<TArg1, TReturn> HasMethod<TArg1, TReturn>(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TReturn> implementation)
        {
            return AddAndReturnMethodMock(methodExpression, new MethodImplementationMock<TArg1, TReturn>(methodExpression, implementation));
        }
        /// <summary>
        /// Defines a mock implementation for given method with given argument.
        /// </summary>
        /// <typeparam name="TArg1">The first argument of the method that is being mocked.</typeparam>
        /// <typeparam name="TArg2">The second argument of the method that is being mocked.</typeparam>
        /// <typeparam name="TReturn">The returntype of the method that is being mocked.</typeparam>
        /// <param name="methodExpression">The method to be mocked with arguments to disambiguate between overloads.</param>
        /// <param name="implementation">A delegate that matches the method signature of the method to be mocked, to provide a custom implementation.</param>
        /// <remarks>The arguments will be ignored as the custom implementation defines what will be returned.</remarks>
        /// <returns></returns>
        public MethodImplementationMock<TArg1, TArg2, TReturn> HasMethod<TArg1, TArg2, TReturn>(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TArg2, TReturn> implementation)
        {
            return AddAndReturnMethodMock(methodExpression, new MethodImplementationMock<TArg1, TArg2, TReturn>(methodExpression, implementation));
        }
        /// <summary>
        /// Defines a mock implementation for given method with given argument.
        /// </summary>
        /// <typeparam name="TArg1">The first argument of the method that is being mocked.</typeparam>
        /// <typeparam name="TArg2">The second argument of the method that is being mocked.</typeparam>
        /// <typeparam name="TArg3">The third argument of the method that is being mocked.</typeparam>
        /// <typeparam name="TReturn">The returntype of the method that is being mocked.</typeparam>
        /// <param name="methodExpression">The method to be mocked with arguments to disambiguate between overloads.</param>
        /// <param name="implementation">A delegate that matches the method signature of the method to be mocked, to provide a custom implementation.</param>
        /// <remarks>The arguments will be ignored as the custom implementation defines what will be returned.</remarks>
        /// <returns></returns>
        public MethodImplementationMock<TArg1, TArg2, TArg3, TReturn> HasMethod<TArg1, TArg2, TArg3, TReturn>(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TArg2, TArg3, TReturn> implementation)
        {
            return AddAndReturnMethodMock(methodExpression, new MethodImplementationMock<TArg1, TArg2, TArg3, TReturn>(methodExpression, implementation));
        }
        /// <summary>
        /// Defines a mock implementation for given method with given argument.
        /// </summary>
        /// <typeparam name="TArg1">The first argument of the method that is being mocked.</typeparam>
        /// <typeparam name="TArg2">The second argument of the method that is being mocked.</typeparam>
        /// <typeparam name="TArg3">The third argument of the method that is being mocked.</typeparam>
        /// <typeparam name="TArg4">The fourth argument of the method that is being mocked.</typeparam>
        /// <typeparam name="TReturn">The returntype of the method that is being mocked.</typeparam>
        /// <param name="methodExpression">The method to be mocked with arguments to disambiguate between overloads.</param>
        /// <param name="implementation">A delegate that matches the method signature of the method to be mocked, to provide a custom implementation.</param>
        /// <remarks>The arguments will be ignored as the custom implementation defines what will be returned.</remarks>
        /// <returns></returns>
        public MethodImplementationMock<TArg1, TArg2, TArg3, TArg4, TReturn> HasMethod<TArg1, TArg2, TArg3, TArg4, TReturn>(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TArg2, TArg3, TArg4, TReturn> implementation)
        {
            return AddAndReturnMethodMock(methodExpression, new MethodImplementationMock<TArg1, TArg2, TArg3, TArg4, TReturn>(methodExpression, implementation));
        }
        /// <summary>
        /// Defines a mock implementation for given method with given argument.
        /// </summary>
        /// <typeparam name="TArg1">The first argument of the method that is being mocked.</typeparam>
        /// <typeparam name="TArg2">The second argument of the method that is being mocked.</typeparam>
        /// <typeparam name="TArg3">The third argument of the method that is being mocked.</typeparam>
        /// <typeparam name="TArg4">The fourth argument of the method that is being mocked.</typeparam>
        /// <typeparam name="TArg5">The fifth argument of the method that is being mocked.</typeparam>
        /// <typeparam name="TReturn">The returntype of the method that is being mocked.</typeparam>
        /// <param name="methodExpression">The method to be mocked with arguments to disambiguate between overloads.</param>
        /// <param name="implementation">A delegate that matches the method signature of the method to be mocked, to provide a custom implementation.</param>
        /// <remarks>The arguments will be ignored as the custom implementation defines what will be returned.</remarks>
        /// <returns></returns>
        public MethodImplementationMock<TArg1, TArg2, TArg3, TArg4, TArg5, TReturn> HasMethod<TArg1, TArg2, TArg3, TArg4, TArg5, TReturn>(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TReturn> implementation)
        {
            return AddAndReturnMethodMock(methodExpression, new MethodImplementationMock<TArg1, TArg2, TArg3, TArg4, TArg5, TReturn>(methodExpression, implementation));
        }

        private TMethodMock AddAndReturnMethodMock<TMethodMock>(Expression expression, TMethodMock methodMock)
            where TMethodMock: MethodMock
        {
            MethodMocks.Add(methodMock);

            return methodMock;
        }

        #region Fluent API Nested Types
        
        public class MethodParameterMock<TReturn> : MethodParameterMockBase, IInvisibleSystemObjectMethods
        {
            internal MethodParameterMock(Expression<Func<T, TReturn>> methodExpression)
                : base (methodExpression)
            {
            }

            /// <summary>
            /// Specifies a value to be returned for specified method and arguments.
            /// </summary>
            /// <param name="returnValue">The value to be returned.</param>
            /// <returns></returns>
            public MethodReturnsMock<TReturn> Returns(TReturn returnValue)
            {
                var methodReturnsMock = new MethodReturnsMock<TReturn>(returnValue);
                MethodCompletesMock = methodReturnsMock;
                return methodReturnsMock;
            }

            /// <summary>
            /// Specifies an exception to be thrown for specified method and arguments.
            /// </summary>
            /// <typeparam name="TException">The type of exception to be thrown.</typeparam>
            /// <remarks>The exception should have a parameterless constructor.</remarks>
            /// <returns></returns>
            public MethodThrowsMock<TException> Throws<TException>()
                where TException : Exception
            {
                var methodThrowsMock = new MethodThrowsMock<TException>();
                MethodCompletesMock = methodThrowsMock;
                return methodThrowsMock;
            }

            /// <summary>
            /// Specifies a delegate that returns an exception to be thrown for specified method and arguments.
            /// </summary>
            /// <param name="exceptionInitializer">The delegate that returns the exception to be thrown.</param>
            /// <typeparam name="TException">The type of exception to be thrown.</typeparam>
            /// <returns></returns>
            public MethodThrowsMock<TException> Throws<TException>(Func<TException> exceptionInitializer)
                where TException : Exception
            {
                var methodThrowsMock = new MethodThrowsMock<TException>(exceptionInitializer);
                MethodCompletesMock = methodThrowsMock;
                return methodThrowsMock;
            }
        }
        public class MethodImplementationMock<TReturn> : MethodImplementationMockBase, IInvisibleSystemObjectMethods
        {
            internal MethodImplementationMock(Expression<Func<T, TReturn>> methodExpression, Func<TReturn> implementation)
                : base(methodExpression, implementation)
            {
            }
        }
        public class MethodImplementationMock<TArg1, TReturn> : MethodImplementationMockBase, IInvisibleSystemObjectMethods
        {
            internal MethodImplementationMock(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TReturn> implementation)
                : base(methodExpression, implementation)
            {
            }
        }
        public class MethodImplementationMock<TArg1, TArg2, TReturn> : MethodImplementationMockBase, IInvisibleSystemObjectMethods
        {
            internal MethodImplementationMock(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TArg2, TReturn> implementation)
                : base(methodExpression, implementation)
            {
            }
        }
        public class MethodImplementationMock<TArg1, TArg2, TArg3, TReturn> : MethodImplementationMockBase, IInvisibleSystemObjectMethods
        {
            internal MethodImplementationMock(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TArg2, TArg3, TReturn> implementation)
                : base(methodExpression, implementation)
            {
            }
        }
        public class MethodImplementationMock<TArg1, TArg2, TArg3, TArg4, TReturn> : MethodImplementationMockBase, IInvisibleSystemObjectMethods
        {
            internal MethodImplementationMock(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TArg2, TArg3, TArg4, TReturn> implementation)
                : base(methodExpression, implementation)
            {
            }
        }
        public class MethodImplementationMock<TArg1, TArg2, TArg3, TArg4, TArg5, TReturn> : MethodImplementationMockBase, IInvisibleSystemObjectMethods
        {
            internal MethodImplementationMock(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TReturn> implementation)
                : base(methodExpression, implementation)
            {
            }
        }
        public class MethodReturnsMock<TReturn> : MethodReturnsMockBase, IInvisibleSystemObjectMethods
        {
            internal MethodReturnsMock(TReturn returnValue)
            {
                ReturnValue = returnValue;
                ReturnType = typeof (TReturn);
            }

            /// <summary>
            /// Subscribes a callback handler to be executed whenever a defined branch is hit.
            /// </summary>
            /// <param name="callback">The callback handler to be executed.</param>
            public void Subscribe(Action callback)
            {
                Callback = callback;
            }
        }
        public class MethodThrowsMock<TException> : MethodThrowsMockBase, IInvisibleSystemObjectMethods
            where TException : Exception
        {
            internal MethodThrowsMock()
            {
                ExceptionType = typeof (TException);
            }
            internal MethodThrowsMock(Func<TException> exceptionInitializer)
            {
                ExceptionType = typeof(TException);
                ExceptionInitializer = () => exceptionInitializer();
            }
        }

        #endregion
    }
}
