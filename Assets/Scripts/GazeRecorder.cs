using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.XR;

public class GazeRecorder : MonoBehaviour
{
    public enum Phase { Off, Visualization, Voting }

    public Transform rigCenterEye;
    public Transform cloudRoot;
    public Collider heatmapVolume;

    public bool recordEveryFrame = false;
    public float sampleHz = 30f;
    public float ignoreFirstSeconds = 0.4f;

    public LayerMask raycastMask = ~0;
    public float maxDistance = 10f;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    public int gridResolution = 32;
    public bool accumulateOnlyWhenHit = true;

    public string outputFolderName = "ResultadosVR";

    [Serializable]
    public class Sample
    {
        public float t;
        public Vector3 pos;
        
        public Quaternion rot;
        public Vector3 rayOrigin;
        
        public Vector3 rayDir;
        public bool hit;
        
        public Vector3 hitPoint;
        
        public Vector3 hitNormal;
        
        public float hitDistance;
        
        public string hitObject;
        public int phase;
    }

    [Serializable]
    public class HeatCell
    {
        public int x;
        
        public int y;
        
        public int z;
        
        public float dwell;
    }

    [Serializable]
    public class Trial
    {
        public string cloudName;
        
        public string startedAt;
        
        public string endedAt;
        public float duration;
        
        public List<Sample> samples = new List<Sample>();
        
        public List<HeatCell> heat = new List<HeatCell>();
    }

    [Serializable]
    public class ExperimentGaze
    {
        public string participantId;
        
        public string sessionStartedAt;
        
        public string sessionEndedAt;
        
        public List<Trial> trials = new List<Trial>();
    }

    public Phase CurrentPhase { get; private set; } = Phase.Off;
    public bool IsRecording => CurrentPhase == Phase.Visualization;

    private float nextSampleT = 0f;
    private float trialStartT = 0f;
    
    private float ignoreUntilT = 0f;

    private ExperimentGaze session = new ExperimentGaze();
    private Trial currentTrial = null;

    private readonly Dictionary<int, float> cellDwell = new Dictionary<int, float>();
    private RaycastHit lastHit;

    void Awake()
    {
        if (rigCenterEye == null) rigCenterEye = Camera.main != null ? Camera.main.transform : null;
        session.sessionStartedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        session.participantId = SystemInfo.deviceUniqueIdentifier;
    }

    void Update()
    {
        if (CurrentPhase != Phase.Visualization) return;
        if (currentTrial == null) return;

        float now = Time.unscaledTime;
        if (now < ignoreUntilT) return;

        if (recordEveryFrame)
        {
            RecordSample(now, Time.unscaledDeltaTime);
            return;
        }

        if (now >= nextSampleT)
        {
            float dt = Mathf.Max(0f, now - (nextSampleT - (1f / Mathf.Max(1f, sampleHz))));
            nextSampleT = now + (1f / Mathf.Max(1f, sampleHz));
            RecordSample(now, dt);
        }
    }

    public void SetPhase(Phase p)
    {
        if (CurrentPhase == p) return;
        CurrentPhase = p;
    }

    public void BeginTrial(string cloudName)
    {
        if (string.IsNullOrEmpty(cloudName)) cloudName = "Nuvem";
        EndTrial();

        currentTrial = new Trial();
        currentTrial.cloudName = cloudName;
        currentTrial.startedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

        trialStartT = Time.unscaledTime;
        ignoreUntilT = trialStartT + Mathf.Max(0f, ignoreFirstSeconds);

        nextSampleT = trialStartT;
        cellDwell.Clear();

        session.trials.Add(currentTrial);
        SetPhase(Phase.Visualization);
    }

    public void EndTrial()
    {
        if (currentTrial == null) { SetPhase(Phase.Off); return; }

        float endT = Time.unscaledTime;
        currentTrial.endedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        currentTrial.duration = Mathf.Max(0f, endT - trialStartT);

        currentTrial.heat.Clear();
        foreach (var kv in cellDwell)
        {
            UnpackCell(kv.Key, out int cx, out int cy, out int cz);
            var hc = new HeatCell { x = cx, y = cy, z = cz, dwell = kv.Value };
            currentTrial.heat.Add(hc);
        }

        currentTrial = null;
        SetPhase(Phase.Off);
    }

