#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using lilToon;

namespace Sigmal00.TsunTsunShader.Editor
{
    public class TsunTsunInspector : lilToonInspector
    {
        private readonly List<MaterialProperty> propertyList = new List<MaterialProperty>();
        private readonly List<MaterialProperty> allPropertyList = new List<MaterialProperty>();
        // Custom properties
        /// <summary>
        /// <see cref="MaterialProperty"> of "_OcclusionColor".
        /// </summary>
        private MaterialProperty _occlusionColor;
        /// <summary>
        /// <see cref="MaterialProperty"> of "_EffectiveRadius".
        /// </summary>
        private MaterialProperty _effectiveRadius;
        /// <summary>
        /// <see cref="MaterialProperty"> of "_EffectiveDepth".
        /// </summary>
        private MaterialProperty _effectiveDepth;
        /// <summary>
        /// <see cref="MaterialProperty"> of "_TsunTsunMask".
        /// </summary>
        private MaterialProperty _tsunTsunMask;
        /// <summary>
        /// <see cref="MaterialProperty"> of "_FingerLengthScale".
        /// </summary>
        private MaterialProperty _fingerLengthScale;
        /// <summary>
        /// <see cref="MaterialProperty"> of "_FingerIndexLength".
        /// </summary>
        private MaterialProperty _fingerIndexLength;
        /// <summary>
        /// <see cref="MaterialProperty"> of "_FingerMiddleLength".
        /// </summary>
        private MaterialProperty _fingerMiddleLength;
        /// <summary>
        /// <see cref="MaterialProperty"> of "_FingerRingLength".
        /// </summary>
        private MaterialProperty _fingerRingLength;
        /// <summary>
        /// <see cref="MaterialProperty"> of "_FingerLittleLength".
        /// </summary>
        private MaterialProperty _fingerLittleLength;

        private static bool isShowCustomProperties;
        private static readonly Dictionary<string, MaterialProperty> copySourcePropertyDict = new Dictionary<string, MaterialProperty>();
        private const string shaderName = "Sigmal00/TsunTsunShader";

        protected override void LoadCustomProperties(MaterialProperty[] props, Material material)
        {
            isCustomShader = true;

            // If you want to change rendering modes in the editor, specify the shader here
            ReplaceToCustomShaders();
            isShowRenderMode = !material.shader.name.Contains("Optional");

            // If not, set isShowRenderMode to false
            //isShowRenderMode = false;

            // lang_custom.tsv の GUID（パッケージ移動時に振り直された値）
            LoadCustomLanguage("0b2d3049fe6b2c34ea095b9729bb6912");
            propertyList.Clear();
            propertyList.Add(_occlusionColor = FindProperty("_OcclusionColor", props));
            propertyList.Add(_effectiveRadius = FindProperty("_EffectiveRadius", props));
            propertyList.Add(_effectiveDepth = FindProperty("_EffectiveDepth", props));
            propertyList.Add(_tsunTsunMask = FindProperty("_TsunTsunMask", props));
            propertyList.Add(_fingerLengthScale = FindProperty("_FingerLengthScale", props));
            propertyList.Add(_fingerIndexLength = FindProperty("_FingerIndexLength", props));
            propertyList.Add(_fingerMiddleLength = FindProperty("_FingerMiddleLength", props));
            propertyList.Add(_fingerRingLength = FindProperty("_FingerRingLength", props));
            propertyList.Add(_fingerLittleLength = FindProperty("_FingerLittleLength", props));

            allPropertyList.Clear();
            allPropertyList.AddRange(propertyList);
        }

