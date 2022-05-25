using System.Runtime.InteropServices;

namespace VirtualHPS
{
    internal static class Program
    {
        // Based on https://stackoverflow.com/questions/18396650/write-to-command-line-if-application-is-executed-from-cmd
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            AttachConsole(ATTACH_PARENT_PROCESS);
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}