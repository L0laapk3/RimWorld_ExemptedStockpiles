using System;
using Verse;
using RimWorld;
using Harmony;

namespace ExemptedStockpiles
{
    public class SpecialThingFilterWorker_DeadmansApparel : SpecialThingFilterWorker_NonDeadmansApparel
    {
        public override bool Matches(Thing t)
        {
            return !base.Matches(t) && base.CanEverMatch(t.def);
        }
    }
}
