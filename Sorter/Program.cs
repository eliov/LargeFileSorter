// Sorter/Sorter.cs
// Compile: csc Sorter.cs /reference:System.IO.Abstractions.dll
// Usage: Sorter.exe
// Reads from C:\Build\AppData\TestFile.txt, writes to C:\Build\AppData\SortedTestFile.txt

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace Sorter
{
    public record LineItem(int Number, string Text) : IComparable<LineItem>
    {
        public int CompareTo(LineItem? other)
        {
            if (other == null) return 1;
            int textCmp = string.CompareOrdinal(Text, other.Text);
            return textCmp != 0 ? textCmp : Number.CompareTo(other.Number);
        }

        public override string ToString() => $"{Number}. {Text}";
    }

    public record MergeNode(LineItem Item, int FileIndex) : IComparable<LineItem>
    {
        public int CompareTo(LineItem? other) => Item.CompareTo(other);
    }

    public class Program
    {
        private static async Task Main()
        {
            const string buildDirectory = @"C:\Build";
            IFileSystem fileSystem = new FileSystem(); // Use real file system; replace with mock for testing
            // Inline the path construction logic from the provided FileGeneratorConfig constructor
            // (No class or duplication needed—just direct reading from the given path)
            string appDataPath = fileSystem.Path.Combine(buildDirectory, "AppData");
            fileSystem.Directory.CreateDirectory(appDataPath); // Ensure directory exists (as in constructor)
            const string fileName = "TestFile.txt"; // From the const in the provided class
            string inputPath = fileSystem.Path.Combine(appDataPath, fileName); // C:\Build\AppData\TestFile.txt
            string outputPath = fileSystem.Path.Combine(appDataPath, "SortedTestFile.txt"); // Output in same directory

            if (!fileSystem.File.Exists(inputPath))
            {
                Console.WriteLine($"Input file {inputPath} does not exist.");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            await ExternalSortAsync(inputPath, outputPath, fileSystem);
            stopwatch.Stop();

            Console.WriteLine($"Sorting completed in {stopwatch.Elapsed}. Output: {outputPath}");
        }

        public static async Task ExternalSortAsync(string inputPath, string outputPath, IFileSystem fileSystem)
        {
            const int maxLinesPerChunk = 1_000_000; // ~100MB assuming avg 100 bytes/line
            var tempFiles = new List<string>();

            // Phase 1: Create sorted chunks (reads directly from inputPath)
            using var reader = fileSystem.File.OpenText(inputPath);
            var currentChunk = new List<LineItem>(maxLinesPerChunk);
            string? line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                var item = ParseLineItem(line);
                currentChunk.Add(item);

                if (currentChunk.Count >= maxLinesPerChunk)
                {
                    await SortAndWriteChunkAsync(currentChunk, tempFiles, fileSystem);
                    currentChunk.Clear();
                }
            }

            if (currentChunk.Count > 0)
            {
                await SortAndWriteChunkAsync(currentChunk, tempFiles, fileSystem);
            }

            // Phase 2: K-way merge
            if (tempFiles.Count == 0)
            {
                fileSystem.File.Copy(inputPath, outputPath, true);
                return;
            }

            await MergeChunksAsync(tempFiles, outputPath, fileSystem);

            // Cleanup temp files
            foreach (var tempFile in tempFiles)
            {
                try { fileSystem.File.Delete(tempFile); } catch { /* Ignore cleanup errors */ }
            }
        }

        public static LineItem ParseLineItem(string line)
        {
            int dotIndex = line.IndexOf('.');
            if (dotIndex == -1 || dotIndex + 1 >= line.Length || line[dotIndex + 1] != ' ')
            {
                throw new FormatException($"Invalid line format: {line}");
            }

            string numStr = line.Substring(0, dotIndex).Trim();
            string text = line.Substring(dotIndex + 2).Trim();

            if (!int.TryParse(numStr, out int number))
            {
                throw new FormatException($"Invalid number in line: {line}");
            }

            return new LineItem(number, text);
        }

        private static async Task SortAndWriteChunkAsync(List<LineItem> chunk, List<string> tempFiles, IFileSystem fileSystem)
        {
            chunk.Sort(); // Uses IComparable

            string tempFile = fileSystem.Path.GetTempFileName();
            tempFiles.Add(tempFile);

            using var writer = fileSystem.File.CreateText(tempFile);
            foreach (var item in chunk)
            {
                await writer.WriteLineAsync(item.ToString());
            }
        }

        private static async Task MergeChunksAsync(List<string> tempFiles, string outputPath, IFileSystem fileSystem)
        {
            using var outputWriter = fileSystem.File.CreateText(outputPath);
            StreamReader[] readers = tempFiles.Select(f => fileSystem.File.OpenText(f)).ToArray();

            // Initialize PriorityQueue (min-heap)
            var pq = new PriorityQueue<MergeNode, LineItem>();
            for (int i = 0; i < readers.Length; i++)
            {
                string? firstLine = await readers[i].ReadLineAsync();
                if (firstLine != null)
                {
                    var item = ParseLineItem(firstLine);
                    pq.Enqueue(new MergeNode(item, i), item);
                }
            }

            // K-way merge
            while (pq.Count > 0)
            {
                var node = pq.Dequeue();
                await outputWriter.WriteLineAsync(node.Item.ToString());

                string? nextLine = await readers[node.FileIndex].ReadLineAsync();
                if (nextLine != null)
                {
                    var nextItem = ParseLineItem(nextLine);
                    pq.Enqueue(new MergeNode(nextItem, node.FileIndex), nextItem);
                }
            }
        }
    }
}