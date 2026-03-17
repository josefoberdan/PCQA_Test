using UnityEngine;

public static class DiscordTelemetryState
{
    private const string PREF_ENABLED = "DISCORD_ENABLED";
    private const string PREF_WEBHOOK = "DISCORD_WEBHOOK";
    private const string PREF_USERNAME = "DISCORD_USERNAME";

    public static bool Enabled
    {
        get => PlayerPrefs.GetInt(PREF_ENABLED, 0) == 1;
        set { PlayerPrefs.SetInt(PREF_ENABLED, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static string WebhookUrl
    {
        get => PlayerPrefs.GetString(PREF_WEBHOOK, "");
        set { PlayerPrefs.SetString(PREF_WEBHOOK, value ?? ""); PlayerPrefs.Save(); }
    }

    public static string Username
    {
        get => PlayerPrefs.GetString(PREF_USERNAME, "gustavo.hr");
        set { PlayerPrefs.SetString(PREF_USERNAME, string.IsNullOrEmpty(value) ? "gustavo.hr" : value); PlayerPrefs.Save(); }
    }
}
