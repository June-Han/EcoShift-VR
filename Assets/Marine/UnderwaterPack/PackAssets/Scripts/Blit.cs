using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/*
 * Blit Renderer Feature                                                
 * https://github.com/Cyanilux/URP_BlitRenderFeature
 * ------------------------------------------------------------------------------------------------------------------------
 * Based on the Blit from the UniversalRenderingExamples
 * https://github.com/Unity-Technologies/UniversalRenderingExamples/tree/master/Assets/Scripts/Runtime/RenderPasses
 * 
 * Extended to allow for :
 * - Specific access to selecting a source and destination (via current camera's color / texture id / render texture object
 * - Automatic switching to using _AfterPostProcessTexture for After Rendering event, in order to correctly handle the blit after post processing is applied
 * - Setting a _InverseView matrix (cameraToWorldMatrix), for shaders that might need it to handle calculations from screen space to world.
 * 		e.g. Reconstruct world pos from depth : https://www.cyanilux.com/tutorials/depth/#blit-perspective 
 * - (URP v10) Enabling generation of DepthNormals (_CameraNormalsTexture)
 * 		This will only include shaders who have a DepthNormals pass (mostly Lit Shaders / Graphs)
 		(workaround for Unlit Shaders / Graphs: https://gist.github.com/Cyanilux/be5a796cf6ddb20f20a586b94be93f2b)
 * ------------------------------------------------------------------------------------------------------------------------
 * @Cyanilux
*/

[CreateAssetMenu(menuName = "Feature/Blit")]
public class Blit : ScriptableRendererFeature {

    public class BlitPass : ScriptableRenderPass {

        public Material blitMaterial = null;
        public FilterMode filterMode { get; set; }

        private BlitSettings settings;

        // CHANGED: RenderTargetIdentifier is now RTHandle
        private RTHandle source { get; set; }
        private RTHandle destination { get; set; }

        RTHandle m_TemporaryColorTexture;
        RTHandle m_DestinationTexture;
        string m_ProfilerTag;

        public BlitPass(RenderPassEvent renderPassEvent, BlitSettings settings, string tag) {
            this.renderPassEvent = renderPassEvent;
            this.settings = settings;
            blitMaterial = settings.blitMaterial;
            m_ProfilerTag = tag;
        }

        // CHANGED: Setup now accepts RTHandles
        public void Setup(RTHandle source, RTHandle destination) {
            this.source = source;
            this.destination = destination;

#if UNITY_2020_1_OR_NEWER
            if (settings.requireDepthNormals)
                ConfigureInput(ScriptableRenderPassInput.Normal);
#endif
        }

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            if (settings.setInverseViewMatrix) {
                Shader.SetGlobalMatrix("_InverseView", renderingData.cameraData.camera.cameraToWorldMatrix);
            }

            if (settings.dstType == Target.TextureID) {
                if (settings.overrideGraphicsFormat){
                    opaqueDesc.graphicsFormat = settings.graphicsFormat;
                }
                RenderingUtils.ReAllocateIfNeeded(ref m_DestinationTexture, opaqueDesc, filterMode, TextureWrapMode.Clamp, name: "_DestinationTexture");
            }

            // Can't read and write to same color target, use a TemporaryRT
            if (source == destination || (settings.srcType == settings.dstType && settings.srcType == Target.CameraColor)) {
                
                RenderingUtils.ReAllocateIfNeeded(ref m_TemporaryColorTexture, opaqueDesc, filterMode, TextureWrapMode.Clamp, name: "_TemporaryColorTexture");
                
                // Pass RTHandles directly to Blit
                Blit(cmd, source, m_TemporaryColorTexture, blitMaterial, settings.blitMaterialPassIndex);
                Blit(cmd, m_TemporaryColorTexture, destination);
            } else {
                Blit(cmd, source, destination, blitMaterial, settings.blitMaterialPassIndex);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // CHANGED: Removed FrameCleanup. ReAllocateIfNeeded creates persistent handles that 
        // shouldn't be released every frame. Instead, we release them when the pass is destroyed.
        public void Dispose() {
            m_DestinationTexture?.Release();
            m_TemporaryColorTexture?.Release();
        }
    }

    [System.Serializable]
    public class BlitSettings {
        public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

        public Material blitMaterial = null;
        public int blitMaterialPassIndex = 0;
        public bool setInverseViewMatrix = false;
        public bool requireDepthNormals = false;

