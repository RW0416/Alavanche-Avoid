using UnityEngine;

public class SnowDeformer : MonoBehaviour
{
    public float brushRadius = 1.5f;
    public float depressionAmount = 0.5f;
    public LayerMask snowLayer; 

    private MeshFilter lastMeshFilter;
    private Mesh lastMesh;
    private Vector3[] cachedVertices;

    void Update()
    {
        Debug.DrawRay(transform.position + Vector3.up * 2, Vector3.down * 5, Color.red);

        if (Physics.Raycast(transform.position + Vector3.up * 2, Vector3.down, out RaycastHit hit, 10.0f, snowLayer))
        {
            MeshFilter mf = hit.collider.GetComponent<MeshFilter>();
            if (mf)
            {
                if (mf != lastMeshFilter)
                {
                    lastMeshFilter = mf;
                    lastMesh = mf.mesh;
                    lastMesh.MarkDynamic();
                    cachedVertices = lastMesh.vertices;
                }

                ModifyMesh(hit.point);
            }
        }
    }

    void ModifyMesh(Vector3 hitPoint)
    {
        bool changed = false;
        Vector3 localHit = lastMeshFilter.transform.InverseTransformPoint(hitPoint);
        float radiusSqr = brushRadius * brushRadius;

        for (int i = 0; i < cachedVertices.Length; i++)
        {
            float distSqr = (localHit - cachedVertices[i]).sqrMagnitude;

            if (distSqr < radiusSqr)
            {
                float dist = Mathf.Sqrt(distSqr);
                float force = depressionAmount * (1f - (dist / brushRadius));

                cachedVertices[i].y -= force * Time.deltaTime * 50f;
                changed = true;
            }
        }

        if (changed)
        {
            lastMesh.vertices = cachedVertices;
            lastMesh.RecalculateNormals(); 
        }
    }
}