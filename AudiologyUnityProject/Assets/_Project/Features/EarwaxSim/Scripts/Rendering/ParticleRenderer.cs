using EarwaxSim;
using System.Xml.Serialization;
using UnityEngine;

/// <summary>
/// Game object manager of particle rendering.
/// </summary>
/// <remarks>This game object is responsible for initializing and updating the particle position buffer.</remarks>
public class ParticleRenderer : MonoBehaviour
{
    public XPBDSim sim;
    public Mesh mesh;
    public Material billboardMaterial;
    public GraphicsBuffer positionBuffer { get; private set; }
    public int particleCount => sim ? sim.ps.maxCount : 0;
    public bool isReady =>
        sim != null &&
        mesh != null &&
        billboardMaterial != null &&
        positionBuffer != null &&
        particleCount > 0;

    public static ParticleRenderer current {  get; private set; } // The current running particle renderer. Accessed by render feature

    private const int strideInBytes = 12; // Vector 3 is 12 bytes. NOTE: Maybe use vector 4 since 16 bytes is better for GPU

    private void OnEnable()
    {
        current = this;
    }

    private void OnDisable()
    {
        if (current = this) current = null;
    }

    /// <summary>
    /// Initializes particle position buffer between CPU and GPU.
    /// </summary>
    private void Start()
    {
        if (sim == null) return;

        positionBuffer = new(
            GraphicsBuffer.Target.Structured,
            sim.ps.count,
            strideInBytes); // Buffer for particle positions

        billboardMaterial.SetBuffer("_Positions", positionBuffer); // Buffer is called "_Positions" inside shader
    }

    /// <summary>
    /// Updates the particle position buffer with new particle positions.
    /// </summary>
    private void LateUpdate()
    {
        if (positionBuffer == null || sim == null) return;
        positionBuffer.SetData(sim.ps.currentPosition); // Update positionBuffer with new currentPositions
    }

    private void OnDestroy()
    {
        positionBuffer?.Release();
    }
}
