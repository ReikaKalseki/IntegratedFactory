using System;

using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Xml;
using ReikaKalseki.FortressCore;

namespace ReikaKalseki.IntegratedFactory
{
	public class IFConfig
	{		
		public enum ConfigEntries {
			[ConfigEntry("Make Blast Furnace as efficient on basegame ores as smelters", true)]EFFICIENT_BLAST,
			[ConfigEntry("Use T1-3 materials in T4, and slightly reduce T4 bar costs", true)]T3_T4,
			[ConfigEntry("T4 Ore Research Pod Cost Multiplier", typeof(float), 1, 0.25F, 8F, 1)]T4_RESEARCH_COST_SCALE,
			[ConfigEntry("T4 Gas Research Pod Cost Multiplier", typeof(float), 1, 0.25F, 8F, 1)]GAS_RESEARCH_COST_SCALE,
		}
	}
}
