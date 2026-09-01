// Mera Brand Pakistan - shared seamless-loop animation core.
// Included by both MBPBackgroundLoop.shader (world/sprite) and
// MBPBackgroundLoopUI.shader (Canvas). Keep the maths in here only, so the two
// variants can never drift apart.

#ifndef MBP_BACKGROUND_CORE_INCLUDED
#define MBP_BACKGROUND_CORE_INCLUDED

#define MBP_TAU 6.28318530718

sampler2D _MainTex;
float4 _MainTex_ST;

float  _LoopT;
float4 _UVRect;              // sub-rect of the texture this quad shows: (x, y, w, h)
float  _Drift, _Pan;
float4 _FlagRect;
float  _FlagAmp, _FlagFreq, _FlagCycles;
float  _RibbonTop, _RibbonIntensity, _RibbonFreq, _RibbonCycles, _RibbonSharp;
float4 _MapRect;
float  _MapPulse, _MapCycles, _ScanIntensity, _ScanWidth;
float4 _LogoRect;
float  _Shimmer, _ShimmerWidth;
float  _Twinkle, _Haze, _VignettePulse;

float MBP_RectMask(float2 uv, float4 r, float feather)
{
    float2 a = smoothstep(r.xy, r.xy + feather, uv);
    float2 b = smoothstep(r.zw, r.zw - feather, uv);
    return a.x * a.y * b.x * b.y;
}

float MBP_Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

// A sweep that is fully off-screen at both t=0 and t=1, so it cannot pop at the wrap.
float MBP_Sweep(float coord, float t, float width)
{
    float pad    = width * 2.0;
    float center = -pad + t * (1.0 + pad * 2.0);
    float d      = abs(coord - center) / width;
    return exp(-d * d * 3.0);
}

// uvLocal: 0..1 across the artwork, y up. Atlas remapping happens before this call.
float3 MBP_Background(float2 uvLocal)
{
    float t = _LoopT;

    // ---------- camera breath (zoom inward only, so no edge bleed) ----------
    float zoom = 1.0 + _Drift * (0.5 - 0.5 * cos(MBP_TAU * t));
    float2 uv  = (uvLocal - 0.5) / zoom + 0.5;
    uv.x += _Pan * sin(MBP_TAU * t);
    uv.y += _Pan * 0.5 * cos(MBP_TAU * t);

    // ---------- flag ripple ----------
    float flagM   = MBP_RectMask(uv, _FlagRect, 0.05);
    float falloff = saturate((uv.x - _FlagRect.x) / max(1e-4, _FlagRect.z - _FlagRect.x));
    falloff = falloff * falloff;                       // still at the pole, loose at the fly
    float w1 = sin(uv.x * _FlagFreq - MBP_TAU * t * _FlagCycles + uv.y * 4.0);
    float w2 = cos(uv.y * _FlagFreq * 0.6 - MBP_TAU * t * _FlagCycles);
    uv.y += w1 * _FlagAmp * flagM * falloff;
    uv.x += w2 * _FlagAmp * 0.35 * flagM * falloff;

    uv = clamp(uv, 0.0015, 0.9985);

    // back to atlas space for the actual fetch
    float2 uvTex = _UVRect.xy + uv * _UVRect.zw;
    float3 col = tex2D(_MainTex, uvTex).rgb;

    float lum = dot(col, float3(0.299, 0.587, 0.114));
    float gb  = saturate(col.g - col.b);               // brand green over the navy field

    // ---------- light ribbons ----------
    float band       = 1.0 - smoothstep(_RibbonTop * 0.55, _RibbonTop, uv.y);
    float ribbonMask = smoothstep(0.05, 0.32, lum) * smoothstep(0.015, 0.11, gb) * band;
    float flow       = pow(0.5 + 0.5 * sin(uv.x * _RibbonFreq - MBP_TAU * t * _RibbonCycles), _RibbonSharp);
    col += ribbonMask * flow * _RibbonIntensity * float3(0.30, 1.0, 0.50);

    // ---------- network map: node pulse + data sweep ----------
    float mapM = MBP_RectMask(uv, _MapRect, 0.07);
    // stone and snow are neutral (high red); the map nodes are teal, so gate on red
    float notStone = 1.0 - smoothstep(0.05, 0.22, col.r);
    float nodeM    = smoothstep(0.09, 0.38, lum) * notStone * mapM;
    float pulse    = 0.5 + 0.5 * sin(MBP_TAU * t * _MapCycles + uv.x * 9.0 + uv.y * 5.0);
    col += nodeM * pulse * _MapPulse * float3(0.35, 1.0, 0.72);

    float diag = saturate((uv.x - _MapRect.x) / max(1e-4, _MapRect.z - _MapRect.x) * 0.65
                        + (1.0 - saturate((uv.y - _MapRect.y) / max(1e-4, _MapRect.w - _MapRect.y))) * 0.35);
    col += nodeM * MBP_Sweep(diag, t, _ScanWidth) * _ScanIntensity * float3(0.45, 1.0, 0.80);

    // ---------- logo shimmer ----------
    float logoM = MBP_RectMask(uv, _LogoRect, 0.03) * smoothstep(0.30, 0.75, lum);
    float lx    = saturate((uv.x - _LogoRect.x) / max(1e-4, _LogoRect.z - _LogoRect.x));
    col += logoM * MBP_Sweep(lx, t, _ShimmerWidth) * _Shimmer * float3(0.85, 1.0, 0.90);

    // ---------- sky twinkle ----------
    float2 cell = floor(uv * float2(150.0, 90.0));
    float  h    = MBP_Hash21(cell);
    float  star = step(0.9955, h) * (0.5 + 0.5 * sin(MBP_TAU * t * 2.0 + h * MBP_TAU));
    float  sky  = smoothstep(0.55, 0.85, uv.y) * (1.0 - smoothstep(0.06, 0.22, lum));
    col += star * sky * _Twinkle * float3(0.7, 1.0, 0.9);

    // ---------- horizon haze ----------
    float hazeBand = smoothstep(0.18, 0.42, uv.y) * (1.0 - smoothstep(0.42, 0.66, uv.y));
    float hazeMove = 0.5 + 0.5 * sin(MBP_TAU * t + uv.x * 3.5);
    col += hazeBand * hazeMove * _Haze * 0.05 * float3(0.25, 1.0, 0.65);

    // ---------- breathing vignette ----------
    float2 d   = uvLocal - 0.5;
    float  vig = 1.0 - dot(d, d) * (0.55 + _VignettePulse * (0.5 - 0.5 * cos(MBP_TAU * t)));
    col *= saturate(vig);

    return col;
}

#endif
