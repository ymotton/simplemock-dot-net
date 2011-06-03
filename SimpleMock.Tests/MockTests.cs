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
            int EchoInt(int parameter);
            int? EchoNullableInt(int? parameter);
            TestEnum EchoEnum(TestEnum parameter);
            TestEnum? EchoNullableEnum(TestEnum? parameter);
            TestType EchoStruct(TestType parameter);
            TestType? EchoNullableStruct(TestType? parameter);
            string EchoString(string parameter);
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

            _testEnumField  = TestEnum.One;

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

    }
}
