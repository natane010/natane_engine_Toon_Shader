// natane_brush_native.cpp - Natane Brush Engine Native DLL (single-file build)
// Build: cl.exe /O2 /MT /EHsc /std:c++17 /LD /utf-8 /DNDEBUG /fp:fast /FeNataneBrushNative.dll natane_brush_native.cpp /link /DLL

#ifdef _WIN32
  #define NATANE_API extern "C" __declspec(dllexport)
#else
  #define NATANE_API extern "C" __attribute__((visibility("default")))
#endif

#include <cstdint>
#include <cmath>
#include <cstring>
#include <algorithm>
#include <vector>
#include <thread>
#include <functional>
#include <atomic>
#ifdef _WIN32
  #ifndef NOMINMAX
    #define NOMINMAX
  #endif
  #ifndef WIN32_LEAN_AND_MEAN
    #define WIN32_LEAN_AND_MEAN
  #endif
  #include <windows.h>
#endif

#if defined(__x86_64__) || defined(_M_X64)
  #define NATANE_X86 1
  #include <immintrin.h>
  #ifdef _MSC_VER
    #include <intrin.h>
    static inline bool natane_has_avx2() { int info[4]; __cpuidex(info,7,0); return (info[1]&(1<<5))!=0; }
  #endif
#elif defined(__aarch64__) || defined(_M_ARM64)
  #define NATANE_ARM64 1
  #include <arm_neon.h>
#endif

#define NATANE_API_VERSION 1
#define NATANE_CAP_SSE  1
#define NATANE_CAP_AVX2 2
#define NATANE_CAP_NEON 4
static const float LUMA_R = 0.2126f, LUMA_G = 0.7152f, LUMA_B = 0.0722f;

static void par_for(int s, int e, std::function<void(int)> fn, int grain = 1024) {
    int n = e - s;
    if (n <= grain) { for (int i = s; i < e; i++) fn(i); return; }
    int nt = std::min((int)std::thread::hardware_concurrency(), 8);
    if (nt <= 1) { for (int i = s; i < e; i++) fn(i); return; }
    int ch = (n + nt - 1) / nt;
    std::vector<std::thread> ts;
    for (int t = 0; t < nt; t++) {
        int a = s + t * ch, b = std::min(a + ch, e);
        if (a >= b) break;
        if (t == nt - 1) { for (int i = a; i < b; i++) fn(i); }
        else ts.emplace_back([fn, a, b]() { for (int i = a; i < b; i++) fn(i); });
    }
    for (auto& th : ts) th.join();
}

static inline float cl01(float x) { return x < 0.f ? 0.f : (x > 1.f ? 1.f : x); }

static inline float bch(float b, float t, int m) {
    switch (m) {
        case 0:  return t;
        case 1:  return b * t;
        case 2:  return 1.f-(1.f-b)*(1.f-t);
        case 3:  return b<.5f ? 2.f*b*t : 1.f-2.f*(1.f-b)*(1.f-t);
        case 4:  return (1.f-2.f*t)*b*b + 2.f*t*b;
        case 5:  return t<.5f ? 2.f*b*t : 1.f-2.f*(1.f-b)*(1.f-t);
        case 6:  return t>=1.f ? 1.f : cl01(b/(1.f-t));
        case 7:  return t<=0.f ? 0.f : cl01(1.f-(1.f-b)/t);
        case 8:  return cl01(b+t);
        case 9:  return cl01(b-t);
        case 10: return std::abs(b-t);
        case 11: return b+t-2.f*b*t;
        default: return t;
    }
}

static void comp_px(float* d, const float* bs, const float* tp, float op, int m) {
    float sa = tp[3] * op;
    if (sa <= 0.0001f) { d[0]=bs[0]; d[1]=bs[1]; d[2]=bs[2]; d[3]=bs[3]; return; }
    float br=bch(bs[0],tp[0],m), bg=bch(bs[1],tp[1],m), bb=bch(bs[2],tp[2],m);
    float ba = bs[3], oa = sa + ba*(1.f-sa);
    if (oa > 0.0001f) {
        float ia=1.f/oa, is=1.f-sa;
        d[0]=cl01((br*sa+bs[0]*ba*is)*ia);
        d[1]=cl01((bg*sa+bs[1]*ba*is)*ia);
        d[2]=cl01((bb*sa+bs[2]*ba*is)*ia);
    } else { d[0]=d[1]=d[2]=0.f; }
    d[3]=cl01(oa);
}

