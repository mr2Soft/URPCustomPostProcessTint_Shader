using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable]

//this class is basicly the "Settings" variant. Other excamples 
public class URPCustomPostProcessTintSettings
{

    public Shader shader;
    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    //here we create all parameters, we use in the shader
    public FloatParameter tintIntensity = new ClampedFloatParameter(1f, 0f, 5f); //create a simple float as default value 1
    public ColorParameter tintColor = new ColorParameter(Color.white); //create a color preset (default white)
    public bool IsActive() => true;
    public bool IsTileCompatible() => true;

    //some also do here
    //public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents; I'm doing it in the PASS class for now.
}

