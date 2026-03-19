using UnityEngine;

public class RobotDataController:MonoBehaviour
{
    [Header("Car Data")]
    public float speed = 0f;           // km/h
    public float direction = 0f;       // degrees (-180 to 180)
    public float batteryLevel = 87f;   // percentage

    [Header("GPS")]
    public float latitude = 10.3910f;  // Cartagena, Colombia 
    public float longitude = -75.4794f;

    [Header("Arm")]
    public float joint1Angle = 0f;
    public float joint2Angle = 0f;
    public float joint3Angle = 0f;
    public float gripperOpen = 1f;     // 0 = closed, 1 = open

    void Update()
    {
        // Simulate data moving so we can test the UI
        speed = Mathf.Abs(Mathf.Sin(Time.time) * 25f);
        direction = Mathf.Sin(Time.time * 0.5f) * 180f;
        batteryLevel = Mathf.Clamp(batteryLevel - Time.deltaTime * 0.5f, 0, 100);
        joint1Angle = Mathf.Sin(Time.time) * 90f;
        joint2Angle = Mathf.Cos(Time.time) * 45f;
    }
}
