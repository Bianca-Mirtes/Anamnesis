using UnityEngine;

public class FlipNormals : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;

        Vector3[] normals = mesh.normals;
        for (int ii = 0; ii < normals.Length; ii++)
        {
            normals[ii] = -1 * normals[ii]; 
        }

        mesh.normals = normals;

        for (int ii = 0; ii < mesh.subMeshCount; ii++)
        {
            int[] tris = mesh.GetTriangles(ii);
            for (int jj = 0; jj < tris.Length; jj+=3)
            {
                int temp = tris[jj];
                tris[jj] = tris[jj + 1];
                tris[jj + 1] = temp;
            }
            mesh.SetTriangles(tris, ii);
        }
    }
}
