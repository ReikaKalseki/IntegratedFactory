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
	
    public class BulkRecipe : CraftData {
		
		public static readonly BulkRecipeCategory[] categories = (BulkRecipeCategory[])Enum.GetValues(typeof(BulkRecipeCategory));
		
		public static string getTypeName(BulkRecipeCategory cat) {
			string s = cat.ToString();
			return s[0]+s.ToLowerInvariant().Substring(1)+"s";
		}
		
		public BulkRecipeCategory category { get; internal set; }
    	
    	public bool needsHeating { get; internal set; }
    	public bool needsCooling { get; internal set; }
    	
    	public TemperatureEffect heatingEffect { get; internal set; }
    	public TemperatureEffect coolingEffect { get; internal set; }
    	
    	public bool acceptHeating { get { return needsHeating || heatingEffect != null; } }
    	public bool acceptCooling { get { return needsCooling || coolingEffect != null; } }
    	
    }
	
	public abstract class TemperatureEffect {
		
		public abstract float modifyCraftTime(float orig);
		
		public abstract float modifyPPS(float orig);
		
		public abstract int modifyYield(int amount);
		
		public abstract void onCraft(BulkPartCrafter machine);
		
	}
	
	public enum BulkRecipeCategory {
		PLATE,
		WIRE,
		PIPE,
		COIL
	}
	
}
