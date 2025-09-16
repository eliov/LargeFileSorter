using Microsoft.VisualStudio.TestPlatform.TestHost;
using Sorter;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FileSorter.Tests
{
    public class SorterTests
    {
        private readonly MockFileSystem _fileSystem;

        public SorterTests()
        {
            _fileSystem = new MockFileSystem();
        }

        [Fact]
        public void LineItem_ParseLineItem_ValidFormat_ReturnsCorrectItem()
        {
            // Arrange
            string line = "123. Hello World";

            // Act
            var item = Sorter.Program.ParseLineItem(line);

            // Assert
            Assert.Equal(123, item.Number);
            Assert.Equal("Hello World", item.Text);
        }

        [Fact]
        public void LineItem_ParseLineItem_InvalidFormatNoDot_ThrowsFormatException()
        {
            // Arrange
            string line = "123 Hello World";

            // Act & Assert
            Assert.Throws<FormatException>(() => Sorter.Program.ParseLineItem(line));
        }

        [Fact]
        public void LineItem_ParseLineItem_InvalidNumber_ThrowsFormatException()
        {
            // Arrange
            string line = "abc. Hello World";

            // Act & Assert
            Assert.Throws<FormatException>(() => Sorter.Program.ParseLineItem(line));
        }

        [Fact]
        public void LineItem_ParseLineItem_MissingText_ThrowsFormatException()
        {
            // Arrange
            string line = "123.";

            // Act & Assert
            Assert.Throws<FormatException>(() => Sorter.Program.ParseLineItem(line));
        }

        [Fact]
        public void LineItem_CompareTo_SameText_DiffNumbers_ComparesByNumber()
        {
            // Arrange
            var item1 = new LineItem(1, "Apple");
            var item2 = new LineItem(2, "Apple");

            // Act
            int result = item1.CompareTo(item2);

            // Assert
            Assert.True(result < 0, "Item1 should be less than Item2 based on Number");
        }

        [Fact]
        public void LineItem_CompareTo_DiffText_SameNumbers_ComparesByText()
        {
            // Arrange
            var item1 = new LineItem(1, "Apple");
            var item2 = new LineItem(1, "Banana");

            // Act
            int result = item1.CompareTo(item2);

            // Assert
            Assert.True(result < 0, "Apple should be less than Banana based on Text");
        }

        [Fact]
        public void LineItem_CompareTo_NullOther_ReturnsPositive()
        {
            // Arrange
            var item = new LineItem(1, "Apple");

            // Act
            int result = item.CompareTo(null);

            // Assert
            Assert.Equal(1, result);
        }
               

        [Fact]
        public async Task ExternalSortAsync_EmptyFile_CopiesInputToOutput()
        {
            // Arrange
            string inputPath = @"C:\Build\AppData\TestFile.txt";
            string outputPath = @"C:\Build\AppData\SortedTestFile.txt";
            _fileSystem.AddFile(inputPath, new MockFileData(""));

            // Act
            await Sorter.Program.ExternalSortAsync(inputPath, outputPath, _fileSystem);

            // Assert
            var outputContent = _fileSystem.File.ReadAllText(outputPath);
            Assert.Equal("", outputContent);
        }

        [Fact]
        public async Task ExternalSortAsync_SingleLineFile_SortsCorrectly()
        {
            // Arrange
            string inputPath = @"C:\Build\AppData\TestFile.txt";
            string outputPath = @"C:\Build\AppData\SortedTestFile.txt";
            var inputLines = new[] { "1. Apple" };
            _fileSystem.AddFile(inputPath, new MockFileData(string.Join(Environment.NewLine, inputLines)));

            // Act
            await Sorter.Program.ExternalSortAsync(inputPath, outputPath, _fileSystem);

            // Assert
            var outputContent = _fileSystem.File.ReadAllLines(outputPath);
            Assert.Equal(inputLines, outputContent);
        }
    }
}