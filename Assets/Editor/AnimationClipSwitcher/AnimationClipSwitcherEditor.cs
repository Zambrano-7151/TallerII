using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

using UnityEditor;
using UnityEditor.Animations;

namespace AnimationClipSwitcher
{
	/**
	 *******************************************************
	 ************************CLASS**************************
	 *******************************************************
	 **/
	[CustomEditor (typeof(AnimationClipSwitcher))]
	public class AnimationClipSwitcherEditor : Editor
	{
		private AnimationClipSwitcher acs;
		private AnimatorController tempController;
		
		private List<string> log;
		
		private int indexTemp;
		
		private bool animatorExist = true;
		private bool keyFrameTypeSelected = false;
		
		private string text;
		
		/**
		*******************************************************
		Just get the target
		*******************************************************
		**/
		private void OnEnable ()
		{
			acs = (AnimationClipSwitcher)target;
		}
		
		/**
		*******************************************************
		Draw the inspector
		*******************************************************
		**/
		public override void OnInspectorGUI ()
		{	//Styles
			GUIStyle marginLeft = new GUIStyle ();
			GUIStyle styleFoldOut = EditorStyles.foldout;
			marginLeft.margin = new RectOffset (10, 0, 0, 0);
			styleFoldOut.fontStyle = FontStyle.Bold;
			styleFoldOut.alignment = TextAnchor.MiddleLeft;
			
			/**
			*******************************************************
			Most of the time the animator we'll be already created. So it'll be the default open one
			*******************************************************
			**/
			animatorExist = EditorGUILayout.Foldout (animatorExist, "Change or duplicate an animator", styleFoldOut);
			if (animatorExist) {
				GUILayout.BeginVertical ("box");
				EditorGUILayout.HelpBox ("Change or duplicate an animator", MessageType.Info);
				
				//ANIMATOR
				EditorGUILayout.LabelField ("Animator", EditorStyles.boldLabel);
				acs.umAnimatorController = (AnimatorController)EditorGUILayout.ObjectField ("Animator", acs.umAnimatorController, typeof(AnimatorController), false);
				acs.umDuplicateAnimator = EditorGUILayout.Toggle ("Duplicate", acs.umDuplicateAnimator);
				
				//SEARCH PARAMETERS
				EditorGUILayout.LabelField ("\n");
				EditorGUILayout.LabelField ("Search Parameters", EditorStyles.boldLabel);
				acs.umClipsPath = EditorGUILayout.TextField ("Clips folder path", acs.umClipsPath);
				
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Search based on ", new GUILayoutOption[]{ GUILayout.MaxWidth (EditorGUIUtility.labelWidth - 4) }); // this weird 4 seem to be some kind of margin I can't get rid of for the moment
				
				indexTemp = (acs.umFromFolderToAnimator ? 0 : 1);
				indexTemp = GUILayout.SelectionGrid (indexTemp, new string[2] { "Folder", "Animator" }, 2, "toggle");
				acs.umFromFolderToAnimator = (indexTemp == 0);
				
				EditorGUILayout.EndHorizontal ();
				
				if (acs.umFromFolderToAnimator) {
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Search for ", new GUILayoutOption[]{ GUILayout.MaxWidth (EditorGUIUtility.labelWidth - 4) }); // this weird 4 seem to be some kind of margin I can't get rid of for the moment
					
					indexTemp = (acs.umSearchForState ? 0 : 1);
					indexTemp = GUILayout.SelectionGrid (indexTemp, new string[2] { "States", "Clips" }, 2, "toggle");
					acs.umSearchForState = (indexTemp == 0);
					
					EditorGUILayout.EndHorizontal ();
				} else {
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Search using ", new GUILayoutOption[]{ GUILayout.MaxWidth (EditorGUIUtility.labelWidth - 4) });// this weird 4 seem to be some kind of margin I can't get rid of for the moment
					
					indexTemp = (acs.umSearchWithState ? 0 : 1);
					indexTemp = GUILayout.SelectionGrid (indexTemp, new string[2] { "States", "Clips" }, 2, "toggle");
					acs.umSearchWithState = (indexTemp == 0);
					
					EditorGUILayout.EndHorizontal ();
				}
				
				EditorGUI.BeginDisabledGroup (!acs.umFromFolderToAnimator);
				acs.umAddNewClip = EditorGUILayout.Toggle ("Add new clips to animator", acs.umAddNewClip);
				EditorGUI.EndDisabledGroup ();
				
				if (acs.umFromFolderToAnimator) {
					if (acs.umSearchForState) {
						EditorGUILayout.HelpBox ("Will use clips present in the folder to search for the animator's \"STATES\".", MessageType.Info);
					} else {
						EditorGUILayout.HelpBox ("Will use clips present in the folder to search for the animator's \"CLIPS\".", MessageType.Info);
					}					
				} else {
					if (acs.umSearchWithState) {
						EditorGUILayout.HelpBox ("Will use \"STATES\" present in the animator to search the folder.", MessageType.Info);
					} else {
						EditorGUILayout.HelpBox ("Will use \"CLIPS\" present in the animator to search the folder.", MessageType.Info);
					}
				}
				
				//CURVES DATA
				EditorGUILayout.LabelField ("\n");
				EditorGUILayout.LabelField ("Curves data", EditorStyles.boldLabel);
				//Creating the list for the properties on each KeyFrame
				if (acs.umAnimatorController != null) {
					//It means we change so let's create the list
					if (!acs.umAnimatorController.Equals (tempController)) {
						tempController = acs.umAnimatorController;
						FillCurveTypes ();
					}
				} else {//The list need to be emptied
					tempController = null;
					acs.umCurveTypes = new List<System.Type> ();
					acs.umCheckedCurveTypes = new List<bool> ();
				}
				acs.umCopyCurve = EditorGUILayout.Toggle ("Activate copy", acs.umCopyCurve);
				acs.umReplaceIfExistCurve = EditorGUILayout.Toggle ("Replace if exist", acs.umReplaceIfExistCurve);
				
				//Here Drawing the list.
				EditorGUILayout.LabelField ("Types list :");
				EditorGUILayout.BeginVertical (marginLeft);
				keyFrameTypeSelected = false;
				if (acs.umCurveTypes.Count > 0) { 
					for (int i = 0; i < acs.umCurveTypes.Count; i++) {
						acs.umCheckedCurveTypes [i] = EditorGUILayout.Toggle ("- " + GetTypeShorterName (acs.umCurveTypes [i].ToString ()), acs.umCheckedCurveTypes [i]);
						
						if (acs.umCheckedCurveTypes [i]) {
							keyFrameTypeSelected = true;// We know that we have at least one type selected. So we have to process keyframes
						}
					}
				} else {
					EditorGUILayout.HelpBox ("No properties to copy in this animator!", MessageType.Warning);
				}
				EditorGUILayout.EndVertical ();
				
				EditorGUILayout.LabelField ("Overall timeline management :");
				EditorGUILayout.BeginVertical (marginLeft);
				
				indexTemp = (int)acs.umOTLCurve;
				indexTemp = GUILayout.SelectionGrid (indexTemp, new string[3] { "Ratio", "Last Frame Only", "None" }, 1, "toggle");
				acs.umOTLCurve = (OTL)indexTemp;
				
				EditorGUILayout.EndVertical ();
				
				if (acs.umCopyCurve) {
					text = "Copying curves.";
					if (acs.umOTLCurve == OTL.LastFrameOnly) {
						text += " Last frames properties will always be copied on the last frame.";
					} else if (acs.umOTLCurve == OTL.Ratio) {
						text += " The process will keep the ratio if any timeline changes are detected.";
					}
				} else {
					text = "Copy disabled.";
				}
				EditorGUILayout.HelpBox (text, MessageType.Info);
				
				//EVENTS
				EditorGUILayout.LabelField ("\n");
				EditorGUILayout.LabelField ("Events", EditorStyles.boldLabel);
				acs.umCopyEvent = EditorGUILayout.Toggle ("Activate copy", acs.umCopyEvent);
				acs.umReplaceIfExistEvent = EditorGUILayout.Toggle ("Replace if exist", acs.umReplaceIfExistEvent);
				
				
				EditorGUILayout.LabelField ("Overall timeline management :");
				EditorGUILayout.BeginVertical (marginLeft);
				
				indexTemp = (int)acs.umOTLEvent;
				indexTemp = GUILayout.SelectionGrid (indexTemp, new string[3] { "Ratio", "Last Frame Only", "None" }, 1, "toggle");
				acs.umOTLEvent = (OTL)indexTemp;
				
				EditorGUILayout.EndVertical ();
				
				if (acs.umCopyEvent) {
					text = "Copying events.";
					if (acs.umOTLEvent == OTL.LastFrameOnly) {
						text += " Last frames events will always be copied on the last frame.";
					} else if (acs.umOTLEvent == OTL.Ratio) {
						text += " The process will keep the ratio if any timeline changes are detected.";
					}
				} else {
					text = "Copy disabled.";
				}
				EditorGUILayout.HelpBox (text, MessageType.Info);
				
				GUILayout.EndVertical ();
			} 
			
			EditorGUILayout.LabelField ("\n");
			/**
			*******************************************************
			But if the animatorController doesn't exists
			*******************************************************
			**/	
			animatorExist = !EditorGUILayout.Foldout (!animatorExist, "Create a new animator", styleFoldOut);
			if (!animatorExist) {
				GUILayout.BeginVertical ("box");
				//here : creating some fields to get our needed informations
				EditorGUILayout.HelpBox ("Create a new animator using clips contained in the \"Clips folder\"", MessageType.Info);
				acs.fiAnimatorPath = EditorGUILayout.TextField ("Animator path", acs.fiAnimatorPath);
				acs.fiAnimatorName = EditorGUILayout.TextField ("Animator name", acs.fiAnimatorName);
				acs.fiClipsPath = EditorGUILayout.TextField ("Clips folder path", acs.fiClipsPath);
				acs.fiReplaceIfExist = EditorGUILayout.Toggle ("Replace if exist", acs.fiReplaceIfExist);
				GUILayout.EndVertical ();
			}
			/**
			*******************************************************
			LOGS Informations
			*******************************************************
			**/	
			
			EditorGUILayout.LabelField ("\n");
			EditorGUILayout.LabelField ("Log File", EditorStyles.boldLabel);
			GUILayout.BeginVertical ("box");
			EditorGUILayout.HelpBox ("This plugin can create a log file to help you follow is work.", MessageType.Info);
			acs.loFilePath = EditorGUILayout.TextField ("File path", acs.loFilePath);
			acs.loFileName = EditorGUILayout.TextField ("File name", acs.loFileName);
			acs.loCreateLog = EditorGUILayout.Toggle ("Create file", acs.loCreateLog);
			GUILayout.EndVertical ();
			
			
			/**
			*******************************************************
			The start button
			*******************************************************
			**/	
			if (GUILayout.Button ("Start")) {
				if (!animatorExist) {
					// Checking user's informations in paths and names.
					acs.fiAnimatorPath = CheckPath (acs.fiAnimatorPath);
					
					string temp;
					if (acs.fiAnimatorName.Length > 0) {
						temp = acs.fiAnimatorName;
						if (temp.IndexOf ("controller") == -1) {
							acs.fiAnimatorName += ".controller";
						}
					} else {
						Debug.LogError ("You must specify a name for the animator controller!");
						return;
					}
					
					acs.fiClipsPath = CheckPath (acs.fiClipsPath);	
					
					//Reset editor and save the asset settings
					AssetDatabase.SaveAssets ();
					Repaint ();
					
					//Starting the process
					CreateNewAnimator ();
				} else {
					// Checking user's informations in paths and names.
					acs.umClipsPath = CheckPath (acs.umClipsPath);
					
					if (acs.umAnimatorController == null) {
						Debug.LogError ("You must choose an animator!");
					}
					
					//Reset editor and save the asset settings
					AssetDatabase.SaveAssets ();
					Repaint ();
					
					if (acs.umFromFolderToAnimator) {
						FromFolderToAnimator ();
					} else {
						FromAnimatorToFolder ();
					}
				}
				
				//Then the log is gonna be created
				if (acs.loCreateLog) {
					CheckLogInformation ();
				}
			}
			
			EditorUtility.SetDirty (acs);
		}
		
		
		/**
		*******************************************************
		Create new animator with all clips put in a state
		*******************************************************
		**/
		void CreateNewAnimator ()
		{
			log = new List<string> ();
			
			string animatorPath = acs.fiAnimatorPath + acs.fiAnimatorName;
			string clipsFolder = acs.fiClipsPath;
			
			//Getting all the clips
			AnimationClip[] animationClips = GetAnimationClipsAtPath (clipsFolder);
			
			if (animationClips.Length == 0) {
				Debug.LogError ("There is no clip in this folder!");
				return;
			}
			
			//Checking if an animator exists at animatorPath
			AnimatorController controller = AssetDatabase.LoadAssetAtPath (animatorPath, typeof(AnimatorController)) as AnimatorController;
			
			if (controller != null) {
				if (acs.fiReplaceIfExist) {
					//Deleting it if necessary
					AssetDatabase.DeleteAsset (animatorPath);
					log.Add ("Deleting old asset at : \"" + animatorPath + "\"");
					log.Add ("Creating new asset at : \"" + animatorPath + "\"");
				} else {
					
					if (AssetDatabase.LoadAssetAtPath (animatorPath, typeof(AnimatorController)) != null) {
						animatorPath = GetAnimatorControllerName (animatorPath);
					}
					log.Add ("Creating new asset. Automatic name generated :\"" + animatorPath + "\"");
				}
			} else {
				log.Add ("Creating new asset at : \"" + animatorPath + "\"");
			}
			
			//Create the new one
			try {
				controller = new AnimatorController ();
				AssetDatabase.CreateAsset (controller, animatorPath);
				AssetDatabase.Refresh ();
			} catch (UnityException ue) {
				Debug.LogError ("Animator path doesn't exists! Error Message : " + ue.Message);
				return;
			}
			
			//Creating a Layer
			controller.AddLayer (controller.name + " Layer");
			log.Add ("\t--> Creating layer : \"" + controller.name + " Layer\"");
			
			//Adding all the clips
			foreach (AnimationClip animationClip in animationClips) {
				controller.AddMotion (animationClip, 0);
				log.Add ("\t--> Adding Motion : \"" + animationClip.name + "\"");
			}
			
			if (acs.loCreateLog)
				CreateLogFile ();
			
			Debug.Log ("Animation Clip Switcher is done!");
		}
		
