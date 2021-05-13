using System.Collections.Generic;
using System.Drawing;
using ConsoleTables;
using Console = Colorful.Console;

namespace FirmwareArchiver.CustomComponents
{
    public static class MyConsole
    {
        public static void Success(string message = "")
        {
            Console.WriteLine(message, Color.LightGreen);
        }

        public static void Fail(string message = "")
        {
            Console.WriteLine(message, Color.PaleVioletRed);
        }

        public static void Info(string message = "")
        {
            Console.WriteLine(message, Color.LightGoldenrodYellow);
        }

        public static void ASCII(string message = "", Color? color = null)
        {
            Console.WriteAscii(message, color ?? Color.LightSkyBlue);
        }

        public static void Exception(string message = "")
        {
            Console.WriteLine(message, Color.Red);
        }

        public static void UserInteraction(string message = "")
        {
            Console.WriteLine(message, Color.LightSkyBlue);
        }

        public static void Gradient(string message = "")
        {
            Console.WriteWithGradient(message.ToCharArray(), Color.Yellow, Color.Fuchsia, 8);
        }

        public static void Table(string[] header, List<string[]> rows)
        {
            var table = new ConsoleTable(header);
            foreach (var row in rows)
                table.AddRow(row);

            table.Write();
            Console.WriteLine();
        }


    }
}
