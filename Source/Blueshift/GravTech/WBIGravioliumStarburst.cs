using UnityEngine;

namespace Blueshift
{
    /// <summary>
    /// Visual settings for a reusable Graviolium radial impact spray.
    /// </summary>
    public class GravioliumStarburstSettings
    {
        /// <summary>Whether callers should display the starburst.</summary>
        public bool enabled = true;

        /// <summary>Extensionless GameDatabase path of the particle texture.</summary>
        public string particleTexturePath = "WildBlueIndustries/Blueshift/FX/smoke3";

        /// <summary>Base electric-blue particle tint.</summary>
        public Color particleColor = new Color(0.0f, 0.35f, 1.0f, 0.3f);

        /// <summary>Particles emitted per second.</summary>
        public float emissionRate = 24.0f;

        /// <summary>Maximum simultaneously living particles.</summary>
        public int maxParticles = 32;

        /// <summary>Minimum particle lifetime, in seconds.</summary>
        public float minLifetime = 0.18f;

        /// <summary>Maximum particle lifetime, in seconds.</summary>
        public float maxLifetime = 0.35f;

        /// <summary>Minimum particle size, in meters.</summary>
        public float minSize = 0.05f;

        /// <summary>Maximum particle size, in meters.</summary>
        public float maxSize = 0.15f;

        /// <summary>Minimum radial spray speed, in meters per second.</summary>
        public float minRadialSpeed = 0.45f;

        /// <summary>Maximum radial spray speed, in meters per second.</summary>
        public float maxRadialSpeed = 1.0f;

        /// <summary>Maximum speed back toward the source after impact.</summary>
        public float backscatterSpeed = 0.2f;

        /// <summary>Fractional variation applied to particle brightness.</summary>
        public float brightnessVariation = 0.2f;

        /// <summary>
        /// Loads any values present in a GRAVIOLIUM_STARBURST_FX config node.
        /// Values omitted from the node retain the defaults declared by this class.
        /// </summary>
        public void Load(ConfigNode node)
        {
            if (node == null)
                return;

            node.TryGetValue("enabled", ref enabled);
            node.TryGetValue("particleTexturePath", ref particleTexturePath);
            node.TryGetValue("particleColor", ref particleColor);
            node.TryGetValue("emissionRate", ref emissionRate);
            node.TryGetValue("maxParticles", ref maxParticles);
            node.TryGetValue("minLifetime", ref minLifetime);
            node.TryGetValue("maxLifetime", ref maxLifetime);
            node.TryGetValue("minSize", ref minSize);
            node.TryGetValue("maxSize", ref maxSize);
            node.TryGetValue("minRadialSpeed", ref minRadialSpeed);
            node.TryGetValue("maxRadialSpeed", ref maxRadialSpeed);
            node.TryGetValue("backscatterSpeed", ref backscatterSpeed);
            node.TryGetValue("brightnessVariation", ref brightnessVariation);
        }
    }

    /// <summary>
    /// Renders a visual-only Graviolium impact spray at a caller-supplied pose.
    /// </summary>
    public class WBIGravioliumStarburst : MonoBehaviour
    {
        #region Housekeeping
        private const string ParticleShaderName = "Legacy Shaders/Particles/Additive";

        private readonly GravioliumStarburstSettings settings =
            new GravioliumStarburstSettings();
        private ParticleSystem particleSystemFX;
        private ParticleSystemRenderer particleRenderer;
        private Material particleMaterial;
        private Vector3 impactPosition;
        private Vector3 impactNormal = Vector3.forward;
        private float emissionAccumulator;
        private bool isPlaying;
        private uint randomState = 0x7f4a7c15u;
        #endregion

        #region API
        /// <summary>Indicates whether the starburst is emitting.</summary>
        public bool IsPlaying
        {
            get { return isPlaying; }
        }

        /// <summary>
        /// Starts emitting at a world-space position. The normal is perpendicular to the
        /// radial spray plane and should point from the source toward the impact.
        /// </summary>
        public void Play(Vector3 position, Vector3 normal,
            GravioliumStarburstSettings visualSettings)
        {
            if (visualSettings == null || !visualSettings.enabled)
            {
                StopEffect();
                return;
            }

            copySettings(visualSettings);
            ensureParticleSystem();
            if (particleSystemFX == null)
                return;

            applySettings();
            SetPose(position, normal);
            emissionAccumulator = 0.0f;
            isPlaying = true;
            particleSystemFX.Play(false);
        }

        /// <summary>Updates the world-space impact position and spray-plane normal.</summary>
        public void SetPose(Vector3 position, Vector3 normal)
        {
            impactPosition = position;
            if (normal.sqrMagnitude > Mathf.Epsilon)
                impactNormal = normal.normalized;

            transform.position = impactPosition;
            transform.rotation = Quaternion.LookRotation(impactNormal);
        }