		/**
		*******************************************************
		Search into folder using animator's informations
		*******************************************************
		**/
		void FromAnimatorToFolder ()
		{
			log = new List<string> ();
			
			//Getting all clips in folder
			string clipsFolder = acs.umClipsPath;
			AnimationClip[] animationClipsFolder = GetAnimationClipsAtPath (clipsFolder);	
			
			if (animationClipsFolder.Length == 0) {
				Debug.LogError ("There is no clip in this folder!");
				return;
			}
			
			//Get the controller that we need
			AnimatorController controller = acs.umAnimatorController;
			AnimationClip[] animationClipsController = controller.animationClips;
			
			if (acs.umDuplicateAnimator) { // Duplicate checked. Let's get the new animator.
				controller = DuplicateAnimatorController (controller);
			}
			
			// Get all states
			List<AnimatorState> states = GetStatesFromAnimatorController (controller);
			AnimationClip animationClipFolder;
			
			if (acs.umSearchWithState) {// Based on states name	
				AnimationClip animationClipController;
				
				log.Add ("Searching the folder using states name");
				
				//For each state
				foreach (AnimatorState state in states) {		
					log.Add ("\t--> State : \"" + state.name + "\"");
					//Getting the clip in the folder
					animationClipFolder = ArrayUtility.Find (animationClipsFolder, delegate (AnimationClip ac) {
						return ac.name == state.name; //using the state.name
					});
					
					if (animationClipFolder == null) {//Didn't find the clip corresponding to the state
						log.Add ("\t\t- Warning : Coundn't find clip named \"" + state.name + "\" in folder\r\n");
					} else {
						log.Add ("\t\t- New clip Found"); 
						if (state.motion != null) {
							animationClipController = ArrayUtility.Find (animationClipsController, delegate(AnimationClip ac) {
								return ac.name == state.motion.name; // Getting the current clip
							});
							if (keyFrameTypeSelected && acs.umCopyCurve) {
								CopyCurves (animationClipController, animationClipFolder);
							}
							if (acs.umCopyEvent) {
								CopyEvents (animationClipController, animationClipFolder);
							}
						}
						
						//Change the motion
						state.motion = animationClipFolder;	
						log.Add ("\t\t- New clip copied\r\n");
					}
				}
			} else {
				AnimatorState state;
				log.Add ("Searching the folder using clips name");
				foreach (AnimationClip animationClipController in animationClipsController) {
					log.Add ("\t--> AnimationClip : \"" + animationClipController.name + "\"");
					//Get the clip in the folder using the clip name in the animator
					animationClipFolder = ArrayUtility.Find (animationClipsFolder, delegate(AnimationClip ac) {
						return ac.name == animationClipController.name;
					});
					
					if (animationClipFolder == null) {
						log.Add ("\t\t- Warning : Coundn't find clip named \"" + animationClipController.name + "\" in folder\r\n");
					} else {
						log.Add ("\t\t- New clip Found");
						
						//Get the state using this clip
						state = states.Find (delegate(AnimatorState st) {
							if (st.motion == null)
								return false;
							else
								return st.motion.name == animationClipController.name;
						});
						
						if (keyFrameTypeSelected && acs.umCopyCurve) {
							CopyCurves (animationClipController, animationClipFolder);
						}
						
						if (acs.umCopyEvent) {
							CopyEvents (animationClipController, animationClipFolder);
						}
						
						//Change the motion on the state
						state.motion = animationClipFolder;
						log.Add ("\t\t- New clip copied\r\n");
					}
				}
			}
			
			if (acs.loCreateLog)
				CreateLogFile ();
			
			Debug.Log ("Animation Clip Switcher is done!");
		}
		
		
		/**
		*******************************************************
		Search into animator using clips in the folder
		*******************************************************
		**/
		void FromFolderToAnimator ()
		{
			log = new List<string> ();
			
			//Getting all clips in folder
			string clipsFolder = acs.umClipsPath;
			AnimationClip[] animationClipsFolder = GetAnimationClipsAtPath (clipsFolder);	
			
			if (animationClipsFolder.Length == 0) {
				Debug.LogError ("There is no clip in this folder!");
				return;
			}
			
			//Get the controller that we need
			AnimatorController controller = acs.umAnimatorController;
			AnimationClip[] animationClipsController = controller.animationClips;
			
			if (acs.umDuplicateAnimator) {//Duplicate checked. Let's get the new animator
				controller = DuplicateAnimatorController (controller);
			}
			
			
			
			AnimationClip animationClipController;
			AnimatorState state;
			List<AnimatorState> states = GetStatesFromAnimatorController (controller);
			
			if (acs.umSearchForState) {
				log.Add ("Searching the animator's state using new clips name");
				foreach (AnimationClip animationClipFolder in animationClipsFolder) {
					log.Add ("\t--> AnimationClip : \"" + animationClipFolder.name + "\"");
					
					//Get the state
					state = states.Find (delegate(AnimatorState st) {
						return st.name == animationClipFolder.name;
					});
					
					if (state == null) { //It means there is not state with a motion.name == clipFolder.name
						//Create new state or error
						if (acs.umAddNewClip) {
							controller.AddMotion (animationClipFolder);
							log.Add ("\t\t- State not Found. Creating new one named : \"" + animationClipFolder.name + "\"\r\n");
						} else {
							log.Add ("\t\t- Warning! Couldn't find state named : \"" + animationClipFolder.name + "\"\r\n");
						}
					} else {
						log.Add ("\t\t- State found");
						//Get the clip if there is one
						if (state.motion != null) {
							animationClipController = ArrayUtility.Find (animationClipsController, delegate(AnimationClip ac) {
								return ac.name == state.motion.name; // Getting the current clip
							});
							
							if (keyFrameTypeSelected && acs.umCopyCurve) {
								CopyCurves (animationClipController, animationClipFolder);
							}
							if (acs.umCopyEvent) {
								CopyEvents (animationClipController, animationClipFolder);
							}
						}
						//Change the motion
						state.motion = animationClipFolder;	
						log.Add ("\t\t- New clip copied\r\n");
					}
				}
			} else {
				log.Add ("Searching the animator's clips using new clips name");
				foreach (AnimationClip animationClipFolder in animationClipsFolder) {
					log.Add ("\t--> AnimationClip : \"" + animationClipFolder.name + "\"");
					//Get the state
					state = states.Find (delegate(AnimatorState st) {
						if (st.motion == null)
							return false;
						else
							return st.motion.name == animationClipFolder.name;
					});
					
					if (state == null) {
						if (acs.umAddNewClip) {
							controller.AddMotion (animationClipFolder);
							log.Add ("\t\t- Clip not Found. Creating new one named : \"" + animationClipFolder.name + "\"\r\n");
						} else {
							log.Add ("\t\t- Warning! Couldn't find clip named : \"" + animationClipFolder.name + "\"\r\n");
						}
					} else {
						log.Add ("\t\t- Clip found");
						animationClipController = ArrayUtility.Find (animationClipsController, delegate(AnimationClip ac) {
							return ac.name == state.motion.name; // Getting the current clip
						});
						
						if (keyFrameTypeSelected && acs.umCopyCurve) {
							CopyCurves (animationClipController, animationClipFolder);
						}
						
						if (acs.umCopyEvent) {
							CopyEvents (animationClipController, animationClipFolder);
						}
						
						//Change the motion
						state.motion = animationClipFolder;	
						log.Add ("\t\t- New clip copied\r\n");
					}
				}
			}
			if (acs.loCreateLog)
				CreateLogFile ();
			
			Debug.Log ("Animation Clip Switcher is done!");
		}
		
