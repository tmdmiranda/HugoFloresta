using UnityEngine;
using UnityEngine.InputSystem;

public class CarInputHandler : MonoBehaviour
{
    [Header("Input Action Asset")]
    [SerializeField] private InputActionAsset carControls;

    [Header("Action Map Name Reference")]
    [SerializeField] private string actionMapName = "Car";

    [Header("Action Name References")]
    [SerializeField] private string steerActionName = "Rotate";
    [SerializeField] private string accelerateActionName = "Acelarate";
    [SerializeField] private string brakeActionName = "Brake";

    private InputAction steerAction;
    private InputAction accelerateAction;
    private InputAction brakeAction;

    [Header("Steer Smooth")]
    [SerializeField] private float steerSmoothSpeed = 5f;

    private float steerInputRaw;

    public float SteerInput { get; private set; }
    public float AccelerateInput { get; private set; }
    public bool BrakeInput { get; private set; }

    private void Awake()
    {
        InputActionMap mapReference = carControls.FindActionMap(actionMapName);

        steerAction = mapReference.FindAction(steerActionName);
        accelerateAction = mapReference.FindAction(accelerateActionName);
        brakeAction = mapReference.FindAction(brakeActionName);

        SubscribeActionValuesToInputEvents();
    }

    void Update()
    {
        SteerInput = Mathf.Lerp(SteerInput, steerInputRaw, Time.deltaTime * steerSmoothSpeed);
    }

    private void SubscribeActionValuesToInputEvents()
    {
        steerAction.performed += inputInfo => steerInputRaw = inputInfo.ReadValue<float>();
        steerAction.canceled += inputInfo => steerInputRaw = 0f;

        accelerateAction.performed += inputInfo => AccelerateInput = inputInfo.ReadValue<float>();
        accelerateAction.canceled += inputInfo => AccelerateInput = 0f;

        brakeAction.performed += inputInfo => BrakeInput = inputInfo.ReadValue<float>() > 0.1f;
        brakeAction.canceled += inputInfo => BrakeInput = false;
    }

    private void OnEnable()
    {
        carControls.FindActionMap(actionMapName).Enable();
    }

    private void OnDisable()
    {
        carControls.FindActionMap(actionMapName).Disable();
    }
}