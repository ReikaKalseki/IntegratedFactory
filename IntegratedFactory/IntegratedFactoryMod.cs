﻿using UnityEngine;  //Needed for most Unity Enginer manipulations: Vectors, GameObjects, Audio, etc.
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
  public class IntegratedFactoryMod : FCoreMod {    
		
    private static Config<IFConfig.ConfigEntries> config;

    public static MultiblockData crafter;
    
    private static readonly Dictionary<string, BulkRecipe> bulkRecipes = new Dictionary<string, BulkRecipe>();
    
    public IntegratedFactoryMod() : base("IntegratedFactory") {
    	config = new Config<IFConfig.ConfigEntries>(this);
    }
	
	public static Config<IFConfig.ConfigEntries> getConfig() {
		return config;
	}
    
    public static List<BulkRecipe> getBulkRecipes() {
    	return bulkRecipes.Values.ToList().AsReadOnly().ToList();
    }
    
    public class BulkRecipe : CraftData {
    	
    	public bool needsHeating;
    	public bool needsCooling;
    	
    }

    protected override void loadMod(ModRegistrationData registrationData) {        
        config.load();
        
        runHarmony();
        
		crafter = FUtil.registerMultiblock(registrationData, "BulkPartCrafter", MultiblockData.BOTTLER);
        
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
		
        uint resingas = (uint)(4*config.getFloat(IFConfig.ConfigEntries.RESIN_GAS_COST_SCALE)/DifficultySettings.mrResourcesFactor); //unmultiply against resource factor
        uint resinresin = (uint)(10*config.getFloat(IFConfig.ConfigEntries.RESIN_RESIN_COST_SCALE)/DifficultySettings.mrResourcesFactor);
        CraftData cresin = RecipeUtil.addRecipe("CryoResin", "ReikaKalseki.CryoResin", "", set: "Refinery");
        cresin.addIngredient("CompressedFreon", resingas);
        cresin.addIngredient("RefinedLiquidResin", resinresin);
        
        CraftData aresin = RecipeUtil.addRecipe("AcidResin", "ReikaKalseki.AcidResin", "", set: "Refinery");
        aresin.addIngredient("CompressedChlorine", resingas);
        aresin.addIngredient("RefinedLiquidResin", resinresin);
        
        CraftData fresin = RecipeUtil.addRecipe("PyroResin", "ReikaKalseki.PyroResin", "", set: "Refinery");
        fresin.addIngredient("CompressedSulphur", resingas);
        fresin.addIngredient("RefinedLiquidResin", resinresin);
        
        CraftData cpod = RecipeUtil.addRecipe("ChromiumExperimentalPod", "ReikaKalseki.ChromiumExperimentalPod", "", set: "ResearchAssembler");
        cpod.addIngredient("ReikaKalseki.ChromiumPlate", 6);
        cpod.addIngredient("ReikaKalseki.ChromiumPCB", 2);
        CraftData mpod = RecipeUtil.addRecipe("MolybdenumExperimentalPod", "ReikaKalseki.MolybdenumExperimentalPod", "", set: "ResearchAssembler");
        mpod.addIngredient("ReikaKalseki.MolybdenumPlate", 6);
        mpod.addIngredient("ReikaKalseki.MolybdenumPCB", 2);
        
        CraftData fpod = RecipeUtil.addRecipe("ColdExperimentalPod", "ReikaKalseki.ColdExperimentalPod", "", set: "ResearchAssembler");
        fpod.addIngredient("ReikaKalseki.CryoResin", 10);
        fpod.addIngredient(config.getBoolean(IFConfig.ConfigEntries.T3_T4) ? "TitaniumPipe" : "ReikaKalseki.ChromiumPipe", 2);
        
        CraftData clpod = RecipeUtil.addRecipe("ToxicExperimentalPod", "ReikaKalseki.ToxicExperimentalPod", "", set: "ResearchAssembler");
        clpod.addIngredient("ReikaKalseki.AcidResin", 10);
        clpod.addIngredient(config.getBoolean(IFConfig.ConfigEntries.T3_T4) ? "NickelPipe" : "ReikaKalseki.MolybdenumPipe", 2);
        
        CraftData spod = RecipeUtil.addRecipe("LavaExperimentalPod", "ReikaKalseki.LavaExperimentalPod", "", set: "ResearchAssembler");
        spod.addIngredient("ReikaKalseki.PyroResin", 10);
        spod.addIngredient(config.getBoolean(IFConfig.ConfigEntries.T3_T4) ? "GoldPipe" : "ReikaKalseki.ChromiumPipe", 2);
        
        CraftData rec;
        
        /* moved to GAC
        CraftData hpod = RecipeUtil.addRecipe("HiemalExperimentalPod", "ReikaKalseki.HiemalExperimentalPod", "", set: "ResearchAssembler");
        hpod.addIngredient("MagneticMachineBlock", 8);
        hpod.addIngredient("ChromedMachineBlock", 8);
        */
       //still need this part
        if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
       		rec = GenericAutoCrafterNew.mMachinesByKey["ReikaKalseki.HiemalPodMaker"].Recipe;
        	rec.Costs.ForEach(c => c.Amount *= 5);
        	rec.CraftTime *= 5;
        	rec.CraftedAmount *= 5;
        	rec.addIngredient("UltimatePCB", 1); //so still 8 blocks each but 1/5 of an ultimate pcb (2 alloyed upgrades=~10 T2 ores, 5 primary upgrades=50 tin, 10 coil=50 lith) each
        	rec.addIngredient("OverclockedCrystalClock", 5); //so 1 clock each
        	
        	//and add this
        	rec = RecipeUtil.getRecipeByKey("ReikaKalseki.HiemalPodMaker");
        	rec.addIngredient("ConductivePCB", 20);
        	rec.addIngredient("SpiderBotPowerCore", 100);
        	rec.addIngredient("TitaniumHousing", 2);
        }
        
        //moved from GAC
        CraftData apod = RecipeUtil.addRecipe("AlloyedExperimentalPod", "ReikaKalseki.AlloyedExperimentalPod", "", set: "ResearchAssembler");
        apod.addIngredient("AlloyedMachineBlock", 4);
        apod.addIngredient("AlloyedPCB", 1);
        
       	addAndSubSomeIf("powerpack2", "ChromiumBar", "ReikaKalseki.ChromiumPCB", "AlloyedPCB", 1/16F);
       	addAndSubSomeIf("powerpack2", "MolybdenumBar", "ReikaKalseki.MolybdenumPCB", "OverclockedCrystalClock", 1/4F);
       	
       	rec = RecipeUtil.getRecipeByKey("build gun mk3");
       	rec.removeIngredient("ImbuedMachineBlock"); //keep <= 5 ingredients and this is the most used elsewhere
       	float ratio = 1;
       	if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
       		rec.addIngredient("UltimatePCB", 5);
       		ratio = 150/512F;
       	}
       	rec.replaceIngredient("ChromiumBar", "ReikaKalseki.ChromiumPlate", ratio);
       	rec.replaceIngredient("MolybdenumBar", "ReikaKalseki.MolybdenumPlate", ratio);
       	
       	RecipeUtil.getRecipeByKey("poisonwarhead").replaceIngredient("CompressedChlorine", "ReikaKalseki.AcidResin", 0.2F); //from 5 to 1
       	RecipeUtil.getRecipeByKey("freezewarhead").replaceIngredient("CompressedFreon", "ReikaKalseki.CryoResin", 0.2F); //from 5 to 1
       	
       	RecipeUtil.getRecipeByKey("CryoMine").replaceIngredient("CompressedChlorine", "ReikaKalseki.AcidResin");
       	
       	RecipeUtil.getRecipeByKey("T4BurnerPlacement").replaceIngredient("CompressedSulphur", "ReikaKalseki.PyroResin", 3); //from 27 per to 81 per, and x12 as much sulfur
       	
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
       	
       	rec = RecipeUtil.getRecipeByKey("MagmaBoreComponent"); //33 total crafts necessary so the MB needs 495 pods = 4950 resins (~20k gas and ~50k liquid resin) + ~1k T2 bars
       	rec.addIngredient(cresin.CraftedKey, 15);
       	rec.addIngredient(fresin.CraftedKey, 15);
       	rec.addIngredient(aresin.CraftedKey, 15);
       	if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
       		rec.addItemPerN("UltimatePCB", 3, 2); //now needs 22 crafts and 22 ultimate upgrades
       	}
       	
       	rec = RecipeUtil.getRecipeByKey("MagmaStorage"); //63 total crafts necessary
       	rec.addIngredient("CastingPipe", 10);
       	rec.addIngredient("ReikaKalseki.ReflectiveAlloy", 32); //total just over 2k
       	rec.replaceIngredient("CompressedSulphur", "ReikaKalseki.PyroResin", 0.5F); //from 2016 sulfur and 0 resin to 4032 sulfur and a little over 10k resin
       	
       	rec = RecipeUtil.getRecipeByKey("CryoPlasmInferno"); //27 crafts necessary
       	rec.replaceIngredient("MagneticMachineBlock", "GenericPipeStraight", 1.25F); //from 4 to 5
       	rec.addIngredient("ReikaKalseki.ReflectiveAlloy", 10);
       	rec.replaceIngredient("CompressedSulphur", "ReikaKalseki.PyroResin", 0.25F); //keep same sulfur, and add 2160 resin
       	
       	addAndSubSomeIf("CargoLiftBulk", "ChromiumBar", "MagneticMachineBlock", "ChromedMachineBlock", 0.5F, true);
       	
       	RecipeUtil.getRecipeByKey("trencher drill component").replaceIngredient("ChromiumBar", "ReikaKalseki.ChromiumWire", 2F);
       	addAndSubSomeIf("trencher drill component", "MolybdenumBar", "ReikaKalseki.MolybdenumPlate", "RackRail", 8F);
       	
       	rec = RecipeUtil.getRecipeByKey("mk2trencherdrillcomponent");
       	addAndSubSomeIf("mk2trencherdrillcomponent", "MolybdenumBar", "MagneticMachineBlock", "ChromedMachineBlock", 0.5F, true);
       	rec.replaceIngredients("ReikaKalseki.AcidResin", 10, "CompressedChlorine", "RefinedLiquidResin"); //replaces 25 chlorine and 32 resin, and costs 120 chlorine and 300 resin for the whole drill
       	if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
       		rec.addIngredient("OrganicCutterHead", 2);
       	}
       	
       	rec = RecipeUtil.getRecipeByKey("mk3trencherdrillcomponent");
       	rec.replaceIngredients("ReikaKalseki.PyroResin", 20, "CompressedSulphur", "RefinedLiquidResin"); //replaces 25 sulfur and 32 resin, and costs 240 sulfur and 600 resin for the whole drill
       	rec.replaceIngredient("ChromiumBar", "HiemalMachineBlock");
       	if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
       		rec.addIngredient("PlasmaCutterHead", 2);
       	}
       	
       	rec = RecipeUtil.getRecipeByKey("trencher motor component");
       	rec.replaceIngredient("ChromiumBar", "ReikaKalseki.ChromiumPCB");
       	rec.replaceIngredient("MolybdenumBar", "ReikaKalseki.MolybdenumPCB");
       	if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
       		foreach (CraftCost cost in rec.Costs)
       			cost.Amount *= 2;
       		rec.CraftedAmount *= 2;
       		rec.addIngredient("FusionDrillMotor", 1); //three motors for two trenchers
       		rec.addIngredient("ReikaKalseki.Turbomotor", 12); //18 motors (= +36 moly) a trencher
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
       	
       	rec = RecipeUtil.getRecipeByKey("FreezonInjector");
       	if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
       		rec.replaceIngredient("T4CreepLancer", "LithiumPipe", 5); //4 to 20
       		rec.addIngredient("LightweightMachineHousing", 2);
       	}
       	
       	addAndSubSomeIf("particlefiltercomponent", "ChromedMachineBlock", "ChromedMachineBlock", "IronGear", 5);
       	addAndSubSomeIf("particlefiltercomponent", "MagneticMachineBlock", "MagneticMachineBlock", "TitaniumHousing", 1/3F);
       	addAndSubSomeIf("particlecompressorcomponent", "ChromedMachineBlock", "ChromedMachineBlock", "IronGear", 5);
       	addAndSubSomeIf("particlecompressorcomponent", "MagneticMachineBlock", "MagneticMachineBlock", "TitaniumHousing", 1/3F);
       	addAndSubSomeIf("ParticleStoragePlacementcomponent", "ChromedMachineBlock", "ChromedMachineBlock", "IronGear", 5);
       	addAndSubSomeIf("ParticleStoragePlacementcomponent", "MagneticMachineBlock", "MagneticMachineBlock", "TitaniumHousing", 1/3F);
       	addAndSubSomeIf("GasBottlerPlacementcomponent", "ChromedMachineBlock", "ChromedMachineBlock", "IronGear", 5);
       	addAndSubSomeIf("GasBottlerPlacementcomponent", "MagneticMachineBlock", "MagneticMachineBlock", "TitaniumHousing", 1/3F);
       	
       	foreach (CraftData rr in CraftData.GetRecipesForSet("Manufacturer")) {
       		replaceGasesWithResins(rr);
       	}
        
       	GenericAutoCrafterNew.mMachinesByKey["ChromedMachineBlockAssembler"].Recipe.replaceIngredient("ChromiumBar", "ReikaKalseki.ChromiumPlate");
       	GenericAutoCrafterNew.mMachinesByKey["MagneticMachineBlockAssembler"].Recipe.replaceIngredient("MolybdenumBar", "ReikaKalseki.MolybdenumPlate");
       	GenericAutoCrafterNew.mMachinesByKey["HiemalMachineBlockAssembler"].PowerTransferPerSecond = 1200;
       	GenericAutoCrafterNew.mMachinesByKey["HiemalMachineBlockAssembler"].MaxPowerStorage = 6000;
       	GenericAutoCrafterNew.mMachinesByKey["HiemalMachineBlockAssembler"].PowerTransferPerSecond = 3000;
       	
       	rec = GenericAutoCrafterNew.mMachinesByKey["CryoBombAssembler"].Recipe;
       	rec.replaceIngredient("CompressedSulphur", "ReikaKalseki.PyroResin");
       	rec.CraftedAmount *= 2; //2 gas and 5 resin each
       	rec.CraftTime *= 2; //keep rates
       	if (config.getBoolean(IFConfig.ConfigEntries.T3_T4))
       		rec.addIngredient("LithiumPipe", 1);
       	
       	if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
       		GenericAutoCrafterNew.mMachinesByKey["ChromedMachineBlockAssembler"].Recipe.addIngredient("LithiumPlate", 4);
       		//GenericAutoCrafterNew.mMachinesByKey["ChromedMachineBlockAssembler"].Recipe.addIngredient("GoldPlate", 2);
       		//GenericAutoCrafterNew.mMachinesByKey["MagneticMachineBlockAssembler"].Recipe.addIngredient("NickelPlate", 2);
       		GenericAutoCrafterNew.mMachinesByKey["MagneticMachineBlockAssembler"].Recipe.addIngredient("IronCoil", 8);
       		GenericAutoCrafterNew.mMachinesByKey["HiemalMachineBlockAssembler"].Recipe.addIngredient("PlasticPellet", 20);
       		GenericAutoCrafterNew.mMachinesByKey["HiemalMachineBlockAssembler"].Recipe.addIngredient("TitaniumHousing", 5);
       		
       		GenericAutoCrafterNew.mMachinesByKey["ChromedMachineBlockAssembler"].Recipe.scaleIOExcept(2, "ImbuedMachineBlock");
       		GenericAutoCrafterNew.mMachinesByKey["MagneticMachineBlockAssembler"].Recipe.scaleIOExcept(2, "ImbuedMachineBlock");
       		
       		GenericAutoCrafterNew.mMachinesByKey["LensChromer"].Recipe.addIngredient("RefinedLiquidResin", 10);       		
       	} 
       	
       	GenericAutoCrafterNew.mMachinesByKey["LensChromer"].Recipe.replaceIngredient("ChromiumBar", "ReikaKalseki.ReflectiveAlloy", 1F);
       	if (GenericAutoCrafterNew.mMachinesByKey.ContainsKey("ReikaKalseki.PerfectLensChromer")) {
       		GenericAutoCrafterNew.mMachinesByKey["ReikaKalseki.PerfectLensChromer"].Recipe.replaceIngredient("ChromiumBar", "ReikaKalseki.ReflectiveAlloy", 1F);
       		GenericAutoCrafterNew.mMachinesByKey["ReikaKalseki.ExceptionalLensChromer"].Recipe.replaceIngredient("ChromiumBar", "ReikaKalseki.ReflectiveAlloy", 1F);
       		
       		if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
	       		GenericAutoCrafterNew.mMachinesByKey["ReikaKalseki.PerfectLensChromer"].Recipe.addIngredient("RefinedLiquidResin", 8);  
	       		GenericAutoCrafterNew.mMachinesByKey["ReikaKalseki.ExceptionalLensChromer"].Recipe.addIngredient("RefinedLiquidResin", 5);  
       		}
       	}
       	
       	if (GenericAutoCrafterNew.mMachinesByKey.ContainsKey("ReikaKalseki.CryoSpawnerMissileCrafter")) { //cryopathy
       		GenericAutoCrafterNew.mMachinesByKey["ReikaKalseki.CryoSpawnerMissileCrafter"].Recipe.replaceIngredient("SecondaryUpgradeModule", "ReikaKalseki.ChromiumPCB", 2F); //from 1 to 2
       		GenericAutoCrafterNew.mMachinesByKey["ReikaKalseki.CryoMelterMissileCrafter"].Recipe.replaceIngredient("ChromiumBar", "ReikaKalseki.ChromiumPipe", 2F); //from 1 to 2
       		GenericAutoCrafterNew.mMachinesByKey["ReikaKalseki.CryoCrafter"].Recipe.replaceIngredient("CompressedFreon", "ReikaKalseki.CryoResin", 0.25F); //keep cost constant
       		
       		rec = RecipeUtil.getRecipeByKey("ReikaKalseki.CryoMissileTurret");
       		rec.replaceIngredient("MolybdenumBar", "ReikaKalseki.MolybdenumPCB");
       		rec.replaceIngredient("ChromiumBar", "ReikaKalseki.ChromiumPCB");
       	}
       	
        if (config.getBoolean(IFConfig.ConfigEntries.EFFICIENT_BLAST)) {
       		uint per = (uint)Math.Max(1F/DifficultySettings.mrResourcesFactor, FUtil.getOrePerBar()); //is multiplied against mrResourcesFactor, so needs to always result in >= 1!
    		foreach (CraftData br in CraftData.GetRecipesForSet("BlastFurnace")) {
       			if (br.Costs[0].Amount == 16) {
    				FUtil.log("Adjusting "+br.Costs[0].ingredientToString()+" cost of Blast Furnace recipe "+br.Key+" to "+per);
    				br.Costs[0].Amount = per;
       			}
	        }
        }
       	
       	ResearchDataEntry.mEntriesByKey["T4_drills_2"].ProjectItemRequirements.ForEach(r => r.Amount /= 2); //make cost half as much as T3 (=64 pods)
       	       	
       	foreach (ResearchDataEntry res in ResearchDataEntry.mEntries) {
       		doResearchCostReplacement(res);
       	}
       
       	ResearchDataEntry.mEntriesByKey["T4defence2"].addIngredient("ReikaKalseki.ChromiumExperimentalPod", 64); //dazzler
       	ResearchDataEntry.mEntriesByKey["T4defence2"].addIngredient("ReikaKalseki.MolybdenumExperimentalPod", 64);
       		
       	ResearchDataEntry.mEntriesByKey["T4defence3"].addIngredient("ReikaKalseki.ChromiumExperimentalPod", 16); //mines
       	ResearchDataEntry.mEntriesByKey["T4defence3"].addIngredient("ReikaKalseki.MolybdenumExperimentalPod", 16);
       		/* do not do, handled by replaceResearchBarsOrBlocksWithPods
       	ResearchDataEntry.mEntriesByKey["T4defence4"].addIngredient("ReikaKalseki.ChromiumExperimentalPod", 64); //falcors
       	ResearchDataEntry.mEntriesByKey["T4defence4"].addIngredient("ReikaKalseki.MolybdenumExperimentalPod", 64);
       		
       	ResearchDataEntry.mEntriesByKey["T4defence5"].addIngredient("ReikaKalseki.ChromiumExperimentalPod", 64); //supercharge
       	ResearchDataEntry.mEntriesByKey["T4defence5"].addIngredient("ReikaKalseki.MolybdenumExperimentalPod", 64);
       	*/
       	if (config.getBoolean(IFConfig.ConfigEntries.T3_T4)) {
       		addAlloyedPodCost("T4defence1", 16);
       		
       		addAlloyedPodCost("T4_Particles", (int)Mathf.Max(1, 128*config.getFloat(IFConfig.ConfigEntries.PARTICLE_RESEARCH_COST_SCALE)));
       		ResearchDataEntry.mEntriesByKey["T4_Particles"].addIngredient("RefinedLiquidResin", (int)Mathf.Max(1, 3000*config.getFloat(IFConfig.ConfigEntries.PARTICLE_RESEARCH_COST_SCALE)));
       		
       		ResearchDataEntry.mEntriesByKey["T4_drills_2"].addIngredient("UltimateExperimentalPod", 64); //titanium
       		ResearchDataEntry.mEntriesByKey["T4_drills_2"].addIngredient("IntermediateExperimentalPod", 64); //iron
       		ResearchDataEntry.mEntriesByKey["T4_drills_3"].addIngredient("IntermediateExperimentalPod", 128); //iron
       		addAlloyedPodCost("T4_drills_3", 128);
       		
       		//ResearchDataEntry.mEntriesByKey["T4defence1"].addIngredient("ComplexExperimentalPod", 64);
       		
       		ResearchDataEntry.mEntriesByKey["T4defence2"].addIngredient("ComplexExperimentalPod", 32);
       		addAlloyedPodCost("T4defence2", 64);
       		ResearchDataEntry.mEntriesByKey["T4defence2"].addIngredient("RefinedLiquidResin", 256);
       		
       		addAlloyedPodCost("T4defence3", 32);
       		ResearchDataEntry.mEntriesByKey["T4defence3"].addIngredient("ComplexExperimentalPod", 64);
       		
       		addAlloyedPodCost("T4defence4", 64);
       		ResearchDataEntry.mEntriesByKey["T4defence4"].addIngredient("ComplexExperimentalPod", 64);
       		
       		addAlloyedPodCost("T4defence5", 64);
       		ResearchDataEntry.mEntriesByKey["T4defence5"].addIngredient("ComplexExperimentalPod", 64);
       		ResearchDataEntry.mEntriesByKey["T4defence5"].addIngredient("RefinedLiquidResin", 512);
       		
       		ResearchDataEntry.mEntriesByKey["T4_8LightsInTheDark"].addIngredient("ReikaKalseki.ColdExperimentalPod", 1024);
       		ResearchDataEntry.mEntriesByKey["T4_8LightsInTheDark"].addIngredient("ReikaKalseki.ToxicExperimentalPod", 1024);
       		ResearchDataEntry.mEntriesByKey["T4_8LightsInTheDark"].addIngredient("ReikaKalseki.LavaExperimentalPod", 1024);
       		addAlloyedPodCost("T4_8LightsInTheDark", 256);
       	}
       	
       	float scale = config.getFloat(IFConfig.ConfigEntries.T4_RESEARCH_COST_SCALE);
       	if (scale < 0.99F || scale > 1.01F) { //do not use == 1
	       	foreach (ResearchDataEntry res in ResearchDataEntry.mEntries) {
	       		res.ProjectItemRequirements.ForEach(pp => {
					if (pp.Key == "ReikaKalseki.ChromiumExperimentalPod" || pp.Key == "ReikaKalseki.MolybdenumExperimentalPod" || pp.Key == "ReikaKalseki.HiemalExperimentalPod")
						pp.Amount = (int)Mathf.Max(1, pp.Amount*scale);
				});
	       	}
       	}
       	
       	foreach (CraftData rr in CraftData.GetRecipesForSet("Stamper")) {
       		createBulkRecipe(rr, true, false);
       	}
       	foreach (CraftData rr in CraftData.GetRecipesForSet("Extruder")) {
       		createBulkRecipe(rr, false, true);
       	}
       	foreach (CraftData rr in CraftData.GetRecipesForSet("PipeExtruder")) {
       		createBulkRecipe(rr, true, false);
       	}
       	foreach (CraftData rr in CraftData.GetRecipesForSet("Coiler")) { //make coils from pipes (equivalent cost)
       		BulkRecipe rec2 = createBulkRecipe(rr, false, true);
       		string wireID = rr.Costs[0].Key;
       		string barID = CraftData.GetRecipesForSet("Extruder").FirstOrDefault(s => s.CraftedKey == wireID).Costs[0].Key;
       		string pipeID = getBeltGACItem("PipeExtruder", barID);
       		if (pipeID == null) {
       			FUtil.log("Could not find pipe recipe made from bar '"+barID+"' via wire '"+wireID+"' to match "+rr.recipeToString());
       			continue;
       		}
       		rec2.replaceIngredient(rr.Costs[0].Key, ItemEntry.mEntriesByKey[pipeID].Key); //replace wire with pipe
       		rec2.addIngredient("ReikaKalseki.AcidResin", 2); //coils need acid to etch
       	}
       	CraftData.LinkEntries(bulkRecipes.Values.Select<BulkRecipe, CraftData>(r => r).ToList(), "Bulk");
    }
    
    private static BulkRecipe createBulkRecipe(CraftData rr, bool heat, bool cool) {
       	BulkRecipe rec2 = RecipeUtil.createNewRecipe<BulkRecipe>("Bulk"+rr.Key);
       	rec2.CraftedKey = rr.CraftedKey;
       	rec2.Category = rr.Category;
       	rec2.CraftedAmount = BulkPartCrafter.BULK_CRAFTER_OUTPUT_AMOUNT;
       	rec2.RecipeSet = "Bulk";
       	rec2.needsCooling = cool;
       	rec2.needsHeating = heat;
       	rec2.addIngredient(rr.Costs[0].Key, BulkPartCrafter.BULK_CRAFTER_INPUT_AMOUNT);
       	//do not add heat/cooling ingredient, use active tick code to consume to set temperature
       	//rec2.addIngredient("ReikaKalseki.PyroResin", 2); //plates need heating
       	bulkRecipes.Add(rr.Key, rec2);
    }
    
    public void addAlloyedPodCost(string research, int amt) {
    	ResearchDataEntry.mEntriesByKey[research].addIngredient("ReikaKalseki.AlloyedExperimentalPod", (int)Mathf.Max(1, amt*config.getFloat(IFConfig.ConfigEntries.ALLOY_RESEARCH_COST_SCALE)));
    }
    
    private void doResearchCostReplacement(string key) {
    	doResearchCostReplacement(ResearchDataEntry.mEntriesByKey[key]);
    }
    
    private void doResearchCostReplacement(ResearchDataEntry rec) {
    	float f = 0.125F; //since a pod costs 8 ingots
    	if (rec.Key == "T4_Particles")
    		f *= 8; //to 32
       	rec.replaceIngredient("ChromiumBar", "ReikaKalseki.ChromiumExperimentalPod", f);
       	rec.replaceIngredient("MolybdenumBar", "ReikaKalseki.MolybdenumExperimentalPod", f);
       	
       	rec.replaceIngredient("ChromedMachineBlock", "ReikaKalseki.ChromiumExperimentalPod", 0.5F); //since a block costs 4 ingots but a pod costs 8
       	rec.replaceIngredient("MagneticMachineBlock", "ReikaKalseki.MolybdenumExperimentalPod", 0.5F);
       	rec.replaceIngredient("HiemalMachineBlock", "ReikaKalseki.HiemalExperimentalPod", 0.25F); //made in a 8:1 ratio but double cost (also adds T1/2 ore cost)
       	
       	ProjectItemRequirement add = rec.replaceIngredient("ImbuedMachineBlock", "ReikaKalseki.ChromiumExperimentalPod", 0.5F);
       	if (add != null)
       		rec.addIngredient("ReikaKalseki.MolybdenumExperimentalPod", add.Amount);
       	
       	//original pod cost was an error, forgot upgrade takes TEN of each, costed 150-180 of each T2 rather than 15-16, net cost 230-260 intead of ~96
       	//now each pod costs 4 alloyed blocks (64 ingots) + 1 alloyed upgrade (10x each T2 "part" @ 5-6 each) -> 114 to 124 vs 16 of a block
       	rec.replaceIngredient("AlloyedMachineBlock", "ReikaKalseki.AlloyedExperimentalPod", 1/8F*config.getFloat(IFConfig.ConfigEntries.ALLOY_RESEARCH_COST_SCALE));
       	
       	f = 0.25F*config.getFloat(IFConfig.ConfigEntries.GAS_RESEARCH_COST_SCALE); //all are 10:1 and then 4:1 in the resin (40 gas per pod) but then 10x cost
       	bool removeResin = false;
       	removeResin |= rec.replaceIngredient("CompressedFreon", "ReikaKalseki.ColdExperimentalPod", f) != null;
       	removeResin |= rec.replaceIngredient("CompressedChlorine", "ReikaKalseki.ToxicExperimentalPod", f) != null;
       	removeResin |= rec.replaceIngredient("CompressedSulphur", "ReikaKalseki.LavaExperimentalPod", f) != null;
       	if (removeResin)
       		rec.removeIngredient("RefinedLiquidResin"); //since is included in the pods, probably more of it (10 gas resin per pod, 10 normal resin per gas resin)
    }
    
    private void replaceGasesWithResins(CraftData rec) {
       	float f = 0.25F; //all are 4 gas per pod
       	bool found = false;
       	found |= rec.replaceIngredient("CompressedFreon", "ReikaKalseki.CryoResin", f) != null;
       	found |= rec.replaceIngredient("CompressedChlorine", "ReikaKalseki.AcidResin", f) != null;
       	found |= rec.replaceIngredient("CompressedSulphur", "ReikaKalseki.PyroResin", f) != null;
       	if (found) {
       		bool flag = rec.removeIngredient("RefinedLiquidResin") != null;
       		FUtil.log("Replacing gases "+(flag ? "and resin " : "")+"with resin in "+rec.recipeToString());
       	}
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
    
    //DO NOT USE, HARDCODES 6/2// private static readonly MethodInfo assemblerItemFetch = typeof(ResearchAssembler).GetMethod("GetItemsForPod", BindingFlags.Instance | BindingFlags.NonPublic);
    //not necessary, just call SMI directly//private static readonly MethodInfo assemblerItemConsume = typeof(ResearchAssembler).GetMethod("RemovePlatesFromHopper", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo assemblerState = typeof(ResearchAssembler).GetMethod("SetNewState", BindingFlags.Instance | BindingFlags.NonPublic);
    
    public static void getResearchAssemblerRecipe(ResearchAssembler ra, StorageMachineInterface[] maAttachedHoppers, int mnNumAttachedHoppers) {
    	if (ra.meState != ResearchAssembler.eState.eLookingForResources)
    		return;
    	foreach (CraftData rec in CraftData.GetRecipesForSet("ResearchAssembler")) {
    		//CraftCost plate = rec.Costs.First(cc => cc.Key.Contains("Plate") || cc.Key == "AlloyedMachineBlock" || cc.Key.StartsWith("Compressed", StringComparison.InvariantCultureIgnoreCase));
    		//CraftCost pcb = rec.Costs.First(cc => cc.Key.Contains("PCB") || cc.Amount == 2);
    		//DO NOT USE//assemblerItemFetch.Invoke(ra, new object[]{pcb.ItemType, plate.ItemType, rec.CraftableItemType});
    		getResearchRecipeItems(ra, rec, maAttachedHoppers, mnNumAttachedHoppers);
	    	if (ra.meState != ResearchAssembler.eState.eLookingForResources)
	    		break;
    	}
    }
    
    private static void getResearchRecipeItems(ResearchAssembler ra, CraftData recipe, StorageMachineInterface[] hoppers, int hopperCount) {
    	CraftCost plate = recipe.Costs[0];
    	CraftCost pcb = recipe.Costs[1];
		for (int i = 0; i < hopperCount; i++) {
			if (hoppers[i].CountItems(pcb.ItemType) > 1) {
    			int plateCountInAttachedHoppers = getHoppersItemCount(plate.ItemType, hoppers, hopperCount);
				if (plateCountInAttachedHoppers >= plate.Amount) {
					ra.mTargetCreation = ItemManager.SpawnItem(recipe.CraftableItemType);
					if (hoppers[i].TryExtractItems(ra, pcb.ItemType, (int)pcb.Amount)) {
						int num = (int)plate.Amount;
						for (int j = 0; j < hopperCount; j++) {
							//num -= ra.RemovePlatesFromHopper(maAttachedHoppers[j], plate.ItemType, num);
							//num -= (int)assemblerItemConsume.Invoke(ra, new object[]{hoppers[j], plate.ItemType, num});
							num -= hoppers[j].TryPartialExtractItems(ra, plate.ItemType, num);
							if (num < 0)
								FUtil.log("Error, we removed too many "+plate.Name+"!");
							if (num <= 0)
								break;
						}
						if (num > 0)
							FUtil.log("Error, we tried to remove "+plate.Amount+" plates, but still need to remove " + num + "!");
						//ra.SetNewState(ResearchAssembler.eState.eCrafting);
						assemblerState.Invoke(ra, new object[]{ResearchAssembler.eState.eCrafting});
						return;
					}
				}
			}
		}
	}
    
	private static int getHoppersItemCount(int itemID, StorageMachineInterface[] hoppers, int hopperCount) {
		int num = 0;
		for (int i = 0; i < hopperCount; i++)
			num += hoppers[i].CountItems(itemID);
		return num;
	}
    
    private static int getBeltGACItem(string set, ItemBase item) {
    	return item == null ? -1 : getBeltGACItem(set, item.mnItemID);
    }
    
    private static string getBeltGACItem(string set, string key) {
    	CraftData cc = CraftData.GetRecipesForSet(set).FirstOrDefault(rec => rec.Costs[0].Key == key);
    	return cc == null ? null : cc.CraftedKey;
    }
    
    private static int getBeltGACItem(string set, int id) {
    	CraftData cc = CraftData.GetRecipesForSet(set).FirstOrDefault(rec => rec.Costs[0].ItemType == id);
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
    
    public override void CheckForCompletedMachine(ModCheckForCompletedMachineParameters parameters) {	 
    	if (parameters.CubeValue == crafter.placerMeta)
			crafter.checkForCompletedMachine(parameters);
	}
    
	public override ModCreateSegmentEntityResults CreateSegmentEntity(ModCreateSegmentEntityParameters parameters) {
		ModCreateSegmentEntityResults modCreateSegmentEntityResults = new ModCreateSegmentEntityResults();
		try {
			if (parameters.Cube == crafter.blockID) {
				modCreateSegmentEntityResults.Entity = new BulkPartCrafter(parameters);
				parameters.ObjectType = crafter.prefab.model;
			}
		}
		catch (Exception e) {
			FUtil.log(e.ToString());
		}
		return modCreateSegmentEntityResults;
	}
  }
}
