using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;
using UnityEngine.InputSystem.Controls;
using System.Xml.Schema;


public class Navigation : MonoBehaviour
{
    
    [Header("Modules")]
    [SerializeField] private MotorDriver motorDriver;
    [SerializeField] private Lidar lidar;
    [SerializeField] private LidarPointCloud lidarPointCloud;

    [Header("Mapping Properties")]
    [SerializeField] private float cellSize;
    [SerializeField] private Vector2 botDimentions; // For marking unreachable points
    private Dictionary<Vector2, CellStatus> hitCells = new Dictionary<Vector2, CellStatus>(100000) {}; // Cell position is top left corner
    private ChunkManager chunkManager;

    [Header("Navigation")]
    [SerializeField] private Transform targetTransform;
    private Vector2 target;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(MainLoop());
        chunkManager = new ChunkManager(cellSize);
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
                Debug.DrawRay(lidar.transform.position, new Vector3(point.x, point.y, point.z) - lidar.transform.position, Color.red); // Draw rays for each hit point
                SetHitCell(new Vector2(point.x, point.z));
            }
        }
    }

    /// --------------------------------------------------
    /// Mapping Functions
    /// --------------------------------------------------

    void SetHitCell(Vector2 hitPos)
    {
        //chunkManager.SetCellStatusAtWorldPosition(hitPos, CellStatus.Wall);
        chunkManager.SetCellWallAtWorldPosition(hitPos, botDimentions.x + botDimentions.y);
    }

    public CellStatus GetCellStatus(Vector2 pos)
    {
        return chunkManager.GetCellStatusAtWorldPosition(pos);
    }

    public ChunkManager getChunkManager()
    {
        return chunkManager;
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
    
    /// --------------------------------------------------
    /// Pathfinding
    /// --------------------------------------------------

    PathNode[] RetracePath(PathNode startPathNode, PathNode targetPathNode, Dictionary<PathNode, PathNode> pathDict)
    {
        Debug.Log("Found Path!");

        List<PathNode> path = new List<PathNode>();

        PathNode currentPathNode = targetPathNode;

        path.Add(currentPathNode);

        while (!currentPathNode.Equal(startPathNode))
        {
            path.Insert(0, pathDict[currentPathNode]);
            currentPathNode = pathDict[currentPathNode];
        }

        return path.ToArray();
    }

    PathNode[] GetNeighbors(PathNode PathNode)
    {
        return new PathNode[]
        {
            new PathNode(PathNode.X - 1, PathNode.Y, PathNode.Distance + 1),
            new PathNode(PathNode.X + 1, PathNode.Y, PathNode.Distance + 1),
            new PathNode(PathNode.X, PathNode.Y + 1, PathNode.Distance + 1),
            new PathNode(PathNode.X, PathNode.Y - 1, PathNode.Distance + 1)
        };
    }

    public PathNode[] ShortestPath()
    {
        int searchesLeft = 10000;

        Dictionary<PathNode, PathNode> path = new Dictionary<PathNode, PathNode>(); 
        Queue<PathNode> queue = new Queue<PathNode>();
        Dictionary<(int x, int y), bool> visited = new Dictionary<(int x, int y), bool>(); //bool[,] visited = new bool[1000, 1000]; // TODO: Add reallocation

        PathNode startPathNode = new PathNode(Mathf.FloorToInt(transform.position.x / cellSize), Mathf.FloorToInt(transform.position.z / cellSize), 0);

        PathNode targetPathNode = new PathNode(Mathf.FloorToInt(targetTransform.position.x / cellSize), Mathf.FloorToInt(targetTransform.position.z / cellSize), 0);

        Debug.Log("Start Pos: " + startPathNode.X + ", " + startPathNode.Y);
        Debug.Log("Target Pos: " + targetPathNode.X + ", " + targetPathNode.Y);

        // Make nodes relative to global coordinates

        queue.Enqueue(startPathNode);

        while (queue.Count > 0 && searchesLeft > 0)
        {
            searchesLeft--;
            PathNode current = queue.Dequeue();

            visited[(current.X, current.Y)] = true;


            //Debug.Log("Intermediate Position: " + current.X + ", " + current.Y);

            if (current.Equal(targetPathNode))
            {
                PathNode[] finalPath = RetracePath(startPathNode, targetPathNode, path);
                return finalPath;
            }

            foreach (PathNode neighbor in GetNeighbors(current))
            {
                //Debug.Log("Neighbor Position: " + neighbor.X + ", " + neighbor.Y);
                if (!visited.ContainsKey((neighbor.X, neighbor.Y)) // if has not visited
                    && chunkManager.GetCellStatusAtCellPosition(neighbor.X, neighbor.Y) != CellStatus.Unreachable
                    && chunkManager.GetCellStatusAtCellPosition(neighbor.X, neighbor.Y) != CellStatus.Wall)
                {
                    visited[(neighbor.X, neighbor.Y)] = true;
                    path[neighbor] = current;  // Set neighbor parent to self
                    queue.Enqueue(neighbor); 
                }
            }
        }

        if (queue.Count <= 0)
        {
            Debug.LogError("Did not find a path");
        } 
        else
        {
            Debug.LogError("Took too long to find path");
        }

        return null;   
    }

    /// --------------------------------------------------
    /// Utility
    /// --------------------------------------------------

    public Vector2 GetTargetPosition()
    {
        return new Vector2(targetTransform.position.x, targetTransform.position.z);
    }

}