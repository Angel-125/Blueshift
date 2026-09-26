using UnityEngine;

namespace Blueshift
{
    /// <summary>
    /// Visual settings for a reusable Graviolium connection beam.
    /// </summary>
    public class GravioliumBeamSettings
    {
        /// <summary>Color of the wide plasma layer.</summary>
        public Color plasmaColor = new Color(0.0f, 0.35f, 1.0f, 0.3f);

        /// <summary>Color of the narrow electric-current layer.</summary>
        public Color currentColor = new Color(0.65f, 0.95f, 1.0f, 0.95f);

        /// <summary>Width, in meters, of the plasma layer.</summary>
        public float plasmaWidth = 0.08f;

        /// <summary>Width, in meters, of the electric-current layer.</summary>
        public float currentWidth = 0.025f;

        /// <summary>Number of points used by each line renderer.</summary>
        public int pointCount = 25;

        /// <summary>Amplitude, in meters, of the primary traveling wave.</summary>
        public float waveAmplitude = 0.055f;

        /// <summary>Number of primary wave cycles along the beam.</summary>
        public float waveCycles = 3.0f;

        /// <summary>Animation speed of the primary wave.</summary>
        public float waveSpeed = 9.0f;

        /// <summary>Amplitude, in meters, of the secondary traveling wave.</summary>
        public float secondaryWaveAmplitude = 0.018f;

        /// <summary>Number of secondary wave cycles along the beam.</summary>
        public float secondaryWaveCycles = 5.0f;

        /// <summary>Animation speed of the secondary wave.</summary>
        public float secondaryWaveSpeed = 5.5f;

        /// <summary>Whether plasma motes travel along the beam.</summary>
        public bool motesEnabled = true;

        /// <summary>Base color of the plasma motes.</summary>
        public Color moteColor = new Color(0.0f, 0.35f, 1.0f, 0.3f);

        /// <summary>
        /// Extensionless GameDatabase path of the texture applied to each mote.
        /// </summary>
        public string moteTexturePath = "WildBlueIndustries/Blueshift/FX/smoke3";

        /// <summary>Approximate number of motes per meter of beam length.</summary>
        public float moteDensity = 1.1f;

        /// <summary>Base mote travel speed along the beam, in meters per second.</summary>
        public float moteTravelSpeed = 3f;

        /// <summary>Maximum number of pooled motes.</summary>
        public int maxMotes = 8;

        /// <summary>Minimum mote width, in meters.</summary>
        public float moteMinSize = 0.165f;

        /// <summary>Maximum mote width, in meters.</summary>
        public float moteMaxSize = 0.45f;

        /// <summary>Maximum distance motes orbit from the animated beam centerline.</summary>
        public float moteOrbitRadius = 0.045f;

        /// <summary>Number of mote orbit cycles along the beam.</summary>
        public float moteOrbitCycles = 1.5f;

        /// <summary>Animation speed of the mote orbit.</summary>
        public float moteOrbitSpeed = 0.8f;

        /// <summary>Fractional variation applied to individual mote speed.</summary>
        public float moteSpeedVariation = 0.2f;

        /// <summary>Fractional variation applied to individual mote brightness.</summary>
        public float moteBrightnessVariation = 0.25f;

