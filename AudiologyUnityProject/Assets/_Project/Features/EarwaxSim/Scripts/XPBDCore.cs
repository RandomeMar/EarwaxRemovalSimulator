using System.Collections.Generic;
using UnityEngine;


namespace EarwaxSim
{
    public class Constants
    {
        public const float EPS = 1e-6f;
        public const float SEAM_EPS = 1e-6f;
        public const int MAX_NEIGHBORS = 64;
    }


    // Contains particle positions, velocities, and mass values
    public class ParticleSet
    {
        public Vector3[] currentPosition;
        public Vector3[] previousPosition;
        public Vector3[] velocity;
        public float[] invMass;
        public float[] mass;

        public float radius;
        public int count;

        public ParticleSet(int count, float radius)
        {
            this.count = count;
            this.radius = radius;

            this.currentPosition = new Vector3[count];
            this.previousPosition = new Vector3[count];
            this.velocity = new Vector3[count];
            this.invMass = new float[count];
            this.mass = new float[count];
        }
    }

    // Dict used to store particles in a 3D grid
    public class SpatialHash
    {
        float cellSize;
        Dictionary<long, int> bucketHeads;
        int[] next;
        int[] neighborBuffer;


        public SpatialHash(float cellSize, int particleCount)
        {
            this.cellSize = cellSize;
            this.bucketHeads = new(particleCount);
            this.next = new int[particleCount];
            this.neighborBuffer = new int[Constants.MAX_NEIGHBORS]; // Array of neighbor ints to be reused for GetNeighbors
        }

        public (int, int, int) CalcCellCoord(Vector3 position)
        {
            return (
                Mathf.FloorToInt(position.x / cellSize),
                Mathf.FloorToInt(position.y / cellSize),
                Mathf.FloorToInt(position.z / cellSize)
                );
        }

        public long HashCoord(int x_coord, int y_coord, int z_coord)
        {
            const int SHIFT = 20;

            long x = (long)(x_coord + (1 << SHIFT));
            long y = (long)(y_coord + (1 << SHIFT));
            long z = (long)(z_coord + (1 << SHIFT));

            return (x << 42) | (y << 21) | z;
        }

        // Rebuilds grid housing particles
        public void BuildGrid(ParticleSet ps)
        {
            bucketHeads.Clear();

            for (int i = 0; i < ps.count; i++)
            {
                (int x, int y, int z) = CalcCellCoord(ps.currentPosition[i]);
                long key = HashCoord(x, y, z);
                if (!this.bucketHeads.TryGetValue(key, out int oldHead))
                {
                    this.bucketHeads[key] = i;
                    this.next[i] = -1;
                }
                else
                {
                    this.next[i] = oldHead;
                    this.bucketHeads[key] = i;
                }
            }
        }

        // Returns the indexes of neighbors to particle i along with the amount of neighbors
        public (int[], int) GetNeighbors(ParticleSet ps, int i, float h)
        {
            (int base_x, int base_y, int base_z) = CalcCellCoord(ps.currentPosition[i]);
            int neighborCount = 0;
            Vector3 distVec = Vector3.zero;
            float r2 = 0f;
            float h2 = h * h;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        // If too many neighbors for buffer
                        if (neighborCount >= this.neighborBuffer.Length)
                            return (this.neighborBuffer, neighborCount);

                        long key = HashCoord(base_x + dx, base_y + dy, base_z + dz);

                        // If no particles in cell
                        if (!this.bucketHeads.TryGetValue(key, out int j))
                        {
                            continue;
                        }

                        // For first index in bucket
                        if (j != i)
                        {
                            distVec = ps.currentPosition[i] - ps.currentPosition[j];
                            r2 = distVec.sqrMagnitude;
                            if (r2 <= h2)
                            {
                                this.neighborBuffer[neighborCount] = j;
                                neighborCount++;
                            }
                        }

                        while (next[j] != -1)
                        {
                            // If too many neighbors for buffer
                            if (neighborCount >= this.neighborBuffer.Length)
                                return (this.neighborBuffer, neighborCount);

                            j = next[j];

                            if (j != i)
                            {
                                distVec = ps.currentPosition[i] - ps.currentPosition[j];
                                r2 = distVec.sqrMagnitude;
                                if (r2 <= h2)
                                {
                                    this.neighborBuffer[neighborCount] = j;
                                    neighborCount++;
                                }
                            }
                        }
                    }
            return (this.neighborBuffer, neighborCount);
        }

    }

}
