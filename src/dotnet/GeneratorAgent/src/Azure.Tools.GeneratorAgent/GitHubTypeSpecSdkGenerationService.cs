using Azure.Tools.GeneratorAgent.Configuration;
using Microsoft.Extensions.Logging;

namespace Azure.Tools.GeneratorAgent
{
    /// <summary>
    /// SDK generation service for TypeSpec projects from GitHub repositories.
    /// </summary>
    internal class GitHubTypeSpecSdkGenerationService : ISdkGenerationService
    {
        private readonly ILogger<GitHubTypeSpecSdkGenerationService> Logger;
        private readonly ProcessExecutor ProcessExecutor;
        private readonly AppSettings AppSettings;
        private readonly string CommitId;
        private readonly string TypespecSpecDirectory;
        private readonly string SdkOutputDirectory;

        public GitHubTypeSpecSdkGenerationService(
            AppSettings appSettings,
            ILogger<GitHubTypeSpecSdkGenerationService> logger,
            ProcessExecutor processExecutor,
            string commitId,
            string typespecSpecDirectory,
            string sdkOutputDirectory)
        {
            ArgumentNullException.ThrowIfNull(appSettings);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(processExecutor);
            ArgumentException.ThrowIfNullOrWhiteSpace(commitId);
            ArgumentException.ThrowIfNullOrWhiteSpace(typespecSpecDirectory);
            ArgumentException.ThrowIfNullOrWhiteSpace(sdkOutputDirectory);

            AppSettings = appSettings;
            Logger = logger;
            ProcessExecutor = processExecutor;
            CommitId = commitId;
            TypespecSpecDirectory = typespecSpecDirectory;
            SdkOutputDirectory = sdkOutputDirectory;
        }

        public async Task<bool> CompileTypeSpecAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogInformation("Starting GitHub-based TypeSpec compilation for commit: {CommitId}", CommitId);
                Logger.LogInformation("SDK output directory: {SdkOutputDirectory}", SdkOutputDirectory);
                Logger.LogInformation("TypeSpec spec directory: {TypeSpecSpecDirectory}", TypespecSpecDirectory);

                // Step 1: Extract azure-sdk-for-net root path
                string azureSdkPath = ExtractAzureSdkPath();

                // Step 2: Run PowerShell generation script
                if (!await RunPowerShellGenerationScript(azureSdkPath, cancellationToken))
                {
                    return false;
                }

                // Step 3: Navigate to src directory and run dotnet build
                if (!await RunDotNetBuildGenerateCode(cancellationToken))
                {
                    return false;
                }

                Logger.LogInformation("GitHub-based TypeSpec compilation completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error during GitHub-based TypeSpec compilation");
                return false;
            }
        }

        private string ExtractAzureSdkPath()
        {
            // Extract azure-sdk-for-net path from SDK output directory
            string azureSdkPath = SdkOutputDirectory;
            while (!string.IsNullOrEmpty(azureSdkPath) && !Path.GetFileName(azureSdkPath).Equals(AppSettings.AzureSdkDirectoryName, StringComparison.OrdinalIgnoreCase))
            {
                string? parentPath = Path.GetDirectoryName(azureSdkPath);
                if (parentPath == null)
                {
                    break;
                }
                azureSdkPath = parentPath;
            }

            if (string.IsNullOrEmpty(azureSdkPath))
            {
                throw new InvalidOperationException("Could not locate azure-sdk-for-net directory from SDK output path");
            }

            Logger.LogInformation("Extracted azure-sdk path: {AzureSdkPath}", azureSdkPath);
            return azureSdkPath;
        }

        private async Task<bool> RunPowerShellGenerationScript(string azureSdkPath, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Running PowerShell generation script");

            string scriptPath = Path.Combine(azureSdkPath, AppSettings.PowerShellScriptPath);

            if (!File.Exists(scriptPath))
            {
                Logger.LogError("PowerShell script not found: {ScriptPath}", scriptPath);
                return false;
            }

            string arguments = $"/c pwsh \"{scriptPath}\" " +
                           $"-sdkFolder \"{SdkOutputDirectory}\" " +
                           $"-typespecSpecDirectory \"{TypespecSpecDirectory}\" " +
                           $"-commit \"{CommitId}\" " +
                           $"-repo \"{AppSettings.AzureSpecRepository}\"";

            (bool success, string output, string error) = await ProcessExecutor.ExecuteAsync(
                AppSettings.CommandExecutor,
                arguments,
                azureSdkPath,
                cancellationToken).ConfigureAwait(false);

            if (!success)
            {
                Logger.LogError("PowerShell generation script failed. Error: {Error}", error);
                if (!string.IsNullOrEmpty(output))
                {
                    Logger.LogError("PowerShell script standard output: {Output}", output);
                }
                return false;
            }

            Logger.LogInformation("PowerShell generation script completed successfully");
            return true;
        }

        private async Task<bool> RunDotNetBuildGenerateCode(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Running dotnet build /t:generateCode");

            string srcDirectory = Path.Combine(SdkOutputDirectory, "src");

            if (!Directory.Exists(srcDirectory))
            {
                Logger.LogError("Source directory not found: {SrcDirectory}", srcDirectory);
                return false;
            }

            Logger.LogInformation("Executing dotnet build in directory: {Directory}", srcDirectory);

            (bool success, string output, string error) = await ProcessExecutor.ExecuteAsync(
                AppSettings.CommandExecutor,
                $"{AppSettings.CommandPrefix} dotnet build /t:generateCode",
                srcDirectory,
                cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(output))
            {
                Logger.LogInformation("dotnet build output: {Output}", output);
            }

            if (!success)
            {
                Logger.LogError("dotnet build /t:generateCode failed with exit code. Error output: {Error}", error);
                if (!string.IsNullOrEmpty(output))
                {
                    Logger.LogError("dotnet build standard output: {Output}", output);
                }
                return false;
            }

            Logger.LogInformation("dotnet build /t:generateCode completed successfully");
            return true;
        }
    }
}