        /// <summary>
        /// Loads any values present in a GRAVIOLIUM_BEAM_FX config node.
        /// Values omitted from the node retain the defaults declared by this class.
        /// </summary>
        public void Load(ConfigNode node)
        {
            if (node == null)
                return;

            node.TryGetValue("plasmaColor", ref plasmaColor);
            node.TryGetValue("currentColor", ref currentColor);
            node.TryGetValue("plasmaWidth", ref plasmaWidth);
            node.TryGetValue("currentWidth", ref currentWidth);
            node.TryGetValue("pointCount", ref pointCount);
            node.TryGetValue("waveAmplitude", ref waveAmplitude);
            node.TryGetValue("waveCycles", ref waveCycles);
            node.TryGetValue("waveSpeed", ref waveSpeed);
            node.TryGetValue("secondaryWaveAmplitude", ref secondaryWaveAmplitude);
            node.TryGetValue("secondaryWaveCycles", ref secondaryWaveCycles);
            node.TryGetValue("secondaryWaveSpeed", ref secondaryWaveSpeed);
            node.TryGetValue("motesEnabled", ref motesEnabled);
            node.TryGetValue("moteColor", ref moteColor);
            node.TryGetValue("moteTexturePath", ref moteTexturePath);
            node.TryGetValue("moteDensity", ref moteDensity);
            node.TryGetValue("moteTravelSpeed", ref moteTravelSpeed);
            node.TryGetValue("maxMotes", ref maxMotes);
            node.TryGetValue("moteMinSize", ref moteMinSize);
            node.TryGetValue("moteMaxSize", ref moteMaxSize);
            node.TryGetValue("moteOrbitRadius", ref moteOrbitRadius);
            node.TryGetValue("moteOrbitCycles", ref moteOrbitCycles);
            node.TryGetValue("moteOrbitSpeed", ref moteOrbitSpeed);
            node.TryGetValue("moteSpeedVariation", ref moteSpeedVariation);
            node.TryGetValue("moteBrightnessVariation", ref moteBrightnessVariation);
        }
    }

    /// <summary>
    /// Renders a visual-only, animated Graviolium connection between two transforms.
    /// Power modules own activation and gameplay; this component only follows endpoints.
    /// </summary>
    public class WBIGravioliumBeam : MonoBehaviour
    {
        #region Housekeeping
        private const string BeamShaderName = "Legacy Shaders/Particles/Additive";

        private readonly GravioliumBeamSettings settings = new GravioliumBeamSettings();
        private LineRenderer plasmaRenderer;
        private LineRenderer currentRenderer;
        private Material plasmaMaterial;
        private Material currentMaterial;
        private Material moteMaterial;
        private string loadedMoteTexturePath;
        private Vector3[] plasmaPoints;
        private Vector3[] currentPoints;
        private LineRenderer[] moteRenderers;
        private float[] moteProgress;
        private float[] moteSpeedScales;
        private float[] moteSizes;
        private Transform sourceTransform;
        private Transform targetTransform;
        private Vector3 sourceLocalOffset;
        private Vector3 targetLocalOffset;
        private bool isPlaying;
        private float animationStartTime;
        private float lastMoteUpdateTime;
        #endregion

        #region API
        /// <summary>
        /// Indicates whether the beam is currently visible and following its endpoints.
        /// </summary>
        public bool IsPlaying
        {
            get { return isPlaying; }
        }

        /// <summary>
        /// Starts the beam or replaces its endpoints and visual settings.
        /// Local offsets allow callers to attach near a hand and at a visible target center.
        /// </summary>
        public void Play(Transform source, Vector3 sourceOffset, Transform target,
            Vector3 targetOffset, GravioliumBeamSettings visualSettings)
        {
            if (source == null || target == null)
            {
                StopEffect();
                return;
            }

            ensureRenderers();
            if (plasmaRenderer == null || currentRenderer == null)
            {
                StopEffect();
                return;
            }

            copySettings(visualSettings);
            ensurePointArrays();
            applyRendererSettings();

            sourceTransform = source;
            sourceLocalOffset = sourceOffset;
            targetTransform = target;
            targetLocalOffset = targetOffset;
            animationStartTime = Time.time;
            lastMoteUpdateTime = Time.time;
            isPlaying = true;
            plasmaRenderer.enabled = true;
            currentRenderer.enabled = true;
            initializeMotes();
            updateBeam();
        }

        /// <summary>
        /// Updates the transforms and local attachment offsets without restarting animation.
        /// </summary>
        public void SetEndpoints(Transform source, Vector3 sourceOffset,
            Transform target, Vector3 targetOffset)
        {
            sourceTransform = source;
            sourceLocalOffset = sourceOffset;
            targetTransform = target;
            targetLocalOffset = targetOffset;

            if (sourceTransform == null || targetTransform == null)
                StopEffect();
        }

