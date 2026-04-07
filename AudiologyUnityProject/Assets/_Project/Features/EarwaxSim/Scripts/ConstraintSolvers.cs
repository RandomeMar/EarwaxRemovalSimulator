using System;
using System.Collections.Generic;
using UnityEngine;


namespace EarwaxSim
{
    // Interface for all constraint solvers
    interface IConstraintSolver
    {
        void ResetLambda();
        void SolveOnce(ParticleSet ps, float dt, SpatialHash grid);
    }

    // Single distance constraint between two particles
    public struct DistanceConstraint
    {
        public int i, j;
        public float targetRestLength;
        public float restLength;
        public float compliance;
        public float lambda;
        public bool active;

        public DistanceConstraint(int i, int j, float restLength, float compliance)
        {
            this.i = i;
            this.j = j;
            this.targetRestLength = restLength;
            this.restLength = restLength;
            this.compliance = compliance;
            this.lambda = 0f;

            this.active = true;
        }
    }

    // Solves distance constraints
    public class DistanceConstraintSet : IConstraintSolver
    {
        public DistanceConstraint[] constraints;

        // For plastic deformation
        public float yieldStrain;
        public float plasticFlow;
        public float breakStrain;

        // For visco-elasticity
        public float adaptRate;
        public float recRate;

        public DistanceConstraintSet(DistanceConstraint[] constraints, float yieldStrain, float plasticFlow, float breakStrain, float adaptRate, float recRate)
        {
            this.constraints = constraints;
            this.yieldStrain = yieldStrain;
            this.plasticFlow = plasticFlow;
            this.breakStrain = breakStrain;

            this.adaptRate = adaptRate;
            this.recRate = recRate;
        }

        public void ResetLambda()
        {
            for (int i = 0; i < this.constraints.Length; i++)
            {
                this.constraints[i].lambda = 0;
            }
        }

        public void SolveOnce(ParticleSet ps, float dt, SpatialHash grid)
        {
            for (int index = 0; index < this.constraints.Length; index++)
            {
                DistanceConstraint constraint = this.constraints[index];

                if (!constraint.active) continue;

                int i = constraint.i;
                int j = constraint.j;
                Vector3 d = ps.currentPosition[i] - ps.currentPosition[j]; // Vector from j to i
                float l = d.magnitude; // Distance between i and j
                float c = l - constraint.restLength; // C

                // if l is close to 0: continue
                if (l <= Constants.EPS) { continue; }

                Vector3 n = d / l; // Normalized direction from j to i

                float alpha = constraint.compliance / (dt * dt);

                // If the denominator is close to 0: continue
                float denom = (ps.invMass[i] + ps.invMass[j] + alpha);
                if (denom <= Constants.EPS) { continue; }

                float deltaLambda = (-c - alpha * constraint.lambda) / denom;

                constraint.lambda += deltaLambda;
                this.constraints[index] = constraint;

                ps.currentPosition[i] += ps.invMass[i] * n * deltaLambda;
                ps.currentPosition[j] -= ps.invMass[j] * n * deltaLambda;
            }
        }

        public void UpdateRestLengths(ParticleSet ps, float dt)
        {
            for (int index = 0; index < this.constraints.Length; index++)
            {
                DistanceConstraint constraint = this.constraints[index];

                if (!constraint.active) continue;
                if (constraint.targetRestLength <= Constants.EPS) continue;

                float currLength = Vector3.Distance(ps.currentPosition[constraint.i], ps.currentPosition[constraint.j]);

                // Makes rest length closer to the current length of the constraint
                constraint.restLength += this.adaptRate * (currLength - constraint.restLength) * dt;

                // Makes rest length closer to the original rest length of the constraint
                constraint.restLength += this.recRate * (constraint.targetRestLength - constraint.restLength) * dt;

                float strain = (currLength - constraint.targetRestLength) / constraint.targetRestLength;

                // Break constraint if strain is too large
                if (strain >= breakStrain)
                {
                    constraint.active = false;
                    this.constraints[index] = constraint;
                    continue;
                }

                if (Mathf.Abs(strain) > this.yieldStrain)
                {
                    float deltaRestLen = this.plasticFlow * (Mathf.Abs(strain) - this.yieldStrain) * Mathf.Sign(strain) * constraint.targetRestLength * dt;
                    constraint.targetRestLength += deltaRestLen;
                }

                this.constraints[index] = constraint;
            }
        }
    }

    // Prevents particles from getting too close together
    public class DensityConstraintSolver : IConstraintSolver
    {
        public float restDensity;
        public float h;
        public float compliance;

