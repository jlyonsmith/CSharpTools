using System;
using ToolBelt;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Tools
{
	public class CsprojReference
	{
		public string Name { get; set; }
		public ParsedPath HintPath { get; set; }
	}

	public class CsprojProjectReference
	{
		public string Name { get; set; }
		public ParsedPath Include { get; set; }
		public string Guid { get; set; }
	}

    public class CsprojDocument
    {
		XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
		XDocument xdoc;
		XElement refItemGroup;
		XElement projRefItemGroup;
		string projectGuid;
		List<CsprojReference> refs;
		List<CsprojProjectReference> projRefs;

		public List<CsprojReference> References { get { return refs; } }
		public List<CsprojProjectReference> ProjectReferences { get { return projRefs; } }
		public string ProjectGuid { get { return projectGuid; } }

        private CsprojDocument() { }

		private CsprojDocument(ParsedPath csprojPath, string content)
		{
			refs = new List<CsprojReference>();
			projRefs = new List<CsprojProjectReference>();

			this.xdoc = XDocument.Parse(content);

			XElement firstItemGroup;
			var refElems = GetElements(ns + "Reference", out firstItemGroup);

			this.refItemGroup = firstItemGroup;

			foreach (var refElem in refElems)
			{
				var hintPath = refElem.Attribute("HintPath");

				refs.Add(new CsprojReference 
				{ 
					Name = refElem.Attribute("Include").Value, 
					HintPath = hintPath != null ? new ParsedPath(hintPath.Value, PathType.File).MakeFullPath(csprojPath) : null 
				});
			}

			refElems.Remove();

			var projRefElems = GetElements(ns + "ProjectReference", out firstItemGroup);

			this.projRefItemGroup = firstItemGroup;

			foreach (var projRefElem in xdoc.Descendants(ns + "ProjectReference"))
			{
				projRefs.Add(new CsprojProjectReference
				{
					Include = new ParsedPath(projRefElem.Attribute("Include").Value, PathType.File).MakeFullPath(csprojPath),
					Guid = projRefElem.Element(ns + "Project").Value,
					Name = projRefElem.Element(ns + "Name").Value
				});
			}

			projRefElems.Remove();

			this.projectGuid = xdoc.Descendants(ns + "ProjectGuid").First().Value;
		}

		private IEnumerable<XElement> GetElements(XName name, out XElement parent)
		{
			var elems = xdoc.Descendants(name);
			var elem = elems.FirstOrDefault();

			if (elem != null)
			{
				parent = elem.Parent;
			}
			else
			{
				parent = new XElement(name.Namespace + "ItemGroup");

				xdoc.Descendants(name.Namespace + "PropertyGroup").Last().AddAfterSelf(parent);
			}

			return elems;
		}

		public void Save(ParsedPath csprojPath)
		{
			this.refItemGroup.Add(this.refs.Select(r => 
			{
				var elem = new XElement(
					ns + "Reference", 
					new XAttribute("Include", r.Name));

				if (r.HintPath != null)
				{
					elem.Add(new XElement(ns + "HintPath", r.HintPath.MakeRelativePath(csprojPath).ToString("\\")));
				}

				return elem;
			}));

			this.projRefItemGroup.Add(this.projRefs.Select(pr => 
			{
				return new XElement(
					ns + "ProjectReference",
					new XAttribute("Include", pr.Include.MakeRelativePath(csprojPath).ToString("\\")),
					new XElement(ns + "Project", pr.Guid),
					new XElement(ns + "Name", pr.Name)
				);
			}));

			// Remove empty ItemGroups
			var emptyItemGroups = xdoc.Descendants(ns + "ItemGroup").Where(e => !e.HasElements);

			if (emptyItemGroups.Count() > 0)
			{
				emptyItemGroups.Remove();
			}

			xdoc.Save(csprojPath);
		}

		public static CsprojDocument Parse(ParsedPath filePath)
		{
			return new CsprojDocument(filePath, File.ReadAllText(filePath));
		}
    }
}

