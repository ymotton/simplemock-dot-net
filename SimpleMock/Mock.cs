using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SimpleMock
{
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
                    _instance = new MockType<T>(MethodParameterMocks.Select(p => p.Value)).BuildUp();
                }

                return _instance;
            }
        }

        private IDictionary<int, IMethodParameterMock> _methodParameterMocks;
        protected IDictionary<int, IMethodParameterMock> MethodParameterMocks
        {
            get
            {
                if (_methodParameterMocks == null)
                {
                    _methodParameterMocks = new Dictionary<int, IMethodParameterMock>();
                }
                
                return _methodParameterMocks;
            }
        }

        #endregion

        public interface IMethodParameterMock
        {
            Expression MethodExpression { get; }
            IMethodCompletesMock MethodReturnsMock { get; }
        }
        public class MethodParameterMock<TReturn> : IMethodParameterMock, IInvisibleSystemObjectMethods
        {
            public Expression MethodExpression { get; private set; }
            public IMethodCompletesMock MethodReturnsMock { get; private set; }

            public MethodParameterMock(Expression<Action<T>> methodExpression)
            {
                MethodExpression = methodExpression;
            }
            public MethodParameterMock(Expression<Func<T, TReturn>> methodExpression)
            {
                MethodExpression = methodExpression;
            }

            public MethodReturnsMock<TReturn> Returns(TReturn returnValue)
            {
                var methodReturnsMock = new MethodReturnsMock<TReturn>(returnValue);
                MethodReturnsMock = methodReturnsMock;
                return methodReturnsMock;
            }

            public MethodThrowsMock<TException> Throws<TException>()
                where TException : Exception
            {
                var methodThrowsMock = new MethodThrowsMock<TException>();
                MethodReturnsMock = methodThrowsMock;
                return methodThrowsMock;
            }
            public MethodThrowsMock<TException> Throws<TException>(Func<TException> exceptionInitializer)
                where TException : Exception
            {
                var methodThrowsMock = new MethodThrowsMock<TException>(exceptionInitializer);
                MethodReturnsMock = methodThrowsMock;
                return methodThrowsMock;
            }
        }
        public interface IMethodCompletesMock
        {
            Action Callback { get; }
        }
        public interface IMethodReturnsMock : IMethodCompletesMock
        {
            object ReturnValue { get; }
            Type ReturnType { get; }
        }
        public class MethodReturnsMock<TReturn> : IMethodReturnsMock, IInvisibleSystemObjectMethods
        {
            public object ReturnValue { get; private set; }
            public Type ReturnType { get; private set; }
            public Action Callback { get; private set; }

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
        public interface IMethodThrowsMock : IMethodCompletesMock
        {
            Type ExceptionType { get; }
            Func<Exception> ExceptionInitializer { get; }
        }
        public class MethodThrowsMock<TException> : IMethodThrowsMock, IInvisibleSystemObjectMethods
            where TException : Exception
        {
            public Type ExceptionType { get; private set; }
            public Func<Exception> ExceptionInitializer { get; private set; }
            public Action Callback { get; private set; }

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

        public MethodParameterMock<TReturn> HasMethod<TReturn>(Expression<Func<T, TReturn>> methodExpression)
        {
            var methodParameterMock = new MethodParameterMock<TReturn>(methodExpression);
            MethodParameterMocks.Add(methodExpression.GetHashCode(), methodParameterMock);
            return methodParameterMock;
        }
    }
}
