#include "UnityCG.cginc"
#include "Packages/com.vrchat.base/ShaderLibrary/VRCContacts.cginc"

VRC_CONTACT_DECLARE(Sigmal00_TsunTsun);
static float3 g_orgPosition = float3(0,0,0);
static float3 g_orgNormal = float3(0,0,1);
static float g_depth = 0;

// ファーのパスで LIL_CUSTOM_VERT_COPY の書き込み先にするダミー（custom.hlsl の LIL_V2F_OUT_BASE 参照）
struct TsunDummyVertCopy
{
    float3 orgPosition;
    float3 orgNormal;
    float depth;
};
static TsunDummyVertCopy g_tsunDummyVertCopy;

struct DeformationData
{
    float3 position;
    float3 normal;
    float depth;
};

struct DepthOutput
{
    float depth;
    float3 p;
    float3 n;
};

struct Capsule
{
    float3 from;
    float3 to;
};

inline float remap(float value, float omin, float omax, float nmin, float nmax)
{
    return nmin + (value - omin) * (nmax - nmin) / (omax - omin);
}

inline float smax(float a, float b, float k)
{
    float h = clamp(0.5 + 0.5*(a - b)/k, 0, 1);
    return lerp(b, a, h) + k*h*(1 - h);
}

// つんつんマスク（UV0、R チャンネル）。1 でそのまま凹み、0 で凹まない
// 頂点シェーダーでも読むのでミップレベルを指定してサンプリングする
inline float SampleTsunTsunMask(float2 uv)
{
    return LIL_SAMPLE_2D_LOD(_TsunTsunMask, sampler_TsunTsunMask, uv, 0).r;
}

inline float sdCapsule(float3 p, float3 a, float3 b, float r)
{
    float3 pa = p - a;
    float3 ba = b - a;
    float h = saturate(dot(pa,ba) / dot(ba,ba));
    return length(pa - ba*h) - r;
}

DepthOutput CalcDepth(float3 p, float3 n, float3 contactPos, float mask)
{
    DepthOutput o;

    const float3 v = contactPos - p;
    const float depthCap = _EffectiveDepth;
    const float rawDepth = -dot(v, n);
    const float maxDepth = clamp(rawDepth, 0, depthCap);

    const float distSqr = dot(v, v);
    const float tangentDist = sqrt(max(distSqr - maxDepth*maxDepth, 0));
    
    const float radius = _EffectiveRadius*remap(maxDepth, 0, depthCap, 0.25, 1);
    const float u = clamp(tangentDist / radius, 0, 1);
    const float w = 2*u*u*u - 3*u*u + 1;
    const float depth = w * maxDepth * mask;

    float3 gradDepth = float3(0,0,0);
    float3 gradU = float3(0,0,0);
    if (u > 0 && u < 1)
    {
        gradU = (-v - maxDepth*gradDepth) / (tangentDist * radius);
    }

    const float3 gradW = (6*u*u - 6*u) * gradU;
    const float3 g = (maxDepth*gradW + w*gradDepth) * mask;   // ∇(w·depth·mask)
    
    // メッシュが重なっているときに内側のメッシュがはみ出ないようにするためのバイアス値、シェーダー全体で固定したい
    const float bias = lerp(0.0, _TsunTsunBias, saturate(depth/depthCap));

    o.n = normalize(n + g - dot(g, n) * n);
    o.p = p - (depth - bias)*n;
    o.depth = depth;
    return o;
}

DepthOutput CalcFingerDepth(float3 p, float3 n, Capsule finger, float mask)
{
    DepthOutput o;
    o.depth = 0;
    o.p = p;
    o.n = n;

    const float stepCount = 20.0f;
    for(int i = 0; i < stepCount; ++i)
    {
        const float3 contactPoint = lerp(finger.from, finger.to, ((float)i)/stepCount);
        DepthOutput current = CalcDepth(p, n, contactPoint, mask);
        if(o.depth < current.depth)
        {
            o = current;
        }
    }
    return o;
}

