using System;
using ToolBelt;

namespace Tools
{
    class Program
    {
        public static int Main(string[] args)
        {
            LindexTool tool = new LindexTool();

            try
            {
                tool.ProcessCommandLine(args);

                tool.Execute();
                return (tool.HasOutputErrors ? 1 : 0);
            }
            catch (Exception e)
            {
                while (e != null)
                {
                    ConsoleUtility.WriteMessage(MessageType.Error, e.Message);  
                    e = e.InnerException;
                }
                return 1;
            }
        }
    }
}
