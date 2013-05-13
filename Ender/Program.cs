using System;

namespace Tools
{
	class Program
	{
		public static int Main(string[] args)
		{
			EnderTool tool = new EnderTool();
			
			try
			{
				tool.ProcessCommandLine(args);
				
				tool.Execute();
				return (tool.HasOutputErrors ? 1 : 0);
			}
			catch (Exception exception)
			{
				Console.WriteLine("Error: Exception: {0}", exception.Message);
				return 1;
			}
		}
	}
}
