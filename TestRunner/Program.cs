using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using TestFramework;

class Program
{
    static void Main(string[] args)
    {
        string testAssemblyPath = Path.Combine(AppContext.BaseDirectory, "TestProject.dll");

        int maxDop = 4;
        if (args.Length > 1 && int.TryParse(args[1], out int parsedDop))
            maxDop = parsedDop;

        Assembly assembly = Assembly.LoadFrom(testAssemblyPath);

        RunTests(assembly, true, maxDop, out long parallelMs);
        RunTests(assembly, false, maxDop, out long sequentialMs);

        ResultsPrinter.PrintBenchmarkComparison(sequentialMs, parallelMs);
    }

    static void RunTests(Assembly assembly, bool isParallel, int maxDop, out long elapsedMs)
    {
        string line = new string('═', 60);

        if (isParallel)
        {
            Console.WriteLine(line);
            Console.WriteLine($"  PARALLEL RUN  (MaxDOP = {maxDop})");
            Console.WriteLine(line);
            Console.WriteLine();

            var runner = new ParallelTestRunner(maxDop);
            var watch = Stopwatch.StartNew();
            var results = runner.RunAll(assembly);
            watch.Stop();

            elapsedMs = watch.ElapsedMilliseconds;
            ResultsPrinter.PrintSummary(results, elapsedMs);
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine(line);
            Console.WriteLine("  SEQUENTIAL RUN");
            Console.WriteLine(line);
            Console.WriteLine();

            var runner = new TestRunner();
            var watch = Stopwatch.StartNew();
            var results = runner.RunAll(assembly);
            watch.Stop();

            elapsedMs = watch.ElapsedMilliseconds;
            ResultsPrinter.PrintSummary(results, elapsedMs);
        }
    }
}