		/**
		******************************************************
		Copy events from one clip to another.
		******************************************************
		*/
		void CopyEvents (AnimationClip currentClip, AnimationClip newClip)
		{
			if (currentClip.events.Length == 0) { // If we don't have any events
				return;
			}
			
			AnimationEvent[] currentEvents = (AnimationEvent[])currentClip.events.Clone ();
			if (currentClip.length != newClip.length && acs.umOTLEvent != OTL.None) { // The timeline changed and we want to make one
				if (acs.umOTLEvent == OTL.LastFrameOnly) {
					// Not sure if there are always be sorted by time. So let's make a research
					for (int i = 0; i < currentEvents.Length; i++) {
						if (currentEvents [i].time == currentClip.length) {
							currentEvents [i].time = newClip.length;
							log.Add ("\t\t\t- Last frame event found");
						}
					}
				} else {
					float ratio = newClip.length / currentClip.length; 
					log.Add ("\t\t\t- Event ratio changed");
					for (int i = 0; i < currentEvents.Length; i++) {
						currentEvents [i].time *= ratio;					
					}
				}
			}
			
			if (!acs.umReplaceIfExistEvent) {
				//We're gonna need to search newClip's events to see if we have any redundancy (Love this word too)
				AnimationEvent[] newEvents = newClip.events;
				for (int i = 0; i < currentEvents.Length; i++) {
					if (ArrayUtility.Find (newEvents, delegate(AnimationEvent ev) {
						return ev.time == currentEvents [i].time; // This mean that we already have an event at this timeline
					}) != null) {
						log.Add ("\t\t\t- New clip already contains a event at " + currentEvents [i].time + " seconds. Can't copy " + currentEvents [i].functionName);
						ArrayUtility.RemoveAt (ref currentEvents, i);
						i--;
					}
				}
				
				if (currentEvents.Length == 0) {// If we don't have any events. Sending an empty array would erase events on the new clip
					log.Add ("\t\t\t- No more events to copy after redundancy check");
					return;
				}
			}
			
			AnimationUtility.SetAnimationEvents (newClip, currentEvents); // Copy current events on new clip
			log.Add ("\t\t\t- Copying previous events in new clip");
		}
		
