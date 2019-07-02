using System;
using System.IO;
using Newtonsoft.Json;

namespace VampireKnightDS
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var arm9Data = LoadData(ReadSettings());
                var userChoice = ShowUserMenu();
                string answer = string.Empty;

                switch (userChoice)
                {
                    case MenuChoices.DumpPointers:
                        Console.WriteLine("DestinationFile: ");
                        answer = Console.ReadLine();
                        arm9Data.ExportPointersToFile(answer);
                        break;
                    case MenuChoices.DumpTextBlocks:
                        arm9Data.DumpTextBlocks();
                        break;
                    case MenuChoices.LoadNewTranslations:
                        arm9Data.LoadTranslationsAndMakeANewBin();
                        break;
                    case MenuChoices.DumpTextByPointer:
                        uint initialPointer;
                        Console.WriteLine("Pointer to start looking from: ");
                        answer = Console.ReadLine();
                        initialPointer = Convert.ToUInt32(answer);
                        var lines = arm9Data.DumpTextByPointer(initialPointer);
                        Console.WriteLine("DestinationFile: ");
                        answer = Console.ReadLine();
                        File.WriteAllLines(answer, lines);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine($"An error occured: {ex.Message}");
                Console.ResetColor();
            }
        }

        static JsonSettings ReadSettings()
        {
            Console.WriteLine("Initializing...");
            string settingsJson = File.ReadAllText(@"Settings.json");
            return JsonConvert.DeserializeObject<JsonSettings>(settingsJson);
        }

        static Arm9Data LoadData(JsonSettings jsonSettings)
        {
            Console.WriteLine("Loading data from arm9.bin...");
            return new Arm9Data(jsonSettings);
        }

        public enum MenuChoices
        {
            DumpTextBlocks,
            LoadNewTranslations,
            DumpPointers,
            DumpTextByPointer,
            Exit
        }

        static MenuChoices ShowUserMenu()
        {
            Console.WriteLine("Pick an option:");
            Console.WriteLine("\t 1. Dump text blocks");
            Console.WriteLine("\t 2. Process translated files and make a new bin");
            Console.WriteLine("\t 3. Scan for pointers and print them");
            Console.WriteLine("\t 4. Dump text block by adjacent pointers");
            Console.WriteLine("\t 5. Exit");

            while (true)
            {
                var result = Console.ReadKey();
                switch (result.KeyChar)
                {
                    case '1':
                        return MenuChoices.DumpTextBlocks;
                    case '2':
                        return MenuChoices.LoadNewTranslations;
                    case '3':
                        return MenuChoices.DumpPointers;
                    case '4':
                        return MenuChoices.DumpTextByPointer;
                    case '5': 
                        return MenuChoices.Exit;
                    default:
                        continue;
                }
            }
        }
    }
}