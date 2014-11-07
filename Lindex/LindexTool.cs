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
    [CommandLineTitle("Lindex C# Line Indexer")]
    [CommandLineDescription("Creates an index of start offset of lines in a text file")]
    [CommandLineCopyright("Copyright (c) John Lyon-Smith 2014")]
    public class LindexTool : ToolBase
    {
        [CommandLineArgument("help", ShortName="?", Description="Shows this help")]
        public bool ShowUsage { get; set; }
        [DefaultCommandLineArgument(Description="Input file to index.", ValueHint="INPUTFILE")]
        public string InputFile { get; set; }

        public override void Execute()
        {
            if (ShowUsage)
            {
                WriteMessage(this.Parser.LogoBanner);
                WriteMessage(this.Parser.Usage);
                return;
            }
            
            if (String.IsNullOrEmpty(InputFile))
            {
                WriteError("No input text file specified");
                return;
            }

            string outputFile = InputFile + ".lines";

            using (StreamReader reader = new StreamReader(InputFile))
            {
                using (BinaryWriter writer = new BinaryWriter(new FileStream(outputFile, FileMode.Create)))
                {
                    long offset = 0;
                    int c;

                    writer.Write(offset);

                    while (true)
                    {
                        c = reader.Read();
                        if (c == -1)
                            break;
                        offset++;

                        if (c == (int)'\r')
                        {
                            if (reader.Peek() == (int)'\n')
                            {
                                c = reader.Read();
                                if (c == -1)
                                    break;
                                offset++;
                            }

                            writer.Write(offset);
                        }
                        else if (c == (int)'\n')
                        {
                            writer.Write(offset);
                        }
                    }
                }
            }
        }
    }
}
