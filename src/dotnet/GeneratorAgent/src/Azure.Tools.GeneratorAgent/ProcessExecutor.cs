using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Azure.Tools.GeneratorAgent
{
    /// <summary>
    /// Executes external processes with comprehensive logging, timeout support, and proper resource management.
    /// </summary>
    internal class ProcessExecutor
    {
        private readonly ILogger<ProcessExecutor> Logger;

        public ProcessExecutor(ILogger<ProcessExecutor> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes a command asynchronously with comprehensive error handling and logging.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="arguments">The command arguments (can be null or empty).</param>
        /// <param name="workingDirectory">The working directory for the command (uses current directory if null).</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <param name="timeout">Optional timeout for the command execution.</param>
        /// <returns>A tuple containing success status, standard output, and error output.</returns>
        /// <exception cref="ArgumentException">Thrown when command is null or whitespace.</exception>
        public virtual async Task<(bool Success, string Output, string Error)> ExecuteAsync(
            string command,
            string arguments,
            string? workingDirectory,
            CancellationToken cancellationToken,
            TimeSpan? timeout = null)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            arguments ??= string.Empty;

            try
            {
                StringBuilder outputBuilder = new StringBuilder(capacity: 4096);
                StringBuilder errorBuilder = new StringBuilder(capacity: 1024);
                object outputLock = new object();
                object errorLock = new object();

                using Process process = CreateProcess(command, arguments, workingDirectory);

                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        lock (outputLock)
                        {
                            outputBuilder.AppendLine(e.Data);
                        }
                    }
                };

                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        lock (errorLock)
                        {
                            errorBuilder.AppendLine(e.Data);
                        }
                    }
                };

                bool processStarted = false;
                try
                {
                    processStarted = process.Start();
                    if (!processStarted)
                    {
                        Logger.LogError("Failed to start process: {Command}", command);
                        return (false, string.Empty, "Failed to start process");
                    }

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (timeout.HasValue)
                    {
                        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        timeoutCts.CancelAfter(timeout.Value);

                        try
                        {
                            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                        {
                            Logger.LogError("Process timed out after {Timeout}ms: {Command}",
                                timeout.Value.TotalMilliseconds, command);

                            try
                            {
                                process.Kill(entireProcessTree: true);
                            }
                            catch (Exception killEx)
                            {
                                Logger.LogWarning(killEx, "Failed to kill timed-out process");
                            }

                            return (false, string.Empty, $"Process timed out after {timeout.Value.TotalMilliseconds}ms");
                        }
                    }
                    else
                    {
                        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == 2)
                {
                    Logger.LogError("Command not found: {Command}", command);
                    return (false, string.Empty, "Command not found");
                }
                catch (Win32Exception ex)
                {
                    Logger.LogError(ex, "Win32 error starting process: {Command}", command);
                    return (false, string.Empty, ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    Logger.LogError(ex, "Invalid operation starting process: {Command}", command);
                    return (false, string.Empty, ex.Message);
                }

                string output, error;
                lock (outputLock)
                {
                    output = outputBuilder.Length > 0 ? outputBuilder.ToString().TrimEnd() : string.Empty;
                }
                lock (errorLock)
                {
                    error = errorBuilder.Length > 0 ? errorBuilder.ToString().TrimEnd() : string.Empty;
                }

                bool success = process.ExitCode == 0;
                if (!success)
                {
                    Logger.LogError(
                        "Command failed with exit code {ExitCode}. Command: {Command}. Error: {Error}",
                        process.ExitCode,
                        command,
                        error);
                }
                else
                {
                    Logger.LogDebug(
                        "Command succeeded. Command: {Command}",
                        command);
                }

                return (success, output, error);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Logger.LogInformation("Command execution was cancelled: {Command}", command);
                return (false, string.Empty, "Operation was cancelled");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error executing command: {Command}", command);
                return (false, string.Empty, ex.Message);
            }
        }

        /// <summary>
        /// Creates a new process with the specified configuration. Virtual for testing.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="arguments">The command arguments.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns>A configured Process instance.</returns>
        protected virtual Process CreateProcess(string command, string arguments, string? workingDirectory)
        {
            return new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory ?? GetCurrentDirectory(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
        }

        /// <summary>
        /// Gets the current directory. Virtual for testing.
        /// </summary>
        /// <returns>The current directory path.</returns>
        protected virtual string GetCurrentDirectory()
        {
            return Directory.GetCurrentDirectory();
        }
    }
}
