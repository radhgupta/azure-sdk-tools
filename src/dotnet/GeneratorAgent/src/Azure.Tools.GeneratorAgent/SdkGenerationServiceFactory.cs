using Azure.Tools.GeneratorAgent.Configuration;
using Microsoft.Extensions.Logging;

namespace Azure.Tools.GeneratorAgent
{
    /// <summary>
    /// Factory for creating SDK generation service instances.
    /// </summary>
    internal static class SdkGenerationServiceFactory
    {
        public static ISdkGenerationService CreateForLocalPath(
            string typeSpecSourcePath,
            string sdkOutputDirectory,
            AppSettings appSettings,
            ILoggerFactory loggerFactory,
            ProcessExecutor processExecutor)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(typeSpecSourcePath);
            ArgumentException.ThrowIfNullOrWhiteSpace(sdkOutputDirectory);
            ArgumentNullException.ThrowIfNull(appSettings);
            ArgumentNullException.ThrowIfNull(loggerFactory);
            ArgumentNullException.ThrowIfNull(processExecutor);

            return new LocalTypeSpecSdkGenerationService(
                appSettings,
                loggerFactory.CreateLogger<LocalTypeSpecSdkGenerationService>(),
                processExecutor,
                typeSpecSourcePath,
                sdkOutputDirectory);
        }

        public static ISdkGenerationService CreateForGitHubCommit(
            string commitId,
            string typespecSpecDirectory,
            string sdkOutputDirectory,
            AppSettings appSettings,
            ILoggerFactory loggerFactory,
            ProcessExecutor processExecutor)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(commitId);
            ArgumentException.ThrowIfNullOrWhiteSpace(typespecSpecDirectory);
            ArgumentException.ThrowIfNullOrWhiteSpace(sdkOutputDirectory);
            ArgumentNullException.ThrowIfNull(appSettings);
            ArgumentNullException.ThrowIfNull(loggerFactory);
            ArgumentNullException.ThrowIfNull(processExecutor);

            return new GitHubTypeSpecSdkGenerationService(
                appSettings,
                loggerFactory.CreateLogger<GitHubTypeSpecSdkGenerationService>(),
                processExecutor,
                commitId,
                typespecSpecDirectory,
                sdkOutputDirectory);
        }
    }
}
