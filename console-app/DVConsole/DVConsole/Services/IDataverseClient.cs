namespace DVConsole.Services;

/// <summary>
/// A typed HTTP client for accessing Dataverse
/// </summary>
public interface IDataverseClient
{
    //Get the current user's ID
    Task<Guid?> GetUserId();
}