using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class MultiMeshRenderer : MonoBehaviour
{
    [SerializeField] private MaterialContainer[] materials;
    [SerializeField] private MaterialSwitch[] materialSwitches;

    private Dictionary<string, Material> materialDict = new ();
    private Dictionary<string, MaterialSwitch> materialSwitchDict = new();

    private MeshFilter meshFilter;

    private void OnEnable()
    {
        meshFilter = gameObject.GetComponent<MeshFilter>();
        
        foreach (MaterialContainer material in materials)
        {
            materialDict.Add(material.MaterialID, material.Material);
        }

        foreach (MaterialSwitch materialSwitch in materialSwitches)
        {
            materialSwitchDict.Add(materialSwitch.SwitchID, materialSwitch);
        }
    }

    private void LateUpdate()
    {
        foreach (Material material in materialDict.Values)
        {
            Graphics.DrawMesh(meshFilter.sharedMesh, gameObject.transform.position, gameObject.transform.rotation, material, 0);
        }
    }

    public void SwitchMaterial(string switchID)
    {
        if (materialSwitchDict.TryGetValue(switchID, out MaterialSwitch ms))
        {
            materialDict.Remove(ms.MaterialID);
            materialDict.Add(ms.MaterialID, ms.NewMaterial);
        }
        else
        {
            Debug.LogWarning($"Could not find material switch pattern with ID [{switchID}]");
        }
    }

    [Serializable]
    public struct MaterialContainer
    {
        public string MaterialID;
        public Material Material;
    }
}
