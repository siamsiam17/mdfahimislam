using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        // Version 1.1: Set ScannerA to run on CPU Core 2
        Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)0x2;

        // Version 1.0: Check for input directory argument
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: ScannerA <directory_path>");
            return;
        }

        string directoryPath = args[0];

        // Version 1.2: Use separate thread to perform file reading and processing
        Task.Run(() => FileReader(directoryPath));

        Console.WriteLine("ScannerA running. Press Enter to exit...");
        Console.ReadLine();
    }

    static void FileReader(string directoryPath)
    {
        // Version 1.4: Connect to Master via Named Pipe "agent1"
        var pipe = new NamedPipeClientStream("agent1");
        pipe.Connect();

        using var writer = new StreamWriter(pipe) { AutoFlush = true };

        // Version 1.2: Process all .txt files in the given directory
        foreach (var file in Directory.GetFiles(directoryPath, "*.txt"))
        {
            var lines = File.ReadAllLines(file);
            var wordCount = new Dictionary<string, int>();

            // Version 1.3: Count occurrences of each word (case-insensitive)
            foreach (var line in lines)
            {
                var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    var cleaned = word.Trim().ToLower(); // Normalize word
                    if (!wordCount.ContainsKey(cleaned))
                        wordCount[cleaned] = 0;

                    wordCount[cleaned]++;
                }
            }

            // Version 1.4: Send filename:word:count to Master
            foreach (var kvp in wordCount)
                writer.WriteLine($"{Path.GetFileName(file)}:{kvp.Key}:{kvp.Value}");
        }

        // Version 1.5: Signal end of file data transmission
        writer.WriteLine("EOF");
    }
}
