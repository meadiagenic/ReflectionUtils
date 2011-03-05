namespace ReflectionUtils.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Text;
    using NUnit.Framework;

    [TestFixture]
    public class ReflectionExtensionsTextFixture
    {
        [Test]
        public void GetProperties_Object_Returns_Object_Properties()
        {
            var a = new TestObject();

            var b = a.GetProperties();

            Assert.IsNotNull(b);
            Assert.AreEqual(2, b.Count());

        }

        [Test]
        public void GetProperties_For_Two_Objects_Of_Same_Type_Return_Same_Properties()
        {
            var a = new TestObject();
            var b = new TestObject();

            Assert.AreSame(a.GetProperties().ToArray(), b.GetProperties().ToArray());
        }

        [Test]
        public void ToDynamic_Returns_Object_As_dynamic()
        {
            var a = new TestObject();
            dynamic b = a.ToDynamic();

            Assert.IsNotNull(b);
            Assert.IsInstanceOf<IDynamicMetaObjectProvider>(b);

            b.Test = "test";

            Assert.AreEqual("test", b.Test);
        }
    }

    public class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
