using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;
using KSP.Localization;

/*
Source code copyright 2021, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace Blueshift
{
    /// <summary>
    /// This class lets you animate textures by displaying a series of images in sequence. You can animate a material's diffuse and emissive texture. You include several textures that
    /// act as the individual animation frames, and the part module will show them in sequence. This is NOT as efficient as a texture strip but it's the best I can do for now, and
    /// it's easier to set up the UV maps on the meshes being animated.
    /// </summary>
    public class WBIAnimatedTexture: WBIPartModule
    {
        #region Fields
        /// <summary>
        /// A flag to enable/disable debug mode.
        /// </summary>
        [KSPField]
        public bool debugMode = false;

        /// <summary>
        /// The ID of the part module. Since parts can have multiple animated textures, this field helps identify them.
        /// </summary>
        [KSPField]
        public string moduleID = string.Empty;

        /// <summary>
        /// Name of the transform whose textures will be animated.
        /// </summary>
        [KSPField]
        public string textureTransformName = string.Empty;

        /// <summary>
        /// The name of the animated texture, like "WarpPlasma." The actual textures should be numbered in sequence (WarpPlasma1, WarpPlasma2, etc). 
        /// </summary>
        [KSPField]
        public string animatedEmissiveTexture = string.Empty;

        /// <summary>
        /// The name of the diffuse texture. It too can be animated.
        /// </summary>
        [KSPField]
        public string animatedDiffuseTexture = string.Empty;

        /// <summary>
        /// The minimum animation speed.
        /// </summary>
        [KSPField]
        public float minFramesPerSecond = 0f;

        /// <summary>
        /// The maximum animation speed. Testing shows that with frame updates happening every 0.02 seconds, that corresponds to 50 frames per second.
        /// </summary>
        [KSPField]
        public float maxFramesPerSecond = 50f;

        /// <summary>
        /// In seconds, how fast should the emissive fade when the animation isn't activated.
        /// </summary>
        [KSPField]
        public float emissiveFadeTime = 3.0f;

        /// <summary>
        /// The activation switch. When not running, the animations won't be animated.
        /// </summary>
        [KSPField(isPersistant = true, guiName = "Animation")]
        [UI_Toggle(enabledText = "On", disabledText = "Off")]
        public bool isActivated = false;

        /// <summary>
        /// A throttle control to vary the animation speed between minFramesPerSecond and maxFramesPerSecond
        /// </summary>
        [KSPField(isPersistant = true, guiName = "Animation Throttle")]
        [UI_FloatRange(stepIncrement = 0.01f, maxValue = 1f, minValue = 0f)]
        public float animationThrottle = 1.0f;

        /// <summary>
        /// A toggle that indicates whether or not to fade out the animations when the animationThrottle is set to zero.
        /// </summary>
        [KSPField(guiName = "Min Throttle Fade")]
        [UI_Toggle(enabledText = "Yes", disabledText = "No")]
        public bool fadesAtMinThrottle = false;
        #endregion

        #region Housekeeping
        private string[] emissiveURLs = null;
        private int emissiveTextureIndex = 0;
        private string[] diffuseURLs = null;
        private int diffuseTextureIndex = 0;
        private Renderer rendererMaterial;
        private double imageSwitchTime = 0;
        private float emissiveFadeLevel = 0;
        private float framesPerSecond = 1f;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return;

            // Get list of textures
            emissiveURLs = getTextures("animatedEmissiveTexture");
            diffuseURLs = getTextures("animatedDiffuseTexture");

            // Setup rendererMaterial
            Transform target = this.part.FindModelTransform(textureTransformName);
            if (target == null)
                return;
            rendererMaterial = target.GetComponent<Renderer>();
            if (rendererMaterial == null)
                return;

            // Setup emissive visibility
            if (isActivated)
            {
                emissiveFadeLevel = 1f;
                rendererMaterial.material.SetColor("_EmissiveColor", new Color(1, 1, 1, 1));
            }
            else
            {
                emissiveFadeLevel = 0f;
                rendererMaterial.material.SetColor("_EmissiveColor", new Color(0, 0, 0, 0));
            }

            // Calculate imageSwitchTime
            framesPerSecond = calculateFramesPerSecond();
            imageSwitchTime = Planetarium.GetUniversalTime() + 1 / framesPerSecond;

            // Show debug GUI
            debugMode = BlueshiftScenario.debugMode;
            Fields["isActivated"].guiActive = debugMode;
            Fields["animationThrottle"].guiActive = debugMode;
            Fields["fadesAtMinThrottle"].guiActive = debugMode;
            Fields["isActivated"].guiActiveEditor = debugMode;
            Fields["animationThrottle"].guiActiveEditor = debugMode;
            Fields["fadesAtMinThrottle"].guiActiveEditor = debugMode;
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return;

            // Calcuate frames per second
            framesPerSecond = calculateFramesPerSecond();

            // Fade in/out the emissive depending on whether or not we're activated
            if (emissiveFadeLevel < 0.95f && isActivated && ((fadesAtMinThrottle && animationThrottle > 0) || !fadesAtMinThrottle))
            {
                emissiveFadeLevel = Mathf.Lerp(emissiveFadeLevel, 1f, (1 / emissiveFadeTime) * TimeWarp.fixedDeltaTime);
                if (emissiveFadeLevel >= 0.9f)
                    emissiveFadeLevel = 1.0f;
            }
            else if (emissiveFadeLevel >= 0.1f && (!isActivated || (fadesAtMinThrottle && animationThrottle <= 0)))
            {
                emissiveFadeLevel = Mathf.Lerp(emissiveFadeLevel, 0f, (1 / emissiveFadeTime) * TimeWarp.fixedDeltaTime);
                if (emissiveFadeLevel <= 0.1f)
                    emissiveFadeLevel = 0f;
            }

            // If we're not activiated or fade level is next to nothing then no need to animate.
            if (!isActivated && (emissiveFadeLevel <= 0.001f || framesPerSecond <= 0))
            {
                return;
            }

            // Switch images if it's time to do so.
            if (Planetarium.GetUniversalTime() >= imageSwitchTime)
            {
                // Calculate next switch time
                imageSwitchTime = Planetarium.GetUniversalTime() + (1 / framesPerSecond);

                // Update diffuse if needed
                if (diffuseURLs != null && diffuseURLs.Length > 2)
                {
                    diffuseTextureIndex = (diffuseTextureIndex + 1) % diffuseURLs.Length;
                    rendererMaterial.material.SetTexture("_MainTex", GameDatabase.Instance.GetTexture(diffuseURLs[diffuseTextureIndex], false));
                }

                // Update emisssive if needed
                if (emissiveURLs != null && emissiveURLs.Length > 2)
                {
                    emissiveTextureIndex = (emissiveTextureIndex + 1) % emissiveURLs.Length;
                    rendererMaterial.material.SetTexture("_Emissive", GameDatabase.Instance.GetTexture(emissiveURLs[emissiveTextureIndex], false));
                }
            }
            rendererMaterial.material.SetColor("_EmissiveColor", new Color(emissiveFadeLevel, emissiveFadeLevel, emissiveFadeLevel, emissiveFadeLevel));
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Blends two textures together and stores the result in an output texture. Curtesy of stupid_chris
        /// </summary>
        /// <param name="blend">Percentage to blend through (from 0 to 1)</param>
        /// <param name="from">Beginning texture</param>
        /// <param name="to">Finishing texture</param>
        /// <param name="output">Texture to appear blended</param>
        private void blendTextures(float blend, Texture2D from, Texture2D to, Texture2D output)
        {
            blend = Mathf.Clamp01(blend);
            Color[] a = from.GetPixels(), b = to.GetPixels();
            if (a.Length != b.Length || a.Length != output.height * output.width) { return; }
            Color[] pixels = new Color[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                pixels[i] = Color.Lerp(a[i], b[i], blend);
            }
            output.SetPixels(pixels);
            output.Apply();
        }

        /// <summary>
        /// Retrieves all the textures for the specified name. If the name is "WarpPlasma" for instance, then the array of textures will have "WarpPlasma1" "WarpPlasma2" and so on.
        /// The method will keep looking for textures until it can no longer find a texture in the numbered sequence.
        /// </summary>
        /// <param name="textureName">The base name of the texture to search for. It should have the relative path such as WildBlueIndustries/Blueshift/Parts/Engine/WarpPlasma.</param>
        /// <returns>An array of string containing the numbered textures that comprise the animation.</returns>
        protected string[] getTextures(string textureName)
        {
            ConfigNode node = getPartConfigNode();
            if (node == null)
                return null;
            if (!node.HasValue(textureName))
                return null;

            List<string> textureURLs = new List<string>();
            string textureURL;
            int textureIndex = 1;

            animatedEmissiveTexture = node.GetValue(textureName);

            do
            {
                textureURL = animatedEmissiveTexture + textureIndex.ToString();
                if (GameDatabase.Instance.ExistsTexture(textureURL))
                {
                    textureURLs.Add(textureURL);
                    textureIndex += 1;
                }
                else
                {
                    textureIndex = -1;
                    break;
                }
            }
            while (textureIndex != -1);

            return textureURLs.ToArray();
        }

        protected float calculateFramesPerSecond()
        {
            return minFramesPerSecond + ((maxFramesPerSecond - minFramesPerSecond) * animationThrottle);
        }
        #endregion
    }
}
