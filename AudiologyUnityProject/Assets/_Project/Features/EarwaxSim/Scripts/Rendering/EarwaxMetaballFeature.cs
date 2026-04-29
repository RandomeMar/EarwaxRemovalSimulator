using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Custom render feature responsible for the metaball style rendering used for the earwax.
/// </summary>
public class EarwaxMetaballFeature : ScriptableRendererFeature
{
    [SerializeField] private ParticleRenderer particleRenderer;
    [SerializeField] private Material compositeMaterial;
    [SerializeField] private Material renderMaterial;

    private EarwaxBillboardPass pass;

    public override void Create()
    {
        pass = new EarwaxBillboardPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        particleRenderer = ParticleRenderer.current;

        if (particleRenderer == null) return;

        pass.Setup(particleRenderer, compositeMaterial, renderMaterial);
        renderer.EnqueuePass(pass);
    }
}
