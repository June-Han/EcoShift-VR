using UnityEngine;

public class MeshClipper : MonoBehaviour
{
    public Vector3 boundsCenter;
    public Vector3 boundsSize = Vector3.one;

    void Start()
    {
        var mf = GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh) return;

        Mesh original = Instantiate(mf.sharedMesh);
        Mesh clipped = Instantiate(original);
        Bounds clip = new Bounds(boundsCenter, boundsSize);

        Vector3[] verts = clipped.vertices;
        int[] tris = original.triangles;

        var newTris = new System.Collections.Generic.List<int>();
        for (int i = 0; i < tris.Length; i += 3)
        {
            if (clip.Contains(verts[tris[i]]) &&
                clip.Contains(verts[tris[i + 1]]) &&
                clip.Contains(verts[tris[i + 2]]))
            {
                newTris.Add(tris[i]);
                newTris.Add(tris[i + 1]);
                newTris.Add(tris[i + 2]);
            }
        }

        clipped.triangles = newTris.ToArray();
        clipped.RecalculateBounds();
        mf.mesh = clipped;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(boundsCenter, boundsSize);
    }
}