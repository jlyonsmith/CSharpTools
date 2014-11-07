using System;
using System.Collections.Generic;
using System.Text;
using ToolBelt;

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