        /// <summary>
        /// Hides the beam while retaining its allocated renderers, materials, and point arrays.
        /// </summary>
        public void StopEffect()
        {
            isPlaying = false;
            sourceTransform = null;
            targetTransform = null;
            if (plasmaRenderer != null)
                plasmaRenderer.enabled = false;
            if (currentRenderer != null)
                currentRenderer.enabled = false;
            setMoteRenderersEnabled(0);
        }
        #endregion

        #region Unity Lifecycle
        private void LateUpdate()
        {
            if (!isPlaying)
                return;
            if (sourceTransform == null || targetTransform == null)
            {
                StopEffect();
                return;
            }

            updateBeam();
        }

        private void OnDestroy()
        {
            StopEffect();
            if (plasmaMaterial != null)
                Destroy(plasmaMaterial);
            if (currentMaterial != null)
                Destroy(currentMaterial);
            if (moteMaterial != null)
                Destroy(moteMaterial);
        }
        #endregion

        #region Helpers
        private void ensureRenderers()
        {
            if (plasmaRenderer != null && currentRenderer != null)
                return;

            Shader beamShader = Shader.Find(BeamShaderName);
            if (beamShader == null)
                beamShader = Shader.Find("Particles/Additive");
            if (beamShader == null)
            {
                Debug.LogError("[Blueshift:WBIGravioliumBeam] Unable to find an additive " +
                    "particle shader; the beam cannot be rendered.");
                return;
            }

            GameObject plasmaObject = new GameObject("Graviolium Plasma");
            plasmaObject.transform.SetParent(transform, false);
            plasmaObject.layer = gameObject.layer;
            plasmaRenderer = plasmaObject.AddComponent<LineRenderer>();
            plasmaMaterial = new Material(beamShader);
            plasmaMaterial.name = "Graviolium Plasma Material";
            plasmaRenderer.material = plasmaMaterial;

            GameObject currentObject = new GameObject("Graviolium Current");
            currentObject.transform.SetParent(transform, false);
            currentObject.layer = gameObject.layer;
            currentRenderer = currentObject.AddComponent<LineRenderer>();
            currentMaterial = new Material(beamShader);
            currentMaterial.name = "Graviolium Current Material";
            currentRenderer.material = currentMaterial;

            configureRenderer(plasmaRenderer);
            configureRenderer(currentRenderer);
        }

        private static void configureRenderer(LineRenderer lineRenderer)
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.textureMode = LineTextureMode.Stretch;
            lineRenderer.numCapVertices = 2;
            lineRenderer.numCornerVertices = 2;
            lineRenderer.receiveShadows = false;
            lineRenderer.enabled = false;
        }

        private void copySettings(GravioliumBeamSettings visualSettings)
        {
            if (visualSettings == null)
                return;

            settings.plasmaColor = visualSettings.plasmaColor;
            settings.currentColor = visualSettings.currentColor;
            settings.plasmaWidth = Mathf.Max(0.0f, visualSettings.plasmaWidth);
            settings.currentWidth = Mathf.Max(0.0f, visualSettings.currentWidth);
            settings.pointCount = Mathf.Max(2, visualSettings.pointCount);
            settings.waveAmplitude = Mathf.Max(0.0f, visualSettings.waveAmplitude);
            settings.waveCycles = visualSettings.waveCycles;
            settings.waveSpeed = visualSettings.waveSpeed;
            settings.secondaryWaveAmplitude =
                Mathf.Max(0.0f, visualSettings.secondaryWaveAmplitude);
            settings.secondaryWaveCycles = visualSettings.secondaryWaveCycles;
            settings.secondaryWaveSpeed = visualSettings.secondaryWaveSpeed;
            settings.motesEnabled = visualSettings.motesEnabled;
            settings.moteColor = visualSettings.moteColor;
            settings.moteTexturePath = visualSettings.moteTexturePath;
            settings.moteDensity = Mathf.Max(0.0f, visualSettings.moteDensity);
            settings.moteTravelSpeed = Mathf.Max(0.0f, visualSettings.moteTravelSpeed);
            settings.maxMotes = Mathf.Max(0, visualSettings.maxMotes);
            settings.moteMinSize = Mathf.Max(0.0f, visualSettings.moteMinSize);
            settings.moteMaxSize = Mathf.Max(settings.moteMinSize,
                visualSettings.moteMaxSize);
            settings.moteOrbitRadius = Mathf.Max(0.0f, visualSettings.moteOrbitRadius);
            settings.moteOrbitCycles = visualSettings.moteOrbitCycles;
            settings.moteOrbitSpeed = visualSettings.moteOrbitSpeed;
            settings.moteSpeedVariation = Mathf.Clamp01(visualSettings.moteSpeedVariation);
            settings.moteBrightnessVariation =
                Mathf.Clamp01(visualSettings.moteBrightnessVariation);
        }

