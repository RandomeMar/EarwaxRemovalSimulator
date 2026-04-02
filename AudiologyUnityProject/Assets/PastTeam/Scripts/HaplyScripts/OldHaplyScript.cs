using System.Collections.Generic;
using Haply.Inverse.DeviceControllers;
using Haply.Inverse.DeviceData;
using UnityEngine;
using System; // Required for DateTime

public class OldHaplyScript : MonoBehaviour
{
    public Inverse3Controller inverse3;

    private readonly List<CubeForceFeedback> cubes = new();
    private readonly List<SphereForceFeedback> spheres = new();
    private readonly List<DangerZone> dangerZones = new();
    private readonly List<CurvedTubeForceFeedback> curvedTubes = new();
    private readonly List<MovingSphere> movingSpheres = new();
    private readonly List<PlaneForceFeedback> planes = new();
    private readonly List<CuretteHapticTrigger> curetteTriggers = new();
    private readonly List<CylinderForceFeedback> cylinders = new();
    private Vector3 previous_force = Vector3.zero;
    private int previous_ms = 0;

    private void OnEnable()
    {
        if (inverse3 == null)
        {
            inverse3 = FindFirstObjectByType<Inverse3Controller>();
        }

        inverse3.DeviceStateChanged += OnDeviceStateChanged;
    }

    private void OnDisable()
    {
        if (inverse3 != null)
            inverse3.DeviceStateChanged -= OnDeviceStateChanged;
    }

    public void RegisterCube(CubeForceFeedback cube)
    {
        if (!cubes.Contains(cube))
            cubes.Add(cube);
    }
    

    public void RegisterSphere(SphereForceFeedback sphere)
    {
        if (!spheres.Contains(sphere))
            spheres.Add(sphere);
    }

    public void RegisterDangerZone(DangerZone zone)
    {
        if (!dangerZones.Contains(zone))
            dangerZones.Add(zone);
    }

    public void RegisterTube(CurvedTubeForceFeedback tube)
    {
        if (!curvedTubes.Contains(tube))
            curvedTubes.Add(tube);
    }

    public void RegisterMovingSphere(MovingSphere sphere)
    {
        if (!movingSpheres.Contains(sphere))
            movingSpheres.Add(sphere);
    }

    public void RegisterPlane(PlaneForceFeedback plane)
    {
        if (!planes.Contains(plane))
            planes.Add(plane);
    }

    public void RegisterCuretteTrigger(CuretteHapticTrigger trigger)
    {
        if (!curetteTriggers.Contains(trigger))
            curetteTriggers.Add(trigger);
    }

    public void RegisterCylinder(CylinderForceFeedback cylinder)
    {
        if (!cylinders.Contains(cylinder))
            cylinders.Add(cylinder);
    }


    void OnDeviceStateChanged(object sender, Inverse3EventArgs args)
    {
        Vector3 pos = args.DeviceController.CursorLocalPosition;
        Vector3 vel = args.DeviceController.CursorLocalVelocity;
        float radius = args.DeviceController.Cursor.Radius;

        //Vector3 totalForce = Vector3.zero;
        float maxDanger = 0f;
        string warningMessage = "";

        previous_ms = DateTime.Now.Millisecond;

        Vector3 cubeForce = Vector3.zero;
        Vector3 sphereForce = Vector3.zero;
        Vector3 zoneForce = Vector3.zero;
        Vector3 tubeForce = Vector3.zero;
        Vector3 movingSphereForce = Vector3.zero;
        Vector3 planeForce = Vector3.zero;
        Vector3 triggerForce = Vector3.zero;
        Vector3 cylinderForce = Vector3.zero;

        const int MAX_FORCE = 3;

        // Need to refactor - calcuations are incorrect
        //foreach (var cube in cubes)
        //{
        //    cubeForce += cube.CalculateForce(pos, vel, radius);

        //    float cubeDanger = cube.NormalizedPenetration();
        //    if (cubeDanger > maxDanger)
        //    {
        //        maxDanger = cubeDanger;
        //        warningMessage = cube.warningMessage;
        //    }
        //}

        foreach (var sphere in spheres)
        {
            sphereForce = sphere.CalculateForce(pos, vel, radius);
        }

        foreach (var zone in dangerZones)
        {
            float penetration = zone.GetPenetrationDepth(pos, radius);
            float zoneDanger = zone.NormalizedPenetration();
            if (zoneDanger > maxDanger)
            {
                maxDanger = zoneDanger;
                warningMessage = zone.warningMessage;
            }
        }

        foreach (var tube in curvedTubes)
        {
            tubeForce = tube.CalculateForce(pos, vel, radius);
        }

        foreach (var sphere in movingSpheres)
        {
            movingSphereForce = sphere.CalculateForce(pos, vel, radius);
        }

        foreach (var plane in planes)
        {
            planeForce = plane.CalculateForce(pos, vel, radius);
        }

        foreach (var trigger in curetteTriggers)
        {
            if (trigger.isTouchingCube)
            {
                triggerForce = trigger.CalculateForce(pos, vel, radius);
            }
        }

        foreach (var cylinder in cylinders)
        {
            cylinderForce = cylinder.CalculateForce(pos, vel, radius);
        }

        cubeForce = Vector3.ClampMagnitude(cubeForce, MAX_FORCE);
        sphereForce = Vector3.ClampMagnitude(sphereForce, MAX_FORCE);
        zoneForce = Vector3.ClampMagnitude(zoneForce, MAX_FORCE);
        tubeForce = Vector3.ClampMagnitude(tubeForce, MAX_FORCE);
        movingSphereForce = Vector3.ClampMagnitude(movingSphereForce, MAX_FORCE / 30); // Higher causes jitter issues
        planeForce = Vector3.ClampMagnitude(planeForce, MAX_FORCE);
        triggerForce = Vector3.ClampMagnitude(triggerForce, MAX_FORCE);
        cylinderForce = Vector3.ClampMagnitude(cylinderForce, MAX_FORCE);

        Vector3 totalForce = cubeForce + sphereForce + zoneForce + tubeForce + movingSphereForce + planeForce + triggerForce + cylinderForce;

        args.DeviceController.SetCursorLocalForce(totalForce);

        DangerOverlayUI.SetIntensity(maxDanger, warningMessage);
    }
}