using System;
using ToolBelt;
using System.IO;

namespace Tools
{
    public class CleanerTool : ToolBase
    {
        [CommandLineArgument("help", ShortName="?", Description="Shows this help")]
        public bool ShowUsage { get; set; }
        [CommandLineArgument("delete", ShortName="d", Description="Actually perform the deletions")]
        public bool Delete { get; set; }

        #region ITool
        public override void Execute()
        {
            if (ShowUsage)
            {
                WriteMessage(Parser.LogoBanner);
                WriteMessage(Parser.Usage);
                return;
            }

            var slnDirs = DirectoryUtility.GetFiles(new ParsedPath("*.sln", PathType.File), SearchScope.RecurseParentDirectories);

            if (slnDirs.Count == 0)
            {
                WriteError("Cannot find solution directory");
                return;
            }

            var slnDir = slnDirs[0];

            WriteMessage("Found solution directory '{0}'", slnDir);

            if (Delete)
                WriteWarning("Deleted the following directories:");

            var csProjs = DirectoryUtility.GetFiles(slnDir.WithFileAndExtension("*.csproj"), SearchScope.RecurseSubDirectoriesBreadthFirst);

            foreach (var csProj in csProjs)
            {
                // HACK: The correct thing to do is open each .csproj and extract the <OutputPath>...</OutputPath>

                var binDirs = DirectoryUtility.GetDirectories(csProj.Directory.Append("bin", PathType.File), SearchScope.DirectoryOnly);

                if (binDirs.Count > 0)
                {
                    WriteMessage(binDirs[0].ToString());

                    if (Delete)
                        Directory.Delete(binDirs[0], true);
                }

                var objDirs = DirectoryUtility.GetDirectories(csProj.Directory.Append("obj", PathType.File), SearchScope.DirectoryOnly);

                if (objDirs.Count > 0)
                {
                    WriteMessage(objDirs[0].ToString());

                    if (Delete)
                        Directory.Delete(objDirs[0], true);
                }
            }
        }

        #endregion
    }
}

