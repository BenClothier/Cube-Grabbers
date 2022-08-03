using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenuForRenderPipeline("Custom/PixelationEffect", typeof(UniversalRenderPipeline))]
public class PixelationEffect : VolumeComponent, IPostProcessComponent
{
    public NoInterpIntParameter pixelsPerUnit = new NoInterpIntParameter(16);

    public bool IsActive() => pixelsPerUnit.value > 0;

    public bool IsTileCompatible() => true;
}
