using UnityEngine;  //Needed for most Unity Enginer manipulations: Vectors, GameObjects, Audio, etc.
using System.IO;    //For data read/write methods
using System;    //For data read/write methods
using System.Collections.Generic;   //Working with Lists and Collections
using System.Linq;   //More advanced manipulation of lists/collections
using System.Threading;
using System.Reflection;
using Harmony;
using ReikaKalseki;
using ReikaKalseki.FortressCore;

namespace ReikaKalseki.IntegratedFactory
{
  public class IntegratedFactoryMod : FCoreMod
  {    
    private static Config<IFConfig.ConfigEntries> config;
    
    public IntegratedFactoryMod() : base("IntegratedFactory") {
    	config = new Config<IFConfig.ConfigEntries>(this);
    }
	
	public static Config<IFConfig.ConfigEntries> getConfig() {
		return config;
	}

    public override ModRegistrationData Register()
    {
        ModRegistrationData registrationData = new ModRegistrationData();
        
        config.load();
        
        //registrationData.RegisterEntityHandler(MOD_KEY);
        /*
        TerrainDataEntry entry;
        TerrainDataValueEntry valueEntry;
        TerrainData.GetCubeByKey(CUBE_KEY, out entry, out valueEntry);
        if (entry != null)
          ModCubeType = entry.CubeType;
         */
        
        runHarmony();
        
        //GenericAutoCrafterNew
        
        RecipeUtil.addRecipe("ChromiumPlate", "ReikaKalseki.ChromiumPlate", "", set: "Stamper").addIngredient("ChromiumBar", 1);
        RecipeUtil.addRecipe("MolybdenumPlate", "ReikaKalseki.MolybdenumPlate", "", set: "Stamper").addIngredient("MolybdenumBar", 1);
        RecipeUtil.addRecipe("ChromiumWire", "ReikaKalseki.ChromiumWire", "", set: "Extruder").addIngredient("ChromiumBar", 1);
        RecipeUtil.addRecipe("MolybdenumWire", "ReikaKalseki.MolybdenumWire", "", set: "Extruder").addIngredient("MolybdenumBar", 1);
        RecipeUtil.addRecipe("ChromiumCoil", "ReikaKalseki.ChromiumCoil", "", set: "Coiler").addIngredient("ReikaKalseki.ChromiumWire", 1);
        RecipeUtil.addRecipe("MolybdenumCoil", "ReikaKalseki.MolybdenumCoil", "", set: "Coiler").addIngredient("ReikaKalseki.MolybdenumWire", 1);
        RecipeUtil.addRecipe("ChromiumPCB", "ReikaKalseki.ChromiumPCB", "", set: "PCBAssembler").addIngredient("ReikaKalseki.ChromiumCoil", 1);
        RecipeUtil.addRecipe("MolybdenumPCB", "ReikaKalseki.MolybdenumPCB", "", set: "PCBAssembler").addIngredient("ReikaKalseki.MolybdenumCoil", 1);
        RecipeUtil.addRecipe("ChromiumPipe", "ReikaKalseki.ChromiumPipe", "", set: "PipeExtruder").addIngredient("ChromiumBar", 1);
        RecipeUtil.addRecipe("MolybdenumPipe", "ReikaKalseki.MolybdenumPipe", "", set: "PipeExtruder").addIngredient("MolybdenumBar", 1);
        CraftData cpod = RecipeUtil.addRecipe("ChromiumExperimentalPod", "ReikaKalseki.ChromiumExperimentalPod", "", set: "ResearchAssembler");
        cpod.addIngredient("ReikaKalseki.ChromiumPlate", 6);
        cpod.addIngredient("ReikaKalseki.ChromiumPCB", 2);
        CraftData mpod = RecipeUtil.addRecipe("MolybdenumExperimentalPod", "ReikaKalseki.MolybdenumExperimentalPod", "", set: "ResearchAssembler");
        mpod.addIngredient("ReikaKalseki.MolybdenumPlate", 6);
        mpod.addIngredient("ReikaKalseki.MolybdenumPCB", 2);
        
       	addAndSubSomeIf("powerpack2", "ChromiumBar", "ReikaKalseki.ChromiumPCB", "AlloyedPCB", 1/16F);
       	addAndSubSomeIf("powerpack2", "MolybdenumBar", "ReikaKalseki.MolybdenumPCB", "OverclockedCrystalClock", 1/4F);
       	
       	CraftData rec = RecipeUtil.getRecipeByKey("build gun mk3");
       	float ratio = 1;
       	if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
       		rec.addIngredient("UltimatePCB", 5);
       		ratio = 150/512F;
       	}
       	rec.replaceIngredient("ChromiumBar", "ReikaKalseki.ChromiumPlate", ratio);
       	rec.replaceIngredient("MolybdenumBar", "ReikaKalseki.MolybdenumPlate", ratio);
       	
