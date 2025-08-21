namespace TorrentHub.Services;

public interface ISettingsService
{
    Task<string?> GetSettingAsync(string key);
    Task SetSettingAsync(string key, string value);
    Task<bool> IsRegistrationOpenAsync();
}
