using EarwaxSim;
using UnityEngine;

public class ParticleRenderer : MonoBehaviour
{
    public XPBDSim sim;
    public Mesh mesh;
    public Material material;

    int strideInBytes = 12; // Vector 3 is 12 bytes. NOTE: Maybe use vector 4 since 16 bytes is better for GPU

    GraphicsBuffer positionBuffer;
    RenderParams rps;

    private void Awake()
    {
        rps = new RenderParams(material);
        rps.worldBounds = new Bounds(Vector3.zero, 1000f * Vector3.one);
    }

    private void Start()
    {
        positionBuffer = new(
            GraphicsBuffer.Target.Structured,
            sim.ps.count,
            strideInBytes); // Buffer for particle positions

        material.SetBuffer("_Positions", positionBuffer); // Buffer is called "_Positions" inside shader
    }

    private void LateUpdate()
    {
        positionBuffer.SetData(sim.ps.currentPosition); // Update positionBuffer with new currentPositions

        Graphics.RenderMeshPrimitives(in rps, mesh, 0, sim.ps.count);
    }

    private void OnDestroy()
    {
        positionBuffer?.Release();
    }
}
