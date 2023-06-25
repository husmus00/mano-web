// using ManoMachine;
using System;
using System.Collections.Generic;
using System.IO;

namespace BlazorMachine
{
    public class Settings
    {
        // Populated with default settings
        private static Dictionary<string, string> settings = new Dictionary<string, string> {
        {"mount", ""},
        {"debug", "false"},
    };

        public Settings()
        {

        }

        public static string GetValue(string parameter)
        {
            if (settings.ContainsKey(parameter))
                return settings[parameter];
            else
                Logger.Print("Settings", "Attempted to access unknown parameter \"" + parameter + "\"");
            return string.Empty;
        }

        public static void SetValue(string parameter, string value)
        {
            if (settings.ContainsKey(parameter))
            {
                settings[parameter] = value;
                Write();
            }
            else
                Logger.Print("Settings", "Attempted to set unknown parameter \"" + parameter + "\"");
        }

        private static void Write()
        {
            string settingsFile = AppDomain.CurrentDomain.BaseDirectory + "settings.txt";
            string contents = "";
            foreach (KeyValuePair<string, string> kvp in settings)
                contents += kvp.Key + "=" + kvp.Value + "\n";
            File.WriteAllText(settingsFile, contents);
        }

        public static void Read()
        {
            string settingsFile = AppDomain.CurrentDomain.BaseDirectory + "settings.txt";

            if (File.Exists(settingsFile))
            {
                string[] contents = File.ReadAllLines(settingsFile);
                foreach (string l in contents)
                {
                    try
                    {
                        // Each parameter is in a "parameter=value" format
                        string parameter = l.Split('=')[0];
                        string value = l.Split('=')[1];

                        // Console.WriteLine("parameter is \"" + parameter + "\" - value is \"" + value + "\"");

                        if (settings.ContainsKey(parameter))
                            settings[parameter] = value;
                        else
                            Logger.Print("Settings", "ERROR: Invalid parameter \"" + parameter + "\" in settings.txt");
                    }
                    catch
                    {
                        Logger.Print("Settings", "ERROR: Invalid line \"" + l + "\" in settings.txt");
                    }
                }
            }
            // Write the settings.txt file with the default values if it doesn't exist
            else
                Write();
        }
    }
}

