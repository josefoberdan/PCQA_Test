using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

using System.IO;
using System.Linq;

using System.Threading;
using UnityEngine;

[RequireComponent(typeof(RuntimePointCloudRenderer))]
public class PointCloudSequencePlayer : MonoBehaviour
{
  
    public bool waitUntilFullyLoaded = false;

    [Tooltip("Mostra progresso no console.")]
    public bool logProgress = false;

    private volatile int totalFramesToLoad = 0;
    
    private volatile int loadedOkCount = 0;
    
    private volatile int loadedAnyCount = 0;

    [Header("Pasta da sequência de PLYs")]
    public string folderPath = "PointCloudSequence/Longdress";

    [Header("FPS lógico da sequência")]
    public float frameRate = 30f;

    [Header("Suavização temporal (troca de frames)")]
    [Range(0.01f, 2f)]
    public float speed = 1f;

    [Header("Loop")]
    public bool loop = true;

    [Header("Quest/Android - Janela deslizante")]
    [Tooltip("Quantos frames ficam em RAM ao mesmo tempo (2–8).")]
    public int windowSize = 4;

    [Tooltip("Quantos frames à frente tentar pré-carregar.")]
    public int prefetchAhead = 2;

    [Tooltip("Limita uploads (SetPoints) por frame.")]
    public int maxUploadsPerFrame = 1;

    [Header("Debug/Status")]
    public bool logDebug = false;

    [Header("Loading UI (anti-pisca)")]
    public float loadingDelay = 0.2f;

    private Coroutine loadingDelayRoutine;
    
    private bool firstFrameUploaded = false;

    public LoadingMessageBillboard loadingUI;

    private RuntimePointCloudRenderer runtimeRenderer;
    
    private List<string> plyFiles = new List<string>();

    private volatile bool isReady = false;
    
    private volatile bool isLoading = false;

    public bool IsReady => isReady;
    
    public bool IsLoading => isLoading;
    
    public bool FirstFrameUploaded => firstFrameUploaded;

    private int frameIndex = 0;
    
    private float frameTimer = 0f;

    private readonly object cacheLock = new object();
    
    private Dictionary<int, FrameData> cache = new Dictionary<int, FrameData>();

    private Thread ioThread;
    
    private CancellationTokenSource cts;
    
    private readonly ConcurrentQueue<int> requestQueue = new ConcurrentQueue<int>();
    
    private readonly ConcurrentQueue<LoadedFrame> loadedQueue = new ConcurrentQueue<LoadedFrame>();

    private int lastUploadedIndex = -1;
    
    private bool isReloading = false;

    private struct FrameData
    {
        public Vector3[] pos;
        public Color32[] col;
    }

    private struct LoadedFrame
    {
    
        public int index;
        
        public Vector3[] pos;
        
        public Color32[] col;
        
        public bool ok;
    }

    void Awake()
    {
        runtimeRenderer = GetComponent<RuntimePointCloudRenderer>();
    }

    void OnEnable()
    {
        
        if (ioThread == null && isLoading)
            StartWorker();
            
    }

    void Start()
    {
    
        ReloadSequence();
        
    }

    void OnDestroy()
    {
    
        StopWorker();
        
    }

    public void ReloadSequence()
    {
    
        if (isReloading) return;
        isReloading = true;

        StopAllCoroutines();
        
        StopWorker();

        isReady = false;
        
        isLoading = true;
        
        firstFrameUploaded = false;
        
        lastUploadedIndex = -1;

        if (runtimeRenderer != null)
            runtimeRenderer.Clear();

        lock (cacheLock) cache.Clear();

        LoadSequencePaths();
        if (plyFiles.Count == 0)
        {
        
            isLoading = false;
           
            SetLoadingUI(false);
            
            isReloading = false;
            return;
        }

        totalFramesToLoad = plyFiles.Count;
        
        loadedOkCount = 0;
        
        loadedAnyCount = 0;

     
     
        if (waitUntilFullyLoaded)
        {
            for (int i = 0; i < plyFiles.Count; i++)
                requestQueue.Enqueue(i);
        }
        else
        {
        
            EnqueueWindowRequests(0);
            
        }

        StartWorker();
        
        StartCoroutine(LoadingAndPlayRoutine_ReleaseReloadGuard());
    }

    private IEnumerator LoadingAndPlayRoutine_ReleaseReloadGuard()
    {
    
        yield return StartCoroutine(LoadingAndPlayRoutine());
        isReloading = false;
        
    }

