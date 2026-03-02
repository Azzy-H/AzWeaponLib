using System;
using Verse;

namespace AzWeaponLib.MultiVerb
{
    public class MultiVerbInfo : GizmoInfo
    {
        public ThingStyleDef styleDef;
        public Type verbSwitchWorker = typeof(VerbSwitchWorker);
    }
}
