// Qua cau nuoc/thuy tinh cho vong bong bong quanh con rong.
//
// Viet tay bang HLSL thay vi Shader Graph vi Shader Graph khong tao duoc bang code.
//
// CO Y BO KHUC XA (khong dung Scene Color / _CameraOpaqueTexture): khuc xa buoc URP copy
// nguyen man hinh moi frame, ma trong AR thi camera passthrough + SLAM tracking da an mot
// phan frame budget TRUOC khi noi dung cua ta ve dong nao (skill ar-3d-asset-pipeline).
// Fresnel rim + specular manh cho ~80% cam giac "khoi cau thuy tinh" voi mot phan nho chi phi.
//
// Gon song mat nuoc lam bang TONG CAC SIN tren toa do object-space, khong dung texture noise
// - re hon va khong ton them 1 sampler tren mobile.
Shader "DragonAR/BubbleWater"
{
    Properties
    {
        _BaseColor      ("Mau nen", Color)               = (0.016, 0.121, 0.078, 0.55)
        _RimColor       ("Mau vien (Fresnel)", Color)    = (0.16, 0.85, 0.55, 1)
        _RimPower       ("Do sac cua vien", Range(0.5, 8))   = 2.5
        _RimStrength    ("Do sang cua vien", Range(0, 4))    = 1.6
        _SpecColor2     ("Mau highlight", Color)         = (1, 1, 1, 1)
        _Smoothness     ("Do bong", Range(0, 1))         = 0.95
        _SpecStrength   ("Do manh highlight", Range(0, 6)) = 2.5
        _RippleScale    ("Mat do gon song", Range(1, 40))  = 11
        _RippleSpeed    ("Toc do gon song", Range(0, 4))   = 0.7
        _RippleStrength ("Bien do gon song", Range(0, 0.5)) = 0.07
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "BubbleForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float4 _SpecColor2;
                float  _RimPower;
                float  _RimStrength;
                float  _Smoothness;
                float  _SpecStrength;
                float  _RippleScale;
                float  _RippleSpeed;
                float  _RippleStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   n = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS = p.positionCS;
                OUT.positionWS = p.positionWS;
                OUT.normalWS   = n.normalWS;
                OUT.positionOS = IN.positionOS.xyz;
                return OUT;
            }

            // Bop nhe phap tuyen theo tong 3 song sin lech pha -> mat cau "chay" nhu nuoc.
            float3 RippleNormal(float3 n, float3 posOS)
            {
                float t = _Time.y * _RippleSpeed;
                float3 s = posOS * _RippleScale;
                float3 wobble = float3(
                    sin(s.y + t) + sin(s.z * 1.3 - t * 0.7),
                    sin(s.z + t * 1.1) + sin(s.x * 1.7 + t * 0.5),
                    sin(s.x + t * 0.9) + sin(s.y * 1.5 - t * 1.3));
                return normalize(n + wobble * _RippleStrength);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = RippleNormal(normalize(IN.normalWS), IN.positionOS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                Light light = GetMainLight();
                float3 L = normalize(light.direction);
                float3 H = normalize(L + V);

                // Fresnel: vien qua cau sang len, giua trong hon -> ra "khoi cau", khong phai
                // "hinh tron to mau". Day la thanh phan quan trong nhat cua hieu ung nay.
                float fresnel = pow(saturate(1.0 - saturate(dot(N, V))), _RimPower);

                float ndotl = saturate(dot(N, L));
                float shininess = exp2(_Smoothness * 11.0) + 2.0;
                float spec = pow(saturate(dot(N, H)), shininess) * _SpecStrength;

                float3 ambient = SampleSH(N);
                float3 body = _BaseColor.rgb * (ambient + light.color * ndotl * 0.6);
                float3 rim  = _RimColor.rgb * fresnel * _RimStrength;
                float3 hi   = _SpecColor2.rgb * light.color * spec;

                float3 col = body + rim + hi;

                // Vien va highlight lam tang do duc -> qua cau khong bi "bien mat" o ria,
                // dong thoi giu long qua cau trong de van doc duoc chu ben trong.
                float alpha = saturate(_BaseColor.a + fresnel * 0.55 + saturate(spec) * 0.4);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
