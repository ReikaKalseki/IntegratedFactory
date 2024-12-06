/*
 * Created by SharpDevelop.
 * User: Reika
 * Date: 04/11/2019
 * Time: 11:28 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
//For data read/write methods
using System.Collections;
//Working with Lists and Collections
using System.Collections.Generic;
//Working with Lists and Collections
using System.Linq;
//More advanced manipulation of lists/collections
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;
//Needed for most Unity Enginer manipulations: Vectors, GameObjects, Audio, etc.
using ReikaKalseki.FortressCore;

namespace ReikaKalseki.IntegratedFactory {
	
	public class BulkPartCrafter : FCoreMBCrafter<BulkPartCrafter, BulkRecipe> {
		
		public static readonly uint BULK_CRAFTER_INPUT_AMOUNT = 25;
		public static readonly int BULK_CRAFTER_OUTPUT_AMOUNT = 20;
		
		public static readonly float HEATED_TEMPERATURE = 800;
		public static readonly float COOLED_TEMPERATURE = -200;
		
		public static readonly float MAX_AMBIENT_NORMALIZATION_RATE = 10;
		public static readonly float MAX_HEATING_RATE = 50;
		public static readonly float MAX_COOLING_RATE = 40;
		
		public static readonly float RESIN_DURATION = 30; //seconds
		
		public BulkRecipeCategory category { get; private set; }
		
		public float temperature { get; private set; }
		
		public WorldUtil.Biomes currentBiome { get; private set; }
		public float ambientTemp { get; private set; }

		public float forcedCoolingTime { get; private set; }
		public float forcedHeatingTime { get; private set; }
		
		private bool mbLinkedToGO;
		
		private Renderer glowOverlay;
		private Renderer acidOverlay;
		
		public BulkPartCrafter(ModCreateSegmentEntityParameters parameters) : base(parameters, IntegratedFactoryMod.crafter, 2000, 10000, 5000, IntegratedFactoryMod.getBulkRecipes()) {
			category = BulkRecipeCategory.PLATE;
		}

		public override void DropGameObject() {
			base.DropGameObject();
			this.mbLinkedToGO = false;
		}
		
		public override void UnityUpdate() {
			if (!this.mbIsCenter) {
				return;
			}
			if (!this.mbLinkedToGO) {
				if (this.mWrapper == null || !this.mWrapper.mbHasGameObject) {
					return;
				}
				if (this.mWrapper.mGameObjectList == null) {
					Debug.LogError(multiblockData.name+" missing game object #0?");
				}
				if (this.mWrapper.mGameObjectList[0].gameObject == null) {
					Debug.LogError(multiblockData.name+" missing game object #0 (GO)?");
				}
				if (this.mValue == 1) {
					this.mWrapper.mGameObjectList[0].gameObject.transform.Rotate(0f, 90f, 0f);
				}
				glowOverlay = this.mWrapper.mGameObjectList[0].gameObject.transform.Search("Gas").GetComponent<Renderer>();
				acidOverlay = UnityEngine.Object.Instantiate(glowOverlay);
				acidOverlay.transform.parent = glowOverlay.transform.parent;
				acidOverlay.material = SegmentMeshCreator.instance.Gas_Chlorine_Swirl;
				this.mbLinkedToGO = true;
			}
			if (mbLinkedToGO) {
				if (temperature >= HEATED_TEMPERATURE) {
					glowOverlay.enabled = true;
					glowOverlay.material = SegmentMeshCreator.instance.Gas_Sulphur_Swirl;
				}
				else if (temperature <= COOLED_TEMPERATURE) {
					glowOverlay.enabled = true;
					glowOverlay.material = SegmentMeshCreator.instance.Gas_Freezon_Swirl;
				}
				else {
					glowOverlay.enabled = false;
				}
				
				if (acidOverlay) {
					acidOverlay.enabled = currentRecipe != null && currentRecipe.Costs.Count > 1 && currentRecipe.Costs[1].Key == "ReikaKalseki.AcidResin";
				}
			}
		}
		
		public static BulkPartCrafter testingInstance;

		public override void LowFrequencyUpdate() {
			//FUtil.log("mode: "+category);
			base.LowFrequencyUpdate();
			testingInstance = this;
			
			bool useHeat = mOperatingState == OperatingState.Processing;
			currentBiome = WorldUtil.getBiome(this);
			ambientTemp = SurvivalLocalTemperature.GetTemperatureAtDepth(mnY);
			float dT = LowFrequencyThread.mrPreviousUpdateTimeStep;
			
			if (useHeat && forcedHeatingTime > 0 && currentRecipe != null && currentRecipe.needsHeating) {
				temperature = Mathf.Min(temperature+MAX_HEATING_RATE*dT, HEATED_TEMPERATURE);
				forcedHeatingTime -= dT;
			}
			else if (useHeat && forcedCoolingTime > 0 && currentRecipe != null && currentRecipe.needsCooling) {
				temperature = Mathf.Max(temperature-MAX_COOLING_RATE*dT, COOLED_TEMPERATURE);
				forcedCoolingTime -= dT;
			}
			else if (!Mathf.Approximately(ambientTemp, temperature)) {
				float diff = ambientTemp-temperature;
				float mag = Math.Abs(diff);
				float sign = Math.Sign(diff);
				temperature += Mathf.Min(MAX_AMBIENT_NORMALIZATION_RATE, mag)*sign*dT;
			}
			
			if (useHeat) {
				if (currentRecipe.needsHeating && forcedHeatingTime <= 0 && temperature < HEATED_TEMPERATURE) {
					if (tryPullItems("ReikaKalseki.PyroResin")) {
						forcedHeatingTime = RESIN_DURATION;
						FloatingCombatTextManager.instance.QueueText(this.mnX, mnY, this.mnZ, 1f, "Heating", Color.red, 3f, 32f);
					}
				}
				else if (currentRecipe.needsCooling && forcedCoolingTime <= 0 && temperature > COOLED_TEMPERATURE) {
					if (tryPullItems("ReikaKalseki.CryoResin")) {
						forcedCoolingTime = RESIN_DURATION;
						FloatingCombatTextManager.instance.QueueText(this.mnX, mnY, this.mnZ, 1f, "Cooling", Color.cyan, 3f, 32f);
					}
				}
			}
		}
		
		protected override bool canProcess() {
			if (currentRecipe.needsHeating && temperature < HEATED_TEMPERATURE)
				return false;
			if (currentRecipe.needsCooling && temperature > COOLED_TEMPERATURE)
				return false;
			return true;
		}
		
		protected override bool isRecipeCurrentlyAccessible(BulkRecipe recipe) {
			return recipe.category == category;
		}
		
		public override bool onInteract(Player ep) {
			if (mOperatingState == OperatingState.Processing) {
				FloatingCombatTextManager.instance.QueueText(this.mnX, mnY, this.mnZ, 0.5F, "Busy making "+BulkRecipe.getTypeName(category), Color.red, 3f, 32f);
				AudioHUDManager.instance.HUDFail();
				return false;
			}
			category = BulkRecipe.categories[((int)category+1)%BulkRecipe.categories.Length];
			FloatingCombatTextManager.instance.QueueText(this.mnX, mnY, this.mnZ, 0.75F, "Now making "+BulkRecipe.getTypeName(category), Color.white, 3f, 32f);
			return true;
		}
		
		public override void Write(BinaryWriter writer) {
			base.Write(writer);
			if (!this.mbIsCenter) {
				return;
			}
			writer.Write(category.ToString());
			writer.Write(temperature);
			writer.Write(forcedCoolingTime);
			writer.Write(forcedHeatingTime);
		}

		public override void Read(BinaryReader reader, int entityVersion) {
			base.Read(reader, entityVersion);
			if (!this.mbIsCenter) {
				return;
			}
			string cat = reader.ReadString();
			try {
				category = (BulkRecipeCategory)Enum.Parse(typeof(BulkRecipeCategory), cat);
			}
			catch (Exception e) {
				category = BulkRecipeCategory.PLATE;
				FUtil.log("Failed to deserialize bulk recipe category '"+cat+"': "+e.ToString());
			}
			temperature = reader.ReadSingle();
			forcedCoolingTime = reader.ReadSingle();
			forcedHeatingTime = reader.ReadSingle();
		}
		
		protected override bool setupHolobaseVisuals(Holobase hb, out GameObject model, out Vector3 size, out Color color) {
			return base.setupHolobaseVisuals(hb, out model, out size, out color);
		}
		
		public override string GetUIText() {
			string ret = base.GetUIText();
			ret += "\nMaking "+BulkRecipe.getTypeName(category);
			ret += "\nEnvironment: "+currentBiome.ToString()+" ("+ambientTemp.ToString("0")+"C)";
			ret += "\nInternal Temperature: "+temperature.ToString("0.00")+"C";
			if (forcedCoolingTime > 0) {
				ret += "\nCooling, next cryo resin in "+forcedCoolingTime.ToString("0.0")+"s";
			}
			if (forcedHeatingTime > 0) {
				ret += "\nHeating, next pyro resin in "+forcedHeatingTime.ToString("0.0")+"s";
			}
			if (currentRecipe != null) {
				if (currentRecipe.needsHeating && temperature < HEATED_TEMPERATURE)
					ret += "\nTemperature too low to produce "+currentRecipe.CraftedName+"!";
				else if (currentRecipe.needsCooling && temperature > COOLED_TEMPERATURE)
					ret += "\nTemperature too high to produce "+currentRecipe.CraftedName+"!";
			}
			//UIManager.instance.Survival_Info_Panel_Label.fontSize = 15;
			return ret;
		}
	
	}

}