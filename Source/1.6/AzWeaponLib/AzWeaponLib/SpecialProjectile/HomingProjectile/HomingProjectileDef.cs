using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AzWeaponLib.SpecialProjectile
{
    public class HomingProjectileDef : DefModExtension
    {
        public float hitChance = 0.5f;
        public float homingSpeed = 0.1f;
        public float initRotateAngle = 30;
        public float proximityFuseRange = 0f;
        public IntRange destroyTicksAfterLosingTrack = new IntRange(60, 120);
        public ThingDef extraProjectile;
        public float speedChangePerTick;
        public FloatRange? speedRangeOverride;
        public float homingRotateErrorRange = -1f;
        public int recalculateRotateErrorTick = -1;
        public float SpeedChangeTilesPerTickOverride => speedChangePerTick / 100f;
        public FloatRange SpeedRangeTilesPerTickOverride => speedRangeOverride.Value * 0.01f;
    }
}