float GetFingerLength(int contactNum)
{
    if (VRC_CONTACT_GET_TAG(Sigmal00_TsunTsun, contactNum, 0))
    {
        return _FingerIndexLength*_FingerLengthScale;
    }
    if (VRC_CONTACT_GET_TAG(Sigmal00_TsunTsun, contactNum, 1))
    {
        return _FingerMiddleLength*_FingerLengthScale;
    }
    if (VRC_CONTACT_GET_TAG(Sigmal00_TsunTsun, contactNum, 2))
    {
        return _FingerRingLength*_FingerLengthScale;
    }
    if (VRC_CONTACT_GET_TAG(Sigmal00_TsunTsun, contactNum, 3))
    {
        return _FingerLittleLength*_FingerLengthScale;
    }
    return 0.0f;
}

DeformationData DeformMesh(float3 position, float3 normal, float mask)
{
    const int contactCount = (int)VRC_CONTACT_TARGETCOUNT(Sigmal00_TsunTsun);
    const float effectiveRadius = _EffectiveRadius;
    const float effectiveDepth = _EffectiveDepth;

    // 全部の指をチェックして一番深くなるところを採用
    DepthOutput result;
    result.depth = 0;
    result.p = position;
    result.n = normal;

    [loop]
    // マスクが 0 の頂点は凹まないので計算しない
    const int loopCount = mask > 0 ? contactCount : 0;
    for (int i = 0; i < loopCount; i++)
    {
        const float fingerLength = GetFingerLength(i);

        Capsule finger;
        {
            const float4x4 fingerXform = VRC_CONTACT_TRANSFORM(Sigmal00_TsunTsun, i);
            finger.from = mul(unity_WorldToObject, mul(fingerXform, float4(0, 0, 0, 1)));
            finger.to = mul(unity_WorldToObject, mul(fingerXform, float4(0, -fingerLength, 0, 1)));
        }

        DepthOutput current = CalcFingerDepth(position, normal, finger, mask);

        if (result.depth < current.depth)
        {
            result = current;
        }
    }

    DeformationData outData;
    outData.depth = distance(position, result.p);
    outData.position = result.p;
    outData.normal = result.n;

    return outData;
}

float3 CalcDeformedNormalWS(float3 orgPositionOS, float3 orgNormalOS, float2 uv)
{
    // 補間すると長さが 1 からずれるので正規化してから使う
    DeformationData o = DeformMesh(orgPositionOS, normalize(orgNormalOS), SampleTsunTsunMask(uv));
    return UnityObjectToWorldNormal(o.normal);
}

void ModifyVertex(inout float3 position, inout float3 normal, inout float depth, float2 uv)
{
    DeformationData o = DeformMesh(position, normal, SampleTsunTsunMask(uv));
    position = o.position;
    normal = o.normal;
    depth = o.depth;
}

void ModifyFragment(inout float3 color, float3 wpos, float depth, float2 uv)
{
    const int contactCount = (int)VRC_CONTACT_TARGETCOUNT(Sigmal00_TsunTsun);
    const float effectiveRadius = _EffectiveRadius;
    const float effectiveDepth = _EffectiveDepth;

    float dist = effectiveRadius;

    [loop]
    for (int i = 0; i < contactCount; i++)
    {
        const float fingerLength = GetFingerLength(i);

        Capsule finger;
        {
            const float4x4 m = VRC_CONTACT_TRANSFORM(Sigmal00_TsunTsun, i);
            const float3 fingerPos = m._m03_m13_m23;
            const float3 axisY = -normalize(m._m01_m11_m21);
            finger.from = fingerPos;
            finger.to = fingerLength*axisY + fingerPos;
        }
        dist = min(dist, sdCapsule(wpos, finger.from, finger.to, effectiveRadius));
    }

    const float t = pow(saturate(-dist/effectiveRadius), 3) * SampleTsunTsunMask(uv);
    color *= lerp(1.0, _OcclusionColor.rgb, t);
}

