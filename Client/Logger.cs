using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BlazorMachine
{
    class Logger
    {
        private readonly static List<string> logList = new List<string>();
        private readonly static string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private readonly static string fileName = "log.txt";

        public static void Initialize()
        {
            string startDate = DateTime.Now.ToString("ddd hh:mm tt dd/MM/yyyy"); // e.g. Wed 05:30 PM 01/01/2020
            Log("Logger", "Starting at date " + startDate + " in directory " + currentDirectory, "start");

            string path = currentDirectory + fileName;
            try
            {
                double logFileSizeInKiloBytes = new FileInfo(path).Length / 1000;
                if (logFileSizeInKiloBytes > 1000)
                {
                    PrintAndLog("Logger", "WARNING: Log file has exceeded 1MB (Currently at " + logFileSizeInKiloBytes / 1000 + "MB)");
                    PrintAndLog("Logger", "It is recommended to delete the log file, a new one will be generated.\n");
                }
            }
            catch
            {
                PrintAndLog("Logger", "ERROR: Could not find log file");
            }
        }

        public static void End()
        {
            string endDate = DateTime.Now.ToString("ddd hh:mm tt dd/MM/yyyy"); // e.g. Wed 05:30 PM 01/01/2020
            Log("Logger", "Ending at date " + endDate + " in directory " + currentDirectory, "end"); ;
        }

        public static void Log(string source, string message, string mode = "Standard")
        {
            string logDate = DateTime.Now.ToString("hh:mm:ss tt"); // e.g. 05:30:45 PM
            string logMessage = "";

            if (mode == "start")
            {
                logMessage += ">>>> ";
                logMessage += "[" + source.ToUpper() + " " + logDate + "]: " + message;
            }
            else if (mode == "end")
            {
                logMessage += "<<<< ";
                logMessage += "[" + source.ToUpper() + " " + logDate + "]: " + message;
                logMessage += Environment.NewLine;
            }
            else
            {
                logMessage += "  [" + source.ToUpper() + " " + logDate + "]: " + message;
            }

            logList.Add(logMessage);

            string path = currentDirectory + fileName;

            try
            {
                File.AppendAllText(path, logMessage + Environment.NewLine);
            }
            catch
            {
                Console.WriteLine("LOG_ERROR: Could not add following message to log.txt: " + logMessage);
            }
        }

        public static void Print(string source, string message)
        {
            // string logDate = DateTime.Now.ToString("hh:mm:ss tt"); // e.g. 05:30:45 PM
            // string logMessage = "[" + source.ToUpper() + "]: " + message;
            Console.WriteLine(message);
        }

        public static void PrintAndLog(string source, string message)
        {
            Print(source, message);
            Log(source, message);
        }

        public static void DisplayLog()
        {
            Print("Logger", "Displaying final 10 log messages... use argument \"--full\" or \"-f\" for full log");

            Console.WriteLine();

            string[] lastTenLines = logList.Take(-10).ToArray();

            foreach (string line in lastTenLines)
            {
                Console.WriteLine("> " + line);
            }

            Log("Logger", "Displaying final 10 log messages");
        }

        public static void DisplayFullLog()
        {
            Print("Logger", "Displaying full log...");

            Console.WriteLine("//");

            Log("Logger", "Displaying full log");
        }
    }
}
