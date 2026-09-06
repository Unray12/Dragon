// Vong sang duoi chan nhan vat luc spawn (Character Ground Glow) - kieu hieu ung
// "circle of light" hay thay trong game khi nhan vat xuat hien.
//
// Viet tay HLSL thay vi Shader Graph vi Shader Graph khong tao duoc bang code (cung ly do
// da dung cho S_BubbleWater.shader).
//
// Ky thuat: 1 quad nam ngang duoi chan, dung SDF vong tron (khong phai texture) de ve 2
// vanh sang + tia quet xoay, alpha giam dan tu tam ra ngoai. Render kieu Additive (Blend
// One One) + ZWrite Off + khong Cull mat sau: day la hao quang phat sang, khong phai vat
// the co khoi, cong 2 mat vao anh nen giong nhu du Light Estimation chua toi (mobile AR
// khong the doi anh sang that theo huong nay - additive luon "noi" len duoc, chap nhan
// duoc cho hieu ung ban phep, khac voi mat cau nuoc dung Fresnel vi do la vat the CO KHOI
// can phan xa anh sang that).
Shader "DragonAR/GroundGlow"
{
    Properties
    {
        _GlowColor      ("Mau hao quang", Color) = (0.20, 0.95, 0.65, 1)
        _InnerRadius    ("Ban kinh vanh trong (0-0.5)", Range(0, 0.5)) = 0.12
        _OuterRadius    ("Ban kinh vanh ngoai (0-0.5)", Range(0, 0.5)) = 0.42
        _RingWidth      ("Do day vanh", Range(0.01, 0.3)) = 0.05
        _RotationSpeed  ("Toc do xoay tia quet (do/s)", Range(-360, 360)) = 40
        _PulseSpeed     ("Toc do nhap nhay", Range(0, 6)) = 1.6
        _Intensity      ("Do sang", Range(0, 6)) = 2.0
        _SpawnProgress  ("Tien trinh spawn (0=vua hien, 1=on dinh)", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "GroundGlowForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One   // Additive: hao quang cong sang vao nen, khong che nen
            ZWrite Off
            Cull Off        // quad nam ngang - nguoi dung co the nhin tu duoi len khi cui xuong

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor;
                float _InnerRadius;
                float _OuterRadius;
                float _RingWidth;
                float _RotationSpeed;
                float _PulseSpeed;
                float _Intensity;
                float _SpawnProgress;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv - 0.5; // tam quad = (0,0), ban kinh toi da = 0.5
                return OUT;
            }

            // Duong vien 1 vong tron ban kinh r, do day w, tam do la 1 (bang 0 ngoai vien).
            float RingMask(float dist, float r, float w)
            {
                return saturate(1.0 - abs(dist - r) / w);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float dist = length(IN.uv);
                float angle = atan2(IN.uv.y, IN.uv.x);

                // Vanh sang: vien ngoai + vien trong, do day theo _RingWidth.
                float outerRing = RingMask(dist, _OuterRadius, _RingWidth);
                float innerRing = RingMask(dist, _InnerRadius, _RingWidth * 0.6);

                // Nen mo giua 2 vanh: sang nhe toan bo vung ben trong _OuterRadius, giam dan
                // ra ngoai - de tong the trong nhu 1 vung sang chu khong chi 2 net vien mong.
                float fill = saturate(1.0 - dist / _OuterRadius);
                fill *= fill; // falloff bac 2 - tam sang ro, mep mo dan tu nhien hon tuyen tinh

                // Tia quet xoay quanh tam - chi tiet "dang chuyen dong" de hao quang khong
                // dung im nhu 1 hinh dan.
                float sweepAngle = angle + _RotationSpeed * _Time.y * (PI / 180.0);
                float sweep = pow(saturate(sin(sweepAngle * 3.0) * 0.5 + 0.5), 4.0);
                float sweepMask = sweep * saturate(1.0 - dist / _OuterRadius) * 0.6;

                float pulse = 0.85 + 0.15 * sin(_Time.y * _PulseSpeed * TWO_PI);

                float shape = (outerRing + innerRing * 0.7 + fill * 0.35 + sweepMask) * pulse;

                // Vua spawn (SpawnProgress=0): hao quang RONG HET CO va SANG HON, roi co lai
                // dung kich thuoc that trong luc mo dan ve nho hon - dung cam giac "nang
                // luong lan ra roi tu trong lai" thay vi bat sang dot ngot.
                float expand = lerp(1.6, 1.0, _SpawnProgress);
                float spawnDist = dist / expand;
                float spawnShape = (RingMask(spawnDist, _OuterRadius, _RingWidth)
                                    + saturate(1.0 - spawnDist / _OuterRadius) * 0.35) * pulse;
                float spawnBoost = lerp(2.2, 1.0, _SpawnProgress);

                float finalShape = max(shape, spawnShape) * spawnBoost;
                float alpha = saturate(finalShape) * saturate(1.0 - dist / (_OuterRadius + _RingWidth));

                // Ngoai ban kinh ngoai + do day vien mot chut thi cat han ve 0 - tranh 1 vung
                // mo rat nhat kip den het canh quad hinh vuong (lo hinh dang thuc la 1 quad).
                alpha *= step(dist, _OuterRadius + _RingWidth * 1.5);

                return half4(_GlowColor.rgb * _Intensity * alpha, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
