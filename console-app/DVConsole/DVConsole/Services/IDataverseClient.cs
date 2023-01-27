namespace DVConsole.Services;

/// <summary>
/// A typed HTTP client for accessing Dataverse
/// </summary>
public interface IDataverseClient
{
    //Get the current user's ID
    Task<Guid?> GetUserId();

    Task<Guid> Create(
        string entityCollection,
        object data,
        CancellationToken ct = default);

    /// <summary>
    /// delete record
    /// </summary>
    /// <param name="entityCollection"></param>
    /// <param name="entityId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task Delete(string entityCollection, Guid entityId, CancellationToken ct = default);
}