// ===== Exports =====
NATANE_API int NataneGetVersion() { return NATANE_API_VERSION; }
NATANE_API int GetNativeCapabilities() {
    int c=0;
#if NATANE_X86
    c|=NATANE_CAP_SSE;
    #ifdef _MSC_VER
    if(natane_has_avx2()) c|=NATANE_CAP_AVX2;
    #endif
#endif
#if NATANE_ARM64
    c|=NATANE_CAP_NEON;
#endif
    return c;
}

NATANE_API void ApplyBrushDab(
    float* cv, int w, int h, float cx, float cy, float rad, float hard, float op,
    float r, float g, float b, float a, int mode)
{
    if (!cv||w<=0||h<=0||rad<=0.f||op<=0.f) return;
    hard=cl01(hard); op=cl01(op);
    int x0=std::max(0,(int)std::floor(cx-rad)), y0=std::max(0,(int)std::floor(cy-rad));
    int x1=std::min(w-1,(int)std::ceil(cx+rad)), y1=std::min(h-1,(int)std::ceil(cy+rad));
    float rs=rad*rad, inn=rad*hard, is=inn*inn, rng=rs-is;
    float brush[4]={r,g,b,a};
    par_for(y0, y1+1, [&](int py) {
        float dy=(float)py-cy, d2y=dy*dy;
        for (int px=x0;px<=x1;px++) {
            float dx=(float)px-cx, d2=dx*dx+d2y;
            if (d2>rs) continue;
            float s; if (d2<=is) s=1.f; else if (rng>0.0001f){float t=(d2-is)/rng;s=1.f-(t*t*(3.f-2.f*t));} else s=1.f;
            float dop=s*op; if(dop<=0.0001f) continue;
            int idx=(py*w+px)*4; float res[4];
            comp_px(res,&cv[idx],brush,dop,mode);
            cv[idx]=res[0];cv[idx+1]=res[1];cv[idx+2]=res[2];cv[idx+3]=res[3];
        }
    }, 4);
}

NATANE_API void BlendLayerRegion(const float* bot, const float* top, float* res,
    int w, int h, int rx, int ry, int rw, int rh, float op, int bm)
{
    if (!bot||!top||!res||w<=0||h<=0||rw<=0||rh<=0) return;
    int x0=std::max(0,rx),y0=std::max(0,ry),x1=std::min(w,rx+rw),y1=std::min(h,ry+rh);
    if (x0>=x1||y0>=y1) return;
    par_for(y0, y1, [&](int y) {
        for (int x=x0;x<x1;x++) { int i=(y*w+x)*4; comp_px((float*)&res[i],&bot[i],&top[i],op,bm); }
    }, 16);
}

NATANE_API void BlendPixelsFull(const float* bot, const float* top, float* res, int total, float op, int bm) {
    if (!bot||!top||!res||total<=0) return;
    par_for(0, total, [&](int i) { int idx=i*4; comp_px((float*)&res[idx],&bot[idx],&top[idx],op,bm); }, 1024);
}

NATANE_API void GaussianBlurSeparable(float* px, int w, int h, float sig, int rad) {
    if (!px||w<=0||h<=0||sig<=0.f||rad<=0) return;
    rad=std::min(rad,128); int ks=2*rad+1;
    std::vector<float> k(ks); float s2=2.f*sig*sig, sum=0.f;
    for (int i=0;i<ks;i++){float x=(float)(i-rad);k[i]=std::exp(-(x*x)/s2);sum+=k[i];}
    for (int i=0;i<ks;i++) k[i]/=sum;
    std::vector<float> tmp(w*h*4);
    par_for(0,h,[&](int y){for(int x=0;x<w;x++){float ar=0,ag=0,ab=0,aa=0;for(int j=-rad;j<=rad;j++){int sx=std::max(0,std::min(w-1,x+j));int si=(y*w+sx)*4;float kw=k[j+rad];ar+=px[si]*kw;ag+=px[si+1]*kw;ab+=px[si+2]*kw;aa+=px[si+3]*kw;}int di=(y*w+x)*4;tmp[di]=ar;tmp[di+1]=ag;tmp[di+2]=ab;tmp[di+3]=aa;}},16);
    par_for(0,h,[&](int y){for(int x=0;x<w;x++){float ar=0,ag=0,ab=0,aa=0;for(int j=-rad;j<=rad;j++){int sy=std::max(0,std::min(h-1,y+j));int si=(sy*w+x)*4;float kw=k[j+rad];ar+=tmp[si]*kw;ag+=tmp[si+1]*kw;ab+=tmp[si+2]*kw;aa+=tmp[si+3]*kw;}int di=(y*w+x)*4;px[di]=ar;px[di+1]=ag;px[di+2]=ab;px[di+3]=aa;}},16);
}

