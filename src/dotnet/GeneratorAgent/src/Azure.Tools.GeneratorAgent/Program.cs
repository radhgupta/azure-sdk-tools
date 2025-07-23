using System.CommandLine;
using Azure.AI.Agents.Persistent;
using Azure.Core;
using Azure.Identity;
using Azure.Tools.GeneratorAgent.Authentication;
using Azure.Tools.GeneratorAgent.Configuration;
using Microsoft.Extensions.Logging;

namespace Azure.Tools.GeneratorAgent
{
    public class Program
    {
        private const int ExitCodeSuccess = 0;
        private const int ExitCodeFailure = 1;

        private readonly ToolConfiguration ToolConfig;
        private readonly ILoggerFactory LoggerFactory;
        private readonly ILogger<Program> Logger;
        private readonly CommandLineConfiguration CommandLineConfig;

        internal Program(ToolConfiguration toolConfig, ILoggerFactory loggerFactory)
        {
            ToolConfig = toolConfig ?? throw new ArgumentNullException(nameof(toolConfig));
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            Logger = LoggerFactory.CreateLogger<Program>();
            CommandLineConfig = new CommandLineConfiguration(LoggerFactory.CreateLogger<CommandLineConfiguration>());
        }

        public static async Task<int> Main(string[] args)
        {
            ToolConfiguration toolConfig = new();
            using ILoggerFactory loggerFactory = toolConfig.CreateLoggerFactory();

            Program program = new(toolConfig, loggerFactory);
            return await program.RunAsync(args).ConfigureAwait(false);
        }

        internal async Task<int> RunAsync(string[] args)
        {
            RootCommand rootCommand = CommandLineConfig.CreateRootCommand(HandleCommandAsync);
            return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
        }

        internal async Task<int> HandleCommandAsync(string? typespecPath, string? commitId, string? typespecSpecDirectory, string sdkPath)
        {
            int validationResult = CommandLineConfig.ValidateInput(typespecPath, commitId, typespecSpecDirectory);
            if (validationResult != ExitCodeSuccess)
            {
                return validationResult;
            }

            using CancellationTokenSource cancellationTokenSource = new();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                Logger.LogInformation("Cancellation requested by user");
                cancellationTokenSource.Cancel();
                eventArgs.Cancel = true;
            };

            try
            {
                return await ExecuteGenerationAsync(typespecPath, commitId, typespecSpecDirectory, sdkPath, cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("Operation was cancelled. Shutting down gracefully");
                return ExitCodeSuccess;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error occurred during command execution");
                return ExitCodeFailure;
            }
        }

        private async Task<int> ExecuteGenerationAsync(
            string? typespecPath,
            string? commitId,
            string? typespecSpecDirectory,
            string sdkOutputPath,
            CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogInformation("Starting SDK generation process");

                AppSettings appSettings = ToolConfig.CreateAppSettings();

                RuntimeEnvironment environment = DetermineRuntimeEnvironment();
                TokenCredentialOptions? credentialOptions = CreateCredentialOptions();

                CredentialFactory credentialFactory = new(LoggerFactory.CreateLogger<CredentialFactory>());
                TokenCredential credential = credentialFactory.CreateCredential(environment, credentialOptions);

                ProcessExecutor processExecutor = new(LoggerFactory.CreateLogger<ProcessExecutor>());
                PersistentAgentsAdministrationClient adminClient = new(
                    new Uri(appSettings.ProjectEndpoint),
                    credential);

                ISdkGenerationService sdkGenerationService = !string.IsNullOrWhiteSpace(typespecPath)
                    ? SdkGenerationServiceFactory.CreateForLocalPath(typespecPath, sdkOutputPath, appSettings, LoggerFactory, processExecutor)
                    : SdkGenerationServiceFactory.CreateForGitHubCommit(commitId!, typespecSpecDirectory!, sdkOutputPath, appSettings, LoggerFactory, processExecutor);

                Logger.LogInformation("Initializing error fixing agent");
                await using ErrorFixerAgent agent = new(
                    appSettings,
                    LoggerFactory.CreateLogger<ErrorFixerAgent>(),
                    adminClient);

                await agent.FixCodeAsync(cancellationToken).ConfigureAwait(false);
                Logger.LogInformation("Error fixing agent completed successfully");

                Logger.LogInformation("Starting TypeSpec compilation");
                bool success = await sdkGenerationService.CompileTypeSpecAsync(cancellationToken).ConfigureAwait(false);

                if (success)
                {
                    Logger.LogInformation("SDK generation completed successfully");
                    return ExitCodeSuccess;
                }
                else
                {
                    Logger.LogError("SDK generation failed");
                    return ExitCodeFailure;
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("SDK generation was cancelled");
                return ExitCodeSuccess;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error occurred during SDK generation");
                return ExitCodeFailure;
            }
        }

        private static RuntimeEnvironment DetermineRuntimeEnvironment()
        {
            bool isGitHubActions = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvironmentVariables.GitHubActions)) ||
                                 !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvironmentVariables.GitHubWorkflow));

            if (isGitHubActions)
            {
                return RuntimeEnvironment.DevOpsPipeline;
            }

            return RuntimeEnvironment.LocalDevelopment;
        }

        private static TokenCredentialOptions? CreateCredentialOptions()
        {
            string? tenantId = Environment.GetEnvironmentVariable(EnvironmentVariables.AzureTenantId);
            Uri? authorityHost = null;

            string? authority = Environment.GetEnvironmentVariable(EnvironmentVariables.AzureAuthorityHost);
            if (!string.IsNullOrEmpty(authority) && Uri.TryCreate(authority, UriKind.Absolute, out Uri? parsedAuthority))
            {
                authorityHost = parsedAuthority;
            }

            if (tenantId == null && authorityHost == null)
            {
                return null;
            }

            var options = new TokenCredentialOptions();

            if (authorityHost != null)
            {
                options.AuthorityHost = authorityHost;
            }

            return options;
        }
    }
}
