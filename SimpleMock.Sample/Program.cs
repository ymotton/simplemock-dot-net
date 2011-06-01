using System;

namespace SimpleMock.Sample
{
    public enum FooType
    {
        One, Two, Three
    }
    public struct FooStruct
    {
        public int A;
    }
    public interface IFoo
    {
        string Foo { get; set; }
        string FooBar (string a, string b = "", string c = "");

        string ToString(bool? i);
        int ToInt(string i);
    }

    class Program
    {
        static void Main(string[] args)
        {
            var mock = new Mock<IFoo>();
            //mock.HasMethod(f => f.ToString(true))
            //    .Returns("true");
            //mock.HasMethod(f => f.ToString(false))
            //    .Returns("false");
            //mock.HasMethod(f => f.ToString(null))
            //    .Throws(() => new InvalidOperationException("NULL AINT VALID YOU MOFO"));
            mock.HasMethod(f => f.ToInt("1"))
                .Returns(1);
            mock.HasMethod(f => f.FooBar("a", "b", "c"))
                .Returns("ac");

            Console.WriteLine(mock.Instance);
            mock.Instance.ToString(true);

            Console.ReadKey();
        }
    }
}
