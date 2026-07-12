using UnityEngine;

public class Lidar : MonoBehaviour
{

    [Header("Lidar Settings")]
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float scanFrequency = 1f; // 10Hz scanning frequency
    [SerializeField] private float scanSpeed = 500f; // 5KHz scanning frequency

    private Vector4[] scanPoints;
    private Vector4[] cycleScanPoints;
    private int cycleScanIndex = 0;

    private float scanResolution; 
    private int pointsPerScan; 
    
    private float currentAngle = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pointsPerScan = (int)(scanSpeed / scanFrequency); 
        scanResolution = 360f / pointsPerScan; 
        scanPoints = new Vector4[(int)scanSpeed/50]; // Store each fixed update scan. 
        cycleScanPoints = new Vector4[pointsPerScan]; // Store a full cycle of scan points.
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        scanPoints = new Vector4[(int)scanSpeed/50];
        if (cycleScanIndex >= pointsPerScan)
        {
            cycleScanIndex = 0; // Reset the index for the next cycle
        }

        for (int i = 0; i < scanSpeed/50; i++) 
        {
            currentAngle += scanResolution;
            Vector3 rotatedDirection = Quaternion.Euler(0, currentAngle, 0) * transform.forward;
            RaycastHit hit;
            if (Physics.Raycast(transform.position, rotatedDirection, out hit, maxDistance))
            {
                scanPoints[i] = new Vector4(hit.point.x, hit.point.y, hit.point.z, 1); // Last value indicates a hit
                cycleScanPoints[cycleScanIndex] = new Vector4(hit.point.x, hit.point.y, hit.point.z, 1);
                cycleScanIndex++;
                //Debug.Log("Hit distance at angle " + currentAngle + ": " + hit.distance);
            }
            else
            {
                scanPoints[i] = new Vector4(0, 0, 0, 0); // Last value indicates no hit
                cycleScanPoints[cycleScanIndex] = new Vector4(0, 0, 0, 0);
                cycleScanIndex++;
            }
        }
    }

    public Vector4[] GetScanPoints()
    {
        return scanPoints;
    }

    public Vector4[] GetCycleScanPoints()
    {
        return cycleScanPoints;
    }

    public float GetMaxDistance()
    {
        return maxDistance;
    }

}
