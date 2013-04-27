using System;

namespace Chisel.MacOS
{
	class MainClass
	{
		public static int Main(string[] args)
		{
			ChiselTool tool = new ChiselTool();

			try
			{
				tool.ProcessCommandLine(args);

				tool.Execute();
				return (tool.HasOutputErrors ? 1 : 0);
			}
			catch (Exception exception)
			{
				Console.WriteLine("Error: Exception: {0}", exception);
				return 1;
			}
		}
	}
}
