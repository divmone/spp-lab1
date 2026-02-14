using System;
using System.Collections.Generic;
using System.Text;


namespace TestFramework
{
    internal class AssertException: Exception
    {
        public AssertException(string message): base(message) { }
    }
}
