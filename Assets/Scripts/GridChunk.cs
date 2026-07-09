public class GridChunk
{

    private readonly ulong[] _bitmask = new ulong[32];
    /// ulong is 64 bits long, which allows support of 64 * 32 bits 
    /// This is perfect for 2 * 32 * 32, as we need 2 bits to keep 
    /// track of state, and the others for the 32 by 32 grid. 

    public void GetValue(int localX, int localY)
    {

    }

}

