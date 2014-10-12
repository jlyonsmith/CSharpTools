using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ToolBelt;

namespace Tools
{
    // TODO: Add ability to rename project files during copy

    public class ProjectorTool : ToolBase
    {
        [CommandLineArgument("help", ShortName="?", Description="Shows this help")]
        public bool ShowUsage { get; set; }
        [CommandLineArgument("sd", Description="Source project directory")]
        public ParsedDirectoryPath SourceDir { get; set; }
        [CommandLineArgument("dd", Description="Destination project directory")]
        public ParsedDirectoryPath DestinationDir { get; set; }
        [CommandLineArgument("sn", Description="Source project name")]
        public string SourceName  { get; set; }
        [CommandLineArgument("dn", Description="Destination project name")]
        public string DestinationName { get; set; }

        private Dictionary<string, string> projGuidMap = new Dictionary<string, string>();
        private Dictionary<string, string> fileNameChanges = new Dictionary<string, string>();
        private List<Regex> excludeDirs = new List<Regex>();
        private List<Regex> excludeFiles = new List<Regex>();
        private List<string> slnFiles = new List<string>();

        #region ITool
        public override void Execute()
        {
            if (ShowUsage)
            {
                WriteMessage(Parser.LogoBanner);
                WriteMessage(Parser.Usage);
                return;
            }

            if (SourceDir == null)
            {
                WriteError("A source directory must be specified");
                return;
            }

            if (!Directory.Exists(SourceDir))
            {
                WriteError("A source directory {0} does not exist", SourceDir);
                return;
            }

            if (!Directory.Exists(Path.Combine(SourceDir, ".git")))
            {
                WriteError("A .git directory was not found in {0}", SourceDir);
                return;
            }
            
            if (DestinationDir == null)
            {
                WriteError("A destination directory must be specified");
                return;
            }
            
            if (Directory.Exists(DestinationDir))
            {
                WriteError("Destination directory {0} already exists!", DestinationDir);
                return;
            }

            if (String.IsNullOrEmpty(SourceName))
            {
                WriteError("A source name is required");
                return;
            }

            if (String.IsNullOrEmpty(DestinationName))
            {
                WriteError("A destination name is required");
                return;
            }

            excludeDirs.Add(WildcardToRegex("*/.git"));

            ExcludeGitignore();
            ExcludeGitSubmodules();

            CopyDirectory(SourceDir, DestinationDir);

            ChangeSlnGuidsAndNames();
        }

        #endregion 

        private static Regex WildcardToRegex(string pattern)
        {
            return new Regex("^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$"); 
        }

        private bool IsExcludedDir(string dir)
        {
            foreach (var excludeDir in excludeDirs)
            {
                if (excludeDir.IsMatch(dir))
                    return true;
            }

            return false;
        }

        private bool IsExcludedFile(string file)
        {
            foreach (var excludeFile in excludeFiles)
            {
                if (excludeFile.IsMatch(file))
                    return true;
            }

            return false;
        }

        private void ExcludeGitSubmodules()
        {
            var file = Path.Combine(SourceDir, ".gitmodules");

            if (!File.Exists(file))
                return;

            var contents = File.ReadAllText(file);
            MatchCollection matches = Regex.Matches(contents, @"(?<=path = ).*");

            foreach (var match in matches)
                excludeDirs.Add(WildcardToRegex("*/" + ((Match)match).Groups[0].Value));
        }

        private void ExcludeGitignore()
        {
            var file = Path.Combine(SourceDir, ".gitignore");

            if (!File.Exists(file))
                return;

            var lines = File.ReadAllLines(file);

            foreach (var line in lines)
            {
                string pattern = line.Trim();

                if (pattern.Length == 0 || pattern.StartsWith("#"))
                    continue;

                if (pattern.EndsWith("/"))
                    excludeDirs.Add(WildcardToRegex("*/" + pattern.TrimEnd('/')));
                else if (pattern.StartsWith("."))
                    excludeFiles.Add(WildcardToRegex("*" + pattern));
                else
                    excludeFiles.Add(WildcardToRegex("*/" + pattern));
            }
        }

        private void ChangeSlnGuidsAndNames()
        {
            foreach (var file in slnFiles)
            {
                string contents = File.ReadAllText(file);

                foreach (var pair in projGuidMap)
                {
                    contents = contents.Replace(pair.Key, pair.Value);
                }

                foreach (var pair in fileNameChanges)
                {
                    contents = contents.Replace(pair.Key, pair.Value);
                }

                File.WriteAllText(file, contents);
            }
        }

        private void ChangeCsprojGuids(string file)
        {
            string contents = File.ReadAllText(file);

            contents = Regex.Replace(
                contents, @"(?<=\<ProjectGuid\>)(.*?)(?=\</ProjectGuid\>)", m =>
            {
                string guid = m.Groups[1].Value;
                string newGuid;

                if (projGuidMap.ContainsKey(guid))
                {
                    newGuid = projGuidMap[guid];
                }
                else
                {   
                    newGuid = Guid.NewGuid().ToString("B");
                    projGuidMap.Add(guid, newGuid);
                }
                return newGuid;
            });

            File.WriteAllText(file, contents);
        }

        private string ChangeFileName(string path, bool record = false)
        {
            string ext = Path.GetExtension(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string dirName = Path.GetDirectoryName(path);
            string newFileName = Regex.Replace(fileName, this.SourceName, this.DestinationName);

            if (record && !fileNameChanges.ContainsKey(fileName))
            {
                fileNameChanges[fileName] = newFileName;
            }

            return Path.Combine(dirName, newFileName + ext);
        }

        private void CopyDirectory(string sourcePath, string destPath)
        {
            if (IsExcludedDir(destPath))
            {
                WriteMessage("Excluded dir {0}", destPath);
                return;
            }

            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            foreach (string file in Directory.GetFiles(sourcePath))
            {
                string dest = Path.Combine(destPath, Path.GetFileName(file));

                if (IsExcludedFile(dest))
                {
                    WriteMessage("Excluded file {0}", sourcePath);
                    continue;
                }

                string ext = Path.GetExtension(file);

                if (ext == ".sln")
                {
                    dest = ChangeFileName(dest);
                    slnFiles.Add(dest);
                }
                else if (ext == ".csproj")
                {
                    dest = ChangeFileName(dest, record: true);
                }

                WriteMessage("{0} -> {1}", file, dest);
                File.Copy(file, dest);

                if (ext == ".csproj")
                {
                    ChangeCsprojGuids(dest);
                }
            }

            foreach (string folder in Directory.GetDirectories(sourcePath))
            {
                string dest = Path.Combine(destPath, Path.GetFileName(folder));

                CopyDirectory(folder, dest);
            }
        }
    }
}

