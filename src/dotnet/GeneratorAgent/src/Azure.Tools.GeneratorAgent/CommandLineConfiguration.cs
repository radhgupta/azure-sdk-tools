using System.CommandLine;
using Microsoft.Extensions.Logging;

namespace Azure.Tools.GeneratorAgent
{
    /// <summary>
    /// Configures the command line interface for the Azure SDK Generator Agent.
    /// </summary>
    internal sealed class CommandLineConfiguration
    {
        private const int ExitCodeSuccess = 0;
        private const int ExitCodeFailure = 1;
        
        private readonly ILogger<CommandLineConfiguration> Logger;

        public CommandLineConfiguration(ILogger<CommandLineConfiguration> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates and configures the root command for the application.
        /// </summary>
        /// <param name="handler">The handler function to execute when the command is invoked.</param>
        /// <returns>The configured root command.</returns>
        public RootCommand CreateRootCommand(Func<string?, string?, string?, string, Task<int>> handler)
        {
            RootCommand rootCommand = new("Azure SDK Generator Agent");

            Option<string?> typespecPathOption = new(
                "--typespec-path",
                "Path to the local TypeSpec project directory");

            Option<string?> commitIdOption = new(
                "--commit-id",
                "GitHub commit ID to generate SDK from");

            Option<string?> typespecSpecDirectoryOption = new(
                "--typespec-spec-directory",
                "TypeSpec specification directory (e.g., specification/testservice/TestService). Required when using --commit-id.");

            Option<string> sdkOutputPathOption = new(
                "--sdk-path",
                "Output directory for generated SDK files")
            {
                IsRequired = true
            };

            rootCommand.AddOption(typespecPathOption);
            rootCommand.AddOption(commitIdOption);
            rootCommand.AddOption(typespecSpecDirectoryOption);
            rootCommand.AddOption(sdkOutputPathOption);

            rootCommand.SetHandler(handler,
                typespecPathOption,
                commitIdOption,
                typespecSpecDirectoryOption,
                sdkOutputPathOption);

            return rootCommand;
        }

        internal int ValidateInput(string? typespecPath, string? commitId, string? typespecSpecDirectory)
        {
            if (string.IsNullOrWhiteSpace(typespecPath) && string.IsNullOrWhiteSpace(commitId))
            {
                Logger.LogError("Either --typespec-path or --commit-id must be specified");
                return ExitCodeFailure;
            }

            if (!string.IsNullOrWhiteSpace(typespecPath) && !string.IsNullOrWhiteSpace(commitId))
            {
                Logger.LogError("Options --typespec-path and --commit-id are mutually exclusive. Specify only one");
                return ExitCodeFailure;
            }

            if (!string.IsNullOrWhiteSpace(commitId) && string.IsNullOrWhiteSpace(typespecSpecDirectory))
            {
                Logger.LogError("Option --typespec-spec-directory is required when using --commit-id");
                return ExitCodeFailure;
            }

            Logger.LogInformation("Input validation completed successfully");
            return ExitCodeSuccess;
        } 
    }
}
