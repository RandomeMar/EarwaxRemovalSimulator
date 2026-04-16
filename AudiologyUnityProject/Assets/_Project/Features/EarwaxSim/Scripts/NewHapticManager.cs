using EarwaxSim;
using Haply.Inverse.DeviceControllers;
using Haply.Inverse.DeviceData;
using UnityEditor;
using UnityEngine;

public class NewHapticManager : MonoBehaviour
{
    public Inverse3Controller inverse3;
    public CuretteCollisionObject curette;

    [Min(0)]
    public float strength;

    [Min(0)]
    public float MAX_FORCE = 10;

    // SAFETY FLAG: Stops the loop instantly if the object is dying
    private bool _isDestroyed = false;

    private void OnEnable()
    {
        if (inverse3 == null) inverse3 = FindFirstObjectByType<Inverse3Controller>();
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
        if (inverse3 != null)
        {
            // 1. Unsubscribe immediately so memory can be freed
            inverse3.DeviceStateChanged -= OnDeviceStateChanged;

            // 2. FORCE RELEASE (Stop the motors)
            if (inverse3.IsReady)
            {
                inverse3.SetCursorLocalForce(Vector3.zero); // Send one last zero
                inverse3.Release(); // Kill the connection
            }
        }
    }

    void OnDeviceStateChanged(object sender, Inverse3EventArgs args)
    {
        // DEADMAN SWITCH: If the object is deleted, STOP immediately.
        // This prevents "Zombie" scripts from calculating forces in the background.
        if (_isDestroyed || this == null)
            return;

        // Vector from target to the curette
        Vector3 force = curette.transform.position - args.DeviceController.transform.position;

        force *= strength;

        Vector3 totalForce = force.magnitude > MAX_FORCE ? force.normalized * MAX_FORCE : force;

        args.DeviceController.SetCursorLocalForce(totalForce);
    }

}
