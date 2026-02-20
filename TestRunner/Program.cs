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
            Console.WriteLine($"Error: file not found: {testAssemblyPath}");
            return;
        }

        Assembly testAssembly = Assembly.LoadFrom(testAssemblyPath);
        var results = new List<TestResult>();

        foreach (Type type in testAssembly.GetTypes())
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

                var classInitMethod = type.GetMethods()
                    .FirstOrDefault(m => m.GetCustomAttribute<TestClassInitAttribute>() != null);
                var classCleanupMethod = type.GetMethods()
                    .FirstOrDefault(m => m.GetCustomAttribute<TestClassCleanupAttribute>() != null);


                try
                {
                    classInitMethod?.Invoke(testInstance, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Critical Error in ClassInit for {type.Name}: {ex}");
                    continue;
                }


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

                    var methodInitMethod = type.GetMethods()
                        .FirstOrDefault(m => m.GetCustomAttribute<TestMethodInitAttribute>() != null);
                    var methodCleanupMethod = type.GetMethods()
                        .FirstOrDefault(m => m.GetCustomAttribute<TestMethodCleanupAttribute>() != null);

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

                try
                {
                    classCleanupMethod?.Invoke(testInstance, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in ClassCleanup for {type.Name}: {ex}");
                }
            }
        }

        PrintResults(results);
    }

    static async Task RunTestWithParametrs(
        List<TestResult> results,
        object testInstance,
        MethodInfo method,
        object[] parametrs,
        string testName,
        MethodInfo initMethod,
        MethodInfo cleanupMethod,
        bool isAsync)
    {
        try
        {
            initMethod?.Invoke(testInstance, null);

            if (isAsync)
            {
                var task = (Task)method.Invoke(testInstance, parametrs);
                task.Wait();

                if (task.IsFaulted && task.Exception != null)
                {
                    throw task.Exception;
                }
            }
            else
            {
                method.Invoke(testInstance, parametrs);
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
        int ignored = results.Count(r => r.Status == "IGNORED");
        int failed = results.Count(r => r.Status == "FAILED");
        Console.WriteLine($"\nTotal: {results.Count}, Passed: {passed}, Failed: {failed}, Ignored: {ignored}");
    }
}