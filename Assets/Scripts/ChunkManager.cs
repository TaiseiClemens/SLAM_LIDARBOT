using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

public class ChunkManager
{
    private readonly float _cellSize;
    private readonly float _chunkWorldSize;
    private readonly Dictionary<ChunkKey, GridChunk> _chunks = new (2048);

    public ChunkManager(float cellSize)
    {
        _cellSize = cellSize;
        _chunkWorldSize = cellSize * GridChunk.chunkSize;
    }

    public void SetCellStatusAtWorldPosition(Vector2 worldPos, CellStatus status)
    {
        int globalCellX = Mathf.FloorToInt(worldPos.x / _cellSize);
        int globalCellY = Mathf.FloorToInt(worldPos.y / _cellSize);

        int chunkX = Mathf.FloorToInt((float)globalCellX / GridChunk.chunkSize);
        int chunkY = Mathf.FloorToInt((float)globalCellY / GridChunk.chunkSize);
        ChunkKey key = new ChunkKey(chunkX, chunkY);

        if (!_chunks.TryGetValue(key, out GridChunk chunk))
        {
            chunk = new GridChunk();
            _chunks.Add(key, chunk);
        }

        int localCellX = globalCellX - (chunkX * GridChunk.chunkSize);
        int localCellY = globalCellY - (chunkY * GridChunk.chunkSize);
        chunk.SetValue(localCellX, localCellY, status);
    }

    public void SetCellWallAtWorldPosition(Vector2 worldPos, float radius, float invisibleBufferSize, float visibleBufferSize)
    {
        /// width of square will be r * 2 at positions worldPos.x - r and worldPos.x + r
        /// then get number of cells within by deviding r * 2 by cellSize
        /// use a nested for loop to loop through ever cell within that area
         
        SetCellStatusAtWorldPosition(worldPos, CellStatus.Wall);

        int invisibleBufferRadius = Mathf.FloorToInt(invisibleBufferSize / _cellSize);
        int visibleBufferRadius = Mathf.FloorToInt(visibleBufferSize / _cellSize);
        int bufferRadius = invisibleBufferRadius + visibleBufferRadius;

        float startX = worldPos.x - radius - invisibleBufferSize - visibleBufferSize;
        float startY = worldPos.y + radius + invisibleBufferSize + visibleBufferSize; 

        int numCells = Mathf.FloorToInt(radius * 2 / _cellSize);

        for (int i = 0; i < numCells + bufferRadius * 2; i++)
        {
            for (int j = 0; j < numCells  + bufferRadius * 2; j++)
            {
                if (i > bufferRadius && i < numCells + bufferRadius && 
                    j > bufferRadius && j < numCells + bufferRadius)
                {
                    if (GetCellStatusAtWorldPosition(new Vector2(startX + i * _cellSize, startY - j * _cellSize)) != CellStatus.Wall)
                        SetCellStatusAtWorldPosition(new Vector2(startX + i * _cellSize, startY - j * _cellSize), CellStatus.Unreachable);
                }
                else if(
                    i > visibleBufferRadius && i < numCells + bufferRadius + invisibleBufferRadius && 
                    j > visibleBufferRadius && j < numCells + bufferRadius + invisibleBufferRadius)
                {
                    if (GetCellStatusAtWorldPosition(new Vector2(startX + i * _cellSize, startY - j * _cellSize)) != CellStatus.Wall &&
                        GetCellStatusAtWorldPosition(new Vector2(startX + i * _cellSize, startY - j * _cellSize)) != CellStatus.Unreachable)
                        SetCellStatusAtWorldPosition(new Vector2(startX + i * _cellSize, startY - j * _cellSize), CellStatus.InvisibleBufferZone);
                }
                else
                {
                    if (GetCellStatusAtWorldPosition(new Vector2(startX + i * _cellSize, startY - j * _cellSize)) != CellStatus.Wall &&
                        GetCellStatusAtWorldPosition(new Vector2(startX + i * _cellSize, startY - j * _cellSize)) != CellStatus.Unreachable &&
                        GetCellStatusAtWorldPosition(new Vector2(startX + i * _cellSize, startY - j * _cellSize)) != CellStatus.InvisibleBufferZone)
                        SetCellStatusAtWorldPosition(new Vector2(startX + i * _cellSize, startY - j * _cellSize), CellStatus.VisibleBufferZone);
                }
                
            }
        }
    }

    public CellStatus GetCellStatusAtWorldPosition(Vector2 worldPos)
    {
        int globalCellX = Mathf.FloorToInt(worldPos.x / _cellSize);
        int globalCellY = Mathf.FloorToInt(worldPos.y / _cellSize);

        int chunkX = Mathf.FloorToInt((float)globalCellX / GridChunk.chunkSize);
        int chunkY = Mathf.FloorToInt((float)globalCellY / GridChunk.chunkSize);
        ChunkKey key = new ChunkKey(chunkX, chunkY);

        if (_chunks.TryGetValue(key, out GridChunk chunk))
        {
            int localCellX = globalCellX - (chunkX * GridChunk.chunkSize);
            int localCellY = globalCellY - (chunkY * GridChunk.chunkSize);
            return chunk.GetValue(localCellX, localCellY);
        }

        return CellStatus.Unexplored;
    }

    public CellStatus GetCellStatusAtCellPosition(int X, int Y)
    {
        int chunkX = Mathf.FloorToInt((float)X / GridChunk.chunkSize);
        int chunkY = Mathf.FloorToInt((float)Y / GridChunk.chunkSize);
        ChunkKey key = new ChunkKey(chunkX, chunkY);

        if (_chunks.TryGetValue(key, out GridChunk chunk))
        {
            int localCellX = X - (chunkX * GridChunk.chunkSize);
            int localCellY = Y - (chunkY * GridChunk.chunkSize);
            return chunk.GetValue(localCellX, localCellY);
        }

        return CellStatus.Unexplored;
    }

    public Vector2 GetWorldPositionOfCell(int X, int Y)
    {
        return new Vector2(X * _cellSize, Y * _cellSize);
    }

}