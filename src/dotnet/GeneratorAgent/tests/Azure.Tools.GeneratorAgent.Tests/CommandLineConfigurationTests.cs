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
            Task<int> MockHandler(string? a, string? b, string c) => Task.FromResult(0);

            RootCommand rootCommand = commandLineConfiguration.CreateRootCommand(MockHandler);

            Assert.That(rootCommand, Is.Not.Null);
            Assert.That(rootCommand.Description, Is.EqualTo("Azure SDK Generator Agent"));
        }

        [Test]
        public void CreateRootCommand_ShouldHaveCorrectNumberOfOptions()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            Task<int> MockHandler(string? a, string? b, string c) => Task.FromResult(0);

            RootCommand rootCommand = commandLineConfiguration.CreateRootCommand(MockHandler);

            Assert.That(rootCommand.Options.Count, Is.EqualTo(3));
        }

        [Test]
        public void CreateRootCommand_ShouldHaveTypespecPathOption()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            Task<int> MockHandler(string? a, string? b, string c) => Task.FromResult(0);

            RootCommand rootCommand = commandLineConfiguration.CreateRootCommand(MockHandler);

            Option? typespecPathOption = rootCommand.Options.FirstOrDefault(o => o.Name == "typespec-path");
            Assert.That(typespecPathOption, Is.Not.Null);
            Assert.That(typespecPathOption.Description, Is.EqualTo("Path to the local TypeSpec project directory or TypeSpec specification directory (e.g., specification/testservice/TestService)"));
            Assert.That(typespecPathOption.IsRequired, Is.True);
        }

        [Test]
        public void CreateRootCommand_ShouldHaveCommitIdOption()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            Task<int> MockHandler(string? a, string? b, string c) => Task.FromResult(0);

            RootCommand rootCommand = commandLineConfiguration.CreateRootCommand(MockHandler);

            Option? commitIdOption = rootCommand.Options.FirstOrDefault(o => o.Name == "commit-id");
            Assert.That(commitIdOption, Is.Not.Null);
            Assert.That(commitIdOption.Description, Is.EqualTo("GitHub commit ID to generate SDK from (optional, used with --typespec-path for GitHub generation)"));
            Assert.That(commitIdOption.IsRequired, Is.False);
        }

        [Test]
        public void CreateRootCommand_ShouldHaveSdkPathOption()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            Task<int> MockHandler(string? a, string? b, string c) => Task.FromResult(0);

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
            Task<int> MockHandler(string? a, string? b, string c) => Task.FromResult(0);

            RootCommand rootCommand = commandLineConfiguration.CreateRootCommand(MockHandler);

            Assert.That(rootCommand.Handler, Is.Not.Null);
        }

        [Test]
        public void ValidateInput_WithValidTypespecPath_ShouldReturnSuccess()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            const string typespecPath = "/path/to/typespec";
            const string? commitId = null;

            int result = commandLineConfiguration.ValidateInput(typespecPath, commitId);

            Assert.That(result, Is.EqualTo(0));
            VerifyLogInformation(mockLogger, "Input validation completed successfully");
        }

        [Test]
        public void ValidateInput_WithTypespecPathAndCommitId_ShouldReturnSuccess()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            const string typespecPath = "specification/testservice/TestService";
            const string commitId = "abc123";

            int result = commandLineConfiguration.ValidateInput(typespecPath, commitId);

            Assert.That(result, Is.EqualTo(0));
            VerifyLogInformation(mockLogger, "Input validation completed successfully");
        }

        [Test]
        public void ValidateInput_WithNullTypespecPath_ShouldReturnFailureAndLogError()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            const string? typespecPath = null;
            const string? commitId = null;

            int result = commandLineConfiguration.ValidateInput(typespecPath, commitId);

            Assert.That(result, Is.EqualTo(1));
            VerifyLogError(mockLogger, "Option --typespec-path is required");
        }

        [Test]
        public void ValidateInput_WithEmptyTypespecPath_ShouldReturnFailureAndLogError()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            const string typespecPath = "";
            const string? commitId = null;

            int result = commandLineConfiguration.ValidateInput(typespecPath, commitId);

            Assert.That(result, Is.EqualTo(1));
            VerifyLogError(mockLogger, "Option --typespec-path is required");
        }

        [Test]
        public void ValidateInput_WithWhitespaceTypespecPath_ShouldReturnFailureAndLogError()
        {
            Mock<ILogger<CommandLineConfiguration>> mockLogger = CreateMockLogger();
            CommandLineConfiguration commandLineConfiguration = CreateCommandLineConfiguration(mockLogger);
            const string typespecPath = "   ";
            const string? commitId = null;

            int result = commandLineConfiguration.ValidateInput(typespecPath, commitId);

            Assert.That(result, Is.EqualTo(1));
            VerifyLogError(mockLogger, "Option --typespec-path is required");
        }
    }
}
