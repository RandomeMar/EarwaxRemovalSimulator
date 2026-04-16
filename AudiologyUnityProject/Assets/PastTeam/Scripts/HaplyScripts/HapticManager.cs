using System.Collections.Generic;
using EarwaxSim;
using Haply.Inverse.DeviceControllers;
using Haply.Inverse.DeviceData;
using UnityEngine;

public class HapticManager : MonoBehaviour
{
    public Inverse3Controller inverse3;

    [Header("Mouse Fallback (no Haply device)")]
    [Tooltip("Enable to drive the cursor with mouse instead of the Haply device.")]
    public bool useMouse = false;
    [Tooltip("The cursor Transform used to read position in mouse mode.")]
    public Transform mouseCursor;
    [Tooltip("Simulated cursor radius used in mouse mode (metres).")]
    public float mouseCursorRadius = 0.006f;

    [Header("XPBD Wax Force Feedback")]
    public float waxForceGain = 1f; //Multiplier on worldspace impulse (XPBDSim publishes impulse/dt) before applying to device. Start at 1, tune by feel.

    // SAFETY FLAG: Stops the loop instantly if the object is dying
    private bool _isDestroyed = false;

    // Mouse-mode velocity tracking
    private Vector3 _prevMouseCursorPos;

    private Quaternion _inverse3WorldRot = Quaternion.identity;


    // --- 1. REGISTRATION LISTS ---
    private readonly List<CubeForceFeedback> _cubesReg = new();
    private readonly List<SphereForceFeedback> _spheresReg = new();
    private readonly List<DangerZone> _zonesReg = new();
    private readonly List<CurvedTubeForceFeedback> _tubesReg = new();
    private readonly List<MovingSphere> _movingReg = new();
    private readonly List<PlaneForceFeedback> _planesReg = new();
    private readonly List<CuretteHapticTrigger> _triggersReg = new();
    private readonly List<CylinderForceFeedback> _cylindersReg = new();
    private readonly List<EarForceFeedback> _earReg = new();
    private readonly List<EarCollision> _earCollisionReg = new();
    private readonly List<DynamicCollisionObject> _dynamicToolsReg = new();

    // --- 2. BUFFERS ---
    private const int BUFFER_SIZE = 64;
    private CubeForceFeedback[] _bufCubes = new CubeForceFeedback[BUFFER_SIZE];
    private SphereForceFeedback[] _bufSpheres = new SphereForceFeedback[BUFFER_SIZE];
    private DangerZone[] _bufZones = new DangerZone[BUFFER_SIZE];
    private CurvedTubeForceFeedback[] _bufTubes = new CurvedTubeForceFeedback[BUFFER_SIZE];
    private MovingSphere[] _bufMoving = new MovingSphere[BUFFER_SIZE];
    private PlaneForceFeedback[] _bufPlanes = new PlaneForceFeedback[BUFFER_SIZE];
    private CuretteHapticTrigger[] _bufTriggers = new CuretteHapticTrigger[BUFFER_SIZE];
    private CylinderForceFeedback[] _bufCylinders = new CylinderForceFeedback[BUFFER_SIZE];
    private EarForceFeedback[] _bufEars = new EarForceFeedback[BUFFER_SIZE];
    private EarCollision[] _bufEarCollisions = new EarCollision[BUFFER_SIZE];
    private DynamicCollisionObject[] _bufDynamicTools = new DynamicCollisionObject[BUFFER_SIZE];

    private volatile int _cntCubes, _cntSpheres, _cntZones, _cntTubes, _cntMoving, _cntPlanes, _cntTriggers, _cntCylinders, _cntEars, _cntEarCollisions, _cntDynamicTools;

