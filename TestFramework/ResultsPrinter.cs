using System;
using System.Collections.Generic;
using System.Linq;

namespace TestFramework
{
    public static class ResultsPrinter
    {
        public static void PrintSummary(List<TestResult> results, long totalElapsedMs)
        {
            int passed = results.Count(r => r.Status == TestStatus.Passed);
            int failed = results.Count(r => r.Status == TestStatus.Failed);
            int ignored = results.Count(r => r.Status == TestStatus.Ignored);
            int timeout = results.Count(r => r.Status == TestStatus.Timeout);

            Console.WriteLine();
            Console.WriteLine(new string('-', 60));
            Console.WriteLine("  TEST RESULTS");
            Console.WriteLine(new string('-', 60));
            Console.WriteLine($"  Total:   {results.Count}");
            Console.WriteLine($"  Passed:  {passed}");
            Console.WriteLine($"  Failed:  {failed}");
            Console.WriteLine($"  Timeout: {timeout}");
            Console.WriteLine($"  Ignored: {ignored}");
            Console.WriteLine(new string('-', 60));
            Console.WriteLine($"  Total time: {totalElapsedMs} ms");
            Console.WriteLine(new string('-', 60));
        }

        public static void PrintBenchmarkComparison(long sequentialMs, long parallelMs)
        {
            double speedup = parallelMs > 0 ? (double)sequentialMs / parallelMs : 0;

            Console.WriteLine();
            Console.WriteLine(new string('-', 60));
            Console.WriteLine("  SEQUENTIAL vs PARALLEL");
            Console.WriteLine(new string('-', 60));
            Console.WriteLine($"  Sequential: {sequentialMs} ms");
            Console.WriteLine($"  Parallel:   {parallelMs} ms");
            Console.WriteLine($"  Speedup:    x{speedup:F2}");
            Console.WriteLine(new string('-', 60));
        }
    }
}