// Mera Brand Pakistan - Family Expo 2026
// Seamless looping background animation.
//
// Every animated term is a function of _LoopT (0..1) built from either
//   sin/cos(TAU * _LoopT * N)        with integer N, or
//   a sweep that starts and ends fully off-screen
// so frame(_LoopT = 1) is bit-identical to frame(_LoopT = 0). No pop, ever.
//
// Works in Built-in RP and URP (unlit, no SRP-batcher dependency).

Shader "MeraBrandPakistan/BackgroundLoop"
{
    Properties
    {
        _MainTex        ("Background", 2D) = "white" {}
        _LoopT          ("Loop T (0-1, driven by script)", Range(0,1)) = 0
        _UVRect         ("Atlas Sub-Rect (x,y,w,h)", Vector) = (0,0,1,1)

        [Header(Camera Breath)]
        _Drift          ("Breath Zoom", Range(0, 0.06)) = 0.016
        _Pan            ("Pan Amount", Range(0, 0.012)) = 0.0025

        [Header(Flag)]
        _FlagRect       ("Flag Rect (x0,y0,x1,y1)", Vector) = (0.0, 0.54, 0.21, 0.96)
        _FlagAmp        ("Flag Ripple", Range(0, 0.02)) = 0.0055
        _FlagFreq       ("Flag Frequency", Range(4, 60)) = 26
        _FlagCycles     ("Flag Cycles (integer)", Range(1, 6)) = 2

        [Header(Light Ribbons)]
        _RibbonTop      ("Ribbon Band Top (uv.y)", Range(0.1, 0.6)) = 0.34
        _RibbonIntensity("Ribbon Intensity", Range(0, 3)) = 1.15
        _RibbonFreq     ("Ribbon Frequency", Range(2, 40)) = 13
        _RibbonCycles   ("Ribbon Cycles (integer)", Range(1, 6)) = 1
        _RibbonSharp    ("Ribbon Sharpness", Range(1, 10)) = 4

        [Header(Network Map)]
        _MapRect        ("Map Rect (x0,y0,x1,y1)", Vector) = (0.55, 0.50, 0.94, 1.0)
        _MapPulse       ("Node Pulse", Range(0, 2)) = 0.40
        _MapCycles      ("Node Pulse Cycles (integer)", Range(1, 6)) = 2
        _ScanIntensity  ("Data Sweep", Range(0, 3)) = 0.8
        _ScanWidth      ("Sweep Width", Range(0.02, 0.5)) = 0.13

        [Header(Logo)]
        _LogoRect       ("Logo Rect (x0,y0,x1,y1)", Vector) = (0.31, 0.62, 0.72, 0.91)
        _Shimmer        ("Logo Shimmer", Range(0, 3)) = 0.7
        _ShimmerWidth   ("Shimmer Width", Range(0.02, 0.4)) = 0.09

        [Header(Atmosphere)]
        _Twinkle        ("Sky Twinkle", Range(0, 1)) = 0.35
        _Haze           ("Horizon Haze", Range(0, 1)) = 0.3
        _VignettePulse  ("Vignette Pulse", Range(0, 0.3)) = 0.05
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Lighting Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "MBPBackgroundCore.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uvLocal = (i.uv - _UVRect.xy) / max(_UVRect.zw, 1e-5);
                return fixed4(MBP_Background(uvLocal), 1.0);
            }

            ENDCG
        }
    }
    FallBack "Unlit/Texture"
}