    private IEnumerator LoadingAndPlayRoutine()
    {
    
        SetLoadingUI(false);

        frameIndex = 0;
        frameTimer = 0f;

        if (waitUntilFullyLoaded)
        {
           
           
            while (loadedAnyCount < totalFramesToLoad)
            
            {
                DrainLoadedQueue(uploadBudget: maxUploadsPerFrame);
                yield return null;
            }

           
            for (int k = 0; k < 999; k++)
            {
                int before = loadedAnyCount;
                DrainLoadedQueue(uploadBudget: 999999);
                if (loadedAnyCount == before) break;
                yield return null;
            }

            if (logDebug || logProgress)
                Debug.Log($"[PointCloudSequencePlayer] Fully loaded: ok={loadedOkCount}/{totalFramesToLoad}");

            if (loadedOkCount <= 0)
            {
            
                Debug.LogError("[PointCloudSequencePlayer] Nenhum frame válido carregado.");
                isLoading = false;
                SetLoadingUI(false);
                yield break;
                
            }

            frameIndex = FindFirstValidIndex();
            if (TryGetFrameFromCache(frameIndex, out var fd))
                UploadFrame(frameIndex, fd);

            isReady = true;
            isLoading = false;
            SetLoadingUI(false);
            yield break;
        }

       
        EnqueueWindowRequests(frameIndex);

        float timeout = 12f;
        
        float t = 0f;

        while (!TryGetFrameFromCache(frameIndex, out var first) && t < timeout)
        {
        
            DrainLoadedQueue(uploadBudget: maxUploadsPerFrame);
            
            t += Time.unscaledDeltaTime;
            yield return null;
            
        }

        DrainLoadedQueue(uploadBudget: maxUploadsPerFrame);

        if (TryGetFrameFromCache(frameIndex, out var fd2))
        {
        
            UploadFrame(frameIndex, fd2);
            
            isReady = true;
            
        }
        else
        {
        
            Debug.LogError("[PointCloudSequencePlayer] Timeout: não conseguiu carregar o primeiro frame.");
            
        }

        isLoading = false;
        
        SetLoadingUI(false);
    }

    void Update()
    {
    
        if (!isReady)
        
        {
        
            DrainLoadedQueue(uploadBudget: maxUploadsPerFrame);
            return;
            
        }

        DrainLoadedQueue(uploadBudget: maxUploadsPerFrame);

        frameTimer += Time.deltaTime * speed;
        
        float frameDuration = 1f / Mathf.Max(0.0001f, frameRate);

        while (frameTimer >= frameDuration)
        {
        
            frameTimer -= frameDuration;
            AdvanceFrame();
            
        }
    }

    private int FindFirstValidIndex()
    {
    
        lock (cacheLock)
        {
        
            if (cache.Count == 0) return 0;
            return cache.Keys.OrderBy(k => k).First();
            
        }
    }

    private void AdvanceFrame()
    
    {
    
        int next = frameIndex + 1;
        if (next >= plyFiles.Count)
        {
        
            if (!loop) return;
            next = 0;
            
        }

        frameIndex = next;

        if (!waitUntilFullyLoaded)
            EnqueueWindowRequests(frameIndex);

        if (TryGetFrameFromCache(frameIndex, out var fd))
        {
            UploadFrame(frameIndex, fd);

            if (!waitUntilFullyLoaded)
                TrimCacheAround(frameIndex);
        }
        else
        {
            if (logDebug)
                Debug.Log($"[PointCloudSequencePlayer] Frame {frameIndex} not cached yet, holding last frame.");
        }
    }

    private void UploadFrame(int index, FrameData fd)
    {
    
        if (index == lastUploadedIndex) return;
        
        if (fd.pos == null || fd.col == null) return;

        runtimeRenderer.SetPoints(fd.pos, fd.col);
        
        lastUploadedIndex = index;

        if (!firstFrameUploaded)
        {
        
            firstFrameUploaded = true;
            
            SetLoadingUI(false);
            
        }
    }

    private void EnqueueWindowRequests(int centerIndex)
    {
    
        if (plyFiles.Count == 0) return;
        
        int w = Mathf.Clamp(windowSize, 2, 8);
        
        int ahead = Mathf.Clamp(prefetchAhead, 0, 8);
        
        int count = w + ahead;
        

        for (int k = 0; k < count; k++)
        {
            int idx = centerIndex + k;
            if (idx >= plyFiles.Count)
            {
                if (!loop) break;
                idx %= plyFiles.Count;
            }

            if (!IsCached(idx))
                requestQueue.Enqueue(idx);
        }
    }

    private bool IsCached(int idx)
    {
    
        lock (cacheLock) return cache.ContainsKey(idx);
        
    }

