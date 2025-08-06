/// <summary>
/// Session Manager Service Interface for clean dependency injection
/// </summary>
public interface ISessionManagerService
{
    void ForceRefreshUserSession(string userCode);
    void InvalidateUserCache(string userCode);
    void CleanupStaleCache();
}
