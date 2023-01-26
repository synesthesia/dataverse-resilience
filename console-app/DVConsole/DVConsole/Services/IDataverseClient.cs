namespace DVConsole.Services;

public interface IDataverseClient
{
    Task<Guid?> GetUserId();
}