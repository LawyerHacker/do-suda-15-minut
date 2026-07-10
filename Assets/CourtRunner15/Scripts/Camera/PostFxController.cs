using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace CourtRunner15
{
    /// <summary>
    /// Пост-обработка (Post-Processing Stack v2, Built-in pipeline).
    /// Профиль собирается кодом: bloom — главный носитель «золотого» свечения
    /// пикапов, лучей и ламп; тёплый color grading (ACES) под golden hour;
    /// виньетка. Ambient Occlusion намеренно выключен: на GTX 1050 Ti при
    /// 2560x1080 он съедает слишком много бюджета кадра.
    /// Требует PostProcessResources.asset в Resources (копируется
    /// редакторским скриптом CourtRunnerRuntimeAssets).
    /// </summary>
    public static class PostFxController
    {
        public static bool Active { get; private set; }

        private const string ResourcesAsset = "PostProcessResources";
        // TransparentFX: встроенный слой, есть в любом проекте, геометрией не занят.
        private const int VolumeObjectLayer = 1;
        private const bool EnableAmbientOcclusion = false; // включать только после замера FPS

        public static void Attach(Camera cam)
        {
            if (cam == null) return;
            if (cam.GetComponent<PostProcessLayer>() != null)
            {
                Active = true;
                return;
            }

            PostProcessResources resources = Resources.Load<PostProcessResources>(ResourcesAsset);
            if (resources == null)
            {
                Debug.LogWarning("[CourtRunner15] PostProcessResources не найден в Resources — пост-обработка выключена.");
                return;
            }

            PostProcessProfile profile = ScriptableObject.CreateInstance<PostProcessProfile>();

            Bloom bloom = profile.AddSettings<Bloom>();
            bloom.enabled.Override(true);
            bloom.intensity.Override(2.6f);
            bloom.threshold.Override(1.05f); // светится только то, что ярче 1.0: эмиссив, неон, блики
            bloom.softKnee.Override(0.55f);
            bloom.diffusion.Override(8.4f);
            bloom.color.Override(new Color(1f, 0.94f, 0.80f));
            bloom.fastMode.Override(true); // половинное разрешение — бюджет GTX 1050 Ti

            ColorGrading grading = profile.AddSettings<ColorGrading>();
            grading.enabled.Override(true);
            grading.gradingMode.Override(GradingMode.HighDefinitionRange);
            grading.tonemapper.Override(Tonemapper.ACES);
            grading.temperature.Override(12f);
            grading.tint.Override(2f);
            grading.postExposure.Override(0.25f);
            grading.saturation.Override(8f);
            grading.contrast.Override(12f);
            grading.gain.Override(new Vector4(1.06f, 1.0f, 0.9f, 0f)); // света — в золото

            Vignette vignette = profile.AddSettings<Vignette>();
            vignette.enabled.Override(true);
            vignette.intensity.Override(0.30f);
            vignette.smoothness.Override(0.45f);

            if (EnableAmbientOcclusion)
            {
                AmbientOcclusion ao = profile.AddSettings<AmbientOcclusion>();
                ao.enabled.Override(true);
                ao.mode.Override(AmbientOcclusionMode.ScalableAmbientObscurance);
                ao.intensity.Override(0.55f);
            }

            GameObject volumeGo = new GameObject("PostFxVolume");
            volumeGo.layer = VolumeObjectLayer;
            PostProcessVolume volume = volumeGo.AddComponent<PostProcessVolume>();
            volume.sharedProfile = profile;
            volume.isGlobal = true;
            volume.priority = 100f;

            PostProcessLayer layer = cam.gameObject.AddComponent<PostProcessLayer>();
            layer.Init(resources);
            layer.volumeTrigger = cam.transform;
            layer.volumeLayer = 1 << VolumeObjectLayer;
            layer.antialiasingMode = PostProcessLayer.Antialiasing.None; // геометрию сглаживает MSAA 2x
            layer.stopNaNPropagation = true;

            Active = true;
            Debug.Log("[CourtRunner15] PostFX active: bloom + warm ACES grading + vignette; AO=off (perf).");
        }
    }
}
