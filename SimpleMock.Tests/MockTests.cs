using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SimpleMock.Tests
{
    [TestClass]
    public class MockTests
    {
        public struct TestType
        {
            public int Value;
        }

        public enum TestEnum
        {
            One = 1,
            Zero = 0
        }

        public interface ITest
        {
            int ThrowsException();
            DateTime ReturnsDateTime();
            int EchoInt(int parameter);
            int? EchoNullableInt(int? parameter);
            TestEnum EchoEnum(TestEnum parameter);
            TestEnum? EchoNullableEnum(TestEnum? parameter);
            TestType EchoStruct(TestType parameter);
            TestType? EchoNullableStruct(TestType? parameter);
            string EchoString(string parameter);
            int Add(int a, int b);
            int Add(int a, int b, int c);
        }

        public interface IFoo<in T>
        {
            string ToString(T value);
            string Foo<U>(U value);
        }

        private TestEnum? _testEnumField;
        private TestEnum? TestEnumProperty { get; set; }
        private static TestEnum? _testEnumStaticField;
        private static TestEnum? TestEnumStaticProperty { get; set; }

        [TestMethod]
        public void EchoInt_WhenArgumentOne_ReturnsOne()
        {
            var mock = new Mock<ITest>();

            const int echoedValue = 1;

            mock.HasMethod(t => t.EchoInt(echoedValue))
                .Returns(echoedValue);

            Assert.AreEqual(echoedValue, mock.Instance.EchoInt(echoedValue));
        }

        [TestMethod]
        public void EchoNullableInt_WhenArgumentOne_ReturnsOne()
        {
            var mock = new Mock<ITest>();

            const int echoedValue = 1;

            mock.HasMethod(t => t.EchoNullableInt(echoedValue))
                .Returns(echoedValue);

            Assert.AreEqual(echoedValue, mock.Instance.EchoNullableInt(echoedValue));
        }

        [TestMethod]
        public void EchoNullableInt_WhenArgumentNull_ReturnZero()
        {
            var mock = new Mock<ITest>();

            const int echoedValue = 0;

            mock.HasMethod(t => t.EchoNullableInt(null))
                .Returns(echoedValue);

            Assert.AreEqual(echoedValue, mock.Instance.EchoNullableInt(null));
        }

        [TestMethod]
        public void EchoEnum_WhenArgumentEnumOne_ReturnsEnumOne()
        {
            var mock = new Mock<ITest>();

            const TestEnum echoedValue = TestEnum.One;

            mock.HasMethod(t => t.EchoEnum(echoedValue))
                .Returns(echoedValue);

            Assert.AreEqual(echoedValue, mock.Instance.EchoEnum(echoedValue));
        }

        [TestMethod]
        public void EchoNullableEnum_WhenArgumentLocalFieldOne_ReturnsEnumOne()
        {
            var mock = new Mock<ITest>();

            TestEnum? echoedValue = TestEnum.One;

            mock.HasMethod(t => t.EchoNullableEnum(echoedValue))
                .Returns(echoedValue);

            Assert.AreEqual(echoedValue, mock.Instance.EchoNullableEnum(echoedValue));
        }

        [TestMethod]
        public void EchoNullableEnum_WhenArgumentPrivateInstanceFieldOne_ReturnsEnumOne()
        {
            var mock = new Mock<ITest>();

            _testEnumField = TestEnum.One;

            mock.HasMethod(t => t.EchoNullableEnum(_testEnumField))
                .Returns(_testEnumField);

            Assert.AreEqual(_testEnumField, mock.Instance.EchoNullableEnum(_testEnumField));
        }

        [TestMethod]
        public void EchoNullableEnum_WhenArgumentPublicInstancePropertyOne_ReturnsEnumOne()
        {
            var mock = new Mock<ITest>();

            TestEnumProperty = TestEnum.One;

            mock.HasMethod(t => t.EchoNullableEnum(TestEnumProperty))
                .Returns(TestEnumProperty);

            Assert.AreEqual(TestEnumProperty, mock.Instance.EchoNullableEnum(TestEnumProperty));
        }

        [TestMethod]
        public void EchoNullableEnum_WhenArgumentPrivateStaticFieldOne_ReturnsEnumOne()
        {
            var mock = new Mock<ITest>();

            _testEnumStaticField = TestEnum.One;

            mock.HasMethod(t => t.EchoNullableEnum(_testEnumStaticField))
                .Returns(_testEnumStaticField);

            Assert.AreEqual(_testEnumStaticField, mock.Instance.EchoNullableEnum(_testEnumStaticField));
        }

        [TestMethod]
        public void EchoNullableEnum_WhenArgumentPublicStaticPropertyOne_ReturnsEnumOne()
        {
            var mock = new Mock<ITest>();

            TestEnumStaticProperty = TestEnum.One;

            mock.HasMethod(t => t.EchoNullableEnum(TestEnumStaticProperty))
                .Returns(TestEnumStaticProperty);

            Assert.AreEqual(TestEnumStaticProperty, mock.Instance.EchoNullableEnum(TestEnumStaticProperty));
        }

        [TestMethod]
        public void EchoNullableEnum_WhenArgumentNull_ReturnsEnumZero()
        {
            var mock = new Mock<ITest>();

            TestEnum? echoedValue = TestEnum.Zero;

            mock.HasMethod(t => t.EchoNullableEnum(null))
                .Returns(echoedValue);

            Assert.AreEqual(echoedValue, mock.Instance.EchoNullableEnum(null));
        }

        [TestMethod]
        public void EchoStruct_WhenArgumentOne_ReturnsStructOne()
        {
            var mock = new Mock<ITest>();

            TestType echoedValue = new TestType() { Value = 1 };

            mock.HasMethod(t => t.EchoStruct(echoedValue))
                .Returns(echoedValue);

            Assert.AreEqual(echoedValue, mock.Instance.EchoStruct(echoedValue));
        }

        [TestMethod]
        public void EchoNullableStruct_WhenArgumentOne_ReturnsStructOne()
        {
            var mock = new Mock<ITest>();

            TestType? echoedValue = new TestType() { Value = 1 };

            mock.HasMethod(t => t.EchoNullableStruct(echoedValue))
                .Returns(echoedValue);

            Assert.AreEqual(echoedValue, mock.Instance.EchoNullableStruct(echoedValue));
        }

        [TestMethod]
        public void EchoNullableStruct_WhenArgumentNull_ReturnsStructDefault()
        {
            var mock = new Mock<ITest>();

            TestType? echoedValue = new TestType();

            mock.HasMethod(t => t.EchoNullableStruct(null))
                .Returns(echoedValue);

            Assert.AreEqual(echoedValue, mock.Instance.EchoNullableStruct(null));
        }

        [TestMethod]
        public void EchoString_WhenArgumentEmpty_ReturnsEmpty()
        {
            var mock = new Mock<ITest>();

            string echoedValue = "";

            mock.HasMethod(t => t.EchoString(echoedValue))
                .Returns(echoedValue);

            Assert.AreEqual(echoedValue, mock.Instance.EchoString(echoedValue));
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void EchoString_WhenArgumentNull_ThrowsArgumentNullException()
        {
            var mock = new Mock<ITest>();

            mock.HasMethod(t => t.EchoString(null))
                .Throws<ArgumentNullException>();

            mock.Instance.EchoString(null);
        }

        [TestMethod]
        public void EchoString_WhenArgumentNull_ThrowsExceptionReturnedFromDelegate()
        {
            var mock = new Mock<ITest>();

            mock.HasMethod(t => t.EchoString(null))
                .Throws(() => new ArgumentNullException("parameter"));

            try
            {
                mock.Instance.EchoString(null);
            }
            catch (ArgumentNullException e)
            {
                Assert.AreEqual("parameter", e.ParamName);
            }
        }
        
        [TestMethod]
        public void EchoInt_CapturedVariableIncrementedInCallBack_CapturedVariableIncremented()
        {
            var mock = new Mock<ITest>();

            int valueToBeIncremented = -1;
            mock.HasMethod(t => t.EchoInt(1))
                .Returns(1)
                .Subscribe(() => valueToBeIncremented++);

            mock.Instance.EchoInt(1); Assert.AreEqual(0, valueToBeIncremented);
            mock.Instance.EchoInt(1); Assert.AreEqual(1, valueToBeIncremented);
            mock.Instance.EchoInt(1); Assert.AreEqual(2, valueToBeIncremented);
        }

        [TestMethod]
        public void EchoInt_ArgumentIncrementedInImplementation_ReturnsIncrementedValue()
        {
            var mock = new Mock<ITest>();

            int value = -1;
            mock.HasMethod<int, int>(
                t => t.EchoInt(value),
                arg1 => ++value);

            Assert.AreEqual(0, mock.Instance.EchoInt(0));
            Assert.AreEqual(1, mock.Instance.EchoInt(0));
            Assert.AreEqual(2, mock.Instance.EchoInt(0));
        }

        [TestMethod]
        public void Add_AddOneAndTwo_ReturnsThree()
        {
            var mock = new Mock<ITest>();
            mock.HasMethod(f => f.Add(1, 2))
                .Returns(3);

            Assert.AreEqual(3, mock.Instance.Add(1, 2));
        }

        [TestMethod]
        public void Add_AddOneAndTwoAndThree_ReturnsSix()
        {
            var mock = new Mock<ITest>();
            mock.HasMethod(f => f.Add(1, 2, 3))
                .Returns(6);

            Assert.AreEqual(6, mock.Instance.Add(1, 2, 3));
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void HasMethod_ThatTakesNoParameters_AndThrowsException()
        {
            var mock = new Mock<ITest>();
            mock.HasMethod(f => f.ThrowsException())
                .Throws(() => new InvalidOperationException());

            mock.Instance.ThrowsException();
        }

        [TestMethod]
        public void HasMethod_ThatTakesNoParameters_AndReturnsDateTime()
        {
            var mock = new Mock<ITest>();

            var expectedDate = DateTime.Parse("01/01/1980 00:00:01");
            mock.HasMethod(f => f.ReturnsDateTime())
                .Returns(expectedDate);

            Assert.AreEqual(expectedDate, mock.Instance.ReturnsDateTime());
        }

        [TestMethod]
        public void GenericInterface_TakesGenericType_ReturnsString()
        {
            var mock = new Mock<IFoo<int>>();

            mock.HasMethod(
                foo => foo.ToString(1),
                (int i) => i.ToString());

            Assert.AreEqual("1", mock.Instance.ToString(1));
        }
        
        [TestMethod]
        public void GenericMethod_TakesGenericTypeInImplementation_ReturnsString()
        {
            var mock = new Mock<IFoo<int>>();

            mock.HasMethod(
                foo => foo.Foo(1),
                (int i) => i.ToString());
            mock.HasMethod(
                foo => foo.Foo("test"),
                (string i) => i);

            Assert.AreEqual("1", mock.Instance.Foo(1));
            Assert.AreEqual("test", mock.Instance.Foo("test"));
        }

        [TestMethod]
        public void GenericMethod_TakesGenericType_ReturnsString()
        {
            var mock = new Mock<IFoo<int>>();

            mock.HasMethod(foo => foo.Foo(1))
                .Returns("1");
            mock.HasMethod(foo => foo.Foo("1"))
                .Returns("1");

            Assert.AreEqual("1", mock.Instance.Foo(1));
            Assert.AreEqual("1", mock.Instance.Foo("1"));
        }

        [TestMethod, ExpectedException(typeof(ArgumentException))]
        public void GenericMethod_TakesGenericType_ThrowsException()
        {
            var mock = new Mock<IFoo<int>>();

            mock.HasMethod(foo => foo.Foo(1))
                .Throws<ArgumentException>();

            Assert.AreEqual("1", mock.Instance.Foo(1));
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void GenericMethod_TakesGenericType_ThrowsExceptionReturnedFromDelegate()
        {
            var mock = new Mock<IFoo<int>>();

            string stringValue = null;
            mock.HasMethod(foo => foo.Foo(stringValue))
                .Throws(() => new ArgumentNullException());

            Assert.AreEqual("1", mock.Instance.Foo(stringValue));
        }
    }
}
