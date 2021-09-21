using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

namespace Blueshift
{
    public class BlueshiftSettings : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("#LOC_BLUESHIFT_settingsCircularizeDesc", toolTip = "#LOC_BLUESHIFT_settingsCircularizeTip", autoPersistance = true, gameMode = GameParameters.GameMode.ANY)]
        public bool autoCircularize = false;

        [GameParameters.CustomParameterUI("#LOC_BLUESHIFT_settingsSpaceAnomaliesDesc", toolTip = "#LOC_BLUESHIFT_settingsSpaceAnomaliesTip", autoPersistance = true, gameMode = GameParameters.GameMode.ANY)]
        public bool enableSpaceAnomalies = true;

        [GameParameters.CustomParameterUI("#LOC_BLUESHIFT_settingsJumpgatesDesc", toolTip = "#LOC_BLUESHIFT_settingsJumpgatesTip", autoPersistance = true, gameMode = GameParameters.GameMode.ANY)]
        public bool enableJumpGates = false;

        [GameParameters.CustomParameterUI("#LOC_BLUESHIFT_settingsDestructiveJumpgatesDesc", toolTip = "", autoPersistance = true, gameMode = GameParameters.GameMode.ANY)]
        public bool enableDestructiveGateStartup = false;
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
            if (member.Name == "enableDestructiveGateStartup")
                return enableSpaceAnomalies && enableJumpGates;

            else if (member.Name == "enableJumpGates")
                return enableSpaceAnomalies;

            return true;
        }
        #endregion

        public static bool JumpgateStartupIsDestructive
        {
            get
            {
                BlueshiftSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BlueshiftSettings>();
                return settings.enableDestructiveGateStartup;
            }
        }

        public static bool JumpgatesEnabled
        {
            get
            {
                BlueshiftSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BlueshiftSettings>();
                return settings.enableJumpGates;
            }
        }

        public static bool SpaceAnomaliesEnabled
        {
            get
            {
                BlueshiftSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<BlueshiftSettings>();
                return settings.enableSpaceAnomalies;
            }
        }

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
