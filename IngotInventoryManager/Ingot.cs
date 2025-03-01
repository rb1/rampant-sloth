using VRage.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program
    {
        class Ingot
        {
            public Ingot(int lowCount, int highCount, MyItemType ingotType, MyInventoryItemFilter oreFilter, string lcdLabel)
            {
                this.lowCount = lowCount;
                this.highCount = highCount;
                this.ingotItemType = ingotType;
                this.oreFilter = oreFilter;
                this.lcdLabel = lcdLabel;
            }
            public readonly int lowCount;
            public readonly int highCount;
            public readonly MyItemType ingotItemType;
            public readonly MyInventoryItemFilter oreFilter;
            public readonly string lcdLabel;
        }

        static Ingot MakeIngot(int lowCount, int highCount, string subTypeId, string lcdLabel)
        {
            return new Ingot(lowCount, highCount, MyItemType.MakeIngot(subTypeId), 
                             new MyInventoryItemFilter("MyObjectBuilder_Ore/"+subTypeId),
                             lcdLabel);
        }
    }
}