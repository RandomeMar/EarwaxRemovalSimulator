using System.ComponentModel;
using UnityEngine;

namespace EarwaxSim
{
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

        public MaterialProperties matProps;

        public Collider unityCollider;
        protected CollisionShape shape;

        public CollisionInfo GetCollisionInfo(Vector3 particlePos)
        {
            Vector3 pLocal = this.transform.InverseTransformPoint(particlePos); // Convert to local space
            CollisionInfo localHit = this.shape.GetCollisionInfo(pLocal); // Get collision info from this.shape
            localHit.collNormal = this.transform.TransformDirection(localHit.collNormal); // Convert to world space
            return localHit;
        }

        public float GetSignedDistance(Vector3 particlePos)
        {
            Vector3 pLocal = this.transform.InverseTransformPoint(particlePos); // Convert to local space
            return this.shape.GetSignedDistance(pLocal); // Get signed distance from this.shape
        }

        protected virtual void Awake()
        {
            this.previousPosition = this.transform.position;
            this.previousRotation = this.transform.rotation;
            this.targetPosition = this.transform.position;
            this.targetRotation = this.transform.rotation;

            this.matProps = BuildMatProps();

            shape = this.BuildShapeTree();
            shape.RecurseSetup(this, null);

            unityCollider = this.GetComponent<MeshCollider>();
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

        protected abstract CollisionShape BuildShapeTree();

    }
}


