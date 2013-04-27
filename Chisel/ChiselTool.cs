using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;

namespace Chisel
{
    public class ChiselTool
    {
        public enum ConvertMode
        {
            None,
            TooTabs,
            TooSpaces
        }

        public bool HasOutputErrors { get; set; }

        public bool ShowUsage { get; set; }

        public bool NoLogo { get; set; }

        public ChiselTool()
        {
        }

        public void Execute()
        {
            if (!NoLogo) 
            {
                string version = 
                    ((AssemblyFileVersionAttribute)Assembly
                     .GetExecutingAssembly()
                     .GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true)[0]).Version;

                WriteMessage("Chisel Space/Tab Fixer. Version {0}", version);
                WriteMessage ("Copyright (c) 2013, John Lyon-Smith." + Environment.NewLine);
            }

            if (ShowUsage) 
            {
                WriteMessage(@"Fixes spaces/tabs at the beginning of text files.
    
Usage: mono Chisel.exe ...

Arguments:
          [-q]                    Suppress logo.
          [-o                     Specify different name for output file.
          [-m:MODE]               The conversion mode if conversion is required. 
                                  '2t' to convert to tabs, '2s' to convert to spaces,
                                  Default is to report only.
          [-s]                    The tab size (default is 4)
          [-h] or [-?]            Show help.
");
                return;
            }

            StreamWriter writer = null;
            try 
            {
            } 
            finally 
            {
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
                    case 'q':
                        NoLogo = true;
                        continue;
                    default:
                        throw new ApplicationException (string.Format ("Unknown argument '{0}'", arg [1]));
                    }
                }
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

