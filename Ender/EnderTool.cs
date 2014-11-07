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
using ToolBelt;

namespace Tools
{
    [CommandLineTitle("Ender Line Ending Fixer")]
    [CommandLineDescription("Reports on and optionally fixes line endings in text files")]
    [CommandLineCopyright("Copyright (c) John Lyon-Smith 2014")]
    public class EnderTool : ToolBase
    {
        public enum LineEnding
        {
            Auto,
            Mixed,
            Cr,
            Lf,
            CrLf
        }

        [DefaultCommandLineArgument(Description="The input file to scan", ValueHint="INPUTFILE")]
        public string InputFileName { get; set; }
        [CommandLineArgument("output", ShortName="o", Description="The output file.  Default is the same as the input file.", ValueHint="OUTPUTFILE")]
        public string OutputFileName { get; set; }
        [CommandLineArgument("mode", ShortName="m", Description="The convert mode, one of auto, cr, lf, crlf. " + 
            "auto will use the most commonly occurring ending. Updates will only be done when this argument is given.", 
            Initializer=typeof(EnderTool), MethodName="ParseMode")]
        public LineEnding? ConvertMode { get; set; }
        [CommandLineArgument("help", ShortName="?", Description="Shows this help.")]
        public bool ShowUsage { get; set; }

        public static LineEnding? ParseMode(string arg)
        {
            return (LineEnding?)Enum.Parse(typeof(LineEnding), arg, true);
        }

        #region Methods
        public override void Execute()
        {
            if (ShowUsage)
            {
                WriteMessage(this.Parser.LogoBanner);
                WriteMessage(this.Parser.Usage);
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

            int numEndings = (numCr > 0 ? 1 : 0) + (numLf > 0 ? 1 : 0) + (numCrLf > 0 ? 1 : 0);
            StringBuilder sb = new StringBuilder();
            LineEnding le = numEndings > 1 ? LineEnding.Mixed : numCr > 0 ? LineEnding.Cr : numLf > 0 ? LineEnding.Lf : LineEnding.CrLf;

            sb.AppendFormat(
                "\"{0}\", {1}", 
                this.InputFileName, Enum.GetName(typeof(LineEnding), le).ToLower());

            if (!ConvertMode.HasValue)
            {
                WriteMessage(sb.ToString());
                return;
            }

            if (this.ConvertMode == LineEnding.Auto)
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

                this.ConvertMode = autoLineEnding;
            }

            int newNumLines;

            if ((this.ConvertMode == LineEnding.Cr && numCr + 1 == numLines) ||
                (this.ConvertMode == LineEnding.Lf && numLf + 1 == numLines) ||
                (this.ConvertMode == LineEnding.CrLf && numCrLf + 1 == numLines))
            {
                // We're not changing the line endings
                newNumLines = numLines;
            }
            else
            {
                string newLineChars = 
                    this.ConvertMode == LineEnding.Cr ? "\r" :
                        this.ConvertMode == LineEnding.Lf ? "\n" :
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
                    " -> \"{0}\", {1}", 
                    OutputFileName, Enum.GetName(typeof(LineEnding), this.ConvertMode.Value).ToLower());
            }

            WriteMessage(sb.ToString());
        }
        #endregion
    }
}
