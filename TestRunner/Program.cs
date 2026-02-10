using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TestFramework;

class TestResult
{
    public string TestName { get; set; }
    public string Status { get; set; }
    public string Error { get; set; }
}

class TestRunner
{
    static void Main(string[] args)
    {
        string testAssemblyPath = @"C:\Users\divmone\source\repos\Lab11\TestProject\bin\Debug\net10.0\TestProject.dll";

        if (!File.Exists(testAssemblyPath))
        {
            Console.WriteLine($"File not found: {testAssemblyPath}");
            return;
        }

        Assembly testAssembly = Assembly.LoadFrom(testAssemblyPath);

        var results = new List<TestResult>();
        var types = testAssembly.GetTypes();

        foreach (Type type in types)
        {
            if (type.GetCustomAttribute<TestClassAttribute>() != null)
            {
                object testInstance = Activator.CreateInstance(type);

                var methods = type.GetMethods()
                    .Where(
                            m => m.GetCustomAttribute<TestMethodAttribute>() != null ||
                                m.GetCustomAttribute<TestAsyncAttribute>() != null)
                    .Select(m => new
                    {
                        Method = m,
                        Priority = m.GetCustomAttribute<TestPriorityAttribute>()?.Priority
                    }
                    )
                    .OrderByDescending(m => m.Priority)
                    .Select(x => x.Method)
                    .ToList();
                
                var classInitMethod = GetMethodWithAttribute<TestClassInitAttribute>(type);
                var classCleanupMethod = GetMethodWithAttribute<TestClassCleanupAttribute>(type);
                
                classInitMethod?.Invoke(testInstance, null);

                foreach (MethodInfo method in methods)
                {
                    var ignoreAttr = method.GetCustomAttribute<TestIgnoreAttribute>();

                    if (ignoreAttr != null)
                    {
                        results.Add(new TestResult
                        {
                            TestName = $"{type.Name}.{method.Name}",
                            Status = "IGNORED"
                        });
                        continue;
                    }

                    var methodInitMethod = GetMethodWithAttribute<TestMethodInitAttribute>(type);
                    var methodCleanupMethod = GetMethodWithAttribute<TestMethodCleanupAttribute>(type);
                       
                    bool isAsync = method.GetCustomAttribute<TestAsyncAttribute>() != null;

                    if (method.GetCustomAttribute<TestMethodAttribute>() != null)
                    {
                        var dataAttributes = method.GetCustomAttributes<TestDataAttribute>().ToArray();

                        if (dataAttributes.Length > 0)
                        {
                            int testNumber = 0;
                            foreach (var parametr in dataAttributes)
                            {
                                testNumber++;
                                string testName = $"{type.Name}.{method.Name} (Case {testNumber})";
                                RunTestWithParametrs(results, testInstance, method, parametr.Parametrs, testName, methodInitMethod, methodCleanupMethod, isAsync);
                            }
                        }
                        else
                        {
                            string testName = $"{type.Name}.{method.Name}";
                            RunTestWithParametrs(results, testInstance, method, null, testName, methodInitMethod, methodCleanupMethod, isAsync);
                        }
                    }

                }

                classCleanupMethod?.Invoke(testInstance, null);

            }
        }

        PrintResults(results);
    }

    static async Task RunTestWithParametrs(List<TestResult> results, object testInstance, 
                                            MethodInfo method, object[] parametrs,
                                            string testName, MethodInfo initMethod,
                                            MethodInfo cleanupMethod, bool isAsync)
    {
        try
        {
            initMethod?.Invoke(testInstance, null);
            var returnValue = method.Invoke(testInstance, parametrs);
            if (returnValue is Task task) { 
                task.GetAwaiter().GetResult();
            }
            results.Add(new TestResult { TestName = testName, Status = "PASSED" });
        }
        catch (Exception ex)
        {
            results.Add(new TestResult { TestName = testName, Status = "FAILED", Error = ex.InnerException?.Message ?? ex.Message });
        }
        finally
        {
            cleanupMethod?.Invoke(testInstance, null);
        }
    }

    static void PrintResults(List<TestResult> results)
    {
        Console.WriteLine("\n--- Test Results ---");
        foreach (var result in results)
        {
            Console.WriteLine($"{result.TestName}: {result.Status}");
            if (result.Error != null)
                Console.WriteLine($"\tError: {result.Error}");
        }

        int passed = results.Count(r => r.Status == "PASSED");
        Console.WriteLine($"\nTotal: {results.Count}, Passed: {passed}, Failed: {results.Count - passed}");
    }

    static MethodInfo GetMethodWithAttribute<T>(Type type)where T: Attribute
    {
        return type.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<T>() != null);
    }

}
