using Microsoft.Extensions.Configuration;

namespace Azure.Tools.GeneratorAgent.Configuration
{
    internal class AppSettings
    {
        private readonly IConfiguration Configuration;

        public AppSettings(IConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public string ProjectEndpoint => Configuration.GetSection("AzureSettings:ProjectEndpoint").Value ?? "";
        public string Model => Configuration.GetSection("AzureSettings:Model").Value ?? "gpt-4o";
        public string AgentName => Configuration.GetSection("AzureSettings:AgentName").Value ?? "AZC Fixer";
        public string AgentInstructions => Configuration.GetSection("AzureSettings:AgentInstructions").Value ?? "";
        public string TypespecEmitterPackage => Configuration.GetSection("AzureSettings:TypespecEmitterPackage").Value ?? "@typespec/http-client-csharp";
        public string TspOutputDirectoryName => Configuration.GetSection("AzureSettings:TspOutputDirectoryName").Value ?? "tsp-output";
        public string TypeSpecDirectoryName => Configuration.GetSection("AzureSettings:TypeSpecDirectoryName").Value ?? "@typespec";
        public string HttpClientCSharpDirectoryName => Configuration.GetSection("AzureSettings:HttpClientCSharpDirectoryName").Value ?? "http-client-csharp";
        public string CommandExecutor => Configuration.GetSection("AzureSettings:CommandExecutor").Value ?? "cmd.exe";
        public string CommandPrefix => Configuration.GetSection("AzureSettings:CommandPrefix").Value ?? "/c";
        public string AzureSpecRepository => Configuration.GetSection("AzureSettings:AzureSpecRepository").Value ?? "azure-rest-api-specs";
        public string PowerShellScriptPath => Configuration.GetSection("AzureSettings:PowerShellScriptPath").Value ?? "eng/scripts/automation/Invoke-TypeSpecDataPlaneGenerateSDKPackage.ps1";
        public string AzureSdkDirectoryName => Configuration.GetSection("AzureSettings:AzureSdkDirectoryName").Value ?? "azure-sdk-for-net";

    }
}