		/**
		******************************************************
		Copy curves from one clip to another.
		******************************************************
		*/
		void CopyCurves (AnimationClip currentClip, AnimationClip newClip)
		{
			int index, i;
			
			//5.3 Means EditorCurveBinding appearance and AnimationUtility.GetAllCurves obsolete. It changes a lot of thing regarding variables. I'll do two distinct processes here
			#if UNITY_5_3_OR_NEWER
			EditorCurveBinding[] newCurveBindings = AnimationUtility.GetCurveBindings (newClip);
			EditorCurveBinding[] currentCurveBindings = (EditorCurveBinding[])AnimationUtility.GetCurveBindings (currentClip).Clone ();
			EditorCurveBinding newCurveBinding = new EditorCurveBinding ();
			AnimationCurve currentCurve;
			Keyframe[] currentKeyFrames;
			
			//Foreach curve data in the clip
			foreach (EditorCurveBinding currentCurveBinding in currentCurveBindings) {
				//Let's find out if this information is checked by the user
				index = acs.umCurveTypes.FindIndex (delegate(System.Type t) {
					return t.Equals (currentCurveBinding.type);
				});
				
				//He wants us to copy this information
				if (index != -1 && acs.umCheckedCurveTypes [index]) {	
					currentCurve = AnimationUtility.GetEditorCurve (currentClip, currentCurveBinding);
					currentKeyFrames = currentCurve.keys;
					//Find out if there is a curve with this type and this property
					bool found = false;
					i = 0;
					
					while (!found && i < newCurveBindings.Length) {
						if (newCurveBindings [i].type.Equals (currentCurveBinding.type) && newCurveBindings [i].propertyName == currentCurveBinding.propertyName) {
							newCurveBinding = newCurveBindings [i]; // Got it
							found = true;
						} else {
							i++;
						}
					}
					
					//Moving the last frame key at the end of the new one
					if (currentClip.length != newClip.length && acs.umOTLCurve != OTL.None) {
						if (acs.umOTLCurve == OTL.LastFrameOnly) {
							for (int j = 0; j < currentKeyFrames.Length; j++) {
								if (currentKeyFrames [j].time == currentClip.length) {
									currentKeyFrames [j].time = newClip.length;
								}
							}
						} else {
							float ratio = newClip.length / currentClip.length; 
							log.Add ("\t\t\t- Curve ratio changed");
							for (int j = 0; j < currentKeyFrames.Length; j++) {
								currentKeyFrames [j].time *= ratio;
							}
						}
					}
					
					//So we didn't find it.
					if (!found) {
						newClip.SetCurve (currentCurveBinding.path, currentCurveBinding.type, currentCurveBinding.propertyName, new AnimationCurve (currentCurve.keys));
						log.Add ("\t\t\t- Copying curves in new clip :" + currentCurveBinding.type + "/" + currentCurveBinding.propertyName);
					} else {
						if (acs.umReplaceIfExistCurve) {
							newClip.SetCurve (newCurveBinding.path, newCurveBinding.type, newCurveBinding.propertyName, new AnimationCurve (currentCurve.keys));
							log.Add ("\t\t\t- Replacing previous curves in new clip :" + newCurveBinding.type + "/" + newCurveBinding.propertyName);
						} else {
							log.Add ("\t\t\t- New clip already contains a curve on " + currentCurveBinding.type + "/" + currentCurveBinding.propertyName + ". Can't copy this curve.");
						}
					}
				}
			}
			
			#endif
			
			#if (UNITY_5_0 || UNITY_5_1 || UNITY_5_2)
			AnimationClipCurveData[] newCurveDatas = AnimationUtility.GetAllCurves (newClip, true);
			AnimationClipCurveData[] currentCurveDatas = (AnimationClipCurveData[])AnimationUtility.GetAllCurves (currentClip, true).Clone ();
			AnimationClipCurveData newCurveData;
			Keyframe[] currentKeysFrame;
			
			//Foreach curve data in the clip
			foreach (AnimationClipCurveData currentCurveData in currentCurveDatas) {
				//Let's find out if this information is checked by the user
				index = acs.umCurveTypes.FindIndex (delegate(System.Type t) {
					return t.Equals (currentCurveData.type);
				});
				
				//He wants us to copy this information
				if (index != -1 && acs.umCheckedCurveTypes [index]) {
					currentKeysFrame = currentCurveData.curve.keys;
					//Find out if there is a curve with this type and this property
					newCurveData = null;
					i = 0;
					
					while (newCurveData == null && i < newCurveDatas.Length) {
						if (newCurveDatas [i].type.Equals (currentCurveData.type) && newCurveDatas [i].propertyName == currentCurveData.propertyName) {
							newCurveData = newCurveDatas [i]; // Got it
						} else {
							i++;
						}
					}
					
					//Moving the last frame key at the end of the new one
					if (currentClip.length != newClip.length && acs.umOTLCurve != OTL.None) {
						if (acs.umOTLCurve == OTL.LastFrameOnly) {
							for (int j = 0; j < currentKeysFrame.Length; j++) {
								if (currentKeysFrame [j].time == currentClip.length) {
									currentKeysFrame [j].time = newClip.length;
									log.Add ("\t\t\t- Last frame curve found");
								}
							}
						} else {
							float ratio = newClip.length / currentClip.length; 
							log.Add ("\t\t\t- Curve ratio changed");
							
							for (int j = 0; j < currentKeysFrame.Length; j++) {
								currentKeysFrame [j].time *= ratio;
							}
						}
					}
					//So we didn't find it.
					if (newCurveData == null) {
						newClip.SetCurve (currentCurveData.path, currentCurveData.type, currentCurveData.propertyName, new AnimationCurve (currentKeysFrame));
						log.Add ("\t\t\t- Copying previous curves in new clip :" + currentCurveData.type + "/" + currentCurveData.propertyName);
					} else {
						if (acs.umReplaceIfExistCurve) {
							newClip.SetCurve (currentCurveData.path, currentCurveData.type, currentCurveData.propertyName, new AnimationCurve (currentKeysFrame));
							log.Add ("\t\t\t- Copying previous curves in new clip :" + currentCurveData.type + "/" + currentCurveData.propertyName);
						} else {
							log.Add ("\t\t\t- New clip already contains a curve on " + currentCurveData.type + "/" + currentCurveData.propertyName + ". Can't copy this curve.");
						}
					}
				}
			}
			#endif
		}
		
