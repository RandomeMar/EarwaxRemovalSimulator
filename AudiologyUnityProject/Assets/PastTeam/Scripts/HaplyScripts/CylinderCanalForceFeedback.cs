using Haply.Inverse.DeviceControllers;
using Haply.Inverse.DeviceData;
using UnityEngine;

namespace Haply.Samples.Tutorials._2_BasicForceFeedback
{
    public class CylinderForceFeedback : MonoBehaviour
    {
        public Inverse3Controller inverse3;

        [Range(0, 800)]
        public float stiffness = 300f;
        [Range(0, 3)]
        public float damping = 1f;

        private Vector3 _cylinderPosition;
        private float _cylinderRadius;
        private float _cylinderHeight;
        private float _cursorRadius;

        private void SaveSceneData()
        {
            var t = transform;
            _cylinderPosition = t.position;
            _cylinderRadius = t.lossyScale.x / 2f;
            _cylinderHeight = t.lossyScale.y;
            _cursorRadius = inverse3.Cursor.Radius;
        }

        private void Awake()
        {
            inverse3 ??= FindFirstObjectByType<Inverse3Controller>();
            inverse3.Ready.AddListener((device, args) => SaveSceneData());
        }

        private void OnEnable()
        {
            inverse3.DeviceStateChanged += OnDeviceStateChanged;
        }

        private void OnDisable()
        {
            inverse3.DeviceStateChanged -= OnDeviceStateChanged;
            inverse3.Release();
        }

        private Vector3 ForceCalculation(Vector3 cursorPosition, Vector3 cursorVelocity, float cursorRadius)
        {
            Vector3 force = Vector3.zero;

            Vector3 localCursorPos = cursorPosition - _cylinderPosition;
            Vector3 radialVector = new Vector3(localCursorPos.x, 0, localCursorPos.z);
            float radialDistance = radialVector.magnitude;
            float penetrationRadial = radialDistance - (_cylinderRadius + cursorRadius);

            float halfHeight = _cylinderHeight / 2f;
            bool withinHeight = (localCursorPos.y >= -halfHeight) && (localCursorPos.y <= halfHeight);

            if (penetrationRadial < 0 && withinHeight)
            {
                Vector3 normal = radialVector.normalized;
                force += normal * -penetrationRadial * stiffness;
            }

            force -= cursorVelocity * damping;
            return force;
        }

        private void OnDeviceStateChanged(object sender, Inverse3EventArgs args)
        {
            var inverse3 = args.DeviceController;
            var force = ForceCalculation(inverse3.CursorLocalPosition, inverse3.CursorLocalVelocity, _cursorRadius);
            inverse3.SetCursorLocalForce(force);
        }
    }
}
