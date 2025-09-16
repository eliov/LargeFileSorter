# File Sorter Solution (.NET 8)

This solution consists of three main components:
- **TestFileGenerator**: Generates a large test file with random lines.
- **Sorter**: Sorts the generated file.
- **Tests**: Validates the sorting logic.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (recommended)
- Windows OS (paths are Windows-style)

---

## Project Structure

- `TestFileGenerator/`: Generates the test file.
- `Sorter/`: Sorts the large test file.
- `Tests/`: Contains unit tests for sorting logic.

---

## Step-by-Step Guide

### 1. Generate the Test File

First, run the **TestFileGenerator** project to create a test file.

#### Using Visual Studio

- Right-click the `TestFileGenerator` project in Solution Explorer.
- Select __Set as Startup Project__.
- Press __F5__ (Start Debugging) or __Ctrl+F5__ (Start Without Debugging).

#### Using Command Line
dotnet run --project TestFileGenerator

This will create `TestFile.txt` in `C:\Build\AppData\`.

---

### 2. Sort the Test File

Next, run the **Sorter** project to sort the generated file.

#### Using Visual Studio

- Right-click the `Sorter` project.
- Select __Set as Startup Project__.
- Press __F5__ or __Ctrl+F5__.

#### Using Command Line
dotnet run --project Sorter

This will read `C:\Build\AppData\TestFile.txt` and write the sorted output to `C:\Build\AppData\SortedTestFile.txt`.

---

### 3. Run the Tests

Finally, run the tests to verify the sorting logic.

#### Using Visual Studio

- Right-click the `Tests` project.
- Select __Run Tests__ or use the __Test Explorer__ window.

---

## Notes

- Ensure the `C:\Build\AppData\` directory exists and is writable.
- The generated test file size is ~50MB by default.
- All paths are hardcoded for demonstration; adjust as needed for your environment.

---

## Troubleshooting

- If you encounter file access errors, run Visual Studio or your terminal as Administrator.
- Check that the .NET 8 SDK is installed and your PATH is set correctly.

---

## License

MIT