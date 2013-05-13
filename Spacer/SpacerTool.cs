using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Tools
{
    public class SpacerTool
    {
        public enum LineStart
        {
            Tabs,
            Spaces
        }

        public bool HasOutputErrors { get; set; }
        public bool ShowUsage;
        public LineStart? FixedLineStarts;
        public string InputFileName;
        public string OutputFileName;

        public SpacerTool()
        {
        }

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
                WriteMessage(@"Makes spaces/tabs consistent at the beginning of text file lines.
    
Usage: mono Spacer.exe ...

Arguments:
          [-o                     Specify different name for output file.
          [-m:MODE]               The conversion mode if conversion is required. 
                                  't' to convert to tabs, 's' to convert to spaces,
                                  Default is to report only.
          [-s]                    The tab size (default is 4)
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

            // Read the entire file and determine all the different line starts
            string fileContents = File.ReadAllText(InputFileName);
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
                    case 'f':
                        string lineStarts = null;
                        if (FixedLineStarts.HasValue)
                            lineStarts = (FixedLineStarts.Value == LineStart.Spaces ? "s" : "t");
                        CheckAndSetArgument(arg, ref lineStarts); 
                        FixedLineStarts = (lineStarts == "s" ? LineStart.Spaces : LineStart.Tabs);
                        break;
                    default:
                        throw new ApplicationException (string.Format ("Unknown argument '{0}'", arg [1]));
                    }
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