NATANE_API void GenerateBrushMask(float* mask, int dia, float hard) {
    if (!mask||dia<=0) return; hard=cl01(hard);
    float ctr=(float)(dia-1)*.5f,rd=ctr,rs=rd*rd,inn=rd*hard,is=inn*inn,rng=rs-is;
    par_for(0,dia,[&](int y){float dy=(float)y-ctr,d2y=dy*dy;for(int x=0;x<dia;x++){float dx=(float)x-ctr,d2=dx*dx+d2y;float v;if(d2>rs)v=0.f;else if(d2<=is)v=1.f;else if(rng>0.0001f){float t=(d2-is)/rng;v=1.f-(t*t*(3.f-2.f*t));}else v=1.f;mask[y*dia+x]=v;}},64);
}

NATANE_API void SobelEdgeDetect(float* px, int w, int h, float str) {
    if (!px||w<3||h<3) return;
    std::vector<float> inp(w*h*4); std::memcpy(inp.data(),px,w*h*4*sizeof(float));
    auto lm=[&](int x,int y)->float{x=std::max(0,std::min(w-1,x));y=std::max(0,std::min(h-1,y));int i=(y*w+x)*4;return inp[i]*LUMA_R+inp[i+1]*LUMA_G+inp[i+2]*LUMA_B;};
    par_for(0,h,[&](int y){for(int x=0;x<w;x++){float gx=-lm(x-1,y-1)+lm(x+1,y-1)-2*lm(x-1,y)+2*lm(x+1,y)-lm(x-1,y+1)+lm(x+1,y+1);float gy=-lm(x-1,y-1)-2*lm(x,y-1)-lm(x+1,y-1)+lm(x-1,y+1)+2*lm(x,y+1)+lm(x+1,y+1);float mg=std::min(1.f,std::sqrt(gx*gx+gy*gy)*str);int i=(y*w+x)*4;px[i]=mg;px[i+1]=mg;px[i+2]=mg;}},16);
}

NATANE_API void AdjustLevels(float* px, int tot, float ib, float iw, float gm, float ob, float ow) {
    if (!px||tot<=0) return;
    float ir=iw-ib; if(ir<0.0001f)ir=0.0001f; float ii=1.f/ir,ig=(gm>0.0001f)?(1.f/gm):1.f,or2=ow-ob;
    par_for(0,tot,[&](int i){int idx=i*4;for(int c=0;c<3;c++){float v=(px[idx+c]-ib)*ii;v=cl01(v);if(ig!=1.f)v=std::pow(v,ig);px[idx+c]=cl01(ob+v*or2);}},1024);
}

NATANE_API void Desaturate(float* px, int tot) {
    if (!px||tot<=0) return;
    par_for(0,tot,[&](int i){int idx=i*4;float l=px[idx]*LUMA_R+px[idx+1]*LUMA_G+px[idx+2]*LUMA_B;px[idx]=l;px[idx+1]=l;px[idx+2]=l;},1024);
}

// ===== Pen Pressure via WM_POINTER API (Windows 8+) =====
// VCC対応: Unity Input Systemに依存せずWindowsから直接筆圧を取得
#ifdef _WIN32
#include <commctrl.h>
#pragma comment(lib, "comctl32.lib")

#ifndef WM_POINTERUPDATE
  #define WM_POINTERUPDATE 0x0245
  #define WM_POINTERDOWN   0x0246
  #define WM_POINTERUP     0x0247
  #define GET_POINTERID_WPARAM(wp) (LOWORD(wp))
#endif
#ifndef PT_PEN
  #define PT_PEN 3
