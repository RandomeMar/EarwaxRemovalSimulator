using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Custom render pass for rendering billboarded earwax particles.
/// </summary>
public class EarwaxBillboardPass : ScriptableRenderPass
{
    const string m_PassName = "EarwaxBillboardPass";
    ParticleRenderer particleRenderer;
    Material compositeMaterial;

    public EarwaxBillboardPass() { }

    /// <summary>
    /// Input data for billboard pass step.
    /// </summary>
    /// <remarks>This class should be passed into ExecutePass.</remarks>
    class PassData
    {
        internal Material material;
        internal Mesh mesh;
        internal int particleCount;
    }

    /// <summary>
    /// Input data for composite pass step.
    /// </summary>
    class CompositePassData
    {
        internal TextureHandle inputTexture;
        internal Material material;
    }

    /// <summary>
    /// Sets up necessary references.
    /// </summary>
    /// <param name="particleRenderer"></param>
    /// <param name="compositeMaterial"></param>
    public void Setup(ParticleRenderer particleRenderer, Material compositeMaterial)
    {
        this.particleRenderer = particleRenderer;
        this.compositeMaterial = compositeMaterial;
    }

    /// <summary>
    /// Render function used by the billboard pass step.
    /// </summary>
    /// <param name="data">Input data to the render function.</param>
    /// <param name="context"></param>
    static void ExecutePass(PassData data, RasterGraphContext context)
    {
        if (data.mesh == null || data.material == null || data.particleCount <= 0)
            return;

        context.cmd.ClearRenderTarget(false, true, new Color(1000f, 0f, 0f, 0f));

        // Draw call for drawing all of the billboard quads
        context.cmd.DrawMeshInstancedProcedural(
            data.mesh,
            0,
            data.material,
            0,
            data.particleCount,
            null
        );
    }

    static void ExecuteCompositePass(CompositePassData data, RasterGraphContext context)
    {
        if (data.material == null || data.inputTexture.IsUnityNull()) return;
        Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), data.material, 0);
    }


    // Sets up input/output for the pass, and binds the ExecutePass method as the function to run when rendering
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (!particleRenderer.isReady) return;

        // Gets texture description from the camera's color texture
        var sceneData = frameData.Get<UniversalResourceData>();


        // Billboard color texture
        var colorDesc = sceneData.cameraColor.GetDescriptor(renderGraph);
        colorDesc.depthBufferBits = 0;
        colorDesc.name = "BillboardColor";
        colorDesc.format = GraphicsFormat.R8G8B8A8_UNorm;
        TextureHandle billboardTexture = renderGraph.CreateTexture(in colorDesc);


        // 1. Render particle billboards to billboardTexture
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(m_PassName, out PassData passData))
        {
            // Supply passData with necessary data
            passData.material = particleRenderer.material;
            passData.mesh = particleRenderer.mesh;
            passData.particleCount = particleRenderer.particleCount;

            // Sets pass's render target to the outputTexture
            builder.SetRenderAttachment(billboardTexture, 0);
            builder.SetRenderAttachmentDepth(sceneData.activeDepthTexture, AccessFlags.Write);

            // Prevents pass from being culled by the render graph optimizer
            builder.AllowPassCulling(false);

            // The function that is called every pass
            builder.SetRenderFunc(static (PassData data, RasterGraphContext context)
                => ExecutePass(data, context));

            int billboardTexID = Shader.PropertyToID("_BillboardTex");
            builder.SetGlobalTextureAfterPass(billboardTexture, billboardTexID);
        }


        var destinationDesc = sceneData.cameraColor.GetDescriptor(renderGraph);
        destinationDesc.depthBufferBits = 0;
        destinationDesc.name = "Destination";
        TextureHandle destinationTexture = renderGraph.CreateTexture(in destinationDesc);


        var blitParams = new RenderGraphUtils.BlitMaterialParameters(
            sceneData.activeColorTexture,
            destinationTexture,
            compositeMaterial,
            0);
        renderGraph.AddBlitPass(blitParams, "Composite Billboard");

        sceneData.cameraColor = destinationTexture;
    }
}