//----------------------------------------------------------------------------------------------------------------------
// Tessellation
// 指（Contact）が近い辺ほど分割数を増やす。テッセレーション版のシェーダーのときだけ有効。
//
// lilToon の hullConst には差し込み口がないので、名前をマクロで別名に逃がして同名の関数をここで定義する。
// hull の [patchconstantfunc("hullConst")] は文字列なのでマクロで置き換わらず、こちらの関数が使われる。
// テッセレーションを使うのは前方レンダリングのパス（アウトライン含む）だけ。影や Meta のパスでは何もしない
#if defined(LIL_TESSELLATION) && (defined(LIL_PASS_FORWARD) || defined(LIL_PASS_FORWARDADD))

// appdataCopy を使うために先に include する（lil_pass_forward_normal.hlsl でも直後に include されるが、多重 include は防止されている）
// ただし appdata は「LIL_PASS_FORWARD_NORMAL_INCLUDED && LIL_SHOULD_TANGENT」のとき接線を入力に含める。
// ここではまだ LIL_PASS_FORWARD_NORMAL_INCLUDED が未定義なので、同じ条件を先に再現しておく
#if defined(LIL_SHOULD_TANGENT) && !defined(LIL_REQUIRE_APP_TANGENT)
    #define LIL_REQUIRE_APP_TANGENT
#endif
#include "Packages/jp.lilxyzw.liltoon/Shader/Includes/lil_common_appdata.hlsl"

// ワールド座標 p から一番近い指（DeformMesh と同じカプセル）までの距離
float TsunContactDistanceWS(float3 p)
{
    const int contactCount = (int)VRC_CONTACT_TARGETCOUNT(Sigmal00_TsunTsun);
    float dist = 1e30;

    [loop]
    for (int i = 0; i < contactCount; i++)
    {
        const float fingerLength = GetFingerLength(i);
        const float4x4 fingerXform = VRC_CONTACT_TRANSFORM(Sigmal00_TsunTsun, i);
        const float3 from = mul(fingerXform, float4(0, 0, 0, 1)).xyz;
        const float3 to = mul(fingerXform, float4(0, -fingerLength, 0, 1)).xyz;
        dist = min(dist, sdCapsule(p, from, to, 0.0f));
    }
    return dist;
}

// 指の近くの辺に使う分割数
// 辺の両端と中点のうち一番指に近い距離で重みを決める（隣の三角形と同じ辺は同じ値になるので割れ目は出ない）
// つんつんマスクも辺の両端と中点の最大値を掛ける（三角形の内側だけ白い場合の取りこぼしを中点で減らす）
float TsunContactTessFactor(float3 wpos0, float3 wpos1, float2 uv0, float2 uv1)
{
    const float mask = max(max(SampleTsunTsunMask(uv0), SampleTsunTsunMask(uv1)),
                           SampleTsunTsunMask(0.5 * (uv0 + uv1)));
    // マスクが黒い辺は凹まないので分割しない（指との距離計算も省く）
    if (mask <= 0) return 0;

    const float d = min(min(TsunContactDistanceWS(wpos0), TsunContactDistanceWS(wpos1)),
                        TsunContactDistanceWS(0.5 * (wpos0 + wpos1)));

    const float range = max(_EffectiveRadius + _EffectiveDepth, 1e-5);      // 凹みが届くくらいの範囲
    const float w = saturate(1.0 - d / range);
    const float targetEdgeLength = max(_EffectiveRadius * 0.25, 1e-5);      // 指の近くで目指す辺の長さ
    return distance(wpos0, wpos1) / targetEdgeLength * w * mask;
}

struct TsunTessellationFactors
{
    float edge[3] : SV_TessFactor;
    float inside  : SV_InsideTessFactor;
};