    private bool TryGetFrameFromCache(int idx, out FrameData fd)
    {
    
        lock (cacheLock) return cache.TryGetValue(idx, out fd);
        
    }
    

    private void TrimCacheAround(int centerIndex)
    {
    
        int w = Mathf.Clamp(windowSize, 2, 8);

        int min = centerIndex - 1;
        
        int max = centerIndex + w + prefetchAhead;

        lock (cacheLock)
        {
        
            var keys = cache.Keys.ToList();
            foreach (var k in keys)
            {
                bool keep;
                if (!loop)
                {
                
                    keep = (k >= min && k <= max);
                }
                
                else
                {
                
                    int dk = Mathf.Abs(k - centerIndex);
                    keep = (dk <= (w + prefetchAhead + 2));
                    
                }

                if (!keep)
                    cache.Remove(k);
            }
        }
    }

    private void DrainLoadedQueue(int uploadBudget)
    {
    
        int drained = 0;

        while (drained < uploadBudget && loadedQueue.TryDequeue(out var lf))
        {
        
            loadedAnyCount++;

            if (!lf.ok || lf.pos == null || lf.col == null)
            {
            
                drained++;
                continue;
                
            }

            lock (cacheLock)
            {
            
                cache[lf.index] = new FrameData { pos = lf.pos, col = lf.col };
                
            }

            loadedOkCount++;

            if (logProgress && (loadedAnyCount % 25 == 0))
                Debug.Log($"[PointCloudSequencePlayer] Loading progress: {loadedAnyCount}/{totalFramesToLoad}");

            drained++;
        }
    }

    private void StartWorker()
    {
        cts = new CancellationTokenSource();
        
        ioThread = new Thread(() => WorkerLoop(cts.Token));
        
        ioThread.IsBackground = true;
        
        ioThread.Start();

        if (loadingDelayRoutine != null)
            StopCoroutine(loadingDelayRoutine);

        loadingDelayRoutine = StartCoroutine(DelayedLoadingUI());
    }

    private void StopWorker()
    {
    
        try { cts?.Cancel(); } catch { }

        if (ioThread != null)
        {
        
            try
            {
            
                if (!ioThread.Join(1500))
                    Debug.LogWarning("[PointCloudSequencePlayer] Worker não parou a tempo (1500ms).");
                    
            }
            catch { }
        }

        ioThread = null;
        
        cts = null;

        while (requestQueue.TryDequeue(out _)) { }
        while (loadedQueue.TryDequeue(out _)) { }
    }

    private void WorkerLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (!requestQueue.TryDequeue(out int idx))
            {
            
                Thread.Sleep(1);
                
                continue;
                
            }

            if (IsCached(idx)) continue;

            string path = plyFiles[idx];
            bool ok = PointCloudLoader.LoadPly(path, out var pos, out var col);

            loadedQueue.Enqueue(new LoadedFrame
            {
                index = idx,
                
                pos = pos,
                
                col = col,
                
                ok = ok
            });
        }
    }

    private void SetLoadingUI(bool on)
    {
        if (on && runtimeRenderer != null)
            runtimeRenderer.Clear();

        if (loadingUI != null)
            loadingUI.SetVisible(on);
    }

    private IEnumerator DelayedLoadingUI()
    {
    
        float t = 0f;

        while (t < loadingDelay)
        {
        
            if (firstFrameUploaded)
                yield break;

            t += Time.unscaledDeltaTime;
            
            yield return null;
            
        }

        if (runtimeRenderer != null)
            runtimeRenderer.Clear();

        SetLoadingUI(true);
        
    }

    private void LoadSequencePaths()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        string basePath = Path.Combine(Application.persistentDataPath, folderPath);
#else
        string basePath = Path.Combine(Application.streamingAssetsPath, folderPath);
#endif

        if (!Directory.Exists(basePath))
        {
            Debug.LogError("[PointCloudSequencePlayer] Pasta não encontrada: " + basePath);
            plyFiles.Clear();
            return;
        }

        plyFiles = Directory.GetFiles(basePath, "*.ply", SearchOption.TopDirectoryOnly)
                            .OrderBy(p => p)
                            .ToList();

        if (logDebug)
            Debug.Log($"[PointCloudSequencePlayer] PLYs={plyFiles.Count} em {basePath}");
    }

    public void ResetToFirstFrame()
    {
        frameIndex = 0;
        frameTimer = 0f;

        if (TryGetFrameFromCache(frameIndex, out var fd))
            UploadFrame(frameIndex, fd);
    }
}