		/**
		*******************************************************
 		Return all states of an animationController
		*******************************************************
		**/
		List<AnimatorState> GetStatesFromAnimatorController (AnimatorController animatorController)
		{
			AnimatorControllerLayer[] animatorControllerLayers = animatorController.layers;
			AnimatorStateMachine animatorStateMachine, subAnimatorStateMachine;
			List<AnimatorState> states = new List<AnimatorState> ();
			
			foreach (AnimatorControllerLayer animatorControllerLayer in animatorControllerLayers) {
				animatorStateMachine = animatorControllerLayer.stateMachine;
				
				//Prepare an array of states. It'll be easier for later processes
				foreach (ChildAnimatorState childAnimatorState in animatorStateMachine.states) {
					states.Add (childAnimatorState.state);
				}
				
				foreach (ChildAnimatorStateMachine childAnimatorStateMachine in animatorStateMachine.stateMachines) {
					subAnimatorStateMachine = childAnimatorStateMachine.stateMachine;
					foreach (ChildAnimatorState childAnimatorState in subAnimatorStateMachine.states) {
						states.Add (childAnimatorState.state);
					}
				}
				
			}
			return states;
		}
		
		/**
		 *************************************************************
		Fill the list umCheckedCurveClasses to permit KeyFrame copy
		 *************************************************************
		 **/
		void FillCurveTypes ()
		{
			//5.3 Means EditorCurveBinding appearance and AnimationUtility.GetAllCurves obsolete. It changes a lot of thing regarding variables. I'll do two distinct processes here
			#if UNITY_5_3_OR_NEWER
			EditorCurveBinding[] curveBindings;
			//Foreach clip in the animator controller
			foreach (AnimationClip animationClip in acs.umAnimatorController.animationClips) {
				//Get all curveBindings
				curveBindings = AnimationUtility.GetCurveBindings (animationClip);
				//Foreach curve
				foreach (EditorCurveBinding curveBinding in curveBindings) {
					// Transform shoudn't be needed in that kind of process since it's what's change in the other software.
					if (!curveBinding.type.Equals (typeof(Transform))) {
						if (!acs.umCurveTypes.Exists (delegate (System.Type t) {
							return t.Equals (curveBinding.type); //Finding if this type is already in the list
						})) {
							//If not add it
							acs.umCurveTypes.Add (curveBinding.type);
							acs.umCheckedCurveTypes.Add (true);
						}
					}
				}
			}
			#endif
			
			#if (UNITY_5_0 || UNITY_5_1 || UNITY_5_2)
			AnimationClipCurveData[] curveDatas;
			//Foreach clip in the animator controller
			foreach (AnimationClip animationClip in acs.umAnimatorController.animationClips) {
				//Get all curveDatas
				curveDatas = AnimationUtility.GetAllCurves (animationClip, false);
				//Foreach curve
				foreach (AnimationClipCurveData curveData in curveDatas) {
					// Transform shoudn't be needed in that kind of process since it's what's change in the other software.
					if (!curveData.type.Equals (typeof(Transform))) {
						
						if (!acs.umCurveTypes.Exists (delegate (System.Type t) {
							return t.Equals (curveData.type); //Finding if this type is already in the list
						})) {
							//If not add it
							acs.umCurveTypes.Add (curveData.type);
							acs.umCheckedCurveTypes.Add (true);
						}
					}
				}
			}
			#endif
		}
		
		
		/**
		 *****************************************
		Just return a shorter version of the type
		******************************************
		**/
		string GetTypeShorterName (string name)
		{
			int index = name.LastIndexOf (".");
			if (index == -1) {
				return name;
			} else {
				return name.Substring (index + 1);
			}
		}
		
