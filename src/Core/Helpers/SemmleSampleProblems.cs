using System;

namespace Core.Helpers
{
    /// <summary>
    /// This class is designed to demonstrate Semmle detection
    /// </summary>
    public class SemmleSampleProblems
    {
        private static void UnusedMethod()
        {
            // This is an unused method
            int i = 2 + 2;
            Console.WriteLine(i);
        }
    }
}
