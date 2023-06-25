using System;
using System.Collections.Generic;
using System.IO;

namespace BlazorMachine
{
    class Computer
    {
        private static readonly string version = "0.1.6" ;
        private static readonly string stage = "Alpha";
        private static readonly string link = "https://github.com/Husmus00/Mano-Machine-CSharp";

        // [Obsolete("assemblyFailureFlag is no longer needed", true)]
        // private static bool assemblyFailureFlag = false;

        private static bool debugMode = false;
        private static string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

        // Components
        private static readonly AssemblyProgram assemblyProgram = new AssemblyProgram();
        private static readonly AddressSymbolTable addressSymbolTable = new AddressSymbolTable();
        private static readonly BinaryProgram binaryProgram = new BinaryProgram();

        private static short[] RAM = new short[4096];
        private static readonly Register AR   = new Register(12); // Address register
        private static readonly Register PC   = new Register(12); // Program counter
        private static readonly Register DR   = new Register(16); // Data register
        private static readonly Register AC   = new Register(16); // Accumulator
        private static readonly Register IR   = new Register(16); // Instruction register
        private static readonly Register TR   = new Register(16); // Temporary register
        private static readonly Register INPR = new Register(8);  // Input register
        private static readonly Register OUTR = new Register(8);  // Output register
        private static readonly Register SC   = new Register(3);  // Sequence counter
        private static readonly Register E    = new Register(1);  // Carry bit
        private static readonly Register S    = new Register(1);  // Start / stop computer
        private static readonly Register R    = new Register(1);  // Interrupt raised
        private static readonly Register IEN  = new Register(1);  // Interrupt enable
        private static readonly Register FGI  = new Register(1);  // Input register available
        private static readonly Register FGO  = new Register(1);  // Output register available

        public Computer()
        {
            // binaryProgram.Set(new Dictionary<uint, string> { { 0x100, "7400" } }); // Testing

            Start();
        }

        private void Start()
        {
            InitialInfo();
            Logger.Initialize();
            Settings.Read();

            // In case "programs" directory does not already exist:
            string programsDirPath = currentDirectory + @"programs\";
            Directory.CreateDirectory(programsDirPath);

            // Auto mount file if found in settings
            string fileToMount = Settings.GetValue("mount");
            if (fileToMount != "")
                ReadProgram(fileToMount);

            // ComputerConsole.Prompt();
        }

        public static void InitialInfo()
        {
            // Prints basic info to the console on program launch
            Console.WriteLine("Mano Machine".ToUpper());
            Console.WriteLine("Version " + version + " " + stage);
            Console.WriteLine("Original repository at " + link);
            Console.WriteLine(new string('-', 20));
            Console.WriteLine();
        }

        public static void ReadProgram(string fileName)
        {
            if (fileName.Trim() != "")
            {
                if (!fileName.EndsWith(".txt"))
                    fileName += ".txt";

                //specifies path to the text file, must be located in "programs" directory, which must be located in same directory as executable file
                string path = currentDirectory + "programs\\" + fileName;
                string[] program;

                Logger.Log("Computer", "Reading file \"" + fileName + "\" from directory \"" + currentDirectory + "\"");
                Logger.Print("Computer", "Reading file \"" + fileName + "\"");

                try
                {
                    program = File.ReadAllLines(path);

                    if (program.Length > 0)
                    {
                        assemblyProgram.Set(program);
                        Logger.Log("Computer", "File \"" + fileName + "\" read successfully");
                        Logger.Print("Computer", "File \"" + fileName + "\" read successfully");
                    }
                    else
                    {
                        Logger.Log("Computer", "File \"" + fileName + "\" is empty");
                        Logger.Print("Computer", "File \"" + fileName + "\" is empty");
                    }
                }
                catch (Exception e)
                {
                    Logger.Log("Computer", "ERROR: Failed to read file \"" + fileName + "\" from directory \"" + currentDirectory + "\". Exception: \"" + e.Message + "\"");
                    Logger.Print("Computer", "ERROR: Could not read file \"" + fileName + "\", check log for details");
                    // program = new string[0];
                }
            }
            else
                Logger.Print("Computer", "ERROR: File name cannot be empty, aborting read");
        }

