using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;
using UnityEngine.InputSystem.Controls;


public class Navigation : MonoBehaviour
{
    
    [Header("Modules")]
    [SerializeField] private MotorDriver motorDriver;
    [SerializeField] private Lidar lidar;
    [SerializeField] private LidarPointCloud lidarPointCloud;

    [Header("Mapping Properties")]
    [SerializeField] private float cellSize;
    [SerializeField] private Vector2 botDimentions; // For marking unreachable points
    private Dictionary<Vector2, cellStatus> hitCells = new Dictionary<Vector2, cellStatus>(100000) {}; // Cell position is top left corner

    [Header("Navigation")]
    [SerializeField] private Transform targetTransform;
    private Vector2 target;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(MainLoop());
    }

    IEnumerator MainLoop()
    {
        while (true)
        {

            FollowTarget(1f, 0.25f);

            //yield return new WaitForSeconds(1f);
            yield return null;
        }
    }

    void Update()
    {
        target = new Vector2(targetTransform.position.x - transform.position.x, targetTransform.position.z - transform.position.z);
    }

    void LateUpdate()
    {
        Vector4[] cycleScanPoints = lidar.GetCycleScanPoints();
        lidarPointCloud.UpdatePointCloud(cycleScanPoints);
        foreach (Vector4 point in cycleScanPoints)
        {
            if (point.w == 1) 
            {
                //Debug.DrawRay(lidar.transform.position, new Vector3(point.x, point.y, point.z) - lidar.transform.position, Color.red); // Draw rays for each hit point
                SetHitCell(new Vector2(point.x, point.z));
            }
        }
    }

    /// --------------------------------------------------
    /// Mapping Functions
    /// --------------------------------------------------
    void SetHitCell(Vector2 hitPos)
    {
        Vector2 hitVec = new Vector2(Mathf.Floor(hitPos.x / cellSize) * cellSize, Mathf.Ceil(hitPos.y / cellSize) * cellSize);

        if (hitCells.ContainsKey(hitVec))
            return;

        hitCells[hitVec] = cellStatus.Hit;

        // // mark radius as unreachable
        // float r = Mathf.Max(botDimentions.x, botDimentions.y);

        // for (float x = hitPos.x - r; x < hitPos.x + r; x += cellSize)
        // {
        //     for (float y = hitPos.y - r; y < hitPos.y + r; y += cellSize)
        //     {
        //         Vector2 vec = new Vector2(x, y);
        //         if (!hitCells.ContainsKey(vec)) // TODO: this would not allow for remapping, should make it a little more dynamic
        //             hitCells[new Vector2(x, y)] = cellStatus.Unreachable;
        //     }
        // }
    }

    public cellStatus GetCellStatus(Vector2 pos)
    {
        return hitCells[new Vector2(Mathf.Floor(pos.x / cellSize) * cellSize, Mathf.Ceil(pos.y / cellSize) * cellSize)];
    }

    public Dictionary<Vector2, cellStatus> GetHitCells()
    {
        return hitCells;
    }

    /// --------------------------------------------------
    /// Navigation Functions
    /// --------------------------------------------------

    void FollowTarget(float angleTolerance, float distanceTolerance)
    {
        if (target.magnitude < distanceTolerance)
        {
            motorDriver.SetRightSpeed(0f);
            motorDriver.SetLeftSpeed(0f);
            return;
        }

        Vector2 botDir = new Vector2(Mathf.Sin(transform.rotation.eulerAngles.y * Mathf.Deg2Rad), Mathf.Cos(transform.rotation.eulerAngles.y * Mathf.Deg2Rad));
        Vector2 targetDir = target / target.magnitude;
        float D = botDir.x * targetDir.y - botDir.y * targetDir.x;
        float angleError = Mathf.Acos(botDir.x * targetDir.x + botDir.y * targetDir.y) * Mathf.Rad2Deg;
        
        if (angleError > angleTolerance)
        {
            if (D > 0) // bot dir is to the right of target dir, have to turn left 
            {
                motorDriver.SetRightSpeed(0.2f);
                motorDriver.SetLeftSpeed(-0.2f);
            }
            else // bot dir is to the left of target dir, have to turn right 
            {
                motorDriver.SetRightSpeed(-0.2f);
                motorDriver.SetLeftSpeed(0.2f);
            }
        }
        else
        {
            motorDriver.SetRightSpeed(1f);
            motorDriver.SetLeftSpeed(1f);
        }
        
    }

}