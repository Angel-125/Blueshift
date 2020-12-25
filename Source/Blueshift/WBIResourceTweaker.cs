using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

/*
Source code copyright 2020, by Michael Billard (Angel-125)
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

        PartResourceDefinition resourceDefinition;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (string.IsNullOrEmpty(resourceName))
                return;

            Events["enableResourceTweak"].guiName = resourceName + " " + tweakEnabledName;
            Events["disableResourceTweak"].guiName = resourceName + " " + tweakDisabledName;

            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            if (definitions.Contains(resourceName))
                resourceDefinition = definitions[resourceName];

            if (this.part.Resources.Contains(resourceName))
            {
                this.part.Resources[resourceName].isTweakable = resourceDefinition.isTweakable;
                if (HighLogic.LoadedSceneIsEditor)
                {
                    if (resourceDefinition.isTweakable)
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
            }
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
    }
}
