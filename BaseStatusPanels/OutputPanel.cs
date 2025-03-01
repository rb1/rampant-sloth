using System;
using System.Linq;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using System.Security.Cryptography;

namespace IngameScript
{
    partial class Program
    {
        class OutputPanel
        {
            public OutputPanel(IMyTextPanel panelBlock)
            {
                outputCycle = ParseOutputCycleFromString(panelBlock.CustomData);
                Block = panelBlock;
                CurrentOutput = GetCurrentOutputIndexFromCustomData(panelBlock.CustomData);
            }
            OutputType GetCurrentOutputIndexFromCustomData(string data)
            {
                var lastComma = data.LastIndexOf(',');
                if(lastComma < 0)
                    return outputCycle.First();
                var indexStr = data.Substring(lastComma+1);
                int outputIndex;
                int.TryParse(indexStr, out outputIndex);
                if(outputIndex >= outputCycle.Count || outputIndex < 0)
                    outputIndex = 0;
                return outputCycle[outputIndex];
            }
            void SetCurrentOutputIndexInCustomData()
            {
                var data = Block.CustomData;
                var lastComma = data.LastIndexOf(',');
                if(lastComma < 0)
                    lastComma = data.Length;

                data = data.Substring(0, lastComma);
                data += string.Format(",{0}", outputCycle.IndexOf(CurrentOutput));
                Block.CustomData = data;
            }
            public OutputType CurrentOutput;
            public readonly IMyTextPanel Block;
            
            public void MoveToNextOutput()
            {
                int index = outputCycle.IndexOf(CurrentOutput);
                index++;
                if(index >= outputCycle.Count)
                    index = 0;
                CurrentOutput = outputCycle[index];
                SetCurrentOutputIndexInCustomData();
            }
            readonly List<OutputType> outputCycle;
            static List<OutputType> ParseOutputCycleFromString(string data)
            {
                data = data.ToLower();
                SortedDictionary<int, OutputType> sortedCycle = new SortedDictionary<int, OutputType>();
                foreach(var output in Enum.GetValues(typeof(OutputType)).Cast<OutputType>())
                {
                    var position = data.IndexOf(GetOutputTypeConfigString(output).ToLower());
                    if(position >= 0)
                    {
                        sortedCycle.Add(position, output);
                    }
                }
                if(sortedCycle.Count > 0)
                {
                    return new List<OutputType>(sortedCycle.Values);
                }
                return new List<OutputType>(Enum.GetValues(typeof(OutputType)).Cast<OutputType>());
            }
            static string GetOutputTypeConfigString(OutputType type)
            {
                switch(type)
                {
                    case OutputType.ComponentLevels: return "ComponentLevels";
                    case OutputType.GasLevels: return "GasLevels";
                    case OutputType.IngotLevelsP1: return "IngotLevels1";
                    case OutputType.IngotLevelsP2: return "IngotLevels2";
                    case OutputType.OreLevelsP1: return "OreLevels1";
                    case OutputType.OreLevelsP2: return "OreLevels2";
                }
                return string.Format("{0}", type);
            }
        }
    }
}