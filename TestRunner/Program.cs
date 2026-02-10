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
    private static Dictionary<string, object> _sharedFixtures = new Dictionary<string, object>();

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

        var collectionDefinitions = FindCollectionDefinitions(testAssembly);

        var testClasses = testAssembly.GetTypes()
            .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
            .ToList();

        var testsByCollection = GroupTestsByCollection(testClasses);

        foreach (var collection in testsByCollection)
        {
            RunCollection(collection.Key, collection.Value, collectionDefinitions, results);
        }

        CleanupSharedFixtures();

        PrintResults(results);
    }

    static Dictionary<string, Type> FindCollectionDefinitions(Assembly assembly)
    {
        var definitions = new Dictionary<string, Type>();

        foreach (var type in assembly.GetTypes())
        {
            var attr = type.GetCustomAttribute<CollectionDefinitionAttribute>();
            if (attr != null)
            {
                definitions[attr.Name] = attr.FixtureType;
            }
        }

        return definitions;
    }

    static Dictionary<string, List<Type>> GroupTestsByCollection(List<Type> testClasses)
    {
        var groups = new Dictionary<string, List<Type>>();

        foreach (var testClass in testClasses)
        {
            var collectionAttr = testClass.GetCustomAttribute<TestCollectionAttribute>();
            string collectionName = collectionAttr?.Name ?? $"__Default_{testClass.Name}";

            if (!groups.ContainsKey(collectionName))
            {
                groups[collectionName] = new List<Type>();
            }

            groups[collectionName].Add(testClass);
        }

        return groups;
    }

    static void RunCollection(string collectionName, List<Type> testClasses,
                              Dictionary<string, Type> collectionDefinitions,
                              List<TestResult> results)
    {
        object sharedFixture = null;

        if (collectionDefinitions.ContainsKey(collectionName))
        {
            Type fixtureType = collectionDefinitions[collectionName];
            sharedFixture = Activator.CreateInstance(fixtureType);

            if (sharedFixture is ISharedFixture sf)
            {
                sf.Initialize();
            }

            _sharedFixtures[collectionName] = sharedFixture;
            Console.WriteLine($"Shared fixture created for collection: {collectionName}");
        }

        foreach (var testClass in testClasses)
        {
            RunTestClass(testClass, results, sharedFixture);
        }
    }

    static void RunTestClass(Type type, List<TestResult> results, object sharedFixture)
    {
        object testInstance;

        if (sharedFixture != null)
        {
            try
            {
                testInstance = Activator.CreateInstance(type, sharedFixture);
            }
            catch
            {
                testInstance = Activator.CreateInstance(type);
            }
        }
        else
        {
            testInstance = Activator.CreateInstance(type);
        }

        var methods = type.GetMethods()
            .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null ||
                       m.GetCustomAttribute<TestAsyncAttribute>() != null)
            .Select(m => new
            {
                Method = m,
                Priority = m.GetCustomAttribute<TestPriorityAttribute>()?.Priority
            })
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
                        RunTestWithParametrs(results, testInstance, method, parametr.Parametrs,
                                           testName, methodInitMethod, methodCleanupMethod, isAsync);
                    }
                }
                else
                {
                    string testName = $"{type.Name}.{method.Name}";
                    RunTestWithParametrs(results, testInstance, method, null, testName,
                                       methodInitMethod, methodCleanupMethod, isAsync);
                }
            }
        }

        classCleanupMethod?.Invoke(testInstance, null);
    }

    static void CleanupSharedFixtures()
    {
        foreach (var fixture in _sharedFixtures.Values)
        {
            if (fixture is ISharedFixture sf)
            {
                sf.Dispose();
            }
            else if (fixture is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        _sharedFixtures.Clear();
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
            if (returnValue is Task task)
            {
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
        int ignored = results.Count(r => r.Status == "IGNORED");
        Console.WriteLine($"\nTotal: {results.Count}, Passed: {passed}, Failed: {results.Count - passed - ignored}, Ignored: {ignored}");
    }

    static MethodInfo GetMethodWithAttribute<T>(Type type) where T : Attribute
    {
        return type.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<T>() != null);
    }
}
