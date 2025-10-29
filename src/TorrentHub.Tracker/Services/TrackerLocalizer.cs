using System.Collections.Generic;
using BencodeNET.Objects;

namespace TorrentHub.Tracker.Services;

public class TrackerLocalizer : ITrackerLocalizer
{
    private readonly Dictionary<string, Dictionary<string, string>> _translations = new();

    public TrackerLocalizer()
    {
        // InvalidCredential (formerly InvalidPasskey)
        AddTranslation("InvalidCredential", "en", "Invalid credential.");
        AddTranslation("InvalidCredential", "zh-CN", "无效的凭证。");
        AddTranslation("InvalidCredential", "ja", "無効な認証情報です。");
        AddTranslation("InvalidCredential", "fr", "Identifiant invalide.");
        
        // BannedAccount
        AddTranslation("BannedAccount", "en", "Your account is banned.");
        AddTranslation("BannedAccount", "zh-CN", "您的账户已被封禁。");
        AddTranslation("BannedAccount", "ja", "あなたのアカウントは禁止されています。");
        AddTranslation("BannedAccount", "fr", "Votre compte est banni.");

        // BannedClient
        AddTranslation("BannedClient", "en", "Your client is banned.");
        AddTranslation("BannedClient", "zh-CN", "您的客户端已被禁用。");
        AddTranslation("BannedClient", "ja", "お使いのクライアントは禁止されています。");
        AddTranslation("BannedClient", "fr", "Votre client est banni.");

        // InvalidInfoHash
        AddTranslation("InvalidInfoHash", "en", "Invalid info_hash format.");
        AddTranslation("InvalidInfoHash", "zh-CN", "无效的 info_hash 格式。");
        AddTranslation("InvalidInfoHash", "ja", "無効な info_hash 形式です。");
        AddTranslation("InvalidInfoHash", "fr", "Format de info_hash invalide.");

        // TorrentNotFound
        AddTranslation("TorrentNotFound", "en", "Torrent not found.");
        AddTranslation("TorrentNotFound", "zh-CN", "种子未找到。");
        AddTranslation("TorrentNotFound", "ja", "トレントが見つかりません。");
        AddTranslation("TorrentNotFound", "fr", "Torrent non trouvé.");
        
        // SpeedTooHigh
        AddTranslation("SpeedTooHigh", "en", "Reported upload speed is too high. This event has been logged.");
        AddTranslation("SpeedTooHigh", "zh-CN", "报告的上传速度过高。此事件已被记录。");
        AddTranslation("SpeedTooHigh", "ja", "報告されたアップロード速度が速すぎます。このイベントは記録されました。");
        AddTranslation("SpeedTooHigh", "fr", "La vitesse de téléversement signalée est trop élevée. Cet événement a été enregistré.");
    }

    private void AddTranslation(string key, string lang, string value)
    {
        if (!_translations.ContainsKey(key))
        {
            _translations[key] = new Dictionary<string, string>();
        }
        _translations[key][lang] = value;
    }

    public BDictionary GetError(string key, string? language)
    {
        // Normalize language code, e.g., "zh-CN" -> "zh-cn"
        var lang = string.IsNullOrEmpty(language) ? "en" : language.ToLowerInvariant();

        string message;
        
        // First try to match the exact language code (e.g., "zh-cn")
        if (_translations.TryGetValue(key, out var langDict) && langDict.TryGetValue(lang, out var translatedMessage))
        {
            message = translatedMessage;
        }
        // Then try to match the primary language part (e.g., "zh")
        else if (lang.Contains('-') && langDict != null && langDict.TryGetValue(lang.Split('-')[0], out var primaryLangMessage))
        {
            message = primaryLangMessage;
        }
        // Fallback to English
        else if (_translations.TryGetValue(key, out var defaultLangDict) && defaultLangDict.TryGetValue("en", out var defaultMessage))
        {
            message = defaultMessage;
        }
        else
        {
            // Ultimate fallback
            message = key; // Return the key itself if no translation is found at all
        }

        return new BDictionary { { "failure reason", new BString(message) } };
    }
}