        protected override void DrawCustomProperties(Material material)
        {
            // GUIStyles Name   Description
            // ---------------- ------------------------------------
            // boxOuter         outer box
            // boxInnerHalf     inner box
            // boxInner         inner box without label
            // customBox        box (similar to unity default box)
            // customToggleFont label for box

            if (!ShouldDrawBlock(propertyList))
            {
                return;
            }

            isShowCustomProperties = Foldout(GetLoc("sTsunTsunSettings"), GetLoc("sTsunTsunSettings"), isShowCustomProperties);
            DrawMenuButton(allPropertyList);
            if(isShowCustomProperties)
            {
                if (ShouldDrawBlock(propertyList))
                {
                    EditorGUILayout.BeginVertical(boxOuter);
                    EditorGUILayout.LabelField(GetLoc("sTsunTsunSettings"), customToggleFont);
                    DrawMenuButton(propertyList);
                    EditorGUILayout.BeginVertical(boxInnerHalf);

                    lilEditorGUI.LocalizedProperty(m_MaterialEditor, _occlusionColor);
                    lilEditorGUI.LocalizedProperty(m_MaterialEditor, _effectiveRadius);
                    lilEditorGUI.LocalizedProperty(m_MaterialEditor, _effectiveDepth);
                    m_MaterialEditor.TexturePropertySingleLine(new GUIContent(GetLoc("sTsunTsunMask")), _tsunTsunMask);
                    lilEditorGUI.LocalizedProperty(m_MaterialEditor, _fingerLengthScale);
                    lilEditorGUI.LocalizedProperty(m_MaterialEditor, _fingerIndexLength);
                    lilEditorGUI.LocalizedProperty(m_MaterialEditor, _fingerMiddleLength);
                    lilEditorGUI.LocalizedProperty(m_MaterialEditor, _fingerRingLength);
                    lilEditorGUI.LocalizedProperty(m_MaterialEditor, _fingerLittleLength);

                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            }
        }

        protected override void ReplaceToCustomShaders()
        {
            lts         = Shader.Find(shaderName + "/lilToon");
            ltsc        = Shader.Find("Hidden/" + shaderName + "/Cutout");
            ltst        = Shader.Find("Hidden/" + shaderName + "/Transparent");
            ltsot       = Shader.Find("Hidden/" + shaderName + "/OnePassTransparent");
            ltstt       = Shader.Find("Hidden/" + shaderName + "/TwoPassTransparent");

            ltso        = Shader.Find("Hidden/" + shaderName + "/OpaqueOutline");
            ltsco       = Shader.Find("Hidden/" + shaderName + "/CutoutOutline");
            ltsto       = Shader.Find("Hidden/" + shaderName + "/TransparentOutline");
            ltsoto      = Shader.Find("Hidden/" + shaderName + "/OnePassTransparentOutline");
            ltstto      = Shader.Find("Hidden/" + shaderName + "/TwoPassTransparentOutline");

            ltsoo       = Shader.Find(shaderName + "/[Optional] OutlineOnly/Opaque");
            ltscoo      = Shader.Find(shaderName + "/[Optional] OutlineOnly/Cutout");
            ltstoo      = Shader.Find(shaderName + "/[Optional] OutlineOnly/Transparent");

            ltstess     = Shader.Find("Hidden/" + shaderName + "/Tessellation/Opaque");
            ltstessc    = Shader.Find("Hidden/" + shaderName + "/Tessellation/Cutout");
            ltstesst    = Shader.Find("Hidden/" + shaderName + "/Tessellation/Transparent");
            ltstessot   = Shader.Find("Hidden/" + shaderName + "/Tessellation/OnePassTransparent");
            ltstesstt   = Shader.Find("Hidden/" + shaderName + "/Tessellation/TwoPassTransparent");

            ltstesso    = Shader.Find("Hidden/" + shaderName + "/Tessellation/OpaqueOutline");
            ltstessco   = Shader.Find("Hidden/" + shaderName + "/Tessellation/CutoutOutline");
            ltstessto   = Shader.Find("Hidden/" + shaderName + "/Tessellation/TransparentOutline");
            ltstessoto  = Shader.Find("Hidden/" + shaderName + "/Tessellation/OnePassTransparentOutline");
            ltstesstto  = Shader.Find("Hidden/" + shaderName + "/Tessellation/TwoPassTransparentOutline");

            ltsl        = Shader.Find(shaderName + "/lilToonLite");
            ltslc       = Shader.Find("Hidden/" + shaderName + "/Lite/Cutout");
            ltslt       = Shader.Find("Hidden/" + shaderName + "/Lite/Transparent");
            ltslot      = Shader.Find("Hidden/" + shaderName + "/Lite/OnePassTransparent");
            ltsltt      = Shader.Find("Hidden/" + shaderName + "/Lite/TwoPassTransparent");

            ltslo       = Shader.Find("Hidden/" + shaderName + "/Lite/OpaqueOutline");
            ltslco      = Shader.Find("Hidden/" + shaderName + "/Lite/CutoutOutline");
            ltslto      = Shader.Find("Hidden/" + shaderName + "/Lite/TransparentOutline");
            ltsloto     = Shader.Find("Hidden/" + shaderName + "/Lite/OnePassTransparentOutline");
            ltsltto     = Shader.Find("Hidden/" + shaderName + "/Lite/TwoPassTransparentOutline");

            ltsref      = Shader.Find("Hidden/" + shaderName + "/Refraction");
            ltsrefb     = Shader.Find("Hidden/" + shaderName + "/RefractionBlur");
            ltsfur      = Shader.Find("Hidden/" + shaderName + "/Fur");
            ltsfurc     = Shader.Find("Hidden/" + shaderName + "/FurCutout");
            ltsfurtwo   = Shader.Find("Hidden/" + shaderName + "/FurTwoPass");
            ltsfuro     = Shader.Find(shaderName + "/[Optional] FurOnly/Transparent");
            ltsfuroc    = Shader.Find(shaderName + "/[Optional] FurOnly/Cutout");
            ltsfurotwo  = Shader.Find(shaderName + "/[Optional] FurOnly/TwoPass");
            ltsgem      = Shader.Find("Hidden/" + shaderName + "/Gem");
            ltsfs       = Shader.Find(shaderName + "/[Optional] FakeShadow");

            ltsover     = Shader.Find(shaderName + "/[Optional] Overlay");
            ltsoover    = Shader.Find(shaderName + "/[Optional] OverlayOnePass");
            ltslover    = Shader.Find(shaderName + "/[Optional] LiteOverlay");
            ltsloover   = Shader.Find(shaderName + "/[Optional] LiteOverlayOnePass");

            ltsm        = Shader.Find(shaderName + "/lilToonMulti");
            ltsmo       = Shader.Find("Hidden/" + shaderName + "/MultiOutline");
            ltsmref     = Shader.Find("Hidden/" + shaderName + "/MultiRefraction");
            ltsmfur     = Shader.Find("Hidden/" + shaderName + "/MultiFur");
            ltsmgem     = Shader.Find("Hidden/" + shaderName + "/MultiGem");
        }

        // You can create a menu like this
        [MenuItem("Assets/TsunTsunShader/Convert material to custom shader", false, 1100)]
        private static void ConvertMaterialToCustomShaderMenu()
        {
            if(Selection.objects.Length == 0) return;
            TsunTsunInspector inspector = new TsunTsunInspector();
            for(int i = 0; i < Selection.objects.Length; i++)
            {
                if(Selection.objects[i] is Material)
                {
                    inspector.ConvertMaterialToCustomShader((Material)Selection.objects[i]);
                }
            }
        }

        private static bool ShouldDrawBlock(List<MaterialProperty> propertyList)
        {
            foreach (var property in propertyList)
            {
                if (lilEditorGUI.CheckPropertyToDraw(property))
                {
                    return true;
                }
            }
            return false;
        }

        private static void DrawMenuButton(List<MaterialProperty> propertyList, string helpUrl = null)
        {
            var position = GUILayoutUtility.GetLastRect();
            position.x += position.width - 24.0f;
            position.width = 24.0f;

            if (!GUI.Button(position, EditorGUIUtility.IconContent("_Popup"), middleButton))
            {
                return;
            }

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent(GetLoc("sCopy")), false, userData => CopyProperties((List<MaterialProperty>)userData), propertyList);
            menu.AddItem(new GUIContent(GetLoc("sPaste")), false, userData => PasteProperties((List<MaterialProperty>)userData, false), propertyList);
            menu.AddItem(new GUIContent(GetLoc("sPasteWithTexture")), false, userData => PasteProperties((List<MaterialProperty>)userData, true), propertyList);
#if UNITY_2019_3_OR_NEWER
            menu.AddItem(new GUIContent(GetLoc("sReset")), false, userData => ResetProperties((List<MaterialProperty>)userData), propertyList);
#endif  // UNITY_2019_3_OR_NEWER
            if (!string.IsNullOrEmpty(helpUrl))
            {
                menu.AddItem(new GUIContent(GetLoc("sOpenManual")), false, userData => OpenHelpPage((string)userData), helpUrl);
            }
            menu.ShowAsContext();
        }

