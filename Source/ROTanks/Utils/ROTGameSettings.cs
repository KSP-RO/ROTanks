using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ROTanks
{
    public class ROTGameSettings : GameParameters.CustomParameterNode
    {

        [GameParameters.CustomParameterUI("Persistent Recolor Selections", toolTip = "If true, custom recolor selections will persist across texture set changes.")]
        public bool persistRecolorSelections = false;

        public override string Section { get { return "ROTanks"; } }

        public override int SectionOrder { get { return 1; } }

        public override string Title { get { return "ROTanks Options"; } }

        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }

        public override bool HasPresets { get { return true; } }

        public override string DisplaySection
        {
            get
            {
                return "ROTanks";
            }
        }

        public static bool persistRecolor()
        {
            if (HighLogic.CurrentGame != null)
            {
                return HighLogic.CurrentGame.Parameters.CustomParams<ROTGameSettings>().persistRecolorSelections;
            }
            return false;
        }

    }
}
