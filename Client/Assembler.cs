using System;
using System.Collections.Generic;

namespace BlazorMachine
{
    class Assembler
    {
        protected static List<string> program;
        protected static uint origin = 0;

        // These are the status flags returned to the caller when calling either PassOne or PassTwo
        protected const bool passSucceeded = true;
        protected const bool passFailed = false; // !passSucceeded

        protected static int ErrorCount = 0; // used to determine final abort message if ErrorCount > 0

        protected static readonly List<string> psuedoInstructions = new List<string>
        {
            "ORG",
            "HEX",
            "DEC",
            "END"
        };
        // Memory reference instructions
        protected static readonly Dictionary<string, ushort> MRI = new Dictionary<string, ushort>
        {
            { "AND", 0x0000 },
            { "ADD", 0x1000 },
            { "LDA", 0x2000 },
            { "STA", 0x3000 },
            { "BUN", 0x4000 },
            { "BSA", 0x5000 },
            { "ISZ", 0x6000 }
        };
        // Register reference instructions
        protected static readonly Dictionary<string, ushort> RRI = new Dictionary<string, ushort>
        {
            { "CLA", 0x7800 },
            { "CLE", 0x7400 },
            { "CMA", 0x7200 },
            { "CME", 0x7100 },
            { "CIR", 0x7080 },
            { "CIL", 0x7040 },
            { "INC", 0x7020 },
            { "SPA", 0x7010 },
            { "SNA", 0x7008 },
            { "SZA", 0x7004 },
            { "SZE", 0x7002 },
            { "HLT", 0x7001 }
        };
        // Input/output instructions
        protected static readonly Dictionary<string, ushort> IO = new Dictionary<string, ushort>
        {
            { "INP", 0xF800 },
            { "OUT", 0xF400 },
            { "SKI", 0xF200 },
            { "SKO", 0xF100 },
            { "ION", 0xF080 },
            { "IOF", 0xF040 }
        };