    public ExperimentGaze GetSessionData()
    {
        return session;
    }

    public void CloseSession()
    {
        session.sessionEndedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    }

    public void SaveSession(string filenameBase)
    {
        CloseSession();

        string folder =
#if UNITY_EDITOR
            Path.Combine(Application.dataPath, outputFolderName);
#else
            Path.Combine(Application.persistentDataPath, outputFolderName);
#endif
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        if (string.IsNullOrEmpty(filenameBase)) filenameBase = "experiment";

        string jsonPath = Path.Combine(folder, filenameBase + "_gaze.json");
        string json = JsonUtility.ToJson(session, true);
        SafeFileWriter.WriteAllTextAtomic(jsonPath, json);

        string csvPath = Path.Combine(folder, filenameBase + "_gaze_samples.csv");
        var sb = new StringBuilder();
        sb.AppendLine("TrialIndex,CloudName,t,Phase,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW,RayOX,RayOY,RayOZ,RayDX,RayDY,RayDZ,Hit,HitX,HitY,HitZ,HitNX,HitNY,HitNZ,HitDist,HitObject");

        for (int ti = 0; ti < session.trials.Count; ti++)
        {
            var tr = session.trials[ti];
            for (int i = 0; i < tr.samples.Count; i++)
            {
                var s = tr.samples[i];
                sb.Append(ti).Append(",");
                sb.Append(Escape(tr.cloudName)).Append(",");
                sb.Append(s.t.ToString("F4", CultureInfo.InvariantCulture)).Append(",");
                sb.Append(s.phase).Append(",");
                sb.Append(F(s.pos.x)).Append(",").Append(F(s.pos.y)).Append(",").Append(F(s.pos.z)).Append(",");
                sb.Append(F(s.rot.x)).Append(",").Append(F(s.rot.y)).Append(",").Append(F(s.rot.z)).Append(",").Append(F(s.rot.w)).Append(",");
                sb.Append(F(s.rayOrigin.x)).Append(",").Append(F(s.rayOrigin.y)).Append(",").Append(F(s.rayOrigin.z)).Append(",");
                sb.Append(F(s.rayDir.x)).Append(",").Append(F(s.rayDir.y)).Append(",").Append(F(s.rayDir.z)).Append(",");
                sb.Append(s.hit ? 1 : 0).Append(",");
                sb.Append(F(s.hitPoint.x)).Append(",").Append(F(s.hitPoint.y)).Append(",").Append(F(s.hitPoint.z)).Append(",");
                sb.Append(F(s.hitNormal.x)).Append(",").Append(F(s.hitNormal.y)).Append(",").Append(F(s.hitNormal.z)).Append(",");
                sb.Append(F(s.hitDistance)).Append(",");
                sb.Append(Escape(s.hitObject));
                sb.AppendLine();
            }
        }

        SafeFileWriter.WriteAllTextAtomic(csvPath, sb.ToString());

        string heatPath = Path.Combine(folder, filenameBase + "_gaze_heat.csv");
        var hb = new StringBuilder();
        hb.AppendLine("TrialIndex,CloudName,CellX,CellY,CellZ,DwellSeconds");
        for (int ti = 0; ti < session.trials.Count; ti++)
        {
            var tr = session.trials[ti];
            for (int i = 0; i < tr.heat.Count; i++)
            {
                var h = tr.heat[i];
                hb.Append(ti).Append(",").Append(Escape(tr.cloudName)).Append(",");
                hb.Append(h.x).Append(",").Append(h.y).Append(",").Append(h.z).Append(",");
                hb.Append(h.dwell.ToString("F4", CultureInfo.InvariantCulture));
                hb.AppendLine();
            }
        }
        SafeFileWriter.WriteAllTextAtomic(heatPath, hb.ToString());
    }