// lilToon の hullConst（lil_tessellation.hlsl）と同じ処理 ＋ 指の近くの分割
TsunTessellationFactors hullConst(InputPatch<appdataCopy, 3> input)
{
    TsunTessellationFactors output;
    LIL_INITIALIZE_STRUCT(TsunTessellationFactors, output);
    LIL_SETUP_INSTANCE_ID(input[0]);

    if(_Invisible) return output;

    LIL_VERTEX_POSITION_INPUTS(input[0].positionOS, vertexInput_0);
    LIL_VERTEX_POSITION_INPUTS(input[1].positionOS, vertexInput_1);
    LIL_VERTEX_POSITION_INPUTS(input[2].positionOS, vertexInput_2);
    LIL_VERTEX_NORMAL_INPUTS(input[0].normalOS, vertexNormalInput_0);
    LIL_VERTEX_NORMAL_INPUTS(input[1].normalOS, vertexNormalInput_1);
    LIL_VERTEX_NORMAL_INPUTS(input[2].normalOS, vertexNormalInput_2);

    // lilToon 標準：画面上の辺の長さで分割（輪郭ほど強く）
    float4 tessFactor;
    tessFactor.x = lilCalcEdgeTessFactor(vertexInput_1.positionWS, vertexInput_2.positionWS, _TessEdge);
    tessFactor.y = lilCalcEdgeTessFactor(vertexInput_2.positionWS, vertexInput_0.positionWS, _TessEdge);
    tessFactor.z = lilCalcEdgeTessFactor(vertexInput_0.positionWS, vertexInput_1.positionWS, _TessEdge);
    tessFactor.xyz = min(tessFactor.xyz, _TessFactorMax);

    // Rim
    float3 nv = float3(abs(dot(vertexNormalInput_0.normalWS, normalize(lilViewDirection(lilToAbsolutePositionWS(vertexInput_0.positionWS))))),
                       abs(dot(vertexNormalInput_1.normalWS, normalize(lilViewDirection(lilToAbsolutePositionWS(vertexInput_1.positionWS))))),
                       abs(dot(vertexNormalInput_2.normalWS, normalize(lilViewDirection(lilToAbsolutePositionWS(vertexInput_2.positionWS))))));
    nv = saturate(1.0 - float3(nv.y + nv.z, nv.z + nv.x, nv.x + nv.y) * 0.5);
    tessFactor.xyz = max(tessFactor.xyz * nv * nv, 1.0);

    // 追加：指の近くの辺は、向きに関係なく細かく分割する（上限は lilToon の Tess Factor Max）
    float3 contactFactor;
    contactFactor.x = TsunContactTessFactor(vertexInput_1.positionWS, vertexInput_2.positionWS, input[1].uv0, input[2].uv0);
    contactFactor.y = TsunContactTessFactor(vertexInput_2.positionWS, vertexInput_0.positionWS, input[2].uv0, input[0].uv0);
    contactFactor.z = TsunContactTessFactor(vertexInput_0.positionWS, vertexInput_1.positionWS, input[0].uv0, input[1].uv0);
    tessFactor.xyz = max(tessFactor.xyz, min(contactFactor, _TessFactorMax));

    tessFactor.w = (tessFactor.x+tessFactor.y+tessFactor.z) / 3.0;

    // Cull out of screen
    float4 pos[3] = {vertexInput_0.positionCS, vertexInput_1.positionCS, vertexInput_2.positionCS};
    pos[0].xy = pos[0].xy/abs(pos[0].w);
    pos[1].xy = pos[1].xy/abs(pos[1].w);
    pos[2].xy = pos[2].xy/abs(pos[2].w);
    tessFactor = (pos[0].x >  1.01 && pos[1].x >  1.01 && pos[2].x >  1.01) ||
                 (pos[0].x < -1.01 && pos[1].x < -1.01 && pos[2].x < -1.01) ||
                 (pos[0].y >  1.01 && pos[1].y >  1.01 && pos[2].y >  1.01) ||
                 (pos[0].y < -1.01 && pos[1].y < -1.01 && pos[2].y < -1.01) ? 0.0 : tessFactor;

    output.edge[0] = tessFactor.x;
    output.edge[1] = tessFactor.y;
    output.edge[2] = tessFactor.z;
    output.inside  = tessFactor.w;

    return output;
}

// lilToon 側の hullConst は別名にして使わない
#define hullConst lilHullConstOriginal

#endif
