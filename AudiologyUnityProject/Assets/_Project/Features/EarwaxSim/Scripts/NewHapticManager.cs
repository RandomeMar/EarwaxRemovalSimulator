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
    [SerializeField] private float stiffness = 1f;

    [Min(0)]
    [SerializeField] private float damping = 1f;

    [Min(0)]
    public float MAX_FORCE = 10;

    [Min(0)]
    public float minPenetration = .01f;

    // SAFETY FLAG: Stops the loop instantly if the object is dying
    private bool _isDestroyed = false;

    private HapticMessage _hapticMessage;
    public readonly object _hapticLock = new object();

    private StatsManager _statsManager;

    
    // Thread-safe setter called by XPBDSim
    public void SetHapticMessage(HapticMessage value)
    {
        lock (_hapticLock)
        {
            _hapticMessage = value;
        }
    }

    // Thread-safe getter called by NewHapticManager
    private HapticMessage GetHapticMessage()
    {
        lock (_hapticLock)
        {
            return _hapticMessage;
        }
    }



    private void OnEnable()
    {
        _isDestroyed = false;

        if (_statsManager == null)
        {
            _statsManager = FindFirstObjectByType<StatsManager>();
        }

        if (_inverse3 == null) _inverse3 = GetComponentInChildren<Inverse3Controller>();

        if (_inverse3 != null)
        {
            // Unsubscribe first just in case (prevents double-subscription)
            _inverse3.DeviceStateChanged -= OnDeviceStateChanged;
            _inverse3.DeviceStateChanged += OnDeviceStateChanged;
        }

        // Initialize haptic message to 0 values
        _hapticMessage = new(
            false,
            Vector3.zero,
            0f,
            Vector3.zero,
            Vector3.zero);
    }

    private void OnDisable() { Cleanup(); }
    private void OnDestroy() {  Cleanup(); }
    private void OnApplicationQuit() {  Cleanup(); }

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


    private Vector3 CalculateForce(HapticMessage msg, Vector3 cursorPos, Vector3 cursorVel)
    {
        if (!msg.isContact) return Vector3.zero;

        //float relVelNorm = Vector3.Dot(msg.toolVelocity - cursorVel, msg.collisionNorm); // Get relative velocity in the normal direction

        // Based on F = (k * d -b * Vn) * collisionNormal
        //Vector3 force = (this.stiffness * msg.penetrationDepth - this.damping * relVelNorm) * msg.collisionNorm;

        Vector3 force = msg.collisionNorm * msg.penetrationDepth * stiffness - cursorVel * damping;
        
        if (force.magnitude >= MAX_FORCE && _statsManager != null && !_statsManager.IsDisqualified())
            _statsManager.Disqualify();
        return force;
    }

    void OnDeviceStateChanged(object sender, Inverse3EventArgs args)
    {
        // DEADMAN SWITCH: If the object is deleted, STOP immediately.
        // This prevents "Zombie" scripts from calculating forces in the background.
        if (_isDestroyed || this == null)
            return;


        var inverse3 = args.DeviceController;

        // Get haptic message sent from XPBDSim
        HapticMessage msg = this.GetHapticMessage();


        Vector3 force = this.CalculateForce(msg, inverse3.CursorPosition, inverse3.CursorVelocity);
        inverse3.SetCursorLocalForce(Vector3.ClampMagnitude(force, MAX_FORCE));
    }

}