        private void ensurePointArrays()
        {
            if (currentPoints != null && currentPoints.Length == settings.pointCount)
                return;

            plasmaPoints = new Vector3[settings.pointCount];
            currentPoints = new Vector3[settings.pointCount];
            plasmaRenderer.positionCount = settings.pointCount;
            currentRenderer.positionCount = settings.pointCount;
        }

        private void applyRendererSettings()
        {
            plasmaRenderer.startColor = settings.plasmaColor;
            plasmaRenderer.endColor = settings.plasmaColor;
            plasmaRenderer.startWidth = settings.plasmaWidth;
            plasmaRenderer.endWidth = settings.plasmaWidth;

            currentRenderer.startColor = settings.currentColor;
            currentRenderer.endColor = settings.currentColor;
            currentRenderer.startWidth = settings.currentWidth;
            currentRenderer.endWidth = settings.currentWidth;

            if (!settings.motesEnabled || settings.maxMotes <= 0)
            {
                setMoteRenderersEnabled(0);
                return;
            }

            ensureMoteRenderers();
            if (moteRenderers == null)
                return;

            applyMoteTexture();
            for (int index = 0; index < moteRenderers.Length; index++)
            {
                float sizeVariation = pseudoRandom(index, 1.7f);
                float brightnessVariation = Mathf.Lerp(1.0f -
                    settings.moteBrightnessVariation,
                    1.0f + settings.moteBrightnessVariation,
                    pseudoRandom(index, 4.9f));
                Color moteColor = settings.moteColor * brightnessVariation;
                moteColor.a = Mathf.Clamp01(settings.moteColor.a * brightnessVariation);
                float moteSize = Mathf.Lerp(settings.moteMinSize,
                    settings.moteMaxSize, sizeVariation);

                moteRenderers[index].startColor = moteColor;
                moteRenderers[index].endColor = moteColor;
                moteRenderers[index].startWidth = moteSize;
                moteRenderers[index].endWidth = moteSize;
                moteSizes[index] = moteSize;
            }
        }

        private void ensureMoteRenderers()
        {
            int existingCount = moteRenderers != null ? moteRenderers.Length : 0;
            if (existingCount >= settings.maxMotes)
                return;

            LineRenderer[] newRenderers = new LineRenderer[settings.maxMotes];
            float[] newProgress = new float[settings.maxMotes];
            float[] newSpeedScales = new float[settings.maxMotes];
            float[] newSizes = new float[settings.maxMotes];
            for (int index = 0; index < existingCount; index++)
            {
                newRenderers[index] = moteRenderers[index];
                newProgress[index] = moteProgress[index];
                newSpeedScales[index] = moteSpeedScales[index];
                newSizes[index] = moteSizes[index];
            }

            if (moteMaterial == null && settings.maxMotes > 0)
            {
                moteMaterial = new Material(plasmaMaterial.shader);
                moteMaterial.name = "Graviolium Mote Material";
            }

            for (int index = existingCount; index < settings.maxMotes; index++)
            {
                GameObject moteObject = new GameObject("Graviolium Mote " + index);
                moteObject.transform.SetParent(transform, false);
                moteObject.layer = gameObject.layer;
                LineRenderer moteRenderer = moteObject.AddComponent<LineRenderer>();
                configureRenderer(moteRenderer);
                moteRenderer.positionCount = 2;
                moteRenderer.numCapVertices = 3;
                moteRenderer.sharedMaterial = moteMaterial;
                newRenderers[index] = moteRenderer;
            }

            moteRenderers = newRenderers;
            moteProgress = newProgress;
            moteSpeedScales = newSpeedScales;
            moteSizes = newSizes;
        }