        private float[] lambda;
        private Vector3[] deltaX; // Used for Jacobi structure
        private Vector3[] gradBuffer;

        // Cached values for better performance
        private float h2;
        private float poly6Coef;
        private float poly6GradCoef;

        public DensityConstraintSolver(float restDensity, float h, float compliance)
        {
            if (h <= Constants.EPS) throw new ArgumentException("float h must be greater than 0");

            this.restDensity = restDensity;
            this.h = h;
            this.h2 = h * h;
            this.compliance = compliance;
            this.poly6Coef = (float)(315f / (64f * Mathf.PI * Mathf.Pow(h, 9)));
            this.poly6GradCoef = (float)(945f / (32f * Mathf.PI * Mathf.Pow(h, 9)));
        }

        private void EnsureCapacity(int count)
        {
            if (this.deltaX == null || this.deltaX.Length != count)
                this.deltaX = new Vector3[count];

            if (this.lambda == null || this.lambda.Length != count)
                this.lambda = new float[count];

            if (this.gradBuffer == null || this.gradBuffer.Length < Constants.MAX_NEIGHBORS)
                this.gradBuffer = new Vector3[Constants.MAX_NEIGHBORS];
        }

        public void ResetLambda()
        {
            if (this.lambda == null) return;
            Array.Clear(this.lambda, 0, this.lambda.Length);
        }

        public void SolveOnce(ParticleSet ps, float dt, SpatialHash grid)
        {
            EnsureCapacity(ps.count);
            Array.Clear(this.deltaX, 0, ps.count);

            float alpha = this.compliance / (dt * dt);

            for (int i = 0; i < ps.count; i++)
            {
                if (ps.invMass[i] <= 0) continue;

                int[] js;
                int jCount;

                (js, jCount) = grid.GetNeighbors(ps, i, this.h);

                float density = 0f;
                Vector3 gradi = Vector3.zero;
                float denom = 0f;

                for (int iter = 0; iter < jCount; iter++)
                {
                    int j = js[iter];

                    if (ps.invMass[j] <= 0) continue;
                    float mj = ps.mass[j];

                    Vector3 distVec = ps.currentPosition[i] - ps.currentPosition[j];
                    float term = (this.h2 - distVec.sqrMagnitude);

                    density += mj * this.poly6Coef * term * term * term; // Poly-6 kernel smoothing function
                    this.gradBuffer[iter] = mj * this.poly6GradCoef * term * term * distVec; // Gradient of Poly-6

                    gradi -= this.gradBuffer[iter];
                    denom += this.gradBuffer[iter].sqrMagnitude * ps.invMass[j];
                }

                float c = Mathf.Max(density / this.restDensity - 1f, 0f); // This makes it so density only pushes, no pulling
                denom += gradi.sqrMagnitude * ps.invMass[i];

                // If the denominator is close to 0: continue
                if (denom + alpha <= Constants.EPS) continue;

                float deltaLambda = (-c - alpha * this.lambda[i]) / (denom + alpha);
                this.lambda[i] += deltaLambda;

                this.deltaX[i] += ps.invMass[i] * gradi * deltaLambda;

                for (int iter = 0; iter < jCount; iter++)
                {
                    int j = js[iter];

                    if (ps.invMass[j] <= 0) continue;

                    this.deltaX[j] += ps.invMass[j] * this.gradBuffer[iter] * deltaLambda;
                }

            }

            // Jacobi structure
            for (int i = 0; i < ps.count; i++)
            {
                ps.currentPosition[i] += deltaX[i];
            }

        }
    }

    // Solves collisions between particles and collision shapes
    public class CollisionConstraintSolver : IConstraintSolver
    {
        // Will be replaced later
        public List<CollisionObjectBase> objects;

        // TODO: Replace objects with these two fields
        public DynamicCollisionObject tool;
        public CollisionObjectBase canal;

        public float compliance;

        public AdhesionConstraint[] adhesConstraints; // For adhesion constraint

        public CollisionConstraintSolver(float compliance, AdhesionConstraint[] adhesConstraints)
        {
            this.objects = new(1);
            this.compliance = compliance;
            this.adhesConstraints = adhesConstraints;
        }

        public void ResetLambda()
        {

        }
        
