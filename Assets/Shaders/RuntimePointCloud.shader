Shader "Custom/RuntimePointCloud"
{
    Properties
    {
        _PointSize ("Point Size (nem todo device usa)", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        ZWrite On
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            StructuredBuffer<float4> _Positions;
            StructuredBuffer<float4> _Colors;

            float3 _PointOffset;
            float  _PointScale;
            float4x4 _PointRot;

            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 col : COLOR0;
            };

            v2f vert (appdata v)
            {
                v2f o;

                float4 p = _Positions[v.vertexID];

             
                float3 pt = p.xyz * _PointScale;
                pt = mul((float3x3)_PointRot, pt);
                pt += _PointOffset;

                o.pos = UnityObjectToClipPos(float4(pt, 1.0));
                o.col = _Colors[v.vertexID];
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(i.col.rgb, 1.0);
            }
            ENDCG
        }
    }
}

