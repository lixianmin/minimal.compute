Shader "Test/ShaderFlocking"
{
    Properties
    {
        _Color("Color", Color) = (1,1, 1, 1)
        _MainTex ("_MainTex (RGBA)", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }
        
        Pass
        {
            Name "Pass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : NORMAL;
                float2 uv0 : TEXCOORD0;
                float4 color : COLOR;
            };

            // Same with the one with compute shader & C# script
            struct VertexData
            {
                uint id;
                float3 pos;
                float3 nor;
                float2 uv;
                float4 col;
            };

            StructuredBuffer<VertexData> vertexBuffer;

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _MainTex_ST;
            CBUFFER_END

            sampler2D _MainTex;

            v2f vert(uint id : SV_VertexID)
            {
                v2f output = (v2f)0;

                const uint real_id = vertexBuffer[id].id;

                const float3 positionOS = vertexBuffer[real_id].pos;
                float2 uv0 = vertexBuffer[real_id].uv;
                const float3 normal = vertexBuffer[real_id].nor;
                output.color = vertexBuffer[real_id].col;

                const float3 positionWS = TransformObjectToWorld(positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldDir(normal);
                output.uv0 = uv0.xy * _MainTex_ST.xy + _MainTex_ST.zw;

                return output;
            }

            half4 frag(v2f i) : SV_TARGET
            {
                float4 color = tex2D(_MainTex, i.uv0) * _Color;

                //Simple lighting
                const half3 diffuse = LightingLambert(_MainLightColor.rgb, _MainLightPosition.xyz, i.normalWS);
                const half3 ambient = SampleSH(i.normalWS);
                color.rgb *= diffuse + ambient;

                return color;
            }
            ENDHLSL
        }
    }
}