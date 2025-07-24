using System.CommandLine;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Azure.Tools.GeneratorAgent.Tests
{
    [TestFixture]
    public class CommandLineConfigurationTests
    {
        private static Mock<ILogger<CommandLineConfiguration>> CreateMockLogger()
        {
            return new Mock<ILogger<CommandLineConfiguration>>();
        }

        private static CommandLineConfiguration CreateCommandLineConfiguration(Mock<ILogger<CommandLineConfiguration>> mockLogger)
        {
            return new CommandLineConfiguration(mockLogger.Object);
        }

        private static void VerifyLogError(Mock<ILogger<CommandLineConfiguration>> mockLogger, string expectedMessage)
        {
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private static void VerifyLogInformation(Mock<ILogger<CommandLineConfiguration>> mockLogger, string expectedMessage)
        {
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public void Constructor_WithValidLogger_ShouldCreateInstance()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();

            CommandLineConfiguration config = CreateCommandLineConfiguration(mockLogger);

            Assert.That(config, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new CommandLineConfiguration(null!));

            Assert.That(exception.ParamName, Is.EqualTo("logger"));
        }

        [Test]
        public void CreateRootCommand_WithValidHandler_ShouldCreateRootCommandWithCorrectDescription()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            Task<int> MockHandler(string? a, string? b, string? c, string d) => Task.FromResult(0);

            RootCommand rootCommand = commandLineConfiguration.CreateRootCommand(MockHandler);

            Assert.That(rootCommand, Is.Not.Null);
            Assert.That(rootCommand.Description, Is.EqualTo("Azure SDK Generator Agent"));
        }

        [Test]
        public void CreateRootCommand_ShouldHaveCorrectNumberOfOptions()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            Task<int> MockHandler(string? a, string? b, string? c, string d) => Task.FromResult(0);

            RootCommand rootCommand = commandLineConfiguration.CreateRootCommand(MockHandler);

            Assert.That(rootCommand.Options.Count, Is.EqualTo(4));
        }

        [Test]
        public void CreateRootCommand_ShouldHaveTypespecPathOption()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            Task<int> MockHandler(string? a, string? b, string? c, string d) => Task.FromResult(0);

            RootCommand rootCommand = commandLineConfiguration.CreateRootCommand(MockHandler);

            Option? typespecPathOption = rootCommand.Options.FirstOrDefault(o => o.Name == "typespec-path");
            Assert.That(typespecPathOption, Is.Not.Null);
            Assert.That(typespecPathOption.Description, Is.EqualTo("Path to the local TypeSpec project directory"));
            Assert.That(typespecPathOption.IsRequired, Is.False);
        }

        [Test]
        public void CreateRootCommand_ShouldHaveCommitIdOption()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            Task<int> MockHandler(string? a, string? b, string? c, string d) => Task.FromResult(0);

            RootCommand rootCommand = commandLineConfiguration.CreateRootCommand(MockHandler);

            Option? commitIdOption = rootCommand.Options.FirstOrDefault(o => o.Name == "commit-id");
            Assert.That(commitIdOption, Is.Not.Null);
            Assert.That(commitIdOption.Description, Is.EqualTo("GitHub commit ID to generate SDK from"));
            Assert.That(commitIdOption.IsRequired, Is.False);
        }

        [Test]
        public void CreateRootCommand_ShouldHaveTypespecSpecDirectoryOption()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            Task<int> MockHandler(string? a, string? b, string? c, string d) => Task.FromResult(0);

            RootCommand rootCommand = commandLineConfiguration.CreateRootCommand(MockHandler);

            Option? typespecSpecDirOption = rootCommand.Options.FirstOrDefault(o => o.Name == "typespec-spec-directory");
            Assert.That(typespecSpecDirOption, Is.Not.Null);
            Assert.That(typespecSpecDirOption.Description, Is.EqualTo("TypeSpec specification directory (e.g., specification/testservice/TestService). Required when using --commit-id."));
            Assert.That(typespecSpecDirOption.IsRequired, Is.False);
        }

        [Test]
        public void CreateRootCommand_ShouldHaveSdkPathOption()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            Task<int> MockHandler(string? a, string? b, string? c, string d) => Task.FromResult(0);

            RootCommand rootCommand = commandLineConfiguration.CreateRootCommand(MockHandler);

            Option? sdkPathOption = rootCommand.Options.FirstOrDefault(o => o.Name == "sdk-path");
            Assert.That(sdkPathOption, Is.Not.Null);
            Assert.That(sdkPathOption.Description, Is.EqualTo("Output directory for generated SDK files"));
            Assert.That(sdkPathOption.IsRequired, Is.True);
        }

        [Test]
        public void CreateRootCommand_ShouldSetHandlerCorrectly()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            Task<int> MockHandler(string? a, string? b, string? c, string d) => Task.FromResult(0);

            RootCommand rootCommand = commandLineConfiguration.CreateRootCommand(MockHandler);

            Assert.That(rootCommand.Handler, Is.Not.Null);
        }

        [Test]
        public void ValidateInput_WithValidTypespecPath_ShouldReturnSuccess()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            const string typespecPath = "/path/to/typespec";
            const string commitId = null;
            const string typespecSpecDirectory = null;

            int result = commandLineConfiguration.ValidateInput(typespecPath, commitId, typespecSpecDirectory);

            Assert.That(result, Is.EqualTo(0));
            VerifyLogInformation(mockLogger, "Input validation completed successfully");
        }

        [Test]
        public void ValidateInput_WithValidCommitIdAndSpecDirectory_ShouldReturnSuccess()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            const string typespecPath = null;
            const string commitId = "abc123";
            const string typespecSpecDirectory = "specification/testservice/TestService";

            int result = commandLineConfiguration.ValidateInput(typespecPath, commitId, typespecSpecDirectory);

            Assert.That(result, Is.EqualTo(0));
            VerifyLogInformation(mockLogger, "Input validation completed successfully");
        }

        [Test]
        public void ValidateInput_WithNullTypespecPathAndNullCommitId_ShouldReturnFailureAndLogError()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            const string typespecPath = null;
            const string commitId = null;
            const string typespecSpecDirectory = null;

            int result = commandLineConfiguration.ValidateInput(typespecPath, commitId, typespecSpecDirectory);

            Assert.That(result, Is.EqualTo(1));
            VerifyLogError(mockLogger, "Either --typespec-path or --commit-id must be specified");
        }

        [Test]
        public void ValidateInput_WithEmptyTypespecPathAndEmptyCommitId_ShouldReturnFailureAndLogError()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            const string typespecPath = "";
            const string commitId = "";
            const string typespecSpecDirectory = null;

            int result = commandLineConfiguration.ValidateInput(typespecPath, commitId, typespecSpecDirectory);

            Assert.That(result, Is.EqualTo(1));
            VerifyLogError(mockLogger, "Either --typespec-path or --commit-id must be specified");
        }

        [Test]
        public void ValidateInput_WithWhitespaceTypespecPathAndWhitespaceCommitId_ShouldReturnFailureAndLogError()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            const string typespecPath = "   ";
            const string commitId = "   ";
            const string typespecSpecDirectory = null;

            int result = commandLineConfiguration.ValidateInput(typespecPath, commitId, typespecSpecDirectory);

            Assert.That(result, Is.EqualTo(1));
            VerifyLogError(mockLogger, "Either --typespec-path or --commit-id must be specified");
        }

        [Test]
        public void ValidateInput_WithBothTypespecPathAndCommitId_ShouldReturnFailureAndLogError()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            const string typespecPath = "/path/to/typespec";
            const string commitId = "abc123";
            const string typespecSpecDirectory = "specification/testservice/TestService";

            int result = commandLineConfiguration.ValidateInput(typespecPath, commitId, typespecSpecDirectory);

            Assert.That(result, Is.EqualTo(1));
            VerifyLogError(mockLogger, "Options --typespec-path and --commit-id are mutually exclusive. Specify only one");
        }

        [Test]
        public void ValidateInput_WithCommitIdButNoSpecDirectory_ShouldReturnFailureAndLogError()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            const string typespecPath = null;
            const string commitId = "abc123";
            const string typespecSpecDirectory = null;

            int result = commandLineConfiguration.ValidateInput(typespecPath, commitId, typespecSpecDirectory);

            Assert.That(result, Is.EqualTo(1));
            VerifyLogError(mockLogger, "Option --typespec-spec-directory is required when using --commit-id");
        }

        [Test]
        public void ValidateInput_WithCommitIdAndEmptySpecDirectory_ShouldReturnFailureAndLogError()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            const string typespecPath = null;
            const string commitId = "abc123";
            const string typespecSpecDirectory = "";

            int result = commandLineConfiguration.ValidateInput(typespecPath, commitId, typespecSpecDirectory);

            Assert.That(result, Is.EqualTo(1));
            VerifyLogError(mockLogger, "Option --typespec-spec-directory is required when using --commit-id");
        }

        [Test]
        public void ValidateInput_WithCommitIdAndWhitespaceSpecDirectory_ShouldReturnFailureAndLogError()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            const string typespecPath = null;
            const string commitId = "abc123";
            const string typespecSpecDirectory = "   ";

            int result = commandLineConfiguration.ValidateInput(typespecPath, commitId, typespecSpecDirectory);

            Assert.That(result, Is.EqualTo(1));
            VerifyLogError(mockLogger, "Option --typespec-spec-directory is required when using --commit-id");
        }

        [Test]
        public void ValidateInput_WithTypespecPathAndSpecDirectory_ShouldReturnSuccessAndIgnoreSpecDirectory()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            const string typespecPath = "/path/to/typespec";
            const string commitId = null;
            const string typespecSpecDirectory = "specification/testservice/TestService";

            int result = commandLineConfiguration.ValidateInput(typespecPath, commitId, typespecSpecDirectory);

            Assert.That(result, Is.EqualTo(0));
            VerifyLogInformation(mockLogger, "Input validation completed successfully");
        }
    }
}
