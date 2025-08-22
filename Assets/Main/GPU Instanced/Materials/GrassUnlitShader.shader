Shader "Unlit/GrassUnlitShader"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.2, 0.8, 0.2, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            Blend One Zero
            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            // ===== URP Includes =====
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ===== Buffers & Uniforms =====
            StructuredBuffer<float4> _Positions;
            float4 _BaseColor;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                uint   instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Get per-instance position from StructuredBuffer
                float4 instanceData = _Positions[IN.instanceID];
                float3 worldPos = IN.positionOS  * instanceData.w + instanceData.xyz;

                // Transform to clip space using URP's transform
                OUT.positionCS = TransformWorldToHClip(worldPos);

                OUT.color = _BaseColor;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return IN.color;
            }

            ENDHLSL
        }
    }
}