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
        public float radius = 0.1f;
        [Tooltip("Capsule の高さ (m)。両端の半球を含む全体の長さです。")]
        public float height = 0.2f;
        [Tooltip("Box の大きさ (m)")]
        public Vector3 size = new Vector3(0.1f, 0.1f, 0.1f);
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

        private void OnDrawGizmosSelected()
        {
            // 生成される ContactReceiver の範囲をおおまかに表示する
            var root = rootTransform != null ? rootTransform : transform;
            var center = root.TransformPoint(position);
            var rot = root.rotation * Quaternion.Euler(rotation);
            var lossy = root.lossyScale;
            var scale = Mathf.Max(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z));

            Gizmos.color = new Color(1.0f, 0.5f, 0.7f, 1.0f);
            switch (shapeType)
            {
                case ContactBase.ShapeType.Sphere:
                    Gizmos.DrawWireSphere(center, radius * scale);
                    break;

                case ContactBase.ShapeType.Capsule:
                {
                    var r = radius * scale;
                    var half = Mathf.Max(height * scale * 0.5f - r, 0.0f);
                    var axis = rot * Vector3.up;
                    var a = center + axis * half;
                    var b = center - axis * half;
                    Gizmos.DrawWireSphere(a, r);
                    Gizmos.DrawWireSphere(b, r);
                    var side1 = rot * Vector3.right * r;
                    var side2 = rot * Vector3.forward * r;
                    Gizmos.DrawLine(a + side1, b + side1);
                    Gizmos.DrawLine(a - side1, b - side1);
                    Gizmos.DrawLine(a + side2, b + side2);
                    Gizmos.DrawLine(a - side2, b - side2);
                    break;
                }

                case ContactBase.ShapeType.Box:
                    Gizmos.matrix = Matrix4x4.TRS(center, rot, lossy);
                    Gizmos.DrawWireCube(Vector3.zero, size);
                    Gizmos.matrix = Matrix4x4.identity;
                    break;
            }
        }
    }
}
