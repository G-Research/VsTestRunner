using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Bulldog;

namespace VsTestRunner
{
    public abstract class ExitCode : ExitCodeBase
    {
        public static int FailedTests => 1;
        public static int InvalidOptions => -1;
        public static int DirectoryNotFound => -2;
        public static int NoTestAssemblies => -3;
    }

    static class Program
    {
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        static async Task<int> Main(string[] args)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
                if (!GetConsoleMode(iStdOut, out uint outConsoleMode))
                {
                    Console.WriteLine("Failed to get output console mode.");
                }
                else
                {
                    outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                    if (!SetConsoleMode(iStdOut, outConsoleMode))
                    {
                        Console.WriteLine($"Failed to set output console mode, error code: {GetLastError()}");
                    }
                }
            }

            TestRunner vsTestRunner = new TestRunner();
            return await vsTestRunner.Run(args);
        }
    }

}
