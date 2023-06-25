/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HelloWorld
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Window());

            // Computer Code
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            Computer c = new Computer();
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            Logger.End();
        }
    }

    public class Console
    {
        public static void WriteLine()
        {
            Window._Window.PrintToOutputWindow(Environment.NewLine);
        }

        public static void WriteLine(string output)
        {
            Window._Window.PrintToOutputWindow(output + Environment.NewLine);
        }

        public static void Write(string output)
        {
            Window._Window.PrintToOutputWindow(output);
        }

        public static void Clear() 
        {
            Window._Window.ClearOutputWindow();
        }

        public static void ReadKey()
        {

        }

        public static string ReadLine()
        {
            return "";
        }
    }
}
*/