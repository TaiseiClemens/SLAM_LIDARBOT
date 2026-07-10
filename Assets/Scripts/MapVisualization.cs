using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;
using System.IO;

public class MapVisualization : MonoBehaviour
{
    [SerializeField] private int resolution = 64;
    [SerializeField] private float distance = 16f;
    [SerializeField] private Navigation navigation;
    [SerializeField] private Transform botTransform;
    [SerializeField] private Color hitColor;
    [SerializeField] private Color emptyColor;
    [SerializeField] private Color pathColor;
    [SerializeField] private Color targetColor;
    [SerializeField] private Color botColor;
    [SerializeField] private Color unreachableColor;
    [SerializeField] private Color bufferColor;

    private Texture2D texture;
    private Color32[] pixelBuffer;
    private Material targetMaterial;
    

    void Start()
    {
        // 1. Create the texture
        texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point; // Prevents blurry edges between cells
        texture.wrapMode = TextureWrapMode.Clamp;

        pixelBuffer = new Color32[resolution * resolution];

        targetMaterial = GetComponent<Renderer>().material;
        targetMaterial.mainTexture = texture;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (navigation.ShortestPath() != null)
            UpdateMapWithPath();
        else 
            UpdateMap();
    }


    void UpdateMap()
    {
        /// Take the position of the bot as the middle
        /// Then take the positions of the dictionary
        /// Filter the positions to the ones that are in range of the bot
        /// Find distance of each points from the bot
        /// Go through each point and draw them using the following position:
        /// d_x = cell.x - bot.x, d_y = cell.y - bot.y
        /// x = floor(d_x / cellSize) + resolution / 2, y = ceil(d_y / cellSize) + resolution / 2
        
        float cellSize = distance * 2f / resolution;
        //Dictionary<Vector2, CellStatus> hitCells = navigation.GetHitCells();
        Vector2 bot = new Vector2(botTransform.position.x, botTransform.position.z);

        ChunkManager chunkManager = navigation.getChunkManager();

        System.Array.Fill(pixelBuffer, emptyColor);


        for (int i = 0; i < resolution; i++)
        {
            for (int j = 0; j < resolution; j++)
            {
                float worldPosX = bot.x - resolution * cellSize / 2 + i * cellSize;
                float worldPosY = bot.y + resolution * cellSize / 2 - j * cellSize;

                CellStatus cellStatus = chunkManager.GetCellStatusAtWorldPosition(new Vector2(worldPosX, worldPosY));

                int index = resolution * resolution - j * resolution + i;

                if (cellStatus == CellStatus.Wall)
                    pixelBuffer[index] = hitColor;
            }
        }

        // foreach (Vector2 cell in hitCells.Keys)
        // {

        //     CellStatus cellStatus = chunkManager.GetCellStatusAtWorldPosition();

        //     float d_x = cell.x - bot.x;
        //     float d_y = cell.y - bot.y;
        //     int x = (int)(d_x / cellSize) + resolution / 2;
        //     int y = (int)(d_y / cellSize) + resolution / 2;

        //     if (x >= 0 && x < resolution && y >= 0 && y < resolution)
        //     {
        //         int index = y * resolution + x;
        //         pixelBuffer[index] = hitColor;
        //     }
        // }

        texture.SetPixels32(pixelBuffer);
        texture.Apply();
    }
    
    void UpdateMapWithPath()
    {
        float cellSize = distance * 2f / resolution;
        //Dictionary<Vector2, CellStatus> hitCells = navigation.GetHitCells();
        Vector2 bot = new Vector2(botTransform.position.x, botTransform.position.z);

        ChunkManager chunkManager = navigation.getChunkManager();
        PathNode[] nodes = navigation.ShortestPath();

        System.Array.Fill(pixelBuffer, emptyColor);

        
        for (int i = 0; i < resolution; i++)
        {
            for (int j = 0; j < resolution; j++)
            {
                float worldPosX = bot.x - resolution * cellSize / 2 + i * cellSize;
                float worldPosY = bot.y + resolution * cellSize / 2 - j * cellSize;

                CellStatus cellStatus = chunkManager.GetCellStatusAtWorldPosition(new Vector2(worldPosX, worldPosY));

                int index = resolution * resolution - j * resolution + i;

                if (cellStatus == CellStatus.Wall)
                    pixelBuffer[index] = hitColor;
                else if (cellStatus == CellStatus.Unreachable)
                    pixelBuffer[index] = unreachableColor;
                else if (cellStatus == CellStatus.BufferZone)
                    pixelBuffer[index] = bufferColor;
            }
        }

        foreach (PathNode node in nodes)
        {
            
            CellStatus cellStatus = chunkManager.GetCellStatusAtWorldPosition(new Vector2(node.X, node.Y));

            Vector2 nodePos = chunkManager.GetWorldPositionOfCell(node.X, node.Y);

            DrawPosition(nodePos, pathColor);
        }


        DrawPosition(navigation.GetTargetPosition(), targetColor);
        DrawPosition(navigation.GetBotPosition(), botColor);

        texture.SetPixels32(pixelBuffer);
        texture.Apply();
    }

    void DrawPosition(Vector2 position, Color color)
    {
        float cellSize = distance * 2f / resolution;
        Vector2 bot = new Vector2(botTransform.position.x, botTransform.position.z);

        float d_x = position.x - bot.x;
        float d_y = position.y - bot.y;
        int x = (int)(d_x / cellSize) + resolution / 2;
        int y = (int)(d_y / cellSize) + resolution / 2;

        if (x >= 0 && x < resolution && y >= 0 && y < resolution)
        {
            int index = y * resolution + x;
            pixelBuffer[index] = color;
        }
    }
}
