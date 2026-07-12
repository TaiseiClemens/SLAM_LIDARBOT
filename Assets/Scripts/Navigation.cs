using UnityEngine;
using System.Collections.Generic;
using System;

public class Navigation : MonoBehaviour
{
    
    public enum NavigationState
    {
        GetToTarget,
        Explore
    }

    [Header("Modules")]
    [SerializeField] private MotorDriver motorDriver;
    [SerializeField] private Lidar lidar;
    [SerializeField] private LidarPointCloud lidarPointCloud;

    [Header("Mapping Properties")]
    [SerializeField] private float cellSize;
    [SerializeField] private Vector2 botDimentions; // For marking unreachable points
    private ChunkManager chunkManager;

    [Header("Navigation")]
    [SerializeField] private NavigationState navigationState = NavigationState.GetToTarget;
    [SerializeField] private Transform targetTransform;
    private PathNode[] currentPath;
    
    private bool mapChangedThisFrame;


    void Start()
    {
        chunkManager = new ChunkManager(cellSize);
    }

    void Update()
    {
        mapChangedThisFrame = false;
        
        if (currentPath != null)
            FollowPath(currentPath, 2f, 0.1f);
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
        FloodSeenCells(new Vector2(transform.position.x, transform.position.z), lidar.GetMaxDistance());
        if (navigationState == NavigationState.GetToTarget)
        {
            PathNode[] newPath = ShortestPath(new Vector2(targetTransform.position.x, targetTransform.position.z), true);
            if (newPath != null)
                currentPath = newPath;
        }
        else if (navigationState == NavigationState.Explore)
        {
            
            PathNode[] newPath = ShortestPath(PathNodeToVec2(NearestUnexploredCell(Vec3ToPathNode(transform.position))));
            if (newPath != null)
            {
                currentPath = newPath;
            }
            else
            {
                motorDriver.SetRightSpeed(0f);
                motorDriver.SetLeftSpeed(0f);
                Debug.Log("Search Ended!");
            }
        }
    }

    /// --------------------------------------------------
    /// Mapping Functions
    /// --------------------------------------------------

    void SetHitCell(Vector2 hitPos)
    {
        //chunkManager.SetCellStatusAtWorldPosition(hitPos, CellStatus.Wall);

        if (chunkManager.GetCellStatusAtWorldPosition(hitPos) != CellStatus.Wall)
        {
            chunkManager.SetCellWallAtWorldPosition(hitPos, botDimentions.y, .5f, .5f);
            mapChangedThisFrame = true;
        }

    }

