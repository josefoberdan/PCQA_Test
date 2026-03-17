using System;
using UnityEngine;
using UnityEngine.Networking;

public class DiscordWebhookSender : MonoBehaviour
{
    [Header("Defaults (se PlayerPrefs estiver vazio)")]
    [TextArea(2, 6)]
    public string defaultWebhookUrl = "https://discord.com/api/webhooks/1205565207502393454/cwccQLdOnejxe6WTb2Zl2E9l02jB8oBWaEbAEgCMp8ln0TFxp5cX12eWu3Mi3qugyPpM";
    public string defaultUsername = "gustavo.hr";

    [Header("Controle")]
    public float minIntervalSeconds = 0.15f; // evita spam por frame/duplo clique
    private float lastSendTime = -999f;

    [Serializable]
    private class DiscordPayload
    {
        public string username;
        public string content;
    }

    private void Awake()
    {
        
        if (string.IsNullOrEmpty(DiscordTelemetryState.WebhookUrl))
            DiscordTelemetryState.WebhookUrl = defaultWebhookUrl;

        if (string.IsNullOrEmpty(DiscordTelemetryState.Username))
            DiscordTelemetryState.Username = defaultUsername;
    }

    public void SendVote(VoteResult vr)
    {
        string msg =
            $"**VOTO REGISTRADO**\n" +
            $"Cloud: `{vr.cloudName}`\n" +
            $"Score: **{vr.score}**\n" +
            $"RT: `{vr.reactionTime:0.000}s`\n" +
            $"Time: `{vr.timestamp}`\n" +
            $"Device: `{SystemInfo.deviceModel}`";

        TryPost(msg);
    }

    public void SendNoVote(string cloudName)
    {
        string msg =
            $"**SEM VOTO (TEMPO ESGOTADO)**\n" +
            $"Cloud: `{cloudName}`\n" +
            $"Time: `{DateTime.Now:dd/MM/yyyy HH:mm:ss}`\n" +
            $"Device: `{SystemInfo.deviceModel}`";

        TryPost(msg);
    }

    public void SendSnapshot(int totalVotes, string jsonPath, string csvPath, string txtPath)
    {
        
        string msg =
            $"**SNAPSHOT**\n" +
            $"Total votos: **{totalVotes}**\n" +
            $"Time: `{DateTime.Now:dd/MM/yyyy HH:mm:ss}`\n" +
            $"Arquivos locais atualizados:\n" +
            $"- results.json\n" +
            $"- results.csv\n" +
            $"- results.txt";

        TryPost(msg);
    }

    private void TryPost(string content)
    {
        if (!DiscordTelemetryState.Enabled) return;

       
        if (Time.unscaledTime - lastSendTime < minIntervalSeconds)
            return;

        string url = DiscordTelemetryState.WebhookUrl;
        if (string.IsNullOrEmpty(url))
            return;

        lastSendTime = Time.unscaledTime;

        var payload = new DiscordPayload
        {
            username = DiscordTelemetryState.Username,
            content = content
        };

        string json = JsonUtility.ToJson(payload);
        StartCoroutine(PostJson(url, json));
    }

    private System.Collections.IEnumerator PostJson(string url, string json)
    {
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool ok = req.result == UnityWebRequest.Result.Success;
#else
            bool ok = !req.isHttpError && !req.isNetworkError;
#endif

            if (!ok)
            {
                Debug.LogWarning($"[DiscordWebhookSender] Falha ao enviar: {req.responseCode} | {req.error}");
            }
        }
    }
}