       	rec = RecipeUtil.getRecipeByKey("chrome_crafter");
       	rec.replaceIngredient("ChromiumBar", "ReikaKalseki.ChromiumPlate");
       	if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
       		rec.addIngredient("IronGear", 20);
       		rec.addIngredient("LightweightMachineHousing", 5);
       	}
       	rec = RecipeUtil.getRecipeByKey("mag_crafter");
       	rec.replaceIngredient("MolybdenumBar", "ReikaKalseki.MolybdenumPlate");
       	if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
       		rec.addIngredient("IronGear", 20);
       		rec.addIngredient("LightweightMachineHousing", 5);
       	}
       	rec = RecipeUtil.getRecipeByKey("hiemal_crafter");
       	if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
       		rec.addIngredient("AlloyedPCB", 10);
       	}
       	
       	addAndSubSomeIf("CargoLiftBulk", "ChromiumBar", "MagneticMachineBlock", "ChromedMachineBlock", 0.5F, true);
       	
       	RecipeUtil.getRecipeByKey("trencher drill component").replaceIngredient("ChromiumBar", "ReikaKalseki.ChromiumWire", 2F);
       	addAndSubSomeIf("trencher drill component", "MolybdenumBar", "ReikaKalseki.MolybdenumPlate", "RackRail", 8F);
       	
       	addAndSubSomeIf("mk2trencherdrillcomponent", "MolybdenumBar", "MagneticMachineBlock", "ChromedMachineBlock", 0.5F, true);
       	if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
       		rec = RecipeUtil.getRecipeByKey("mk2trencherdrillcomponent");
       		rec.addIngredient("AlloyedPCB", 2);
       		rec.addIngredient("OrganicCutterHead", 1);
       	}
       	rec = RecipeUtil.getRecipeByKey("mk3trencherdrillcomponent");
       	rec.replaceIngredient("ChromiumBar", "HiemalMachineBlock");
       	if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
       		rec.addIngredient("UltimatePCB", 2);
       		rec.addIngredient("PlasmaCutterHead", 1);
       	}
       	rec = RecipeUtil.getRecipeByKey("trencher motor component");
       	rec.replaceIngredient("ChromiumBar", "ReikaKalseki.ChromiumPCB");
       	rec.replaceIngredient("MolybdenumBar", "ReikaKalseki.MolybdenumPCB");
       	if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
       		foreach (CraftCost cost in rec.Costs)
       			cost.Amount *= 2;
       		rec.CraftedAmount *= 2;
       		rec.addIngredient("FusionDrillMotor", 1); //three motors for two trenchers
       		rec.addIngredient("ReikaKalseki.Turbomotor", 12); //18 motors a trencher
       	}
       	
       	//these two recipes need to match because of FortressTweaks adding intercraft!
       	addAndSubSomeIf("CCBPlacement", "MolybdenumBar", "ReikaKalseki.MolybdenumPlate", "LowGradeSteelBar", 2F);
       	addAndSubSomeIf("BlastFurnacePlacement", "MolybdenumBar", "ReikaKalseki.MolybdenumPlate", "LowGradeSteelBar", 2F);
       	addAndSubSomeIf("CCBPlacement", "ChromiumBar", "ReikaKalseki.ChromiumPlate", "TitaniumHousing", 0.25F);
       	addAndSubSomeIf("BlastFurnacePlacement", "ChromiumBar", "ReikaKalseki.ChromiumPlate", "TitaniumHousing", 0.25F);
       // put foil in the pipes instead	if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) { //both recipes have matching costs
       //		RecipeUtil.getRecipeByKey("CCBPlacement").addIngredient("GoldFoil", 5);
       //		RecipeUtil.getRecipeByKey("BlastFurnacePlacement").addIngredient("GoldFoil", 5);
       //	}
       	
       	string[] pipes = new string[]{"CastingPipeStraight", "CastingPipeBend"};
       	foreach (string rk in pipes) {
	       	rec = RecipeUtil.getRecipeByKey(rk);
	       	rec.replaceIngredient("HeatConductingPipe", "ReikaKalseki.ReinforcedPipe");
	       	rec.replaceIngredient("ChromiumBar", "ReikaKalseki.ReflectiveAlloy");
	       	rec.removeIngredient("MolybdenumBar");
       	}
       	pipes = new string[]{"GenericPipeStraightkey", "GenericPipeBendkeyMP"};
       	foreach (string rk in pipes) {
	       	rec = RecipeUtil.getRecipeByKey(rk);
	       	rec.replaceIngredient("ChromedMachineBlock", "ReikaKalseki.ChromiumPipe");
	       	rec.replaceIngredient("MagneticMachineBlock", "ReikaKalseki.MolybdenumPlate");
	       	if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
	       		rec.addIngredient("TitaniumPipe", 1);
	       	}
       	}
       	
       	addAndSubSomeIf("particlefiltercomponent", "ChromedMachineBlock", "ChromedMachineBlock", "IronGear", 5);
       	addAndSubSomeIf("particlefiltercomponent", "MagneticMachineBlock", "MagneticMachineBlock", "TitaniumHousing", 1/3F);
       	addAndSubSomeIf("particlecompressorcomponent", "ChromedMachineBlock", "ChromedMachineBlock", "IronGear", 5);
       	addAndSubSomeIf("particlecompressorcomponent", "MagneticMachineBlock", "MagneticMachineBlock", "TitaniumHousing", 1/3F);
       	addAndSubSomeIf("ParticleStoragePlacementcomponent", "ChromedMachineBlock", "ChromedMachineBlock", "IronGear", 5);
       	addAndSubSomeIf("ParticleStoragePlacementcomponent", "MagneticMachineBlock", "MagneticMachineBlock", "TitaniumHousing", 1/3F);
       	addAndSubSomeIf("GasBottlerPlacementcomponent", "ChromedMachineBlock", "ChromedMachineBlock", "IronGear", 5);
       	addAndSubSomeIf("GasBottlerPlacementcomponent", "MagneticMachineBlock", "MagneticMachineBlock", "TitaniumHousing", 1/3F);
        
       	if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
       		addItemButScaleRest("ChromedMachineBlockAssembler", "GoldFoil", 3);
       		addItemButScaleRest("MagneticMachineBlockAssembler", "TitaniumHousing", 4);
       		GenericAutoCrafterNew.mMachinesByKey["HiemalMachineBlockAssembler"].Recipe.addIngredient("PlasticPellet", 2); //do not scale
       		
       		GenericAutoCrafterNew.mMachinesByKey["LensPolisher"].Recipe.addIngredient("RefinedLiquidResin", 10);       		
       	}
       	
       	GenericAutoCrafterNew.mMachinesByKey["LensChromer"].Recipe.replaceIngredient("ChromiumBar", "ReikaKalseki.ReflectiveAlloy", 1F);
       	if (GenericAutoCrafterNew.mMachinesByKey.ContainsKey("ReikaKalseki.PerfectLensChromer")) {
       		GenericAutoCrafterNew.mMachinesByKey["ReikaKalseki.PerfectLensChromer"].Recipe.replaceIngredient("ChromiumBar", "ReikaKalseki.ReflectiveAlloy", 1F);
       		GenericAutoCrafterNew.mMachinesByKey["ReikaKalseki.ExceptionalLensChromer"].Recipe.replaceIngredient("ChromiumBar", "ReikaKalseki.ReflectiveAlloy", 1F);
       	}
       	if (GenericAutoCrafterNew.mMachinesByKey.ContainsKey("ReikaKalseki.CryoSpawnerMissileCrafter")) { //cryopathy
       		GenericAutoCrafterNew.mMachinesByKey["ReikaKalseki.CryoSpawnerMissileCrafter"].Recipe.replaceIngredient("SecondaryUpgradeModule", "ReikaKalseki.ChromiumPCB", 2F); //from 1 to 2
       		GenericAutoCrafterNew.mMachinesByKey["ReikaKalseki.CryoMelterMissileCrafter"].Recipe.replaceIngredient("ChromiumBar", "ReikaKalseki.ChromiumPipe", 2F); //from 1 to 2
       		
       		rec = RecipeUtil.getRecipeByKey("ReikaKalseki.CryoMissileTurret");
       		rec.replaceIngredient("MolybdenumBar", "ReikaKalseki.MolybdenumPCB");
       		rec.replaceIngredient("ChromiumBar", "ReikaKalseki.ChromiumPCB");
       	}
       	/*
       	//FUtil.log("Pipe reinforcer ("+GenericAutoCrafterNew.mMachinesByKey["ReikaKalseki.ReinforcedPipeMaker"].Value+"="+GenericAutoCrafterNew.mMachinesByKey["ReikaKalseki.ReinforcedPipeMaker"].CubeValue+"):");
       	//FUtil.log("Ingredients:");
       	//why is this necessary, why is this one failing to link automatically
       	rec = GenericAutoCrafterNew.mMachinesByKey["ReikaKalseki.ReinforcedPipeMaker"].Recipe;
       	MaterialData.GetItemIdOrCubeValues(rec.CraftedKey, out rec.CraftableItemType, out rec.CraftableCubeType, out rec.CraftableCubeValue);
       	rec.Costs.ForEach(cc => {
       	    bool flag = MaterialData.GetItemIdOrCubeValues(cc.Key, out cc.ItemType, out cc.CubeType, out cc.CubeValue);
			//FUtil.log(cc.ingredientToString()+" {"+flag+"}");
		});
        */
        if (config.getBoolean(IFConfig.ConfigEntries.EFFICIENT_BLAST)) {
       		uint per = (uint)Math.Max(1F/DifficultySettings.mrResourcesFactor, FUtil.getOrePerBar()); //is multiplied against mrResourcesFactor, so needs to always result in >= 1!
    		foreach (CraftData br in CraftData.GetRecipesForSet("BlastFurnace")) {
       			if (br.Costs[0].Amount == 16) {
    				FUtil.log("Adjusting "+br.Costs[0].ingredientToString()+" cost of Blast Furnace recipe "+br.Key+" to "+per);
    				br.Costs[0].Amount = per;
       			}
	        }
        }
       	
       	foreach (ResearchDataEntry res in ResearchDataEntry.mEntries)
       		replaceResearchBarsWithPods(res);
		
        return registrationData;
    }
    
    private void addItemButScaleRest(string gac, string add, float scale) {
    	CraftData rec = GenericAutoCrafterNew.mMachinesByKey[gac].Recipe;
    	rec.CraftedAmount = (int)(rec.CraftedAmount*scale);
    	rec.Costs.ForEach(cc => cc.Amount = (uint)(cc.Amount*scale));
    	rec.addIngredient(add, 1);
    }
    
    private void replaceResearchBarsWithPods(string key) {
    	replaceResearchBarsWithPods(ResearchDataEntry.mEntriesByKey[key]);
    }
    
    private void replaceResearchBarsWithPods(ResearchDataEntry rec) {
    	//still 50% more expensive
       	rec.replaceIngredient("ChromiumBar", "ReikaKalseki.ChromiumExperimentalPod", 0.25F); 
       	rec.replaceIngredient("MolybdenumBar", "ReikaKalseki.MolybdenumExperimentalPod", 0.25F);
    }
    
    private void addAndSubSomeIf(string rec, string find, string replace, string sub, float ratio = 1, bool force = false) {
    	CraftData recipe = RecipeUtil.getRecipeByKey(rec);
    	if (force || config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
    		CraftCost put = find == replace ? recipe.Costs.Find(c => c.Key == find) : recipe.replaceIngredient(find, replace, 0.5F);
	    	recipe.addIngredient(sub, (uint)Mathf.Max(1, put.Amount*ratio));
    	}
    	else {
    		recipe.replaceIngredient(find, replace);
    	}
    }
    
    private static readonly MethodInfo assemblerItemFetch = typeof(ResearchAssembler).GetMethod("GetItemsForPod", BindingFlags.Instance | BindingFlags.NonPublic);
    
    public static void getResearchAssemblerRecipe(ResearchAssembler ra) {
    	if (ra.meState != ResearchAssembler.eState.eLookingForResources)
    		return;
    	foreach (CraftData rec in CraftData.GetRecipesForSet("ResearchAssembler")) {
    		CraftCost plate = rec.Costs.First(cc => cc.Amount == 6);
    		CraftCost pcb = rec.Costs.First(cc => cc.Amount == 2);
    		assemblerItemFetch.Invoke(ra, new object[]{pcb.ItemType, plate.ItemType, rec.CraftableItemType});
	    	if (ra.meState != ResearchAssembler.eState.eLookingForResources)
	    		break;
    	}
    }
    
    private static int getBeltGACItem(string set, ItemBase item) {
    	if (item == null)
    		return -1;
    	CraftData cc = CraftData.GetRecipesForSet(set).FirstOrDefault(rec => rec.Costs[0].ItemType == item.mnItemID);
    	return cc == null ? -1 : cc.CraftableItemType;
    }
    
    public static int GetPlateFromBar(ConveyorEntity stamper, ItemBase item) {
    	return getBeltGACItem("Stamper", item);
    }
    
    public static int GetPipeFromBar(ConveyorEntity extruder, ItemBase item) {
    	return getBeltGACItem("PipeExtruder", item);
    }
    
    public static int GetWireFromBar(ConveyorEntity extruder, ItemBase item) {
    	return getBeltGACItem("Extruder", item);
    }
    
    public static int GetCoilFromWire(ConveyorEntity coiler, ItemBase item) {
    	return getBeltGACItem("Coiler", item);
    }
    
    public static int GetPCBFromCoil(ConveyorEntity maker, ItemBase item) {
    	return getBeltGACItem("PCBAssembler", item);
    }
  }
}
