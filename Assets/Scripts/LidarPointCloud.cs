using UnityEngine;


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LidarPointCloud : MonoBehaviour
{

    private Mesh mesh;

    void Awake()
    {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Allow for more than 65k vertices
        GetComponent<MeshFilter>().mesh = mesh;
    }

    public void UpdatePointCloud(Vector4[] points)
    {
        Vector3[] vertices = new Vector3[points.Length];
        Color[] colors = new Color[points.Length];
        int[] indices = new int[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            vertices[i] = new Vector3(points[i].x, points[i].y, points[i].z);
            colors[i] = points[i].w == 1 ? Color.red : Color.clear; // Red for hits, clear for no hits
            indices[i] = i;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.colors = colors;

        mesh.SetIndices(indices, MeshTopology.Points, 0);
    }

}