        private void applyMoteTexture()
        {
            if (moteMaterial == null || loadedMoteTexturePath == settings.moteTexturePath)
                return;

            loadedMoteTexturePath = settings.moteTexturePath;
            Texture2D moteTexture = null;
            if (!string.IsNullOrEmpty(loadedMoteTexturePath) &&
                GameDatabase.Instance != null)
            {
                moteTexture = GameDatabase.Instance.GetTexture(loadedMoteTexturePath, false);
            }

            if (moteTexture == null && !string.IsNullOrEmpty(loadedMoteTexturePath))
            {
                Debug.LogWarning("[Blueshift:WBIGravioliumBeam] Mote texture not found: " +
                    loadedMoteTexturePath + ". Using an untextured mote.");
            }

            moteMaterial.mainTexture = moteTexture != null
                ? moteTexture
                : Texture2D.whiteTexture;
            if (moteMaterial.HasProperty("_TintColor"))
                moteMaterial.SetColor("_TintColor", Color.white);
        }

        private void initializeMotes()
        {
            if (moteRenderers == null)
                return;

            int moteCount = moteRenderers.Length;
            for (int index = 0; index < moteCount; index++)
            {
                float spacing = (index + 1) * 0.618034f;
                float jitter = (pseudoRandom(index, 8.3f) - 0.5f) * 0.08f;
                moteProgress[index] = Mathf.Repeat(spacing + jitter, 1.0f);
                moteSpeedScales[index] = Mathf.Lerp(1.0f - settings.moteSpeedVariation,
                    1.0f + settings.moteSpeedVariation, pseudoRandom(index, 12.1f));
                moteRenderers[index].enabled = false;
            }
        }

        private void updateBeam()
        {
            Vector3 source = sourceTransform.TransformPoint(sourceLocalOffset);
            Vector3 target = targetTransform.TransformPoint(targetLocalOffset);
            Vector3 beamVector = target - source;
            float beamLength = beamVector.magnitude;
            if (beamLength <= Mathf.Epsilon)
            {
                plasmaRenderer.enabled = false;
                currentRenderer.enabled = false;
                setMoteRenderersEnabled(0);
                return;
            }

            plasmaRenderer.enabled = true;
            currentRenderer.enabled = true;
            Vector3 direction = beamVector / beamLength;
            Vector3 reference = FlightCamera.fetch != null &&
                FlightCamera.fetch.mainCamera != null
                ? FlightCamera.fetch.mainCamera.transform.forward
                : Vector3.up;
            Vector3 waveRight = Vector3.Cross(direction, reference);
            if (waveRight.sqrMagnitude <= 0.0001f)
                waveRight = Vector3.Cross(direction, Vector3.up);
            if (waveRight.sqrMagnitude <= 0.0001f)
                waveRight = Vector3.Cross(direction, Vector3.right);
            waveRight.Normalize();
            Vector3 waveUp = Vector3.Cross(direction, waveRight).normalized;

            float animationTime = Time.time - animationStartTime;
            float twoPi = Mathf.PI * 2.0f;
            int lastPoint = settings.pointCount - 1;
            for (int index = 0; index < settings.pointCount; index++)
            {
                float progress = (float)index / lastPoint;
                Vector3 currentOffset = getWaveOffset(progress, waveRight, waveUp,
                    animationTime, twoPi);
                Vector3 center = Vector3.Lerp(source, target, progress);

                currentPoints[index] = center + currentOffset;
                plasmaPoints[index] = center + currentOffset * 0.35f;
            }

            plasmaRenderer.SetPositions(plasmaPoints);
            currentRenderer.SetPositions(currentPoints);
            updateMotes(source, target, beamLength, waveRight, waveUp,
                animationTime, twoPi);
        }

