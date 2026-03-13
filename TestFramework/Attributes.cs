using System;

namespace TestFramework
{

    [AttributeUsage(AttributeTargets.Class)]
    public class TestClassAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestClassInitAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestClassCleanupAttribute : Attribute { }


    [AttributeUsage(AttributeTargets.Method)]
    public class TestMethodAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestMethodInitAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestMethodCleanupAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestAsyncAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestIgnoreAttribute : Attribute { }


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestDataAttribute : Attribute
    {
        public object[] Parametrs { get; }
        public TestDataAttribute(params object[] parametrs) => Parametrs = parametrs;
    }


    [AttributeUsage(AttributeTargets.Method)]
    public class TestPriorityAttribute : Attribute
    {
        public int Priority { get; }
        public TestPriorityAttribute(int priority) => Priority = priority;
    }


    [AttributeUsage(AttributeTargets.Method)]
    public class TestTimeoutAttribute : Attribute
    {
        public int MillisecondsTimeout { get; }
        public TestTimeoutAttribute(int milliseconds) => MillisecondsTimeout = milliseconds;
    }
}