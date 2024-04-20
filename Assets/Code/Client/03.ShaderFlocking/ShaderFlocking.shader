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

            struct appdata_custom
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 texcoord : TEXCOORD0;
                float4 tangent : TANGENT;

                uint id : SV_VertexID;
                uint instance_id : SV_InstanceID;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : NORMAL;
                float2 uv0 : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Bounds
            {
                float3 center;
                float3 size;
            };

            struct Boid
            {
                float3 position;
                float3 direction;
                Bounds local_bounds;
            };

            StructuredBuffer<Boid> boids_buffer;

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _MainTex_ST;
            CBUFFER_END

            sampler2D _MainTex;

            float4x4 create_trs_matrix(float3 pos, float3 dir, float3 up)
            {
                const float3 zaxis = normalize(dir);
                const float3 xaxis = normalize(cross(up, zaxis));
                const float3 yaxis = cross(zaxis, xaxis);
                return float4x4(
                    xaxis.x, yaxis.x, zaxis.x, pos.x,
                    xaxis.y, yaxis.y, zaxis.y, pos.y,
                    xaxis.z, yaxis.z, zaxis.z, pos.z,
                    0, 0, 0, 1
                );
            }

            float3 transform_object_to_world_dir(float4x4 object_to_world, float3 dirOS, bool doNormalize = true)
            {
                float3 dirWS = mul((float3x3)object_to_world, dirOS);
                if (doNormalize)
                    return SafeNormalize(dirWS);

                return dirWS;
            }

            v2f vert(appdata_custom input)
            {
                // 计算positionWS
                const Boid boid = boids_buffer[input.instance_id];
                const float3 up = float3(0, 1, 0);
                const float4x4 object_to_world = create_trs_matrix(boid.position, boid.direction, up);
                const float3 positionWS = mul(object_to_world, input.vertex);

                // 计算output
                v2f output;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = transform_object_to_world_dir(object_to_world, input.normal);
                output.uv0 = _MainTex_ST.xy + _MainTex_ST.zw;
                output.color = _Color;

                return output;
            }

            half4 frag(v2f i) : SV_TARGET
            {
                float4 color = tex2D(_MainTex, i.uv0) * _Color;

                //Simple lighting
                const half3 diffuse = LightingLambert(_MainLightColor.rgb, _MainLightPosition.xyz, i.normalWS);
                const half3 ambient = SampleSH(i.normalWS);
                color.rgb *= diffuse + ambient;

                return i.color;
            }
            ENDHLSL
        }
    }
}