using Unity.VisualScripting;
using UnityEngine;

public class MotorDriver : MonoBehaviour
{

    [SerializeField] private float l_maxSpeed;
    [SerializeField] private float r_maxSpeed;
    [SerializeField] private float innerDistance;

    private float l_velocity;
    private float r_velocity;

    [SerializeField] private Rigidbody rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    void FixedUpdate()
    {
        float rotationRadius;
        if (l_velocity == r_velocity)
        {
            rotationRadius = -1;
            rb.angularVelocity = transform.up * 0;
            rb.linearVelocity = transform.forward * l_velocity * Time.fixedDeltaTime;
        }
        else if (l_velocity > r_velocity)
        {
            rotationRadius = -innerDistance * r_velocity / (r_velocity - l_velocity);
            rb.angularVelocity = transform.up * (r_velocity / rotationRadius) * Time.fixedDeltaTime;
            rb.linearVelocity = transform.forward * (r_velocity * (1 + innerDistance / (2 * rotationRadius))) * Time.fixedDeltaTime;
        }
        else
        {
            rotationRadius = innerDistance * l_velocity/ (l_velocity - r_velocity);
            rb.angularVelocity = transform.up * (l_velocity / rotationRadius) * Time.fixedDeltaTime;
            rb.linearVelocity = transform.forward * (l_velocity * (1 + innerDistance / (2 * rotationRadius))) * Time.fixedDeltaTime;
        }
        
    }

    public void SetRightSpeed(float magnitude) => r_velocity = r_maxSpeed * magnitude;
    public void SetLeftSpeed(float magnitude) => l_velocity = l_maxSpeed * magnitude;

}
