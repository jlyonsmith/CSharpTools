using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using ToolBelt;

namespace Tools
{
    [CommandLineTitle("Text File Space/Tab Line Fixer Tool")]
    [CommandLineDescription("Text file tab/space reporter and fixer. " +
        "For C# source files, the tool reports on beginning-of-line tabs/spaces. " + 
        "All tabs not at the beginning of a line are replaced with spaces. " + 
        "Spaces/tabs inside C# multi-line strings are ignored.")]
    [CommandLineCopyright("Copyright (c) John Lyon-Smith 2014")]
    public class SpacerTool : ToolBase
    {
        public enum Whitespace
        {
            Mixed,
            M = Mixed,
            Tabs,
            T = Tabs,
            Spaces,
            S = Spaces
        }

        private enum FileType 
        {
            CSharp,
            Other
        }

        private FileType fileType = FileType.Other;

        [CommandLineArgument("help", ShortName="?", Description="Shows this help")]
        public bool ShowUsage { get; set; }
        [CommandLineArgument("mode", ShortName="m", Description="The convert mode. One of mixed, tabs or spaces.  Default is to just display the files current state.",
            Initializer=typeof(SpacerTool), MethodName="ParseConvertMode")]
        public Whitespace? ConvertMode { get; set; }
        [DefaultCommandLineArgument(Description="The input file to analyze and convert", ValueHint="INPUTFILE")]
        public ParsedFilePath InputFileName { get; set; }
        [CommandLineArgument("output", ShortName="o", Description="An optional output file name.  Default is to use the input file.", ValueHint="OUTPUTFILE")]
        public ParsedFilePath OutputFileName { get; set; }
        [CommandLineArgument("tabsize", ShortName="t", Description="The tabsize to assume. Default is 4 spaces.", ValueHint="TABSIZE")]
        public int? TabSize { get; set; }

        public static Whitespace? ParseConvertMode(string arg)
        {
            return (Whitespace?)Enum.Parse(typeof(Whitespace), arg, true);
        }

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

            if (InputFileName.Extension == ".cs")
            {
                fileType = FileType.CSharp;
            }

            if (OutputFileName == null)
            {
                OutputFileName = InputFileName;
            }
            else
            {
                if (!ConvertMode.HasValue)
                {
                    WriteError("Must specify conversion mode with output file");
                    return;
                }
            }

            if (!TabSize.HasValue)
            {
                TabSize = 4;
            }

            List<string> lines = ReadFileLines();
            int beforeTabs;
            int beforeSpaces;

            if (fileType == FileType.CSharp)
                CountCSharpBolSpacesAndTabs(lines, out beforeTabs, out beforeSpaces);
            else
                CountBolSpacesAndTabs(lines, out beforeTabs, out beforeSpaces);

            if (ConvertMode.HasValue)
            {
                if (fileType == FileType.CSharp)
                    CSharpUntabify(lines);
                else
                    Untabify(lines);

                if (this.ConvertMode == Whitespace.Tabs)
                {
                    if (fileType == FileType.CSharp)
                        CSharpTabify(lines);
                    else
                        Tabify(lines);
                }
            }

            StringBuilder sb = new StringBuilder();
            Whitespace ws = (beforeTabs > 0) ? (beforeSpaces > 0 ? Whitespace.Mixed : Whitespace.Tabs) : Whitespace.Spaces;

            sb.AppendFormat("\"{0}\", {1}, {2}", 
                this.InputFileName, fileType == FileType.CSharp ? "c#" : "other", Enum.GetName(typeof(Whitespace), ws).ToLower());

            if (this.ConvertMode.HasValue)
            {
                int afterTabs, afterSpaces;

                if (fileType == FileType.CSharp)
                    CountCSharpBolSpacesAndTabs(lines, out afterTabs, out afterSpaces);
                else
                    CountBolSpacesAndTabs(lines, out afterTabs, out afterSpaces);

                using (StreamWriter writer = new StreamWriter(this.OutputFileName))
                {
                    foreach (var line in lines)
                    {
                        writer.Write(line);
                    }
                }

                sb.AppendFormat(" -> \"{0}\", {1}", this.OutputFileName, afterTabs > 0 ? "tabs" : "spaces");
            }

            WriteMessage(sb.ToString());
        }

        public List<string> ReadFileLines()
        {
            // Read the entire file
            string fileContents = File.ReadAllText(InputFileName);

            // Convert to a list of lines, preserving the end-of-lines
            List<string> lines = new List<string>();
            int s = 0;
            int i = 0;

            while (i < fileContents.Length)
            {
                char c = fileContents[i];
                char c1 = i < fileContents.Length - 1 ? fileContents[i + 1] : '\0';

                if (c == '\r')
                {
                    i++;

                    if (c1 == '\n')
                        i++;
                }
                else if (c == '\n')
                {
                    i++;
                }
                else
                {
                    i++;
                    continue;
                }

                lines.Add(fileContents.Substring(s, i - s));
                s = i;
            }

            if (s != i)
                lines.Add(fileContents.Substring(s, i - s));

            return lines;
        }

