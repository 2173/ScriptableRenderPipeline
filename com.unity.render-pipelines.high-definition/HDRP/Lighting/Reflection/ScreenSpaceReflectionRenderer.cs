using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ScreenSpaceReflectionRenderer
    {
        public struct Settings
        {
            /// <summary>
            /// Maximum number of ray allocation per rendering
            /// </summary>
            public int MaxRayAllocation;

            /// <summary>
            /// Mip to use for raymarching and color resolve
            /// </summary>
            public int ResolutionMip;
        }

        struct CSMeta
        {
            public static readonly int _SSReflectionResolveNextTexture      = Shader.PropertyToID("_SSReflectionResolveNextTexture");
            public static readonly int _SSReflectionResolveNextSize         = Shader.PropertyToID("_SSReflectionResolveNextSize");
            public static readonly int _SSReflectionResolveNextScale        = Shader.PropertyToID("_SSReflectionResolveNextScale");
            public static readonly int _SSReflectionRayHitNextTexture       = Shader.PropertyToID("_SSReflectionRayHitNextTexture");
            public static readonly int _SSReflectionRayHitNextSize          = Shader.PropertyToID("_SSReflectionRayHitNextSize");
            public static readonly int _SSReflectionRayHitNextScale         = Shader.PropertyToID("_SSReflectionRayHitNextScale");
            public static readonly int _PayloadIndirect                     = Shader.PropertyToID("_PayloadIndirect");
            public static readonly int _Payload                             = Shader.PropertyToID("_Payload");
            public static readonly int _Payload1                            = Shader.PropertyToID("_Payload1");
            public static readonly int _Payload2                            = Shader.PropertyToID("_Payload2");
            public const int KAllocateRay_KernelSize = 8;
            public const int KCastRay_KernelSize = 8;
            public const int KResolve_KernelSize = 8;

            public Vector3Int           KClear_NumThreads;
            public int                  KClear;
            public Vector3Int           KAllocateRays_NumThreads;
            public int                  KAllocateRays;
            Vector3Int                  m_KCastRays_NumThreads;
            int                         m_KCastRays;
            Vector3Int                  m_KCastRaysDebug_NumThreads;
            int                         m_KCastRaysDebug;
            public Vector3Int           KResolve_NumThreads;
            public int                  KResolve;

            public int GetKCastRays(
                bool debug
            )
            {
                return debug
                    ? m_KCastRaysDebug
                    : m_KCastRays;
            }

            public Vector3Int GetKCastRays_NumThreads(
                bool debug
            )
            {
                return debug
                    ? m_KCastRaysDebug_NumThreads
                    : m_KCastRays_NumThreads;
            }

            public void FindKernels(ComputeShader cs)
            {
                FindKernel(cs, "KClear", out KClear, ref KClear_NumThreads);
                FindKernel(cs, "KAllocateRays_HiZ", out KAllocateRays, ref KAllocateRays_NumThreads);
                FindKernel(cs, "KCastRays_HiZ", out m_KCastRays, ref m_KCastRays_NumThreads);
                FindKernel(cs, "KCastRays_Debug_HiZ", out m_KCastRaysDebug, ref m_KCastRaysDebug_NumThreads);
                FindKernel(cs, "KResolve", out KResolve, ref KResolve_NumThreads);
            }

            void FindKernel(ComputeShader cs, string name, out int id, ref Vector3Int threads)
            {
                uint x, y, z;
                id = cs.FindKernel(name);
                cs.GetKernelThreadGroupSizes(id, out x, out y, out z);
                threads.Set((int)x, (int)y, (int)z);
            }
        }

        ComputeShader m_CS;
        CSMeta m_Kernels;
        RTHandleSystem m_RTHSystem;
        ComputeBuffer m_DispatchIndirectBuffer;
        ComputeBuffer m_PayloadBuffer;
        ComputeBuffer m_Payload1Buffer;
        ComputeBuffer m_Payload2Buffer;

        Settings m_Settings;

        public ScreenSpaceReflectionRenderer(
            Settings settings,
            RTHandleSystem rthSystem,
            ComputeShader cs
        )
        {
            m_RTHSystem = rthSystem;
            m_CS = cs;
            m_Kernels.FindKernels(m_CS);
            m_Settings = settings;
        }

        public void AllocateBuffers(RenderPipelineSettings settings)
        {
            // TODO: Allocate HiZ buffer only if supported
            m_DispatchIndirectBuffer = new ComputeBuffer(
                3,
                Marshal.SizeOf(typeof(uint)), 
                ComputeBufferType.IndirectArguments
            );
            m_PayloadBuffer = new ComputeBuffer(
                m_Settings.MaxRayAllocation, 
                Marshal.SizeOf(typeof(uint)), 
                ComputeBufferType.Default
            );
            m_Payload1Buffer = new ComputeBuffer(
                m_Settings.MaxRayAllocation, 
                Marshal.SizeOf(typeof(float)) * 4, 
                ComputeBufferType.Default
            );
            m_Payload2Buffer = new ComputeBuffer(
                m_Settings.MaxRayAllocation, 
                Marshal.SizeOf(typeof(float)) * 4, 
                ComputeBufferType.Default
            );
        }

        public void AllocateCameraBuffersIfRequired(HDCamera hdCamera)
        {
            if (hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.SSReflectionRayHit) == null)
                hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.SSReflectionRayHit, AllocateCameraBufferRayHit);

            if (hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.SSReflectionResolve) == null)
                hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.SSReflectionResolve, AllocateCameraBufferResolve);
        }

        public void ClearBuffers(CommandBuffer cmd, HDCamera hdCamera)
        {
            RenderPassClear(hdCamera, cmd);
        }

        public void ReleaseBuffers()
        {
            m_DispatchIndirectBuffer.Release();
            m_DispatchIndirectBuffer = null;
            m_PayloadBuffer.Release();
            m_PayloadBuffer = null;
            m_Payload1Buffer.Release();
            m_Payload1Buffer = null;
            m_Payload2Buffer.Release();
            m_Payload2Buffer = null;
        }

        public void PushGlobalParams(HDCamera hdCamera, CommandBuffer cmd, FrameSettings frameSettings)
        {
            Assert.IsNotNull(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.SSReflectionRayHit));

            cmd.SetGlobalRTHandle(
                hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.SSReflectionRayHit),
                HDShaderIDs._SSReflectionRayHitTexture,
                HDShaderIDs._SSReflectionRayHitSize,
                HDShaderIDs._SSReflectionRayHitScale
            );
        }

        public void RenderSSR(
            HDCamera hdCamera, 
            CommandBuffer cmd, 
            bool debug,
            RTHandleSystem.RTHandle debugTextureHandle
        )
        {
            var ssReflection = VolumeManager.instance.stack.GetComponent<ScreenSpaceReflection>()
                    ?? ScreenSpaceReflection.@default;

            var projectionModel     = (Lit.ProjectionModel)ssReflection.deferredProjectionModel.value;

            if (projectionModel == Lit.ProjectionModel.HiZ)
                RenderPassAllocateRays(hdCamera, cmd, ssReflection);

            RenderPassCastRays(hdCamera, cmd, debug, debugTextureHandle, ssReflection);
            RenderPassResolve(hdCamera, cmd, debug, debugTextureHandle, ssReflection);
        }

        void RenderPassClear(
            HDCamera hdCamera, 
            CommandBuffer cmd
        )
        {
            var kernel              = m_Kernels.KClear;
            var threadGroups        = new Vector3Int(1, 1, 1);

            using (new ProfilingSample(cmd, "Screen Space Reflection - Clear Buffers", CustomSamplerId.SSRClear.GetSampler()))
            {
                cmd.SetComputeBufferParam(
                    m_CS,
                    kernel,
                    CSMeta._PayloadIndirect,
                    m_DispatchIndirectBuffer
                );
                cmd.DispatchCompute(
                    m_CS,
                    kernel,
                    threadGroups.x, threadGroups.y, threadGroups.z
                );
            }
        }
        
        void RenderPassAllocateRays(
            HDCamera hdCamera, 
            CommandBuffer cmd,
            ScreenSpaceReflection ssReflection
        )
        {
            var kernel              = m_Kernels.KAllocateRays;
            var threadGroups        = new Vector3Int(
                                        Mathf.CeilToInt((hdCamera.actualWidth) / (float)CSMeta.KAllocateRay_KernelSize),
                                        Mathf.CeilToInt((hdCamera.actualHeight) / (float)CSMeta.KAllocateRay_KernelSize),
                                        1
                                    );

            using (new ProfilingSample(cmd, "Screen Space Reflection - Allocate Rays", CustomSamplerId.SSRAllocateRays.GetSampler()))
            {
                cmd.SetComputeBufferParam(
                    m_CS,
                    kernel,
                    CSMeta._PayloadIndirect,
                    m_DispatchIndirectBuffer
                );
                cmd.SetComputeBufferParam(
                    m_CS,
                    kernel,
                    CSMeta._Payload,
                    m_PayloadBuffer
                );
                cmd.SetComputeBufferParam(
                    m_CS,
                    kernel,
                    CSMeta._Payload,
                    m_PayloadBuffer
                );
                cmd.SetComputeBufferParam(
                    m_CS,
                    kernel,
                    CSMeta._Payload1,
                    m_Payload1Buffer
                );
                cmd.SetComputeBufferParam(
                    m_CS,
                    kernel,
                    CSMeta._Payload2,
                    m_Payload2Buffer
                );
                cmd.DispatchCompute(
                    m_CS,
                    kernel,
                    threadGroups.x, threadGroups.y, threadGroups.z
                );
            }
        }

        void RenderPassCastRays(
            HDCamera hdCamera, 
            CommandBuffer cmd, 
            bool debug,
            RTHandleSystem.RTHandle debugTextureHandle,
            ScreenSpaceReflection ssReflection
        )
        {
            var projectionModel     = (Lit.ProjectionModel)ssReflection.deferredProjectionModel.value;

            var kernel              = m_Kernels.GetKCastRays(debug);
            var threadGroups        = new Vector3Int(
                                        // We use 8x8 kernel for KCastRays
                                        Mathf.CeilToInt((hdCamera.actualWidth) / (float)CSMeta.KCastRay_KernelSize),
                                        Mathf.CeilToInt((hdCamera.actualHeight) / (float)CSMeta.KCastRay_KernelSize),
                                        1
                                    );

            using (new ProfilingSample(cmd, "Screen Space Reflection - Cast Rays", CustomSamplerId.SSRCastRays.GetSampler()))
            {
                var currentRTHRayHit = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.SSReflectionRayHit);
                Assert.IsNotNull(currentRTHRayHit);

                if (debug)
                    cmd.SetComputeTextureParam(m_CS, kernel, HDShaderIDs._DebugTexture, debugTextureHandle);

                cmd.SetComputeBufferParam(m_CS, kernel, CSMeta._Payload, m_PayloadBuffer);
                cmd.SetComputeBufferParam(m_CS, kernel, CSMeta._Payload1, m_Payload1Buffer);
                cmd.SetComputeBufferParam(m_CS, kernel, CSMeta._Payload2, m_Payload2Buffer);
                
                cmd.SetComputeRTHandleParam(
                    m_CS, kernel, currentRTHRayHit, 
                    CSMeta._SSReflectionRayHitNextTexture,
                    CSMeta._SSReflectionRayHitNextSize,
                    CSMeta._SSReflectionRayHitNextScale
                );

                cmd.DispatchCompute(m_CS, kernel, m_DispatchIndirectBuffer, 0);
                
                cmd.SetGlobalRTHandle(
                    currentRTHRayHit,
                    HDShaderIDs._SSReflectionRayHitTexture,
                    HDShaderIDs._SSReflectionRayHitSize,
                    HDShaderIDs._SSReflectionRayHitScale
                );
            }
        }

        void RenderPassResolve(
            HDCamera hdCamera, 
            CommandBuffer cmd, 
            bool debug,
            RTHandleSystem.RTHandle debugTextureHandle,
            ScreenSpaceReflection ssReflection
        )
        {
            var kernel              = m_Kernels.KResolve;
            var threadGroups        = new Vector3Int(
                                        Mathf.CeilToInt((hdCamera.actualWidth) / (float)CSMeta.KResolve_KernelSize),
                                        Mathf.CeilToInt((hdCamera.actualHeight) / (float)CSMeta.KResolve_KernelSize),
                                        1
                                    );

            using (new ProfilingSample(cmd, "Screen Space Reflection - Resolve", CustomSamplerId.SSRResolve.GetSampler()))
            {
                var currentResolveTexture = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.SSReflectionResolve);
                Assert.IsNotNull(currentResolveTexture);

                if (debug)
                    cmd.SetComputeTextureParam(m_CS, kernel, HDShaderIDs._DebugTexture, debugTextureHandle);

                cmd.SetComputeRTHandleParam(
                    m_CS, kernel, currentResolveTexture,
                    CSMeta._SSReflectionResolveNextTexture,
                    CSMeta._SSReflectionResolveNextSize,
                    CSMeta._SSReflectionResolveNextScale
                );
                
                cmd.DispatchCompute(m_CS, kernel, threadGroups.x, threadGroups.y, threadGroups.z);
                
                cmd.SetGlobalRTHandle(
                    currentResolveTexture,
                    HDShaderIDs._SSReflectionResolveTexture,
                    HDShaderIDs._SSReflectionResolveSize,
                    HDShaderIDs._SSReflectionResolveScale
                );
            }
        }

        RTHandleSystem.RTHandle AllocateCameraBufferRayHit(string id, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(
                Vector2.one,
                filterMode: FilterMode.Point,
                colorFormat: RenderTextureFormat.ARGBInt,
                sRGB: false,
                useMipMap: false,
                autoGenerateMips: false,
                enableRandomWrite: true,
                name: string.Format("SSRRayHit-{0}-{1}", id, frameIndex)
            );
        }

        RTHandleSystem.RTHandle AllocateCameraBufferResolve(string id, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(
                new Vector2(1.0f / (1 << m_Settings.ResolutionMip), 1.0f / (1 << m_Settings.ResolutionMip)),
                colorFormat: RenderTextureFormat.ARGBHalf,
                enableRandomWrite: true,
                autoGenerateMips: false,
                useMipMap: false
            );
        }
    }
}