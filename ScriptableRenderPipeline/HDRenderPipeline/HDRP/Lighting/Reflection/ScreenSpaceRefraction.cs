using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class ScreenSpaceRefraction : VolumeComponent
    {
        static ScreenSpaceRefraction s_Default = null;
        public static ScreenSpaceRefraction @default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<ScreenSpaceRefraction>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }

        public IntParameter                 rayMinLevel = new IntParameter(2);
        public IntParameter                 rayMaxLevel = new IntParameter(6);
        public IntParameter                 rayMaxIterations = new IntParameter(32);
        public FloatParameter               rayDepthSuccessBias = new FloatParameter(0.1f);
        public ClampedFloatParameter        screenWeightDistance = new ClampedFloatParameter(0.1f, 0, 1);

        public void PushShaderParameters(CommandBuffer cmd)
        {
            cmd.SetGlobalInt(HDShaderIDs._SSRefractionRayMinLevel, rayMinLevel.value);
            cmd.SetGlobalInt(HDShaderIDs._SSRefractionRayMaxLevel, rayMaxLevel.value);
            cmd.SetGlobalInt(HDShaderIDs._SSRefractionRayMaxIterations, rayMaxIterations.value);
            cmd.SetGlobalFloat(HDShaderIDs._SSRefractionRayDepthSuccessBias, rayDepthSuccessBias.value);
            cmd.SetGlobalFloat(HDShaderIDs._SSRefractionInvScreenWeightDistance, 1f / screenWeightDistance.value);
        }
    }
}
