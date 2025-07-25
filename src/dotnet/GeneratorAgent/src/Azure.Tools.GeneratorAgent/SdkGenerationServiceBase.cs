namespace Azure.Tools.GeneratorAgent
{
    /// <summary>
    /// Abstract base class that provides common file system operations for SDK generation services.
    /// <remarks>
    /// This class follows the Template Method pattern, providing concrete implementations of 
    /// common file/directory operations while allowing derived classes to override them for testing.
    /// All methods are marked as virtual to enable mocking and testing.
    /// </remarks>
    /// </summary>
    internal abstract class SdkGenerationServiceBase : ISdkGenerationService
    {
        /// <summary>
        /// Compiles a TypeSpec project into an SDK.
        /// Derived classes must implement this method to provide specific compilation logic.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if compilation succeeds, false otherwise</returns>
        public abstract Task<bool> CompileTypeSpecAsync(CancellationToken cancellationToken = default);

        #region File System Operations
        // These methods are virtual to allow for easy testing and mocking

        /// <summary>
        /// Determines whether the given path refers to an existing directory.
        /// </summary>
        /// <param name="path">The path to test</param>
        /// <returns>True if the directory exists, false otherwise</returns>
        protected virtual bool DirectoryExists(string path) => Directory.Exists(path);

        /// <summary>
        /// Determines whether the specified file exists.
        /// </summary>
        /// <param name="path">The file to check</param>
        /// <returns>True if the file exists, false otherwise</returns>
        protected virtual bool FileExists(string path) => File.Exists(path);

        /// <summary>
        /// Creates all directories and subdirectories in the specified path.
        /// </summary>
        /// <param name="path">The directory to create</param>
        protected virtual void CreateDirectory(string path) => Directory.CreateDirectory(path);

        /// <summary>
        /// Deletes an existing directory.
        /// </summary>
        /// <param name="path">The name of the directory to remove</param>
        /// <param name="recursive">True to remove directories, subdirectories, and files in path</param>
        protected virtual void DeleteDirectory(string path, bool recursive) => Directory.Delete(path, recursive);

        /// <summary>
        /// Returns the names of files in the specified directory.
        /// </summary>
        /// <param name="path">The directory to search</param>
        /// <returns>An array of file names</returns>
        protected virtual string[] GetFiles(string path) => Directory.GetFiles(path);

        /// <summary>
        /// Returns the names of subdirectories in the specified directory.
        /// </summary>
        /// <param name="path">The directory to search</param>
        /// <returns>An array of directory names</returns>
        protected virtual string[] GetDirectories(string path) => Directory.GetDirectories(path);

        /// <summary>
        /// Moves a file to a new location.
        /// </summary>
        /// <param name="sourceFileName">The name of the file to move</param>
        /// <param name="destFileName">The new path and name for the file</param>
        protected virtual void MoveFile(string sourceFileName, string destFileName) => File.Move(sourceFileName, destFileName);

        /// <summary>
        /// Replaces the contents of a specified file with the contents of another file.
        /// </summary>
        /// <param name="sourceFileName">The name of a file that replaces the file specified by destinationFileName</param>
        /// <param name="destinationFileName">The name of the file being replaced</param>
        /// <param name="destinationBackupFileName">The name of the backup file</param>
        protected virtual void ReplaceFile(string sourceFileName, string destinationFileName, string destinationBackupFileName) => File.Replace(sourceFileName, destinationFileName, destinationBackupFileName);

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        /// <param name="path">The name of the file to be deleted</param>
        protected virtual void DeleteFile(string path) => File.Delete(path);

        /// <summary>
        /// Moves a file or a directory and its contents to a new location.
        /// </summary>
        /// <param name="sourceDirName">The path of the file or directory to move</param>
        /// <param name="destDirName">The path to the new location for sourceDirName</param>
        protected virtual void MoveDirectory(string sourceDirName, string destDirName) => Directory.Move(sourceDirName, destDirName);

        #endregion
    }
}
