// ORIGINAL TEMPLATES:
// https://github.com/Unity-Technologies/UniversalRenderingExamples/blob/cc4f/Assets/Scripts/Runtime/RenderPasses/DrawFullscreenFeature.cs
// https://github.com/Unity-Technologies/UniversalRenderingExamples/blob/cc4f/Assets/Scripts/Runtime/RenderPasses/DrawFullscreenPass.cs

namespace UnityEngine.Rendering.Universal
{
    public enum BufferType
    {
        CameraColor,
        Custom
    }

    public class PostProcess : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

            public BufferType sourceType = BufferType.CameraColor;
            public BufferType destinationType = BufferType.CameraColor;
            public string sourceTextureId = "_SourceTexture";
            public string destinationTextureId = "_DestinationTexture";

            public ComputeShader postProcessShaderH = null;
            public ComputeShader postProcessShaderV = null;

            public bool postProcessing = false;
        }

        public Settings settings = new Settings();
        PostProcessPass m_ScriptablePass;

        public override void Create()
        {
            m_ScriptablePass = new PostProcessPass(name);

            // Configures where the render pass should be injected.
            m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.renderPassEvent = settings.renderPassEvent;
            m_ScriptablePass.settings = settings;
            m_ScriptablePass.Setup(renderer.cameraColorTarget, renderer.cameraDepth);

            renderer.EnqueuePass(m_ScriptablePass);
        }
    }

    internal class PostProcessPass : ScriptableRenderPass
    {
        public FilterMode filterMode { get; set; }
        public PostProcess.Settings settings;

        RenderTargetIdentifier source;
        RenderTargetIdentifier destination;
        int temporaryRTId = Shader.PropertyToID("_TempRT");

        int sourceId;
        int destinationId;
        bool isSourceAndDestinationSameTarget;

        string m_ProfilerTag;

        public PostProcessPass(string tag)
        {
            m_ProfilerTag = tag;
        }

        // kernel pointers
        private int watercolorHKernelIndex;
        private int watercolorVKernelIndex;

        // shader resource slots
        private int mainCameraOutputTexSlot;
        private int inputTexSlot;
        private int outputUavSlot;

        // shader resources
        private RenderTargetIdentifier depthTex;
        private RenderTexture[] gOutput = new RenderTexture[2];
        private Texture2D noiseTex = TextureCreator.PerlinClouds(512, 0);

        public void Setup(RenderTargetIdentifier sourceColor, RenderTargetIdentifier sourceDepth)
        {
            source = sourceColor;
            depthTex = sourceDepth;
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            watercolorHKernelIndex = settings.postProcessShaderH.FindKernel("main");
            watercolorVKernelIndex = settings.postProcessShaderV.FindKernel("main");

            mainCameraOutputTexSlot = Shader.PropertyToID("gCameraOutput");
            inputTexSlot = Shader.PropertyToID("gPostInput");
            outputUavSlot = Shader.PropertyToID("gPostOutput");
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            blitTargetDescriptor.depthBufferBits = 0;

            isSourceAndDestinationSameTarget = settings.sourceType == settings.destinationType &&
                (settings.sourceType == BufferType.CameraColor || settings.sourceTextureId == settings.destinationTextureId);

            if (settings.sourceType == BufferType.CameraColor) {
                sourceId = -1;
            } else {
                sourceId = Shader.PropertyToID(settings.sourceTextureId);
                cmd.GetTemporaryRT(sourceId, blitTargetDescriptor, filterMode);
                source = new RenderTargetIdentifier(sourceId);
            }

            //if (isSourceAndDestinationSameTarget) {
            //    destinationId = temporaryRTId;
            //    cmd.GetTemporaryRT(destinationId, blitTargetDescriptor, filterMode);
            //    destination = new RenderTargetIdentifier(destinationId);
            //} else
            if (settings.destinationType == BufferType.CameraColor) {
                destinationId = -1;
                destination = RenderTargetHandle.CameraTarget.Identifier();
            } else {
                destinationId = Shader.PropertyToID(settings.destinationTextureId);
                cmd.GetTemporaryRT(destinationId, blitTargetDescriptor, filterMode);
                destination = new RenderTargetIdentifier(destinationId);
            }

            if (!settings.postProcessing) {
                Blit(cmd, source, destination);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
                return;
            }

            int width = blitTargetDescriptor.width;
            int height = blitTargetDescriptor.height;

            for (int i = 0; i < gOutput.Length; i++) {
                gOutput[i] = createIfInvalid(gOutput[i], width, height, true, false);
            }

            // Set cbuffer values
            cmd.SetGlobalInt("screenWidth", width);
            cmd.SetGlobalInt("screenHeight", height);
            cmd.SetGlobalTexture("NoiseTex", noiseTex);

            const int DispatchGroupWidth = 64;

            // pass 0
            {
                cmd.SetComputeTextureParam(settings.postProcessShaderH, watercolorHKernelIndex, mainCameraOutputTexSlot, source);
                cmd.SetComputeTextureParam(settings.postProcessShaderH, watercolorHKernelIndex, inputTexSlot, depthTex);
                cmd.SetComputeTextureParam(settings.postProcessShaderH, watercolorHKernelIndex, outputUavSlot, gOutput[0]);
                cmd.DispatchCompute(settings.postProcessShaderH, watercolorHKernelIndex, MathUtil.DivideByMultiple(width, DispatchGroupWidth), height, 1);
            }

            // pass 1
            {
                cmd.SetComputeTextureParam(settings.postProcessShaderV, watercolorVKernelIndex, mainCameraOutputTexSlot, source);
                cmd.SetComputeTextureParam(settings.postProcessShaderV, watercolorVKernelIndex, inputTexSlot, gOutput[0]);
                cmd.SetComputeTextureParam(settings.postProcessShaderV, watercolorVKernelIndex, outputUavSlot, gOutput[1]);
                cmd.DispatchCompute(settings.postProcessShaderV, watercolorVKernelIndex, width, MathUtil.DivideByMultiple(height, DispatchGroupWidth), 1);
            }

            Blit(cmd, gOutput[1], source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private RenderTexture createIfInvalid(RenderTexture current, int width, int height, bool enableRandomWrite = false, bool isLinearSpace = true)
        {
            if (current == null || current.width != width || current.height != height) {
                current?.Release();
                current = new RenderTexture(width, height, 1, RenderTextureFormat.ARGBFloat, isLinearSpace ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
                current.enableRandomWrite = enableRandomWrite;
                current.Create();
            }
            return current;
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (destinationId != -1)
                cmd.ReleaseTemporaryRT(destinationId);

            if (source == destination && sourceId != -1)
                cmd.ReleaseTemporaryRT(sourceId);

            for (int i = 0; i < gOutput.Length; i++) {
                if (gOutput[i] != null) {
                    gOutput[i].Release();
                    gOutput[i] = null;
                }
            }
        }
    }
}
