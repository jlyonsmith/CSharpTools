using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Tools
{
    // TODO: Add ability to rename project files during copy

    public class ProjectorTool
    {
        public string SourceDir { get; set; }

        public string DestinationDir { get; set; }

        public bool ShowUsage { get; set; }

        public bool HasOutputErrors { get; set; }
        public string SourceName  { get; set; }
        public string DestinationName { get; set; }

        private Dictionary<string, string> projGuidMap = new Dictionary<string, string>();
        private Dictionary<string, string> fileNameChanges = new Dictionary<string, string>();
        private List<Regex> excludeDirs = new List<Regex>();
        private List<Regex> excludeFiles = new List<Regex>();
        private List<string> slnFiles = new List<string>();

        public ProjectorTool()
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
    <src-dir>              Source directory contain C#/Git project tree.
    <dst-dir>              Destination directory for copy with new GUIDs
    [-s:<from-name>]       Source project name
    [-d:<to-name>]         Destination project name
    [-h] or [-?]           Show help.
");
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

            excludeDirs.Add(WildcardToRegex("*/.git"));

            ExcludeGitignore();
            ExcludeGitSubmodules();

            CopyDirectory(SourceDir, DestinationDir);

            ChangeSlnGuidsAndNames();
        }

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
            var contents = File.ReadAllText(file);
            MatchCollection matches = Regex.Matches(contents, @"(?<=path = ).*");

            foreach (var match in matches)
                excludeDirs.Add(WildcardToRegex("*/" + ((Match)match).Groups[0].Value));
        }

        private void ExcludeGitignore()
        {
            var file = Path.Combine(SourceDir, ".gitignore");
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
            if (IsExcludedDir(sourcePath))
                return;

            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            foreach (string file in Directory.GetFiles(sourcePath))
            {
                string dest = Path.Combine(destPath, Path.GetFileName(file));

                if (IsExcludedFile(dest))
                    continue;

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

        public void ProcessCommandLine(string[] args)
        {
            string sourceName = null;
            string destinationName = null; 

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
                    case 's':
                        CheckAndSetArgument(arg, ref sourceName);
                        break;
                    case 'd':
                        CheckAndSetArgument(arg, ref destinationName);
                        break;
                    default:
                        throw new Exception(string.Format("Unknown argument '{0}'", arg[1]));
                    }
                }
                else if (String.IsNullOrEmpty(SourceDir))
                {
                    SourceDir = Path.GetFullPath(arg);
                }
                else if (String.IsNullOrEmpty(DestinationDir))
                {
                    DestinationDir = Path.GetFullPath(arg);
                }
                else
                    throw new Exception(string.Format("Unexpected argument '{0}'", arg));
            }

            this.SourceName = sourceName;
            this.DestinationName = destinationName;
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

        private void WriteMessage(string format, params object[] args)
        {
            Console.WriteLine(format, args);
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
    }
}

