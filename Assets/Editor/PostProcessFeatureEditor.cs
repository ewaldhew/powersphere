// ORIGINAL TEMPLATE: https://github.com/Unity-Technologies/UniversalRenderingExamples/blob/cc4f/Assets/Scripts/Editor/DrawFullscreenFeatureDrawer.cs
using UnityEditor;

namespace UnityEngine.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(PostProcess.Settings))]
    public class DrawFullScreenFeatureDrawer : PropertyDrawer
    {
        static class Styles
        {
            public static readonly GUIContent materialLabel = EditorGUIUtility.TrTextContent("Material");
            public static readonly GUIContent materialPassLabel = EditorGUIUtility.TrTextContent("Material Pass");
            public static readonly GUIContent sourceTypeLabel = EditorGUIUtility.TrTextContent("Source Type");
            public static readonly GUIContent destinationTypeLabel = EditorGUIUtility.TrTextContent("Destination Type");
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var renderPassEventProperty = property.FindPropertyRelative("renderPassEvent");
            var sourceTypeProperty = property.FindPropertyRelative("sourceType");
            var destinationTypeProperty = property.FindPropertyRelative("destinationType");
            var sourceTextureIdProperty = property.FindPropertyRelative("sourceTextureId");
            var destinationTextureIdProperty = property.FindPropertyRelative("destinationTextureId");

            var postProcessShaderHProperty = property.FindPropertyRelative("postProcessShaderH");
            var postProcessShaderVProperty = property.FindPropertyRelative("postProcessShaderV");

            var postProcessingEnabledProperty = property.FindPropertyRelative("postProcessing");

            EditorGUI.BeginProperty(position, label, property);
            EditorGUILayout.PropertyField(renderPassEventProperty);

            EditorGUI.BeginChangeCheck();
            var selectedSourceType = (BufferType)EditorGUILayout.EnumPopup(Styles.sourceTypeLabel, (BufferType)sourceTypeProperty.enumValueIndex);
            if (EditorGUI.EndChangeCheck())
                sourceTypeProperty.enumValueIndex = (int)selectedSourceType;

            if (selectedSourceType != BufferType.CameraColor)
                EditorGUILayout.PropertyField(sourceTextureIdProperty);

            EditorGUI.BeginChangeCheck();
            var selectedDestinationType = (BufferType)EditorGUILayout.EnumPopup(Styles.destinationTypeLabel, (BufferType)destinationTypeProperty.enumValueIndex);
            if (EditorGUI.EndChangeCheck())
                destinationTypeProperty.enumValueIndex = (int)selectedDestinationType;

            if (selectedDestinationType != BufferType.CameraColor)
                EditorGUILayout.PropertyField(destinationTextureIdProperty);

            EditorGUI.BeginChangeCheck();
            ComputeShader shaderH = EditorGUILayout.ObjectField("Compute Shader H", postProcessShaderHProperty.objectReferenceValue, typeof(ComputeShader), allowSceneObjects: false) as ComputeShader;
            if (EditorGUI.EndChangeCheck())
                postProcessShaderHProperty.objectReferenceValue = shaderH;

            EditorGUI.BeginChangeCheck();
            ComputeShader shaderV = EditorGUILayout.ObjectField("Compute Shader V", postProcessShaderVProperty.objectReferenceValue, typeof(ComputeShader), allowSceneObjects: false) as ComputeShader;
            if (EditorGUI.EndChangeCheck())
                postProcessShaderVProperty.objectReferenceValue = shaderV;

            EditorGUILayout.PropertyField(postProcessingEnabledProperty);

            EditorGUI.EndProperty();
        }
    }
}