		/**
		 * Create the log file using log variable
		 * */
		void CreateLogFile ()
		{
			string path = acs.loFilePath + acs.loFileName;
			
			try {
				//Delete if exists
				if (File.Exists (path)) {
					File.Delete (path);
				}
				
				//Create and close this stream
				FileStream fs = File.Create (path);
				fs.Close ();
				
			} catch (DirectoryNotFoundException dnfe) {
				Debug.LogError ("Couldn't find directory! Message Error : " + dnfe.Message);
				return;
			}
			
			//Cause this one's a better on :D
			StreamWriter textWriter = new StreamWriter (path);
			for (int i = 0; i < log.Count; i++) {
				textWriter.WriteLine (log [i]);
			}
			
			//Close the second stream
			textWriter.Close (); 
			
			// Refreshs assets
			AssetDatabase.Refresh ();
		}
		
		/**
		*******************************************************
 		Checking log information
		*******************************************************
		**/
		void CheckLogInformation ()
		{
			acs.loFilePath = CheckPath (acs.loFilePath);
			
			string temp;
			if (acs.loFileName.Length > 0) {
				temp = acs.loFileName;
				if (temp.IndexOf ("log") == -1) {
					acs.loFileName += ".log";
				}
			} else {
				acs.loFileName = "switcher.log";
				return;
			}
			
		}
		
