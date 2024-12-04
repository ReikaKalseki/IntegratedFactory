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
	
	public class BulkPartCrafter : FCoreMBCrafter<BulkPartCrafter, IntegratedFactoryMod.BulkRecipe> {
		
		public static readonly uint BULK_CRAFTER_INPUT_AMOUNT = 25;
		public static readonly int BULK_CRAFTER_OUTPUT_AMOUNT = 20;
		
		public static readonly float HEATED_TEMPERATURE = 800;
		public static readonly float COOLED_TEMPERATURE = -200;
		
		public static readonly float MAX_AMBIENT_NORMALIZATION_RATE = 10;
		public static readonly float MAX_HEATING_RATE = 50;
		public static readonly float MAX_COOLING_RATE = 40;
		
		public static readonly float RESIN_DURATION = 30; //seconds
		
		public float temperature { get; private set; }
		
		public WorldUtil.Biomes currentBiome { get; private set; }
		public float ambientTemp { get; private set; }

		public float forcedCoolingTime { get; private set; }
		public float forcedHeatingTime { get; private set; }
		
		private bool mbLinkedToGO;
	
		private RotateConstantlyScript mRotCont;

		private ParticleSystem SuckParticles;
		
		public BulkPartCrafter(ModCreateSegmentEntityParameters parameters) : base(parameters, IntegratedFactoryMod.crafter, 2000, 10000, 5000, IntegratedFactoryMod.getBulkRecipes()) {
			
		}

		public override void DropGameObject() {
			base.DropGameObject();
			this.mbLinkedToGO = false;
			this.mRotCont = null;
			this.SuckParticles = null;
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
				Renderer component = this.mWrapper.mGameObjectList[0].gameObject.transform.Search("Gas").GetComponent<Renderer>();
				Material material = SegmentMeshCreator.instance.Gas_Sulphur_Swirl;//null;
				/*
				long num = this.mnY - WorldUtil.COORD_OFFSET;
				if (num < (long)BiomeLayer.CavernColdCeiling && num > (long)BiomeLayer.CavernColdFloor) {
					material = SegmentMeshCreator.instance.Gas_Freezon_Swirl;
				}
				if (num < (long)BiomeLayer.CavernToxicCeiling && num > (long)BiomeLayer.CavernToxicFloor) {
					material = SegmentMeshCreator.instance.Gas_Chlorine_Swirl;
				}
				if (num < (long)BiomeLayer.CavernMagmaCeiling && num > (long)BiomeLayer.CavernMagmaFloor) {
					material = SegmentMeshCreator.instance.Gas_Sulphur_Swirl;
				}
				*/
				if (material != null) {
					component.material = material;
				}
				else {
					component.enabled = false;
				}
				this.mbLinkedToGO = true;
			}
		}
		
		public static BulkPartCrafter testingInstance;

		public override void LowFrequencyUpdate() {
			base.LowFrequencyUpdate();
			testingInstance = this;
			
			currentBiome = WorldUtil.getBiome(this);
			ambientTemp = SurvivalLocalTemperature.GetTemperatureAtDepth(mnY);
			float dT = LowFrequencyThread.mrPreviousUpdateTimeStep;
			if (forcedHeatingTime > 0 && currentRecipe != null && currentRecipe.needsHeating) {
				temperature = Mathf.Min(temperature+MAX_HEATING_RATE*dT, HEATED_TEMPERATURE);
				forcedHeatingTime -= dT;
			}
			else if (forcedCoolingTime > 0 && currentRecipe != null && currentRecipe.needsCooling) {
				temperature = Mathf.Max(temperature-MAX_COOLING_RATE*dT, COOLED_TEMPERATURE);
				forcedCoolingTime -= dT;
			}
			else if (!Mathf.Approximately(ambientTemp, temperature)) {
				float diff = ambientTemp-temperature;
				float mag = Math.Abs(diff);
				float sign = Math.Sign(diff);
				temperature += Mathf.Min(MAX_AMBIENT_NORMALIZATION_RATE, mag)*sign*dT;
			}
			
			if (currentRecipe != null) {
				if (currentRecipe.needsHeating && forcedHeatingTime <= 0 && temperature < HEATED_TEMPERATURE) {
					if (tryConsumeTemperatureResin("ReikaKalseki.PyroResin"))
						forcedHeatingTime = RESIN_DURATION;
				}
				else if (currentRecipe.needsCooling && forcedCoolingTime <= 0 && temperature > COOLED_TEMPERATURE) {
					if (tryConsumeTemperatureResin("ReikaKalseki.CryoResin"))
						forcedCoolingTime = RESIN_DURATION;
				}
			}
		}
		
		private bool tryConsumeTemperatureResin(string item) {
			
		}
		
		public override void Write(BinaryWriter writer) {
			base.Write(writer);
			if (!this.mbIsCenter) {
				return;
			}
			writer.Write(temperature);
			writer.Write(forcedCoolingTime);
			writer.Write(forcedHeatingTime);
		}

		public override void Read(BinaryReader reader, int entityVersion) {
			base.Read(reader, entityVersion);
			if (!this.mbIsCenter) {
				return;
			}
			temperature = reader.ReadSingle();
			forcedCoolingTime = reader.ReadSingle();
			forcedHeatingTime = reader.ReadSingle();
		}
		
		public override HoloMachineEntity CreateHolobaseEntity(Holobase holobase) {
			HolobaseEntityCreationParameters holobaseEntityCreationParameters = new HolobaseEntityCreationParameters(this);
			if (this.mbIsCenter) {
				HolobaseVisualisationParameters holobaseVisualisationParameters = holobaseEntityCreationParameters.AddVisualisation(holobase.PowerStorage);
				holobaseVisualisationParameters.Scale = new Vector3((float)this.machineBounds.width, (float)this.machineBounds.height, (float)this.machineBounds.depth);
				holobaseVisualisationParameters.Color = new Color(1f, 0.7f, 0.1f);
				return holobase.CreateHolobaseEntity(holobaseEntityCreationParameters);
			}
			return null;
		}
		
		public override string GetPopupText() {
			string ret = base.GetPopupText();
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
			return ret;
		}
	
	}

}