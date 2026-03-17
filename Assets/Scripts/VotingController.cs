using UnityEngine;
using UnityEngine.UI;
using System.Collections;

using System.Collections.Generic;

using System.IO;

using System.Text;

using System;

public class VotingController : MonoBehaviour
{
    public GameObject panelVisualization;
    
    public GameObject panelVoting;

    public Text visualizationTimerText;
    
    public Text votingTimerText;
    
    public Text resultsListText;

    public Button[] voteButtons;
    
    public Button nextButton;

    public GameObject cloudRoot;
    
    public PointCloudSequencePlayer cloudPlayer;

    public CloudNameLabel cloudNameLabel;

    public float visualizationTime = 15f;
    
    public float votingTime = 15f;

    public int visualizationFontSize = 250;
    
    public int votingFontSize = 250;

    public List<VoteResult> results = new List<VoteResult>();

    public SceneFlowController flow;
    public ExperimentCloudManager experimentManager;

    public GazeRecorder gazeRecorder;

    [Header("Discord (opcional)")]
    public DiscordWebhookSender discordSender;

    private float countdown;
    
    private bool canVote;
    
    private float voteStartTime;
    
    private Coroutine timerCoroutine;

    private bool uiLocked = false;
    private bool isAdvancing = false;

    private RuntimePointCloudRenderer cachedRenderer;

    
    private bool resultsLoadedFromDisk = false;

    private void Awake()
    {
        if (cloudPlayer != null)
            cachedRenderer = cloudPlayer.GetComponent<RuntimePointCloudRenderer>();

        
        LoadResultsFromDiskOnce();
    }

    public void StartVotingFlow()
    {
        StopAllCoroutines();

        canVote = false;
        uiLocked = false;
        isAdvancing = false;

        if (panelVoting != null) panelVoting.SetActive(false);
        if (panelVisualization != null) panelVisualization.SetActive(true);

        if (visualizationTimerText != null)
        {
            visualizationTimerText.gameObject.SetActive(true);
            visualizationTimerText.enabled = true;
            visualizationTimerText.fontSize = visualizationFontSize;
            visualizationTimerText.text = "";
        }

        if (votingTimerText != null)
        {
            votingTimerText.gameObject.SetActive(false);
            votingTimerText.text = "";
        }

        SetUILocked(true);

        if (nextButton != null) nextButton.gameObject.SetActive(false);

        StartCoroutine(VisualizationRoutine());
    }

    private IEnumerator VisualizationRoutine()
    {
        SetCloudVisible(true);
        SetUILocked(true);

        if (panelVoting != null) panelVoting.SetActive(false);
        if (panelVisualization != null) panelVisualization.SetActive(true);

        if (visualizationTimerText != null)
        {
            visualizationTimerText.text = "";
            visualizationTimerText.gameObject.SetActive(false);
        }

        if (cloudPlayer == null)
        {
            Debug.LogError("[VotingController] cloudPlayer == null");
            yield break;
        }

        ShowLoading(true);

        float timeout = 12f;
        float t = 0f;

        while (!cloudPlayer.FirstFrameUploaded && t < timeout)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (!cloudPlayer.FirstFrameUploaded)
        {
            Debug.LogWarning("[VotingController] Timeout no 1º frame. Tentando ReloadSequence() 1 vez...");
            cloudPlayer.ReloadSequence();

            float timeout2 = 8f;
            float t2 = 0f;

            while (!cloudPlayer.FirstFrameUploaded && t2 < timeout2)
            {
                t2 += Time.deltaTime;
                yield return null;
            }
        }

        ShowLoading(false);

        if (!cloudPlayer.FirstFrameUploaded)
        {
            Debug.LogError("[VotingController] Falhou novamente. Pulando para próxima nuvem.");
            SetCloudVisible(false);
            HideCloudNameForVoting();

            if (experimentManager != null && !experimentManager.IsLoadingCloud && experimentManager.TryLoadNext())
            {
                yield return null;
                StartCoroutine(VisualizationRoutine());
                yield break;
            }

            if (flow != null) flow.GoToResults();
            yield break;
        }

        string cloudName = GetCurrentCloudLeafNameOrDefault();

        if (gazeRecorder != null)
            gazeRecorder.BeginTrial(cloudName);

        if (visualizationTimerText != null)
        {
            visualizationTimerText.gameObject.SetActive(true);
            visualizationTimerText.enabled = true;
            visualizationTimerText.fontSize = visualizationFontSize;
        }

        float vis = visualizationTime;
        while (vis > 0f)
        {
            if (visualizationTimerText != null)
            {
                visualizationTimerText.text =
                    "Visualize a\nNuvem de Pontos!\n\nTempo restante de\nvisualização:\n" + Mathf.CeilToInt(vis);
            }

            vis -= Time.deltaTime;
            yield return null;
        }

        if (visualizationTimerText != null)
        {
            visualizationTimerText.text = "";
            visualizationTimerText.gameObject.SetActive(false);
        }

        if (gazeRecorder != null)
            gazeRecorder.SetPhase(GazeRecorder.Phase.Voting);

        SetCloudVisible(false);
        HideCloudNameForVoting();

        if (panelVisualization != null) panelVisualization.SetActive(false);
        if (panelVoting != null) panelVoting.SetActive(true);

        StartVotingPhase();
    }

