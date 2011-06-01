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
            IMethodReturnsMock MethodReturnsMock { get; }
        }
        public class MethodParameterMock<TReturn> : IMethodParameterMock, IInvisibleSystemObjectMethods
        {
            public Expression MethodExpression { get; private set; }
            public IMethodReturnsMock MethodReturnsMock { get; private set; }

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
            {
                var methodThrowsMock = new MethodThrowsMock<TException>();
                MethodReturnsMock = methodThrowsMock;
                return methodThrowsMock;
            }
            public MethodThrowsMock<TException> Throws<TException>(Func<TException> exceptionInitializer)
            {
                var methodThrowsMock = new MethodThrowsMock<TException>(exceptionInitializer);
                MethodReturnsMock = methodThrowsMock;
                return methodThrowsMock;
            }
        }
        public interface IMethodReturnsMock { }
        public class MethodReturnsMock<TReturn> : IMethodReturnsMock, IInvisibleSystemObjectMethods
        {
            public TReturn ReturnValue { get; private set; }
            public Action Callback { get; private set; }

            public MethodReturnsMock(TReturn returnValue)
            {
                ReturnValue = returnValue;
            }

            public void Subscribe(Action callback)
            {
                Callback = callback;
            }
        }
        public class MethodThrowsMock<TException> : IMethodReturnsMock, IInvisibleSystemObjectMethods
        {
            public Func<TException> ExceptionInitializer { get; private set; }

            public MethodThrowsMock()
            {
            }
            public MethodThrowsMock(Func<TException> exceptionInitializer)
            {
                ExceptionInitializer = exceptionInitializer;
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
