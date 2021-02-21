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
        [GameParameters.CustomParameterUI("Enable circularization helper", toolTip = "Adds a button to the warp engine PAW to Auto-circularize your orbit- no gravity braking needed.", autoPersistance = true, gameMode = GameParameters.GameMode.ANY)]
        public bool autoCircularize = false;

        [GameParameters.CustomParameterUI("Allow Space Anomalies", toolTip = "Allows Space Anomalies to spawn in game.", autoPersistance = true, gameMode = GameParameters.GameMode.ANY)]
        public bool enableSpaceAnomalies = true;

        [GameParameters.CustomParameterUI("Allow Jumpgates", toolTip = "Allows Jumpgate Anomalies to spawn in game. Player-built gates are unaffected.", autoPersistance = true, gameMode = GameParameters.GameMode.ANY)]
        public bool enableJumpGates = false;

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
            if (member.Name == "enableJumpGates")
                return enableSpaceAnomalies;

            return true;
        }
        #endregion

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
