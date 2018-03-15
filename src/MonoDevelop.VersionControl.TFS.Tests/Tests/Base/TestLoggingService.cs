using System;
using MonoDevelop.VersionControl.TFS.Services;

namespace MonoDevelop.VersionControl.TFS.Tests
{
    internal sealed class TestLoggingService : ILoggingService
    {
        public bool IsDebugMode { get { return true; } }

        public void LogToDebug(string message)
        {
            Console.WriteLine(message);
        }

        public void LogToInfo(string message)
        {
            Console.WriteLine(message);
        }

        public void LogToError(string message)
        {
            Console.WriteLine(message);
        }
    }
}