    private void OnEnable()
    {
        _isDestroyed = false;

        if (useMouse)
        {
            // Mouse mode: no device needed
            if (mouseCursor != null)
                _prevMouseCursorPos = mouseCursor.position;
            return;
        }

        if (inverse3 == null) inverse3 = FindFirstObjectByType<Inverse3Controller>();

        if (inverse3 != null)
        {
            // Unsubscribe first just in case (prevents double-subscription)
            inverse3.DeviceStateChanged -= OnDeviceStateChanged;
            inverse3.DeviceStateChanged += OnDeviceStateChanged;
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

    // --- THE FIX: Aggressive Cleanup Method ---
    private void Cleanup()
    {
        _isDestroyed = true;

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

    // --- REGISTRATION STUBS ---
    public void RegisterCube(CubeForceFeedback x) { if (!_cubesReg.Contains(x)) _cubesReg.Add(x); }
    public void RegisterSphere(SphereForceFeedback x) { if (!_spheresReg.Contains(x)) _spheresReg.Add(x); }
    public void RegisterDangerZone(DangerZone x) { if (!_zonesReg.Contains(x)) _zonesReg.Add(x); }
    public void RegisterTube(CurvedTubeForceFeedback x) { if (!_tubesReg.Contains(x)) _tubesReg.Add(x); }
    public void RegisterMovingSphere(MovingSphere x) { if (!_movingReg.Contains(x)) _movingReg.Add(x); }
    public void RegisterPlane(PlaneForceFeedback x) { if (!_planesReg.Contains(x)) _planesReg.Add(x); }
    public void RegisterCuretteTrigger(CuretteHapticTrigger x) { if (!_triggersReg.Contains(x)) _triggersReg.Add(x); }
    public void RegisterCylinder(CylinderForceFeedback x) { if (!_cylindersReg.Contains(x)) _cylindersReg.Add(x); }
    public void RegisterEar(EarForceFeedback x) { if (!_earReg.Contains(x)) _earReg.Add(x); }
    public void RegisterEarCollision(EarCollision x) { if (!_earCollisionReg.Contains(x)) _earCollisionReg.Add(x); }
    public void RegisterDynamicTool(DynamicCollisionObject x) { if (!_dynamicToolsReg.Contains(x)) _dynamicToolsReg.Add(x); }

    // --- MAIN THREAD ---
    private void Update()
    {
        if (_isDestroyed) return;

        _cntCubes = FillBuffer(_cubesReg, _bufCubes);
        _cntSpheres = FillBuffer(_spheresReg, _bufSpheres);
        _cntZones = FillBuffer(_zonesReg, _bufZones);
        _cntTubes = FillBuffer(_tubesReg, _bufTubes);
        _cntMoving = FillBuffer(_movingReg, _bufMoving);
        _cntPlanes = FillBuffer(_planesReg, _bufPlanes);
        _cntTriggers = FillBuffer(_triggersReg, _bufTriggers);
        _cntCylinders = FillBuffer(_cylindersReg, _bufCylinders);
        _cntEars = FillBuffer(_earReg, _bufEars);
        _cntEarCollisions = FillBuffer(_earCollisionReg, _bufEarCollisions);
        _cntDynamicTools = FillBuffer(_dynamicToolsReg, _bufDynamicTools);

        if (inverse3 != null)
        {
            _inverse3WorldRot = inverse3.transform.rotation;
        }

        if (useMouse && mouseCursor != null)
        {
            Vector3 pos = mouseCursor.position;
            Vector3 vel = (pos - _prevMouseCursorPos) / Time.deltaTime;
            _prevMouseCursorPos = pos;

            RunForceLoop(pos, vel, mouseCursorRadius);
        }
    }

    private int FillBuffer<T>(List<T> sourceList, T[] targetArray) where T : MonoBehaviour
    {
        int count = 0;
        for (int i = 0; i < sourceList.Count; i++)
        {
            var item = sourceList[i];
            if (item != null && item.isActiveAndEnabled)
            {
                if (count < targetArray.Length)
                {
                    targetArray[count] = item;
                    count++;
                }
            }
        }
        return count;
    }

    // --- SHARED FORCE LOOP ---
    // Called from both the haptic thread (device mode) and Update (mouse mode)
    private Vector3 RunForceLoop(Vector3 pos, Vector3 vel, float radius)
    {
        Vector3 totalForce = Vector3.zero;

        for (int i = 0; i < _cntTubes; i++) totalForce += _bufTubes[i].CalculateForce(pos, vel, radius);
        for (int i = 0; i < _cntCylinders; i++) totalForce += _bufCylinders[i].CalculateForce(pos, vel, radius);
        for (int i = 0; i < _cntEars; i++) totalForce += _bufEars[i].CalculateForce(pos, vel, radius);
        for (int i = 0; i < _cntEarCollisions; i++) totalForce += _bufEarCollisions[i].CalculateForce(pos, vel, radius);
        for (int i = 0; i < _cntSpheres; i++) totalForce += _bufSpheres[i].CalculateForce(pos, vel, radius);
        for (int i = 0; i < _cntCubes; i++) totalForce += _bufCubes[i].CalculateForce(pos, vel, radius);
        for (int i = 0; i < _cntMoving; i++) totalForce += _bufMoving[i].CalculateForce(pos, vel, radius);
        for (int i = 0; i < _cntPlanes; i++) totalForce += _bufPlanes[i].CalculateForce(pos, vel, radius);
        for (int i = 0; i < _cntTriggers; i++)
        {
            if (_bufTriggers[i].isTouchingCube) totalForce += _bufTriggers[i].CalculateForce(pos, vel, radius);
        }

        return Vector3.ClampMagnitude(totalForce, 3.0f);
    }

    // --- HAPTIC THREAD ---
    void OnDeviceStateChanged(object sender, Inverse3EventArgs args)
    {
        // DEADMAN SWITCH: If the object is deleted, STOP immediately.
        // This prevents "Zombie" scripts from calculating forces in the background.
        if (_isDestroyed || this == null)
            return;

        Vector3 pos    = args.DeviceController.CursorLocalPosition;
        Vector3 vel    = args.DeviceController.CursorLocalVelocity;
        float   radius = args.DeviceController.Cursor.Radius;

        Vector3 waxForceLocal = Vector3.zero;
        if (_cntDynamicTools > 0)
        {
            Quaternion invRot = Quaternion.Inverse(_inverse3WorldRot);
            for (int i = 0; i < _cntDynamicTools; i++)
            {
                waxForceLocal += invRot * _bufDynamicTools[i].collisionForceWorld;
            }
            waxForceLocal *= waxForceGain;
        }

        Vector3 totalForce = Vector3.ClampMagnitude(RunForceLoop(pos, vel, radius) + waxForceLocal, 3.0f);
        args.DeviceController.SetCursorLocalForce(totalForce);
    }
}