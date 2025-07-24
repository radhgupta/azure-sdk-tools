using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Tools.GeneratorAgent;
using Azure.Tools.GeneratorAgent.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Azure.Tools.GeneratorAgent.Tests
{
    [TestFixture]
    public class GitHubTypeSpecSdkGenerationServiceTests
    {
        private const string ValidCommitId = "abc123def456";
        private const string ValidTypeSpecDirectory = "specification/compute/data-plane";
        private const string ValidAzureSdkPath = @"C:\azure-sdk-for-net";
        private const string ValidSdkOutputDirectory = @"C:\azure-sdk-for-net\sdk\compute";
        private const string ValidScriptPath = @"eng\scripts\automation\Invoke-TypeSpecDataPlaneGenerateSDKPackage.ps1";
        private const string ValidSrcDirectory = @"C:\azure-sdk-for-net\sdk\compute\src";
        private const string DefaultRepository = "azure-rest-api-specs";

        private class TestableGitHubTypeSpecSdkGenerationService : GitHubTypeSpecSdkGenerationService
        {
            public bool ShouldFileExist { get; set; } = true;
            public bool ShouldDirectoryExist { get; set; } = true;
            public bool ShouldCallBaseExtractAzureSdkPath { get; set; } = false;

            public TestableGitHubTypeSpecSdkGenerationService(
                AppSettings appSettings,
                ILogger<GitHubTypeSpecSdkGenerationService> logger,
                ProcessExecutor processExecutor,
                string commitId,
                string typespecSpecDirectory,
                string sdkOutputDirectory)
                : base(appSettings, logger, processExecutor, commitId, typespecSpecDirectory, sdkOutputDirectory)
            {
            }

            protected override bool FileExists(string path) => ShouldFileExist;
            protected override bool DirectoryExists(string path) => ShouldDirectoryExist;

            public string TestExtractAzureSdkPath()
            {
                return ShouldCallBaseExtractAzureSdkPath ? base.ExtractAzureSdkPath() : ValidAzureSdkPath;
            }

            public async Task<bool> TestRunPowerShellGenerationScript(string azureSdkPath, CancellationToken cancellationToken)
                => await base.RunPowerShellGenerationScript(azureSdkPath, cancellationToken);

            public async Task<bool> TestRunDotNetBuildGenerateCode(CancellationToken cancellationToken)
                => await base.RunDotNetBuildGenerateCode(cancellationToken);
        }

        [Test]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var service = new GitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidCommitId,
                ValidTypeSpecDirectory,
                ValidSdkOutputDirectory);

            Assert.That(service, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithNullAppSettings_ThrowsArgumentNullException()
        {
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var ex = Assert.Throws<ArgumentNullException>(() => new GitHubTypeSpecSdkGenerationService(
                null!,
                logger.Object,
                processExecutor.Object,
                ValidCommitId,
                ValidTypeSpecDirectory,
                ValidSdkOutputDirectory));

            Assert.That(ex.ParamName, Is.EqualTo("appSettings"));
        }

        [Test]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            var appSettings = CreateValidAppSettings();
            var processExecutor = CreateMockProcessExecutor();

            var ex = Assert.Throws<ArgumentNullException>(() => new GitHubTypeSpecSdkGenerationService(
                appSettings,
                null!,
                processExecutor.Object,
                ValidCommitId,
                ValidTypeSpecDirectory,
                ValidSdkOutputDirectory));

            Assert.That(ex.ParamName, Is.EqualTo("logger"));
        }

        [Test]
        public void Constructor_WithNullProcessExecutor_ThrowsArgumentNullException()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();

            var ex = Assert.Throws<ArgumentNullException>(() => new GitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                null!,
                ValidCommitId,
                ValidTypeSpecDirectory,
                ValidSdkOutputDirectory));

            Assert.That(ex.ParamName, Is.EqualTo("processExecutor"));
        }

        [Test]
        public void Constructor_WithNullCommitId_ThrowsArgumentNullException()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var ex = Assert.Throws<ArgumentNullException>(() => new GitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                null!,
                ValidTypeSpecDirectory,
                ValidSdkOutputDirectory));

            Assert.That(ex.ParamName, Is.EqualTo("commitId"));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t\n")]
        public void Constructor_WithInvalidCommitId_ThrowsArgumentException(string invalidCommitId)
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var ex = Assert.Throws<ArgumentException>(() => new GitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                invalidCommitId,
                ValidTypeSpecDirectory,
                ValidSdkOutputDirectory));

            Assert.That(ex.ParamName, Is.EqualTo("commitId"));
        }

        [Test]
        public void Constructor_WithNullTypespecSpecDirectory_ThrowsArgumentNullException()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var ex = Assert.Throws<ArgumentNullException>(() => new GitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidCommitId,
                null!,
                ValidSdkOutputDirectory));

            Assert.That(ex.ParamName, Is.EqualTo("typespecSpecDirectory"));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t\n")]
        public void Constructor_WithInvalidTypespecSpecDirectory_ThrowsArgumentException(string invalidDirectory)
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var ex = Assert.Throws<ArgumentException>(() => new GitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidCommitId,
                invalidDirectory,
                ValidSdkOutputDirectory));

            Assert.That(ex.ParamName, Is.EqualTo("typespecSpecDirectory"));
        }

        [Test]
        public async Task CompileTypeSpecAsync_WithSuccessfulExecution_ReturnsTrue()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            SetupSuccessfulProcessExecution(processExecutor);

            var service = new TestableGitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidCommitId,
                ValidTypeSpecDirectory,
                ValidSdkOutputDirectory);

            var result = await service.CompileTypeSpecAsync();

            Assert.That(result, Is.True);
            VerifyExpectedLogging(logger);
        }

        [Test]
        public async Task CompileTypeSpecAsync_WithPowerShellScriptFailure_ReturnsFalse()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            SetupFailedPowerShellExecution(processExecutor);

            var service = new TestableGitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidCommitId,
                ValidTypeSpecDirectory,
                ValidSdkOutputDirectory);

            var result = await service.CompileTypeSpecAsync();

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CompileTypeSpecAsync_WithDotNetBuildFailure_ReturnsFalse()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            SetupFailedDotNetBuildExecution(processExecutor);

            var service = new TestableGitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidCommitId,
                ValidTypeSpecDirectory,
                ValidSdkOutputDirectory);

            var result = await service.CompileTypeSpecAsync();

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CompileTypeSpecAsync_WithCancellation_HandlesCancellationGracefully()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var service = new TestableGitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidCommitId,
                ValidTypeSpecDirectory,
                ValidSdkOutputDirectory);

            var result = await service.CompileTypeSpecAsync(cts.Token);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CompileTypeSpecAsync_WithUnexpectedException_ReturnsFalseAndLogsError()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var expectedException = new InvalidOperationException("Unexpected error occurred");
            processExecutor.Setup(x => x.ExecuteAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()))
                .ThrowsAsync(expectedException);

            var service = new TestableGitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidCommitId,
                ValidTypeSpecDirectory,
                ValidSdkOutputDirectory);

            var result = await service.CompileTypeSpecAsync();

            Assert.That(result, Is.False);
            VerifyErrorLogging(logger, "Unexpected error during GitHub-based TypeSpec compilation");
        }

        [Test]
        public void ExtractAzureSdkPath_WithValidPath_ReturnsCorrectPath()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var service = new TestableGitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidCommitId,
                ValidTypeSpecDirectory,
                ValidSdkOutputDirectory);

            var result = service.TestExtractAzureSdkPath();

            Assert.That(result, Is.EqualTo(ValidAzureSdkPath));
        }

        [TestCase(@"C:\azure-sdk-for-net\sdk\compute\Azure.Compute")]
        [TestCase(@"C:\azure-sdk-for-net\sdk\storage")]
        [TestCase(@"C:\azure-sdk-for-net\tools\generate")]
        public void ExtractAzureSdkPath_WithVariousValidPaths_ReturnsAzureSdkRoot(string sdkOutputPath)
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var service = new TestableGitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidCommitId,
                ValidTypeSpecDirectory,
                sdkOutputPath)
            {
                ShouldCallBaseExtractAzureSdkPath = true
            };

            var result = service.TestExtractAzureSdkPath();

            Assert.That(result, Is.EqualTo(@"C:\azure-sdk-for-net"));
        }

        [Test]
        public async Task RunPowerShellGenerationScript_WithMissingScript_ReturnsFalse()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var service = new TestableGitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidCommitId,
                ValidTypeSpecDirectory,
                ValidSdkOutputDirectory)
            {
                ShouldFileExist = false
            };

            var result = await service.TestRunPowerShellGenerationScript(ValidAzureSdkPath, CancellationToken.None);

            Assert.That(result, Is.False);
            VerifyInformationLogging(logger, "Running PowerShell generation script");
            VerifyErrorLogging(logger, "PowerShell script not found");
        }

        [Test]
        public async Task RunPowerShellGenerationScript_WithSuccessfulExecution_ReturnsTrue()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            SetupSuccessfulPowerShellExecution(processExecutor);

            var service = new TestableGitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidCommitId,
                ValidTypeSpecDirectory,
                ValidSdkOutputDirectory);

            var result = await service.TestRunPowerShellGenerationScript(ValidAzureSdkPath, CancellationToken.None);

            Assert.That(result, Is.True);
            VerifyInformationLogging(logger, "PowerShell generation script completed successfully");
        }

        [Test]
        public async Task RunPowerShellGenerationScript_WithCorrectArguments_CallsProcessExecutorWithExpectedParameters()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            SetupSuccessfulPowerShellExecution(processExecutor);

            var service = new TestableGitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidCommitId,
                ValidTypeSpecDirectory,
                ValidSdkOutputDirectory);

            await service.TestRunPowerShellGenerationScript(ValidAzureSdkPath, CancellationToken.None);

            processExecutor.Verify(x => x.ExecuteAsync(
                "cmd.exe",
                It.Is<string>(args =>
                    args.Contains($"-sdkFolder \"{ValidSdkOutputDirectory}\"") &&
                    args.Contains($"-typespecSpecDirectory \"{ValidTypeSpecDirectory}\"") &&
                    args.Contains($"-commit \"{ValidCommitId}\"") &&
                    args.Contains($"-repo \"{DefaultRepository}\"")),
                ValidAzureSdkPath,
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()), Times.Once);
        }

        [Test]
        public async Task RunPowerShellGenerationScript_WithFailedExecution_LogsAppropriateErrors()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();
            const string expectedErrorOutput = "PowerShell execution failed";
            const string expectedStandardOutput = "Some diagnostic output";

            processExecutor.Setup(x => x.ExecuteAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()))
                .ReturnsAsync((false, expectedStandardOutput, expectedErrorOutput));

            var service = new TestableGitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidCommitId,
                ValidTypeSpecDirectory,
                ValidSdkOutputDirectory);

            var result = await service.TestRunPowerShellGenerationScript(ValidAzureSdkPath, CancellationToken.None);

            Assert.That(result, Is.False);
            VerifyErrorLogging(logger, $"PowerShell generation script failed. Error: {expectedErrorOutput}");
            VerifyErrorLogging(logger, $"PowerShell script standard output: {expectedStandardOutput}");
        }

        [Test]
        public async Task RunDotNetBuildGenerateCode_WithMissingSourceDirectory_ReturnsFalse()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var service = new TestableGitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidCommitId,
                ValidTypeSpecDirectory,
                ValidSdkOutputDirectory)
            {
                ShouldDirectoryExist = false
            };

            var result = await service.TestRunDotNetBuildGenerateCode(CancellationToken.None);

            Assert.That(result, Is.False);
            VerifyInformationLogging(logger, "Running dotnet build /t:generateCode");
            VerifyErrorLogging(logger, "Source directory not found");
        }

        [Test]
        public async Task RunDotNetBuildGenerateCode_WithSuccessfulBuild_ReturnsTrue()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();
            const string expectedBuildOutput = "Build succeeded.";

            SetupSuccessfulDotNetBuildExecution(processExecutor, expectedBuildOutput);

            var service = new TestableGitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidCommitId,
                ValidTypeSpecDirectory,
                ValidSdkOutputDirectory);

            var result = await service.TestRunDotNetBuildGenerateCode(CancellationToken.None);

            Assert.That(result, Is.True);
            VerifyInformationLogging(logger, "dotnet build /t:generateCode completed successfully");
            VerifyInformationLogging(logger, $"dotnet build output: {expectedBuildOutput}");
        }

        [Test]
        public async Task RunDotNetBuildGenerateCode_WithCorrectArguments_CallsProcessExecutorWithExpectedParameters()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            SetupSuccessfulDotNetBuildExecution(processExecutor);

            var service = new TestableGitHubTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidCommitId,
                ValidTypeSpecDirectory,
                ValidSdkOutputDirectory);

            await service.TestRunDotNetBuildGenerateCode(CancellationToken.None);

            processExecutor.Verify(x => x.ExecuteAsync(
                "cmd.exe",
                "/c dotnet build /t:generateCode",
                ValidSrcDirectory,
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()), Times.Once);
        }

        private static AppSettings CreateValidAppSettings()
        {
            var mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            var mockSection = new Mock<Microsoft.Extensions.Configuration.IConfigurationSection>();

            mockSection.Setup(x => x.Value).Returns((string?)null);
            mockConfiguration.Setup(x => x.GetSection(It.IsAny<string>())).Returns(mockSection.Object);

            return new AppSettings(mockConfiguration.Object);
        }

        private static Mock<ILogger<GitHubTypeSpecSdkGenerationService>> CreateMockLogger()
        {
            return new Mock<ILogger<GitHubTypeSpecSdkGenerationService>>();
        }

        private static Mock<ProcessExecutor> CreateMockProcessExecutor()
        {
            var mockLogger = new Mock<ILogger<ProcessExecutor>>();
            return new Mock<ProcessExecutor>(mockLogger.Object);
        }

        private static void SetupSuccessfulProcessExecution(Mock<ProcessExecutor> processExecutor)
        {
            processExecutor.Setup(x => x.ExecuteAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()))
                .ReturnsAsync((true, "Success", string.Empty));
        }

        private static void SetupSuccessfulPowerShellExecution(Mock<ProcessExecutor> processExecutor)
        {
            processExecutor.Setup(x => x.ExecuteAsync(
                    "cmd.exe",
                    It.Is<string>(args => args.Contains("pwsh")),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()))
                .ReturnsAsync((true, "PowerShell succeeded", string.Empty));
        }

        private static void SetupSuccessfulDotNetBuildExecution(Mock<ProcessExecutor> processExecutor, string output = "Build succeeded")
        {
            processExecutor.Setup(x => x.ExecuteAsync(
                    "cmd.exe",
                    "/c dotnet build /t:generateCode",
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()))
                .ReturnsAsync((true, output, string.Empty));
        }

        private static void SetupFailedPowerShellExecution(Mock<ProcessExecutor> processExecutor)
        {
            processExecutor.SetupSequence(x => x.ExecuteAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()))
                .ReturnsAsync((false, "PowerShell output", "PowerShell failed"));
        }

        private static void SetupFailedDotNetBuildExecution(Mock<ProcessExecutor> processExecutor)
        {
            processExecutor.SetupSequence(x => x.ExecuteAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()))
                .ReturnsAsync((true, "PowerShell success", string.Empty))
                .ReturnsAsync((false, "Build output", "Build failed"));
        }

        private static void VerifyExpectedLogging(Mock<ILogger<GitHubTypeSpecSdkGenerationService>> logger)
        {
            VerifyInformationLogging(logger, $"Starting GitHub-based TypeSpec compilation for commit: {ValidCommitId}");
            VerifyInformationLogging(logger, $"SDK output directory: {ValidSdkOutputDirectory}");
            VerifyInformationLogging(logger, $"TypeSpec spec directory: {ValidTypeSpecDirectory}");
        }

        private static void VerifyErrorLogging(Mock<ILogger<GitHubTypeSpecSdkGenerationService>> logger, string expectedMessage)
        {
            logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce,
                $"Expected error log message containing: {expectedMessage}");
        }

        private static void VerifyInformationLogging(Mock<ILogger<GitHubTypeSpecSdkGenerationService>> logger, string expectedMessage)
        {
            logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce,
                $"Expected information log message containing: {expectedMessage}");
        }
    }
}
