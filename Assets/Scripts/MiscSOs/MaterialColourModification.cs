using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Misc/Material Modifications/Colour Modification")]
public class MaterialColourModification : MaterialModification
{
    public string propertyReference;
    public Color color;

    public override Action<Material> ModificationAction => material => material.SetColor(propertyReference, color);
}
