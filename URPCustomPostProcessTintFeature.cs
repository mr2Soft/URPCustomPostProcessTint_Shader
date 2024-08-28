using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//// CS-File must be named like the feature - or URP-High Fidelity-Renderer cant find
////
//// URPCustomPostProcessTint : VolumeComponent, IPostProcessComponent
//// URPCustomPostProcessTintPass:ScriptableRenderPass
//// URPCustomPostProcessTintFeature : ScriptableRendererFeature
//// URPCustomPostProcessTintSettings (separate file) 
//// URPCustomPostProcessTintShader (separate shader - HLSL)
//// 


/// <summary>
/// Create a custom render pass that uses the settings.
/// </summary>
public class URPCustomPostProcessTintPass : ScriptableRenderPass
{
    private Material material;
    private URPCustomPostProcessTintSettings settings;
    private RTHandle cameraColorTarget;
    private RTHandle tempTextureHandle;
    public URPCustomPostProcessTintPass(Material material, URPCustomPostProcessTintSettings settings)
    {
        this.material = material;
        this.settings = settings;
        Debug.Log($"(Construktor) Pass setting: {settings.tintColor.ToString()} @{settings.tintIntensity} intensity");
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        // Setup the camera color target using RTHandle
        cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
        // Create a temporary RTHandle for processing
        RenderTextureDescriptor cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        RenderingUtils.ReAllocateIfNeeded(ref tempTextureHandle, cameraTextureDescriptor, name: "_TempTintTexture");
 
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!settings.IsActive()) return;

        CommandBuffer cmd = CommandBufferPool.Get("Custom Tint Pass");

        // Set shader parameters
        material.SetFloat("_TintIntensity", settings.tintIntensity.value);
        material.SetColor("_TintColor", settings.tintColor.value);
        Debug.Log($"Material setting: {settings.tintColor.ToString()} @{settings.tintIntensity} intensity");
        // Blit from camera color target to temp texture and apply the tint effect
        Blitter.BlitCameraTexture(cmd, cameraColorTarget, tempTextureHandle, material, 0);

        // Blit back to the camera color target
        Blitter.BlitCameraTexture(cmd, tempTextureHandle, cameraColorTarget);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        // Release the temporary RTHandle
        if (tempTextureHandle != null)
        {
            tempTextureHandle.Release(); //release the memory for the handle
        }
    }
}


[Serializable, VolumeComponentMenu("Custom/URPCustomPostProcessTintShader")]
public class URPCustomPostProcessTint : VolumeComponent, IPostProcessComponent
{
    public URPCustomPostProcessTintSettings settings = new URPCustomPostProcessTintSettings();

    public bool IsActive() => settings.IsActive();
    public bool IsTileCompatible() => settings.IsTileCompatible();
}

/// <summary>
/// The feature is there to inject the pass into the rendering pipeline:
/// </summary>
public class URPCustomPostProcessTintFeature : ScriptableRendererFeature
{
    public Shader shader;
    private Material material;
    private URPCustomPostProcessTintPass pass;
    //public URPCustomPostProcessTintSettings settings = new URPCustomPostProcessTintSettings();

    public override void Create()
    {
        if (shader == null)
        {
            Debug.LogError("Shader missing!");
            return;
        }

        material = CoreUtils.CreateEngineMaterial(shader);
        var volumeSettings = VolumeManager.instance.stack.GetComponent<URPCustomPostProcessTint>();
        if (volumeSettings == null)
        {
            Debug.LogError("volumeSettings missing!");
            return;
        }
        Debug.Log($"(Create) setting: {volumeSettings.settings.tintColor.ToString()} @{volumeSettings.settings.tintIntensity} intensity");
        pass = new URPCustomPostProcessTintPass(material, volumeSettings.settings)
        {
            //renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            renderPassEvent = volumeSettings.settings.renderPassEvent
        };
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        base.SetupRenderPasses(renderer, renderingData);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(pass);
    }

}
