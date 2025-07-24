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
    public class LocalTypeSpecSdkGenerationServiceTests
    {
        private const string ValidTypeSpecPath = @"C:\source\typespec-project";
        private const string ValidSdkOutputDirectory = @"C:\azure-sdk-for-net\sdk\compute";
        private const string ValidCommandExecutor = "cmd.exe";
        private const string ValidCommandPrefix = "/c";
        private const string ValidTypespecEmitterPackage = "@typespec/http-client-csharp";
        private const string ValidTspOutputDirectoryName = "tsp-output";
        private const string ValidTypeSpecDirectoryName = "@typespec";
        private const string ValidHttpClientCSharpDirectoryName = "http-client-csharp";

        private class TestableLocalTypeSpecSdkGenerationService : LocalTypeSpecSdkGenerationService
        {
            public bool ShouldDirectoryExist { get; set; } = true;
            public bool ShouldFileExist { get; set; } = true;
            public string[] FilesToReturn { get; set; } = new[] { "file1.cs", "file2.cs" };
            public string[] DirectoriesToReturn { get; set; } = new[] { "Models", "Generated" };

            public TestableLocalTypeSpecSdkGenerationService(
                AppSettings appSettings,
                ILogger<LocalTypeSpecSdkGenerationService> logger,
                ProcessExecutor processExecutor,
                string typeSpecSourcePath,
                string sdkOutputDirectory)
                : base(appSettings, logger, processExecutor, typeSpecSourcePath, sdkOutputDirectory)
            {
            }

            protected override bool DirectoryExists(string path) => ShouldDirectoryExist;
            protected override bool FileExists(string path) => ShouldFileExist;
            protected override void CreateDirectory(string path) { }
            protected override void DeleteDirectory(string path, bool recursive) { }
            protected override string[] GetFiles(string path) => FilesToReturn;
            protected override string[] GetDirectories(string path) => DirectoriesToReturn;
            protected override void MoveFile(string sourceFileName, string destFileName) { }
            protected override void ReplaceFile(string sourceFileName, string destinationFileName, string destinationBackupFileName) { }
            protected override void DeleteFile(string path) { }
            protected override void MoveDirectory(string sourceDirName, string destDirName) { }

            public async Task<bool> TestInstallTypeSpecDependencies(CancellationToken cancellationToken)
                => await base.InstallTypeSpecDependencies(cancellationToken);

            public async Task<bool> TestCompileTypeSpec(CancellationToken cancellationToken)
                => await base.CompileTypeSpec(cancellationToken);

            public bool TestMoveGeneratedFilesAndCleanup(CancellationToken cancellationToken)
                => base.MoveGeneratedFilesAndCleanup(cancellationToken);
        }

        [Test]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var service = new LocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidTypeSpecPath,
                ValidSdkOutputDirectory);

            Assert.That(service, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithNullAppSettings_ThrowsArgumentNullException()
        {
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var ex = Assert.Throws<ArgumentNullException>(() => new LocalTypeSpecSdkGenerationService(
                null!,
                logger.Object,
                processExecutor.Object,
                ValidTypeSpecPath,
                ValidSdkOutputDirectory));

            Assert.That(ex.ParamName, Is.EqualTo("appSettings"));
        }

        [Test]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            var appSettings = CreateValidAppSettings();
            var processExecutor = CreateMockProcessExecutor();

            var ex = Assert.Throws<ArgumentNullException>(() => new LocalTypeSpecSdkGenerationService(
                appSettings,
                null!,
                processExecutor.Object,
                ValidTypeSpecPath,
                ValidSdkOutputDirectory));

            Assert.That(ex.ParamName, Is.EqualTo("logger"));
        }

        [Test]
        public void Constructor_WithNullProcessExecutor_ThrowsArgumentNullException()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();

            var ex = Assert.Throws<ArgumentNullException>(() => new LocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                null!,
                ValidTypeSpecPath,
                ValidSdkOutputDirectory));

            Assert.That(ex.ParamName, Is.EqualTo("processExecutor"));
        }

        [Test]
        public void Constructor_WithNullTypeSpecSourcePath_ThrowsArgumentNullException()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var ex = Assert.Throws<ArgumentNullException>(() => new LocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                null!,
                ValidSdkOutputDirectory));

            Assert.That(ex.ParamName, Is.EqualTo("typeSpecSourcePath"));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t\n")]
        public void Constructor_WithInvalidTypeSpecSourcePath_ThrowsArgumentException(string invalidPath)
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var ex = Assert.Throws<ArgumentException>(() => new LocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                invalidPath,
                ValidSdkOutputDirectory));

            Assert.That(ex.ParamName, Is.EqualTo("typeSpecSourcePath"));
        }

        [Test]
        public void Constructor_WithNullSdkOutputDirectory_ThrowsArgumentNullException()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var ex = Assert.Throws<ArgumentNullException>(() => new LocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidTypeSpecPath,
                null!));

            Assert.That(ex.ParamName, Is.EqualTo("sdkOutputDirectory"));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t\n")]
        public void Constructor_WithInvalidSdkOutputDirectory_ThrowsArgumentException(string invalidPath)
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var ex = Assert.Throws<ArgumentException>(() => new LocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidTypeSpecPath,
                invalidPath));

            Assert.That(ex.ParamName, Is.EqualTo("sdkOutputDirectory"));
        }

        [Test]
        public async Task CompileTypeSpecAsync_WithSuccessfulExecution_ReturnsTrue()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            SetupSuccessfulProcessExecution(processExecutor);

            var service = new TestableLocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidTypeSpecPath,
                ValidSdkOutputDirectory);

            var result = await service.CompileTypeSpecAsync();

            Assert.That(result, Is.True);
            VerifyExpectedLogging(logger);
        }

        [Test]
        public async Task CompileTypeSpecAsync_WithMissingTypeSpecDirectory_ReturnsFalse()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var service = new TestableLocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidTypeSpecPath,
                ValidSdkOutputDirectory)
            {
                ShouldDirectoryExist = false
            };

            var result = await service.CompileTypeSpecAsync();
            
            Assert.That(result, Is.False);
            VerifyErrorLogging(logger, "Unexpected error during TypeSpec compilation");
        }

        [Test]
        public async Task CompileTypeSpecAsync_WithDependencyInstallationFailure_ReturnsFalse()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            SetupFailedNpmInstallExecution(processExecutor);

            var service = new TestableLocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidTypeSpecPath,
                ValidSdkOutputDirectory);

            var result = await service.CompileTypeSpecAsync();

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CompileTypeSpecAsync_WithTypeSpecCompilationFailure_ReturnsFalse()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            SetupFailedTypeSpecCompilationExecution(processExecutor);

            var service = new TestableLocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidTypeSpecPath,
                ValidSdkOutputDirectory);

            var result = await service.CompileTypeSpecAsync();

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CompileTypeSpecAsync_WithFileMovementFailure_ReturnsFalse()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            SetupSuccessfulProcessExecution(processExecutor);

            var service = new TestableLocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidTypeSpecPath,
                ValidSdkOutputDirectory)
            {
                ShouldDirectoryExist = false
            };

            var result = await service.CompileTypeSpecAsync();

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CompileTypeSpecAsync_WithCancellation_ReturnsFalse()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var service = new TestableLocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidTypeSpecPath,
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

            var service = new TestableLocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidTypeSpecPath,
                ValidSdkOutputDirectory);

            var result = await service.CompileTypeSpecAsync();

            Assert.That(result, Is.False);
            VerifyErrorLogging(logger, "Unexpected error during TypeSpec compilation");
        }

        [Test]
        public async Task InstallTypeSpecDependencies_WithSuccessfulExecution_ReturnsTrue()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            SetupSuccessfulNpmInstallExecution(processExecutor);

            var service = new TestableLocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidTypeSpecPath,
                ValidSdkOutputDirectory);

            var result = await service.TestInstallTypeSpecDependencies(CancellationToken.None);

            Assert.That(result, Is.True);
            VerifyInformationLogging(logger, "Installing TypeSpec dependencies");
            VerifyInformationLogging(logger, "TypeSpec dependencies installed successfully");
        }

        [Test]
        public async Task InstallTypeSpecDependencies_WithFailedExecution_ReturnsFalse()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            SetupFailedNpmInstallExecution(processExecutor);

            var service = new TestableLocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidTypeSpecPath,
                ValidSdkOutputDirectory);

            var result = await service.TestInstallTypeSpecDependencies(CancellationToken.None);

            Assert.That(result, Is.False);
            VerifyErrorLogging(logger, "npm install failed");
        }

        [Test]
        public async Task InstallTypeSpecDependencies_WithCorrectArguments_CallsProcessExecutorWithExpectedParameters()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            SetupSuccessfulNpmInstallExecution(processExecutor);

            var service = new TestableLocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidTypeSpecPath,
                ValidSdkOutputDirectory);

            await service.TestInstallTypeSpecDependencies(CancellationToken.None);

            processExecutor.Verify(x => x.ExecuteAsync(
                ValidCommandExecutor,
                $"{ValidCommandPrefix} npm install --save-dev {ValidTypespecEmitterPackage}",
                ValidTypeSpecPath,
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()), Times.Once);
        }

        [Test]
        public async Task CompileTypeSpec_WithSuccessfulExecution_ReturnsTrue()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            SetupSuccessfulTypeSpecCompilationExecution(processExecutor);

            var service = new TestableLocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidTypeSpecPath,
                ValidSdkOutputDirectory);

            var result = await service.TestCompileTypeSpec(CancellationToken.None);

            Assert.That(result, Is.True);
            VerifyInformationLogging(logger, "Compiling TypeSpec project");
            VerifyInformationLogging(logger, "TypeSpec compilation completed");
        }

        [Test]
        public async Task CompileTypeSpec_WithCorrectArguments_CallsProcessExecutorWithExpectedParameters()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            SetupSuccessfulTypeSpecCompilationExecution(processExecutor);

            var service = new TestableLocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidTypeSpecPath,
                ValidSdkOutputDirectory);

            await service.TestCompileTypeSpec(CancellationToken.None);

            var expectedTspOutputPath = Path.Combine(ValidSdkOutputDirectory, ValidTspOutputDirectoryName);
            processExecutor.Verify(x => x.ExecuteAsync(
                ValidCommandExecutor,
                $"{ValidCommandPrefix} npx tsp compile . --emit {ValidTypespecEmitterPackage} --output-dir \"{expectedTspOutputPath}\"",
                ValidTypeSpecPath,
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()), Times.Once);
        }

        [Test]
        public void MoveGeneratedFilesAndCleanup_WithSuccessfulExecution_ReturnsTrue()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var service = new TestableLocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidTypeSpecPath,
                ValidSdkOutputDirectory);

            var result = service.TestMoveGeneratedFilesAndCleanup(CancellationToken.None);

            Assert.That(result, Is.True);
            VerifyInformationLogging(logger, "Moving generated files and cleaning up");
            VerifyInformationLogging(logger, "Successfully moved");
        }

        [Test]
        public void MoveGeneratedFilesAndCleanup_WithMissingGeneratedDirectory_ReturnsFalse()
        {
            var appSettings = CreateValidAppSettings();
            var logger = CreateMockLogger();
            var processExecutor = CreateMockProcessExecutor();

            var service = new TestableLocalTypeSpecSdkGenerationService(
                appSettings,
                logger.Object,
                processExecutor.Object,
                ValidTypeSpecPath,
                ValidSdkOutputDirectory)
            {
                ShouldDirectoryExist = false
            };

            var result = service.TestMoveGeneratedFilesAndCleanup(CancellationToken.None);

            Assert.That(result, Is.False);
            VerifyErrorLogging(logger, "Generated source directory not found");
        }

        private static AppSettings CreateValidAppSettings()
        {
            var mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            var mockSection = new Mock<Microsoft.Extensions.Configuration.IConfigurationSection>();

            mockSection.Setup(x => x.Value).Returns((string?)null);
            mockConfiguration.Setup(x => x.GetSection(It.IsAny<string>())).Returns(mockSection.Object);

            return new AppSettings(mockConfiguration.Object);
        }

        private static Mock<ILogger<LocalTypeSpecSdkGenerationService>> CreateMockLogger()
        {
            return new Mock<ILogger<LocalTypeSpecSdkGenerationService>>();
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

        private static void SetupSuccessfulNpmInstallExecution(Mock<ProcessExecutor> processExecutor)
        {
            processExecutor.Setup(x => x.ExecuteAsync(
                    ValidCommandExecutor,
                    It.Is<string>(args => args.Contains("npm install")),
                    ValidTypeSpecPath,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()))
                .ReturnsAsync((true, "npm install succeeded", string.Empty));
        }

        private static void SetupFailedNpmInstallExecution(Mock<ProcessExecutor> processExecutor)
        {
            processExecutor.Setup(x => x.ExecuteAsync(
                    ValidCommandExecutor,
                    It.Is<string>(args => args.Contains("npm install")),
                    ValidTypeSpecPath,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()))
                .ReturnsAsync((false, "npm install output", "npm install failed"));
        }

        private static void SetupSuccessfulTypeSpecCompilationExecution(Mock<ProcessExecutor> processExecutor)
        {
            processExecutor.Setup(x => x.ExecuteAsync(
                    ValidCommandExecutor,
                    It.Is<string>(args => args.Contains("npx tsp compile")),
                    ValidTypeSpecPath,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()))
                .ReturnsAsync((true, "TypeSpec compilation succeeded", string.Empty));
        }

        private static void SetupFailedTypeSpecCompilationExecution(Mock<ProcessExecutor> processExecutor)
        {
            processExecutor.SetupSequence(x => x.ExecuteAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()))
                .ReturnsAsync((true, "npm install success", string.Empty))
                .ReturnsAsync((false, "TypeSpec compilation output", "TypeSpec compilation failed"));
        }

        private static void VerifyExpectedLogging(Mock<ILogger<LocalTypeSpecSdkGenerationService>> logger)
        {
            VerifyInformationLogging(logger, $"Starting TypeSpec compilation for project: {ValidTypeSpecPath}");
            VerifyInformationLogging(logger, $"Output SDK path: {ValidSdkOutputDirectory}");
            VerifyInformationLogging(logger, "TypeSpec compilation completed successfully");
        }

        private static void VerifyErrorLogging(Mock<ILogger<LocalTypeSpecSdkGenerationService>> logger, string expectedMessage)
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

        private static void VerifyInformationLogging(Mock<ILogger<LocalTypeSpecSdkGenerationService>> logger, string expectedMessage)
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
