using System;
using System.IO;
using System.Collections.Generic;
using ToolBelt;
using System.Text.RegularExpressions;

namespace Tools
{
	public sealed class SlnProject
	{
		public string TypeGuid { get; set; }
		public string Name { get; set; }
		public ParsedPath Path { get; set; }
		public string ProjectGuid { get; set; }
		public string Content { get; set; }
	}

	public sealed class SlnGlobalSection
	{
		public string Name { get; set; }
		public string Order { get; set; }
		public string Content { get; set; }
	}

	public sealed class SlnConfiguration
	{
		public string Configuration { get; set; }
		public string Platform { get; set; }
	}

	public sealed class SlnProjectConfiguration
	{
		public string Guid { get; set; }
		public SlnConfiguration ProjectConfiguration { get; set; }
		public SlnConfiguration SolutionConfiguration { get; set; }
	}

    public sealed class SlnDocument
    {
		List<SlnProject> projects;
		List<SlnGlobalSection> globalSections;
		List<SlnConfiguration> solutionConfigs;
		List<SlnProjectConfiguration> projectConfigs;

		public List<SlnProject> Projects { get { return projects; } }
		public List<SlnConfiguration> SolutionConfigurations { get { return solutionConfigs; } }
		public List<SlnProjectConfiguration> ProjectConfigurations { get { return projectConfigs; } }

		private SlnDocument() {}

		private SlnDocument(ParsedPath filePath, string content)
        {
			projects = new List<SlnProject>();
			globalSections = new List<SlnGlobalSection>();
			solutionConfigs = new List<SlnConfiguration>();
			projectConfigs = new List<SlnProjectConfiguration>();

			Regex slnProjectRegex = new Regex("Project\\(\"(?'type'{.*?})\"\\) = \"(?'name'.*?)\", \"(?'path'.*?)\", \"(?'guid'{.*?})\"[\\t\\r ]*\\n(?'content'(.*\\n)*?)EndProject[\\t\\r ]*\\n", 
				RegexOptions.Multiline | RegexOptions.ExplicitCapture);
			MatchCollection matches = slnProjectRegex.Matches(content);

			foreach (Match match in matches)
			{
				this.projects.Add(new SlnProject 
				{
					TypeGuid = match.Groups["type"].Value,
					Name = match.Groups["name"].Value,
					Path = new ParsedFilePath(match.Groups["path"].Value).MakeFullPath(filePath),
					ProjectGuid = match.Groups["guid"].Value,
					Content = match.Groups["content"].Value
				});
			}

			Regex slnGlobalSectionRegex = new Regex("[\\t ]*GlobalSection\\((?'name'.*?)\\) = (?'order'(pre|post)Solution)[\\t\\r ]*\\n(?'content'([\\t ]*.*[\\t ]*\\n)+?)[\\t ]*EndGlobalSection[\\t\\r ]*\\n", 
				RegexOptions.Multiline | RegexOptions.ExplicitCapture);

			matches = slnGlobalSectionRegex.Matches(content);

			foreach (Match match in matches)
			{
				var name = match.Groups["name"].Value;
				var groupContent = match.Groups["content"].Value;

				if (name == "SolutionConfigurationPlatforms")
				{
					ParseSolutionConfigurationPlatforms(groupContent);
				}
				else if (name == "ProjectConfigurationPlatforms")
				{
					ParseProjectConfigurationPlatforms(groupContent);
				}
				else
				{
					this.globalSections.Add(new SlnGlobalSection
					{
						Name = name,
						Order = match.Groups["order"].Value,
						Content = groupContent
					});
				}
			}
        }

		private void ParseSolutionConfigurationPlatforms(string content)
		{
			Regex slnCfgPlatRegex = new Regex("[\\t ]*(?'config'.*?)\\|(?'platform'.*?) = .*\\n", 
				RegexOptions.Multiline | RegexOptions.ExplicitCapture);
			var matches = slnCfgPlatRegex.Matches(content);

			foreach (Match match in matches)
			{
				solutionConfigs.Add(new SlnConfiguration
				{
					Configuration = match.Groups["config"].Value,
					Platform = match.Groups["platform"].Value
				});
			}
		}

		private void ParseProjectConfigurationPlatforms(string content)
		{
			Regex slnCfgPlatRegex = new Regex("^[\\t ]*(?'guid'{.*?})\\.(?'slnConfig'.*?)\\|(?'slnPlatform'.*?)\\.(?'tag'.*?) = (?'prjConfig'.*?)\\|(?'prjPlatform'.*?)[\\t\\r ]*\\n", 
				RegexOptions.Multiline | RegexOptions.ExplicitCapture);
			var matches = slnCfgPlatRegex.Matches(content);

			foreach (Match match in matches)
			{
				if (match.Groups["tag"].Value == "ActiveCfg")
				{
					projectConfigs.Add(new SlnProjectConfiguration
					{
						Guid = match.Groups["guid"].Value,
						SolutionConfiguration = new SlnConfiguration
						{
							Configuration = match.Groups["slnConfig"].Value,
							Platform = match.Groups["slnPlatform"].Value
						},
						ProjectConfiguration = new SlnConfiguration
						{
							Configuration = match.Groups["prjConfig"].Value,
							Platform = match.Groups["prjPlatform"].Value
						}
					});
				}
			}
		}

		public void Save(ParsedPath fileName)
		{
			using (var writer = new StreamWriter(fileName))
			{
				writer.Write("Microsoft Visual Studio Solution File, Format Version 12.00\r\n");
				writer.Write("# Visual Studio 2012\r\n");

				var csprojGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";

				foreach (var project in this.projects)
				{
					writer.Write("Project(\"{0}\") = \"{1}\", \"{2}\", \"{3}\"\r\n{4}EndProject\r\n",
						csprojGuid, project.Name, project.Path.MakeRelativePath(fileName).ToString("\\"), project.ProjectGuid, project.Content);
				}

				writer.Write("Global\r\n");

				writer.Write("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution\r\n");
				foreach (var solutionConfig in this.solutionConfigs)
				{
					writer.Write("\t\t{0}|{1} = {0}|{1}\r\n", solutionConfig.Configuration, solutionConfig.Platform);
				}
				writer.Write("\tEndGlobalSection\r\n");

				writer.Write("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution\r\n");
				foreach (var projectConfig in this.projectConfigs)
				{
					writer.Write("\t\t{0}.{1}|{2}.ActiveCfg = {3}|{4}\r\n",
						projectConfig.Guid, 
						projectConfig.SolutionConfiguration.Configuration,
						projectConfig.SolutionConfiguration.Platform,
						projectConfig.ProjectConfiguration.Configuration,
						projectConfig.ProjectConfiguration.Platform);
					writer.Write("\t\t{0}.{1}|{2}.Build.0 = {3}|{4}\r\n",
						projectConfig.Guid, 
						projectConfig.SolutionConfiguration.Configuration,
						projectConfig.SolutionConfiguration.Platform,
						projectConfig.ProjectConfiguration.Configuration,
						projectConfig.ProjectConfiguration.Platform);
				}
				writer.Write("\tEndGlobalSection\r\n");

				foreach (var globalSection in this.globalSections)
				{
					writer.Write("\tGlobalSection({0})\r\n", globalSection.Name);
					writer.Write(globalSection.Content);
					writer.Write("\tEndGlobalSection\r\n");
				}

				writer.Write("Global\r\n");
			}
		}

		public static SlnDocument Parse(ParsedPath filePath)
		{
			return new SlnDocument(filePath, File.ReadAllText(filePath));
		}
    }
}
