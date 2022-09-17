using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class MultiMeshRenderer : MonoBehaviour
{
    [SerializeField] private Material[] materials;

    private MeshFilter meshFilter;

    private void OnEnable()
    {
        meshFilter = gameObject.GetComponent<MeshFilter>();
    }

    private void LateUpdate()
    {
        foreach (Material material in materials)
        {
            Graphics.DrawMesh(meshFilter.sharedMesh, gameObject.transform.position, gameObject.transform.rotation, material, 0);
        }
    }
}