        public void SolveOnce(ParticleSet ps, float dt, SpatialHash grid)
        {
            float alpha = this.compliance / (dt * dt);

            // TODO: Implement Tool vs Canal solving here. It should solve before Tool vs Particle.

            // ------ Tool vs. Particles ------
            for (int i = 0; i < ps.count; i++)
            {
                if (ps.invMass[i] == 0) continue;

                foreach (CollisionObjectBase obj in this.objects)
                {
                    // ------ Collision ------
                    CollisionInfo collisionInfo = obj.GetCollisionInfo(ps.currentPosition[i], ps.radius);
                    float c = collisionInfo.signedDistance;
                    Vector3 collNorm = collisionInfo.collNormal;

                    // As long as C is not negative, we assume there is no correction to be made
                    if (c >= 0) continue;

                    float wp = ps.invMass[i];
                    float wo = obj.invMass;

                    // Check if denom is close to 0
                    float denom = (wp + wo + alpha);
                    if (denom <= Constants.EPS) continue;

                    // Calculate delta lambda
                    float deltaLambda = -c / denom;

                    // Calculate normal correction
                    Vector3 pNormalCorrection = wp * collNorm * deltaLambda;
                    Vector3 oNormalCorrection = -wo * collNorm * deltaLambda; // Inverse of particle correction

                    // Update position
                    ps.currentPosition[i] += pNormalCorrection;
                    obj.transform.position += oNormalCorrection;


                    // ------ Adhesion ------

                    // Set adhesion anchor to active and add local position and owner shape to the struct array
                    AdhesionConstraint adhesConst = new(
                        collisionInfo.owner.GetLocalPos(ps.currentPosition[i]),
                        collisionInfo.owner,
                        obj.matProps.adhesCompliance,
                        obj.matProps.adhesBreakDist);

                    this.adhesConstraints[i] = adhesConst;


                    // ------ Friction ------
                    Vector3 dxParticle = ps.currentPosition[i] - ps.previousPosition[i];
                    Vector3 dxObj = obj.transform.position - obj.previousPosition;
                    Vector3 dxRel = dxParticle - dxObj; // Relative change in position

                    Vector3 dxNormal = Vector3.Dot(dxRel, collNorm) * collNorm;
                    Vector3 dxTangent = dxRel - dxNormal;

                    float tangLength = dxTangent.magnitude;

                    if (tangLength > Constants.EPS)
                    {
                        Vector3 normalCorrectionRel = pNormalCorrection - oNormalCorrection;

                        float maxFriction = obj.matProps.dynamicFriction * normalCorrectionRel.magnitude;

                        // Friction cannot cause the particle to move backwards, only resist forward motion
                        Vector3 frictionCorrectionRel = -dxTangent.normalized * Mathf.Min(maxFriction, tangLength);

                        float wSum = wp + wo;

                        // Update position
                        ps.currentPosition[i] += (wp / wSum) * frictionCorrectionRel;
                        obj.transform.position -= (wo / wSum) * frictionCorrectionRel; // Inverse of particle correction
                    }
                }
            }
            return;
        }

        public void NewSolveOnce(ParticleSet ps, float dt)
        {
            float alpha = this.compliance / (dt * dt);

            // Tool vs. Canal
            SolveColliderCollider(alpha);

            // Tool + Canal vs. Particles
            SolvePSCollider(ps, this.tool, alpha);
            SolvePSCollider(ps, this.canal, alpha);
        }

        // Solves particle vs. sdf based collider collisions
        private void SolvePSCollider(ParticleSet ps, CollisionObjectBase obj, float alpha)
        {
            float wo = obj.invMass;
            for (int i = 0; i < ps.count; i++)
            {
                if (ps.invMass[i] == 0) continue;
                // ------ Collision ------
                CollisionInfo collisionInfo = obj.GetCollisionInfo(ps.currentPosition[i], ps.radius);
                float c = collisionInfo.signedDistance;
                Vector3 collNorm = collisionInfo.collNormal;

                // As long as C is not negative, we assume there is no correction to be made
                if (collisionInfo.signedDistance >= 0) continue;

                float wp = ps.invMass[i];

                // Check if denom is close to 0
                float denom = (wp + wo + alpha);
                if (denom <= Constants.EPS) continue;

                // Calculate delta lambda
                float deltaLambda = -c / denom;

                // Calculate normal correction
                Vector3 pNormalCorrection = deltaLambda * wp * collNorm;
                Vector3 oNormalCorrection = -deltaLambda * wo * collNorm; // Inverse of particle correction

                // Update position
                ps.currentPosition[i] += pNormalCorrection;
                obj.transform.position += oNormalCorrection;


                // ------ Adhesion ------

                // Set adhesion anchor to active and add local position and owner shape to the struct array
                AdhesionConstraint adhesConst = new(
                    collisionInfo.owner.GetLocalPos(ps.currentPosition[i]),
                    collisionInfo.owner,
                    obj.matProps.adhesCompliance,
                    obj.matProps.adhesBreakDist);

                this.adhesConstraints[i] = adhesConst;


                // ------ Friction ------
                Vector3 dxParticle = ps.currentPosition[i] - ps.previousPosition[i];
                Vector3 dxObj = obj.transform.position - obj.previousPosition;
                Vector3 dxRel = dxParticle - dxObj; // Relative change in position

                Vector3 dxNormal = Vector3.Dot(dxRel, collNorm) * collNorm;
                Vector3 dxTangent = dxRel - dxNormal;

                float tangLength = dxTangent.magnitude;

                if (tangLength > Constants.EPS)
                {
                    Vector3 normalCorrectionRel = pNormalCorrection - oNormalCorrection;

                    float maxFriction = obj.matProps.dynamicFriction * normalCorrectionRel.magnitude;

                    // Friction cannot cause the particle to move backwards, only resist forward motion
                    Vector3 frictionCorrectionRel = -dxTangent.normalized * Mathf.Min(maxFriction, tangLength);

                    float wSum = wp + wo;

                    // Update position
                    ps.currentPosition[i] += (wp / wSum) * frictionCorrectionRel;
                    obj.transform.position -= (wo / wSum) * frictionCorrectionRel; // Inverse of particle correction
                }
            }
        }

