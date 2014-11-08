using System;
using System.IO;

public class Program
{
    public static void Main(string[] args)
    {
        File.WriteAllText(
            "Test.cs", 
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
            "    \"@\"\n" +
            "\tg\n" +
            "\n");

        File.WriteAllText(
            "Test.tson", 
            "a:\n" +
            "{\n" + 
            "\tb: 1,\n" + 
            "  c: 2\n" + 
            " \t d:\t3\n" + 
            "\t\n" + 
            "\n");
    }
}
