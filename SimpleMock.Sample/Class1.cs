using System;
using System.Collections.Generic;
using System.Threading;

namespace SimpleMock.Sample
{

    public class Class1
    {
        public Class1 _foo;

        public Class1 Bar(Class1 foo)
        {
            if (EqualityComparer<Class1>.Default.Equals(foo, new Class1()))
            {
                return new Class1();
            }
            else if (EqualityComparer<Class1>.Default.Equals(foo, new Class1()))
            {
                return _foo;
            }

            throw new NotImplementedException();
        }

        public string Bar(bool? foo)
        {
            if (EqualityComparer<bool?>.Default.Equals(foo, true))
            {
                return "true";
            }
            else if (EqualityComparer<bool?>.Default.Equals(foo, false))
            {
                return "false";
            }
            else if (EqualityComparer<bool?>.Default.Equals(foo, null))
            {
                throw new Exception();
            }
            
            throw new NotImplementedException();
        }

        public FooType Bar(double b)
        {
            throw new NotImplementedException();
        }

        public FooStruct Bar(float a, int b, double c, decimal e, string f, object g)
        {
            if (a == 0.0f && b == 0 && c == 0.0d && e == 0.0m && f == "123" && g == null)
            {
                return new FooStruct();
            }

            throw new NotImplementedException();
        }
        
        public void Foo ()
        {
            throw new NotImplementedException();
        }
    }
}
