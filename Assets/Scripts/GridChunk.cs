using System;
using UnityEngine;
/*
public class GridChunk
{
    public const int chunkSize = 32;
    private readonly ulong[] _bitmask = new ulong[32]; 
    /// ulong is 64 bits long, which allows support of 64 * 32 bits 
    /// This is perfect for 2 * 32 * 32, as we need 2 bits to keep 
    /// track of state, and the others for the 32 by 32 grid. 

    public CellStatus GetValue(int localX, int localY)
    {
        Debug.Assert(localX >= 0 && localX < chunkSize);
        Debug.Assert(localY >= 0 && localY < chunkSize);

        int bitPos = localX * 2;

        return (CellStatus)((_bitmask[localY] >> bitPos) & 3UL);
    }

    public void SetValue(int localX, int localY, CellStatus cellStatus)
    {
        Debug.Assert(localX >= 0 && localX < chunkSize);
        Debug.Assert(localY >= 0 && localY < chunkSize);
        Debug.Assert(((CellStatus[])System.Enum.GetValues(typeof(CellStatus))).Length <= 4);

        int bitPos = localX * 2;

        ulong cellBits = (ulong)cellStatus & 3UL;

        _bitmask[localY] &= ~(3UL << bitPos); // Clears the desired bits
        _bitmask[localY] |= cellBits << bitPos; // Adds in the new status
    }

}
*/

public class GridChunk
{
    public const int chunkSize = 32;
    private readonly ulong[] _bitmask = new ulong[64]; 
    /// ulong is 64 bits long, which allows support of 64 * 64 bits 
    /// This is perfect for 4 * 32 * 32, as we need 4 bits to keep 
    /// track of state, and the others for the 32 by 32 grid. 
    /// 
    /// 16 cells can be kept track per ulong
    /// bit position y would be local y * 2
    /// bit position x would be local x % 16

    public CellStatus GetValue(int localX, int localY)
    {
        Debug.Assert(localX >= 0 && localX < chunkSize);
        Debug.Assert(localY >= 0 && localY < chunkSize);

        int bitPosX = localX % 16 * 4;
        int bitPosY = localY * 2 + localX / 16;

        return (CellStatus)((_bitmask[bitPosY] >> bitPosX) & 15UL);
    }

    public void SetValue(int localX, int localY, CellStatus cellStatus)
    {
        Debug.Assert(localX >= 0 && localX < chunkSize);
        Debug.Assert(localY >= 0 && localY < chunkSize);
        Debug.Assert(((CellStatus[])System.Enum.GetValues(typeof(CellStatus))).Length <= 16);

        int bitPosX = localX % 16 * 4;
        int bitPosY = localY * 2 + localX / 16;

        ulong cellBits = (ulong)cellStatus & 15UL;

        _bitmask[bitPosY] &= ~(15UL << bitPosX); // Clears the desired bits
        _bitmask[bitPosY] |= cellBits << bitPosX; // Adds in the new status
    }
}
