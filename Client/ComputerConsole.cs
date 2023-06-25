using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorMachine
{
    class ComputerConsole
    {
        private static string lastProgramName = "";

        public static void Prompt()
        {
            while (true)
            {
                // Read command and write log information
                Console.WriteLine();
                Console.Write(">> ");
                string command = Console.ReadLine();

                // Check if empty
                if (String.IsNullOrWhiteSpace(command))
                {
                    Logger.Print("Console", "Please input a command");
                    continue;
                }

                ParseCommand(command);
            }
        }

        public static void ParseCommand(string command) 
        { 
            string[] splitCommand = command.Split();
            Logger.Log("Console", "Command \"" + command + "\" inputted to console");

            // Check if empty
            if (splitCommand.Length == 0)
            {
                Logger.Print("Console", "Please input a command");
                return;
            }

            // Parse command into program and arguments
            string program = splitCommand[0];
            string[] arguments = new string[splitCommand.Length - 1];
            Array.Copy(splitCommand, 1, arguments, 0, splitCommand.Length - 1);

            // Determine inputted command
            if (program == "quit")
            {
                Logger.Print("Console", "Quitting...");
                return;
            }
            else if (program == "read")
            {
                if (arguments.Length < 1)
                {
                    if (lastProgramName == "")
                        Logger.Print("Console", "Please input name of file");
                    else
                        Computer.ReadProgram(lastProgramName);
                }
                else
                {
                    // If the number of arguments is 1 or higher, merge them into a string that represents the file name
                    // (Because a file name that contains spaces is split into several arguments)

                    lastProgramName = string.Join(" ", arguments);
                    Computer.ReadProgram(lastProgramName);
                }        
            }
            else if (program == "mount")
            {
                if (arguments.Length < 1)
                    Logger.Print("Console", "Please input name of file");
                else
                {
                    lastProgramName = string.Join(" ", arguments);

                    if (lastProgramName.Trim() != "")
                    {
                        Settings.SetValue("mount", lastProgramName);
                        Logger.Print("Console", "Mounted \"" + lastProgramName + ".txt\"");
                        Computer.ReadProgram(lastProgramName);
                    }
                    else
                        Logger.Print("Console", "Please input name of file");
                }
            }
            else if (program == "unmount")
            {
                string currentlyMountedFile = Settings.GetValue("mount");
                if (currentlyMountedFile == "")
                    Logger.Print("Console", "No file to unmount");
                else
                {
                    Settings.SetValue("mount", "");
                    Logger.Print("Console", "Unmounted \"" + currentlyMountedFile + ".txt\"");
                }
            }
            else if (program == "print")
            {
                if (arguments.Length == 0)
                    Computer.PrintAll();
                else if (arguments.Length > 1)
                {
                    if (arguments[0].ToLower() == "memory")
                    {
                        if (int.TryParse(arguments[1], out int location))
                            Computer.PrintFromMemory(location);
                        else
                            Logger.Print("Console", "Invalid location for memory");
                    }
                }
            }
            else if (program == "log")
            {
                if (arguments.Length > 0)
                {
                    if (arguments.Length > 1)
                    {
                        Logger.Print("Console", "Command \"log\" takes only 1 argument");
                    }   
                    else if (arguments[0] == "--full" || arguments[0] == "-f")
                    {
                        Logger.DisplayFullLog();
                    }
                    else
                    {
                        Logger.Print("Console", "Invalid argument for command \"log\"");
                    }
                }
                else
                    Logger.DisplayLog();
            }
            else if (program == "clear")
            {
                Logger.Print("Console", "Cleared assembly program, address symbol table, binary program");
                Computer.Clear();
            }
            else if (program == "assemble")
            {
                Computer.Assemble();
            }
            else if (program == "load")
            {
                if (arguments.Length > 0)
                    Logger.Print("Console", "Command \"load\" takes no arguments");
                else
                    Computer.Load();
            }
            else if (program == "run")
            {
                if (arguments.Length == 0)
                    Computer.Run();
                else if (arguments.Length > 0)
                {
                    if (int.TryParse(arguments[0], out int programStart))
                    {
                        if (arguments.Length > 1 && (arguments[1].ToLower() == "--debug" || arguments[1].ToLower() == "-d"))
                            Computer.Run(programStart, true);
                        else
                            Computer.Run(programStart);
                    }
                    else if (arguments[0].ToLower() == "--debug" || arguments[0].ToLower() == "-d")
                    {
                        Computer.Run(0, true);
                    }
                }
            }
            else
            {
                Logger.Print("Console", "Unknown command \"" + program + "\"");
            }   
        }
    }
}
