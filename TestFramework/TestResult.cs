using System;

namespace TestFramework
{
    public enum TestStatus
    {
        Passed,
        Failed,
        Ignored,
        Timeout
    }

    public class TestResult
    {
        public string TestName { get; set; }
        public TestStatus Status { get; set; }
        public string ErrorMessage { get; set; }

        public long ElapsedMs { get; set; }

        public override string ToString()
        {
            string statusLabel = Status switch
            {
                TestStatus.Passed => "PASSED",
                TestStatus.Failed => "FAILED",
                TestStatus.Ignored => "IGNORED",
                TestStatus.Timeout => "TIMEOUT",
                _ => "UNKNOWN"
            };

            string line = $"{TestName}: {statusLabel} ({ElapsedMs} ms)";
            if (ErrorMessage != null)
                line += $"\n    Error: {ErrorMessage}";
            return line;
        }
    }
}