        /// <summary>Stops emission and immediately removes all living particles.</summary>
        public void StopEffect()
        {
            isPlaying = false;
            emissionAccumulator = 0.0f;
            if (particleSystemFX != null)
            {
                particleSystemFX.Stop(true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (!isPlaying || particleSystemFX == null)
                return;

            emissionAccumulator += settings.emissionRate * Time.deltaTime;
            int particlesToEmit = Mathf.Min(8, Mathf.FloorToInt(emissionAccumulator));
            emissionAccumulator -= particlesToEmit;
            for (int index = 0; index < particlesToEmit; index++)
                emitParticle();
        }

        private void OnDestroy()
        {
            StopEffect();
            if (particleMaterial != null)
                Destroy(particleMaterial);
        }
        #endregion

        #region Helpers
        private void ensureParticleSystem()
        {
            if (particleSystemFX != null)
                return;

            Shader particleShader = Shader.Find(ParticleShaderName);
            if (particleShader == null)
                particleShader = Shader.Find("Particles/Additive");
            if (particleShader == null)
            {
                Debug.LogError("[Blueshift:WBIGravioliumStarburst] Unable to find an " +
                    "additive particle shader; the starburst cannot be rendered.");
                return;
            }

            particleSystemFX = gameObject.AddComponent<ParticleSystem>();
            particleRenderer = gameObject.GetComponent<ParticleSystemRenderer>();
            particleMaterial = new Material(particleShader);
            particleMaterial.name = "Graviolium Starburst Material";
            particleRenderer.sharedMaterial = particleMaterial;
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sortMode = ParticleSystemSortMode.YoungestInFront;

            ParticleSystem.MainModule main = particleSystemFX.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0.0f;
            ParticleSystem.EmissionModule emission = particleSystemFX.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = particleSystemFX.shape;
            shape.enabled = false;
            particleSystemFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void copySettings(GravioliumStarburstSettings visualSettings)
        {
            if (visualSettings == null)
                return;

            settings.particleTexturePath = visualSettings.particleTexturePath;
            settings.particleColor = visualSettings.particleColor;
            settings.emissionRate = Mathf.Max(0.0f, visualSettings.emissionRate);
            settings.maxParticles = Mathf.Max(1, visualSettings.maxParticles);
            settings.minLifetime = Mathf.Max(0.01f, visualSettings.minLifetime);
            settings.maxLifetime = Mathf.Max(settings.minLifetime,
                visualSettings.maxLifetime);
            settings.minSize = Mathf.Max(0.0f, visualSettings.minSize);
            settings.maxSize = Mathf.Max(settings.minSize, visualSettings.maxSize);
            settings.minRadialSpeed = Mathf.Max(0.0f, visualSettings.minRadialSpeed);
            settings.maxRadialSpeed = Mathf.Max(settings.minRadialSpeed,
                visualSettings.maxRadialSpeed);
            settings.backscatterSpeed = Mathf.Max(0.0f,
                visualSettings.backscatterSpeed);
            settings.brightnessVariation =
                Mathf.Clamp01(visualSettings.brightnessVariation);
        }

        private void applySettings()
        {
            ParticleSystem.MainModule main = particleSystemFX.main;
            main.maxParticles = settings.maxParticles;

            Texture2D particleTexture = null;
            if (!string.IsNullOrEmpty(settings.particleTexturePath) &&
                GameDatabase.Instance != null)
            {
                particleTexture = GameDatabase.Instance.GetTexture(
                    settings.particleTexturePath, false);
            }

            if (particleTexture == null && !string.IsNullOrEmpty(settings.particleTexturePath))
            {
                Debug.LogWarning("[Blueshift:WBIGravioliumStarburst] Particle texture " +
                    "not found: " + settings.particleTexturePath +
                    ". Using an untextured particle.");
            }

            particleMaterial.mainTexture = particleTexture != null
                ? particleTexture
                : Texture2D.whiteTexture;
            if (particleMaterial.HasProperty("_TintColor"))
                particleMaterial.SetColor("_TintColor", Color.white);
        }

        private void emitParticle()
        {
            Vector3 sprayRight = Vector3.Cross(impactNormal, Vector3.up);
            if (sprayRight.sqrMagnitude <= 0.0001f)
                sprayRight = Vector3.Cross(impactNormal, Vector3.right);
            sprayRight.Normalize();
            Vector3 sprayUp = Vector3.Cross(impactNormal, sprayRight).normalized;

            float sprayAngle = nextRandom() * Mathf.PI * 2.0f;
            Vector3 radialDirection = sprayRight * Mathf.Cos(sprayAngle) +
                sprayUp * Mathf.Sin(sprayAngle);
            float radialSpeed = Mathf.Lerp(settings.minRadialSpeed,
                settings.maxRadialSpeed, nextRandom());
            float backscatter = settings.backscatterSpeed * nextRandom();
            float brightness = Mathf.Lerp(1.0f - settings.brightnessVariation,
                1.0f + settings.brightnessVariation, nextRandom());
            Color particleColor = settings.particleColor * brightness;
            particleColor.a = Mathf.Clamp01(settings.particleColor.a * brightness);

            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            emitParams.position = impactPosition;
            emitParams.velocity = radialDirection * radialSpeed -
                impactNormal * backscatter;
            emitParams.startLifetime = Mathf.Lerp(settings.minLifetime,
                settings.maxLifetime, nextRandom());
            emitParams.startSize = Mathf.Lerp(settings.minSize,
                settings.maxSize, nextRandom());
            emitParams.startColor = particleColor;
            particleSystemFX.Emit(emitParams, 1);
        }

        private float nextRandom()
        {
            randomState = randomState * 1664525u + 1013904223u;
            return (randomState & 0x00ffffffu) / 16777215.0f;
        }
        #endregion
    }
}
