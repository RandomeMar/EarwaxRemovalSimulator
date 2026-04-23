using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class EarwaxBillboardPass : ScriptableRenderPass
{
    const string m_PassName = "EarwaxBillboardPass";
    ParticleRenderer particleRenderer;

    Material compositeMaterial;
    Material renderMaterial;

    public EarwaxBillboardPass() { }

    // Class defining what will be passed into the pass when it actually runs.
    // Goes into ExecutePass()
    class BillboardPassData
    {
        internal Material material;
        internal Mesh mesh;
        internal int particleCount;
    }

    class CompositePassData
    {
        internal Material material;
        internal TextureHandle sceneColor;
        internal TextureHandle earwaxLit;

        internal TextureHandle sceneDepth;
        internal TextureHandle earwaxDepth;
    }


    public void Setup(ParticleRenderer particleRenderer, Material compositeMaterial, Material renderMaterial)
    {
        this.particleRenderer = particleRenderer;
        this.compositeMaterial = compositeMaterial;
        this.renderMaterial = renderMaterial;
    }

    static void ExecuteBillboardPass(BillboardPassData data, RasterGraphContext context)
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
    

    // Sets up input/output for the pass, and binds the ExecutePass method as the function to run when rendering
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (!particleRenderer.isReady) return;

        // Gets texture description from the camera's color texture
        var sceneData = frameData.Get<UniversalResourceData>();

        #region Textures
        // Billboard color texture
        var colorDesc = sceneData.cameraColor.GetDescriptor(renderGraph);
        colorDesc.depthBufferBits = 0;
        colorDesc.name = "BillboardColor";
        colorDesc.format = GraphicsFormat.R8G8B8A8_UNorm;
        TextureHandle billboardColor = renderGraph.CreateTexture(in colorDesc);

        // Billboard depth texture
        var depthDesc = sceneData.cameraDepth.GetDescriptor(renderGraph);
        depthDesc.name = "BillboardDepth";
        TextureHandle billboardDepth = renderGraph.CreateTexture(in depthDesc);

        // Debug texture
        var debugDesc = sceneData.cameraColor.GetDescriptor(renderGraph);
        debugDesc.depthBufferBits = 0;
        debugDesc.name = "EarwaxLit";
        debugDesc.format = GraphicsFormat.R8G8B8A8_UNorm;
        TextureHandle earwaxLit = renderGraph.CreateTexture(in debugDesc);

        // Destination texture
        var destinationDesc = sceneData.cameraColor.GetDescriptor(renderGraph);
        destinationDesc.depthBufferBits = 0;
        destinationDesc.name = "Destination";
        TextureHandle destinationTexture = renderGraph.CreateTexture(in destinationDesc);
        #endregion

        // 1. Render particle billboards to a depth texture
        using (var builder = renderGraph.AddRasterRenderPass<BillboardPassData>(m_PassName, out BillboardPassData passData))
        {
            // Supply passData with necessary data
            passData.material = particleRenderer.billboardMaterial;
            passData.mesh = particleRenderer.mesh;
            passData.particleCount = particleRenderer.particleCount;

            // Sets pass's render target to the outputTexture
            builder.SetRenderAttachment(billboardColor, 0);
            builder.SetRenderAttachmentDepth(billboardDepth, AccessFlags.Write);

            // Prevents pass from being culled by the render graph optimizer
            builder.AllowPassCulling(false);

            // The function that is called every pass
            builder.SetRenderFunc(static (BillboardPassData data, RasterGraphContext context)
                => ExecuteBillboardPass(data, context));

            // TODO: May need to make depth texture global instead.
            int billboardTexID = Shader.PropertyToID("_BillboardTex");
            builder.SetGlobalTextureAfterPass(billboardColor, billboardTexID);
        }


        // 2. Calculate normals using gradient of depth texture. Use normals for lambertian diffuse
        var renderBlitParams = new RenderGraphUtils.BlitMaterialParameters(
            billboardDepth,
            earwaxLit,
            renderMaterial,
            0);
        renderGraph.AddBlitPass(renderBlitParams, "Render Particles from Field");

        // 3. Composite earwax color texture with scene color texture based on depth textures
        using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>("CompositePass", out CompositePassData passData))
        {
            passData.material = compositeMaterial;
            passData.sceneColor = sceneData.activeColorTexture;
            passData.earwaxLit = earwaxLit;
            passData.sceneDepth = sceneData.activeDepthTexture;
            passData.earwaxDepth = billboardDepth;

            builder.UseTexture(passData.sceneColor, AccessFlags.Read);
            builder.UseTexture(passData.earwaxLit, AccessFlags.Read);
            builder.UseTexture(passData.sceneDepth, AccessFlags.Read);
            builder.UseTexture(billboardDepth, AccessFlags.Read);

            builder.SetRenderAttachment(destinationTexture, 0, AccessFlags.WriteAll);

            builder.AllowPassCulling(false);

            builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext context) =>
            {
                if (data.material == null)
                    return;

                data.material.SetTexture("_SceneColorTex", data.sceneColor);
                data.material.SetTexture("_EarwaxLitTex", data.earwaxLit);

                data.material.SetTexture("_SceneDepthTex", data.sceneDepth);
                data.material.SetTexture("_EarwaxDepthTex", data.earwaxDepth);

                // Draw a screen sized triangle for blit
                context.cmd.DrawProcedural(
                    Matrix4x4.identity,
                    data.material,
                    0,
                    MeshTopology.Triangles,
                    3,
                    1
                );
            });
        }

        sceneData.cameraColor = destinationTexture;
    }
}
