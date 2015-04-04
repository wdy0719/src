using System;
using System.Collections.Generic;

namespace TestDoc
{
    using System.Xml.Serialization;

    [Serializable]
    [XmlRoot]
    public class TestDoc
    {
        [XmlElement]
        public List<TestClass> TestClass { get; set; }
    }

    [Serializable]
    public class TestClass
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Summary { get; set; }

        [XmlElement]
        public List<TestMethod> TestMethod { get; set; }
    }

    [Serializable]
    public class TestMethod
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlElement]
        public string Summary { get; set; }
    }
}
