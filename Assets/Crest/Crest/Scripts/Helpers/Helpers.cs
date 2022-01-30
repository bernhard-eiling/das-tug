// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    using UnityEngine;
    using UnityEngine.Rendering;

    /// <summary>
    /// General purpose helpers which, at the moment, do not warrant a seperate file.
    /// </summary>
    public static class Helpers
    {
        static Material s_UtilityMaterial;
        public static Material UtilityMaterial
        {
            get
            {
                if (s_UtilityMaterial == null)
                {
                    s_UtilityMaterial = new Material(Shader.Find("Hidden/Crest/Helpers/Utility"));
                }

                return s_UtilityMaterial;
            }
        }

        // Need to cast to int but no conversion cost.
        // https://stackoverflow.com/a/69148528
        internal enum UtilityPass
        {
            CopyColor,
            CopyDepth,
            ClearDepth,
            ClearStencil,
        }

        public static bool IsPreviewOfGameCamera(Camera camera)
        {
            return camera.cameraType == CameraType.Game && camera.name.StartsWith("Preview");
        }

        public static bool IsMSAAEnabled(Camera camera)
        {
            var isMSAA = camera.allowMSAA;
#if UNITY_EDITOR
            // Game View Preview ignores allowMSAA.
            isMSAA = isMSAA || IsPreviewOfGameCamera(camera);
            // Scene view doesn't support MSAA.
            isMSAA = isMSAA && camera.cameraType != CameraType.SceneView;
#endif
            return isMSAA && QualitySettings.antiAliasing > 1;
        }

        public static bool IsMotionVectorsEnabled()
        {
            // Default to false until we support MVs.
            return false;
        }

        public static bool IsIntelGPU()
        {
            // Works for Windows and MacOS. Grabbed from Unity Graphics repository:
            // https://github.com/Unity-Technologies/Graphics/blob/68b0d42c/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/HDRenderPipeline.PostProcess.cs#L198-L199
            return SystemInfo.graphicsDeviceName.ToLowerInvariant().Contains("intel");
        }

        public static bool MaskIncludesLayer(int mask, int layer)
        {
            // Taken from:
            // http://answers.unity.com/answers/1332280/view.html
            return mask == (mask | (1 << layer));
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            var temp = b;
            b = a;
            a = temp;
        }

        public static void SetGlobalKeyword(string keyword, bool enabled)
        {
            if (enabled)
            {
                Shader.EnableKeyword(keyword);
            }
            else
            {
                Shader.DisableKeyword(keyword);
            }
        }

        public static void RenderTargetIdentifierXR(ref RenderTexture texture, ref RenderTargetIdentifier target)
        {
            target = new RenderTargetIdentifier
            (
                texture,
                mipLevel: 0,
                CubemapFace.Unknown,
                depthSlice: -1 // Bind all XR slices.
            );
        }

        /// <summary>
        /// Creates an RT reference and adds it to the RTI. Native object behind RT is not created so you can change its
        /// properties before being used.
        /// </summary>
        public static void CreateRenderTargetTextureReference(ref RenderTexture texture, ref RenderTargetIdentifier target)
        {
            // Do not overwrite reference or it will create reference leak.
            if (texture == null)
            {
                // Dummy values. We are only creating an RT reference, not an RT native object. RT should be configured
                // properly before using or calling Create.
                texture = new RenderTexture(0, 0, 0)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                };
            }

            // Always call this in case of recompilation as RTI will lose its reference to the RT.
            RenderTargetIdentifierXR(ref texture, ref target);
        }

        public static bool RenderTargetTextureNeedsUpdating(RenderTexture texture, RenderTextureDescriptor descriptor)
        {
            return
                descriptor.width != texture.width ||
                descriptor.height != texture.height ||
                descriptor.volumeDepth != texture.volumeDepth ||
                descriptor.useDynamicScale != texture.useDynamicScale;
        }

        /// <summary>
        /// Uses Destroy in play mode or DestroyImmediate in edit mode.
        /// </summary>
        public static void Destroy(Object @object)
        {
#if UNITY_EDITOR
            // We must use DestroyImmediate in edit mode. As it apparently has an overhead, use recommended Destroy in
            // play mode. DestroyImmediate is generally recommended in edit mode by Unity:
            // https://docs.unity3d.com/ScriptReference/Object.DestroyImmediate.html
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(@object);
            }
            else
#endif
            {
                Object.Destroy(@object);
            }
        }

        /// <summary>
        /// Blit using full screen triangle.
        /// </summary>
        public static void Blit(CommandBuffer buffer, RenderTargetIdentifier target, Material material, int pass, MaterialPropertyBlock properties = null)
        {
            buffer.SetRenderTarget(target);
            buffer.DrawProcedural
            (
                Matrix4x4.identity,
                material,
                pass,
                MeshTopology.Triangles,
                vertexCount: 3,
                instanceCount: 1,
                properties
            );
        }
    }

    static class Extensions
    {
        public static void SetKeyword(this Material material, string keyword, bool enabled)
        {
            if (enabled)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }

        public static void SetKeyword(this ComputeShader shader, string keyword, bool enabled)
        {
            if (enabled)
            {
                shader.EnableKeyword(keyword);
            }
            else
            {
                shader.DisableKeyword(keyword);
            }
        }

        public static void SetShaderKeyword(this CommandBuffer buffer, string keyword, bool enabled)
        {
            if (enabled)
            {
                buffer.EnableShaderKeyword(keyword);
            }
            else
            {
                buffer.DisableShaderKeyword(keyword);
            }
        }

        ///<summary>
        /// Sets the msaaSamples property to the highest supported MSAA level in the settings.
        ///</summary>
        public static void SetMSAASamples(this ref RenderTextureDescriptor descriptor, Camera camera)
        {
            // QualitySettings.antiAliasing is zero when disabled which is invalid for msaaSamples.
            // We need to set this first as GetRenderTextureSupportedMSAASampleCount uses it:
            // https://docs.unity3d.com/ScriptReference/SystemInfo.GetRenderTextureSupportedMSAASampleCount.html
            descriptor.msaaSamples = Helpers.IsMSAAEnabled(camera) ? Mathf.Max(QualitySettings.antiAliasing, 1) : 1;
            descriptor.msaaSamples = SystemInfo.GetRenderTextureSupportedMSAASampleCount(descriptor);
        }
    }
}
