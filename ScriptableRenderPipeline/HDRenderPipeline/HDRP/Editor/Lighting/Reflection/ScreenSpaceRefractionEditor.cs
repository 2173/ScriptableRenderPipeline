using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(ScreenSpaceRefraction))]
    public class ScreenSpaceRefractionEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_RayMinLevel;
        SerializedDataParameter m_RayMaxLevel;
        SerializedDataParameter m_RayMaxIterations;
        SerializedDataParameter m_RayDepthSuccessBias;
        SerializedDataParameter m_ScreenWeightDistance;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ScreenSpaceRefraction>(serializedObject);

            m_RayMinLevel = Unpack(o.Find(x => x.rayMinLevel));
            m_RayMaxLevel = Unpack(o.Find(x => x.rayMaxLevel));
            m_RayMaxIterations = Unpack(o.Find(x => x.rayMaxIterations));
            m_RayDepthSuccessBias = Unpack(o.Find(x => x.rayDepthSuccessBias));
            m_ScreenWeightDistance = Unpack(o.Find(x => x.screenWeightDistance));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField(CoreEditorUtils.GetContent("HiZ Settings"));
            PropertyField(m_RayMinLevel, CoreEditorUtils.GetContent("Ray Min Level"));
            PropertyField(m_RayMaxLevel, CoreEditorUtils.GetContent("Ray Max Level"));
            PropertyField(m_RayMaxIterations, CoreEditorUtils.GetContent("Ray Max Iterations"));
            PropertyField(m_RayDepthSuccessBias, CoreEditorUtils.GetContent("Ray Depth Success Bias"));

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField(CoreEditorUtils.GetContent("Common Settings"));
            PropertyField(m_ScreenWeightDistance, CoreEditorUtils.GetContent("Screen Weight Distance"));
        }
    }
}