﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AzWeaponLib
{
    public interface IStatable
    {
        IEnumerable<StatDrawEntry> GetStatDrawEntries(StatRequest req);
    }
}
