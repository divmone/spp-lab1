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

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestCategoryAttribute : Attribute
    {
        public string Category { get; }
        public TestCategoryAttribute(string category) => Category = category;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestAuthorAttribute : Attribute
    {
        public string Author { get; }
        public TestAuthorAttribute(string author) => Author = author;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestDataSourceAttribute : Attribute
    {
        public string MethodName { get; }
        public TestDataSourceAttribute(string methodName) => MethodName = methodName;
    }
}