using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SimpleMock
{
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

    public class Mock<T> : IInvisibleSystemObjectMethods
        where T : class
    {
        #region Properties

        private T _instance;
        public T Instance
        {
            get
            {
                if (EqualityComparer<T>.Default.Equals(_instance, default(T)))
                {
                    _instance = new MockType<T>(MethodMocks.Select(p => p.Value)).BuildUp();
                }

                return _instance;
            }
        }

        private IDictionary<int, MethodMock> _methodMocks;
        protected IDictionary<int, MethodMock> MethodMocks
        {
            get
            {
                if (_methodMocks == null)
                {
                    _methodMocks = new Dictionary<int, MethodMock>();
                }
                
                return _methodMocks;
            }
        }

        #endregion

        public MethodParameterMock<TReturn> HasMethod<TReturn>(Expression<Func<T, TReturn>> methodExpression)
        {
            var methodParameterMock = new MethodParameterMock<TReturn>(methodExpression);
            MethodMocks.Add(methodExpression.GetHashCode(), methodParameterMock);
            return methodParameterMock;
        }

        public MethodImplementationMock<TReturn> HasMethod<TReturn>(Expression<Func<T, TReturn>> methodExpression, Func<TReturn> implementation)
        {
            var methodImplementationMock = new MethodImplementationMock<TReturn>(methodExpression, implementation);
            MethodMocks.Add(methodExpression.GetHashCode(), methodImplementationMock);
            return methodImplementationMock;
        }
        public MethodImplementationMock<TArg1, TReturn> HasMethod<TArg1, TReturn>(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TReturn> implementation)
        {
            var methodImplementationMock = new MethodImplementationMock<TArg1, TReturn>(methodExpression, implementation);
            MethodMocks.Add(methodExpression.GetHashCode(), methodImplementationMock);
            return methodImplementationMock;
        }
        public MethodImplementationMock<TArg1, TArg2, TReturn> HasMethod<TArg1, TArg2, TReturn>(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TArg2, TReturn> implementation)
        {
            var methodImplementationMock = new MethodImplementationMock<TArg1, TArg2, TReturn>(methodExpression, implementation);
            MethodMocks.Add(methodExpression.GetHashCode(), methodImplementationMock);
            return methodImplementationMock;
        }
        public MethodImplementationMock<TArg1, TArg2, TArg3, TReturn> HasMethod<TArg1, TArg2, TArg3, TReturn>(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TArg2, TArg3, TReturn> implementation)
        {
            var methodImplementationMock = new MethodImplementationMock<TArg1, TArg2, TArg3, TReturn>(methodExpression, implementation);
            MethodMocks.Add(methodExpression.GetHashCode(), methodImplementationMock);
            return methodImplementationMock;
        }
        public MethodImplementationMock<TArg1, TArg2, TArg3, TArg4, TReturn> HasMethod<TArg1, TArg2, TArg3, TArg4, TReturn>(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TArg2, TArg3, TArg4, TReturn> implementation)
        {
            var methodImplementationMock = new MethodImplementationMock<TArg1, TArg2, TArg3, TArg4, TReturn>(methodExpression, implementation);
            MethodMocks.Add(methodExpression.GetHashCode(), methodImplementationMock);
            return methodImplementationMock;
        }
        public MethodImplementationMock<TArg1, TArg2, TArg3, TArg4, TArg5, TReturn> HasMethod<TArg1, TArg2, TArg3, TArg4, TArg5, TReturn>(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TReturn> implementation)
        {
            var methodImplementationMock = new MethodImplementationMock<TArg1, TArg2, TArg3, TArg4, TArg5, TReturn>(methodExpression, implementation);
            MethodMocks.Add(methodExpression.GetHashCode(), methodImplementationMock);
            return methodImplementationMock;
        }

        public class MethodParameterMock<TReturn> : MethodParameterMockBase, IInvisibleSystemObjectMethods
        {
            public MethodParameterMock(Expression<Func<T, TReturn>> methodExpression)
                : base (methodExpression)
            {
            }

            public MethodReturnsMock<TReturn> Returns(TReturn returnValue)
            {
                var methodReturnsMock = new MethodReturnsMock<TReturn>(returnValue);
                MethodCompletesMock = methodReturnsMock;
                return methodReturnsMock;
            }

            public MethodThrowsMock<TException> Throws<TException>()
                where TException : Exception
            {
                var methodThrowsMock = new MethodThrowsMock<TException>();
                MethodCompletesMock = methodThrowsMock;
                return methodThrowsMock;
            }
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
            public MethodImplementationMock(Expression<Func<T, TReturn>> methodExpression, Func<TReturn> implementation)
                : base(methodExpression, implementation)
            {
            }
        }
        public class MethodImplementationMock<TArg1, TReturn> : MethodImplementationMockBase, IInvisibleSystemObjectMethods
        {
            public MethodImplementationMock(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TReturn> implementation)
                : base(methodExpression, implementation)
            {
            }
        }
        public class MethodImplementationMock<TArg1, TArg2, TReturn> : MethodImplementationMockBase, IInvisibleSystemObjectMethods
        {
            public MethodImplementationMock(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TArg2, TReturn> implementation)
                : base(methodExpression, implementation)
            {
            }
        }
        public class MethodImplementationMock<TArg1, TArg2, TArg3, TReturn> : MethodImplementationMockBase, IInvisibleSystemObjectMethods
        {
            public MethodImplementationMock(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TArg2, TArg3, TReturn> implementation)
                : base(methodExpression, implementation)
            {
            }
        }
        public class MethodImplementationMock<TArg1, TArg2, TArg3, TArg4, TReturn> : MethodImplementationMockBase, IInvisibleSystemObjectMethods
        {
            public MethodImplementationMock(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TArg2, TArg3, TArg4, TReturn> implementation)
                : base(methodExpression, implementation)
            {
            }
        }
        public class MethodImplementationMock<TArg1, TArg2, TArg3, TArg4, TArg5, TReturn> : MethodImplementationMockBase, IInvisibleSystemObjectMethods
        {
            public MethodImplementationMock(Expression<Func<T, TReturn>> methodExpression, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TReturn> implementation)
                : base(methodExpression, implementation)
            {
            }
        }
        public class MethodReturnsMock<TReturn> : MethodReturnsMockBase, IInvisibleSystemObjectMethods
        {
            public MethodReturnsMock(TReturn returnValue)
            {
                ReturnValue = returnValue;
                ReturnType = typeof (TReturn);
            }

            public void Subscribe(Action callback)
            {
                Callback = callback;
            }
        }
        public class MethodThrowsMock<TException> : MethodThrowsMockBase, IInvisibleSystemObjectMethods
            where TException : Exception
        {
            public MethodThrowsMock()
            {
                ExceptionType = typeof (TException);
            }
            public MethodThrowsMock(Func<TException> exceptionInitializer)
            {
                ExceptionType = typeof(TException);
                ExceptionInitializer = () => exceptionInitializer();
            }
        }
    }
}
