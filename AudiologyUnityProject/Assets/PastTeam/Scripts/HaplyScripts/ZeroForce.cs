using UnityEngine;
using Haply.Inverse.DeviceControllers; // <-- REQUIRED for Inverse3EventArgs
using Haply.Inverse.DeviceData;        // <-- REQUIRED for data types

public class ZeroForce : MonoBehaviour
{
    // Changed 'Inverse3' to 'Inverse3Controller' to match your actual project scripts
    private Inverse3Controller _inverse3;

    private void Awake()
    {
        _inverse3 = GetComponentInChildren<Inverse3Controller>();
    }

    protected void OnEnable()
    {
        if (_inverse3 != null)
            _inverse3.DeviceStateChanged += OnDeviceStateChanged;
    }

    protected void OnDisable()
    {
        if (_inverse3 != null)
        {
            _inverse3.DeviceStateChanged -= OnDeviceStateChanged;
            // Optional: Release control when disabled
            _inverse3.Release(); 
        }
    }

    // The signature must match: (object, Inverse3EventArgs)
    private void OnDeviceStateChanged(object sender, Inverse3EventArgs args)
    {
        // SEND ZERO FORCE
        // This overrides gravity compensation and damping.
        if (args.DeviceController.IsReady)
        {
            args.DeviceController.SetCursorLocalForce(Vector3.zero);
        }
    }
}