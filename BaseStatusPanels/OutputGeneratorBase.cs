using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program
    {
        public abstract class OutputGeneratorBase : IOutputGenerator
        {
            public OutputGeneratorBase(IMyInventory storage) : this(new List<IMyInventory>{storage})
            {}
            public OutputGeneratorBase(IReadOnlyList<IMyInventory> storage)
            {
                this.storage = storage;
            }
            public string GetStatusText()
            {
                var output = Title + Environment.NewLine;
                foreach(var typeLabel in TypeLabels)
                {
                    output += (typeLabel.Value).PadRight(3) + ": ";
                    var amount = GetItemAmount(typeLabel.Key);
                    output += FormatItemAmount((float)amount).PadLeft(6);
                    output += Environment.NewLine;
                }
                return output;                           
            }
            protected VRage.MyFixedPoint GetItemAmount(MyItemType itemType)
            {
                VRage.MyFixedPoint amount = 0;
                foreach(var store in storage)
                {
                    amount += store.GetItemAmount(itemType);
                }
                return amount;
            }
            protected abstract string Title { get; }
            protected abstract Dictionary<MyItemType, string> TypeLabels { get; }
            readonly IReadOnlyList<IMyInventory> storage;

            string FormatItemAmount(float amount)
            {
                if (amount == 0f)
                    return "0";

                if(amount < 1000)
                    return string.Format("{0}", amount.ToString("n1"));
                
                amount /= 1000;
                if(amount < 1000)
                    return string.Format("{0}K", amount.ToString("n1"));
                
                amount /= 1000;
                if(amount < 1000)
                    return string.Format("{0}M", amount.ToString("n1"));

                amount /= 1000;
                if(amount < 1000)
                    return string.Format("{0}G", amount.ToString("n1"));

                amount /= 1000;
                if(amount < 1000)
                    return string.Format("{0}T", amount.ToString("n1"));

                amount /= 1000;
                if(amount < 1000)
                    return string.Format("{0}P", amount.ToString("n1"));

                return "lots";
            }
        }
    }
}