using EarwaxSim;
using System;
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

    public GraphicsBuffer PositionBuffer { get; private set; }
    public GraphicsBuffer ActiveBuffer { get; private set; }

    public int particleCount => sim ? sim.ps.maxCount : 0;
    public bool isReady =>
        sim != null &&
        mesh != null &&
        billboardMaterial != null &&
        PositionBuffer != null &&
        particleCount > 0;

    public static ParticleRenderer Instance {  get; private set; } // The current running particle renderer. Accessed by render feature

    private const int strideInBytes = 12; // Vector 3 is 12 bytes. NOTE: Maybe use vector 4 since 16 bytes is better for GPU

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        ParticleRenderer.Instance = this;
    }


    /// <summary>
    /// Initializes particle position buffer between CPU and GPU.
    /// </summary>
    private void Start()
    {
        if (sim == null) return;

        PositionBuffer = new(
            GraphicsBuffer.Target.Structured,
            sim.ps.maxCount,
            strideInBytes); // Buffer for particle positions

        ActiveBuffer = new(
            GraphicsBuffer.Target.Structured,
            sim.ps.maxCount,
            sizeof(int)); // Buffer for if particles are active

        billboardMaterial.SetBuffer("_Positions", PositionBuffer); // Buffer is called "_Positions" inside shader
        billboardMaterial.SetBuffer("_Actives", ActiveBuffer);
    }

    /// <summary>
    /// Updates the particle position buffer with new particle positions.
    /// </summary>
    private void LateUpdate()
    {
        if (PositionBuffer == null || ActiveBuffer == null || sim == null) return;

        PositionBuffer.SetData(sim.ps.currentPosition); // Update PositionBuffer with new currentPositions

        int[] activeInts = Array.ConvertAll(sim.ps.active, b => b ? 1 : 0); // Because bools cannot be passed to a buffer
        ActiveBuffer.SetData(activeInts); // Update ActiveBuffer
    }

    private void OnDestroy()
    {
        PositionBuffer?.Release();
        ActiveBuffer?.Release();
    }
}
