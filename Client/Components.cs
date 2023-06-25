using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BlazorMachine
{
    class AssemblyProgram
    {
        private List<string> content;

        public AssemblyProgram()
        {
            content = new List<string>();
        }

        public int Size()
        {
            try
            {
                return content.Count();
            }
            catch (Exception e)
            {
                return 0;
            }
        }
            
        public void Set(string[] contentToSet)
        {
            Clear();
            foreach (string line in contentToSet)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    content.Add(line);
            }
        }

        public List<string> Content()
        {
            return content;
        }

        public void Print()
        {
            // retrives length of longest line by ordering by length in descending order, and returning the length of first instance
            int longestLineLength = content.OrderByDescending(s => s.Length).First().Length;

            Console.WriteLine(new string('-', longestLineLength + 8)); // Add 8 to account for the leading number e.g. "[0001]: "

            for (int i = 0; i < content.Count(); i++)
            {
                string line = "[" + i.ToString().PadLeft(4, '0') + "]: " + content[i];
                Console.WriteLine(line);
            }

            Console.WriteLine(new string('-', longestLineLength + 8));
        }

        public void Clear()
        {
            content.Clear();
        }
    }

    class AddressSymbolTable
    {
        private Dictionary<string, uint> content;

        public AddressSymbolTable()
        {
            content = new Dictionary<string, uint>();
        }

        public int Size()
        {
            try
            {
                return content.Count();
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public void Set(Dictionary<string, uint> contentToSet)
        {
            Clear();
            content = contentToSet;
        }

        public Dictionary<string, uint> Content()
        {
            return content;
        }

        public void Print()
        {
            // retrives length of longest label by ordering by length in descending order, and returning the length of first instance
            int longestLabelLength = content.OrderByDescending(s => s.Key.Length).First().Key.Length;

            Console.WriteLine(new string('-', longestLabelLength + 11)); // Add 7 to account for " : " and label address (with padding) and side "|"s

            foreach (KeyValuePair<string, uint> label in content)
            {
                string line = "| " + label.Key.PadLeft(longestLabelLength) + " : " + label.Value.ToString().PadLeft(4, '0') + " |";
                Console.WriteLine(line);
            }

            Console.WriteLine(new string('-', longestLabelLength + 11));
        }

        public void Clear()
        {
            content = new Dictionary<string, uint>();
            content.Clear();
        }
    }

    class BinaryProgram
    {
        private Dictionary<uint, string> content;

        public BinaryProgram()
        {
            content = new Dictionary<uint, string>();
        }

        public int Size()
        {
            try
            {
                return content.Count();
            }
            catch
            {
                return 0;
            }
        }

        public bool Empty()
        {
            if (content.Count == 0)
                return true;
            else
                return false;
        }

        public void Set(Dictionary<uint, string> contentToSet)
        {
            Clear();
            content = contentToSet;
        }

        public Dictionary<uint, string> Content()
        {
            return content;
        }

        public void Print()
        {
            Console.WriteLine(new string('-', 34));
            Console.WriteLine(("| Adrs : Instruction").PadRight(33) + "|");

            foreach (KeyValuePair<uint, string> binaryInstruction in content)
            {
                string line = "| " + Convert.ToString(binaryInstruction.Key, 16).PadLeft(4, '0') + " : "
                    + binaryInstruction.Value
                    + " (" + Convert.ToString(Int32.Parse(binaryInstruction.Value, NumberStyles.HexNumber), 2).PadLeft(16, '0') + ") |";
                Console.WriteLine(line);
            }
            Console.WriteLine(new string('-', 34));
        }

        public void Clear()
        {
            content = new Dictionary<uint, string>();
            content.Clear();
        }
    }

    public class Register
    {
        private readonly byte bits;
        private readonly int maxValue;
        private readonly int mask;
        private short word;

        public Register(byte b)
        {
            bits = b;
            maxValue = (int)Math.Pow(2, bits);
            mask = maxValue - 1;
            word = 0;
        }

        public short Word
        {
            get { return word; }
            // set { word = (ushort)value; }
        }

        /*
        public ushort Word
        {
            get { return word; }
            set { word = value; }
        }
        */

        public void Set(int w)
        {
            // word = (ushort)(w % maxValue);
            word = (short)w;
        }

        public void Clear()
        {
            word = 0;
        }

        public void Increment()
        {
            Set(word + 1);
        }

        public void LogicAND(int w)
        {
            Set(word & w);
        }

        public int Add(int w)
        {
            int value = word + w;
            int carry = value & maxValue;
            Set(word + w);

            if (carry > 0)
                return 1;
            else
                return 0;
        }

        public void Complement()
        {
            Set(~word);
        }

        // Note: MSB = most significant bit, LSB = least significant bit

        public int ShiftRight(int msb)
        {
            int lsb = word & 1;
            Set(word >> 1);
            if (Convert.ToBoolean(msb))
            {
                int msbMask = maxValue >> 1;
                Set(word | msbMask);
            }
            return lsb;
        }

        public int ShiftLeft(int lsb)
        {
            int msbMask = maxValue >> 1;
            int msb = Convert.ToBoolean(word & msbMask) ? 1 : 0;
            Set((word << 1) & mask);
            if (Convert.ToBoolean(lsb))
                Set(word | 1);
            return msb;
        }
    }
}