        // Solves unity based collider vs unity based collider collisions
        private void SolveColliderCollider(float alpha)
        {
            if (!Physics.ComputePenetration(
                this.tool.unityCollider,
                this.tool.transform.position,
                this.tool.transform.rotation,
                this.canal.unityCollider,
                this.canal.transform.position,
                this.canal.transform.rotation,
                out Vector3 collNorm,
                out float c)) return;

            float wt = this.tool.invMass;
            float wc = this.canal.invMass;

            // Check if denom is close to 0
            float denom = (wt + wc + alpha);
            if (denom <= Constants.EPS) return;

            // Calculate delta lambda
            float deltaLambda = c / denom;

            // Calculate normal correction
            Vector3 tNormalCorrection = deltaLambda * wt * collNorm;
            Vector3 cNormalCorrection = -deltaLambda * wc * collNorm;

            // Update position
            this.tool.transform.position += tNormalCorrection;
            this.canal.transform.position += cNormalCorrection;
        }
    }

    public struct AdhesionConstraint
    {
        public Vector3 localAnchorPos;
        public bool isActive;
        public CollisionShape shape;
        public float compliance;
        public float breakDist;

        public AdhesionConstraint(Vector3 localAnchorPos, CollisionShape shape, float compliance, float breakDist)
        {
            this.localAnchorPos = localAnchorPos;
            this.isActive = true;
            this.shape = shape;
            this.compliance = compliance;
            this.breakDist = breakDist;
        }

        public Vector3 GetWorldAnchorPos()
        {
            return this.shape.GetWorldPos(this.localAnchorPos);
        }

    }

    public class AdhesionConstraintSolver : IConstraintSolver
    {
        public float[] lambdas;
        public AdhesionConstraint[] constraints;

        public AdhesionConstraintSolver(AdhesionConstraint[] constraints)
        {
            this.constraints = constraints;
        }

        private void EnsureCapacity(int count)
        {
            if (this.constraints == null || this.constraints.Length != count)
                this.constraints = new AdhesionConstraint[count];

            if (this.lambdas == null || this.lambdas.Length != count)
                this.lambdas = new float[count];
        }

        public void ResetLambda()
        {
            if (this.lambdas == null) return;
            Array.Clear(this.lambdas, 0, this.lambdas.Length);
        }

        public void SolveOnce(ParticleSet ps, float dt, SpatialHash grid)
        {
            this.EnsureCapacity(ps.count);

            for (int i = 0; i < ps.count; i++)
            {
                if (ps.invMass[i] <= Constants.EPS) continue;
                if (!constraints[i].isActive) continue;
                
                Vector3 anchorPos = this.constraints[i].GetWorldAnchorPos();

                Vector3 distVec = ps.currentPosition[i] - anchorPos;
                float c = distVec.magnitude;

                if (c > constraints[i].breakDist)
                {
                    this.constraints[i].isActive = false;
                    continue;
                }

                float alpha = constraints[i].compliance / (dt * dt);

                float denom = ps.invMass[i] + alpha;

                float deltaLambda = (-c - alpha * lambdas[i]) / denom;

                this.lambdas[i] += deltaLambda;
                ps.currentPosition[i] += ps.invMass[i] * distVec.normalized * deltaLambda;
            }
        }
    }
}