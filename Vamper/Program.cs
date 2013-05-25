using System;

namespace Tools
{
    class Program
    {
        public static int Main(string[] args)
        {
            VamperTool tool = new VamperTool();
            
            try
            {
                tool.ProcessCommandLine(args);
                
                tool.Execute();
                return (tool.HasOutputErrors ? 1 : 0);
            }
            catch (Exception exception)
            {
                Console.WriteLine("error: {0}", exception);
                return 1;
            }
        }
    }
}
