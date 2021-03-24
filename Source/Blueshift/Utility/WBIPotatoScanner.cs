using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

namespace Blueshift
{
    [KSPModule("#LOC_BLUESHIFT_scannerTitle")]
    public class WBIPotatoScanner : PartModule
    {
        protected PotatoScannerInfo scannerInfo;
        ModuleAsteroid asteroid = null;
        ModuleComet comet = null;

        [KSPEvent(guiName = "#LOC_BLUESHIFT_openScannerGUI", guiActive = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void OpenScannerGUI()
        {
            //Display the window
            scannerInfo.SetVisible(true);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            scannerInfo = new PotatoScannerInfo();
            scannerInfo.part = part;
        }
    }
}
