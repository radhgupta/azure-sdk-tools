namespace Azure.Tools.GeneratorAgent
{
    /// <summary>
    /// Defines the contract for SDK generation from TypeSpec sources.
    /// <remarks>
    /// Implementations should inherit from <see cref="SdkGenerationServiceBase"/> 
    /// to get common file system operations for free.
    /// </remarks>
    /// </summary>
    internal interface ISdkGenerationService
    {
        /// <summary>
        /// Compiles a TypeSpec project into an SDK.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if compilation succeeds, false otherwise</returns>
        Task<bool> CompileTypeSpecAsync(CancellationToken cancellationToken = default);
    }
}