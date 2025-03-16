using System;
using System.Management.Automation;

namespace Punk
{
    // Основной класс, содержащий логику программы
    public class Punk
    {
        public string Path { get; set; }
        public bool Help { get; set; }

        public void Execute()
        {
            if (Help)
            {
                ShowHelp();
                return;
            }

            if (!string.IsNullOrEmpty(Path))
            {
                Console.WriteLine($"Path specified: {Path}");
                // Здесь можно добавить логику обработки пути
            }
            else
            {
                Console.WriteLine("No path specified.");
            }
        }

        private void ShowHelp()
        {
            Console.WriteLine("Usage: Punk -Path <string> [-Help]");
            Console.WriteLine("  -Path: Specifies the path to process.");
            Console.WriteLine("  -Help: Displays this help message.");
        }
    }

    // Класс для работы в режиме CLI
    public class PunkCli
    {
        public static void Main(string[] args)
        {
            var punk = new Punk();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-Path":
                        if (i + 1 < args.Length)
                        {
                            punk.Path = args[i + 1];
                            i++;
                        }
                        break;
                    case "-Help":
                        punk.Help = true;
                        break;
                }
            }

            punk.Execute();
        }
    }

    // Класс для работы в режиме PowerShell командлета
    [Cmdlet(VerbsCommon.Get, "Punk")]
    public class PunkCommandlet : Cmdlet
    {
        [Parameter(Position = 0, Mandatory = false)]
        public string Path { get; set; }

        [Parameter(Position = 1, Mandatory = false)]
        public SwitchParameter Help { get; set; }

        protected override void ProcessRecord()
        {
            var punk = new Punk
            {
                Path = this.Path,
                Help = this.Help.IsPresent
            };

            punk.Execute();
        }
    }
}
