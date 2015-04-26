using System;
using ToolBelt;
using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

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
		#if DEBUG
		[CommandLineArgument("test", ShortName="t", Description="Write .test files instead of overwriting the originals.")]
		public bool Test { get; set; }
		#endif
        [DefaultCommandLineArgument(Description="The case sensitive project name to swap.  This must be the same name used in NuGet and for the .csproj file name", ValueHint = "PROJECT_NAME")]
        public string ProjectName { get; set; }

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

            Dictionary<ParsedPath, CsprojDocument> saveCsprojs = null;
            ParsedPath slnPath = null;
            SlnDocument slnDocument = null;
            bool failed = false;

            try
            {
                slnPath = GetSlnPath();

                var popperConfigPath = slnPath.Directory.WithFileAndExtension("popper.config");
                var popperConfig = PopperDocument.Parse(popperConfigPath);
                ParsedPath localCsprojPath;

                if (!popperConfig.Projects.TryGetValue(ProjectName, out localCsprojPath))
                {
                    WriteError("'{0}' does not contain a location for project '{1}'", popperConfigPath, ProjectName);
                    return;
                }

				localCsprojPath = localCsprojPath.MakeFullPath(popperConfigPath);

                if (!File.Exists(localCsprojPath))
                {
                    WriteError("The project '{0}' does not exist at location '{1}'", ProjectName, localCsprojPath);
                    return;
                }

				var localCsprojDocument = CsprojDocument.Parse(localCsprojPath);

				var localSlnPath = GetSlnForProject(localCsprojPath);
				var localSlnDocument = SlnDocument.Parse(localSlnPath);

                slnDocument = SlnDocument.Parse(slnPath);

				var existingSlnProject = slnDocument.Projects.FirstOrDefault(p => p.Name == this.ProjectName);
				var swapDirection = (existingSlnProject != null ? SwapDirection.ToNuGet : SwapDirection.ToLocalProject);

                if (swapDirection == SwapDirection.ToLocalProject)
                {
                    WriteMessage("Swapping '{0}' to local project '{1}'", ProjectName, localCsprojPath);
                }
                else
                {
                    WriteMessage("Swapping '{0}' to NuGet", ProjectName);
                }

                saveCsprojs = new Dictionary<ParsedPath, CsprojDocument>();

                foreach (var project in slnDocument.Projects)
                {
					if (project.Name == ProjectName || project.TypeGuid != "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}")
						continue;

					var csprojPath = project.Path.MakeFullPath(slnPath);
                    var csprojDocument = CsprojDocument.Parse(csprojPath);

                    if (swapDirection == SwapDirection.ToLocalProject)
                    {
						var count = csprojDocument.References.RemoveAll(r => r.Name == ProjectName);

						if (count > 0)
						{
							// Add a ProjectReference to this .csproj
							csprojDocument.ProjectReferences.Add(new CsprojProjectReference
							{
								Guid = localCsprojDocument.ProjectGuid,
								Include = localCsprojPath,
								Name = ProjectName
							});
							saveCsprojs[csprojPath] = csprojDocument;
						}
                    }
                    else
                    {
						var count = csprojDocument.ProjectReferences.RemoveAll(r => r.Name == ProjectName);

						if (count > 0)
						{
							var nugetPath = FindNugetLibrary(slnPath, csprojPath);

							csprojDocument.References.Add(new CsprojReference
							{
								Name = ProjectName,
								HintPath = nugetPath
							});
							saveCsprojs[csprojPath] = csprojDocument;
						}
                    }
                }

				if (swapDirection == SwapDirection.ToLocalProject)
				{
					// Add enough configs to enable the project
					foreach (var solutionConfig in slnDocument.SolutionConfigurations)
					{
						slnDocument.ProjectConfigurations.Add(new SlnProjectConfiguration
						{
							Guid = localCsprojDocument.ProjectGuid,
							SolutionConfiguration = solutionConfig,
							ProjectConfiguration = new SlnConfiguration 
							{
								Configuration = solutionConfig.Configuration,
								Platform = localSlnDocument.SolutionConfigurations.Find(c => c.Configuration == solutionConfig.Configuration).Platform
							},
						});
					}

					slnDocument.Projects.Add(new SlnProject
					{
						Name = ProjectName,
						Path = localCsprojPath,
						ProjectGuid = localCsprojDocument.ProjectGuid,
						TypeGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}" // C# project
					});
				}
				else
				{
					slnDocument.ProjectConfigurations.RemoveAll(pc => pc.Guid == localCsprojDocument.ProjectGuid);
					slnDocument.Projects.Remove(existingSlnProject);
				}

            }
            catch (Exception ex)
            {
                failed = true;
                WriteError(ex.Message);
            }


            if (!failed)
            {
				ParsedPath filePath;

                foreach (var pair in saveCsprojs)
                {
					filePath = pair.Key;

					if (Test)
						filePath = filePath.WithExtension(".test" + filePath.Extension);

                    WriteMessage("Writing '{0}'", filePath);
					pair.Value.Save(filePath);
                }

				filePath = slnPath;

				if (Test)
					filePath = filePath.WithExtension(".test" + filePath.Extension);
				
				WriteMessage("Writing '{0}'", filePath);
				slnDocument.Save(filePath);
            }

            WriteMessage("Done");
        }

        #endregion 

        private ParsedPath FindNugetLibrary(ParsedPath slnPath, ParsedPath csprojPath)
        {
            var projectName = ProjectName;
            var packagesConfigPath = csprojPath.WithFileAndExtension("packages.config");

            if (!File.Exists(packagesConfigPath))
                throw new PopperToolExeception("Cannot find '{0}'".InvariantFormat(packagesConfigPath));

            var doc = XDocument.Parse(File.ReadAllText(packagesConfigPath));
            var element = doc.Root.Elements("package").Where(e => e.Attribute("id").Value == projectName).FirstOrDefault();

            if (element == null)
                throw new PopperToolExeception("Cannot find project '{0}' in '{1}'".InvariantFormat(projectName, packagesConfigPath));

            var s = "packages/{0}.{1}/lib/{2}/{0}.dll".InvariantFormat(
                projectName, element.Attribute("version").Value, element.Attribute("targetFramework").Value);

            return slnPath.VolumeAndDirectory.Append(s , PathType.File);
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

		private ParsedPath GetSlnForProject(ParsedPath csprojPath)
		{
			var files = DirectoryUtility.GetFiles(csprojPath.WithFileAndExtension("*.sln"), SearchScope.RecurseParentDirectories);

			if (files.Count == 0)
				throw new PopperToolExeception("Cannot find .sln for project '{0}'".InvariantFormat(csprojPath));

			return files[0];
		}
    }
}

