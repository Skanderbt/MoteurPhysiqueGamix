using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SimulationUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider gravitySlider;
    public Slider timeScaleSlider;
    public Slider massSlider;
    public Slider bouncinessSlider;
    public Slider frictionSlider;
    public Slider radiusSlider;
    public Toggle airResistanceToggle;
    public Toggle collisionsToggle;
    public Dropdown simulationDropdown;
    public Button spawnButton;
    public Button resetButton;
    public Text statusText;

    [Header("Value Displays")]
    public Text gravityValueText;
    public Text timeScaleValueText;
    public Text massValueText;
    public Text bouncinessValueText;
    public Text frictionValueText;
    public Text radiusValueText;
    public Text objectCountText;
    public Text energyText;
    public Text velocityText;

    private PhysicsSimulationManager simulationManager;

    void Start()
    {
        simulationManager = FindAnyObjectByType<PhysicsSimulationManager>();
        if (simulationManager == null)
        {
            Debug.LogError("PhysicsSimulationManager not found in scene!");
            return;
        }

        InitializeUI();
    }

    void InitializeUI()
    {
        // Setup all sliders
        SetupSlider(gravitySlider, simulationManager.gravity, 0f, 20f, OnGravityChanged);
        SetupSlider(timeScaleSlider, simulationManager.timeScale, 0f, 3f, OnTimeScaleChanged);
        SetupSlider(massSlider, simulationManager.spawnMass, 0.1f, 10f, OnMassChanged);
        SetupSlider(bouncinessSlider, simulationManager.spawnBounciness, 0f, 1f, OnBouncinessChanged);
        SetupSlider(frictionSlider, simulationManager.spawnFriction, 0f, 1f, OnFrictionChanged);
        SetupSlider(radiusSlider, simulationManager.spawnRadius, 0.1f, 1f, OnRadiusChanged);

        // Setup toggles
        if (airResistanceToggle != null)
        {
            airResistanceToggle.isOn = simulationManager.enableAirResistance;
            airResistanceToggle.onValueChanged.AddListener(OnAirResistanceChanged);
        }

        if (collisionsToggle != null)
        {
            collisionsToggle.isOn = simulationManager.enableCollisions;
            collisionsToggle.onValueChanged.AddListener(OnCollisionsChanged);
        }

        // Setup dropdown
        if (simulationDropdown != null)
        {
            simulationDropdown.ClearOptions();
            simulationDropdown.AddOptions(new List<string>
            {
                "Free Fall",
                "Rigid Body 3D",
                "Sphere Bounce",
                "Rubik Cube"
            });
            simulationDropdown.value = (int)simulationManager.currentSimulation;
            simulationDropdown.onValueChanged.AddListener(OnSimulationChanged);
        }

        // Setup buttons
        if (spawnButton != null)
            spawnButton.onClick.AddListener(OnSpawnObject);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetSimulation);
    }

    void SetupSlider(Slider slider, float value, float min, float max, UnityEngine.Events.UnityAction<float> action)
    {
        if (slider != null)
        {
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(action);
        }
    }

    void Update()
    {
        UpdateDisplayValues();
    }

    void UpdateDisplayValues()
    {
        // Update all value displays
        UpdateTextDisplay(gravityValueText, gravitySlider, " m/s²");
        UpdateTextDisplay(timeScaleValueText, timeScaleSlider, "x");
        UpdateTextDisplay(massValueText, massSlider, " kg");
        UpdateTextDisplay(bouncinessValueText, bouncinessSlider, "");
        UpdateTextDisplay(frictionValueText, frictionSlider, "");
        UpdateTextDisplay(radiusValueText, radiusSlider, " m");

        // Update physics info
        if (objectCountText != null && simulationManager != null)
            objectCountText.text = "Objects: " + simulationManager.GetActiveSimulationCount();

        if (energyText != null && simulationManager != null)
        {
            float kineticEnergy = simulationManager.GetTotalKineticEnergy();
            float potentialEnergy = simulationManager.GetTotalPotentialEnergy();
            energyText.text = $"Energy: K={kineticEnergy:F1}J, P={potentialEnergy:F1}J";
        }

        if (velocityText != null && simulationManager != null)
        {
            float avgVelocity = simulationManager.GetAverageVelocity();
            velocityText.text = $"Avg Velocity: {avgVelocity:F1} m/s";
        }

        if (statusText != null && simulationManager != null)
        {
            statusText.text = $"Simulation : {simulationManager.currentSimulation} | " +
                            $"Running: {simulationManager.simulationRunning}";
        }
    }

    void UpdateTextDisplay(Text text, Slider slider, string suffix)
    {
        if (text != null && slider != null)
            text.text = slider.value.ToString("F1") + suffix;
    }

    // Event handlers
    public void OnGravityChanged(float value) => simulationManager?.SetGravity(value);
    public void OnTimeScaleChanged(float value) { simulationManager.timeScale = value; Time.timeScale = value; }
    public void OnMassChanged(float value) => simulationManager?.SetSpawnMass(value);
    public void OnBouncinessChanged(float value) => simulationManager?.SetSpawnBounciness(value);
    public void OnFrictionChanged(float value) => simulationManager?.SetSpawnFriction(value);
    public void OnRadiusChanged(float value) => simulationManager?.SetSpawnRadius(value);
    public void OnAirResistanceChanged(bool enabled) => simulationManager?.ToggleAirResistance(enabled);
    public void OnCollisionsChanged(bool enabled) => simulationManager?.ToggleCollisions(enabled);
    public void OnSimulationChanged(int index) => simulationManager?.SetSimulation((SimulationType)index);
    public void OnSpawnObject() => simulationManager?.SpawnObject();
    public void OnResetSimulation() => simulationManager?.SetSimulation(simulationManager.currentSimulation);
    public void OnPauseResume() => simulationManager?.ToggleSimulation();
}