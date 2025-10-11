using UnityEngine;
using UnityEngine.InputSystem;

public class AnimateHandOnInput : MonoBehaviour
{
    public InputActionProperty trigerValue;
    public InputActionProperty gripValue;

    public Animator handAnimator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float triger = trigerValue.action.ReadValue<float>();
        float grip = gripValue.action.ReadValue<float>();

        handAnimator.SetFloat("Trigger", triger);
        handAnimator.SetFloat("Grip", grip); 
    }
}
