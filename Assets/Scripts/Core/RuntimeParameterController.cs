using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RuntimeParameterController : MonoBehaviour
{
    [Header("UI Controls - Physics")]
    public Slider gravitySlider;
    public Slider timeScaleSlider;
    public Toggle airResistanceToggle;
    public Toggle collisionsToggle;
    public Dropdown simulationDropdown;

    [Header("UI Controls - Spawn Parameters")]
    public Slider massSlider;
    public Slider bouncinessSlider;
    public Slider frictionSlider;
    public Slider radiusSlider;

    [Header("UI Controls - Buttons")]
    public Button spawnObjectButton;
    public Button resetButton;

    [Header("Value Texts - Just Values")]
    public Text gravityValueText;
    public Text timeScaleValueText;
    public Text massValueText;
    public Text bouncinessValueText;
    public Text frictionValueText;
    public Text radiusValueText;
    public Text activeObjectsText;
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

        SetupUI();
        UpdateAllUIValues();
    }

    void SetupUI()
    {
        // Setup physics sliders
        if (gravitySlider != null)
        {
            gravitySlider.value = simulationManager.gravity;
            gravitySlider.onValueChanged.AddListener(OnGravityChanged);
        }

        if (timeScaleSlider != null)
        {
            timeScaleSlider.value = simulationManager.timeScale;
            timeScaleSlider.onValueChanged.AddListener(OnTimeScaleChanged);
        }

        // Setup spawn parameter sliders
        if (massSlider != null)
        {
            massSlider.minValue = 0.1f;
            massSlider.maxValue = 10f;
            massSlider.value = simulationManager.GetSpawnMass();
            massSlider.onValueChanged.AddListener(OnMassChanged);
        }

        if (bouncinessSlider != null)
        {
            bouncinessSlider.minValue = 0f;
            bouncinessSlider.maxValue = 1f;
            bouncinessSlider.value = simulationManager.GetSpawnBounciness();
            bouncinessSlider.onValueChanged.AddListener(OnBouncinessChanged);
        }

        if (frictionSlider != null)
        {
            frictionSlider.minValue = 0f;
            frictionSlider.maxValue = 1f;
            frictionSlider.value = simulationManager.GetSpawnFriction();
            frictionSlider.onValueChanged.AddListener(OnFrictionChanged);
        }

        if (radiusSlider != null)
        {
            radiusSlider.minValue = 0.1f;
            radiusSlider.maxValue = 1f;
            radiusSlider.value = simulationManager.GetSpawnRadius();
            radiusSlider.onValueChanged.AddListener(OnRadiusChanged);
        }

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
        if (spawnObjectButton != null)
            spawnObjectButton.onClick.AddListener(OnSpawnObject);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetSimulation);
    }

    void Update()
    {
        UpdateDisplayValues();
    }

    void UpdateDisplayValues()
    {
        // Mettre à jour uniquement les valeurs numériques
        if (gravityValueText != null)
            gravityValueText.text = simulationManager.gravity.ToString("F1");

        if (timeScaleValueText != null)
            timeScaleValueText.text = simulationManager.timeScale.ToString("F1");

        if (massValueText != null)
            massValueText.text = simulationManager.GetSpawnMass().ToString("F1");

        if (bouncinessValueText != null)
            bouncinessValueText.text = simulationManager.GetSpawnBounciness().ToString("F2");

        if (frictionValueText != null)
            frictionValueText.text = simulationManager.GetSpawnFriction().ToString("F2");

        if (radiusValueText != null)
            radiusValueText.text = simulationManager.GetSpawnRadius().ToString("F1");

        // Mettre à jour les informations physiques
        if (activeObjectsText != null)
            activeObjectsText.text = simulationManager.GetActiveSimulationCount().ToString();

        if (energyText != null)
        {
            float kineticEnergy = simulationManager.GetTotalKineticEnergy();
            energyText.text = kineticEnergy.ToString("F1");
        }

        if (velocityText != null)
        {
            float avgVelocity = simulationManager.GetAverageVelocity();
            velocityText.text = avgVelocity.ToString("F1");
        }
    }

    void UpdateAllUIValues()
    {
        // Force la mise à jour de tous les sliders
        if (massSlider != null) massSlider.value = simulationManager.GetSpawnMass();
        if (bouncinessSlider != null) bouncinessSlider.value = simulationManager.GetSpawnBounciness();
        if (frictionSlider != null) frictionSlider.value = simulationManager.GetSpawnFriction();
        if (radiusSlider != null) radiusSlider.value = simulationManager.GetSpawnRadius();
    }

    public void OnGravityChanged(float value)
    {
        simulationManager.SetGravity(value);
    }

    public void OnTimeScaleChanged(float value)
    {
        simulationManager.timeScale = value;
        Time.timeScale = value;
    }

    public void OnMassChanged(float value)
    {
        simulationManager.SetSpawnMass(value);
    }

    public void OnBouncinessChanged(float value)
    {
        simulationManager.SetSpawnBounciness(value);
    }

    public void OnFrictionChanged(float value)
    {
        simulationManager.SetSpawnFriction(value);
    }

    public void OnRadiusChanged(float value)
    {
        simulationManager.SetSpawnRadius(value);
    }

    public void OnAirResistanceChanged(bool enabled)
    {
        simulationManager.ToggleAirResistance(enabled);
    }

    public void OnCollisionsChanged(bool enabled)
    {
        simulationManager.ToggleCollisions(enabled);
    }

    public void OnSimulationChanged(int index)
    {
        simulationManager.SetSimulation((SimulationType)index);
        UpdateAllUIValues(); // Met à jour l'UI avec les nouvelles valeurs par défaut
    }

    public void OnSpawnObject()
    {
        simulationManager.SpawnObject();
    }

    public void OnResetSimulation()
    {
        simulationManager.ResetSimulation();
        UpdateAllUIValues(); // Met à jour l'UI après le reset
    }
}