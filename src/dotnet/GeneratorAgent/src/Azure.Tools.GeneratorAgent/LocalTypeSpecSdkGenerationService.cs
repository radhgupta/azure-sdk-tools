using Microsoft.Extensions.Logging;
using Azure.Tools.GeneratorAgent.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Tools.GeneratorAgent
{
    internal class LocalTypeSpecSdkGenerationService : ISdkGenerationService
    {
        private readonly AppSettings AppSettings;
        private readonly ILogger<LocalTypeSpecSdkGenerationService> Logger;
        private readonly ProcessExecutor ProcessExecutor;
        private readonly string TypeSpecSourcePath;
        private readonly string SdkOutputDirectory;


        public LocalTypeSpecSdkGenerationService(
            AppSettings appSettings,
            ILogger<LocalTypeSpecSdkGenerationService> logger,
            ProcessExecutor processExecutor,
            string typeSpecSourcePath,
            string sdkOutputDirectory)
        {
            AppSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ProcessExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
            TypeSpecSourcePath = typeSpecSourcePath ?? throw new ArgumentNullException(nameof(typeSpecSourcePath));
            SdkOutputDirectory = sdkOutputDirectory ?? throw new ArgumentNullException(nameof(sdkOutputDirectory));
            if (string.IsNullOrWhiteSpace(TypeSpecSourcePath))
            {
                throw new ArgumentException("TypeSpec source path cannot be null or whitespace", nameof(typeSpecSourcePath));
            }

            if (string.IsNullOrWhiteSpace(SdkOutputDirectory))
            {
                throw new ArgumentException("SDK output directory cannot be null or whitespace", nameof(sdkOutputDirectory));
            }
        }

        protected virtual bool DirectoryExists(string path) => Directory.Exists(path);
        protected virtual bool FileExists(string path) => File.Exists(path);
        protected virtual void CreateDirectory(string path) => Directory.CreateDirectory(path);
        protected virtual void DeleteDirectory(string path, bool recursive) => Directory.Delete(path, recursive);
        protected virtual string[] GetFiles(string path) => Directory.GetFiles(path);
        protected virtual string[] GetDirectories(string path) => Directory.GetDirectories(path);
        protected virtual void MoveFile(string sourceFileName, string destFileName) => File.Move(sourceFileName, destFileName);
        protected virtual void ReplaceFile(string sourceFileName, string destinationFileName, string destinationBackupFileName) => File.Replace(sourceFileName, destinationFileName, destinationBackupFileName);
        protected virtual void DeleteFile(string path) => File.Delete(path);
        protected virtual void MoveDirectory(string sourceDirName, string destDirName) => Directory.Move(sourceDirName, destDirName);

        /// <summary>
        /// Compiles a TypeSpec project into an SDK using the new simplified flow.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if compilation succeeds, false otherwise</returns>
        public async Task<bool> CompileTypeSpecAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!DirectoryExists(TypeSpecSourcePath))
                {
                    throw new DirectoryNotFoundException($"TypeSpec project directory not found: {TypeSpecSourcePath}");
                }

                Logger.LogInformation("Starting TypeSpec compilation for project: {ProjectPath}", TypeSpecSourcePath);
                Logger.LogInformation("Output SDK path: {OutputPath}", SdkOutputDirectory);

                if (!await InstallTypeSpecDependencies(cancellationToken))
                {
                    return false;
                }

                if (!await CompileTypeSpec(cancellationToken))
                {
                    return false;
                }

                if (!MoveGeneratedFilesAndCleanup(cancellationToken))
                {
                    return false;
                }

                Logger.LogInformation("TypeSpec compilation completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error during TypeSpec compilation");
                return false;
            }
        }

        protected virtual async Task<bool> InstallTypeSpecDependencies(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Installing TypeSpec dependencies in: {ProjectPath}", TypeSpecSourcePath);

            (bool success, string output, string error) = await ProcessExecutor.ExecuteAsync(
                AppSettings.CommandExecutor,
                $"{AppSettings.CommandPrefix} npm install --save-dev {AppSettings.TypespecEmitterPackage}",
                TypeSpecSourcePath,
                cancellationToken).ConfigureAwait(false);

            if (!success)
            {
                Logger.LogError("npm install failed. Error: {Error}", error);
                return false;
            }

            Logger.LogInformation("TypeSpec dependencies installed successfully: {Output}", output);
            return true;
        }

        protected virtual async Task<bool> CompileTypeSpec(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Compiling TypeSpec project");

            string tspOutputPath = Path.Combine(SdkOutputDirectory, AppSettings.TspOutputDirectoryName);

            (bool success, string output, string error) = await ProcessExecutor.ExecuteAsync(
                AppSettings.CommandExecutor,
                $"{AppSettings.CommandPrefix} npx tsp compile . --emit {AppSettings.TypespecEmitterPackage} --output-dir \"{tspOutputPath}\"",
                TypeSpecSourcePath,
                cancellationToken).ConfigureAwait(false);

            if (!success)
            {
                Logger.LogError("TypeSpec compilation failed. Error: {Error}", error);
                return false;
            }

            Logger.LogInformation("TypeSpec compilation completed: {Output}", output);
            return true;
        }

        protected virtual bool MoveGeneratedFilesAndCleanup(CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogInformation("Moving generated files and cleaning up");

                string tspOutputPath = Path.Combine(SdkOutputDirectory, AppSettings.TspOutputDirectoryName);
                string generatedSourcePath = Path.Combine(tspOutputPath, AppSettings.TypeSpecDirectoryName, AppSettings.HttpClientCSharpDirectoryName);

                if (!DirectoryExists(generatedSourcePath))
                {
                    Logger.LogError("Generated source directory not found: {GeneratedSourcePath}", generatedSourcePath);
                    return false;
                }

                Logger.LogInformation("Moving files from {SourcePath} to {TargetPath}", generatedSourcePath, SdkOutputDirectory);

                MoveDirectoryContents(generatedSourcePath, SdkOutputDirectory, cancellationToken);

                if (DirectoryExists(tspOutputPath))
                {
                    DeleteDirectory(tspOutputPath, true);
                    Logger.LogInformation("Deleted tsp-output directory: {TspOutputPath}", tspOutputPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to move generated files and cleanup");
                return false;
            }
        }

        private void MoveDirectoryContents(string sourceDir, string targetDir, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!DirectoryExists(targetDir))
            {
                CreateDirectory(targetDir);
            }

            string[] files = GetFiles(sourceDir);
            string[] directories = GetDirectories(sourceDir);

            Logger.LogInformation("Moving {FileCount} files and {DirectoryCount} directories", files.Length, directories.Length);

            int filesMoved = 0;
            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);

                try
                {
                    MoveFileWithRetry(file, destFile);
                    filesMoved++;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to move file from {SourceFile} to {DestFile}", file, destFile);
                    throw;
                }
            }

            int directoriesMoved = 0;
            foreach (string dir in directories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string dirName = Path.GetFileName(dir);
                string destDir = Path.Combine(targetDir, dirName);

                try
                {
                    MoveDirectoryWithRetry(dir, destDir);
                    directoriesMoved++;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to move directory from {SourceDir} to {DestDir}", dir, destDir);
                    throw;
                }
            }

            Logger.LogInformation("Successfully moved {FilesMoved} files and {DirectoriesMoved} directories", filesMoved, directoriesMoved);
        }

        private void MoveFileWithRetry(string sourceFile, string destFile)
        {
            const int maxRetries = 3;
            const int retryDelayMs = 100;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    if (FileExists(destFile))
                    {
                        string backupFile = destFile + ".backup";
                        ReplaceFile(sourceFile, destFile, backupFile);
                        
                        if (FileExists(backupFile))
                        {
                            DeleteFile(backupFile);
                        }
                    }
                    else
                    {
                        MoveFile(sourceFile, destFile);
                    }
                    
                    return;
                }
                catch (IOException) when (attempt < maxRetries)
                {
                    Thread.Sleep(retryDelayMs * attempt);
                }
            }
        }

        private void MoveDirectoryWithRetry(string sourceDir, string destDir)
        {
            const int maxRetries = 3;
            const int retryDelayMs = 100;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    if (DirectoryExists(destDir))
                    {
                        DeleteDirectory(destDir, true);
                        Thread.Sleep(10);
                    }

                    MoveDirectory(sourceDir, destDir);
                    return;
                }
                catch (IOException) when (attempt < maxRetries)
                {
                    Thread.Sleep(retryDelayMs * attempt);
                }
            }
        }
    }
}
