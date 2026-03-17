using UnityEngine;

public class RuntimePointCloudRenderer : MonoBehaviour
{
    public Material pointMaterial;

    [Header("Transformações da Nuvem")]
    public Vector3 pointOffset = Vector3.zero;
    public Vector3 pointRotation = Vector3.zero;
    public float pointScale = 1f;

    [Header("Capacidade (evita realloc frequente)")]
    public int initialCapacity = 200000;

    [Header("Render")]
    [Tooltip("não desenha a nuvem (mas o player pode continuar carregando em background).")]
    public bool renderEnabled = true;

    private ComputeBuffer positionBuffer;
    private ComputeBuffer colorBuffer;

    private int capacity = 0;
    private int pointCount = 0;

    static readonly int ID_Positions = Shader.PropertyToID("_Positions");
    
    static readonly int ID_Colors  = Shader.PropertyToID("_Colors");
    
    static readonly int ID_Offset    = Shader.PropertyToID("_PointOffset");
    
    static readonly int ID_Scale  = Shader.PropertyToID("_PointScale");
    
    static readonly int ID_Rot     = Shader.PropertyToID("_PointRot");

    void Awake()
    {
        EnsureCapacity(initialCapacity);
    }

    public void SetPoints(Vector3[] positions, Color32[] colors)
    {
        if (positions == null || colors == null)
        {
            pointCount = 0;
            return;
        }

        pointCount = Mathf.Min(positions.Length, colors.Length);
        if (pointCount <= 0)
        {
            pointCount = 0;
            return;
        }

        EnsureCapacity(pointCount);

        EnsureScratch(pointCount);

        for (int i = 0; i < pointCount; i++)
        {
            var p = positions[i];
            scratchPos[i].x = p.x;
            
            scratchPos[i].y = p.y;
            
            scratchPos[i].z = p.z;
            
            scratchPos[i].w = 1f;

            var c = colors[i];
            scratchCol[i].x = c.r / 255f;
            
            scratchCol[i].y = c.g / 255f;
            
            scratchCol[i].z = c.b / 255f;
            
            scratchCol[i].w = c.a / 255f;
        }

        positionBuffer.SetData(scratchPos, 0, 0, pointCount);
        
        colorBuffer.SetData(scratchCol, 0, 0, pointCount);

        pointMaterial.SetVector(ID_Offset, pointOffset);
        
        pointMaterial.SetFloat(ID_Scale, pointScale);

        Quaternion rot = Quaternion.Euler(pointRotation);
        
        Matrix4x4 rotM = Matrix4x4.Rotate(rot);
        
        pointMaterial.SetMatrix(ID_Rot, rotM);

        pointMaterial.SetBuffer(ID_Positions, positionBuffer);
        
        pointMaterial.SetBuffer(ID_Colors, colorBuffer);
    }

    void OnRenderObject()
    {
        if (!renderEnabled) return; 

        if (pointMaterial == null || positionBuffer == null) return;
        if (pointCount <= 0) return;

        pointMaterial.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, pointCount);
    }

    void OnDestroy()
    {
        ReleaseBuffers();
    }

    private void EnsureCapacity(int required)
    {
        if (required <= capacity && positionBuffer != null && colorBuffer != null) return;

        ReleaseBuffers();

        capacity = Mathf.Max(required, 1024);
        
        positionBuffer = new ComputeBuffer(capacity, sizeof(float) * 4);
        
        
        colorBuffer    = new ComputeBuffer(capacity, sizeof(float) * 4);
    }

    private void ReleaseBuffers()
    {
        positionBuffer?.Release();
        
        colorBuffer?.Release();
        
        positionBuffer = null;
        
        colorBuffer = null;
        
        capacity = 0;
    }

    private Vector4[] scratchPos;
    
    private Vector4[] scratchCol;
    
    private int scratchCap = 0;

    private void EnsureScratch(int required)
    {
        if (required <= scratchCap && scratchPos != null && scratchCol != null) return;

        scratchCap = required;
        
        scratchPos = new Vector4[scratchCap];
        
        scratchCol = new Vector4[scratchCap];
    }

    public void Clear()
    {
        pointCount = 0;
    }
}

