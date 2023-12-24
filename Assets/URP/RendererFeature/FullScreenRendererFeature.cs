using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class FullScreenRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Material m_material;

    FullScreenPass m_fullScreenPass = null;

    public override void AddRenderPasses(ScriptableRenderer renderer,
                                    ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
            renderer.EnqueuePass(m_fullScreenPass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer,
                                        in RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            // ensure depth and normal textures are available
            m_fullScreenPass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);

            m_fullScreenPass.SetTarget(renderer.cameraColorTargetHandle);
        }
    }

    public override void Create()
    {
        m_fullScreenPass = new FullScreenPass(m_material);
    }

    class FullScreenPass : ScriptableRenderPass
    {
        ProfilingSampler m_profilingSampler = new ProfilingSampler("FullScreen");
        Material m_material;
        RTHandle m_cameraColorTarget;

        public FullScreenPass(Material material)
        {
            m_material = material;
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        public void SetTarget(RTHandle colorHandle)
        {
            m_cameraColorTarget = colorHandle;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureTarget(m_cameraColorTarget);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            if (cameraData.camera.cameraType != CameraType.Game)
                return;

            if (m_material == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_profilingSampler))
            {
                Blitter.BlitCameraTexture(cmd, m_cameraColorTarget, m_cameraColorTarget, m_material, 0);
            }
            context.ExecuteCommandBuffer(cmd);

            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }
    }
}