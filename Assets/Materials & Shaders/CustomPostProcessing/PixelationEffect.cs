using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenuForRenderPipeline("Custom/PixelationEffect", typeof(UniversalRenderPipeline))]
public class PixelationEffect : VolumeComponent, IPostProcessComponent
{
    public NoInterpIntParameter pixelHeight = new NoInterpIntParameter(240);
    public NoInterpFloatParameter aspectRatio = new NoInterpFloatParameter(16.0f/9.0f);

    public bool IsActive() => aspectRatio.value > 0 && pixelHeight.value > 0;

    public bool IsTileCompatible() => true;
}
