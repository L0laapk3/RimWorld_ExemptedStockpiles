using System;
using Verse;
using RimWorld;
using Harmony;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;

namespace ExemptedStockpiles
{
    public class SpecialThingFilterWorker_CountCompleted : SpecialThingFilterWorker
    {


        public override bool Matches(Thing t)
        {
            return false;
        }
        
        public override bool CanEverMatch(ThingDef def)
        {
            return ITab_Storage_FillTab_Patch.visible;
        }
    }




    
    //dirty hack.. Only make this specialthingfilter visible when rendering storage tab aka zone stockpile.

    [HarmonyPatch(typeof(ITab_Storage))]
    [HarmonyPatch("FillTab")]
    [HarmonyPatch(new Type[] { })]
    public static class ITab_Storage_FillTab_Patch
    {
        public static bool visible = false;
        public static ITab_Storage lastITab_Storage;

        public static void Prefix(ITab_Storage __instance)
        {
            visible = true;
            Log.Warning(__instance.ToString());
            lastITab_Storage = __instance;
        }
        public static void Postfix()
        {
            visible = false;
        }
    }

    


    
    //dumping have default off, normal stockpiles on

    [HarmonyPatch(typeof(ThingFilter))]
    [HarmonyPatch("SetFromPreset")]
    [HarmonyPatch(new Type[] { typeof(StorageSettingsPreset) })]
    public static class SetCountCompletedToTrueForDumpingStockpiles_Patch
    {

        public static void Postfix(StorageSettingsPreset preset, ThingFilter __instance)
        {
            if (null == CountCompletedDefOf.CountCompleted) return;
            __instance.SetAllow(CountCompletedDefOf.CountCompleted, preset != StorageSettingsPreset.DumpingStockpile);
        }
    }




    public static class SetAllowDisallowAll_Patch
    {
        public static void Prefix()
        {
            if (null != CountCompletedDefOf.CountCompleted)
                CountCompletedDefOf.CountCompleted.configurable = false;
        }
        public static void Postfix()
        {
            if (null != CountCompletedDefOf.CountCompleted)
                CountCompletedDefOf.CountCompleted.configurable = true;
        }
    }





    //insert on toggle stuff..

