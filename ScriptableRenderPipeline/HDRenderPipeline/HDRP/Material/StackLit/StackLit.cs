using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class StackLit : RenderPipelineMaterial
    {
        [GenerateHLSL(PackingRules.Exact)]
        public enum MaterialFeatureFlags
        {
            LitStandard             = 1 << 0
        };

        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, true, 1300)]
        public struct SurfaceData
        {
            [SurfaceDataAttributes("Material Features")]
            public uint materialFeatures;

            // Standard
            [SurfaceDataAttributes("Base Color", false, true)]
            public Vector3 baseColor;
            
            [SurfaceDataAttributes(new string[]{"Normal", "Normal View Space"}, true)]
            public Vector3 normalWS;

            [SurfaceDataAttributes("Smoothness A")]
            public float perceptualSmoothnessA;
            [SurfaceDataAttributes("Smoothness B")]
            public float perceptualSmoothnessB;

            [SurfaceDataAttributes("Lobe Mixing")]
            public float lobeMix;

            [SurfaceDataAttributes("Metallic")]
            public float metallic;

        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, true, 1400)]
        public struct BSDFData
        {
            public uint materialFeatures;

            [SurfaceDataAttributes("", false, true)]
            public Vector3 diffuseColor;
            public Vector3 fresnel0;

            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true)]
            public Vector3 normalWS;
            public float perceptualRoughnessA;
            public float perceptualRoughnessB;
            public float lobeMix;
            public float roughnessAT;
            public float roughnessAB;
            public float roughnessBT;
            public float roughnessBB;
            public float anisotropy;
            //public fixed float test[2];


        };
        //-----------------------------------------------------------------------------
        // Init precomputed textures
        //-----------------------------------------------------------------------------

        bool m_isInit;

        public StackLit() {}

        public override void Build(HDRenderPipelineAsset hdAsset)
        {
            PreIntegratedFGD.instance.Build();
            //LTCAreaLight.instance.Build();

            m_isInit = false;
        }

        public override void Cleanup()
        {
            PreIntegratedFGD.instance.Cleanup();
            //LTCAreaLight.instance.Cleanup();

            m_isInit = false;
        }

        public override void RenderInit(CommandBuffer cmd)
        {
            if (m_isInit)
                return;

            PreIntegratedFGD.instance.RenderInit(cmd);

            m_isInit = true;
        }

        public override void Bind()
        {
            PreIntegratedFGD.instance.Bind();
            //LTCAreaLight.instance.Bind();
        }

    }
}
