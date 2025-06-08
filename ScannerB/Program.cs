using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        // Set this process to run on CPU core 3 (bitmask 0x4 = core 3)
        Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)0x4;

        // Ensure a directory path is provided
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: ScannerB <directory_path>");
            return;
        }

        string directoryPath = args[0];

        // Start the file reading and pipe communication on a background thread
        Task.Run(() => FileReader(directoryPath));

        Console.WriteLine("ScannerB running. Press Enter to exit...");
        Console.ReadLine();
    }

    static void FileReader(string directoryPath)
    {
        // Connect to the Master process via named pipe "agent2"
        var pipe = new NamedPipeClientStream("agent2");
        pipe.Connect();

        // Setup writer to send data through the pipe
        using var writer = new StreamWriter(pipe) { AutoFlush = true };

        // Process each .txt file in the directory
        foreach (var file in Directory.GetFiles(directoryPath, "*.txt"))
        {
            var lines = File.ReadAllLines(file);
            var wordCount = new Dictionary<string, int>(); // Store word frequency

            // Count words line by line
            foreach (var line in lines)
            {
                var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    var cleaned = word.Trim().ToLower(); // Normalize word
                    if (!wordCount.ContainsKey(cleaned)) wordCount[cleaned] = 0;
                    wordCount[cleaned]++;
                }
            }

            // Send result to Master: format -> filename:word:count
            foreach (var kvp in wordCount)
                writer.WriteLine($"{Path.GetFileName(file)}:{kvp.Key}:{kvp.Value}");
        }

        // Signal the end of transmission
        writer.WriteLine("EOF");
    }
}
