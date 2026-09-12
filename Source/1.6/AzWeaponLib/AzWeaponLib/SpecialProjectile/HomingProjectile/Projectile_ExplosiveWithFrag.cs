using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AzWeaponLib.SpecialProjectile
{
    public class Projectile_ExplosiveWithFrag : Projectile_Explosive
    {
        protected override void Explode()
        {
            ModExtension_Cone modext = def.GetModExtension<ModExtension_Cone>();
            if (modext != null)
            {
                modext.DoConeExplosion(Position, Map, ExactRotation, def.projectile.damageDef, launcher);
            }
            Destroy();
        }
    }
}