#endif

typedef BOOL (WINAPI *PFN_GetPointerType)(UINT32, DWORD*);
typedef BOOL (WINAPI *PFN_GetPointerPenInfo)(UINT32, POINTER_PEN_INFO*);

static PFN_GetPointerType    s_fnGetPointerType    = nullptr;
static PFN_GetPointerPenInfo s_fnGetPointerPenInfo = nullptr;
static bool s_pointerApiLoaded = false;

static std::atomic<float> s_penPressure{0.f};
static std::atomic<float> s_penTiltX{0.f};
static std::atomic<float> s_penTiltY{0.f};
static std::atomic<int>   s_penActive{0};
static HWND  s_hookedHwnd = nullptr;
static const UINT_PTR SUBCLASS_ID = 0x4E415441; // "NATA"

static bool LoadPointerApi() {
    if (s_pointerApiLoaded) return s_fnGetPointerPenInfo != nullptr;
    s_pointerApiLoaded = true;
    HMODULE u32 = GetModuleHandleW(L"user32.dll");
    if (!u32) return false;
    s_fnGetPointerType    = (PFN_GetPointerType)GetProcAddress(u32, "GetPointerType");
    s_fnGetPointerPenInfo = (PFN_GetPointerPenInfo)GetProcAddress(u32, "GetPointerPenInfo");
    return s_fnGetPointerPenInfo != nullptr;
}

static LRESULT CALLBACK PenSubclassProc(HWND hw, UINT msg, WPARAM wp, LPARAM lp,
    UINT_PTR, DWORD_PTR) {
    if ((msg == WM_POINTERUPDATE || msg == WM_POINTERDOWN || msg == WM_POINTERUP)
        && s_fnGetPointerType && s_fnGetPointerPenInfo) {
        UINT32 pid = GET_POINTERID_WPARAM(wp);
        DWORD ptype = 0;
        if (s_fnGetPointerType(pid, &ptype) && ptype == PT_PEN) {
            POINTER_PEN_INFO ppi = {};
            if (s_fnGetPointerPenInfo(pid, &ppi)) {
                float p = ppi.pressure > 0 ? (float)ppi.pressure / 1024.f : 0.f;
                if (p > 1.f) p = 1.f;
                s_penPressure.store(p, std::memory_order_relaxed);
                s_penTiltX.store((float)ppi.tiltX, std::memory_order_relaxed);
                s_penTiltY.store((float)ppi.tiltY, std::memory_order_relaxed);
                s_penActive.store(1, std::memory_order_relaxed);
            }
        }
    }
    if (msg == WM_NCDESTROY) {
        RemoveWindowSubclass(hw, PenSubclassProc, SUBCLASS_ID);
        s_hookedHwnd = nullptr;
    }
    return DefSubclassProc(hw, msg, wp, lp);
}

NATANE_API int InitPenPressure(void* hwnd) {
    if (!LoadPointerApi()) return 0;
    if (s_hookedHwnd) { RemoveWindowSubclass(s_hookedHwnd, PenSubclassProc, SUBCLASS_ID); s_hookedHwnd = nullptr; }
    HWND h = (HWND)hwnd;
    if (!h) h = GetForegroundWindow();
    if (!h || !SetWindowSubclass(h, PenSubclassProc, SUBCLASS_ID, 0)) return 0;
    s_hookedHwnd = h;
    s_penPressure.store(0.f); s_penActive.store(0);
    return 1;
}
NATANE_API void ShutdownPenPressure() {
    if (s_hookedHwnd) { RemoveWindowSubclass(s_hookedHwnd, PenSubclassProc, SUBCLASS_ID); s_hookedHwnd = nullptr; }
    s_penPressure.store(0.f); s_penActive.store(0);
}
NATANE_API float GetPenPressure() { return s_penPressure.load(std::memory_order_relaxed); }
NATANE_API float GetPenTiltX()    { return s_penTiltX.load(std::memory_order_relaxed); }
NATANE_API float GetPenTiltY()    { return s_penTiltY.load(std::memory_order_relaxed); }
NATANE_API int   IsPenActive()    { return s_penActive.load(std::memory_order_relaxed); }
NATANE_API int   IsPenPressureAvailable() { return LoadPointerApi() ? 1 : 0; }
#endif
