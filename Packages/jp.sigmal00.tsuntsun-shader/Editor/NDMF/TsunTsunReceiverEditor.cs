using UnityEditor;
using VRC.Dynamics;

namespace Sigmal00.TsunTsunShader.Editor
{
    /// <summary>
    /// TsunTsunReceiver のインスペクター。ShapeType に関係しない形状の項目は表示しない。
    /// </summary>
    [CustomEditor(typeof(TsunTsunReceiver))]
    [CanEditMultipleObjects]
    public class TsunTsunReceiverEditor : UnityEditor.Editor
    {
        private SerializedProperty _rootTransform;
        private SerializedProperty _shapeType;
        private SerializedProperty _radius;
        private SerializedProperty _height;
        private SerializedProperty _size;
        private SerializedProperty _position;
        private SerializedProperty _rotation;
        private SerializedProperty _allowSelf;
        private SerializedProperty _allowOthers;
        private SerializedProperty _localOnly;
        private SerializedProperty _targetRenderers;

        private void OnEnable()
        {
            _rootTransform   = serializedObject.FindProperty(nameof(TsunTsunReceiver.rootTransform));
            _shapeType       = serializedObject.FindProperty(nameof(TsunTsunReceiver.shapeType));
            _radius          = serializedObject.FindProperty(nameof(TsunTsunReceiver.radius));
            _height          = serializedObject.FindProperty(nameof(TsunTsunReceiver.height));
            _size            = serializedObject.FindProperty(nameof(TsunTsunReceiver.size));
            _position        = serializedObject.FindProperty(nameof(TsunTsunReceiver.position));
            _rotation        = serializedObject.FindProperty(nameof(TsunTsunReceiver.rotation));
            _allowSelf       = serializedObject.FindProperty(nameof(TsunTsunReceiver.allowSelf));
            _allowOthers     = serializedObject.FindProperty(nameof(TsunTsunReceiver.allowOthers));
            _localOnly       = serializedObject.FindProperty(nameof(TsunTsunReceiver.localOnly));
            _targetRenderers = serializedObject.FindProperty(nameof(TsunTsunReceiver.targetRenderers));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_rootTransform);

            // Shape（[Header("Shape")] は shapeType に付いているのでここで表示される）
            EditorGUILayout.PropertyField(_shapeType);

            // 複数選択で ShapeType が混在しているときは全部表示する
            var mixed = _shapeType.hasMultipleDifferentValues;
            var shape = (ContactBase.ShapeType)_shapeType.intValue;

            if (mixed || shape == ContactBase.ShapeType.Sphere || shape == ContactBase.ShapeType.Capsule)
            {
                EditorGUILayout.PropertyField(_radius);
            }
            if (mixed || shape == ContactBase.ShapeType.Capsule)
            {
                EditorGUILayout.PropertyField(_height);
            }
            if (mixed || shape == ContactBase.ShapeType.Box)
            {
                EditorGUILayout.PropertyField(_size);
            }
            EditorGUILayout.PropertyField(_position);
            EditorGUILayout.PropertyField(_rotation);

            // Filtering
            EditorGUILayout.PropertyField(_allowSelf);
            EditorGUILayout.PropertyField(_allowOthers);
            EditorGUILayout.PropertyField(_localOnly);

            // Write to Materials
            EditorGUILayout.PropertyField(_targetRenderers);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
