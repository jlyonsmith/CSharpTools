using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
	class Program
	{
		static int Main(string[] args)
		{
			StrapperTool tool = new StrapperTool();

			try
			{
				tool.ProcessCommandLine(args);

				tool.Execute();
				return (tool.HasOutputErrors ? 1 : 0);
			}
			catch (Exception exception)
			{
				Console.WriteLine("error: {0}", exception.Message);
				return 1;
			}
		}
	}
}
