using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using Xunit.Abstractions;


namespace Kiota.NPM.IntegrationTests;

public class InstallTests
{
    private readonly ITestOutputHelper _outputHelper;

    public InstallTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
    }

    [Fact]
    public void Install_Creates_Working_Bin_Command()
    {
        // This test first compiles then packs the Kiota project to a local folder created for 
        // the individual test run, such that there is a tarball waiting in the folder.
        // Then it copies the package.json file from the Assets folder to the test folder.
        // Then it runs npm install in the test folder.
        // Finally, it asserts two things:
        // Firstly, if the kiota command is available in the node_modules/.bin folder.
        // Secondly, if the npx command can be used to run the kiota command successfully
        // with the '--version' command line option which must successfully return a string which conforms to a pattern expressing the version of kiota.

        // Create a temporary directory for the test
        var testDir = Path.Combine(Path.GetTempPath(), $"kiota-npm-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            // Pack the project using npm
            var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../vscode/npm-package"));
            _outputHelper.WriteLine($"Running pack in {projectDir}");
            var npmPackProcess = RunProcess("npm", $"pack --pack-destination {testDir}", projectDir);
            _outputHelper.WriteLine("Checking if npm pack process exited successfully.");
            Assert.Equal(0, npmPackProcess.exitCode);

            // Copy package.json from Assets folder to test directory
            var assetsDir = Path.Combine(AppContext.BaseDirectory, "../../../Assets");
            File.Copy(Path.Combine(assetsDir, "package.json"), Path.Combine(testDir, "package.json"));

            // Run npm install in the test directory
            var npmProcess = RunProcess("npm", "install", testDir);
            _outputHelper.WriteLine("Checking if npm install process exited successfully.");
            Assert.Equal(0, npmProcess.exitCode);

            // Assert 1: Check if kiota exists in node_modules/.bin
            var kiotaPath = Path.Combine(testDir, "node_modules", ".bin", "kiota");
            if (OperatingSystem.IsWindows())
            {
                kiotaPath += ".cmd"; // On Windows, the bin command is a .cmd file
            }
            _outputHelper.WriteLine($"Checking if Kiota command exists at {kiotaPath}.");
            Assert.True(File.Exists(kiotaPath), $"Kiota command not found at {kiotaPath}");

            // Assert 2: Run kiota --version using npx and check output
            var npxProcess = RunProcess("npx", "kiota --version", testDir);
            _outputHelper.WriteLine("Checking if npx process to run 'kiota --version' exited successfully.");
            Assert.Equal(0, npxProcess.exitCode);

            // Check if output matches version pattern (e.g., 1.2.3 or 1.2.3-preview.4)
            var versionPattern = @"^\d+\.\d+\.\d+.*$";
            _outputHelper.WriteLine("Checking if the output of 'kiota --version' matches the expected version pattern.");
            Assert.Matches(versionPattern, npxProcess.output);
        }
        finally
        {
            // Clean up
            try
            {
                Directory.Delete(testDir, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    private (int exitCode, string output) RunProcess(string fileName, string arguments, string workingDirectory)
    {
        // Try to find npm or npx in standard locations if not found directly  
        if ((fileName == "npm" || fileName == "npx") && OperatingSystem.IsWindows())
        {
            // Check common locations for npm/npx on Windows  
            string commandExtension = ".cmd";
            string[] possiblePaths = new[]
            {
               Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs", $"{fileName}{commandExtension}"),
               Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "nodejs", $"{fileName}{commandExtension}"),  
               // Add from npm global prefix  
               Path.Combine(Environment.GetEnvironmentVariable("APPDATA") ?? "", "npm", $"{fileName}{commandExtension}"),  
               // Try npm global installation path  
               Path.Combine(Environment.GetEnvironmentVariable("APPDATA") ?? "", "npm", "node_modules", "npm", "bin", $"{fileName}{commandExtension}")
           };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    _outputHelper.WriteLine($"Found {fileName} at {path}");
                    fileName = path;
                    break;
                }
            }
        }
        else if ((fileName == "npm" || fileName == "npx") && (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()))
        {
            // Check common locations for npm/npx on Linux/macOS  
            string[] possiblePaths = new[]
            {
               $"/usr/bin/{fileName}",
               $"/usr/local/bin/{fileName}",
               $"/opt/homebrew/bin/{fileName}", // Common on macOS with Homebrew  
               $"{Environment.GetEnvironmentVariable("HOME")}/.npm/bin/{fileName}", // User's npm bin directory  
               $"{Environment.GetEnvironmentVariable("HOME")}/.nvm/versions/node/*/bin/{fileName}" // nvm installations  
           };

            foreach (var path in possiblePaths)
            {
                // Handle wildcard paths (for nvm)  
                if (path.Contains("*"))
                {
                    var directory = Path.GetDirectoryName(path);
                    if (directory != null && Directory.Exists(directory.Replace("*", "")))
                    {
                        // Get the highest version directory  
                        var dirs = Directory.GetDirectories(directory.Replace("*", ""));
                        if (dirs.Length > 0)
                        {
                            var exactPath = Path.Combine(dirs[^1], Path.GetFileName(path));
                            if (File.Exists(exactPath))
                            {
                                _outputHelper.WriteLine($"Found {fileName} at {exactPath}");
                                fileName = exactPath;
                                break;
                            }
                        }
                    }
                    continue;
                }

                if (File.Exists(path))
                {
                    _outputHelper.WriteLine($"Found {fileName} at {path}");
                    fileName = path;
                    break;
                }
            }
        }

        // Fall back to PATH environment if still using the short name  
        if (fileName == "npm" || fileName == "npx")
        {
            // Log that we're using PATH resolution  
            _outputHelper.WriteLine($"Using PATH environment to resolve {fileName}");
        }

        using (var process = new Process())
        {
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var outputBuilder = new StringWriter();

            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    outputBuilder.WriteLine(args.Data);
                }
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    outputBuilder.WriteLine($"Error: {args.Data}");
                }
            };

            if (!process.Start())
            {
                throw new InvalidOperationException($"Failed to start process '{fileName}' in directory '{workingDirectory}'");
            }
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();
            return (process.ExitCode, outputBuilder.ToString().Trim());
        }
    }
}
