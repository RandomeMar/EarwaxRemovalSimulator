using System.Collections.Generic;
using UnityEngine;

namespace EarwaxSim
{
    // Quick-pick wax types. Applied by XPBDSim.ApplyWaxPreset() before BuildSimulation.
    // Custom = leave all inspector values alone.
    public enum WaxPreset
    {
        Custom,
        DryCrumbly,
        SoftSticky,
        OldImpacted
    }

    public class ProceduralEarwax
    {
        //Shape Params
        //Seed for reproducible shapes
        public int seed = 42;

        //Base radius of the ellipsoid before noise
        public Vector3 baseRadii = new Vector3(0.5f, 0.3f, 0.4f);

        //num of particles along each axis of the sampling grid
        public int resolution = 6;

        //World pos of the earwax center
        public Vector3 origin = Vector3.zero;


        //Noise Params
        //How much the surface is displaced by noise
        public float noiseStrength = 0.3f;

        //Frequency of the noise. Higher = more bumps
        public float noiseFrequency = 2.0f;

        //noise detail
        public int noiseOctaves = 3;

        //Material (copied over form XPBDSim)
        public float materialDensity = 1.0f;
        public float baseBondCompliance = 0.0f;
        public float hMult = 1.25f;
        public float denseCompliance = 0.0f;

        //plasticity (passed through to DistanceConstraintSet)
        public float yieldStrain = 0.5f;
        public float plasticFlow = 1.0f;
        public float breakStrain = 1.0f;
        public float adaptRate = 0.0f;
        public float recoveryRate = 0.0f;


        //If true, baseRadii / noiseStrength / noiseFrequency are rolled from the ranges below per seed
        public bool useParametricRanges = false;

        //min radius when useParametricRanges is on
        public Vector3 radiiMin = new Vector3(0.3f, 0.2f, 0.3f);
        //max radius when useParametricRanges is on
        public Vector3 radiiMax = new Vector3(0.6f, 0.5f, 0.5f);

        //Min/max noise strength range (x = min, y = max)
        public Vector2 noiseStrengthRange = new Vector2(0.15f, 0.45f);
        //Min/max noise frequency range 
        public Vector2 noiseFrequencyRange = new Vector2(1.5f, 3.5f);

        //Asymmetry / lobes
        // Breaks the ellipsoid symmetry so wax isn't perfect blob.
        [Range(0f, 1f)]
        public float bumpinessBias = 0f;    //0 = uniform bumpiness, 1 = noise fully biased to one side of the blob
        public Vector3 bumpinessBiasAxis = Vector3.up;  //direction the extra bumpiness points toward

        [Range(0f, 1f)]
        public float stretchAmount = 0f;    // 0 = straight, higher = more bent/lobey
        public float stretchFrequency = 3f;//frequency for the stretch warp

   
        [Range(0f, 1f)]
        public float squashAmount = 0f; //0 = no squash, 1 = fully flatten
        //Direction that gets squashed. Default -Y = flatten the bottom
        public Vector3 squashAxis = Vector3.down;


