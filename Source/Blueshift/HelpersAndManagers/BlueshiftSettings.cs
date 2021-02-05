using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.IO;

namespace Blueshift
{
    public class BlueshiftSettings : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("Auto-circularize orbit after warp", toolTip = "Auto-circularize your orbit after warp- no gravity braking needed.", autoPersistance = true, gameMode = GameParameters.GameMode.ANY)]
        public bool autoCircularize = false;

        #region CustomParameterNode

        public override string DisplaySection
        {
            get
            {
                return Section;
            }
        }

        public override string Section
        {
            get
            {
                return "Blueshift";
            }
        }

        public override string Title
        {
            get
            {
                return "FTL";
            }
        }

        public override int SectionOrder
        {
            get
            {
                return 1;
            }
        }

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            base.SetDifficultyPreset(preset);
        }

        public override GameParameters.GameMode GameMode
        {
            get
            {
                return GameParameters.GameMode.ANY;
            }
        }

        public override bool HasPresets
        {
            get
            {
                return false;
            }
        }

        public override bool Enabled(System.Reflection.MemberInfo member, GameParameters parameters)
        {
            return true;
        }
        #endregion

        public static bool AutoCircularize
        {
            get
            {
                BlueshiftSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BlueshiftSettings>();
                return settings.autoCircularize;
            }
        }
    }
}
