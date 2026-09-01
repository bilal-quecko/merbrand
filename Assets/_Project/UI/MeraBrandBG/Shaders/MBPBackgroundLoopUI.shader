// Mera Brand Pakistan - seamless background loop, Canvas/UI variant.
//
// Use this one on a UI Image or RawImage. It is a proper UI shader: it respects
// masks, RectMask2D clipping, CanvasGroup alpha and Graphic colour tint, and it
// remaps sprite-atlas UVs back to 0..1 via _UVRect (set for you by
// MBPBackgroundLoop.cs) so the effect rects stay where they belong.
//
// Works in Built-in RP and URP. No RenderPipeline tag, deliberately.

Shader "MeraBrandPakistan/BackgroundLoopUI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color          ("Tint", Color) = (1,1,1,1)
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

        // --- standard UI plumbing, written by Mask/RectMask2D at runtime ---
        _StencilComp      ("Stencil Comparison", Float) = 8
        _Stencil          ("Stencil ID", Float) = 0
        _StencilOp        ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask", Float) = 255
        _ColorMask        ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref  [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "MBP_UI"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "MBPBackgroundCore.cginc"

            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex        = UnityObjectToClipPos(v.vertex);
                o.texcoord      = v.texcoord;
                o.color         = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // atlas UV -> 0..1 across the artwork
                float2 uvLocal = (i.texcoord - _UVRect.xy) / max(_UVRect.zw, 1e-5);

                float3 rgb = MBP_Background(uvLocal);
                float  a   = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd).a;

                half4 col = half4(rgb, a) * i.color;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
