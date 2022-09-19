using UnityEngine;

[CreateAssetMenu(fileName = "MaterialSwitch", menuName = "Misc/Material Switch")]
public class MaterialSwitch : ScriptableObject
{
    public string SwitchID;
    public string MaterialID;
    public Material NewMaterial;
}
