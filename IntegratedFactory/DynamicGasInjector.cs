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
				owner.effects[blockID] = this;
				owner.validItems.Add(requiredItem);
			}
			
			public abstract bool invoke(SegmentEntity e);
			
		}
		
		abstract class BoostEffect<E> : BoostEffectBase where E : SegmentEntity {
			
			protected BoostEffect(ushort id, int item, eSegmentEntity entity, string n) : base(id, item, entity, n) {

			}
			
			public override sealed bool invoke(SegmentEntity e) {
				return apply((E)e);
			}
			
			public abstract bool apply(E e);
			
		}
		
		class FreezonLancerEffect : BoostEffect<CreepLancer> {
			
			public FreezonLancerEffect() : base(eCubeTypes.T4_Lancer, FREEZON_ID, eSegmentEntity.T4_Creep_Lancer, "Freezon Boost") {
				
			}
			
			public override bool apply(CreepLancer e) {
				if (!e.mbHasGas) {
					e.AddGas();
					return true;
				}
				return false;
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
			
		}
		
		class BlastFurnaceBoostEffect : RateConsumptionEffect<BlastFurnace> {
			
			public BlastFurnaceBoostEffect() : base(eCubeTypes.BlastFurnace, HOT_RESIN_ID, eSegmentEntity.BlastFurnace, "Pyro Boost", 30) {
				
			}
			
			public override bool tick(BlastFurnace e, float dT) {
				if (e.mLinkedCenter != null)
					e = e.mLinkedCenter;
				//could call UpdateSmelting() to make cost more power if wanted
				if (e.mOperatingState == BlastFurnace.OperatingState.Smelting) { 
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
		
		private ushort cachedValue;
		
		private readonly Dictionary<ushort, BoostEffectBase> effects = new Dictionary<ushort, BoostEffectBase>();
		private readonly HashSet<int> validItems = new HashSet<int>();
	
		public DynamicGasInjector(ModCreateSegmentEntityParameters parameters) : base(parameters) {
			new FreezonLancerEffect().register(this); //register a new one per instance, so can store values and have callbacks
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
				ushort id = s.GetCube(this.mnX - (long)vx, this.mnY - (long)vy, this.mnZ - (long)vz);
				BoostEffectBase boost = effects.ContainsKey(id) ? effects[id] : null;
				if (boost != null && boost.requiredItem == storedGas) {
					cachedValue = s.GetCubeData(this.mnX - (long)vx, this.mnY - (long)vy, this.mnZ - (long)vz).mValue;
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

		public override bool ShouldNetworkUpdate() {
			return true;
		}

		public override void ReadNetworkUpdate(BinaryReader reader) {
			Read(reader, GetVersion());
		}

		public override void WriteNetworkUpdate(BinaryWriter writer) {
			Write(writer);
		}

		public override void Read(BinaryReader reader, int entityVersion) {
			this.mbHasHopper = reader.ReadBoolean();
			this.storedGas = reader.ReadInt32();
			if (reader.ReadBoolean())
				this.cachedEffect = effects[reader.ReadUInt16()];
		}
	
		public override void Write(BinaryWriter writer) {
			writer.Write(this.mbHasHopper);
			writer.Write(this.storedGas);
			writer.Write(cachedEffect != null);
			if (cachedEffect != null)
				writer.Write(this.cachedEffect.blockID);
		}

		public override string GetPopupText() {
			string text = base.GetPopupText();
			if (!this.mbHasHopper)
				text += "\n"+PersistentSettings.GetString("Unable_locate_Hopper");
			else if (storedGas <= 0)
				text += "\nMissing items!";
			else
				text += "\nStoring "+ItemEntry.mEntriesById[storedGas].Name+", ready to inject!";
			
			if (cachedEffect == null)
				text += "\nNo compatible machine located";
			else if (cachedEffect.requiredItem != storedGas)
				text += "\nEffect "+cachedEffect.displayName+" requires "+ItemEntry.mEntriesById[cachedEffect.requiredItem].Name+"!";
			else
				text += "\nFound "+FUtil.getBlockName(cachedEffect.blockID, cachedValue)+", applying effect: '"+cachedEffect.displayName+"'";
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
