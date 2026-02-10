using System;
using System.Collections.Generic;
using System.Text;

namespace TestFramework
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TestClassAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TestClassInitAttribute: Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TestClassCleanupAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestMethodAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestMethodInitAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestMethodCleanupAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestDataAttribute : Attribute
    {
        public object[] Parametrs { get; set; }


        public TestDataAttribute(params object[] parametrs)
        {
            Parametrs = parametrs;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestAsyncAttribute: Attribute
    {

    }

    [AttributeUsage (AttributeTargets.Method)]
    public class TestIgnoreAttribute: Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestPriorityAttribute: Attribute
    {
        public int Priority { get; set; }

        public TestPriorityAttribute(int priority)
        {
            Priority = priority;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TestCollectionAttribute : Attribute
    {
        public string Name { get; }

        public TestCollectionAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CollectionDefinitionAttribute : Attribute
    {
        public string Name { get; }
        public Type FixtureType { get; }

        public CollectionDefinitionAttribute(string name, Type fixtureType)
        {
            Name = name;
            FixtureType = fixtureType;
        }
    }

    public interface ISharedFixture : IDisposable
    {
        void Initialize();
    }
}
