using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyTitle("Doozer C# Project TODO Scraper")]
[assembly: AssemblyDescription("Finds all the //TODO comments in a tree of C# projects")]
[assembly: AssemblyCompany("${Company}")]
[assembly: AssemblyProduct("${Product}")]
[assembly: AssemblyCopyright("${Copyright}")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("2.2.0.0")]
[assembly: AssemblyFileVersion("2.2.20212.0")]
