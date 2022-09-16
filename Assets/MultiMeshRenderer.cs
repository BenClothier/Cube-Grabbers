using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class MultiMeshRenderer : MonoBehaviour
{
    [SerializeField] private Material[] materials;

    private void LateUpdate()
    {
        foreach (Material material in materials)
        {
            DrawAllMeshes(gameObject, material);
        }
    }

    public static void DrawAllMeshes(GameObject gameObject, Material material)
    {
        MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();

        foreach (var meshFilter in meshFilters)
        {
            // Static objects may use static batching, preventing us from accessing their default mesh
            if (!meshFilter.gameObject.isStatic)
            {
                var mesh = meshFilter.sharedMesh;
                // Render all submeshes
                for (int i = 0; i < mesh.subMeshCount; i++)
                    Graphics.DrawMesh(mesh, gameObject.transform.position, Quaternion.identity, material, 0);
            }
        }
    }
}
