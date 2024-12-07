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
	
	public class DynamicGasInjector : FCoreMachine {
		
		public static readonly int COLD_RESIN_ID = ItemEntry.mEntriesByKey["ReikaKalseki.CryoResin"].ItemID;
		public static readonly int HOT_RESIN_ID = ItemEntry.mEntriesByKey["ReikaKalseki.PyroResin"].ItemID;
		public static readonly int FREEZON_ID = ItemEntry.mEntriesByKey["CompressedFreon"].ItemID;
		public static readonly int SULFUR_ID = ItemEntry.mEntriesByKey["CompressedSulphur"].ItemID;
		
		public static readonly float BLAST_SMELTING_RATE_FACTOR = 3;
		
		abstract class BoostEffectBase {
			
			public readonly ushort blockID;
			public readonly int requiredItem;
			public readonly eSegmentEntity entityType;
			public readonly string displayName;
			
			protected BoostEffectBase(ushort id, int item, eSegmentEntity entity, string n) {
				blockID = id;
				requiredItem = item;
				entityType = entity;
				displayName = n;
			}
			
			public void register(DynamicGasInjector owner) {
				if (!owner.effects.ContainsKey(blockID))
					owner.effects[blockID] = new Dictionary<int, BoostEffectBase>();
				owner.effects[blockID][requiredItem] = this;
				owner.validItems.Add(requiredItem);
			}
			
			public abstract bool invoke(SegmentEntity e);
			
			public virtual string applyTooltip(string text) {
				return text;
			}
			
		}
		
		abstract class BoostEffect<E> : BoostEffectBase where E : SegmentEntity {
			
			protected BoostEffect(ushort id, int item, eSegmentEntity entity, string n) : base(id, item, entity, n) {

			}
			
			public override sealed bool invoke(SegmentEntity e) {
				return apply((E)e);
			}
			
			public abstract bool apply(E e);
			
		}
		
		abstract class FreezonLancerEffect : BoostEffect<CreepLancer> {
			
			public FreezonLancerEffect(int id, string n) : base(eCubeTypes.T4_Lancer, id, eSegmentEntity.T4_Creep_Lancer, n) {
				
			}
			
			public override bool apply(CreepLancer e) {
				if (!e.mbHasGas) {
					e.AddGas();
					return true;
				}
				return false;
			}
			
		}
		
		class FreezonLancerBasicEffect : FreezonLancerEffect {
			
			public FreezonLancerBasicEffect() : base(FREEZON_ID, "Freezon Boost") {
				
			}
			
		}
		
		class FreezonLancerResinEffect : FreezonLancerEffect {
			
			private static readonly FieldInfo countField = typeof(CreepLancer).GetField("mnSuperChargedShots", BindingFlags.Instance | BindingFlags.NonPublic);
			
			public FreezonLancerResinEffect() : base(COLD_RESIN_ID, "Cryo Resin Boost") {
				
			}
			
			public override bool apply(CreepLancer e) {
				bool flag = base.apply(e);
				if (flag) {
					countField.SetValue(e, 100); //100 shots instead of 20
				}
				return flag;
			}
			
		}
		
		class ResinAblatorEffect : BoostEffect<LaserAblator> {
			
			public ResinAblatorEffect() : base(eCubeTypes.LaserResinAblator, FREEZON_ID, eSegmentEntity.LaserAblator, "Freezon Boost") {
				
			}
			
			public override bool apply(LaserAblator te) {
				if (!te.mbHasGas) {
					te.AddGas();
					return true;
				}
				return false;
			}
			
		}
		
		class ResinMelterEffect : BoostEffect<LaserLiquifier> {
			
			public ResinMelterEffect() : base(eCubeTypes.LaserResinLiquifier, FREEZON_ID, eSegmentEntity.LaserLiquifier, "Freezon Boost") {
				
			}
			
			public override bool apply(LaserLiquifier te) {
				if (!te.mbHasGas) {
					te.AddGas();
					return true;
				}
				return false;
			}
			
		}
		
		abstract class RateConsumptionEffect<E> : BoostEffect<E> where E : SegmentEntity {
			
			public readonly float itemLifetime;
			
			protected float currentRemainingLife;
			
			protected RateConsumptionEffect(ushort id, int item, eSegmentEntity entity, string n, float lifetime) : base(id, item, entity, n) {
				itemLifetime = lifetime;
			}
			
			public override bool apply(E e) { //doubles as tick
				if (currentRemainingLife > 0) {
					float dT = LowFrequencyThread.mrPreviousUpdateTimeStep;
					if (tick(e, dT))
						currentRemainingLife -= dT;
				}
				return currentRemainingLife <= 0; //needs new item
			}
			
			public abstract bool tick(E e, float dT);
			
			public virtual string applyTooltip(string text) {
				if (currentRemainingLife > 0)
					text += "\nEffect Duration Remaining: "+currentRemainingLife.ToString("0.0")+"s";
				return text;
			}
			
		}
		
		class BlastFurnaceBoostEffect : RateConsumptionEffect<BlastFurnace> {
			
			public BlastFurnaceBoostEffect() : base(eCubeTypes.BlastFurnace, COLD_RESIN_ID, eSegmentEntity.BlastFurnace, "Cryo Boost", 30) {
				
			}
			
			public override bool tick(BlastFurnace e, float dT) {
				if (e.mLinkedCenter != null)
					e = e.mLinkedCenter;
				//could call UpdateSmelting() to make cost more power if wanted
				if (e.mOperatingState == BlastFurnace.OperatingState.Smelting) { 
					//e.rotateConstantlyScript.spinFaster();
					e.mrSmeltTimer -= dT*(BLAST_SMELTING_RATE_FACTOR-1);
					return true;
				}
				return false;
			}
			
		}
		
		class CCBBoostEffect : RateConsumptionEffect<ContinuousCastingBasin> {
			
			public CCBBoostEffect() : base(eCubeTypes.ContinuousCastingBasin, COLD_RESIN_ID, eSegmentEntity.ContinuousCastingBasin, "Cryo Boost", 10) {
				
			}
			
			public override bool tick(ContinuousCastingBasin e, float dT) {
				if (e.mLinkedCenter != null)
					e = e.mLinkedCenter;
				bool flag = false;
				for (int i = 0; i < 4; i++) {
					if (e.mItemsCooling[i] != null) {
						e.mItemCoolTimers[i] -= dT*(BLAST_SMELTING_RATE_FACTOR-1);
						flag = true;
					}
				}
				return flag;
			}
			
		}

		private bool mbLinkedToGO;

		public Vector3 mForwards;

		private GameObject jetFX;

		private float mrJetOffset;

		private GameObject hopperRender;

		private bool mbHasHopper;

		private int storedGas;

		private BoostEffectBase cachedEffect;
		
		private ushort cachedID;
		private ushort cachedValue;
		
		private readonly Dictionary<ushort, Dictionary<int, BoostEffectBase>> effects = new Dictionary<ushort, Dictionary<int, BoostEffectBase>>();
		private readonly HashSet<int> validItems = new HashSet<int>();
	
		public DynamicGasInjector(ModCreateSegmentEntityParameters parameters) : base(parameters) {
			new FreezonLancerBasicEffect().register(this); //register a new one per instance, so can store values and have callbacks
			new FreezonLancerResinEffect().register(this);
			new ResinAblatorEffect().register(this);
			new ResinMelterEffect().register(this);
			new BlastFurnaceBoostEffect().register(this);
			new CCBBoostEffect().register(this);
		}
		
		public override void DropGameObject() {
			base.DropGameObject();
			this.mbLinkedToGO = false;
		}

		public override void UnityUpdate() {
			if (!this.mbLinkedToGO) {
				if (this.mWrapper == null || !this.mWrapper.mbHasGameObject) {
					return;
				}
				if (this.mWrapper.mGameObjectList == null) {
					Debug.LogError("RA missing game object #0?");
				}
				if (this.mWrapper.mGameObjectList[0].gameObject == null) {
					Debug.LogError("RA missing game object #0 (GO)?");
				}
				this.mbLinkedToGO = true;
				this.jetFX = this.mWrapper.mGameObjectList[0].gameObject.transform.Search("Jet").gameObject;
				this.hopperRender = this.mWrapper.mGameObjectList[0].gameObject.transform.Search("Hopper_Tut").gameObject;
			}
			if (this.hopperRender.activeSelf == this.mbHasHopper) {
				this.hopperRender.SetActive(!this.mbHasHopper);
			}
			if (this.cachedEffect != null) {
				if (this.mrJetOffset < 0f) {
					this.mrJetOffset += Time.deltaTime;
				}
			}
			else
			if (this.mrJetOffset > -0.5f) {
				this.mrJetOffset -= Time.deltaTime;
			}
			if (this.mDistanceToPlayer < 32f) {
				this.jetFX.transform.localPosition = new Vector3(0f, this.mrJetOffset, 0f);
			}
		}

		public override void LowFrequencyUpdate() {
			this.UpdatePlayerDistanceInfo();
			if (!WorldScript.mbIsServer) {
				return;
			}
			if (mForwards.magnitude < 0.9)
				this.OnUpdateRotation(mFlags);
			int vx = (int)this.mForwards.x;
			int vy = (int)this.mForwards.y;
			int vz = (int)this.mForwards.z;
			Segment s;
			if (storedGas <= 0) {
				s = AttemptGetSegment(this.mnX + (long)vx, this.mnY + (long)vy, this.mnZ + (long)vz);
				if (s != null) {
					ushort cube = s.GetCube(this.mnX + (long)vx, this.mnY + (long)vy, this.mnZ + (long)vz);
					bool flag = false;
					this.mbHasHopper = true;
					SegmentEntity e = s.SearchEntity(this.mnX + (long)vx, this.mnY + (long)vy, this.mnZ + (long)vz);
					if (e is StorageMachineInterface && !e.mbDelete) {
						StorageMachineInterface smi = (StorageMachineInterface)e;
						flag = true;
						foreach (int id in validItems) {
							if (FUtil.getCount(id, smi) > 0) {
								this.storedGas = id;
								FUtil.removeFromInventory(id, 1, smi, this);
								this.RequestImmediateNetworkUpdate();
								break;
							}
						}
					}
					if (this.mbHasHopper != flag)
						this.RequestImmediateNetworkUpdate();
					this.mbHasHopper = flag;
				}
			}
			if (storedGas <= 0)
				return;
			s = AttemptGetSegment(this.mnX - (long)vx, this.mnY - (long)vy, this.mnZ - (long)vz);
			if (s != null) {
				cachedID = s.GetCube(this.mnX - (long)vx, this.mnY - (long)vy, this.mnZ - (long)vz);
				cachedValue = s.GetCubeData(this.mnX - (long)vx, this.mnY - (long)vy, this.mnZ - (long)vz).mValue;
				Dictionary<int, BoostEffectBase> dict = effects.ContainsKey(cachedID) ? effects[cachedID] : null;
				BoostEffectBase boost = dict != null && dict.ContainsKey(storedGas) ? dict[storedGas] : null;
				if (boost != null) {
					SegmentEntity e = s.FetchEntity(boost.entityType, this.mnX - (long)vx, this.mnY - (long)vy, this.mnZ - (long)vz);
					if (e != null && !e.mbDelete && boost.invoke(e)) {
						this.storedGas = -1;
						this.RequestImmediateNetworkUpdate();
					}
				}
				if (this.cachedEffect != boost) {
					this.cachedEffect = boost;
					this.RequestImmediateNetworkUpdate();
				}
			}
		}
		
		public override void OnDelete() {
			base.OnDelete();
			if (storedGas > 0) {
				if (WorldScript.mbIsServer) {
					FUtil.dropItem(this.mnX, this.mnY, this.mnZ, storedGas);
				}
			}
		}

		public override bool ShouldNetworkUpdate() {
			return true;
		}

		public override void ReadNetworkUpdate(BinaryReader reader) {
			Read(reader, GetVersion());
			if (reader.ReadBoolean()) {
				ushort id = reader.ReadUInt16();
				int item = reader.ReadInt32();
				try {
					this.cachedEffect = effects[id][item];
				}
				catch (Exception ex) {
					FUtil.log("Failed to deserialize cached effect from id/item pair: "+id+"/"+item);
				}
			}
		}

		public override void WriteNetworkUpdate(BinaryWriter writer) {
			Write(writer);
			writer.Write(cachedEffect != null);
			if (cachedEffect != null) {
				writer.Write(this.cachedEffect.blockID);
				writer.Write(this.cachedEffect.requiredItem);
			}
		}

		public override void Read(BinaryReader reader, int entityVersion) {
			this.mbHasHopper = reader.ReadBoolean();
			this.storedGas = reader.ReadInt32();
		}
	
		public override void Write(BinaryWriter writer) {
			writer.Write(this.mbHasHopper);
			writer.Write(this.storedGas);
		}

		public override string GetPopupText() {
			string text = base.GetPopupText();
			if (!this.mbHasHopper)
				text += "\n"+PersistentSettings.GetString("Unable_locate_Hopper");
			else if (storedGas <= 0)
				text += "\nMissing items!";
			else
				text += "\nStoring "+ItemEntry.mEntriesById[storedGas].Name+", ready to inject!";
			
			if (!mbHasHopper || storedGas <= 0)
				return text;
			
			if (cachedID <= 0)
				text += "\nNo target machine found.";
			else if (cachedEffect == null)
				text += "\nNo "+ItemEntry.mEntriesById[storedGas].Name+" compatibility for machine "+FUtil.getBlockName(cachedID, cachedValue);
			else if (cachedEffect.requiredItem != storedGas) //should not happen anymore
				text += "\nEffect "+cachedEffect.displayName+" requires "+ItemEntry.mEntriesById[cachedEffect.requiredItem].Name+"!";
			else {
				text += "\nFound "+FUtil.getBlockName(cachedID, cachedValue)+", applying effect: '"+cachedEffect.displayName+"'";
				text = cachedEffect.applyTooltip(text);
			}
			return text;
		}

		public override void OnUpdateRotation(byte newFlags) {
			base.OnUpdateRotation(newFlags);
			this.mFlags = newFlags;
			this.mForwards = SegmentCustomRenderer.GetRotationQuaternion(this.mFlags) * Vector3.forward;
			this.mForwards.Normalize();
		}
	}
	
}