        public static (bool passOneSuccess, Dictionary<string, uint> addressSymbolTable) PassOne(List<string> assemblyProgram)
        {
            // This function returns an address symbol table from a symbolic program

            /*
             *  // From Morris Mano's Computer System Architecture 3rd Ed.
             * 
             *  "During the first pass, it (the assembler) generates a table that correlates all
             *   user-defined address symbols with their binary equivalent value."
             *   
             *  "To avoid ambiguity in case ORG is missing, the assembler sets the
             *   location counter to 0 initially."
             *   
             *   "The tasks performed by the assembler during the first pass are described
             *    in the flowchart of Fig. 6-1."
             */

            var addressSymbolTable = new Dictionary<string, uint>();
            program = assemblyProgram;

            // first check for ORG on first line
            string firstLine = program[0];
            if (firstLine.Contains("ORG"))
            {
                // ORG must be first part of instruction
                // Parsed ORG instruction line must contain only 2 components (ORG instruction and following address number).
                string[] parsedFirstLine = ParseInstruction(firstLine);

                if (parsedFirstLine[0] == "ORG" && parsedFirstLine.Length == 2)
                {
                    // now check if second component is in fact an address (number) using TryParse
                    if (uint.TryParse(parsedFirstLine[1], out _))
                    {
                        origin = uint.Parse(parsedFirstLine[1], System.Globalization.NumberStyles.HexNumber); // Origin will be in hex format
                        addressSymbolTable.Add("ORG", origin);

                        Logger.Print("Assembler", "ORG is at address " + origin);
                    }
                    else
                    {
                        Logger.Print("Assembler", "Invalid address for ORG instruction");
                        Logger.Log("Assembler", "Invalid address for ORG instruction");
                        ErrorAtLine(0);
                        return (passFailed, null);
                    }
                }
                else
                {
                    Logger.Print("Assembler", "Invalid syntax for ORG instruction");
                    Logger.Log("Assembler", "Invalid syntax for ORG instruction");
                    ErrorAtLine(0);
                    return (passFailed, null);
                }
            }
            else
            {
                addressSymbolTable.Add("ORG", origin);

                Logger.Print("Assembler", "No ORG found on first line, setting to 0");
                Logger.Log("Assembler", "No ORG found on first line, setting to 0");
            }

            // Check for labels and record locations in address-symbol table
            // A label will contain a comma
            // Also check for END of symbolic program
            for (int i = 0; i < program.Count; i++)
            {
                string line = RemoveComment(program[i].ToUpper());

                // Check for END
                if (line.Contains("END"))
                {
                    string[] parsedLine = ParseInstruction(line);
                    if (parsedLine[0] == "END")
                    {
                        Logger.Print("Assembler", "Found END of symbolic program at program line " + (i + 1));
                        Logger.Log("Assembler", "Found END of symbolic program at program line " + (i + 1));
                        break;
                    }
                    else
                    {
                        Logger.Print("Assembler", "Invalid syntax for END instruction");
                        Logger.Log("Assembler", "Invalid syntax for END instruction");
                        ErrorAtLine(i);
                        return (passFailed, null);
                    }
                }

                // Check for label
                if (line.Contains(","))
                {
                    int commaLocation = line.IndexOf(',');
                    string label = line.Substring(0, commaLocation).Trim();

                    // checks if invalid
                    if (psuedoInstructions.Contains(label))
                    {
                        Logger.PrintAndLog("Assembler", "Cannot use invalid label \"" + label + "\"");
                        ErrorAtLine(i);
                        return (passFailed, null);
                    }
                    else if (addressSymbolTable.ContainsKey(label))
                    {
                        Logger.PrintAndLog("Assembler", "Label \"" + label + "\" is already used");
                        ErrorAtLine(i);
                        return (passFailed, null);
                    }
                    else
                    {
                        uint address = Convert.ToUInt32(i) + origin - 1; // Subtract 1 to account for ORG being on line 1 (no longer the case)
                        addressSymbolTable.Add(label, address);

                        Logger.Print("Assembler", "Found label \"" + label + "\" at program line " + (i + 1) + ", address " + address);
                    }
                }
                else
                    continue;
            }

            return (passSucceeded, addressSymbolTable);
        }