        public static void PrintAll()
        {
            PrintAssemblyProgram();
            Console.WriteLine();
            PrintAddressSymbolTable();
            Console.WriteLine();
            PrintBinaryProgram();
            Console.WriteLine();
        }

        public static void PrintAssemblyProgram()
        {
            Logger.Log("Computer", "Attempting to print assembly program");

            if (assemblyProgram.Size() > 0)
            {
                Console.WriteLine("Assembly program:");
                assemblyProgram.Print();
            }    
            else
            {
                Logger.Log("Computer", "Failed to print assembly program to console, assembly program is empty");
                Logger.Print("Computer", "Assembly program is empty");
            }
        }

        public static void PrintAddressSymbolTable()
        { 
            Logger.Log("Computer", "Attempting to print address symbol table");

            if (addressSymbolTable.Size() > 0)
            {
                Console.WriteLine("Address Symbol Table:");
                addressSymbolTable.Print();
            }   
            else
            {
                Logger.Log("Computer", "Failed to print address symbol table to console, assembly program is empty");
                Logger.Print("Computer", "Address symbol table is empty");
            }
        }

        public static void PrintBinaryProgram()
        {
            Logger.Log("Computer", "Attempting to print binary program");

            if (binaryProgram.Size() > 0)
            {
                Console.WriteLine("Binary Program:");
                binaryProgram.Print();
            }
            else
            {
                Logger.Log("Computer", "Failed to print binary program to console, assembly program is empty");
                Logger.Print("Computer", "Binary program is empty");
            }
        }

        public static void PrintFromMemory(int location)
        {
            Logger.Log("Computer", "Attempting to print memory location " + location);

            if (location > 4095 || location < 0)
            {
                Logger.Log("Computer", "Failed to print memory location " + location + " to console, out of range");
                Logger.Print("Computer", "Location is out of range");
            }
            else
            {
                string line = Convert.ToString(RAM[location], 16) + " (bin: " + Convert.ToString(RAM[location], 2) + ") (dec: " + RAM[location] + ")";
                Console.WriteLine(line);
            }
        }

        public static void Clear()
        {
            assemblyProgram.Clear();
            addressSymbolTable.Clear();
            binaryProgram.Clear();
        }

        public static void AssembleFromGUI(string program)
        {
            string[] splitProgram = program.Split(Environment.NewLine);
            assemblyProgram.Set(splitProgram);
            Assemble();
        }

        public static void Assemble()
        {
            if (assemblyProgram.Size() == 0)
                Logger.Print("Computer", "Program is empty");
            else
            {
                Logger.PrintAndLog("Computer", "Starting pass one of assembly");

                // Receive success flag and address symbol table from Assembler.PassOne
                (bool passOneSuccess, var tempAddressSymbolTable) = Assembler.PassOne(assemblyProgram.Content());

                if (!passOneSuccess)
                    Logger.PrintAndLog("Computer", "Pass one failed, aborting assembly process");
                else
                {
                    addressSymbolTable.Set(tempAddressSymbolTable);

                    Logger.Print("Computer", "Pass one of assembly ended successfully");
                    Logger.Log("Computer", "Pass one of assembly ended successfully");
                    Console.WriteLine();

                    Logger.Print("Computer", "Starting pass two of assembly");
                    Logger.Log("Computer", "Starting pass two of assembly");

                    // Receive success flag and binary program from Assembler.PassTwo
                    (bool passTwoSuccess, var tempBinaryProgram) = Assembler.PassTwo(assemblyProgram.Content(), addressSymbolTable.Content());

                    if (!passTwoSuccess)
                        Logger.PrintAndLog("Computer", "Pass two failed, aborting assembly process");
                    else
                    {
                        binaryProgram.Set(tempBinaryProgram);

                        Logger.Print("Computer", "Pass two of assembly ended successfully");
                        Logger.Log("Computer", "Pass two of assembly ended successfully");
                        Console.WriteLine();
                    }
                } 
            }

            // assemblyFailureFlag = false;
        }

