using UnityEngine;
using VRC.Dynamics;
using VRC.SDKBase;

namespace Sigmal00.TsunTsunShader
{
    /// <summary>
    /// つんつんマスクを部分的に上書きするコンポーネント。
    /// TsunTsunReceiver のマスク生成時にアバター内から集められ、この範囲の内側では Receiver の設定より優先される。
    ///  - 複数の TsunTsunMask が重なったところは、マスク最大値の大きいほうが優先される
    ///  - Receiver の範囲の外側にはみ出した部分は無視される（マスクは黒になる）
    ///  - Receiver の「手を除外」はこのコンポーネントより優先される
    /// </summary>
    [AddComponentMenu("Sigmal00/TsunTsun Mask")]
    public class TsunTsunMask : MonoBehaviour, IEditorOnly
    {
        [Tooltip("範囲が追従する Transform（例: LeftLowerLeg）。空ならこの GameObject を基準にします。")]
        public Transform rootTransform;

        [Header("Shape")]
        public ContactBase.ShapeType shapeType = ContactBase.ShapeType.Sphere;
        [Tooltip("Sphere / Capsule の半径 (m)")]
        public float radius = 0.1f;
        [Tooltip("Capsule の高さ (m)。両端の半球を含む全体の長さです。")]
        public float height = 0.3f;
        [Tooltip("Box の大きさ (m)")]
        public Vector3 size = new Vector3(0.1f, 0.1f, 0.1f);
        [Tooltip("Root Transform からの位置オフセット")]
        public Vector3 position = Vector3.zero;
        [Tooltip("Root Transform からの回転オフセット（オイラー角）")]
        public Vector3 rotation = Vector3.zero;

        [Header("Mask")]
        [Tooltip("範囲の端で、外側の値からこのマスクの値へ切り替わる幅 (m)")]
        [Min(0.0f)]
        public float fadeWidth = 0.02f;
        [Tooltip("範囲の内側でのマスクの強さ（0〜1）")]
        [Range(0.0f, 1.0f)]
        public float maxValue = 0.5f;

        private void OnDrawGizmosSelected()
        {
            TsunTsunShapeGizmo.Draw(rootTransform != null ? rootTransform : transform,
                shapeType, radius, height, size, position, rotation, Color.magenta);
        }
    }
}
