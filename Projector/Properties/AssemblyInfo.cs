using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyTitle("Projector C# Copier")]
[assembly: AssemblyDescription("Makes a copy of a project, changing the core project file names and GUIDs")]
[assembly: AssemblyCompany("John Lyon-Smith")]
[assembly: AssemblyProduct("CSharpTools")]
[assembly: AssemblyCopyright("Copyright (c) 2015, John Lyon-Smith")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("2.2.0.0")]
[assembly: AssemblyFileVersion("2.2.20212.1")]
