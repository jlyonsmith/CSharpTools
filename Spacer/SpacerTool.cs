using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;

namespace Tools
{
    public class SpacerTool
    {
        public enum Whitespace
        {
            Mixed,
            Tabs,
            Spaces,
        }

        public bool HasOutputErrors { get; set; }
        public bool ShowUsage;
        public Whitespace? ConvertMode;
        public string InputFileName;
        public string OutputFileName;
        public int TabSize = 4;

        public SpacerTool()
        {
        }

        public void Execute()
        {
            if (ShowUsage) 
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string name = assembly.FullName.Substring(0, assembly.FullName.IndexOf(','));
                object[] attributes = assembly.GetCustomAttributes(true);
                string version = ((AssemblyFileVersionAttribute)attributes.First(x => x is AssemblyFileVersionAttribute)).Version;
                string copyright = ((AssemblyCopyrightAttribute)attributes.First(x => x is AssemblyCopyrightAttribute)).Copyright;
                string title = ((AssemblyTitleAttribute)attributes.First(x => x is AssemblyTitleAttribute)).Title;
                string description = ((AssemblyDescriptionAttribute)attributes.First(x => x is AssemblyDescriptionAttribute)).Description;

                WriteMessage("{0}. Version {1}", title, version);
                WriteMessage("{0}.\n", copyright);
                WriteMessage("{0}\n", description);
                WriteMessage("Usage: mono {0}.exe ...\n", name);
                WriteMessage(@"Arguments:
    [-o:OUTPUTFILE]         Specify different name for output file.
    [-m:MODE]               The conversion mode if conversion is required. 
                            't' to convert to tabs, 's' to convert to spaces,
                            Default is to report only.
    [-s:TABSTOP]            The distance between each tabstop (default is 4)
    [-h] or [-?]            Show help.
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
            else
            {
                if (!ConvertMode.HasValue)
                {
                    WriteError("Must specify conversion mode with output file");
                    return;
                }
            }

            List<string> lines = ReadFileLines();
            int beforeTabs;
            int beforeSpaces;

            CountBolSpacesAndTabs(lines, out beforeTabs, out beforeSpaces);

            if (ConvertMode.HasValue)
            {
                SmartUntabify(lines);

                if (this.ConvertMode == Whitespace.Tabs)
                {
                    SmartTabify(lines);
                }
            }

            StringBuilder sb = new StringBuilder();
            Whitespace ws = (beforeTabs > 0) ? (beforeSpaces > 0 ? Whitespace.Mixed : Whitespace.Tabs) : Whitespace.Spaces;

            sb.AppendFormat("\"{0}\", {1}", this.InputFileName, Enum.GetName(typeof(Whitespace), ws).ToLower());

            if (this.ConvertMode.HasValue)
            {
                int afterTabs, afterSpaces;

                CountBolSpacesAndTabs(lines, out afterTabs, out afterSpaces);

                if (afterTabs != beforeTabs || afterSpaces != beforeSpaces)
                {
                    using (StreamWriter writer = new StreamWriter(this.OutputFileName))
                    {
                        foreach (var line in lines)
                        {
                            writer.Write(line);
                        }
                    }

                    sb.AppendFormat(" -> {0}", afterTabs > 0 ? "tabs" : "spaces");
                }
            }

            WriteMessage(sb.ToString());
        }

        public void CountBolSpacesAndTabs(List<string> lines, out int numBolTabs, out int numBolSpaces)
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

        public void SmartUntabify(List<string> lines)
        {
            // Expand tabs anywhere on a line, but not inside @"..." strings

            StringBuilder sb = new StringBuilder();
            bool inStringConst = false;

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];

                for (int j = 0; j < line.Length; j++)
                {
                    char c = line[j];
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
                        if (c == '\t')
                        {
                            // Add spaces to next tabstop
                            int numSpaces = this.TabSize - (sb.Length % this.TabSize);

                            sb.Append(' ', numSpaces);
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

        public void SmartTabify(List<string> lines)
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
                        sb.Append(new string('\t', numBolSpaces / this.TabSize));
                        sb.Append(new string(' ', numBolSpaces % this.TabSize));
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

        public void ProcessCommandLine (string[] args)
        {
            foreach (var arg in args) {
                if (arg.StartsWith ("-")) {
                    switch (arg [1]) {
                    case 'h':
                    case '?':
                        ShowUsage = true;
                        return;
                    case 'o':
                        CheckAndSetArgument(arg, ref OutputFileName); 
                        continue;
                    case 'm':
                        string lineStarts = null;
                        if (ConvertMode.HasValue)
                            lineStarts = (ConvertMode.Value == Whitespace.Spaces ? "s" : "t");
                        CheckAndSetArgument(arg, ref lineStarts); 
                        ConvertMode = (lineStarts == "s" ? Whitespace.Spaces : Whitespace.Tabs);
                        break;
                    case 's':
                        string tabSize = this.TabSize.ToString();
                        CheckAndSetArgument(arg, ref tabSize);
                        this.TabSize = int.Parse(tabSize);
                        break;
                    default:
                        throw new ApplicationException(string.Format ("Unknown argument '{0}'", arg [1]));
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

        private void WriteError (string format, params object[] args)
        {
            Console.Write("error: ");
            Console.WriteLine (format, args);
            this.HasOutputErrors = true;
        }

        private void WriteWarning (string format, params object[] args)
        {
            Console.Write("warning: ");
            Console.WriteLine(format, args);
            this.HasOutputErrors = true;
        }

        private void WriteMessage(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }
}
