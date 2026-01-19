using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class turnSwitcher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ControllerInputActionManager rightController;

    [Header("Input")]
    [SerializeField] private InputActionReference toggleInput;

    private void OnEnable()
    {
        if (toggleInput != null && toggleInput.action != null)
        {
            toggleInput.action.Enable();
            toggleInput.action.performed += OnToggle;
        }
    }

    private void OnDisable()
    {
        if (toggleInput != null && toggleInput.action != null)
        {
            toggleInput.action.performed -= OnToggle;
        }
    }

    private void OnToggle(InputAction.CallbackContext context)
    {
        bool newValue = true;

        if (rightController != null)
        {
            newValue = !rightController.smoothTurnEnabled;
            rightController.smoothTurnEnabled = newValue;
        }

        Debug.Log($"Smooth Turn: {(newValue ? "ON" : "OFF")}");
    }
}