using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

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

    private CopyDepthPass c;
    private Material material;
    private URPCustomPostProcessTintSettings settings;
    //old
    private RTHandle cameraColorTarget; 
    private RTHandle tempTextureHandle; //storing texture
    //new
    private RTHandle source;      //cameraColorTarget?? same?
    private RTHandle destination; //tempTextureHandle?? same?

    public URPCustomPostProcessTintPass(Material material, URPCustomPostProcessTintSettings settings)
    {
        this.material = material;
        this.settings = settings;
        Debug.Log($"(Construktor) Pass setting: {settings.tintColor.ToString()} @{settings.tintIntensity} intensity");
    }

    public void Setup(RTHandle sourceTRHandle, RTHandle destinationRTHandle)
    {
        this.source = sourceTRHandle; //cameraDepthTargetHandle
        this.destination = destinationRTHandle; //m_DepthRTHandle
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        //in Sample from Unity done in SetupRenderPasses


        // Setup the camera color target using RTHandle (they are stored in the pass-class)
        this.cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
        // Create a temporary RTHandle for processing
        RenderTextureDescriptor cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        RenderingUtils.ReAllocateIfNeeded(ref this.tempTextureHandle, cameraTextureDescriptor, name: "_TempTintTexture");
 
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

    private RTHandle m_SCR_Handle;
    private const string k_DepthRTName = "_URPCustomPostProcessingTintTexture"; //name of texture (just unique for feature)

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
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques
        };
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        // Create an RTHandle for storing the depth
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.graphicsFormat = GraphicsFormat.None;
        desc.msaaSamples = 1; //???

        RenderingUtils.ReAllocateIfNeeded(ref m_SCR_Handle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_DepthRTName);
        pass.Setup(renderer.cameraColorTargetHandle, m_SCR_Handle); //connect pass with camera and texture

        base.SetupRenderPasses(renderer, renderingData);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType != CameraType.Game)
            //do not render in editor
            return;

        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        //added disposal of variables
        m_SCR_Handle?.Release();     //clear RTHandle
        CoreUtils.Destroy(material); //dispose material
        //reset pass vars
        pass = null;
        base.Dispose(disposing);
    }

}