    [HarmonyPatch(typeof(Listing_TreeThingFilter))]
    [HarmonyPatch("DoSpecialFilter")]
    [HarmonyPatch(new Type[] { typeof(SpecialThingFilterDef), typeof(int) })]
    public static class UpdateCounterOnCountCompletedFilterToggle_Patch
    {

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            for (int i = 0; i < instructions.Count(); i++)
            {
                yield return instructions.ElementAt(i);
                if (instructions.ElementAt(i).opcode == OpCodes.Beq)
                {
                    int j = i + 1;
                    while (instructions.ElementAt(j).opcode != OpCodes.Callvirt)
                    {
                        yield return instructions.ElementAt(j);
                        j++;
                    }
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UpdateCounterOnCountCompletedFilterToggle_Patch), nameof(ToggleCountCompletedOnStockpile)));
                }
            }
        }

        static PropertyInfo prop = typeof(ITab_Storage).GetProperty("SelStoreSettingsParent", BindingFlags.NonPublic | BindingFlags.Instance);
        static MethodInfo getter = prop.GetGetMethod(nonPublic: true);

        public static void ToggleCountCompletedOnStockpile(ThingFilter __instance, SpecialThingFilterDef sfDef, bool allow)
        {

            if (allow == __instance.Allows(sfDef)) return;
            if (null == CountCompletedDefOf.CountCompleted) return;
            if (sfDef != CountCompletedDefOf.CountCompleted) return;

            if (ITab_Storage_FillTab_Patch.lastITab_Storage == null) Log.Error("What the heck??? CountCompleted was flipped before any ITab_Storage was printed.. gg");
            else
            {
                if (Find.VisibleMap == null) Log.Error("What the flop??? CountCompleted was flipped while there is no map loaded.. gg");
                else
                {
                    //Zone_Stockpile zone = (Zone_Stockpile)Find.VisibleMap.zoneManager.AllZones.Find(z => z is Zone_Stockpile && ((Zone_Stockpile)z).GetInspectTabs().Contains(ITab_Storage_FillTab_Patch.lastITab_Storage));
                    //IStoreSettingsParent tmp = (IStoreSettingsParent)getter.Invoke(ITab_Storage_FillTab_Patch.lastITab_Storage, null);

                    Zone_Stockpile zone = (Zone_Stockpile)getter.Invoke(ITab_Storage_FillTab_Patch.lastITab_Storage, null);
                    if (zone == null) Log.Error("What the truck??? CountCompleted was flipped, but there are no stockpiles that have the ITab_Storage on which FillTab was last called.. gg");
                    else
                    {
                        Log.Warning(zone.label);
                        if (allow)
                            foreach (IntVec3 sq in zone.AllSlotCells())
                                foreach (Thing thing in sq.GetThingList(zone.Map))
                                    zone.Map.GetComponent<UncountCompletedThingTracker>().UpdateCounter(thing.def, -thing.stackCount);
                        else
                            foreach (IntVec3 sq in zone.AllSlotCells())
                                foreach (Thing thing in sq.GetThingList(zone.Map))
                                    zone.Map.GetComponent<UncountCompletedThingTracker>().UpdateCounter(thing.def, thing.stackCount);

                    }
                }
            }
        }
    }

    /*
    [HarmonyPatch(typeof(ThingFilter))]
    [HarmonyPatch("SetAllow")]
    [HarmonyPatch(new Type[] { typeof(SpecialThingFilterDef), typeof(bool) })]
    public static class ThingFilter_SetAllow_Patch
    {
        public static void PostFix(SpecialThingFilterDef sfDef, bool allow, ThingFilter __instance)
        {
            Log.Warning("HELLLLLLOOOOOOOO???");

            if (allow == __instance.Allows(sfDef)) return;
            if (null == CountCompletedDefOf.CountCompleted) return;
            if (sfDef != CountCompletedDefOf.CountCompleted) return;

            if (ITab_Storage_FillTab_Patch.lastITab_Storage == null) Log.Error("What the heck??? CountCompleted was flipped before any ITab_Storage was printed.. gg");
            else
            {
                if (Find.VisibleMap == null) Log.Error("What the flop??? CountCompleted was flipped while there is no map loaded.. gg");
                else
                {
                    Zone_Stockpile zone = (Zone_Stockpile)Find.VisibleMap.zoneManager.AllZones.Find(z => z is Zone_Stockpile && ((Zone_Stockpile)z).GetInspectTabs().Contains(ITab_Storage_FillTab_Patch.lastITab_Storage));
                    if (zone == null) Log.Error("What the truck??? CountCompleted was flipped, but there are no stockpiles that have the ITab_Storage on which FillTab was last called.. gg");
                    else
                    {
                        if (allow)
                            foreach (IntVec3 sq in zone.AllSlotCells())
                                foreach (Thing thing in sq.GetThingList(zone.Map))
                                    zone.Map.GetComponent<UncountCompletedThingTracker>().UpdateCounter(thing.def, thing.stackCount);
                        else
                            foreach (IntVec3 sq in zone.AllSlotCells())
                                foreach (Thing thing in sq.GetThingList(zone.Map))
                                    zone.Map.GetComponent<UncountCompletedThingTracker>().UpdateCounter(thing.def, -thing.stackCount);

                    }
                }
            }
        }
    }
    */



    /*
    [HarmonyPatch(typeof(StorageSettings))]
    [HarmonyPatch("TryNotifyChanged")]
    [HarmonyPatch(new Type[] { })]
    public static class StorageSettings_TryNotifyChanged_Patch
    {
        public static Dictionary<StorageSettings, bool> wasOn = new Dictionary<StorageSettings, bool>();

        public static void Postfix(StorageSettings __instance)
        {
            Log.Warning("TRYNOTIFYCHANGED");

            if (!(__instance.owner is Zone_Stockpile)) return;

            bool exists = wasOn.TryGetValue(__instance, out bool old);
            Log.Warning(__instance.filter.Allows(CountCompletedDefOf.CountCompleted) + " " + old);
            if (__instance.filter.Allows(CountCompletedDefOf.CountCompleted) != old)
            {
                Zone_Stockpile zone = (Zone_Stockpile)__instance.owner;

                if (old)
                {
                    foreach(IntVec3 sq in zone.AllSlotCells())
                        foreach (Thing thing in sq.GetThingList(zone.Map))
                            zone.Map.GetComponent<UncountCompletedThingTracker>().UpdateCounter(thing.def, -thing.stackCount);

                    wasOn[__instance] = false;
                }
                else
                {
                    foreach (IntVec3 sq in zone.AllSlotCells())
                        foreach (Thing thing in sq.GetThingList(zone.Map))
                            zone.Map.GetComponent<UncountCompletedThingTracker>().UpdateCounter(thing.def, thing.stackCount);

                    if (exists)
                        wasOn[__instance] = true;
                    else
                        wasOn.Add(__instance, true);
                }
            }
        }
    }*/
}