        public Target srcType = Target.CameraColor;
        public string srcTextureId = "_CameraColorTexture";
        public RenderTexture srcTextureObject;

        public Target dstType = Target.CameraColor;
        public string dstTextureId = "_BlitPassTexture";
        public RenderTexture dstTextureObject;

        public bool overrideGraphicsFormat = false;
        public UnityEngine.Experimental.Rendering.GraphicsFormat graphicsFormat;
    }

    public enum Target {
        CameraColor,
        TextureID,
        RenderTextureObject
    }

    public BlitSettings settings = new BlitSettings();
    public BlitPass blitPass;

    // CHANGED: These are now RTHandles
    private RTHandle srcIdentifier, dstIdentifier;

    public override void Create() {
        var passIndex = settings.blitMaterial != null ? settings.blitMaterial.passCount - 1 : 1;
        settings.blitMaterialPassIndex = Mathf.Clamp(settings.blitMaterialPassIndex, -1, passIndex);
        blitPass = new BlitPass(settings.Event, settings, name);

        if (settings.Event == RenderPassEvent.AfterRenderingPostProcessing) {
            Debug.LogWarning("Note that the \"After Rendering Post Processing\"'s Color target doesn't seem to work? (or might work, but doesn't contain the post processing) :( -- Use \"After Rendering\" instead!");
        }

        if (settings.graphicsFormat == UnityEngine.Experimental.Rendering.GraphicsFormat.None) {
            settings.graphicsFormat = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR);
        }

        UpdateSrcIdentifier();
        UpdateDstIdentifier();
    }

    private void UpdateSrcIdentifier() {
        srcIdentifier?.Release(); // Prevent memory leaks when changing targets
        srcIdentifier = UpdateIdentifier(settings.srcType, settings.srcTextureId, settings.srcTextureObject);
    }

    private void UpdateDstIdentifier() {
        dstIdentifier?.Release(); // Prevent memory leaks when changing targets
        dstIdentifier = UpdateIdentifier(settings.dstType, settings.dstTextureId, settings.dstTextureObject);
    }

    // CHANGED: Returns RTHandle and uses RTHandles.Alloc()
    private RTHandle UpdateIdentifier(Target type, string s, RenderTexture obj) {
        if (type == Target.RenderTextureObject && obj != null) {
            return RTHandles.Alloc(obj);
        } else if (type == Target.TextureID) {
            return RTHandles.Alloc(s, name: s);
        }
        return null;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if (settings.blitMaterial == null) {
            Debug.LogWarningFormat("Missing Blit Material. {0} blit pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
            return;
        }

        if (settings.Event == RenderPassEvent.AfterRenderingPostProcessing) {
        } else if (settings.Event == RenderPassEvent.AfterRendering && renderingData.postProcessingEnabled) {
            if (settings.srcType == Target.CameraColor) {
                settings.srcType = Target.TextureID;
                settings.srcTextureId = "_AfterPostProcessTexture";
                UpdateSrcIdentifier();
            }
            if (settings.dstType == Target.CameraColor) {
                settings.dstType = Target.TextureID;
                settings.dstTextureId = "_AfterPostProcessTexture";
                UpdateDstIdentifier();
            }
        } else {
            if (settings.srcType == Target.TextureID && settings.srcTextureId == "_AfterPostProcessTexture") {
                settings.srcType = Target.CameraColor;
                settings.srcTextureId = "";
                UpdateSrcIdentifier();
            }
            if (settings.dstType == Target.TextureID && settings.dstTextureId == "_AfterPostProcessTexture") {
                settings.dstType = Target.CameraColor;
                settings.dstTextureId = "";
                UpdateDstIdentifier();
            }
        }
    }

    // CHANGED: SetupRenderPasses is the modern URP standard for grabbing camera targets safely
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (settings.blitMaterial == null) return;

        // CHANGED: Used cameraColorTargetHandle instead of cameraColorTarget
        var src = (settings.srcType == Target.CameraColor) ? renderer.cameraColorTargetHandle : srcIdentifier;
        var dest = (settings.dstType == Target.CameraColor) ? renderer.cameraColorTargetHandle : dstIdentifier;

        blitPass.Setup(src, dest);
        renderer.EnqueuePass(blitPass);
    }

    // CHANGED: Clean up allocated handles when the feature is destroyed to prevent memory leaks
    protected override void Dispose(bool disposing)
    {
        srcIdentifier?.Release();
        dstIdentifier?.Release();
        blitPass?.Dispose();
    }
}