		/**
		*******************************************************
 		Function checking if the an animator exists at path. If not return path else return path+Number
		*******************************************************
		**/
		string GetAnimatorControllerName (string controllerPath)
		{
			controllerPath = controllerPath.Replace (".controller", "");
			
			int index = 1;
			
			while (AssetDatabase.LoadAssetAtPath (controllerPath + index.ToString () + ".controller", typeof(AnimatorController)) != null) {
				index++;
			}
			
			controllerPath += index.ToString () + ".controller";
			return controllerPath;
		}
		
		/**
		*******************************************************
 		Checking a Path informations
		*******************************************************
		**/
		string CheckPath (string path)
		{
			if (path == null) {
				path = "";
			}
			
			if (path.IndexOf ("Assets") == -1) {
				path = "Assets/" + path;
			}
			
			if (path.Substring (path.Length - 1, 1) != "/") {
				path += "/";
			}
			return  path;
		}
		
		
		/**
		 * Get the path of the controller by testing existing ones. Used when duplicate is checked.
		 * */
		public AnimatorController DuplicateAnimatorController (AnimatorController controller)
		{			
			string controllerPath = GetAnimatorControllerName (AssetDatabase.GetAssetPath (controller));
			
			//Copy the existing one
			AssetDatabase.CopyAsset (AssetDatabase.GetAssetPath (controller), controllerPath);
			log.Add ("AnimatorController duplicated at :\"" + controllerPath + "\"");
			
			// Refreshs assets
			AssetDatabase.Refresh ();
			
			controller = AssetDatabase.LoadAssetAtPath (controllerPath, typeof(AnimatorController)) as AnimatorController;
			return controller;
		}
		
