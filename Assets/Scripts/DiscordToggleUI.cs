using UnityEngine;
using UnityEngine.UI;

public class DiscordToggleUI : MonoBehaviour
{
    [Header("UI")]
    public Toggle toggleDiscord;
    
    public InputField webhookInput;    
      
    public InputField usernameInput;   
      
    public Text statusText;              

    private void OnEnable()
    {
        SyncUIFromState();
    }

    public void SyncUIFromState()
    {
        if (toggleDiscord != null)
            toggleDiscord.isOn = DiscordTelemetryState.Enabled;

        if (webhookInput != null)
            webhookInput.text = DiscordTelemetryState.WebhookUrl;

        if (usernameInput != null)
            usernameInput.text = DiscordTelemetryState.Username;

        UpdateStatus();
    }

    public void OnToggleChanged(bool on)
    {
        DiscordTelemetryState.Enabled = on;
        UpdateStatus();
    }

    public void OnWebhookEdited(string url)
    {
        DiscordTelemetryState.WebhookUrl = url;
        UpdateStatus();
    }

    public void OnUsernameEdited(string username)
    {
        DiscordTelemetryState.Username = username;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (statusText == null) return;

        statusText.text = DiscordTelemetryState.Enabled
            ? "Discord: ON (enviando em tempo real)"
            : "Discord: OFF (somente salvando local)";
    }
}
