using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController:MonoBehaviour

{
    [Header("Reference to Data")]
    public RobotDataManager robotData;

    [Header("Speed & Direction")]
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI directionText;
    public RectTransform directionArrow; // a UI arrow image

    [Header("Battery")]
    public Slider batterySlider;
    public TextMeshProUGUI batteryText;
    public Image batteryFill;           // to change color

    [Header("GPS")]
    public TextMeshProUGUI gpsText;

    [Header("Arm Joints")]
    public TextMeshProUGUI joint1Text;
    public TextMeshProUGUI joint2Text;
    public TextMeshProUGUI joint3Text;
    public TextMeshProUGUI gripperText;
    public Slider joint1Slider;
    public Slider joint2Slider;
    public Slider joint3Slider;

    void Update()
    {
        UpdateSpeed();
        UpdateBattery();
        UpdateGPS();
        UpdateArm();
    }

    void UpdateSpeed()
    {
        speedText.text = $"{robotData.speed:F1} km/h";
        directionText.text = $"{robotData.direction:F1}°";

        // Rotate arrow to match direction
        directionArrow.rotation = Quaternion.Euler(0, 0, -robotData.direction);
    }

    void UpdateBattery()
    {
        batterySlider.value = robotData.batteryLevel / 100f;
        batteryText.text = $"{robotData.batteryLevel:F0}%";

        // Color: green > yellow > red
        if (robotData.batteryLevel > 50f)
            batteryFill.color = Color.green;
        else if (robotData.batteryLevel > 20f)
            batteryFill.color = Color.yellow;
        else
            batteryFill.color = Color.red;
    }

    void UpdateGPS()
    {
        gpsText.text = $"LAT: {robotData.latitude:F4}\nLON: {robotData.longitude:F4}";
    }

    void UpdateArm()
    {
        // Text readouts
        joint1Text.text = $"J1: {robotData.joint1Angle:F1}°";
        joint2Text.text = $"J2: {robotData.joint2Angle:F1}°";
        joint3Text.text = $"J3: {robotData.joint3Angle:F1}°";
        gripperText.text = robotData.gripperOpen > 0.5f ? "GRIPPER: OPEN" : "GRIPPER: CLOSED";

        // Sliders (-90 to 90 range mapped to 0-1)
        joint1Slider.value = (robotData.joint1Angle + 90f) / 180f;
        joint2Slider.value = (robotData.joint2Angle + 90f) / 180f;
        joint3Slider.value = (robotData.joint3Angle + 90f) / 180f;
    }
}