        private Vector3 getWaveOffset(float progress, Vector3 waveRight,
            Vector3 waveUp, float animationTime, float twoPi)
        {
            float endpointEnvelope = Mathf.Sin(progress * Mathf.PI);
            float primaryWave = Mathf.Sin(progress * settings.waveCycles * twoPi -
                animationTime * settings.waveSpeed) * settings.waveAmplitude;
            float secondaryWave = Mathf.Sin(progress * settings.secondaryWaveCycles *
                twoPi + animationTime * settings.secondaryWaveSpeed) *
                settings.secondaryWaveAmplitude;
            return endpointEnvelope *
                (waveRight * primaryWave + waveUp * secondaryWave);
        }

        private void updateMotes(Vector3 source, Vector3 target, float beamLength,
            Vector3 waveRight, Vector3 waveUp, float animationTime, float twoPi)
        {
            if (!settings.motesEnabled || settings.maxMotes <= 0 ||
                settings.moteDensity <= 0.0f ||
                settings.moteTravelSpeed <= 0.0f || moteRenderers == null)
            {
                setMoteRenderersEnabled(0);
                return;
            }

            int activeMoteCount = Mathf.Clamp(
                Mathf.CeilToInt(beamLength * settings.moteDensity),
                1, Mathf.Min(settings.maxMotes, moteRenderers.Length));
            float moteDeltaTime = Mathf.Max(0.0f, Time.time - lastMoteUpdateTime);
            lastMoteUpdateTime = Time.time;

            for (int index = 0; index < activeMoteCount; index++)
            {
                moteProgress[index] = Mathf.Repeat(moteProgress[index] +
                    moteDeltaTime * settings.moteTravelSpeed *
                    moteSpeedScales[index] / beamLength, 1.0f);
                float headProgress = moteProgress[index];
                float moteProgressLength = moteSizes[index] / beamLength;
                float tailProgress = Mathf.Max(0.0f, headProgress - moteProgressLength);
                Vector3 head = Vector3.Lerp(source, target, headProgress) +
                    getMoteOffset(headProgress, index, waveRight, waveUp,
                        animationTime, twoPi);
                Vector3 tail = Vector3.Lerp(source, target, tailProgress) +
                    getMoteOffset(tailProgress, index, waveRight, waveUp,
                        animationTime, twoPi);

                LineRenderer moteRenderer = moteRenderers[index];
                moteRenderer.enabled = true;
                moteRenderer.SetPosition(0, tail);
                moteRenderer.SetPosition(1, head);
            }

            setMoteRenderersEnabled(activeMoteCount);
        }

        private Vector3 getMoteOffset(float progress, int moteIndex,
            Vector3 waveRight, Vector3 waveUp, float animationTime, float twoPi)
        {
            Vector3 waveOffset = getWaveOffset(progress, waveRight, waveUp,
                animationTime, twoPi);
            float orbitEnvelope = Mathf.Sin(progress * Mathf.PI);
            float orbitPhase = pseudoRandom(moteIndex, 20.3f) * twoPi;
            float orbitAngle = progress * settings.moteOrbitCycles * twoPi +
                animationTime * settings.moteOrbitSpeed + orbitPhase;
            Vector3 orbitOffset = (waveRight * Mathf.Cos(orbitAngle) +
                waveUp * Mathf.Sin(orbitAngle)) * settings.moteOrbitRadius * orbitEnvelope;
            return waveOffset + orbitOffset;
        }

        private void setMoteRenderersEnabled(int enabledCount)
        {
            if (moteRenderers == null)
                return;

            for (int index = enabledCount; index < moteRenderers.Length; index++)
                moteRenderers[index].enabled = false;
        }

        private static float pseudoRandom(int index, float seed)
        {
            float value = Mathf.Sin((index + 1) * 12.9898f + seed) * 43758.5453f;
            return value - Mathf.Floor(value);
        }
        #endregion
    }
}