    private void RecordSample(float now, float dt)
    {
        if (rigCenterEye == null) return;

        Vector3 pos = rigCenterEye.position;
        Quaternion rot = rigCenterEye.rotation;

        Vector3 rayO = pos;
        Vector3 rayD = rigCenterEye.forward;

        Debug.DrawRay(rayO, rayD * 5f, Color.red);

        bool hit = false;
        Vector3 hp = Vector3.zero;
        Vector3 hn = Vector3.zero;
        float hd = 0f;
        string ho = "";

        if (heatmapVolume != null)
        {
            hit = heatmapVolume.Raycast(new Ray(rayO, rayD), out lastHit, maxDistance);
        }
        else
        {
            hit = Physics.Raycast(rayO, rayD, out lastHit, maxDistance, raycastMask, triggerInteraction);
        }

        if (hit)
        {
            hp = lastHit.point;
            hn = lastHit.normal;
            hd = lastHit.distance;
            ho = lastHit.collider != null ? lastHit.collider.gameObject.name : "";

            if (!accumulateOnlyWhenHit || hit)
                AccumulateHeat(hp, dt);
        }

        var s = new Sample();
        s.t = Mathf.Max(0f, now - trialStartT);
        s.pos = pos;
        s.rot = rot;
        s.rayOrigin = rayO;
        s.rayDir = rayD;
        s.hit = hit;
        s.hitPoint = hp;
        s.hitNormal = hn;
        s.hitDistance = hd;
        s.hitObject = ho;
        s.phase = (int)CurrentPhase;

        currentTrial.samples.Add(s);

        if (hit)
        {
            Debug.Log("HIT em: " + ho);
        }
    }

    private void AccumulateHeat(Vector3 hitPointWorld, float dt)
    {
        if (heatmapVolume == null) return;

        Transform ht = heatmapVolume.transform;
        Vector3 local = ht.InverseTransformPoint(hitPointWorld);

        Vector3 size = Vector3.one;
        if (heatmapVolume is BoxCollider bc)
            size = bc.size;
        else if (heatmapVolume is SphereCollider sc)
            size = Vector3.one * (sc.radius * 2f);

        Vector3 half = size * 0.5f;

        float nx = Mathf.InverseLerp(-half.x, half.x, local.x);
        float ny = Mathf.InverseLerp(-half.y, half.y, local.y);
        float nz = Mathf.InverseLerp(-half.z, half.z, local.z);

        nx = Mathf.Clamp01(nx);
        ny = Mathf.Clamp01(ny);
        nz = Mathf.Clamp01(nz);

        int r = Mathf.Clamp(gridResolution, 4, 256);
        int cx = Mathf.Clamp(Mathf.FloorToInt(nx * r), 0, r - 1);
        int cy = Mathf.Clamp(Mathf.FloorToInt(ny * r), 0, r - 1);
        int cz = Mathf.Clamp(Mathf.FloorToInt(nz * r), 0, r - 1);

        int key = PackCell(cx, cy, cz);
        if (cellDwell.TryGetValue(key, out float v))
            cellDwell[key] = v + Mathf.Max(0f, dt);
        else
            cellDwell[key] = Mathf.Max(0f, dt);
    }

    private int PackCell(int x, int y, int z)
    {
        int r = Mathf.Clamp(gridResolution, 4, 256);
        return (x & 0xFF) | ((y & 0xFF) << 8) | ((z & 0xFF) << 16) | ((r & 0xFF) << 24);
    }

    private void UnpackCell(int key, out int x, out int y, out int z)
    {
        x = (key) & 0xFF;
        y = (key >> 8) & 0xFF;
        z = (key >> 16) & 0xFF;
    }

    private string F(float v) => v.ToString("F6", CultureInfo.InvariantCulture);

    private string Escape(string s)
    {
        if (s == null) return "";
        if (s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r"))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}