    void FloodSeenCells(Vector2 selfPos, float visionRadius)
    {
        int searchesLeft = 1000000;

        PathNode startPathNode = Vec2ToPathNode(selfPos);

        Func<PathNode, bool> allowedBlock = node => (PathNodeToVec2(node) - PathNodeToVec2(startPathNode)).magnitude < visionRadius &&
                                                    chunkManager.GetCellStatusAtCellPosition(node.X, node.Y) != CellStatus.Wall &&
                                                    chunkManager.GetCellStatusAtCellPosition(node.X, node.Y) != CellStatus.VisibleBufferZone &&
                                                    chunkManager.GetCellStatusAtCellPosition(node.X, node.Y) != CellStatus.InvisibleBufferZone &&
                                                    chunkManager.GetCellStatusAtCellPosition(node.X, node.Y) != CellStatus.VisibleBufferZone;

        Dictionary<PathNode, PathNode> path = new Dictionary<PathNode, PathNode>(); 
        Queue<PathNode> queue = new Queue<PathNode>();
        Dictionary<(int x, int y), bool> visited = new Dictionary<(int x, int y), bool>();

        queue.Enqueue(startPathNode);

        while (queue.Count > 0 && searchesLeft > 0)
        {
            searchesLeft--;
            PathNode current = queue.Dequeue();

            visited[(current.X, current.Y)] = true;
            chunkManager.SetCellStatusAtCellPosition(current.X, current.Y, CellStatus.Empty);

            foreach (PathNode neighbor in GetNeighbors(current))
            {
                //Debug.Log("Neighbor Position: " + neighbor.X + ", " + neighbor.Y);
                if (!visited.ContainsKey((neighbor.X, neighbor.Y)) && allowedBlock(neighbor))
                {
                    visited[(neighbor.X, neighbor.Y)] = true;
                    chunkManager.SetCellStatusAtCellPosition(neighbor.X, neighbor.Y, CellStatus.Empty);
                    path[neighbor] = current;  // Set neighbor parent to self
                    queue.Enqueue(neighbor); 
                }
            }
        }
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

    
    bool FollowTarget(float angleTolerance, float distanceTolerance, Vector2 target, float baseSpeed = 0.8f)
    {
        // Returns true if moving, if on target returns false

        Vector2 targetVec = new Vector2(target.x - transform.position.x, target.y - transform.position.z);

        // Debug.Log("Target Vector: " + targetVec);

        Debug.DrawRay(transform.position, new Vector3(targetVec.x, 0, targetVec.y), Color.green);


        if (targetVec.magnitude < distanceTolerance)
        {
            motorDriver.SetRightSpeed(0f);
            motorDriver.SetLeftSpeed(0f);
            return false;
        }

        Vector2 botDir = new Vector2(Mathf.Sin(transform.rotation.eulerAngles.y * Mathf.Deg2Rad), Mathf.Cos(transform.rotation.eulerAngles.y * Mathf.Deg2Rad));
        Vector2 targetDir = targetVec / targetVec.magnitude;
        float D = botDir.x * targetDir.y - botDir.y * targetDir.x;
        float angleError = Mathf.Acos(botDir.x * targetDir.x + botDir.y * targetDir.y) * Mathf.Rad2Deg;
        
        if (angleError > angleTolerance)
        {
            
            //float turnSpeed = angleError/180;
            float turnSpeed = 0.1f;

            //Debug.Log(angleError);

            if (angleError > 90f)
            {
                turnSpeed = 1f;
                if (D > 0) // bot dir is to the right of target dir, have to turn left 
                {
                    motorDriver.SetRightSpeed(turnSpeed);
                    motorDriver.SetLeftSpeed(0.01f);
                }
                else // bot dir is to the left of target dir, have to turn right 
                {
                    motorDriver.SetRightSpeed(0.01f);
                    motorDriver.SetLeftSpeed(turnSpeed);
                }
            }
            else
            {
                turnSpeed = 0.4f;
                if (D > 0) // bot dir is to the right of target dir, have to turn left 
                {
                    motorDriver.SetRightSpeed(baseSpeed);
                    motorDriver.SetLeftSpeed(baseSpeed - turnSpeed);
                }
                else // bot dir is to the left of target dir, have to turn right 
                {
                    motorDriver.SetRightSpeed(baseSpeed - turnSpeed);
                    motorDriver.SetLeftSpeed(baseSpeed);
                }
            }
        }
        else
        {
            motorDriver.SetRightSpeed(baseSpeed);
            motorDriver.SetLeftSpeed(baseSpeed);
        }
        return true;
    }
    
    void FollowPath(PathNode[] path, float angleTolerance, float distanceTolerance)
    {

        // if (chunkManager.GetCellStatusAtWorldPosition(new Vector2(transform.position.x, transform.position.z)) == CellStatus.BufferZone)
        // {
        //     motorDriver.SetRightSpeed(0.8f);
        //     motorDriver.SetLeftSpeed(0.8f);
        //     return;
        // }

        int nodeIndex = 0;

        

        /// First we find all the corners in the path
        /// Then we find the furthest corner that the bot can see
        /// The last node will also be considered a corner
        
        PathNode[] corners = GetPathCorners(path);

        

        while (nodeIndex < corners.Length &&
                !IntersectsWithCellType(new Vector2(transform.position.x, transform.position.z), 
                            chunkManager.GetWorldPositionOfCell(corners[nodeIndex].X, corners[nodeIndex].Y), 
                            CellStatus.Unreachable) && 
                !IntersectsWithCellType(new Vector2(transform.position.x, transform.position.z), 
                            chunkManager.GetWorldPositionOfCell(corners[nodeIndex].X, corners[nodeIndex].Y), 
                            CellStatus.InvisibleBufferZone))
        {
            nodeIndex++;
            // Debug.Log("Node index: " + nodeIndex + 
            // ", path length: " + path.Length + 
            // " Node Chunk Position: " + path[nodeIndex].X + ", " + path[nodeIndex].Y + 
            // " Node World Position: " + chunkManager.GetWorldPositionOfCell(path[nodeIndex].X, path[nodeIndex].Y).x + ", " + chunkManager.GetWorldPositionOfCell(path[nodeIndex].X, path[nodeIndex].Y).y + 
            // " Is intersecting: " + IntersectsWithCellType(new Vector2(transform.position.x, transform.position.z), chunkManager.GetWorldPositionOfCell(path[nodeIndex].X, path[nodeIndex].Y), CellStatus.Unreachable)
            // );
        }

        
        Vector2 target = chunkManager.GetWorldPositionOfCell(corners[nodeIndex-1].X, corners[nodeIndex-1].Y);

        //Vector2 target = new Vector2(targetTransform.position.x, targetTransform.position.z);

        Debug.DrawLine(new Vector3(target.x + 0.1f, 0, target.y + 0.1f), new Vector3(target.x + 0.1f, 0, target.y + 0.1f), Color.brown);

        FollowTarget(angleTolerance, distanceTolerance, target);


    }

    /// --------------------------------------------------
    /// Pathfinding
    /// --------------------------------------------------

    PathNode[] RetracePath(PathNode startPathNode, PathNode targetPathNode, Dictionary<PathNode, PathNode> pathDict)
    {
        //Debug.Log("Found Path!");

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

    public PathNode[] ShortestPath(Vector2 target, bool raiseWhenPathNotFound = false)
    {
        int searchesLeft = 1000000;

        Dictionary<PathNode, PathNode> path = new Dictionary<PathNode, PathNode>(); 
        Queue<PathNode> queue = new Queue<PathNode>();
        Dictionary<(int x, int y), bool> visited = new Dictionary<(int x, int y), bool>();

        PathNode startPathNode = Vec3ToPathNode(transform.position);

        PathNode targetPathNode = Vec2ToPathNode(target);

        if (chunkManager.GetCellStatusAtCellPosition(startPathNode.X, startPathNode.Y) == CellStatus.VisibleBufferZone || 
            chunkManager.GetCellStatusAtCellPosition(startPathNode.X, startPathNode.Y) == CellStatus.InvisibleBufferZone)
        {
            startPathNode = NearestEmptyCell(startPathNode);
        }

        //Debug.Log("Start Pos: " + startPathNode.X + ", " + startPathNode.Y);
        //Debug.Log("Target Pos: " + targetPathNode.X + ", " + targetPathNode.Y);

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
                    && chunkManager.GetCellStatusAtCellPosition(neighbor.X, neighbor.Y) != CellStatus.VisibleBufferZone
                    && chunkManager.GetCellStatusAtCellPosition(neighbor.X, neighbor.Y) != CellStatus.InvisibleBufferZone
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
            if (raiseWhenPathNotFound)
                Debug.LogError("Did not find a path");
        } 
        else
        {
            Debug.LogError("Took too long to find path");
        }

        return null;   
    }

    PathNode NearestEmptyCell(PathNode startNode)
    {
        Func<PathNode, bool> targetCondition = node => chunkManager.GetCellStatusAtCellPosition(node.X, node.Y) == CellStatus.Empty; 
        Func<PathNode, bool> allowedBlock = node => chunkManager.GetCellStatusAtCellPosition(node.X, node.Y) != CellStatus.Wall && 
                                                    chunkManager.GetCellStatusAtCellPosition(node.X, node.Y) != CellStatus.Unreachable; 
        PathNode[] pathToCell = PathToTarget
        (
            startNode,
            targetCondition,
            allowedBlock
        );

        return pathToCell[pathToCell.Length-1];
    }

    PathNode NearestUnexploredCell(PathNode startNode)
    {
        Func<PathNode, bool> targetCondition = node => chunkManager.GetCellStatusAtCellPosition(node.X, node.Y) == CellStatus.Unexplored; 
        Func<PathNode, bool> allowedBlock = node => chunkManager.GetCellStatusAtCellPosition(node.X, node.Y) != CellStatus.Wall && 
                                                    chunkManager.GetCellStatusAtCellPosition(node.X, node.Y) != CellStatus.Unreachable; 
        PathNode[] pathToCell = PathToTarget
        (
            startNode,
            targetCondition,
            allowedBlock
        );

        return pathToCell[pathToCell.Length-1];
    }

    PathNode[] PathToTarget(PathNode startPathNode, Func<PathNode, bool> targetCondition, Func<PathNode, bool> allowedBlock, bool raiseWhenPathNotFound = false)
    {

        /// targetCondition is a function which takes in a path node and returns if that path node is a target
        /// It is there so that if you are looking for a certain type of cell instead of a specific point, you
        /// can still just as well do that using a function.
        /// Similarly, allowedBlock is a function taking in a path node and returns if the path is allowed to 
        /// go through that cell. 

        int searchesLeft = 1000000;

        Dictionary<PathNode, PathNode> path = new Dictionary<PathNode, PathNode>(); 
        Queue<PathNode> queue = new Queue<PathNode>();
        Dictionary<(int x, int y), bool> visited = new Dictionary<(int x, int y), bool>();

        queue.Enqueue(startPathNode);

        while (queue.Count > 0 && searchesLeft > 0)
        {
            searchesLeft--;
            PathNode current = queue.Dequeue();

            visited[(current.X, current.Y)] = true;

            //Debug.Log("Intermediate Position: " + current.X + ", " + current.Y);

            if (targetCondition(current))
            {
                PathNode[] finalPath = RetracePath(startPathNode, current, path);
                return finalPath;
            }

            foreach (PathNode neighbor in GetNeighbors(current))
            {
                //Debug.Log("Neighbor Position: " + neighbor.X + ", " + neighbor.Y);
                if (!visited.ContainsKey((neighbor.X, neighbor.Y)) && allowedBlock(neighbor))
                {
                    visited[(neighbor.X, neighbor.Y)] = true;
                    path[neighbor] = current;  // Set neighbor parent to self
                    queue.Enqueue(neighbor); 
                }
            }
        }

        if (queue.Count <= 0)
        {
            if (raiseWhenPathNotFound)
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

    public Vector2 GetBotPosition()
    {
        return new Vector2(transform.position.x, transform.position.z);
    }

    bool IntersectsWithCellType(Vector2 startPos, Vector2 endPos, CellStatus cellStatus)
    {
        Debug.DrawLine(new Vector3(startPos.x, 0, startPos.y), new Vector3(endPos.x, 0, endPos.y));

        float lineLength = (endPos - startPos).magnitude;

        Vector2 unitVec = (endPos - startPos) / lineLength * cellSize * 0.2f;

        for (int i = 0; i < Mathf.FloorToInt(lineLength / cellSize) * 5; i++)
        {
            Vector2 checkVector = startPos + unitVec * i;
            if (chunkManager.GetCellStatusAtWorldPosition(checkVector) == cellStatus)
            {
                return true;
            }
        }
        return false;
    }
    
    public PathNode[] GetPath()
    {
        return currentPath;
    }

    bool IsCorner(PathNode centerNode, PathNode adjacentNode, PathNode otherAdjacentNode)
    {
        bool isHorizontal = adjacentNode.X == centerNode.X;
        return !((adjacentNode.X == centerNode.X && otherAdjacentNode.X == centerNode.X) || 
                (adjacentNode.Y == centerNode.Y && otherAdjacentNode.Y == centerNode.Y));
    }
    
    PathNode[] GetPathCorners(PathNode[] path)
    {
        if (path.Length < 3)
            return new PathNode[1] {path[path.Length - 1]};
        
        List<PathNode> corners = new List<PathNode>();

        for (int i = 1; i < path.Length - 1; i++)
        {
            if (IsCorner(path[i], path[i-1], path[i+1]))
                corners.Add(path[i]);
        }
        corners.Add(path[path.Length-1]);
        return corners.ToArray();
    }

    PathNode Vec2ToPathNode(Vector2 vec)
    {
        return new PathNode(Mathf.FloorToInt(vec.x / cellSize), Mathf.FloorToInt(vec.y / cellSize), 0);
    }

    PathNode Vec3ToPathNode(Vector3 vec)
    {
        return new PathNode(Mathf.FloorToInt(vec.x / cellSize), Mathf.FloorToInt(vec.z / cellSize), 0);
    }

    Vector2 PathNodeToVec2(PathNode node)
    {
        return chunkManager.GetWorldPositionOfCell(node.X, node.Y);
    }
}