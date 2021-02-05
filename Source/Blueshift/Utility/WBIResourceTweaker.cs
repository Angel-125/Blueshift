using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
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
    public class WBIResourceTweaker: PartModule
    {
        [KSPField]
        public string resourceName = string.Empty;

        [KSPField]
        public string tweakEnabledName = "Enable Tweak";

        [KSPField]
        public string tweakDisabledName = "Disable Tweak";

        [KSPField]
        public bool isEnabled = false;

        PartResourceDefinition resourceDefinition;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (string.IsNullOrEmpty(resourceName))
                return;

            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            if (definitions.Contains(resourceName))
            {
                resourceDefinition = definitions[resourceName];

                Events["enableResourceTweak"].guiName = resourceDefinition.displayName + " " + tweakEnabledName;
                Events["disableResourceTweak"].guiName = resourceDefinition.displayName + " " + tweakDisabledName;

                if (HighLogic.LoadedSceneIsEditor)
                {
                    this.part.Resources[resourceName].isTweakable = resourceDefinition.isTweakable;
                    updateUI();
                    if (isEnabled)
                        enableResourceTweak();
                    GameEvents.onPartResourceListChange.Add(onPartResourceListChange);
                }
            }
        }

        public void OnDestroy()
        {
            GameEvents.onPartResourceListChange.Remove(onPartResourceListChange);
        }

        [KSPEvent(guiActiveEditor = true)]
        public void enableResourceTweak()
        {
            Events["enableResourceTweak"].active = false;
            Events["disableResourceTweak"].active = true;

            if (this.part.Resources.Contains(resourceName))
            {
                this.part.Resources[resourceName].isTweakable = true;
            }

            //Dirty the GUI
            MonoUtilities.RefreshContextWindows(this.part);
            GameEvents.onPartResourceListChange.Fire(this.part);
        }

        [KSPEvent(guiActiveEditor = true)]
        public void disableResourceTweak()
        {
            Events["enableResourceTweak"].active = true;
            Events["disableResourceTweak"].active = false;

            if (this.part.Resources.Contains(resourceName))
            {
                this.part.Resources[resourceName].isTweakable = false;
            }

            //Dirty the GUI
            MonoUtilities.RefreshContextWindows(this.part);
            GameEvents.onPartResourceListChange.Fire(this.part);
        }

        protected void updateUI()
        {
            if (this.part.Resources.Contains(resourceName))
            {
                if (this.part.Resources[resourceName].isTweakable)
                {
                    Events["enableResourceTweak"].active = false;
                    Events["disableResourceTweak"].active = true;
                }
                else
                {
                    Events["enableResourceTweak"].active = true;
                    Events["disableResourceTweak"].active = false;
                }
            }
            else
            {
                Events["enableResourceTweak"].active = false;
                Events["disableResourceTweak"].active = false;
            }
        }

        protected void onPartResourceListChange(Part modifiedPart)
        {
            if (modifiedPart == this.part)
                updateUI();
        }
    }
}
