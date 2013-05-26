using System;
using System.IO;

public class Program
{
    public static void Main(string[] args)
    {
        File.WriteAllText(
            "test.txt", 
            "    a\n" +
            "\n" + 
            "\tb\n" + 
            " \t   c = @\"1\"; c1 = @\"2\"\n" + 
            "  d; d1\t; d2\n" + 
            "\t  e\n" + 
            "\t@\"123\"\n" + 
            "    @\"1\n" + 
            "\t1\n" + 
            "    2\"\n" + 
            "f\n" +
            "\n" +
            "\tg\n" +
            "\n");
    }
}
