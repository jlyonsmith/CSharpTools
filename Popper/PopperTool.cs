using System;
using ToolBelt;
using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tools
{
    public class PopperToolExeception : Exception
    {
        public PopperToolExeception(string message) : base(message)
        {
        }

        public PopperToolExeception(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    enum SwapDirection
    {
        ToNuGet,
        ToLocalProject
    }

    public class PopperTool : ToolBase
    {
        [CommandLineArgument("help", ShortName="?", Description="Shows this help")]
        public bool ShowUsage { get; set; }
        [CommandLineArgument("slndir", ShortName="s", Description="Specify solution directory.  Defaults to first parent directory containing .sln file.")]
        public ParsedDirectoryPath SlnDir { get; set; }
        [DefaultCommandLineArgument(Description="The case sensitive project name to swap.  This must be the same name used in NuGet and for the .csproj file name", ValueHint = "PROJECT_NAME")]
        public string ProjectName { get; set; }

        Regex slnProjectRegex = new Regex("Project\\(\".*?\"\\) = \"(?'name'.*?)\", \"(?'path'.*?)\", \"(?'guid'.*?)\"\r\nEndProject\r\n", 
            RegexOptions.Multiline | RegexOptions.ExplicitCapture);
        Regex csprojReferenceRegex = new Regex("\\s*<Reference Include=\"(?'name'.*?)\">\r\n\\s*<HintPath>(?'path'.*?)</HintPath>\r\n\\s*</Reference>\r\n", 
            RegexOptions.Multiline | RegexOptions.ExplicitCapture);

        #region ITool
        public override void Execute()
        {
            if (ShowUsage)
            {
                WriteMessage(Parser.LogoBanner);
                WriteMessage(Parser.Usage);
                return;
            }

            if (String.IsNullOrEmpty(ProjectName))
            {
                WriteError("A project name must be given");
                return;
            }

            try
            {
                var slnPath = GetSlnPath();
                var popperConfigPath = slnPath.Directory.WithFileAndExtension("popper.config");
                var popperConfig = ReadPopperConfig(popperConfigPath);
                ParsedPath localCsproj;

                if (!popperConfig.TryGetValue(ProjectName, out localCsproj))
                {
                    WriteError("'{0}' does not contain a location for project '{1}'", popperConfigPath, ProjectName);
                    return;
                }

                if (!File.Exists(localCsproj))
                {
                    WriteWarning("The project '{0}' does not exist at location '{1}'", ProjectName, localCsproj);
                }

                var slnContents = File.ReadAllText(slnPath);
                var slnProjects = GetProjectsListedInSln(slnPath, slnContents);
                var swapDirection = slnProjects.ContainsKey(this.ProjectName) ? SwapDirection.ToNuGet : SwapDirection.ToLocalProject;

                if (swapDirection == SwapDirection.ToLocalProject)
                    WriteMessage("Swapping '{0}' to local project '{1}'", ProjectName, localCsproj);
                else
                    WriteMessage("Swapping '{0}' to NuGet");

                foreach (var pair in slnProjects)
                {
                    var csprojFile = pair.Value;
                    var csprojContents = File.ReadAllText(csprojFile);

                    if (swapDirection == SwapDirection.ToLocalProject)
                    {
                        RemoveNuGetReference(csprojContents);
                        AddLocalProjectReference(csprojContents);
                        AddGlobalSectionEntries(slnContents);
                    }
                    else
                    {
                        RemoveGlobalSectionEntries(slnContents);
                        RemoveLocalProjectReference(csprojContents);
                        AddNuGetReference(csprojFile, csprojContents);
                    }

                    File.WriteAllText(csprojFile, csprojContents);
                }

                File.WriteAllText(slnPath, slnContents);
            }
            catch (Exception ex)
            {
                WriteError(ex.Message);
            }
        }

        #endregion 

        private void RemoveNuGetReference(string csprojContents)
        {
            var name = ProjectName;

            csprojReferenceRegex.Replace(csprojContents, m => 
            {
                if (m.Groups["name"].Value == name)
                    return "";
                else
                    return m.Groups[0];
            });
        }

        private void AddNuGetReference(ParsedPath csprojFile, string csprojContents)
        {
        }

        private void AddLocalProjectReference(string csprojContents)
        {
        }

        private void RemoveLocalProjectReference(string csprojContents)
        {
        }

        private void AddGlobalSectionEntries(string slnContents)
        {
        }

        private void RemoveGlobalSectionEntries(string slnContents)
        {
        }

        private Dictionary<string, ParsedPath> GetProjectsListedInSln(ParsedPath slnPath, string slnContents)
        {
            var matches = slnProjectRegex.Matches(slnContents);
            var projects = new Dictionary<string, ParsedPath>();

            foreach (Match match in matches)
            {
                projects.Add(match.Groups["name"].Value, new ParsedPath(match.Groups["path"].Value, PathType.File).MakeFullPath(slnPath));
            }

            return projects;
        }

        private Dictionary<string, ParsedPath> ReadPopperConfig(ParsedPath popperConfigPath)
        {
            if (!File.Exists(popperConfigPath))
                throw new PopperToolExeception("'{0}' file not found.  Stopping.".CultureFormat(popperConfigPath));

            var doc = XDocument.Parse(File.ReadAllText(popperConfigPath));

            try
            {
                return doc.Root.Elements("Project").ToDictionary(x => x.Attribute("Name").Value, x =>
                {
                    return new ParsedPath(StringUtility.ReplaceTags(x.Attribute("ProjectFile").Value, "$(", ")", 
                        Environment.GetEnvironmentVariables(), TaggedStringOptions.ThrowOnUnknownTags), PathType.File);
                });
            }
            catch (Exception ex)
            {
                throw new PopperToolExeception("Unable to read configuration file", ex);
            }
        }

        private ParsedPath GetSlnPath()
        {
            IList<ParsedPath> files = null;

            if (!String.IsNullOrEmpty(SlnDir))
            {
                var spec = SlnDir.WithFileAndExtension("*.sln").MakeFullPath();

                files = DirectoryUtility.GetFiles(spec, SearchScope.DirectoryOnly);

                if (files.Count == 0)
                    throw new PopperToolExeception("Directory '{0}' is not a .sln directory".InvariantFormat(SlnDir));
            }
            else
            {
                files = DirectoryUtility.GetFiles(new ParsedFilePath("*.sln"), SearchScope.RecurseParentDirectories);

                if (files.Count == 0)
                    throw new PopperToolExeception("Unable to find a .sln directory");
            }

            return files[0];
        }
    }
}

