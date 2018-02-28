using Harmony;
using RimWorld;
using System;
using System.Linq;
using System.Reflection;
using Verse;

namespace ExemptedStockpiles
{
    public class Main : Mod
    {

        public Main(ModContentPack content) : base(content)
        {
            Log.Warning("Starting patch!");


            var harmony = HarmonyInstance.Create("com.github.L0laapk3.RimWorld.ExemptedStockpiles");



            var RecipeWorkerCounter_postfix = new HarmonyMethod(typeof(RecipeWorkerCounter_CountProducts_Patch).GetMethod("Postfix"));

            harmony.Patch(typeof(RecipeWorkerCounter).GetMethod("CountProducts"), null, RecipeWorkerCounter_postfix);
            harmony.Patch(typeof(RecipeWorkerCounter_MakeStoneBlocks).GetMethod("CountProducts"), null, RecipeWorkerCounter_postfix);


            var ThingFilter_prefix = new HarmonyMethod(typeof(SetAllowDisallowAll_Patch).GetMethod("Prefix"));
            var ThingFilter_postfix = new HarmonyMethod(typeof(SetAllowDisallowAll_Patch).GetMethod("Postfix"));

            harmony.Patch(typeof(ThingFilter).GetMethod("SetAllowAll"), ThingFilter_prefix, ThingFilter_postfix);
            harmony.Patch(typeof(ThingFilter).GetMethod("SetDisallowAll"), ThingFilter_prefix, ThingFilter_postfix);


            harmony.PatchAll(Assembly.GetExecutingAssembly());



            Log.Warning("Patch completed!");
        }
    }


    


    
    public static class RecipeWorkerCounter_CountProducts_Patch
    {
        public static void Postfix(Bill_Production bill, ref int __result)
        {

            //Log.Warning("Patch visited! " + __result.ToString() + " " + bill.ToString() + " " + subtract + " found in excluded stockpiles");

            /*int subtract = bill.Map.zoneManager.AllZones
                .Where(z => z is Zone_Stockpile && !((Zone_Stockpile)z).settings.filter.Allows(CountCompletedDefOf.CountCompleted))
                .Select<Zone, int>(z => ((Zone_Stockpile)z).AllContainedThings
                    .Where(t => t.def == bill.recipe.products[0].thingDef)
                    .Select<Thing, int>(t => t.stackCount)
                    .Sum())
                .Sum();

            __result -= subtract;
            */

            bill.Map.GetComponent<UncountCompletedThingTracker>().uncountedCompletedThings.TryGetValue(bill.recipe.products[0].thingDef, out int subtract);
            __result -= subtract;
        }
    }








}