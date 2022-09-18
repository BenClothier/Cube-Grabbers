using System;
using UnityEngine;

public abstract class MaterialModification : ScriptableObject
{
    public abstract Action<Material> ModificationAction { get; }
}