        public (ParticleSet, SpatialHash, DistanceConstraintSet, DensityConstraintSolver) Generate()
        {
            // Seed the noise offset so different seeds produce different shapes
            Random.State prevState = Random.state;
            Random.InitState(seed);
            Vector3 noiseOffset = new Vector3(
                Random.Range(-1000f, 1000f),
                Random.Range(-1000f, 1000f),
                Random.Range(-1000f, 1000f)
            );
            // Slight random rotation of the whole shape so it doesn't always align to axis
            Quaternion randomRot = Quaternion.Euler(
                Random.Range(-15f, 15f),
                Random.Range(-180f, 180f),
                Random.Range(-15f, 15f)
            );

            // roll actual shape values from ranges using the sameseeded RNG so results stay reproducible per seed.
            Vector3 actualRadii = baseRadii;
            float actualNoiseStrength = noiseStrength;
            float actualNoiseFrequency = noiseFrequency;
            if (useParametricRanges)
            {
                actualRadii = new Vector3(
                    Random.Range(radiiMin.x, radiiMax.x),
                    Random.Range(radiiMin.y, radiiMax.y),
                    Random.Range(radiiMin.z, radiiMax.z)
                );
                actualNoiseStrength = Random.Range(noiseStrengthRange.x, noiseStrengthRange.y);
                actualNoiseFrequency = Random.Range(noiseFrequencyRange.x, noiseFrequencyRange.y);
            }

            Random.state = prevState;

            //prenormalize asymmetry axes
            Vector3 biasAxisN = bumpinessBiasAxis.sqrMagnitude > 1e-6f ? bumpinessBiasAxis.normalized : Vector3.up;
            Vector3 squashAxisN = squashAxis.sqrMagnitude > 1e-6f ? squashAxis.normalized : Vector3.down;

            float maxRadius = Mathf.Max(actualRadii.x, actualRadii.y, actualRadii.z);
            // Grid extent also has to account for the stretch warp pushing particles outward.
            float gridExtent = maxRadius * (1f + actualNoiseStrength + stretchAmount) * 1.2f;
            float spacing = (gridExtent * 2f) / (resolution - 1);

            List<Vector3> positions = new List<Vector3>();

            for (int ix = 0; ix < resolution; ix++)
            for (int iy = 0; iy < resolution; iy++)
            for (int iz = 0; iz < resolution; iz++)
            {
                // Grid position centered at origin
                Vector3 gridPos = new Vector3(
                    -gridExtent + ix * spacing,
                    -gridExtent + iy * spacing,
                    -gridExtent + iz * spacing
                );

                // Apply random rotation
                Vector3 rotatedPos = randomRot * gridPos;

                // bends the blob so it isn't a straight ellipsoid.
                Vector3 warpedPos = rotatedPos;
                if (stretchAmount > 0f)
                {
                    float s = stretchAmount * maxRadius;
                    warpedPos.y += Mathf.Sin(rotatedPos.x * stretchFrequency) * s;
                    warpedPos.x += Mathf.Sin(rotatedPos.z * stretchFrequency) * s * 0.5f;
                }

                //Squash one end as if pressed against the earcanal wall
                if (squashAmount > 0f)
                {
                    float t = Vector3.Dot(warpedPos, squashAxisN) / maxRadius; // ~[-1, 1]
                    if (t > 0f)
                    {
                        warpedPos -= squashAxisN * (t * maxRadius * squashAmount);
                    }
                }

                // Normalize to ellipsoid space
                Vector3 ellipNorm = new Vector3(
                    warpedPos.x / actualRadii.x,
                    warpedPos.y / actualRadii.y,
                    warpedPos.z / actualRadii.z
                );
                float ellipDist = ellipNorm.magnitude; // <1 means inside ellipsoid

                //Perlin noise at this point to deform the surface
                float noiseVal = SampleNoise(gridPos, noiseOffset, actualNoiseFrequency, actualNoiseStrength);

                // amplify noise on one side of the blob, suppress on the other.
                // Uses the rotated grid position projected onto biasAxis to pick a side.
                if (bumpinessBias > 0f && maxRadius > 1e-6f)
                {
                    float bt = Vector3.Dot(rotatedPos, biasAxisN) / maxRadius;
                    float biasFactor = 1f + bumpinessBias * bt;
                    noiseVal *= Mathf.Max(0f, biasFactor);
                }

                float surfaceThreshold = 1.0f + noiseVal;

                if (ellipDist < surfaceThreshold)
                {
                    positions.Add(gridPos + origin);
                }
            }

            if (positions.Count < 2)
            {
                Debug.LogWarning("[ProceduralEarwax] try increasing radii.");// Fallback: at least put one particle at origin
                positions.Add(origin);
                positions.Add(origin + Vector3.right * spacing);
            }

            int particleCount = positions.Count;
            float particleRad = .1f;

            //ParticleSet
            float totalVolume = (4f / 3f) * Mathf.PI * actualRadii.x * actualRadii.y * actualRadii.z;
            float totalMass = materialDensity * totalVolume;
            float particleMass = totalMass / particleCount;
            float invMass = 1f / particleMass;

            ParticleSet ps = new ParticleSet(particleCount, particleRad);
            for (int i = 0; i < particleCount; i++)
            {
                ps.currentPosition[i] = positions[i];
                ps.previousPosition[i] = positions[i];
                ps.velocity[i] = Vector3.zero;
                ps.invMass[i] = invMass;
                ps.mass[i] = particleMass;
            }

            // spatial hash and find neighbor bonds, uses a larger search radius for bonding so we catch diagonal neighbors
            float bondSearchRadius = spacing * 1.8f; 
            float h = spacing * hMult; // smaller radius for density calculations (same as lattice in SIM)

            SpatialHash grid = new SpatialHash(h, particleCount);
            grid.BuildGrid(ps);

            // Temlp wider grid for finding bond neighbors
            SpatialHash bondGrid = new SpatialHash(bondSearchRadius, particleCount);
            bondGrid.BuildGrid(ps);

            float bondCompliance = baseBondCompliance * spacing;
            List<DistanceConstraint> bonds = new List<DistanceConstraint>();

            // Cconnects each particle to its spatial neighbors (direct + diagonal)
            for (int i = 0; i < particleCount; i++)
            {
                int[] neighbors;
                int nCount;
                (neighbors, nCount) = bondGrid.GetNeighbors(ps, i, bondSearchRadius);

                for (int n = 0; n < nCount; n++)
                {
                    int j = neighbors[n];
                    if (j <= i) continue; // avoids duplicate bonds

                    float dist = Vector3.Distance(ps.currentPosition[i], ps.currentPosition[j]);

                    // Only bond particles within ~sqrt(3) * spacing 
                    if (dist <= spacing * 1.75f)
                    {
                        bonds.Add(new DistanceConstraint(i, j, dist, bondCompliance));
                    }
                }
            }
            DistanceConstraintSet dcs = new DistanceConstraintSet(
                bonds.ToArray(), yieldStrain, plasticFlow, breakStrain, adaptRate, recoveryRate);

            // Step 4: Calculate rest density from a central particle
            float restDensity = CalcRestDensity(ps, grid, h);

            DensityConstraintSolver dense = new DensityConstraintSolver(restDensity, h, denseCompliance);

            return (ps, grid, dcs, dense);
        }

