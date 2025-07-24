using Azure.Tools.GeneratorAgent;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.ComponentModel;
using System.Diagnostics;

namespace Azure.Tools.GeneratorAgent.Tests
{
    [TestFixture]
    public class ProcessExecutorTests
    {
        private const string ValidCommand = "cmd.exe";
        private const string EchoArguments = "/c echo test message";
        private const string FailArguments = "/c exit 1";
        private const string ValidWorkingDirectory = @"C:\Windows\System32";
        private const string ExpectedOutput = "test message";
        private const int TimeoutMilliseconds = 5000;

        private Mock<ILogger<ProcessExecutor>> mockLogger = null!;
        private ProcessExecutor executor = null!;
        private CancellationTokenSource cancellationTokenSource = null!;

        [SetUp]
        public void SetUp()
        {
            mockLogger = new Mock<ILogger<ProcessExecutor>>();
            executor = new ProcessExecutor(mockLogger.Object);
            cancellationTokenSource = new CancellationTokenSource();
        }

        [TearDown]
        public void TearDown()
        {
            cancellationTokenSource?.Dispose();
        }

        [Test]
        public async Task ExecuteAsync_WithValidCommand_ReturnsSuccessResult()
        {
            var result = await executor.ExecuteAsync(
                ValidCommand,
                EchoArguments,
                ValidWorkingDirectory,
                cancellationTokenSource.Token);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.Output.Trim(), Is.EqualTo(ExpectedOutput));
                Assert.That(result.Error, Is.Empty);
            });
        }

        [Test]
        public async Task ExecuteAsync_WithFailingCommand_ReturnsFailureResult()
        {
            var result = await executor.ExecuteAsync(
                ValidCommand,
                FailArguments,
                ValidWorkingDirectory,
                cancellationTokenSource.Token);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Output, Is.Empty);
                Assert.That(result.Error, Is.Empty); // cmd.exe doesn't write to stderr for exit 1
            });
        }

        [Test]
        public void ExecuteAsync_WithNullCommand_ThrowsArgumentException()
        {
            var exception = Assert.ThrowsAsync<ArgumentException>(async () =>
                await executor.ExecuteAsync(
                    null!,
                    EchoArguments,
                    ValidWorkingDirectory,
                    cancellationTokenSource.Token));

            Assert.That(exception!.ParamName, Is.EqualTo("command"));
        }

        [Test]
        public void ExecuteAsync_WithEmptyCommand_ThrowsArgumentException()
        {
            var exception = Assert.ThrowsAsync<ArgumentException>(async () =>
                await executor.ExecuteAsync(
                    string.Empty,
                    EchoArguments,
                    ValidWorkingDirectory,
                    cancellationTokenSource.Token));

            Assert.That(exception!.ParamName, Is.EqualTo("command"));
        }

        [Test]
        public void ExecuteAsync_WithWhitespaceCommand_ThrowsArgumentException()
        {
            var exception = Assert.ThrowsAsync<ArgumentException>(async () =>
                await executor.ExecuteAsync(
                    "   ",
                    EchoArguments,
                    ValidWorkingDirectory,
                    cancellationTokenSource.Token));

            Assert.That(exception!.ParamName, Is.EqualTo("command"));
        }

        [Test]
        public async Task ExecuteAsync_WithNullArguments_UsesEmptyArguments()
        {
            var result = await executor.ExecuteAsync(
                ValidCommand,
                null,
                ValidWorkingDirectory,
                cancellationTokenSource.Token);

            // Should succeed as cmd.exe with no arguments just opens and closes
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public async Task ExecuteAsync_WithTimeout_CompletesWithinTimeout()
        {
            var timeout = TimeSpan.FromMilliseconds(TimeoutMilliseconds);
            var result = await executor.ExecuteAsync(
                ValidCommand,
                EchoArguments,
                ValidWorkingDirectory,
                cancellationTokenSource.Token,
                timeout);

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public async Task ExecuteAsync_WithTimeoutExceeded_ReturnsTimeoutError()
        {
            // Use a command that sleeps/waits longer than the timeout
            var longRunningArgs = "/c ping 127.0.0.1 -n 10";
            var timeout = TimeSpan.FromMilliseconds(100);
            
            var result = await executor.ExecuteAsync(
                ValidCommand,
                longRunningArgs,
                ValidWorkingDirectory,
                cancellationTokenSource.Token,
                timeout);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Error, Does.Contain("timed out"));
            });
        }

        [Test]
        public async Task ExecuteAsync_WithCancellation_ReturnsCancellationError()
        {
            // Use a long-running command that won't exit quickly
            var longRunningArgs = "/c ping 127.0.0.1 -n 10";
            cancellationTokenSource.CancelAfter(100);

            var result = await executor.ExecuteAsync(
                ValidCommand,
                longRunningArgs,
                ValidWorkingDirectory,
                cancellationTokenSource.Token);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Error, Is.EqualTo("Operation was cancelled"));
            });
        }

        [Test]
        public async Task ExecuteAsync_WithNonexistentCommand_ReturnsCommandNotFoundError()
        {
            var result = await executor.ExecuteAsync(
                "nonexistent-command-12345",
                EchoArguments,
                ValidWorkingDirectory,
                cancellationTokenSource.Token);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Error, Is.EqualTo("Command not found"));
            });
        }

        [Test]
        public async Task ExecuteAsync_LogsSuccessfulCommand()
        {
            await executor.ExecuteAsync(
                ValidCommand,
                EchoArguments,
                ValidWorkingDirectory,
                cancellationTokenSource.Token);

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Command succeeded")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_LogsFailedCommand()
        {
            await executor.ExecuteAsync(
                ValidCommand,
                FailArguments,
                ValidWorkingDirectory,
                cancellationTokenSource.Token);

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Command failed with exit code")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_LogsCancelledCommand()
        {
            var longRunningArgs = "/c ping 127.0.0.1 -n 10";
            cancellationTokenSource.CancelAfter(100);

            await executor.ExecuteAsync(
                ValidCommand,
                longRunningArgs,
                ValidWorkingDirectory,
                cancellationTokenSource.Token);

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Command execution was cancelled")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_WithMultilineOutput_CapturesAllLines()
        {
            var multilineArgs = "/c echo Line1 & echo Line2 & echo Line3";
            
            var result = await executor.ExecuteAsync(
                ValidCommand,
                multilineArgs,
                ValidWorkingDirectory,
                cancellationTokenSource.Token);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.Output, Does.Contain("Line1"));
                Assert.That(result.Output, Does.Contain("Line2"));
                Assert.That(result.Output, Does.Contain("Line3"));
            });
        }

        [Test]
        public async Task ExecuteAsync_WithInvalidWorkingDirectory_HandlesGracefully()
        {
            var invalidDir = @"C:\NonExistentDirectory12345";
            
            var result = await executor.ExecuteAsync(
                ValidCommand,
                EchoArguments,
                invalidDir,
                cancellationTokenSource.Token);

            // Should fail as directory doesn't exist
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public async Task ExecuteAsync_WithNullWorkingDirectory_UsesCurrentDirectory()
        {
            var result = await executor.ExecuteAsync(
                ValidCommand,
                EchoArguments,
                null, // null working directory
                cancellationTokenSource.Token);

            Assert.That(result.Success, Is.True);
        }
    }
}
