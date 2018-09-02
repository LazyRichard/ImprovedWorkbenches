using System.Collections.Generic;
using System.Linq;
using Harmony;
using ImprovedWorkbenches.Filtering;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(RecipeWorkerCounter), "CountProducts")]
    public static class RecipeWorkerCounter_CountProducts_Detour
    {
        [HarmonyPrefix]
        static bool Prefix(ref Bill_Production bill, ref int __result)
        {
            if (!ExtendedBillDataStorage.CanOutputBeFiltered(bill))
                return true;

            var billMap = bill.Map;

            // Find player pawns to check inventories of
            var playerFactionPawnsToCheck = new List<Pawn>();
            if (bill.includeEquipped)
            {
                // Fix for vanilla not counting items being hauled by colonists or animals
                playerFactionPawnsToCheck.AddRange(
                    billMap.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).Where(p => p.IsFreeColonist && !p.IsColonist));
            }

            if (bill.includeEquipped)
            {
                var extendedBillData = Main.Instance.GetExtendedBillDataStorage().GetExtendedDataFor(bill);
                if (extendedBillData != null && extendedBillData.CountAway)
                {
                    PopulatePawnsCurrentlyAway(billMap, playerFactionPawnsToCheck);
                }
            }

            var thingCountClass = bill.recipe.products.First();
            var productThingDef = thingCountClass.thingDef;

            // Helper function to count matching items in inventory lists
            int CountMatchingThingsIn(IEnumerable<Thing> things)
            {
                var count = 0;
                foreach (var thing in things)
                {
                    Thing item = thing.GetInnerIfMinified();
                    if (item.def != productThingDef)
                        continue;

                    if (!statFilterWrapper.DoesThingMatchFilter(billIngredientFilter, item))
                        continue;

                    if (nonDeadmansApparelFilter != null && !nonDeadmansApparelFilter.Matches(item))
                        continue;

                    count += item.stackCount;
                }

                return count;
            }

            // Look for matching items in found colonist inventories
            foreach (var pawn in playerFactionPawnsToCheck)
            {
                if (pawn.apparel != null)
                    __result += CountMatchingThingsIn(pawn.apparel.WornApparel.Cast<Thing>());

                if (pawn.equipment != null)
                    __result += CountMatchingThingsIn(pawn.equipment.AllEquipmentListForReading.Cast<Thing>());

                if (pawn.inventory != null)
                    __result += CountMatchingThingsIn(pawn.inventory.innerContainer);

                if (pawn.carryTracker != null)
                    __result += CountMatchingThingsIn(pawn.carryTracker.innerContainer);
            }

            return false;
        }

        private static void PopulatePawnsCurrentlyAway(Map billMap, List<Pawn> playerFactionPawnsToCheck)
        {
            // Given a colonist or animal from the player faction, check if its home map
            // is the bill's map.
            bool IsPlayerPawnFromBillMap(Pawn pawn)
            {
                if (!pawn.IsFreeColonist && pawn.RaceProps.Humanlike)
                    return false;

                // Assumption: pawns transferring between colonies will "settle"
                // in the destination.

                return pawn.Map == billMap || pawn.GetOriginMap() == billMap;
            }

            // Include all colonists and colony animals, unspawned (transport pods, cryptosleep, etc.)
            // in all maps other than current.
            foreach (var otherMap in Find.Maps)
            {
                if (otherMap == billMap)
                    continue;

                playerFactionPawnsToCheck.AddRange(
                    otherMap.mapPawns.PawnsInFaction(Faction.OfPlayer)
                        .Where(IsPlayerPawnFromBillMap));
            }

            // and caravans
            playerFactionPawnsToCheck.AddRange(Find.WorldPawns.AllPawnsAlive
                // OriginMap is only set on pawns we care about
                .Where(p => p.GetOriginMap() == billMap));
        }
    }
}