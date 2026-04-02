using EarwaxSim;
using UnityEngine;

public class ParticleRenderer : MonoBehaviour
{
    public GameObject xpbdSim;
    public Mesh mesh;
    public Material material;

    public int particleCount = 512; // NOTE: This is so bad. We should read particleCount from sim, but it is initiallized inside start, not awake


    int strideInBytes = 12; // Vector 3 is 12 bytes. Maybe use vector 4 since 16 bytes is better

    XPBDSim sim;
    

    GraphicsBuffer positionBuffer;
    RenderParams rps;

    private void Awake()
    {
        sim = xpbdSim.GetComponent<XPBDSim>();

        rps = new RenderParams(material);
        rps.worldBounds = new Bounds(Vector3.zero, 1000f * Vector3.one);

        positionBuffer = new(
            GraphicsBuffer.Target.Structured,
            particleCount,
            strideInBytes);

        material.SetBuffer("_Positions", positionBuffer);
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
