using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace EarwaxSim
{
    /// <summary>
    /// Game object that interacts with the XPBD sim.
    /// </summary>
    public abstract class CollisionObjectBase : MonoBehaviour
    {
        #region Public Parameters
        [Header("Material Properties")]
        public float invMass;

        public float dynamicFriction;
        public float adhesCompliance;
        public float adhesBreakDist;
        #endregion

        [ReadOnly(true)] public Vector3 previousPosition;
        [ReadOnly(true)] public Quaternion previousRotation;

        [ReadOnly(true)] public Vector3 targetPosition;
        [ReadOnly(true)] public Quaternion targetRotation;

        [ReadOnly(true)] public Vector3 velocity;

        public MaterialProperties matProps;

        public List<Collider> unityColliders = new List<Collider>(2);
        protected CollisionShape shape;


        /// <summary>
        /// Returns collision info from a particle vs. collider collision.
        /// </summary>
        /// <param name="particlePos">World space particle position.</param>
        /// <param name="particleRadius">Radius of particle interacting with this collision object.</param>
        /// <returns>Collision info about the particle and this collision object.</returns>
        public CollisionInfo GetCollisionInfo(Vector3 particlePos, float particleRadius)
        {
            Vector3 pLocal = this.transform.InverseTransformPoint(particlePos); // Convert to local space
            CollisionInfo localHit = this.shape.GetCollisionInfoPoint(pLocal); // Get collision info from this.shape

            float s = this.transform.lossyScale.x; // For scaling signed distance

            localHit.collNormal = this.transform.TransformDirection(localHit.collNormal); // Convert to world space
            localHit.signedDistance = localHit.signedDistance * s - particleRadius; // Convert to world space
            return localHit;
        }

        /// <summary>
        /// Returns signed distance from a particle vs. collider collision.
        /// </summary>
        /// <param name="particlePos">World space particle position.</param>
        /// <param name="particleRadius">Radius of particle interacting with this collision object.</param>
        /// <returns>Signed distance from particle to the surface of this collision object.</returns>
        public float GetSignedDistance(Vector3 particlePos, float particleRadius)
        {
            Vector3 pLocal = this.transform.InverseTransformPoint(particlePos); // Convert to local space
            float sdLocal = this.shape.GetSignedDistancePoint(pLocal); // Get local signed distance from this.shape
            float sdWorld = sdLocal * this.transform.lossyScale.x; // Scale signed distance by transform

            return sdLocal * this.transform.lossyScale.x - particleRadius; // Particle offset
        }

        /// <summary>
        /// Initializes kinematic properties, initializes material properties, and builds CollisionShape tree.
        /// </summary>
        protected virtual void Awake()
        {
            this.previousPosition = this.transform.position;
            this.previousRotation = this.transform.rotation;
            this.targetPosition = this.transform.position;
            this.targetRotation = this.transform.rotation;
            this.velocity = Vector3.zero;

            this.matProps = BuildMatProps();

            shape = this.BuildShapeTree();
            shape.RecurseSetup(this, null);
        }

        protected MaterialProperties BuildMatProps()
        {
            return new MaterialProperties
            {
                dynamicFriction = this.dynamicFriction,
                adhesCompliance = this.adhesCompliance,
                adhesBreakDist = this.adhesBreakDist
            };
        }

        /// <summary>
        /// Builds SDF based CollisionShapeTree.
        /// </summary>
        /// <returns>Root node of CollisionShapeTree.</returns>
        protected abstract CollisionShape BuildShapeTree();
    }
}


