using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
}