        public static (bool passTwoSuccess, Dictionary<uint, string> binaryProgram) PassTwo(List<string> assemblyProgram, Dictionary<string, uint> AST)
        {

            // This function returns a binary program from a symbolic program and an address symbol table

            /*
             *  // From Morris Mano's Computer System Architecture 3rd Ed.
             * 
             *  "Machine instructions are translated during the second pass by means of tablelookup procedures."
             *  
             *  "The assembler uses four tables. Any symbol that is encountered in the program must be available as
             *   an entry in one of these tables; otherwise, the symbol cannot be interpreted."
             *            
             *  "We assign the following names to the four tables:
             *   1. Pseudoinstruction table.
             *   2. MRI table.
             *   3. Non-MRI table.
             *   4. Address symbol table."
             */

            ErrorCount = 0;

            program = assemblyProgram;
            var addressSymbolTable = AST;
            var binaryProgram = new Dictionary<uint, string>();

            // Set origin
            if (addressSymbolTable.ContainsKey("ORG"))
                origin = addressSymbolTable["ORG"];
            else
                ErrorAtLine(-1, "Address symbol table does not contain entry for ORG");

            uint binaryLocation = uint.Parse(origin.ToString("X"), System.Globalization.NumberStyles.HexNumber);
            // ^ represents the value of "origin" (ORG instruction)
            Logger.PrintAndLog("Assembler", "Set binary start location to " + binaryLocation);

            // Check each line and determine type
            for (int i = 0; i < program.Count; i++)
            {
                string line = program[i];
                line = RemoveComment(line);
                line = RemoveLabel(line);
                string[] parsedLine = ParseInstruction(line);
                string instruction = parsedLine[0];
                string binaryInstruction = ""; // Final instruction to be added to binaryProgram list
                // Console.WriteLine(binaryLocation);

                // After removing label and comment, check for number of components in each line.
                // Each line must contain no less than 1 component (an instruction) and
                // no more than 3 components (an instruction, operand and direct/indirect indicator
                if (parsedLine.Length > 3 || parsedLine.Length < 1)
                    ErrorAtLine(i, "Incorrect number of components in instruction");

                // If instruction is a psuedoinstruction:
                if (psuedoInstructions.Contains(instruction))
                {
                    // Psuedoinstructions cannot contain more than an instruction and a
                    // single operand (2 components)
                    if (parsedLine.Length > 2)
                        ErrorAtLine(i, "Incorrect number of components for psuedoinstruction");

                    if (instruction == "ORG")
                        continue;
                    else if (instruction == "END")
                        break;
                    else if (instruction == "DEC" || instruction == "HEX")
                    {
                        // If instruction is DEC or HEX the operand must be a valid number
                        string operand = parsedLine[1];
                        if (!int.TryParse(operand, out _))
                            ErrorAtLine(i, "Invalid operand \"" + operand + "\" for instruction \"" + instruction + "\"", operand);
                        else
                        {
                            // If operand is a valid number proceed to convert into proper format
                            if (instruction == "DEC")
                            {
                                ushort decOperand = (ushort)short.Parse(operand); // convert the operand to an unsigned form
                                binaryInstruction = decOperand.ToString("X").PadLeft(4, '0');
                                binaryProgram.Add(binaryLocation, binaryInstruction);

                                Logger.Print("Assembler", "Instruction \"" + instruction + "\" at program line " + i + " and converted to \"" + binaryInstruction + "\" at binary program location " + binaryLocation);
                            }
                            else if (instruction == "HEX")
                            {
                                ushort hexOperand = (ushort)ushort.Parse(operand, System.Globalization.NumberStyles.HexNumber);
                                binaryInstruction = hexOperand.ToString("X").PadLeft(4, '0');
                                binaryProgram.Add(binaryLocation, binaryInstruction);

                                Logger.Print("Assembler", "Instruction \"" + instruction + "\" at program line " + i + " and converted to \"" + binaryInstruction + "\" at binary program location " + binaryLocation);
                            }
                        }
                    }
                    else
                        ErrorAtLine(i, "Invalid psuedoinstruction \"" + instruction + "\"", instruction);
                }

                // If instruction is a memory reference instruction
                else if (MRI.ContainsKey(instruction))
                {
                    // Memory reference instructions must contain at minimum
                    // 2 components, an instruction and a memory location (label)
                    // and an optional i component (specifying indirect address)

                    string label;

                    if (parsedLine.Length < 2)
                    {
                        ErrorAtLine(i, "Incorrect number of components in instruction");
                        label = "";
                    }  
                    else
                        label = parsedLine[1];

                    uint labelAddress;

                    if (addressSymbolTable.ContainsKey(label))
                        labelAddress = addressSymbolTable[label];
                    else
                    {
                        ErrorAtLine(i, "Unknown label \"" + label + "\"", label);
                        labelAddress = 0;
                    }

                    ushort hexBinaryInstruction = (ushort)(MRI[instruction] + labelAddress);

                    // Determine if address is direct or indirect
                    if (parsedLine.Length == 3)
                    {
                        if (parsedLine[2] == "i")
                        {
                            // If "i" is present instruction is indirect. Add (hex) 8000 to convert to indirect
                            hexBinaryInstruction += (ushort)int.Parse("8000", System.Globalization.NumberStyles.HexNumber);
                        }
                        else
                        {
                            Logger.PrintAndLog("Assembler", "Unknown address modifier \"" + parsedLine[2] + "\"");
                            ErrorAtLine(i, "Unknown address modifier \"" + parsedLine[2] + "\"", " " + parsedLine[2]);
                            // ^ Added spaces to the front of the "cause" so that it'll catch the correct position, and
                            // not a random letter in the middle of a word (unless it's at the beginnning, then we're screwed)
                        }
                    }

                    binaryInstruction = hexBinaryInstruction.ToString("X");
                    binaryProgram.Add(binaryLocation, binaryInstruction);
                    Logger.Print("Assembler", "Instruction \"" + instruction + "\" at program line " + i + " and converted to \"" + binaryInstruction + "\" at binary program location " + binaryLocation);
                }

                // If instruction is a register reference instruction or input/output instruction
                else if (RRI.ContainsKey(instruction) || IO.ContainsKey(instruction))
                {
                    if (RRI.ContainsKey(instruction))
                    {
                        binaryInstruction = RRI[instruction].ToString("X");
                    }
                    else if (IO.ContainsKey(instruction))
                    {
                        binaryInstruction = IO[instruction].ToString("X");
                    }

                    binaryProgram.Add(binaryLocation, binaryInstruction);
                    Logger.Print("Assembler", "Instruction \"" + instruction + "\" at program line " + i + " and converted to \"" + binaryInstruction + "\" at binary program location " + binaryLocation);
                }
                // If instruction is not found
                else
                {
                    ErrorAtLine(i, "Unknown instruction \"" + instruction + "\"", instruction);
                }

                binaryLocation++;
            }

            // Final check for occurence of error(s)

            if (ErrorCount > 0)
            {
                if (ErrorCount == 1)
                    Logger.PrintAndLog("Assembler", "Encountered 1 error");
                else
                    Logger.PrintAndLog("Assembler", "Encountered " + ErrorCount + " errors");

                return (passFailed, null);
            }
            else
                return (passSucceeded, binaryProgram);
        }

