using System;

namespace Tools
{
    class Program
    {
        public static int Main(string[] args)
        {
            SpacerTool tool = new SpacerTool();

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
