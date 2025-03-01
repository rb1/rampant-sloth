using System;
using System.Collections.Generic;
using System.Reflection;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program
    {
        public interface IOutputGenerator
        {
            string GetStatusText();
        }
        class OutputCache
        {
            public OutputCache(Dictionary<OutputType, IOutputGenerator> outputGenerators)
            {
                this.outputGenerators = outputGenerators;
            }
            public string GetOutput(OutputType type)
            {
                if(outputCache.ContainsKey(type))
                {
                    return outputCache[type];
                }
                var output = outputGenerators[type].GetStatusText();
                outputCache[type] = output;
                return output;
            }
            public void ClearCache()
            {
                outputCache.Clear();
            }
            readonly Dictionary<OutputType, string> outputCache = new Dictionary<OutputType, string>();
            readonly Dictionary<OutputType, IOutputGenerator> outputGenerators;
        }
    }
}