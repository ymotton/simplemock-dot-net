using System;
using System.Threading;

namespace SimpleMock.Sample
{

    public class Class1
    {
        public string Bar(bool foo)
        {
            if (foo == true)
            {
                return "true";
            }
            else if (foo == false)
            {
                return "false";
            }

            throw new NotImplementedException();
        }

        public int Bar(string foo)
        {
            if (foo == "foo")
            {
                return 1;
            }
            else if (foo == "bar")
            {
                return 2;
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
