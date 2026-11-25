using UnityEngine;
using VRC.Dynamics;
using VRC.SDKBase;

namespace Sigmal00.TsunTsunShader
{
    /// <summary>
    /// ビルド時（NDMF）に、TsunTsunShader 用に設定済みの VRCContactReceiver をこの GameObject に生成するコンポーネント。
    /// 利用者が決めるのは追従先・形状・フィルタ・書き込み先レンダラーだけで、
    /// コリジョンタグやパラメータ名などシェーダーと合わせる必要がある項目は生成時に固定で設定する。
    /// </summary>
    [AddComponentMenu("Sigmal00/TsunTsun Receiver")]
    [DisallowMultipleComponent]
    public class TsunTsunReceiver : MonoBehaviour, IEditorOnly
    {
        [Tooltip("ContactReceiver が追従する Transform（例: Head）。空ならこの GameObject を基準にします。")]
        public Transform rootTransform;

        [Header("Shape")]
        public ContactBase.ShapeType shapeType = ContactBase.ShapeType.Sphere;
        [Tooltip("Sphere / Capsule の半径 (m)")]
        public float radius = 0.5f;
        [Tooltip("Capsule の高さ (m)。両端の半球を含む全体の長さです。")]
        public float height = 2.0f;
        [Tooltip("Box の大きさ (m)")]
        public Vector3 size = new Vector3(1.0f, 1.0f, 1.0f);
        [Tooltip("Root Transform からの位置オフセット")]
        public Vector3 position = Vector3.zero;
        [Tooltip("Root Transform からの回転オフセット（オイラー角）")]
        public Vector3 rotation = Vector3.zero;

        [Header("Filtering")]
        public bool allowSelf = true;
        public bool allowOthers = true;
        public bool localOnly = false;

        [Header("Write to Materials")]
        [Tooltip("TsunTsunShader のマテリアルを使っているレンダラー（最大 4 つ）")]
        public Renderer[] targetRenderers = new Renderer[0];

        // ---- マスク生成（エディタ専用。インスペクターの「マスクを生成」で使う） ----

        [Tooltip("生成したマスクの保存先フォルダ（Assets/ 以下）。空ならマテリアルと同じフォルダに保存します。")]
        public string maskOutputFolder = "";
        [Tooltip("生成するマスクの解像度")]
        public int maskResolution = 512;
        [Tooltip("範囲の端でマスクを白から黒へフェードさせる幅 (m)")]
        public float maskFadeWidth = 0.02f;
        [Tooltip("範囲の内側でのマスクの強さ（0〜1）")]
        [Range(0.0f, 1.0f)]
        public float maskMaxValue = 1.0f;
        [Tooltip("手・指のボーンにウェイトが乗っている部分をマスクから除外する")]
        public bool maskExcludeHands = true;

        private void OnDrawGizmosSelected()
        {
            // 生成される ContactReceiver の範囲をおおまかに表示する
            TsunTsunShapeGizmo.Draw(rootTransform != null ? rootTransform : transform,
                shapeType, radius, height, size, position, rotation, Color.cyan);
        }
    }
}
