using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace GemTD.Rendering
{
    public sealed class MoebiusStyleFeature : ScriptableRendererFeature
    {
        sealed class MoebiusStylePass : ScriptableRenderPass
        {
            static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
            static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
            static readonly int HatchTexId = Shader.PropertyToID("_HatchTex");
            static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
            static readonly int OutlineThicknessId = Shader.PropertyToID("_OutlineThickness");
            static readonly int DepthThresholdId = Shader.PropertyToID("_DepthThreshold");
            static readonly int NormalThresholdId = Shader.PropertyToID("_NormalThreshold");
            static readonly int HatchTilingId = Shader.PropertyToID("_HatchTiling");
            static readonly int HatchIntensityId = Shader.PropertyToID("_HatchIntensity");
            static readonly int KeepBrightCutoffId = Shader.PropertyToID("_KeepBrightCutoff");
            static readonly int DesaturateId = Shader.PropertyToID("_Desaturate");

            static readonly MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();

            Material _material;
            Texture2D _hatchTexture;
            Color _outlineColor;
            float _outlineThickness;
            float _depthThreshold;
            float _normalThreshold;
            float _hatchTiling;
            float _hatchIntensity;
            float _keepBrightCutoff;
            float _desaturate;

            public MoebiusStylePass()
            {
                profilingSampler = new ProfilingSampler("Moebius Style");
                requiresIntermediateTexture = true;
            }

            public void Setup(
                Material material,
                Texture2D hatchTexture,
                Color outlineColor,
                float outlineThickness,
                float depthThreshold,
                float normalThreshold,
                float hatchTiling,
                float hatchIntensity,
                float keepBrightCutoff,
                float desaturate)
            {
                _material = material;
                _hatchTexture = hatchTexture;
                _outlineColor = outlineColor;
                _outlineThickness = outlineThickness;
                _depthThreshold = depthThreshold;
                _normalThreshold = normalThreshold;
                _hatchTiling = hatchTiling;
                _hatchIntensity = hatchIntensity;
                _keepBrightCutoff = keepBrightCutoff;
                _desaturate = desaturate;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer)
                    return;

                var source = resourceData.activeColorTexture;
                var destinationDesc = renderGraph.GetTextureDesc(source);
                destinationDesc.name = "MoebiusStyleColor";
                destinationDesc.clearBuffer = false;
                TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Moebius Style", out var passData, profilingSampler))
                {
                    passData.material = _material;
                    passData.inputTexture = source;
                    passData.hatchTexture = _hatchTexture;
                    passData.outlineColor = _outlineColor;
                    passData.outlineThickness = _outlineThickness;
                    passData.depthThreshold = _depthThreshold;
                    passData.normalThreshold = _normalThreshold;
                    passData.hatchTiling = _hatchTiling;
                    passData.hatchIntensity = _hatchIntensity;
                    passData.keepBrightCutoff = _keepBrightCutoff;
                    passData.desaturate = _desaturate;

                    builder.UseTexture(passData.inputTexture, AccessFlags.Read);

                    if (resourceData.cameraDepthTexture.IsValid())
                        builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                    if (resourceData.cameraNormalsTexture.IsValid())
                        builder.UseTexture(resourceData.cameraNormalsTexture, AccessFlags.Read);

                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.SetRenderFunc(static (PassData data, RasterGraphContext context) => ExecutePass(data, context));
                }

                resourceData.cameraColor = destination;
            }

            static void ExecutePass(PassData data, RasterGraphContext context)
            {
                PropertyBlock.Clear();
                PropertyBlock.SetTexture(BlitTextureId, data.inputTexture);
                PropertyBlock.SetVector(BlitScaleBiasId, new Vector4(1f, 1f, 0f, 0f));
                if (data.hatchTexture != null)
                    PropertyBlock.SetTexture(HatchTexId, data.hatchTexture);

                PropertyBlock.SetColor(OutlineColorId, data.outlineColor);
                PropertyBlock.SetFloat(OutlineThicknessId, data.outlineThickness);
                PropertyBlock.SetFloat(DepthThresholdId, data.depthThreshold);
                PropertyBlock.SetFloat(NormalThresholdId, data.normalThreshold);
                PropertyBlock.SetFloat(HatchTilingId, data.hatchTiling);
                PropertyBlock.SetFloat(HatchIntensityId, data.hatchIntensity);
                PropertyBlock.SetFloat(KeepBrightCutoffId, data.keepBrightCutoff);
                PropertyBlock.SetFloat(DesaturateId, data.desaturate);

                context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3, 1, PropertyBlock);
            }

            class PassData
            {
                internal Material material;
                internal TextureHandle inputTexture;
                internal Texture2D hatchTexture;
                internal Color outlineColor;
                internal float outlineThickness;
                internal float depthThreshold;
                internal float normalThreshold;
                internal float hatchTiling;
                internal float hatchIntensity;
                internal float keepBrightCutoff;
                internal float desaturate;
            }
        }

        [SerializeField] Shader shader;
        [SerializeField] Texture2D hatchTexture;
        [SerializeField] Color outlineColor = Color.black;
        [SerializeField, Range(0.2f, 6f)] float outlineThickness = 1.4f;
        [SerializeField, Range(0.1f, 8f)] float depthThreshold = 1.6f;
        [SerializeField, Range(0.1f, 8f)] float normalThreshold = 1.25f;
        [SerializeField, Range(1f, 32f)] float hatchTiling = 8f;
        [SerializeField, Range(0f, 1f)] float hatchIntensity = 0.85f;
        [SerializeField, Range(0f, 1f)] float keepBrightCutoff = 0.88f;
        [SerializeField, Range(0f, 1f)] float desaturate;

        MoebiusStylePass _pass;
        Material _material;

        public override void Create()
        {
            _pass = new MoebiusStylePass
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Preview
                || renderingData.cameraData.cameraType == CameraType.Reflection)
                return;

            if (!EnsureMaterial())
                return;

            _pass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
            _pass.Setup(
                _material,
                hatchTexture,
                outlineColor,
                outlineThickness,
                depthThreshold,
                normalThreshold,
                hatchTiling,
                hatchIntensity,
                keepBrightCutoff,
                desaturate);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
            _material = null;
        }

        bool EnsureMaterial()
        {
            if (shader == null)
                shader = Shader.Find("Hidden/GemTD/MoebiusStyle");
            if (shader == null)
                return false;
            if (_material == null)
                _material = CoreUtils.CreateEngineMaterial(shader);
            return _material != null;
        }
    }
}