        public void CountCSharpBolSpacesAndTabs(List<string> lines, out int numBolTabs, out int numBolSpaces)
        {
            numBolSpaces = 0;
            numBolTabs = 0;
            bool inStringConst = false;

            foreach (string line in lines)
            {
                bool bol = true;

                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    char c1 = i < line.Length - 1 ? line[i + 1] : '\0';

                    if (inStringConst)
                    {
                        if (c == '"' && c1 != '"')
                            inStringConst = false;
                    }
                    else
                    {
                        if (c == '@' && c1 == '"')
                            inStringConst = true;
                    }

                    if (bol && !inStringConst)
                    {
                        if (c == ' ')
                            numBolSpaces++;
                        else if (c == '\t')
                            numBolTabs++;
                        else
                            bol = false;
                    }
                }
            }
        }

        public void CSharpUntabify(List<string> lines)
        {
            // Expand tabs anywhere on a line, but not inside @"..." strings

            StringBuilder sb = new StringBuilder();
            bool inMultiLineString = false;

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                bool inString = false;

                for (int j = 0; j < line.Length; j++)
                {
                    char c_1 = j > 0 ? line[j - 1] : '\0';
                    char c = line[j];
                    char c1 = j < line.Length - 1 ? line[j + 1] : '\0';

                    if (inString)
                    {
                        if (c == '"' && c_1 != '\\')
                            inString = false;
                    }
                    else
                    {
                        if (c == '"')
                            inString = true;
                    }

                    if (inMultiLineString)
                    {
                        if (c == '"' && c1 != '"')
                        {
                            inMultiLineString = false;
                        }
                    }
                    else
                    {
                        if (c == '\t')
                        {
                            // Add spaces to next tabstop
                            int numSpaces = this.TabSize.Value - (sb.Length % this.TabSize.Value);

                            sb.Append(' ', numSpaces);
                            continue;
                        }
                        else if (c == '@' && c1 == '"' && !inString)
                        {
                            sb.Append(c);
                            sb.Append(c1);
                            j++;
                            inMultiLineString = true;
                            continue;
                        }
                    }

                    sb.Append(c);
                }

                lines[i] = sb.ToString();
                sb.Clear();
            }
        }

        public void CSharpTabify(List<string> lines)
        {
            // Insert tabs where there are only spaces between two tab stops, but only at the beginning of lines
            // and not inside @"..." strings

            StringBuilder sb = new StringBuilder();
            bool inStringConst = false;

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                bool beginningOfLine = true;
                int numBolSpaces = 0;

                for (int j = 0; j < line.Length; j++)
                {
                    char c = line[j];

                    if (beginningOfLine && c != ' ')
                    {
                        sb.Append(new string('\t', numBolSpaces / this.TabSize.Value));
                        sb.Append(new string(' ', numBolSpaces % this.TabSize.Value));
                        beginningOfLine = false;
                    }

                    char c1 = j < line.Length - 1 ? line[j + 1] : '\0';

                    if (inStringConst)
                    {
                        if (c == '"' && c1 != '"')
                        {
                            inStringConst = false;
                        }
                    }
                    else
                    {
                        if (beginningOfLine && c == ' ')
                        {
                            // Just count the spaces
                            numBolSpaces++;
                            continue;
                        }
                        else if (c == '@' && c1 == '"')
                        {
                            sb.Append(c);
                            sb.Append(c1);
                            j++;
                            inStringConst = true;
                            continue;
                        }
                    }

                    sb.Append(c);
                }

                lines[i] = sb.ToString();
                sb.Clear();
            }
        }

        public void CountBolSpacesAndTabs(List<string> lines, out int numBolTabs, out int numBolSpaces)
        {
            numBolSpaces = 0;
            numBolTabs = 0;

            foreach (string line in lines)
            {
                bool bol = true;

                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];

                    if (bol)
                    {
                        if (c == ' ')
                            numBolSpaces++;
                        else if (c == '\t')
                            numBolTabs++;
                        else
                            bol = false;
                    }
                }
            }
        }

        public void Untabify(List<string> lines)
        {
            // Expand tabs anywhere on a line
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];

                for (int j = 0; j < line.Length; j++)
                {
                    char c = line[j];

                    if (c == '\t')
                    {
                        // Add spaces to next tabstop
                        int numSpaces = this.TabSize.Value - (sb.Length % this.TabSize.Value);

                        sb.Append(' ', numSpaces);
                        continue;
                    }

                    sb.Append(c);
                }

                lines[i] = sb.ToString();
                sb.Clear();
            }
        }

        public void Tabify(List<string> lines)
        {
            // Insert tabs where there are only spaces between two tab stops, but only at the beginning of lines

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                bool beginningOfLine = true;
                int numBolSpaces = 0;

                for (int j = 0; j < line.Length; j++)
                {
                    char c = line[j];

                    if (beginningOfLine && c != ' ')
                    {
                        sb.Append(new string('\t', numBolSpaces / this.TabSize.Value));
                        sb.Append(new string(' ', numBolSpaces % this.TabSize.Value));
                        beginningOfLine = false;
                    }

                    if (beginningOfLine && c == ' ')
                    {
                        // Just count the spaces
                        numBolSpaces++;
                        continue;
                    }

                    sb.Append(c);
                }

                lines[i] = sb.ToString();
                sb.Clear();
            }
        }
    }
}
