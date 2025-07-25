using Microsoft.Extensions.Logging;
using Azure.Tools.GeneratorAgent.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Tools.GeneratorAgent
{
    internal class LocalTypeSpecSdkGenerationService : SdkGenerationServiceBase
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

        /// <summary>
        /// Compiles a TypeSpec project into an SDK using the new simplified flow.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if compilation succeeds, false otherwise</returns>
        public override async Task<bool> CompileTypeSpecAsync(CancellationToken cancellationToken = default)
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
            Logger.LogInformation("Installing TypeSpec dependencies globally");

            (bool success, string output, string error) = await ProcessExecutor.ExecuteAsync(
                AppSettings.CommandExecutor,
                $"{AppSettings.CommandPrefix} npm install --global {AppSettings.TypespecEmitterPackage}",
                null, // Use current directory, no need to change to TypeSpec source path
                cancellationToken).ConfigureAwait(false);

            if (!success)
            {
                Logger.LogError("Global npm install failed. Error: {Error}", error);
                return false;
            }

            Logger.LogInformation("TypeSpec dependencies installed globally successfully: {Output}", output);
            return true;
        }

        protected virtual async Task<bool> CompileTypeSpec(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Compiling TypeSpec project");

            string tspOutputPath = Path.Combine(SdkOutputDirectory, AppSettings.TspOutputDirectoryName);

            (bool success, string output, string error) = await ProcessExecutor.ExecuteAsync(
                AppSettings.CommandExecutor,
                $"{AppSettings.CommandPrefix} npx tsp compile . --emit {AppSettings.TypespecEmitterPackage} --option \"{AppSettings.TypespecEmitterPackage}.emitter-output-dir={tspOutputPath}\"",
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
   }
}