        private float SampleNoise(Vector3 pos, Vector3 offset, float frequencyBase, float strength)
        {
            float total = 0f;
            float amplitude = 1f;
            float frequency = frequencyBase;
            float maxAmplitude = 0f;

            for (int o = 0; o < noiseOctaves; o++)
            {
                Vector3 samplePos = (pos + offset) * frequency;
                // Unity's Mathf.PerlinNoise is 2D, so we sample two planes and combine
                float nx = Mathf.PerlinNoise(samplePos.x, samplePos.y);
                float ny = Mathf.PerlinNoise(samplePos.y, samplePos.z);
                float nz = Mathf.PerlinNoise(samplePos.z, samplePos.x);
                float noise = (nx + ny + nz) / 3f; // average to approximate 3D noise

                total += (noise - 0.5f) * 2f * amplitude; // remap to [-1, 1]
                maxAmplitude += amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }

            return (total / maxAmplitude) * strength;
        }

        // Calculates rest density from the most central particle (same approach as XPBDSim).
        private float CalcRestDensity(ParticleSet ps, SpatialHash grid, float h)
        {
            // Find the particle closest to the center of mass
            Vector3 center = Vector3.zero;
            for (int i = 0; i < ps.count; i++)
                center += ps.currentPosition[i];
            center /= ps.count;

            int centralIdx = 0;
            float minDist = float.MaxValue;
            for (int i = 0; i < ps.count; i++)
            {
                float d = (ps.currentPosition[i] - center).sqrMagnitude;
                if (d < minDist) { minDist = d; centralIdx = i; }
            }

            // Calculate density at the central particle
            float density = 0f;
            int[] neighbors;
            int nCount;
            (neighbors, nCount) = grid.GetNeighbors(ps, centralIdx, h);

            for (int n = 0; n < nCount; n++)
            {
                int j = neighbors[n];
                if (ps.invMass[j] <= 0) continue;
                float mj = 1f / ps.invMass[j];
                Vector3 distVec = ps.currentPosition[centralIdx] - ps.currentPosition[j];
                density += mj * Poly6(distVec.sqrMagnitude, h);
            }

            return density;
        }

        //Poly6 smoothing kernel (same as XPBDSim)
        private static float Poly6(float r2, float h)
        {
            float h2 = h * h;
            if (h2 < r2) return 0;
            float h4 = h2 * h2;
            float term = h2 - r2;
            return 315f / (64f * Mathf.PI * h4 * h4 * h) * term * term * term;
        }
    }
}
