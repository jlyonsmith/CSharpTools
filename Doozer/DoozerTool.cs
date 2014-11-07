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
    [CommandLineTitle("Doozer C# Project TODO Scraper")]
    [CommandLineDescription("Finds all the //TODO comments in a tree of C# projects")]
    [CommandLineCopyright("Copyright (c) John Lyon-Smith 2014")]
    public class DoozerTool : ToolBase
    {
        private Regex todoRegex = new Regex(@"// *(TODO*.*$)", RegexOptions.Singleline);

        [CommandLineArgument("help", ShortName="?", Description="Shows this help")]
        public bool ShowUsage { get; set; }
        [DefaultCommandLineArgument(Description="Directory to scan. Default is the current directory.", ValueHint="ROOTDIR")]
        public ParsedDirectoryPath RootDir { get; set; }

        public override void Execute()
        {
            if (ShowUsage)
            {
                WriteMessage(this.Parser.LogoBanner);
                WriteMessage(this.Parser.Usage);
                return;
            }

            if (RootDir == null)
            {
                RootDir = new ParsedDirectoryPath(Environment.CurrentDirectory);
            }

            ProcessDirectory(RootDir);
        }

        private void ProcessDirectory(ParsedPath dir)
        {
            var files = DirectoryUtility.GetFiles(dir, SearchScope.DirectoryOnly);

            foreach (var file in files)
            {
                if (file.Extension == ".cs")
                    ScanFile(file);
            }

            var dirs = DirectoryUtility.GetDirectories(dir, SearchScope.DirectoryOnly);

            foreach (var subDir in dirs)
            {
                ProcessDirectory(subDir);
            }
        }

        private void ScanFile(string fileName)
        {
            using (StreamReader reader = new StreamReader(fileName))
            {
                string line;
                int lineNum = 1;

                while ((line = reader.ReadLine()) != null)
                {
                    Match m = todoRegex.Match(line);

                    if (m.Success)
                    {
                        WriteMessage("{0}({1}): {2}", fileName, lineNum, m.Groups[0].Value);
                    }

                    lineNum++;
                }
            }
        }
    }
}
