using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static ConcurrentDictionary<string, int> globalWordCount = new();

    static void Main(string[] args)
    {
        // Assign Master process to CPU core 1 (0x1 = core 0)
        Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)0x1;

        Console.WriteLine("Master started. Waiting for agent connections...");

        // Start listeners on two separate threads, one for each pipe
        Task t1 = Task.Run(() => ListenToAgent("agent1"));
        Task t2 = Task.Run(() => ListenToAgent("agent2"));

        // Wait for both agents to finish
        Task.WaitAll(t1, t2);

        // Output aggregated results
        Console.WriteLine("\n=== Final Aggregated Word Count ===");
        foreach (var entry in globalWordCount)
        {
            Console.WriteLine(entry.Key + ":" + entry.Value);
        }
    }

    static void ListenToAgent(string pipeName)
    {
        using var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In);

        Console.WriteLine($"Waiting for connection on pipe '{pipeName}'...");
        pipeServer.WaitForConnection();
        Console.WriteLine($"Connected to agent via pipe '{pipeName}'.");

        using var reader = new StreamReader(pipeServer);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line == "EOF") break;

            //
