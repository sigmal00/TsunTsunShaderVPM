using UnityEngine;
using VRC.Dynamics;

namespace Sigmal00.TsunTsunShader
{
    /// <summary>
    /// TsunTsunReceiver / TsunTsunMask の形状（Sphere / Capsule / Box）をギズモで描く。
    /// スケールの解釈はマスク生成（TsunTsunMaskGenerator.Shape）と合わせること。
    /// </summary>
    internal static class TsunTsunShapeGizmo
    {
        public static void Draw(Transform root, ContactBase.ShapeType shapeType, float radius, float height,
            Vector3 size, Vector3 position, Vector3 rotation, Color color)
        {
            var center = root.TransformPoint(position);
            var rot = root.rotation * Quaternion.Euler(rotation);
            var lossy = root.lossyScale;
            var scale = Mathf.Max(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z));

            Gizmos.color = color;
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