        private static void CopyProperties(List<MaterialProperty> propertyList)
        {
            copySourcePropertyDict.Clear();
            foreach (var prop in propertyList)
            {
                copySourcePropertyDict[prop.name] = prop;
            }
        }

        private static void PasteProperties(List<MaterialProperty> propertyList, bool shouldCopyTex)
        {
            foreach (var prop in propertyList)
            {
                if (!copySourcePropertyDict.TryGetValue(prop.name, out var sourceProp))
                {
                    continue;
                }

                switch (prop.type)
                {
                    case MaterialProperty.PropType.Color:
                        prop.colorValue = sourceProp.colorValue;
                        break;
                    case MaterialProperty.PropType.Vector:
                        prop.vectorValue = sourceProp.vectorValue;
                        break;
                    case MaterialProperty.PropType.Float:
                        prop.floatValue = sourceProp.floatValue;
                        break;
                    case MaterialProperty.PropType.Range:
                        prop.floatValue = sourceProp.floatValue;
                        break;
                    case MaterialProperty.PropType.Texture:
                        if (shouldCopyTex)
                        {
                            prop.textureValue = sourceProp.textureValue;
                        }
                        break;
                    case MaterialProperty.PropType.Int:
                        prop.intValue = sourceProp.intValue;
                        break;
                }
            }
        }
#if UNITY_2019_3_OR_NEWER
        private static void ResetProperties(List<MaterialProperty> propertyList)
        {
            foreach (var prop in propertyList)
            {
                var material = prop.targets[0] as Material;
                if (material == null)
                {
                    continue;
                }

                var shader = material.shader;
                if (shader == null)
                {
                    continue;
                }

                var propID = shader.FindPropertyIndex(prop.name);
                if (propID == -1)
                {
                    continue;
                }

                switch (prop.type)
                {
                    case MaterialProperty.PropType.Color:
                        prop.colorValue = shader.GetPropertyDefaultVectorValue(propID);
                        break;
                    case MaterialProperty.PropType.Vector:
                        prop.vectorValue = shader.GetPropertyDefaultVectorValue(propID);
                        break;
                    case MaterialProperty.PropType.Float:
                        prop.floatValue = shader.GetPropertyDefaultFloatValue(propID);
                        break;
                    case MaterialProperty.PropType.Range:
                        prop.floatValue = shader.GetPropertyDefaultFloatValue(propID);
                        break;
                    case MaterialProperty.PropType.Texture:
                        prop.textureValue = null;
                        break;
                    case MaterialProperty.PropType.Int:
                        prop.intValue = shader.GetPropertyDefaultIntValue(propID);
                        break;
                }
            }
        }
#endif  // UNITY_2019_3_OR_NEWER
        private static void OpenHelpPage(string url)
        {
            Application.OpenURL(url);
        }
    }
}
#endif
