using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class PointCloudSequenceManager : MonoBehaviour
{
    [Header("Pastas das nuvens (relativas a StreamingAssets)")]
    public string[] cloudFolders;

    [Header("Player da nuvem")]
    public PointCloudSequencePlayer player;

    private List<string> orderedFolders;
    public int currentIndex { get; private set; }

    void Awake()
    {
        ResetSequence();
    }

    
    public void ResetSequence()
    {
        orderedFolders = BuildQueueInParentOrder(cloudFolders);
        currentIndex = 0;

        Debug.Log($"[PointCloudSequenceManager] total={orderedFolders.Count}");
        for (int i = 0; i < orderedFolders.Count; i++)
            Debug.Log($"[PointCloudSequenceManager] ordered[{i}]={orderedFolders[i]}");
    }

    public bool HasNextCloud()
    {
        return orderedFolders != null && currentIndex < orderedFolders.Count;
    }

    public void LoadCurrentCloud()
    {
        if (!HasNextCloud()) return;

        player.folderPath = orderedFolders[currentIndex];
        player.ReloadSequence();
    }

    public void LoadNextCloud()
    {
        currentIndex++;

        if (HasNextCloud())
            LoadCurrentCloud();
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

