using Verse;

namespace AzWeaponLib.MultiVerb
{
    public class Command_VerbTargetInvisible : Command_VerbTarget
    {
        public override bool Visible => false;
        public override bool GroupsWith(Gizmo other) 
        {
            return false;
        }
        public override void GizmoUpdateOnMouseover()
        {
        }
    }
}