        // Used to set the assembly failure flag
        [Obsolete("assemblyFailureFlag is no longer needed")]
        public static void AssemblyFailed()
        {
            // assemblyFailureFlag = true;
            Console.WriteLine();
            Logger.Print("Computer", "Assembly Failed");
            Logger.Log("Computer", "Assembly Failed");
        }

        public static void Load()
        {
            if (binaryProgram.Empty())
                Logger.Print("Computer", "Binary program is empty, nothing to load");
            else
            {
                foreach (KeyValuePair<uint, string> instruction in binaryProgram.Content())
                {
                    RAM[instruction.Key] = Convert.ToInt16(instruction.Value, 16);
                }
            }
        }

        private static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        private static void DebugMode()
        {
            Console.Clear();
            Console.WriteLine("DEBUG MODE");
            Console.WriteLine();
            Console.WriteLine("AR: " + AR.Word + " (Hex: " + AR.Word.ToString("X").PadLeft(4, '0') + ")");
            Console.WriteLine("PC: " + PC.Word + " (Hex: " + PC.Word.ToString("X").PadLeft(4, '0') + ")");
            Console.WriteLine("DR: " + DR.Word + " (Hex: " + DR.Word.ToString("X").PadLeft(4, '0') + ")");
            Console.WriteLine("AC: " + AC.Word + " (Hex: " + AC.Word.ToString("X").PadLeft(4, '0') + ")");
            Console.WriteLine("IR: " + IR.Word + " (Hex: " + IR.Word.ToString("X").PadLeft(4, '0') + ")");
            Console.WriteLine("TR: " + TR.Word + " (Hex: " + TR.Word.ToString("X").PadLeft(4, '0') + ")");
            Console.WriteLine("SC: " + SC.Word + " (Hex: " + SC.Word.ToString("X").PadLeft(4, '0') + ")");
            Console.WriteLine("E : " + E.Word + " (Hex: " + E.Word.ToString("X").PadLeft(4, '0') + ")");
            Console.WriteLine("S : " + S.Word + " (Hex: " + S.Word.ToString("X").PadLeft(4, '0') + ")");
            Console.WriteLine("Memory at AR: " + RAM[AR.Word] + " (Hex: " + RAM[AR.Word].ToString("X").PadLeft(4, '0') + ")");

            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        public static void Run(int programStart = 0, bool debug = false)
        {
            Load();

            // Figure 5-15 chapter 5 page 158 (169 in reader)
            if (programStart < 0 || programStart > 4095)
            {
                Logger.Print("Computer", "Out of bounds starting point for program");
            }
            else if (binaryProgram.Size() == 0)
            {
                Logger.Print("Computer", "Binary program is empty");
            }
            else
            {
                PC.Set(programStart);
                S.Set(1);
                IR.Set(1);
                while (S.Word == 1)
                {
                    if (debug)
                        DebugMode();
                    Tick();
                }
                // if (PC.Word != 0)
                debugMode = false;
            }
        }

        private static void Tick()
        {
            // Figure 5-15 chapter 5 page 158 (169 in reader)

            int tick = SC.Word;
            int interruptRaised = R.Word;
            int opcode = GetOpcode();    // Bits 12-14
            int iBit = GetIndirectBit(); // Bit 15 (indirect bit)

            /*
            if (IR.Word == 0)
            {
                Logger.Print("Computer", "IR is 0");
                S.Set(0);
            }
            */

            if (tick < 3 && interruptRaised == 1)
                Interrupt(tick);
            else if (tick < 2 && interruptRaised == 0)
                InstructionFetch(tick);
            else if (tick == 2 && interruptRaised == 0)
                InstructionDecode();
            else if (tick == 3 && opcode != 7)
                FetchOperand(iBit);
            else if (tick > 3 && opcode != 7)
                ExecuteMRI(opcode, tick);
            else if (tick == 3 && opcode == 7)
            {
                int instruction = GetInstructionBit();
                if (iBit == 1)
                    ExecuteIO(instruction);
                else
                    ExecuteRRI(instruction);
            }
        }

        private static void WriteToMemory(Register targetRegister)
        {
            RAM[AR.Word] = (short)targetRegister.Word;
        }

        private static int ReadFromMemory()
        {
            return RAM[AR.Word];
        }

        private static int GetOpcode()
        {
            // Returns bits 12-14 in IR
            int opcode = (IR.Word >> 12) & 7;
            return opcode;
        }

        private static int GetIndirectBit()
        {
            // Returns bit 15 in IR
            int i = (IR.Word >> 15) & 1;
            return i;
        }

        private static byte GetInstructionBit()
        {
            // Returns index of "1" bit in bits 0-11 that specifies the operation for
            // register reference and input / output instructions
            string IRString = Convert.ToString(IR.Word, 2).PadLeft(16, '0');
            IRString = Reverse(IRString);
            byte InstructionBit = (byte)IRString.IndexOf('1');
            // Logger.Print("Computer", "IR is " + IRString + " and instruction bit is " + InstructionBit);
            return InstructionBit;
        }

        private static void Interrupt(int tick)
        {
            if (tick == 0)
            {
                // "RT0: AR <- 0, TR <- PC"
                AR.Clear();
                TR.Set(PC.Word);
                SC.Increment();
            }
            else if (tick == 1)
            {
                // "RT1: M[AR] <- TR, PC <- 0"
                WriteToMemory(TR);
                PC.Clear();
                SC.Increment();
            }
            else if (tick == 2)
            {
                // "RT2: PC <- PC + 1, IEN <- 0, R <- 0, SC <- 0"
                PC.Increment();
                IEN.Clear();
                R.Clear();
                SC.Clear();
            }
        }

        private static void InstructionFetch(int tick)
        {
            if (tick == 0)
            {
                // "R'T0: AR <- PC"
                AR.Set(PC.Word);
                SC.Increment();
            }
            else if (tick == 1)
            {
                // "R'T1: IR <- M[AR], PC <- PC + 1"
                IR.Set(ReadFromMemory());
                PC.Increment();
                SC.Increment();
            }
        }

        private static void InstructionDecode()
        {
            // "R'T2: AR <- IR(0-11)"
            ushort addressBits = (ushort)(IR.Word << 4);
            addressBits = (ushort)(addressBits >> 4);
            AR.Set(addressBits);
            SC.Increment();
        }

        private static void FetchOperand(int iBit)
        {
            // For when the operand is indirect
            if (iBit == 1)
            {
                // "D7'IT3: AR <- M[AR]"
                AR.Set(ReadFromMemory());
            }
            // else;
                // "D7'I'T3: NOOP" // No operation
                
            SC.Increment();
        }

        private static void ExecuteMRI(int opcode, int tick)
        {

            if (opcode == 0)
                ExecuteAND(tick);
            else if (opcode == 1)
                ExecuteADD(tick);
            else if (opcode == 2)
                ExecuteLDA(tick);
            else if (opcode == 3)
                ExecuteSTA();
            else if (opcode == 4)
                ExecuteBUN();
            else if (opcode == 5)
                ExecuteBSA(tick);
            else if (opcode == 6)
                ExecuteISZ(tick);
        }

        private static void ExecuteRRI(int instruction)
        {
            if (instruction == 11)
                ExecuteCLA();
            else if (instruction == 10)
                ExecuteCLE();
            else if (instruction == 9)
                ExecuteCMA();
            else if (instruction == 8)
                ExecuteCME();
            else if (instruction == 7)
                ExecuteCIR();
            else if (instruction == 6)
                ExecuteCIL();
            else if (instruction == 5)
                ExecuteINC();
            else if (instruction == 4)
                ExecuteSPA();
            else if (instruction == 3)
                ExecuteSNA();
            else if (instruction == 2)
                ExecuteSZA();
            else if (instruction == 1)
                ExecuteSZE();
            else if (instruction == 0)
                ExecuteHLT();

            SC.Clear();
        }

        private static void ExecuteIO(int instruction)
        {
            if (instruction == 11)
                ExecuteINP();
            else if (instruction == 10)
                ExecuteOUT();
            else if (instruction == 9)
                ExecuteSKI();
            else if (instruction == 8)
                ExecuteSKO();
            else if (instruction == 7)
                ExecuteION();
            else if (instruction == 6)
                ExecuteIOF();

            SC.Clear();
        }

        // MRI instructions
        // Figure 5-11 chapter 5 page 150 (161 in reader)

        private static void ExecuteAND(int tick)
        {
            Logger.Print("Computer", "AND");

            if (tick == 4)
            {
                Logger.PrintAndLog("Computer", "D0T4: DR <- M[AR]");
                DR.Set(ReadFromMemory());
                SC.Increment();
            }
            else if (tick == 5)
            {
                Logger.PrintAndLog("Computer", "D0T5: AC <- AC & DR, SC <- 0");
                AC.LogicAND(DR.Word); // AC = AC & DC
                SC.Clear();
            }
        }

        private static void ExecuteADD(int tick)
        {
            Logger.Print("Computer", "ADD");

            if (tick == 4)
            {
                Logger.PrintAndLog("Computer", "D1T4: DR <- M[AR]"); // A typo exists on this page (150) [MAR] instead of M[AR]
                DR.Set(ReadFromMemory());
                SC.Increment();
            }
            else if (tick == 5)
            {
                Logger.PrintAndLog("Computer", "D1T5: AC <- AC + DR, E <- Cout, SC <- 0");
                E.Set(AC.Add(DR.Word)); // AC = AC + DC, E = Cout of AC + DC
                SC.Clear();
            }
        }

        private static void ExecuteLDA(int tick)
        {
            Logger.Print("Computer", "LDA");

            if (tick == 4)
            {
                Logger.PrintAndLog("Computer", "D2T4: DR <- M[AR]");
                DR.Set(ReadFromMemory());
                SC.Increment();
            }
            else if (tick == 5)
            {
                Logger.PrintAndLog("Computer", "D2T5: AC <- DR, SC <- 0");
                AC.Set(DR.Word);
                SC.Clear();
            }
        }

        private static void ExecuteSTA()
        {
            Logger.Print("Computer", "STA");

            Logger.PrintAndLog("Computer", "D3T4: M[AR] <- AC, SC <- 0");
            WriteToMemory(AC);
            SC.Clear();
        }

        private static void ExecuteBUN()
        {
            Logger.Print("Computer", "BUN");

            Logger.PrintAndLog("Computer", "D4T4: PC <- AR, SC <- 0");
            PC.Set(AR.Word);
            SC.Clear();
        }

        private static void ExecuteBSA(int tick)
        {
            Logger.Print("Computer", "BSA");

            if (tick == 4)
            {
                Logger.PrintAndLog("Computer", "D5T4: M[AR] <- PC, AR <- AR + 1");
                WriteToMemory(PC);
                AR.Increment();
                SC.Increment();
            }
            else if (tick == 5)
            {
                Logger.PrintAndLog("Computer", "D5T5: PC <- AR, SC <- 0");
                PC.Set(AR.Word);
                SC.Clear();
            }
        }

        private static void ExecuteISZ(int tick)
        {
            Logger.Print("Computer", "ISZ");

            if (tick == 4)
            {
                Logger.PrintAndLog("Computer", "D6T4: DR <- M[AR]");
                DR.Set(ReadFromMemory());
                SC.Increment();
            }
            else if (tick == 5)
            {
                Logger.PrintAndLog("Computer", "D6T5: DR <- DR + 1");
                DR.Increment();
                SC.Increment();
            }
            else if (tick == 6)
            {
                Logger.PrintAndLog("Computer", "D6T6: M[AR] <- DR, if (DR = 0) then (PC <- PC + 1), SC <- 0");
                WriteToMemory(DR);
                if (DR.Word == 0)
                    PC.Increment();
                SC.Clear();
            }
        }

        // RRI instructions
        // Table 5-6 Chapter 5 page 159 (170 in reader)

        private static void ExecuteCLA()
        {
            Logger.Print("Computer", "CLA");
            Logger.PrintAndLog("Computer", "D7I'T3rB11: AC <- 0, SC <- 0");
            AC.Clear();
        }

        private static void ExecuteCLE()
        {
            Logger.Print("Computer", "CLE");
            Logger.PrintAndLog("Computer", "D7I'T3rB10: E <- 0, SC <- 0");
            E.Clear();
        }

        private static void ExecuteCMA()
        {
            Logger.Print("Computer", "CMA");
            Logger.PrintAndLog("Computer", "D7I'T3rB9: AC <- AC', SC <- 0");
            AC.Complement();
        }

        private static void ExecuteCME()
        {
            Logger.Print("Computer", "CME");
            Logger.PrintAndLog("Computer", "D7I'T3rB8: AC <- 0, SC <- 0");
            E.Complement();
        }

        private static void ExecuteCIR()
        {
            Logger.Print("Computer", "CIR");
            Logger.PrintAndLog("Computer", "D7I'T3rB7: AC <- shr AC, AC(15) <- E, E <- AC(0), SC <- 0");
            E.Set(AC.ShiftRight(E.Word)); // Set E to the return value of AC shift right operation
        }

        private static void ExecuteCIL()
        {
            Logger.Print("Computer", "CIL");
            Logger.PrintAndLog("Computer", "D7I'T3rB6: AC <- shl AC, AC(0) <- E, E <- AC(15), SC <- 0");
            E.Set(AC.ShiftLeft(E.Word)); // Set E to the return value of AC shift left operation
        }

        private static void ExecuteINC()
        {
            Logger.Print("Computer", "INC");
            Logger.PrintAndLog("Computer", "D7I'T3rB5: AC <- AC + 1, SC <- 0");
            AC.Increment();
        }

        private static void ExecuteSPA()
        {
            Logger.Print("Computer", "SPA");
            Logger.PrintAndLog("Computer", "D7I'T3rB4: if (AC(15) = 0) then (PC <- PC + 1), SC <- 0");
            // 0x8000 is (binary) 1000 0000 0000 0000. This will determine if the first bit is 0 or not
            if ((AC.Word & 0x8000) == 0)
                PC.Increment();
        }

        private static void ExecuteSNA()
        {
            Logger.Print("Computer", "SNA");
            Logger.PrintAndLog("Computer", "D7I'T3rB3: if (AC(15) = 1) then (PC <- PC + 1), SC <- 0");
            // 0x8000 is (binary) 1000 0000 0000 0000. This will determine if the first bit is 1 or not
            if ((AC.Word & 0x8000) == 0) 
                PC.Increment();
        }

        private static void ExecuteSZA()
        {
            Logger.Print("Computer", "SZA");
            Logger.PrintAndLog("Computer", "D7I'T3rB2: if (AC = 0) then (PC <- PC + 1), SC <- 0");
            if (AC.Word == 0)
                PC.Increment();
        }

        private static void ExecuteSZE()
        {
            Logger.Print("Computer", "SZE");
            Logger.PrintAndLog("Computer", "D7I'T3rB1: if (E = 0) then (PC <- PC + 1), SC <- 0");
            if (E.Word == 0)
                PC.Increment();
        }

        private static void ExecuteHLT()
        {
            Logger.Print("Computer", "HLT");
            Logger.PrintAndLog("Computer", "D7I'T3rB0: S <- 0, SC <- 0");
            S.Set(0);
        }

        // IO instructions
        // Table 5-6 Chapter 5 page 159 (170 in reader)

        private static void ExecuteINP()
        {
            Logger.Print("Computer", "INP");
        }

        private static void ExecuteOUT()
        {
            Logger.Print("Computer", "OUT");
        }

        private static void ExecuteSKI()
        {
            Logger.Print("Computer", "SKI");
        }

        private static void ExecuteSKO()
        {
            Logger.Print("Computer", "SKO");
        }

        private static void ExecuteION()
        {
            Logger.Print("Computer", "ION");
            Logger.PrintAndLog("Computer", "D7IT3pB7: IEN <- 1, SC <- 0");
            IEN.Set(1);
        }

        private static void ExecuteIOF()
        {
            Logger.Print("Computer", "IOF");
            Logger.PrintAndLog("Computer", "D7IT3pB6: IEN <- 0, SC <- 0");
            IEN.Set(0);
        }
    }
}
