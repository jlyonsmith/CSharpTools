using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Resources;
using System.Linq;

namespace Tools
{
    public class EnderTool
    {
        public enum LineEnding
        {
            Auto,
            Cr,
            Lf,
            CrLf
        }
        #region Fields
        public string InputFileName;
        public string OutputFileName;
        public LineEnding? FixedEndings;
        public bool ShowUsage;

        public bool HasOutputErrors { get; set; }
        #endregion
        
        #region Constructors
        public EnderTool()
        {
        }
        #endregion      
        
        #region Methods
        public void Execute()
        {
            if (ShowUsage)
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(true);
                string version = ((AssemblyFileVersionAttribute)attributes.First(x => x is AssemblyVersionAttribute)).Version;
                string copyright = ((AssemblyCopyrightAttribute)attributes.First(x => x is AssemblyCopyrightAttribute)).Copyright;
                string title = ((AssemblyTitleAttribute)attributes.First(x => x is AssemblyTitleAttribute)).Title;

                WriteMessage("{0}. Version {1}", title, version);
                WriteMessage("{0}.{1}", copyright, Environment.NewLine);
                WriteMessage(@"Reports on and fixes line endings for text files.
    
Usage: mono Ender.exe ...

Arguments:
    <text-file>              Input text file.
    [-o:<output-file>]       Specify different name for output file.
    [-f:<line-endings>]      Fix line endings to be cr, lf, crlf or auto.
    [-h] or [-?]             Show help.
");
                return;
            }

            if (InputFileName == null)
            {
                WriteError("A text file must be specified");
                return;
            }

            if (!File.Exists(InputFileName))
            {
                WriteError("The file '{0}' does not exist", InputFileName);
                return;
            }

            if (OutputFileName == null)
            {
                OutputFileName = InputFileName;
            }

            // Read the entire file and determine all the different line endings
            string fileContents = File.ReadAllText(InputFileName);

            int numCr = 0;
            int numLf = 0;
            int numCrLf = 0;
            int numLines = 1;

            for (int i = 0; i < fileContents.Length; i++)
            {
                char c = fileContents[i];

                if (c == '\r')
                {
                    if (i < fileContents.Length - 1 && fileContents[i + 1] == '\n')
                    {
                        numCrLf++;
                        i++;
                    }
                    else
                    {
                        numCr++;
                    }
                    numLines++;
                }
                else if (c == '\n')
                {
                    numLf++;
                    numLines++;
                }
            }

            int numEndings = 
                (numCr > 0 ? 1 : 0) + (numLf > 0 ? 1 : 0) + (numCrLf > 0 ? 1 : 0);
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat(
                "\"{0}\", lines={1}, cr={2}, lf={3}, crlf={4} {5}", 
                this.InputFileName, numLines, numCr, numLf, numCrLf, numEndings > 1 ? ", mixed" : "");

            if (!FixedEndings.HasValue)
            {
                WriteMessage(sb.ToString());
                return;
            }

            if (this.FixedEndings == LineEnding.Auto)
            {
                // Find the most common line ending and make that the automatic line ending
                LineEnding autoLineEnding = LineEnding.Lf;
                int n = numLf;

                if (numCrLf > n)
                {
                    autoLineEnding = LineEnding.CrLf;
                    n = numCrLf;
                }
                if (numCr > n)
                {
                    autoLineEnding = LineEnding.Cr;
                }

                this.FixedEndings = autoLineEnding;
            }

            int newNumLines;

            if ((this.FixedEndings == LineEnding.Cr && numCr + 1 == numLines) ||
                (this.FixedEndings == LineEnding.Lf && numLf + 1 == numLines) ||
                (this.FixedEndings == LineEnding.CrLf && numCrLf + 1 == numLines))
            {
                // We're not changing the line endings
                newNumLines = numLines;
            }
            else
            {
                string newLineChars = 
                    this.FixedEndings == LineEnding.Cr ? "\r" :
                        this.FixedEndings == LineEnding.Lf ? "\n" :
                        "\r\n";

                newNumLines = 0;

                using (StreamWriter writer = new StreamWriter(OutputFileName))
                {
                    for (int i = 0; i < fileContents.Length; i++)
                    {
                        char c = fileContents[i];

                        if (c == '\r')
                        {
                            if (i < fileContents.Length - 1 && fileContents[i + 1] == '\n')
                            {
                                i++;
                            }

                            newNumLines++;
                            writer.Write(newLineChars);
                        }
                        else if (c == '\n')
                        {
                            newNumLines++;
                            writer.Write(newLineChars);
                        }
                        else
                        {
                            writer.Write(c);
                        }
                    }
                }
                
                sb.AppendFormat(
                    " -> \"{0}\", lines={1}, {2}={3}", 
                    OutputFileName, 
                    newNumLines + 1,
                    FixedEndings.Value == LineEnding.Cr ? "cr" : 
                    FixedEndings.Value == LineEnding.Lf ? "lf" : "crlf",
                    newNumLines);
            }

            WriteMessage(sb.ToString());
        }

        public void ProcessCommandLine(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    switch (arg[1])
                    {
                    case 'h':
                    case '?':
                        ShowUsage = true;
                        return;
                    case 'o':
                        CheckAndSetArgument(arg, ref OutputFileName); 
                        continue;
                    case 'f':
                        string lineEndings = (FixedEndings.HasValue ? FixedEndings.Value.ToString() : null);
                        CheckAndSetArgument(arg, ref lineEndings); 
                        FixedEndings = (LineEnding)Enum.Parse(typeof(LineEnding), lineEndings, true);
                        break;
                    default:
                        throw new ApplicationException(string.Format("Unknown argument '{0}'", arg[1]));
                    }
                }
                else if (String.IsNullOrEmpty(InputFileName))
                {
                    InputFileName = arg;
                }
                else
                {
                    throw new ApplicationException("Only one file can be specified");
                }
            }
        }

        private void CheckAndSetArgument(string arg, ref string val)
        {
            if (arg[2] != ':')
            {
                throw new ApplicationException(string.Format("Argument {0} is missing a colon", arg[1]));
            }
            
            if (string.IsNullOrEmpty(val))
            {
                val = arg.Substring(3);
            }
            else
            {
                throw new ApplicationException(string.Format("Argument {0} has already been set", arg[1]));
            }
        }

        private void WriteError(string format, params object[] args)
        {
            Console.Write("error: ");
            Console.WriteLine(format, args);
            this.HasOutputErrors = true;
        }

        private void WriteWarning(string format, params object[] args)
        {
            Console.Write("warning: ");
            Console.WriteLine(format, args);
            this.HasOutputErrors = true;
        }

        private void WriteMessage(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
        #endregion
    }
}
