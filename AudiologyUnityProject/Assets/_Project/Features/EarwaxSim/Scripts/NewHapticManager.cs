using EarwaxSim;
using Haply.Inverse.DeviceControllers;
using Haply.Inverse.DeviceData;
using UnityEditor;
using UnityEngine;

public class NewHapticManager : MonoBehaviour
{
    public Inverse3Controller _inverse3;
    public CuretteCollisionObject curette;

    [Min(0)]
    public float strength;

    [Min(0)]
    public float MAX_FORCE = 10;

    // SAFETY FLAG: Stops the loop instantly if the object is dying
    private bool _isDestroyed = false;

    private Quaternion _inverse3WorldRot = Quaternion.identity;

    // Curette Position
    volatile float curetteX;
    volatile float curetteY;
    volatile float curetteZ;

    private void OnEnable()
    {
        _isDestroyed = false;

        if (_inverse3 == null) _inverse3 = GetComponentInChildren<Inverse3Controller>();

        if (_inverse3 != null)
        {
            // Unsubscribe first just in case (prevents double-subscription)
            _inverse3.DeviceStateChanged -= OnDeviceStateChanged;
            _inverse3.DeviceStateChanged += OnDeviceStateChanged;
        }
    }
    private void OnDisable()
    {
        Cleanup();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void OnApplicationQuit()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        _isDestroyed = true;

        if (_inverse3 != null)
        {
            // 1. Unsubscribe immediately so memory can be freed
            _inverse3.DeviceStateChanged -= OnDeviceStateChanged;

            // 2. FORCE RELEASE (Stop the motors)
            if (_inverse3.IsReady)
            {
                _inverse3.SetCursorLocalForce(Vector3.zero); // Send one last zero
                _inverse3.Release(); // Kill the connection
            }
        }
    }


    private void Update()
    {
        if (_inverse3 != null)
        {
            _inverse3WorldRot = _inverse3.transform.rotation;

            curetteX = curette.transform.position.x;
            curetteY = curette.transform.position.y;
            curetteZ = curette.transform.position.z;
        }
    }



    void OnDeviceStateChanged(object sender, Inverse3EventArgs args)
    {
        // DEADMAN SWITCH: If the object is deleted, STOP immediately.
        // This prevents "Zombie" scripts from calculating forces in the background.
        if (_isDestroyed || this == null)
            return;

        var inverse3 = args.DeviceController;

        // Vector from target to the curette
        Vector3 force = new Vector3(curetteX, curetteY, curetteZ) - inverse3.CursorPosition;

        force *= strength;

        Vector3 totalForce = Vector3.ClampMagnitude(force, MAX_FORCE);

        inverse3.SetCursorLocalForce(totalForce);
    }

}
