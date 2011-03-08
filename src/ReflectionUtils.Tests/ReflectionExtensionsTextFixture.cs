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

       

    }

    public class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; }

    }

    [Serializable]
    public class SerializableTestObject
    {
        public int Id { get; set; }
        public string Name { get; set; }

    }
}