        public static string RemoveComment(string instruction)
        {
            // Remove comment and remaining surrounding whitespace
            int locationOfSlash = instruction.IndexOf('/');
            if (locationOfSlash > -1)
                instruction = instruction.Substring(0, locationOfSlash).Trim();
            return instruction;
        }

        public static string RemoveLabel(string instruction)
        {
            if (instruction.Contains(","))
            {
                int commaLocation = instruction.IndexOf(',');
                // Logger.Print("Assembler", "Removing label from instruction \"" + instruction + "\", substringing from " + (commaLocation + 1) + " with length " + (instruction.Length - commaLocation - 1));
                instruction = instruction.Substring(commaLocation + 1, instruction.Length - commaLocation - 1).Trim();
                // Logger.Print("Assembler", "Instruction after removing label: \"" + instruction + "\"");
            }
            return instruction;
        }

        public static string[] ParseInstruction(string instruction)
        {
            // Remove comment and remaining surrounding whitespace
            instruction = RemoveComment(instruction);
            // Split instruction into it's components
            string[] parsedInstruction = instruction.Split();
            return parsedInstruction;
        }

        public static void ErrorAtLine(int lineIndex, string error = "", string cause = "")
        {
            // "lineIndex" represents the program line which caused the error,
            // "cause" represents the portion of the line which caused the error
            // "error" is the error description

            ErrorCount++;

            if (lineIndex > -1)
            {
                string faultyLine = program[lineIndex].Trim();
                Console.WriteLine();
                Logger.PrintAndLog("Assembler", "Error No." + ErrorCount + " at line " + lineIndex + ": ");
                Logger.PrintAndLog("Assembler", " <" + lineIndex + "> " + faultyLine);

                int padding;
                if (cause != "" && faultyLine.Contains(cause))
                {
                    padding = 4 + lineIndex.ToString().Length + faultyLine.IndexOf(cause);
                    // ^ compensates for the line number indicator (e.g. <10>) plus the offset of the fault's location
                }
                else
                {
                    padding = 4 + lineIndex.ToString().Length;
                    // ^ compensates for the line number indicator (e.g. <10>)
                }

                Logger.PrintAndLog("Assembler", new string(' ', padding) + "^");
                Logger.PrintAndLog("Assembler", new string(' ', padding) + error);
            }
            else
                Logger.PrintAndLog("Assembler", "Error in program");

            Console.WriteLine();
        }
    }
}
