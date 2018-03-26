using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Experimental.Rendering.HDPipeline.Attributes;
using System.Linq;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public enum FullScreenDebugMode
    {
        None,

        // Lighting
        MinLightingFullScreenDebug,
        SSAO,
        DeferredShadows,
        PreRefractionColorPyramid,
        DepthPyramid,
        FinalColorPyramid,
        ScreenSpaceTracingRefraction,
        MaxLightingFullScreenDebug,

        // Rendering
        MinRenderingFullScreenDebug,
        MotionVectors,
        NanTracker,
        MaxRenderingFullScreenDebug
    }

    [GenerateHLSL]
    public struct ScreenSpaceTracingDebug
    {
        public uint startPositionSSX;
        public uint startPositionSSY;
        public uint cellSizeW;
        public uint cellSizeH;

        public Vector3 positionSS;
        public float startLinearDepth;

        public uint level;
        public uint levelMax;
        public uint iteration;
        public uint iterationMax;

        public bool hitSuccess;
        public float hitLinearDepth;
        public Vector2 hitPositionSS;

        public float hiZLinearDepth;
        public Vector3 raySS;

        public uint intersectionKind;
        public float resultHitDepth;
        public uint endPositionSSX;
        public uint endPositionSSY;

        public Vector2 startPositionSS { get { return new Vector2(startPositionSSX, startPositionSSY); } }
        public Vector2 endPositionSS { get { return new Vector2(endPositionSSX, endPositionSSY); } }
        public Vector2 cellId { get { return new Vector2(((int)positionSS.x) >> (int)level, ((int)positionSS.y) >> (int)level); } }
    }

    public class DebugDisplaySettings
    {
        public static string k_PanelDisplayStats = "Display Stats";
        public static string k_PanelMaterials = "Material";
        public static string k_PanelLighting = "Lighting";
        public static string k_PanelRendering = "Rendering";
        public static string k_PanelStatistics = "Statistics";

        static readonly string[] k_HiZIntersectionKind = { "None", "Depth", "Cell" };

        DebugUI.Widget[] m_DebugDisplayStatsItems;
        DebugUI.Widget[] m_DebugMaterialItems;
        DebugUI.Widget[] m_DebugLightingItems;
        DebugUI.Widget[] m_DebugRenderingItems;
        DebugUI.Widget[] m_DebugStatisticsItems;

        public float debugOverlayRatio = 0.33f;
        public FullScreenDebugMode  fullScreenDebugMode = FullScreenDebugMode.None;
        public float fullscreenDebugMip = 0.0f;
        public bool showSSRayGrid = true;
        public bool showSSRayDepthPyramid = true;

        public MaterialDebugSettings materialDebugSettings = new MaterialDebugSettings();
        public LightingDebugSettings lightingDebugSettings = new LightingDebugSettings();
        public MipMapDebugSettings mipMapDebugSettings = new MipMapDebugSettings();
        public ColorPickerDebugSettings colorPickerDebugSettings = new ColorPickerDebugSettings();

        public static GUIContent[] lightingFullScreenDebugStrings = null;
        public static int[] lightingFullScreenDebugValues = null;
        public static GUIContent[] renderingFullScreenDebugStrings = null;
        public static int[] renderingFullScreenDebugValues = null;
        public static GUIContent[] debugScreenSpaceTracingStrings = null;
        public static int[] debugScreenSpaceTracingValues = null;

        public ScreenSpaceTracingDebug screenSpaceTracingDebugData { get; internal set; }

        public DebugDisplaySettings()
        {
            FillFullScreenDebugEnum(ref lightingFullScreenDebugStrings, ref lightingFullScreenDebugValues, FullScreenDebugMode.MinLightingFullScreenDebug, FullScreenDebugMode.MaxLightingFullScreenDebug);
            FillFullScreenDebugEnum(ref renderingFullScreenDebugStrings, ref renderingFullScreenDebugValues, FullScreenDebugMode.MinRenderingFullScreenDebug, FullScreenDebugMode.MaxRenderingFullScreenDebug);
            debugScreenSpaceTracingStrings = Enum.GetNames(typeof(DebugScreenSpaceTracing)).Select(s => new GUIContent(s)).ToArray();
            debugScreenSpaceTracingValues = (int[])Enum.GetValues(typeof(DebugScreenSpaceTracing));
        }

        public int GetDebugMaterialIndex()
        {
            return materialDebugSettings.GetDebugMaterialIndex();
        }

        public DebugLightingMode GetDebugLightingMode()
        {
            return lightingDebugSettings.debugLightingMode;
        }

        public int GetDebugLightingSubMode()
        {
            switch (GetDebugLightingMode())
            {
                default:
                    return 0;
                case DebugLightingMode.ScreenSpaceTracingRefraction:
                    return (int)lightingDebugSettings.debugScreenSpaceTracingMode;
            }
        }

        public DebugMipMapMode GetDebugMipMapMode()
        {
            return mipMapDebugSettings.debugMipMapMode;
        }

        public bool IsDebugDisplayEnabled()
        {
            return materialDebugSettings.IsDebugDisplayEnabled() || lightingDebugSettings.IsDebugDisplayEnabled() || mipMapDebugSettings.IsDebugDisplayEnabled();
        }

        public bool IsDebugMaterialDisplayEnabled()
        {
            return materialDebugSettings.IsDebugDisplayEnabled();
        }
        public bool IsDebugMipMapDisplayEnabled()
        {
            return mipMapDebugSettings.IsDebugDisplayEnabled();
        }

        private void DisableNonMaterialDebugSettings()
        {
            lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
            mipMapDebugSettings.debugMipMapMode = DebugMipMapMode.None;
        }

        public void SetDebugViewMaterial(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewMaterial(value);
        }

        public void SetDebugViewEngine(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewEngine(value);
        }

        public void SetDebugViewVarying(DebugViewVarying value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewVarying(value);
        }

        public void SetDebugViewProperties(DebugViewProperties value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewProperties(value);
        }

        public void SetDebugViewGBuffer(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewGBuffer(value);
        }

        public void SetDebugLightingMode(DebugLightingMode value)
        {
            if (value != 0)
            {
                materialDebugSettings.DisableMaterialDebug();
                mipMapDebugSettings.debugMipMapMode = DebugMipMapMode.None;
            }
            lightingDebugSettings.debugLightingMode = value;
        }

        public void SetMipMapMode(DebugMipMapMode value)
        {
            if (value != 0)
            {
                materialDebugSettings.DisableMaterialDebug();
                lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
            }
            mipMapDebugSettings.debugMipMapMode = value;
        }

        public void UpdateMaterials()
        {
            //if (mipMapDebugSettings.debugMipMapMode != 0)
            //    Texture.SetStreamingTextureMaterialDebugProperties();
        }

        void RegisterDisplayStatsDebug()
        {
            m_DebugDisplayStatsItems = new DebugUI.Widget[]
            {
                new DebugUI.Value { displayName = "Frame Rate", getter = () => 1f / Time.smoothDeltaTime, refreshRate = 1f / 30f },
                new DebugUI.Value { displayName = "Frame Rate (ms)", getter = () => Time.smoothDeltaTime * 1000f, refreshRate = 1f / 30f }
            };

            var panel = DebugManager.instance.GetPanel(k_PanelDisplayStats, true);
            panel.flags = DebugUI.Flags.RuntimeOnly;
            panel.children.Add(m_DebugDisplayStatsItems);
        }

        bool IsScreenSpaceTracingIterationDebugEnabled()
        {
            return fullScreenDebugMode == FullScreenDebugMode.ScreenSpaceTracingRefraction;
        }

        void SetScreenSpaceTracingIterationDebugEnabled(bool value)
        {
            if (value)
            {
                lightingDebugSettings.debugLightingMode = DebugLightingMode.ScreenSpaceTracingRefraction;
                fullScreenDebugMode = FullScreenDebugMode.ScreenSpaceTracingRefraction;
            }
            else
            {
                lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
                fullScreenDebugMode = FullScreenDebugMode.None;
            }
        }

        void SetScreenSpaceTracingDebugMode(int value)
        {
            var val = (DebugScreenSpaceTracing)value;
            if (val != DebugScreenSpaceTracing.None)
            {
                lightingDebugSettings.debugLightingMode = DebugLightingMode.ScreenSpaceTracingRefraction;
                lightingDebugSettings.debugScreenSpaceTracingMode = (DebugScreenSpaceTracing)value;
            }
            else
            {
                lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
                lightingDebugSettings.debugScreenSpaceTracingMode = DebugScreenSpaceTracing.None;
            }
        }

        void RegisterStatisticsDebug()
        {
            var list = new List<DebugUI.Container>();
            list.Add(new DebugUI.Container
            {
                displayName = "Screen Space Tracing Debug",
                children = 
                {
                    new DebugUI.Container
                    {
                        displayName = "Configuration",
                        children =
                        {
                            new DebugUI.BoolField { displayName = "Debug Iterations", getter = IsScreenSpaceTracingIterationDebugEnabled, setter = SetScreenSpaceTracingIterationDebugEnabled, onValueChanged = RefreshStatisticsDebug },
                            new DebugUI.EnumField { displayName = "Debug Mode", getter = GetDebugLightingSubMode, setter = SetScreenSpaceTracingDebugMode, enumNames = debugScreenSpaceTracingStrings, enumValues = debugScreenSpaceTracingValues, onValueChanged = RefreshStatisticsDebug },
                            new DebugUI.BoolField { displayName = "Display Grid", getter = () => showSSRayGrid, setter = v => showSSRayGrid = v },
                            new DebugUI.BoolField { displayName = "Display Depth", getter = () => showSSRayDepthPyramid, setter = v => showSSRayDepthPyramid = v },
                        }
                    }
                }
            });

            if (IsScreenSpaceTracingIterationDebugEnabled() || GetDebugLightingSubMode() != 0)
            {
                list[list.Count - 1].children.Add(new DebugUI.Container
                    {
                        displayName = "Loop Information",
                        children = 
                        {
                            new DebugUI.Value { displayName = "Start Position", getter = () => string.Format("({0:D0}, {1:D0}) px", screenSpaceTracingDebugData.startPositionSSX, screenSpaceTracingDebugData.startPositionSSY) },
                            new DebugUI.Value { displayName = "Start Depth", getter = () => string.Format("{0:F7} m", screenSpaceTracingDebugData.startLinearDepth) },
                            new DebugUI.Value { displayName = "Ray SS", getter = () => string.Format("({0:F7}, {1:F7}) px", screenSpaceTracingDebugData.raySS.x, screenSpaceTracingDebugData.raySS.y) },
                            new DebugUI.Value { displayName = "Ray SS Depth", getter = () => string.Format("({0:F7}) m", 1f / screenSpaceTracingDebugData.raySS.z) },
                            new DebugUI.Value { displayName = "Intersection Kind", getter = () => k_HiZIntersectionKind[screenSpaceTracingDebugData.intersectionKind] },
                        }
                    });
                list[list.Count - 1].children.Add(
                    new DebugUI.Container
                    {
                        displayName = "Iteration Information",
                        children = 
                        {
                            new DebugUI.Value { displayName = "Cell id", getter = () => string.Format("({0:F0}, {1:F0}) px", screenSpaceTracingDebugData.cellId.x, screenSpaceTracingDebugData.cellId.y) },
                            new DebugUI.Value { displayName = "Cell Size", getter = () => string.Format("({0:D0}, {1:D0}) px", screenSpaceTracingDebugData.cellSizeW, screenSpaceTracingDebugData.cellSizeH) },
                            new DebugUI.Value { displayName = "Level / Max", getter = () => string.Format("{0}/{1}", screenSpaceTracingDebugData.level, screenSpaceTracingDebugData.levelMax) },
                            new DebugUI.Value { displayName = "Iteration / Max", getter = () => string.Format("{0}/{1}", screenSpaceTracingDebugData.iteration + 1, screenSpaceTracingDebugData.iterationMax) },
                            new DebugUI.Value { displayName = "Position", getter = () => string.Format("({0:F0}, {1:F0}) px", screenSpaceTracingDebugData.positionSS.x, screenSpaceTracingDebugData.positionSS.y) },
                            new DebugUI.Value { displayName = "Position Depth", getter = () => string.Format("{0:F7} m", screenSpaceTracingDebugData.hitLinearDepth) },
                            new DebugUI.Value { displayName = "Depth Buffer", getter = () => string.Format("{0:F7} m", screenSpaceTracingDebugData.hiZLinearDepth) },
                            new DebugUI.Value { displayName = "Raymarched Distance", getter = () => string.Format("{0:F0} px", ((Vector2)screenSpaceTracingDebugData.positionSS - screenSpaceTracingDebugData.startPositionSS).magnitude) },
                        }
                    });
                list[list.Count - 1].children.Add( new DebugUI.Container
                    {
                        displayName = "Result Information",
                        children = 
                        {
                            new DebugUI.Value { displayName = "Success", getter = () => screenSpaceTracingDebugData.hitSuccess },
                            new DebugUI.Value { displayName = "Position", getter = () => string.Format("({0:D0}, {1:D0}) px", screenSpaceTracingDebugData.endPositionSSX, screenSpaceTracingDebugData.endPositionSSY) },
                            new DebugUI.Value { displayName = "Position Depth", getter = () => string.Format("{0:F7} m", screenSpaceTracingDebugData.resultHitDepth) },
                            new DebugUI.Value { displayName = "Raymarched Distance", getter = () => string.Format("{0:F0} px", (screenSpaceTracingDebugData.startPositionSS - screenSpaceTracingDebugData.endPositionSS).magnitude) },
                        }
                    });
            }

            m_DebugStatisticsItems = list.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelStatistics, true);
            panel.flags |= DebugUI.Flags.ForceUpdate;
            panel.children.Add(m_DebugStatisticsItems);
        }

        public void RegisterMaterialDebug()
        {
            m_DebugMaterialItems = new DebugUI.Widget[]
            {
                new DebugUI.EnumField { displayName = "Material", getter = () => materialDebugSettings.debugViewMaterial, setter = value => SetDebugViewMaterial(value), enumNames = MaterialDebugSettings.debugViewMaterialStrings, enumValues = MaterialDebugSettings.debugViewMaterialValues },
                new DebugUI.EnumField { displayName = "Engine", getter = () => materialDebugSettings.debugViewEngine, setter = value => SetDebugViewEngine(value), enumNames = MaterialDebugSettings.debugViewEngineStrings, enumValues = MaterialDebugSettings.debugViewEngineValues },
                new DebugUI.EnumField { displayName = "Attributes", getter = () => (int)materialDebugSettings.debugViewVarying, setter = value => SetDebugViewVarying((DebugViewVarying)value), autoEnum = typeof(DebugViewVarying) },
                new DebugUI.EnumField { displayName = "Properties", getter = () => (int)materialDebugSettings.debugViewProperties, setter = value => SetDebugViewProperties((DebugViewProperties)value), autoEnum = typeof(DebugViewProperties) },
                new DebugUI.EnumField { displayName = "GBuffer", getter = () => materialDebugSettings.debugViewGBuffer, setter = value => SetDebugViewGBuffer(value), enumNames = MaterialDebugSettings.debugViewMaterialGBufferStrings, enumValues = MaterialDebugSettings.debugViewMaterialGBufferValues }
            };

            var panel = DebugManager.instance.GetPanel(k_PanelMaterials, true);
            panel.children.Add(m_DebugMaterialItems);
        }

        // For now we just rebuild the lighting panel if needed, but ultimately it could be done in a better way
        void RefreshLightingDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebugItems(k_PanelLighting, m_DebugLightingItems);
            RegisterLightingDebug();
        }

        void RefreshStatisticsDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebugItems(k_PanelStatistics, m_DebugStatisticsItems);
            RegisterStatisticsDebug();
        }

        public void RegisterLightingDebug()
        {
            var list = new List<DebugUI.Widget>();

            list.Add(new DebugUI.EnumField
            {
                displayName = "Shadow Debug Mode",
                getter = () => (int)lightingDebugSettings.shadowDebugMode,
                setter = value => lightingDebugSettings.shadowDebugMode = (ShadowMapDebugMode)value,
                autoEnum = typeof(ShadowMapDebugMode),
                onValueChanged = RefreshLightingDebug
            });

            if (lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.VisualizeShadowMap)
            {
                var container = new DebugUI.Container();
                container.children.Add(new DebugUI.BoolField { displayName = "Use Selection", getter = () => lightingDebugSettings.shadowDebugUseSelection, setter = value => lightingDebugSettings.shadowDebugUseSelection = value, flags = DebugUI.Flags.EditorOnly, onValueChanged = RefreshLightingDebug });

                if (!lightingDebugSettings.shadowDebugUseSelection)
                    container.children.Add(new DebugUI.UIntField { displayName = "Shadow Map Index", getter = () => lightingDebugSettings.shadowMapIndex, setter = value => lightingDebugSettings.shadowMapIndex = value, min = () => 0u, max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetCurrentShadowCount() - 1u });

                list.Add(container);
            }
            else if (lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.VisualizeAtlas)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.UIntField { displayName = "Shadow Atlas Index", getter = () => lightingDebugSettings.shadowAtlasIndex, setter = value => lightingDebugSettings.shadowAtlasIndex = value, min = () => 0u, max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetShadowAtlasCount() - 1u }
                    }
                });
            }

            list.Add(new DebugUI.FloatField { displayName = "Shadow Range Min Value", getter = () => lightingDebugSettings.shadowMinValue, setter = value => lightingDebugSettings.shadowMinValue = value });
            list.Add(new DebugUI.FloatField { displayName = "Shadow Range Max Value", getter = () => lightingDebugSettings.shadowMaxValue, setter = value => lightingDebugSettings.shadowMaxValue = value });

            list.Add(new DebugUI.EnumField { displayName = "Lighting Debug Mode", getter = () => (int)lightingDebugSettings.debugLightingMode, setter = value => SetDebugLightingMode((DebugLightingMode)value), autoEnum = typeof(DebugLightingMode), onValueChanged = RefreshLightingDebug });

            switch (lightingDebugSettings.debugLightingMode)
            {
                case DebugLightingMode.EnvironmentProxyVolume:
                {
                    list.Add(new DebugUI.Container
                    {
                        children =
                        {
                            new DebugUI.FloatField { displayName = "Debug Environment Proxy Depth Scale", getter = () => lightingDebugSettings.environmentProxyDepthScale, setter = value => lightingDebugSettings.environmentProxyDepthScale = value, min = () => 0.1f, max = () => 50f }
                        }
                    });
                    break;
                }
                case DebugLightingMode.ScreenSpaceTracingRefraction:
                {
                    list.Add(new DebugUI.Container
                    {
                        children =
                        {
                            new DebugUI.EnumField
                            {
                                displayName = "Screen Space Tracing Debug Mode",
                                getter = GetDebugLightingSubMode,
                                setter = value => lightingDebugSettings.debugScreenSpaceTracingMode = (DebugScreenSpaceTracing)value,
                                enumNames = debugScreenSpaceTracingStrings,
                                enumValues = debugScreenSpaceTracingValues,
                                onValueChanged = RefreshLightingDebug
                            }
                        }
                    });
                    break;
                }
            }

            list.Add(new DebugUI.EnumField { displayName = "Fullscreen Debug Mode", getter = () => (int)fullScreenDebugMode, setter = value => fullScreenDebugMode = (FullScreenDebugMode)value, enumNames = lightingFullScreenDebugStrings, enumValues = lightingFullScreenDebugValues, onValueChanged = RefreshLightingDebug });
            switch (fullScreenDebugMode)
            {
                case FullScreenDebugMode.PreRefractionColorPyramid:
                case FullScreenDebugMode.FinalColorPyramid:
                case FullScreenDebugMode.DepthPyramid:
                {
                    list.Add(new DebugUI.Container
                    {
                        children =
                        {
                            new DebugUI.UIntField
                            {
                                displayName = "Fullscreen Debug Mip",
                                getter = () =>
                                {
                                    int id;
                                    switch (fullScreenDebugMode)
                                    {
                                        case FullScreenDebugMode.FinalColorPyramid:
                                        case FullScreenDebugMode.PreRefractionColorPyramid:
                                            id = HDShaderIDs._ColorPyramidScale;
                                            break;
                                        default:
                                            id = HDShaderIDs._DepthPyramidScale;
                                            break;
                                    }
                                    var size = Shader.GetGlobalVector(id);
                                    float lodCount = size.z;
                                    return (uint)(fullscreenDebugMip * lodCount);
                                },
                                setter = value =>
                                {
                                    int id;
                                    switch (fullScreenDebugMode)
                                    {
                                        case FullScreenDebugMode.FinalColorPyramid:
                                        case FullScreenDebugMode.PreRefractionColorPyramid:
                                            id = HDShaderIDs._ColorPyramidScale;
                                            break;
                                        default:
                                            id = HDShaderIDs._DepthPyramidScale;
                                            break;
                                    }
                                    var size = Shader.GetGlobalVector(id);
                                    float lodCount = size.z;
                                    fullscreenDebugMip = (float)Convert.ChangeType(value, typeof(float)) / lodCount;
                                },
                                min = () => 0u,
                                max = () =>
                                {
                                    int id;
                                    switch (fullScreenDebugMode)
                                    {
                                        case FullScreenDebugMode.FinalColorPyramid:
                                        case FullScreenDebugMode.PreRefractionColorPyramid:
                                            id = HDShaderIDs._ColorPyramidScale;
                                            break;
                                        default:
                                            id = HDShaderIDs._DepthPyramidScale;
                                            break;
                                    }
                                    var size = Shader.GetGlobalVector(id);
                                    float lodCount = size.z;
                                    return (uint)lodCount;
                                }
                            }
                        }
                    });
                    break;
                }
                case FullScreenDebugMode.ScreenSpaceTracingRefraction:
                {
                    
                    break;
                }
                default:
                    fullscreenDebugMip = 0;
                    break;
            }

            list.Add(new DebugUI.BoolField { displayName = "Override Smoothness", getter = () => lightingDebugSettings.overrideSmoothness, setter = value => lightingDebugSettings.overrideSmoothness = value, onValueChanged = RefreshLightingDebug });
            if (lightingDebugSettings.overrideSmoothness)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.FloatField { displayName = "Smoothness", getter = () => lightingDebugSettings.overrideSmoothnessValue, setter = value => lightingDebugSettings.overrideSmoothnessValue = value, min = () => 0f, max = () => 1f, incStep = 0.025f }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Override Albedo", getter = () => lightingDebugSettings.overrideAlbedo, setter = value => lightingDebugSettings.overrideAlbedo = value, onValueChanged = RefreshLightingDebug });
            if (lightingDebugSettings.overrideAlbedo)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.ColorField { displayName = "Albedo", getter = () => lightingDebugSettings.overrideAlbedoValue, setter = value => lightingDebugSettings.overrideAlbedoValue = value, showAlpha = false, hdr = false }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Override Normal", getter = () => lightingDebugSettings.overrideNormal, setter = value => lightingDebugSettings.overrideNormal = value });

            list.Add(new DebugUI.EnumField { displayName = "Tile/Cluster Debug", getter = () => (int)lightingDebugSettings.tileClusterDebug, setter = value => lightingDebugSettings.tileClusterDebug = (LightLoop.TileClusterDebug)value, autoEnum = typeof(LightLoop.TileClusterDebug), onValueChanged = RefreshLightingDebug });
            if (lightingDebugSettings.tileClusterDebug != LightLoop.TileClusterDebug.None && lightingDebugSettings.tileClusterDebug != LightLoop.TileClusterDebug.MaterialFeatureVariants)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.EnumField { displayName = "Tile/Cluster Debug By Category", getter = () => (int)lightingDebugSettings.tileClusterDebugByCategory, setter = value => lightingDebugSettings.tileClusterDebugByCategory = (LightLoop.TileClusterCategoryDebug)value, autoEnum = typeof(LightLoop.TileClusterCategoryDebug) }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Display Sky Reflection", getter = () => lightingDebugSettings.displaySkyReflection, setter = value => lightingDebugSettings.displaySkyReflection = value, onValueChanged = RefreshLightingDebug });
            if (lightingDebugSettings.displaySkyReflection)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.FloatField { displayName = "Sky Reflection Mipmap", getter = () => lightingDebugSettings.skyReflectionMipmap, setter = value => lightingDebugSettings.skyReflectionMipmap = value, min = () => 0f, max = () => 1f, incStep = 0.05f }
                    }
                });
            }

            m_DebugLightingItems = list.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelLighting, true);
            panel.children.Add(m_DebugLightingItems);
        }

        public void RegisterRenderingDebug()
        {
            m_DebugRenderingItems = new DebugUI.Widget[]
            {
                new DebugUI.EnumField { displayName = "Fullscreen Debug Mode", getter = () => (int)fullScreenDebugMode, setter = value => fullScreenDebugMode = (FullScreenDebugMode)value, enumNames = renderingFullScreenDebugStrings, enumValues = renderingFullScreenDebugValues },
                new DebugUI.EnumField { displayName = "MipMaps", getter = () => (int)mipMapDebugSettings.debugMipMapMode, setter = value => SetMipMapMode((DebugMipMapMode)value), autoEnum = typeof(DebugMipMapMode) },

                new DebugUI.Container
                {
                    displayName = "Color Picker",
                    flags = DebugUI.Flags.EditorOnly,
                    children =
                    {
                        new DebugUI.EnumField  { displayName = "Debug Mode", getter = () => (int)colorPickerDebugSettings.colorPickerMode, setter = value => colorPickerDebugSettings.colorPickerMode = (ColorPickerDebugMode)value, autoEnum = typeof(ColorPickerDebugMode) },
                        new DebugUI.FloatField { displayName = "Range Threshold 0", getter = () => colorPickerDebugSettings.colorThreshold0, setter = value => colorPickerDebugSettings.colorThreshold0 = value },
                        new DebugUI.FloatField { displayName = "Range Threshold 1", getter = () => colorPickerDebugSettings.colorThreshold1, setter = value => colorPickerDebugSettings.colorThreshold1 = value },
                        new DebugUI.FloatField { displayName = "Range Threshold 2", getter = () => colorPickerDebugSettings.colorThreshold2, setter = value => colorPickerDebugSettings.colorThreshold2 = value },
                        new DebugUI.FloatField { displayName = "Range Threshold 3", getter = () => colorPickerDebugSettings.colorThreshold3, setter = value => colorPickerDebugSettings.colorThreshold3 = value },
                        new DebugUI.ColorField { displayName = "Font Color", flags = DebugUI.Flags.EditorOnly, getter = () => colorPickerDebugSettings.fontColor, setter = value => colorPickerDebugSettings.fontColor = value }
                    }
                }
            };

            var panel = DebugManager.instance.GetPanel(k_PanelRendering, true);
            panel.children.Add(m_DebugRenderingItems);
        }

        public void RegisterDebug()
        {
            RegisterDisplayStatsDebug();
            RegisterMaterialDebug();
            RegisterLightingDebug();
            RegisterRenderingDebug();
            RegisterStatisticsDebug();
        }

        public void UnregisterDebug()
        {
            UnregisterDebugItems(k_PanelDisplayStats, m_DebugDisplayStatsItems);
            UnregisterDebugItems(k_PanelMaterials, m_DebugMaterialItems);
            UnregisterDebugItems(k_PanelLighting, m_DebugLightingItems);
            UnregisterDebugItems(k_PanelRendering, m_DebugLightingItems);
            UnregisterDebugItems(k_PanelStatistics, m_DebugStatisticsItems);
        }

        void UnregisterDebugItems(string panelName, DebugUI.Widget[] items)
        {
            var panel = DebugManager.instance.GetPanel(panelName);
            if (panel != null)
                panel.children.Remove(items);
        }

        void FillFullScreenDebugEnum(ref GUIContent[] strings, ref int[] values, FullScreenDebugMode min, FullScreenDebugMode max)
        {
            int count = max - min - 1;
            strings = new GUIContent[count + 1];
            values = new int[count + 1];
            strings[0] = new GUIContent(FullScreenDebugMode.None.ToString());
            values[0] = (int)FullScreenDebugMode.None;
            int index = 1;
            for (int i = (int)min + 1; i < (int)max; ++i)
            {
                strings[index] = new GUIContent(((FullScreenDebugMode)i).ToString());
                values[index] = i;
                index++;
            }
        }
    }
}
