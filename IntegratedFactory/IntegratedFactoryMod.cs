﻿using UnityEngine;  //Needed for most Unity Enginer manipulations: Vectors, GameObjects, Audio, etc.
using System.IO;    //For data read/write methods
using System;    //For data read/write methods
using System.Collections.Generic;   //Working with Lists and Collections
using System.Linq;   //More advanced manipulation of lists/collections
using System.Threading;
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
       	rec.replaceIngredient("ChromiumBar", "HeimalMachineBlock");
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
       		rec.addIngredient("ReikaKalseki.Turbomotor", 1); //three motors for two trenchers
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
       		GenericAutoCrafterNew.mMachinesByKey["ChromedMachineBlockAssembler"].Recipe.addIngredient("GoldFoil", 1);
       		GenericAutoCrafterNew.mMachinesByKey["MagneticMachineBlockAssembler"].Recipe.addIngredient("TitaniumHousing", 1);
       		GenericAutoCrafterNew.mMachinesByKey["HiemalMachineBlockAssembler"].Recipe.addIngredient("PlasticPellet", 2);
       	}
       	
       	GenericAutoCrafterNew.mMachinesByKey["LensChromer"].Recipe.replaceIngredient("ChromiumBar", "ReikaKalseki.ReflectiveAlloy", 1F);
        
        if (config.getBoolean(IFConfig.ConfigEntries.EFFICIENT_BLAST)) {
    		foreach (CraftData br in CraftData.GetRecipesForSet("BlastFurnace")) {
    			if (br.Costs[0].Amount == 16)
    				br.Costs[0].Amount = FUtil.getOrePerBar();
	        }
        }
		
        return registrationData;
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
  }
}
