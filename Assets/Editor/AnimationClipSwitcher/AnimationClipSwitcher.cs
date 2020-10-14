 using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

using UnityEditor;
using UnityEditor.Animations;

namespace AnimationClipSwitcher
{

	public enum OTL // For Overall Timeline Management
	{
		Ratio = 0,
		LastFrameOnly = 1,
		None = 2
	}

	/**
	 *******************************************************
	 ************************CLASS**************************
	 *******************************************************
	 **/
	public class AnimationClipSwitcher : ScriptableObject
	{

		/**
		 *******************************************************
		 **********************MENU ITEM************************
		 *******************************************************
		 **/
		[MenuItem ("Window/Animation Clip Switcher")]
		public static void Select ()
		{
			Selection.activeObject = GetAsset;
		}

		/**
		 *******************************************************
		 **********************FIRST IMPORT*********************
		 *******************************************************
		 **/
		public string fiAnimatorPath = "";
		public string fiAnimatorName = "";
		public string fiClipsPath = "";
		public bool fiReplaceIfExist = false;


		/**
		 *******************************************************
		 ******UMPTEENTH (Love this word) IMPORT VARIABLES******
		 *******************************************************
		 **/
		public AnimatorController umAnimatorController;
		public bool umDuplicateAnimator = true;


		public bool umCopyCurve = true;
	
		public OTL umOTLCurve = OTL.Ratio;
		public bool umReplaceIfExistCurve = true;
		public List<System.Type> umCurveTypes = new List<System.Type> ();
		// This list will have all changed types in frame of the animationController
		public List<bool> umCheckedCurveTypes = new List<bool> ();
		// A reminder of which type is checked


		public string umClipsPath = "";
		public bool umFromFolderToAnimator = false;
		public bool umSearchForState = true;
		public bool umSearchWithState = false;
		public bool umAddNewClip = true;

		public bool umCopyEvent = false;
		public OTL umOTLEvent = OTL.Ratio;
		public bool umReplaceIfExistEvent = true;
		/**
		 *******************************************************
		 ******************LOG INFORMATIONS*********************
		 *******************************************************
		 **/
		public string loFilePath = "";
		public string loFileName = "";
		public bool loCreateLog = false;


		/**
		 *******************************************************
		 ******************PATH TO THE ASSET********************
		 *******************************************************
		 **/
		public const string ASSET_PATH = "Assets/Editor/AnimationClipSwitcher/AnimationClipSwitcher.asset";

		/**
		 **********************************************************
		 **********Create the asset if needed then return it*******
		 **********************************************************
		 **/
		public static AnimationClipSwitcher GetAsset {
			get {
				AnimationClipSwitcher asset;
				asset = AssetDatabase.LoadAssetAtPath (ASSET_PATH, typeof(AnimationClipSwitcher)) as AnimationClipSwitcher;

				if (!asset) {
					asset = CreateInstance<AnimationClipSwitcher> ();
					try {
						AssetDatabase.CreateAsset (asset, ASSET_PATH);
						AssetDatabase.SaveAssets ();
						AssetDatabase.Refresh ();
					} catch (UnityException ue) {
						Debug.LogError ("ASSET_PATH is invalid in AnimationClipSwitcher.cs! Error Message : " + ue.Message);
						return null;
					} 

				}
				return asset;
			}
		}
	}
}