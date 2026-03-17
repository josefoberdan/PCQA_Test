using UnityEngine;

public class DynamicPointCloud : MonoBehaviour
{
    public Material pointMaterial;   //Pcx (Point.mat ou Disk.mat)
    public int pointCount = 10000;   
    private ComputeBuffer pointBuffer;

    struct PointData
    {
        public Vector3 pos;
        public Color col;
    }

    void Start()
    {
       
        pointBuffer = new ComputeBuffer(pointCount, sizeof(float) * 3 + sizeof(float) * 4);
        pointMaterial.SetBuffer("_PointBuffer", pointBuffer);

        PointData[] points = new PointData[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            points[i].pos = Random.insideUnitSphere * 5f;
            points[i].col = Color.Lerp(Color.red, Color.blue, (float)i / pointCount);
        }
        pointBuffer.SetData(points);
    }

    void Update()
    {
        
        PointData[] points = new PointData[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            float angle = (float)i / pointCount * Mathf.PI * 2f;
            points[i].pos = new Vector3(Mathf.Cos(angle), Mathf.Sin(Time.time + angle), Mathf.Sin(angle)) * 3f;
            points[i].col = Color.HSVToRGB(Mathf.PingPong(Time.time * 0.1f, 1f), 1f, 1f);
        }
        pointBuffer.SetData(points);

        Graphics.DrawProcedural(pointMaterial, new Bounds(Vector3.zero, Vector3.one * 100f),
            MeshTopology.Points, pointCount);
    }

    void OnDestroy()
    {
        if (pointBuffer != null) pointBuffer.Release();
    }
}