		/**
		 * Get All animation clips at path
		 * */
		public static AnimationClip[] GetAnimationClipsAtPath (string path)
		{			
			//Starting by being sure that the path contains Assets/.
			string applicationPath = path;
			if (path.IndexOf ("Assets/") != -1) {
				applicationPath = path.Replace ("Assets/", "");
			}
			
			//Then adding the system path in front of the local path
			applicationPath = Application.dataPath + "/" + applicationPath;
			
			//Get All file in the directory
			string[] fileEntries;
			try {
				fileEntries = Directory.GetFiles (applicationPath);
			} catch (DirectoryNotFoundException dnfe) {
				Debug.LogError ("Couldn't find directory! Error Message : " + dnfe.Message);
				return  new AnimationClip[0];
			}
			
			//Variables we're gonna need in the loop
			List<AnimationClip> animationClips = new List<AnimationClip> ();
			AnimationClip temp;
			string localPath;
			
			//Foreach fileEntries
			foreach (string fileEntry in fileEntries) {
				localPath = path + fileEntry.Substring (fileEntry.LastIndexOf ("/") + 1);
				
				temp = AssetDatabase.LoadAssetAtPath (localPath, typeof(AnimationClip)) as AnimationClip;
				
				//If we got something we're adding it to the list
				if (temp != null)
					animationClips.Add (temp);
			}
			
			return animationClips.ToArray ();
		}
	}
}