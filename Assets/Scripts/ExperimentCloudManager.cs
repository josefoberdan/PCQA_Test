using System.IO;
using System.Collections;

using System.Collections.Generic;

using System.Linq;

using UnityEngine;

public class ExperimentCloudManager : MonoBehaviour
{
    [Header("Pastas (StreamingAssets)")]
    public string[] cloudFolders;

    [Header("Player da sequência (GameObject)")]
    public PointCloudSequencePlayer player;

    [Header("UI Nome da Nuvem")]
    public CloudNameLabel cloudNameLabel;

    [Header("Comportamento")]
    [Tooltip("1º frame da nuvem foi enviado para GPU.")]
    public bool showNameOnlyAfterFirstFrame = true;

    private List<string> queue = new List<string>();
    
    private int queueIndex = 0;
    
    private bool isLoadingCloud = false;
    
    private Coroutine waitLabelRoutine;

    public bool IsLoadingCloud => isLoadingCloud;

    public void ResetAndShuffle()
    {
        queue = BuildQueueInParentOrder(cloudFolders);
        queueIndex = 0;

        Debug.Log($"[ExperimentCloudManager] total={queue.Count}");
        for (int i = 0; i < queue.Count; i++)
            Debug.Log($"[ExperimentCloudManager] queue[{i}]={queue[i]}");
    }

    public bool TryLoadNext()
    {
        if (isLoadingCloud)
        {
            Debug.LogWarning("[ExperimentCloudManager] TryLoadNext ignorado: já está carregando uma nuvem.");
            return false;
        }

        if (player == null)
        {
            Debug.LogError("[ExperimentCloudManager] player == null");
            return false;
        }

        if (queue == null || queue.Count == 0)
        {
            Debug.LogError("[ExperimentCloudManager] queue vazia (cloudFolders não configurado?)");
            return false;
        }

        if (queueIndex >= queue.Count)
        {
            Debug.Log("[ExperimentCloudManager] Fim da fila.");
            return false;
        }

        string folder = queue[queueIndex];
        queueIndex++;

        string cloudName = GetParentFolderName(folder);

        Debug.Log($"[ExperimentCloudManager] Carregando: {cloudName}");
        
        
        Debug.Log($"[ExperimentCloudManager] folderPath = {folder}");

        
        if (!player.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[ExperimentCloudManager] player estava INATIVO. Reativando antes do ReloadSequence().");
            player.gameObject.SetActive(true);
        }

        if (!player.enabled)
        {
            Debug.LogWarning("[ExperimentCloudManager] player estava DESABILITADO. Habilitando antes do ReloadSequence().");
            player.enabled = true;
        }

        if (waitLabelRoutine != null)
        {
        
            StopCoroutine(waitLabelRoutine);
            
            waitLabelRoutine = null;
            
        }

        if (cloudNameLabel != null)
            cloudNameLabel.Hide();

        isLoadingCloud = true;

        player.folderPath = folder;
        player.ReloadSequence();

        if (cloudNameLabel != null)
        {
        
            if (showNameOnlyAfterFirstFrame)
            
                waitLabelRoutine = StartCoroutine(ShowLabelWhenReady(cloudName));
            else
            {
            
                cloudNameLabel.Show(cloudName);
                
                isLoadingCloud = false;
            }
        }
        else
        {
            isLoadingCloud = false;
        }

        return true;
    }

    private IEnumerator ShowLabelWhenReady(string cloudName)
    {
        if (player == null)
        {
            isLoadingCloud = false;
            yield break;
        }

        float timeout = 12f;
        
        float t = 0f;

        while (!player.FirstFrameUploaded && t < timeout)
        {
        
            t += Time.unscaledDeltaTime;
            
            yield return null;
            
        }

        
        if (cloudNameLabel != null)
            cloudNameLabel.Show(cloudName);

        isLoadingCloud = false;
        waitLabelRoutine = null;
    }

    private List<string> BuildQueueInParentOrder(string[] parents)
    {
        var result = new List<string>();

        foreach (var parent in parents ?? new string[0])
        {
            var expanded = ExpandIfParentFolder(parent);

            expanded = expanded
                .OrderBy(p => p, new NaturalPathComparer())
                .ToList();

            result.AddRange(expanded);
        }

        return result;
    }

    private string GetParentFolderName(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath)) return "";

        string p = folderPath.Replace("\\", "/").TrimEnd('/');
        int lastSlash = p.LastIndexOf('/');
        if (lastSlash <= 0) return p;

        string parent = p.Substring(0, lastSlash);
        int parentSlash = parent.LastIndexOf('/');
        if (parentSlash < 0) return parent;

        return parent.Substring(parentSlash + 1);
    }

    private List<string> ExpandIfParentFolder(string folder)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        string basePath = Path.Combine(Application.persistentDataPath, folder);
#else
        string basePath = Path.Combine(Application.streamingAssetsPath, folder);
#endif

        if (Directory.Exists(basePath) &&
            Directory.GetFiles(basePath, "*.ply", SearchOption.TopDirectoryOnly).Length > 0)
            return new List<string> { folder };

        if (Directory.Exists(basePath))
        {
            var dirs = Directory.GetDirectories(basePath, "*", SearchOption.TopDirectoryOnly);

            return dirs
                .Select(d => folder.Replace("\\", "/").TrimEnd('/') + "/" + Path.GetFileName(d))
                .ToList();
        }

        return new List<string> { folder };
    }

    private class NaturalPathComparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            if (ReferenceEquals(a, b)) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            string la = Leaf(a);
            
            
            string lb = Leaf(b);

            int c = NaturalCompare(la, lb);
            if (c != 0) return c;

            return string.CompareOrdinal(a, b);
        }

        private static string Leaf(string p)
        {
            p = p.Replace("\\", "/").TrimEnd('/');
            int idx = p.LastIndexOf('/');
            return (idx < 0) ? p : p.Substring(idx + 1);
        }

        private static int NaturalCompare(string x, string y)
        {
            int ix = 0, iy = 0;

            while (ix < x.Length && iy < y.Length)
            {
                char cx = x[ix];
                
                char cy = y[iy];

                bool dx = char.IsDigit(cx);
                
                
                bool dy = char.IsDigit(cy);

                if (dx && dy)
                {
                    long vx = 0;
                    while (ix < x.Length && char.IsDigit(x[ix]))
                        vx = vx * 10 + (x[ix++] - '0');

                    long vy = 0;
                    while (iy < y.Length && char.IsDigit(y[iy]))
                        vy = vy * 10 + (y[iy++] - '0');

                    int num = vx.CompareTo(vy);
                    if (num != 0) return num;
                }
                else
                {
                    cx = char.ToLowerInvariant(cx);
                    
                    
                    cy = char.ToLowerInvariant(cy);

                    if (cx != cy) return cx.CompareTo(cy);
                    ix++;
                    iy++;
                }
            }

            return (x.Length - ix).CompareTo(y.Length - iy);
        }
    }
}

