using System;
using System.Collections.Generic;
using ToolBelt;
using System.IO;
using System.Xml.Linq;
using System.Linq;

namespace Tools
{
    public class PopperDocument
    {
		Dictionary<string, ParsedPath> projects = new Dictionary<string, ParsedPath>();

		public Dictionary<string, ParsedPath> Projects { get { return projects; } }

        private PopperDocument()
        {
        }

		private PopperDocument(string content)
		{
			var doc = XDocument.Parse(content);

			this.projects = doc.Root.Elements("Project").ToDictionary(x => x.Attribute("Name").Value, x =>
			{
				return new ParsedPath(StringUtility.ReplaceTags(x.Attribute("ProjectFile").Value, "$(", ")", 
					Environment.GetEnvironmentVariables(), TaggedStringOptions.ThrowOnUnknownTags), PathType.File);
			});
		}

		public static PopperDocument Parse(string fileName)
		{
			if (!File.Exists(fileName))
				throw new FileNotFoundException("'{0}' file not found".CultureFormat(fileName));

			return new PopperDocument(File.ReadAllText(fileName));
		}
    }
}