    private void StartVotingPhase()
    {
        uiLocked = false;
        canVote = true;

        countdown = votingTime;
        voteStartTime = Time.time;

        if (panelVisualization != null) panelVisualization.SetActive(false);
        if (panelVoting != null) panelVoting.SetActive(true);

        if (votingTimerText != null)
        {
            votingTimerText.gameObject.SetActive(true);
            votingTimerText.enabled = true;
            votingTimerText.fontSize = votingFontSize;
            votingTimerText.text = "Vote!\n\nTempo de votação:\n" + Mathf.CeilToInt(countdown);
        }

        SetVoteButtonsInteractable(true);

        if (nextButton != null) nextButton.gameObject.SetActive(false);

        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        timerCoroutine = StartCoroutine(VotingTimerRoutine());
    }

    private IEnumerator VotingTimerRoutine()
    {
        while (countdown > 0f && canVote)
        {
            countdown -= Time.deltaTime;

            if (votingTimerText != null)
            {
                votingTimerText.gameObject.SetActive(true);
                votingTimerText.text = "Vote!\n\nTempo de votação:\n" + Mathf.CeilToInt(countdown);
            }

            yield return null;
        }

        TimeOver();
    }

    private void TimeOver()
    {
        canVote = false;

        SetVoteButtonsInteractable(false);

        if (gazeRecorder != null)
            gazeRecorder.EndTrial();

        HideCloudNameForVoting();

        string cloudName = GetCurrentCloudLeafNameOrDefault();

        if (votingTimerText != null)
        {
            votingTimerText.gameObject.SetActive(true);
            votingTimerText.text = "Nenhum voto registrado!\n\nTempo de votação:\nEsgotado";
        }

        
        TryDiscordSendNoVote(cloudName);

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            HighlightNext();
        }
    }

    public void Vote(int value)
    {
        if (uiLocked) return;
        if (!canVote) return;

        canVote = false;
        uiLocked = true;

        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        float reactionTime = Time.time - voteStartTime;

        string cloudName = GetCurrentCloudLeafNameOrDefault();

        var vr = new VoteResult
        {
            cloudName = cloudName,
            score = value,
            reactionTime = reactionTime,
            timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
        };

        results.Add(vr);

        if (gazeRecorder != null)
            gazeRecorder.EndTrial();

        if (votingTimerText != null)
            votingTimerText.text = "Voto registrado";

        
        TryDiscordSendVote(vr);

        SetVoteButtonsInteractable(false);

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            HighlightNext();
        }
    }

    public void Next()
    {
        if (isAdvancing) return;
        isAdvancing = true;

        SetUILocked(true);

        if (nextButton != null)
        {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
        }

        SaveResultsAppend();
        StartCoroutine(NextRoutine());
    }

    private IEnumerator NextRoutine()
    {
        SetCloudVisible(false);
        HideCloudNameForVoting();

        ShowLoading(true);
        yield return null;

        if (experimentManager == null)
        {
            ShowLoading(false);
            if (flow != null) flow.GoToResults();
            yield break;
        }

        if (experimentManager.IsLoadingCloud)
        {
            float w = 0f;
            while (experimentManager.IsLoadingCloud && w < 2f)
            {
                w += Time.deltaTime;
                yield return null;
            }
        }

        bool hasMore = experimentManager.TryLoadNext();
        if (!hasMore)
        {
            ShowLoading(false);
            if (flow != null) flow.GoToResults();
            yield break;
        }

        if (cloudPlayer != null)
        {
            float timeout = 12f;
            float t = 0f;
            while (!cloudPlayer.FirstFrameUploaded && t < timeout)
            {
                t += Time.deltaTime;
                yield return null;
            }

            if (!cloudPlayer.FirstFrameUploaded)
            {
                Debug.LogWarning("[VotingController] Timeout pós-TryLoadNext. Tentando ReloadSequence() 1 vez...");
                cloudPlayer.ReloadSequence();

                float timeout2 = 8f;
                float t2 = 0f;

                while (!cloudPlayer.FirstFrameUploaded && t2 < timeout2)
                {
                    t2 += Time.deltaTime;
                    yield return null;
                }
            }
        }

        ShowLoading(false);

        isAdvancing = false;
        uiLocked = false;

        SetCloudVisible(true);

        StartVotingFlow();
    }

    private void SetUILocked(bool locked)
    {
        uiLocked = locked;
        SetVoteButtonsInteractable(!locked && canVote);

        if (nextButton != null)
            nextButton.interactable = !locked && nextButton.gameObject.activeSelf;
    }

    private void ShowLoading(bool on)
    {
        if (cloudPlayer != null && cloudPlayer.loadingUI != null)
            cloudPlayer.loadingUI.SetVisible(on);
    }

    private void SetCloudVisible(bool visible)
    {
        if (cloudRoot != null)
            cloudRoot.SetActive(true);

        if (cloudPlayer != null)
            cloudPlayer.enabled = true;

        if (cachedRenderer != null)
        {
            cachedRenderer.renderEnabled = visible;
            if (!visible)
                cachedRenderer.Clear();
        }
    }

    private void HideCloudNameForVoting()
    {
        if (cloudNameLabel != null)
            cloudNameLabel.Hide();
    }

    private void SetVoteButtonsInteractable(bool on)
    {
        foreach (var b in voteButtons)
            if (b != null) b.interactable = on;
    }

    private void HighlightNext()
    {
        var selector = FindObjectOfType<VRJoystickUISelector>();
        if (selector != null && nextButton != null)
            selector.HighlightNextButton(nextButton);
    }

    

    private string GetResultsFolder()
    {
#if UNITY_EDITOR
        return Path.Combine(Application.dataPath, "ResultadosVR");
#else
        return Path.Combine(Application.persistentDataPath, "ResultadosVR");
#endif
    }

    private void LoadResultsFromDiskOnce()
    {
        if (resultsLoadedFromDisk) return;
        resultsLoadedFromDisk = true;

        string folder = GetResultsFolder();
        if (!Directory.Exists(folder)) return;

        string jsonPath = Path.Combine(folder, "results.json");
        if (!File.Exists(jsonPath)) return;

        try
        {
            string json = File.ReadAllText(jsonPath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json)) return;

            VotePackage pkg = JsonUtility.FromJson<VotePackage>(json);
            if (pkg == null || pkg.results == null) return;

            
            var seen = new HashSet<string>();
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                seen.Add($"{r.cloudName}|{r.score}|{r.timestamp}");
            }

            int added = 0;
            for (int i = 0; i < pkg.results.Count; i++)
            {
                var r = pkg.results[i];
                string key = $"{r.cloudName}|{r.score}|{r.timestamp}";
                if (!seen.Contains(key))
                {
                    results.Add(r);
                    seen.Add(key);
                    added++;
                }
            }

            Debug.Log($"[VotingController] Histórico carregado. added={added} total={results.Count}");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[VotingController] Falha ao ler results.json: " + e.Message);
        }
    }

   

    private void SaveResultsAppend()
    {
        
        LoadResultsFromDiskOnce();

        string folder = GetResultsFolder();
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        
        string jsonPath = Path.Combine(folder, "results.json");
        string json = JsonUtility.ToJson(new VotePackage(results), true);
        SafeFileWriter.WriteAllTextAtomic(jsonPath, json);

       
        string csvPath = Path.Combine(folder, "results.csv");
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("CloudName,Score,ReactionTime,Timestamp");
        foreach (var r in results)
            csv.AppendLine($"{r.cloudName},{r.score},{r.reactionTime:F3},{r.timestamp}");
        SafeFileWriter.WriteAllTextAtomic(csvPath, csv.ToString());

        
        string txtPath = Path.Combine(folder, "results.txt");
        StringBuilder txt = new StringBuilder();
        txt.AppendLine("RESULTADOS - VR PCQA");
        txt.AppendLine("========================================");
        txt.AppendLine("Device: " + SystemInfo.deviceModel);
        txt.AppendLine("DeviceUniqueId: " + SystemInfo.deviceUniqueIdentifier);
        txt.AppendLine("Total votos: " + results.Count);
        txt.AppendLine("Gerado em: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
        txt.AppendLine("----------------------------------------");

        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            txt.AppendLine($"{i + 1:000}) Cloud={r.cloudName} | Score={r.score} | RT={r.reactionTime:0.000}s | Time={r.timestamp}");
        }

        txt.AppendLine("========================================");
        SafeFileWriter.WriteAllTextAtomic(txtPath, txt.ToString());

       
        if (gazeRecorder != null)
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            gazeRecorder.SaveSession("experiment_" + stamp);
        }

        
        TryDiscordSendSnapshot(results.Count, jsonPath, csvPath, txtPath);
    }

    public void PrepareResultsUI()
    {
        if (resultsListText == null) return;

        StringBuilder sb = new StringBuilder("Resultados\n\n");
        foreach (var r in results)
            sb.AppendLine($"{r.cloudName} | Nota {r.score} | {r.reactionTime:0.0}s");

        resultsListText.text = sb.ToString();
    }

    private string GetLeafFolderName(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath)) return "";
        string p = folderPath.Replace("\\", "/").TrimEnd('/');
        int lastSlash = p.LastIndexOf('/');
        return (lastSlash < 0) ? p : p.Substring(lastSlash + 1);
    }

    private string GetCurrentCloudLeafNameOrDefault()
    {
        string cloudName = "Nuvem";
        if (cloudPlayer != null && !string.IsNullOrEmpty(cloudPlayer.folderPath))
            cloudName = GetLeafFolderName(cloudPlayer.folderPath);
        return cloudName;
    }

    

    private void TryDiscordSendVote(VoteResult vr)
    {
        if (discordSender == null) return;
        if (!DiscordTelemetryState.Enabled) return;
        discordSender.SendVote(vr);
    }

    private void TryDiscordSendNoVote(string cloudName)
    {
        if (discordSender == null) return;
        if (!DiscordTelemetryState.Enabled) return;
        discordSender.SendNoVote(cloudName);
    }

    private void TryDiscordSendSnapshot(int totalVotes, string jsonPath, string csvPath, string txtPath)
    {
        if (discordSender == null) return;
        if (!DiscordTelemetryState.Enabled) return;
        discordSender.SendSnapshot(totalVotes, jsonPath, csvPath, txtPath);
    }
}
