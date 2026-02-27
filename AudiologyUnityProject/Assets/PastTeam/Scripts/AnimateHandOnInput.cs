using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class HandAnimator : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionProperty gripAction;
    [SerializeField] private InputActionProperty triggerAction;

    [Header("Hand Animator")]
    [SerializeField] private Animator handAnimator;

    [Header("Smoothing")]
    [SerializeField] private float smoothSpeed = 5f;

    private float currentGrip;
    private float currentTrigger;

    void OnEnable()
    {
        gripAction.action.Enable();
        triggerAction.action.Enable();
    }

    void OnDisable()
    {
        gripAction.action.Disable();
        triggerAction.action.Disable();
    }

    void Update()
    {
        if (handAnimator == null)
            return;

        float gripValue = gripAction.action.ReadValue<float>();
        float triggerValue = triggerAction.action.ReadValue<float>();

        // Smooth the transitions
        currentGrip = Mathf.Lerp(currentGrip, gripValue, Time.deltaTime * smoothSpeed);
        currentTrigger = Mathf.Lerp(currentTrigger, triggerValue, Time.deltaTime * smoothSpeed);

        handAnimator.SetFloat("Grip", currentGrip);
        handAnimator.SetFloat("Trigger", currentTrigger);

        //Debug.Log($"Grip: {currentGrip}, Trigger: {currentTrigger}");
    }
}
