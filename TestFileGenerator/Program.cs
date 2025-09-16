using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Text;
using System.Threading.Tasks;

namespace TestFileGenerator
{
    /// <summary>
    /// Configuration for file generation with static values.
    /// </summary>
    public class FileGeneratorConfig
    {
        public string OutputPath { get; }
        public double SizeMb { get; }
        public long TargetBytes { get; }
        public long EstimatedLines { get; }

        public const double BytesPerMb = 1024 * 1024; // Change from private to public
        public const int AvgLineLength = 20; // ~3.5 digits + 6 bytes (". \n") + ~10 bytes string
        public const string FileName = "TestFile.txt";
        public const double DefaultSizeMb = 50;

        public FileGeneratorConfig(string buildDirectory, IFileSystem fileSystem)
        {
            string appDataPath = fileSystem.Path.Combine(buildDirectory, "AppData");
            fileSystem.Directory.CreateDirectory(appDataPath);
            OutputPath = fileSystem.Path.Combine(appDataPath, FileName);
            SizeMb = DefaultSizeMb;
            TargetBytes = (long)(SizeMb * BytesPerMb);
            EstimatedLines = Math.Max(1, TargetBytes / AvgLineLength);
        }
    }

    /// <summary>
    /// Generates a text file with lines in format "<Number>. <String>" with duplicates.
    /// </summary>
    public class FileGenerator
    {
        private readonly IFileSystem _fileSystem;
        private readonly Random _random;
        private static readonly string[] SampleStrings =
        {
            "Apple", "Banana is yellow", "Cherry is the best", "Date", "Elderberry",
            "Fig", "Grape", "Honeydew", "Something something something", "Watermelon"
        };

        public FileGenerator(IFileSystem fileSystem, Random random = null)
        {
            _fileSystem = fileSystem ?? new FileSystem();
            _random = random ?? new Random();
        }

        public async Task GenerateFileAsync(FileGeneratorConfig config)
        {
            var stopwatch = Stopwatch.StartNew();
            long linesWritten = 0;
            var buffer = new ConcurrentBag<string>();

            // Generate lines in parallel
            await Task.Run(() =>
            {
                Parallel.For(0, config.EstimatedLines, i =>
                {
                    int number = Random.Shared.Next(1, 1_000_001); // Thread-safe random
                    string str = SampleStrings[(i / 1000) % SampleStrings.Length]; // ~1000 duplicates per string
                    buffer.Add($"{number}. {str}");
                });
            });

            // Replace the problematic code with the following:
            using var writer = _fileSystem.File.Open(
                config.OutputPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);
            using var streamWriter = new StreamWriter(writer, Encoding.UTF8, bufferSize: 8192);
            foreach (var line in buffer)
            {
                await streamWriter.WriteLineAsync(line);
                linesWritten++;
                if (linesWritten % 10000 == 0 && _fileSystem.FileInfo.New(config.OutputPath).Length > config.TargetBytes)
                    break;
            }

            stopwatch.Stop();
            // Fix for CS1061: Replace 'FromFileName' with 'New' to create an IFileInfo instance
            double actualSizeMb = _fileSystem.FileInfo.New(config.OutputPath).Length / FileGeneratorConfig.BytesPerMb;
            Console.WriteLine($"Generated file: {config.OutputPath} (~{actualSizeMb:F2} MB, {linesWritten} lines, {stopwatch.ElapsedMilliseconds} ms)");
        }
    }

    class Program
    {
        static async Task Main()
        {
            try
            {
                const string buildDirectory = @"C:\Build";
                var fileSystem = new FileSystem();
                var config = new FileGeneratorConfig(buildDirectory, fileSystem);
                var generator = new FileGenerator(fileSystem);
                await generator.GenerateFileAsync(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}