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
        public RootCommand CreateRootCommand(Func<string?, string?, string, Task<int>> handler)
        {
            RootCommand rootCommand = new("Azure SDK Generator Agent");

            Option<string?> typespecPathOption = new(
                new[] { "--typespec-path", "-t" },
                "Path to the local TypeSpec project directory or TypeSpec specification directory (e.g., specification/testservice/TestService)")
            {
                IsRequired = true
            };

            Option<string?> commitIdOption = new(
                new[] { "--commit-id", "-c" },
                "GitHub commit ID to generate SDK from (optional, used with --typespec-path for GitHub generation)");

            Option<string> sdkOutputPathOption = new(
                new[] { "--sdk-path", "-o" },
                "Output directory for generated SDK files")
            {
                IsRequired = true
            };

            rootCommand.AddOption(typespecPathOption);
            rootCommand.AddOption(commitIdOption);
            rootCommand.AddOption(sdkOutputPathOption);

            rootCommand.SetHandler(handler,
                typespecPathOption,
                commitIdOption,
                sdkOutputPathOption);

            return rootCommand;
        }

        internal int ValidateInput(string? typespecPath, string? commitId)
        {
            if (string.IsNullOrWhiteSpace(typespecPath))
            {
                Logger.LogError("Option --typespec-path is required");
                return ExitCodeFailure;
            }

            Logger.LogInformation("Input validation completed successfully");
            return ExitCodeSuccess;
        } 
    }
}
