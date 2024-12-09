/*
 * Created by SharpDevelop.
 * User: Reika
 * Date: 04/11/2019
 * Time: 11:28 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;    //For data read/write methods
using System.Collections;   //Working with Lists and Collections
using System.Collections.Generic;   //Working with Lists and Collections
using System.Linq;   //More advanced manipulation of lists/collections
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;  //Needed for most Unity Enginer manipulations: Vectors, GameObjects, Audio, etc.
using ReikaKalseki.FortressCore;

namespace ReikaKalseki.IntegratedFactory {
	
	[HarmonyPatch(typeof(ConveyorEntity))]
	[HarmonyPatch("GetItemConversionID")]
	public static class BeltGACRecipeFix {
		
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			try {
				FileLog.Log("Running patch "+MethodBase.GetCurrentMethod().DeclaringType);
				for (int i = 0; i < codes.Count; i++) {
					CodeInstruction ci = codes[i];
					if (ci.opcode == OpCodes.Call) {
						//Lib.redirectToRecipeMethod(ci);
						codes[i] = InstructionHandlers.createMethodCall(typeof(IntegratedFactoryMod), ((MethodInfo)ci.operand).Name, false, new Type[]{typeof(ConveyorEntity), typeof(ItemBase)});
					}
				}
				FileLog.Log("Done patch "+MethodBase.GetCurrentMethod().DeclaringType);
			}
			catch (Exception e) {
				FileLog.Log("Caught exception when running patch "+MethodBase.GetCurrentMethod().DeclaringType+"!");
				FileLog.Log(e.Message);
				FileLog.Log(e.StackTrace);
				FileLog.Log(e.ToString());
			}
			return codes.AsEnumerable();
		}
	}
	
	[HarmonyPatch(typeof(ResearchAssembler))]
	[HarmonyPatch("UpdateLookingForResources")]
	public static class ResearchAssemblerRecipeFix {
		
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			List<CodeInstruction> codes = new List<CodeInstruction>();
			try {
				FileLog.Log("Running patch "+MethodBase.GetCurrentMethod().DeclaringType);
				codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
				codes.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
				codes.Add(InstructionHandlers.createMethodCall(typeof(ResearchAssembler), "UpdateAttachedHoppers", true, new Type[]{typeof(bool)}));
				codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
				codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
				codes.Add(new CodeInstruction(OpCodes.Ldfld, InstructionHandlers.convertFieldOperand(typeof(ResearchAssembler), "maAttachedHoppers")));
				codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
				codes.Add(new CodeInstruction(OpCodes.Ldfld, InstructionHandlers.convertFieldOperand(typeof(ResearchAssembler), "mnNumAttachedHoppers")));
				codes.Add(InstructionHandlers.createMethodCall(typeof(IntegratedFactoryMod), "getResearchAssemblerRecipe", false, new Type[]{typeof(ResearchAssembler), typeof(StorageMachineInterface).MakeArrayType(), typeof(int)}));
				
				/*
				codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
				codes.Add(new CodeInstruction(OpCodes.Ldfld, InstructionHandlers.convertFieldOperand(typeof(ResearchAssembler), "mnNumAttachedHoppers")));
				codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
				codes.Add(new CodeInstruction(OpCodes.Ldfld, InstructionHandlers.convertFieldOperand(typeof(ResearchAssembler), "maAttachedHoppers")));
				codes.Add(InstructionHandlers.createMethodCall(typeof(IntegratedFactoryMod), "getResearchAssemblerRecipe", false, new Type[]{typeof(ResearchAssembler), typeof(int), typeof(StorageMachineInterface).MakeArrayType()}));
				 */
				
				codes.Add(new CodeInstruction(OpCodes.Ret));
				FileLog.Log("Done patch "+MethodBase.GetCurrentMethod().DeclaringType);
			}
			catch (Exception e) {
				FileLog.Log("Caught exception when running patch "+MethodBase.GetCurrentMethod().DeclaringType+"!");
				FileLog.Log(e.Message);
				FileLog.Log(e.StackTrace);
				FileLog.Log(e.ToString());
			}
			return codes.AsEnumerable();
		}
	}
	
	[HarmonyPatch(typeof(GeothermalGenerator))]
	[HarmonyPatch("CheckHopper")]
	public static class GeothermalFreezonHook {
		
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			List<CodeInstruction> codes = new List<CodeInstruction>();
			try {
				FileLog.Log("Running patch "+MethodBase.GetCurrentMethod().DeclaringType);
				codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
				codes.Add(new CodeInstruction(OpCodes.Ldarg_1));
				codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
				codes.Add(new CodeInstruction(OpCodes.Ldfld, InstructionHandlers.convertFieldOperand(typeof(GeothermalGenerator), "mrBoostTime")));
				codes.Add(InstructionHandlers.createMethodCall(typeof(IntegratedFactoryMod), "geoCheckForGas", false, new Type[]{typeof(GeothermalGenerator), typeof(StorageHopper), typeof(float)}));
				codes.Add(new CodeInstruction(OpCodes.Ret));
				FileLog.Log("Done patch "+MethodBase.GetCurrentMethod().DeclaringType);
			}
			catch (Exception e) {
				FileLog.Log("Caught exception when running patch "+MethodBase.GetCurrentMethod().DeclaringType+"!");
				FileLog.Log(e.Message);
				FileLog.Log(e.StackTrace);
				FileLog.Log(e.ToString());
			}
			return codes.AsEnumerable();
		}
	}
	
	[HarmonyPatch(typeof(BlastFurnace))]
	[HarmonyPatch("UpdateWaitingForResources")]
	public static class BlastFurnaceTimeRespect {
		
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			try {
				FileLog.Log("Running patch "+MethodBase.GetCurrentMethod().DeclaringType);
				int idx = InstructionHandlers.getInstruction(codes, 0, 0, OpCodes.Stfld, typeof(BlastFurnace), "mrSmeltTimer");
				codes[idx-1] = InstructionHandlers.createMethodCall(typeof(IntegratedFactoryMod), "getBlastFurnaceSmeltDuration", false, new Type[]{typeof(BlastFurnace)});
				codes.Insert(idx-1, new CodeInstruction(OpCodes.Ldarg_0));
				FileLog.Log("Done patch "+MethodBase.GetCurrentMethod().DeclaringType);
			}
			catch (Exception e) {
				FileLog.Log("Caught exception when running patch "+MethodBase.GetCurrentMethod().DeclaringType+"!");
				FileLog.Log(e.Message);
				FileLog.Log(e.StackTrace);
				FileLog.Log(e.ToString());
			}
			return codes.AsEnumerable();
		}
	}
	
	
	static class Lib {
		
		//internal static void redirectToRecipeMethod(CodeInstruction call) {
		//	
		//}
		
	}
	
}
