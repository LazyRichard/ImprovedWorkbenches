using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public class ExtendedBillData : IExposable
    {
        public ThingFilter OutputFilter = new ThingFilter();
        public bool CountAway;
        public string Name;

        public ExtendedBillData()
        {
        }

        public void CloneFrom(ExtendedBillData other, bool cloneName)
        {
            OutputFilter.CopyAllowancesFrom(other.OutputFilter);
            CountAway = other.CountAway;
            if (cloneName)
                Name = other.Name;
        }

        public void SetDefaultFilter(Bill_Production bill)
        {
            var thingDef = bill.recipe.products.First().thingDef;
            OutputFilter.SetDisallowAll();
            OutputFilter.SetAllow(thingDef, true);
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref OutputFilter, "outputFilter", new object[0]);
            Scribe_Values.Look(ref CountAway, "countAway", false);
            Scribe_Values.Look(ref Name, "name", null);
        }

        private static bool IsValidStockpileName(string name)
        {
            return !string.IsNullOrEmpty(name) && name != "null";
        }
    }
}