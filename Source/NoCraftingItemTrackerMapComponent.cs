using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace ExemptedStockpiles
{
    public class UncountCompletedThingTracker : MapComponent
    {
        public UncountCompletedThingTracker(Map map) : base(map) { }




        public Dictionary<ThingDef, int> uncountedCompletedThings;


        public override void FinalizeInit()
        {
            uncountedCompletedThings = new Dictionary<ThingDef, int>();
            foreach (Zone zone in this.map.zoneManager.AllZones)
                if (zone is Zone_Stockpile && !((Zone_Stockpile)zone).settings.filter.Allows(CountCompletedDefOf.CountCompleted))
                    foreach (Thing thing in ((Zone_Stockpile)zone).AllContainedThings)
                       UpdateCounter(thing);
        }


        public void UpdateCounter(Thing thing) { UpdateCounter(thing.def, thing.stackCount); }
        public void UpdateCounter(ThingDef def, int amount)
        {
            //Log.Message("update: " + def.ToString() + " " + amount);
            if (uncountedCompletedThings.TryGetValue(def, out int tmp))
                uncountedCompletedThings[def] = tmp + amount;
            else
                uncountedCompletedThings.Add(def, amount);
        }


        public override void MapComponentUpdate() { }
        public override void MapComponentTick() { }
        public override void MapComponentOnGUI() { }
        public override void ExposeData() { }
        public override void MapGenerated() { }
        public override void MapRemoved() { }
    }










    [HarmonyPatch(typeof(Zone_Stockpile))]
    [HarmonyPatch("Notify_ReceivedThing")]
    [HarmonyPatch(new Type[] { typeof(Thing) })]
    public static class Zone_Stockpile_Notify_ReceivedThing_Patch
    {
        public static void Postfix(Thing newItem, Zone_Stockpile __instance)
        {
            //Log.Message("added thing to stockpile: " + newItem);
            if (!__instance.settings.filter.Allows(CountCompletedDefOf.CountCompleted))
                __instance.Map.GetComponent<UncountCompletedThingTracker>().UpdateCounter(newItem.def, newItem.stackCount);
        }
    }

    [HarmonyPatch(typeof(Zone_Stockpile))]
    [HarmonyPatch("Notify_LostThing")]
    [HarmonyPatch(new Type[] { typeof(Thing) })]
    public static class Zone_Stockpile_Notify_LostThing_Patch
    {
        public static void Postfix(Thing newItem, Zone_Stockpile __instance) //not actually newItem, but lost item (following B18 rimworld naming
        {
            //Log.Message("removed thing to stockpile: " + newItem);
            if (!__instance.settings.filter.Allows(CountCompletedDefOf.CountCompleted))
                __instance.Map.GetComponent<UncountCompletedThingTracker>().UpdateCounter(newItem.def, -newItem.stackCount);
        }
    }

    [HarmonyPatch(typeof(Zone_Stockpile))]
    [HarmonyPatch("AddCell")]
    [HarmonyPatch(new Type[] { typeof(IntVec3) })]
    public static class Zone_Stockpile_Notify_AddCell_Patch
    {
        public static void Postfix(IntVec3 sq, Zone_Stockpile __instance)
        {
            //Log.Message("addcell: " + sq.ToString());
            if (!__instance.settings.filter.Allows(CountCompletedDefOf.CountCompleted))
                foreach(Thing thing in sq.GetThingList(__instance.Map))
                    __instance.Map.GetComponent<UncountCompletedThingTracker>().UpdateCounter(thing.def, thing.stackCount);
        }
    }

    [HarmonyPatch(typeof(Zone_Stockpile))]
    [HarmonyPatch("RemoveCell")]
    [HarmonyPatch(new Type[] { typeof(IntVec3) })]
    public static class Zone_Stockpile_Notify_RemoveCell_Patch
    {
        public static void Postfix(IntVec3 sq, Zone_Stockpile __instance) //not actually newItem, but lost item (following B18 rimworld naming
        {
            //Log.Message("removecell: " + sq.ToString());
            if (!__instance.settings.filter.Allows(CountCompletedDefOf.CountCompleted))
                foreach (Thing thing in sq.GetThingList(__instance.Map))
                    __instance.Map.GetComponent<UncountCompletedThingTracker>().UpdateCounter(thing.def, -thing.stackCount);
        }
    }
}
