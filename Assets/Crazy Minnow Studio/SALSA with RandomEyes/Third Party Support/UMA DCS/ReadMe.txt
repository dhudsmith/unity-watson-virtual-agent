**********************************************
UMA DCS add-on for SALSA
version 1.7.0
https://crazyminnowstudio.com/posts/uma-lipsync-using-salsa-with-randomeyes/
				
Copyright ©2017 Crazy Minnow Studio, LLC
http://crazyminnowstudio.com/projects/salsa-with-randomeyes-lipsync/
**********************************************

Package Contents
----------------
Crazy Minnow Studio/SALSA with RandomEyes/Third Party Support/
	UMA DCS
		Editor
			SalsaUmaSetup_Existing_Insp.cs
				Custom inspector for SalsaUmaSetup_Existing.
			SalsaUmaSetup_New_Insp.cs
				Custom inspector for SalsaUmaSetup_New_Insp.
			SalsaUmaSync_Insp.cs
				Custom inspector for SalsaUmaSync_Insp.
		Examples
			Scenes
				SalsaUmaSync 1-Click Setup
					An example scene that demonstrates a working SALSA enabled UMA DCS character.
			Scripts
				SalsaUmaSync_ExpressionsTester
					A simple tester class to demonstration the expression functions built into SalsaUmaSync.
		ReadMe.txt
			This readme file.
		SalsaUmaSetup_Existing.cs
			SALSA setup for an existing UMA DCS character.	
		SalsaUmaSetup_New.cs
			SALSA setup including a new UMA DCS character.	
		SalsaUmaSync.cs
			Helper script to apply Salsa and RandomEyes configuration settings and BlendShape data to the UMAExpressionPlayer.


Installation Instructions
-------------------------
1. Install SALSA with RandomEyes into your project.
	Select [Window] -> [Asset Store]
	Once the Asset Store window opens, select the download icon, and download and import [SALSA with RandomEyes].

2. Install UMA 2 into your project.
	Select [Window] -> [Asset Store]
	Once the Asset Store window opens, select the download icon, and download and import [UMA 2 - Unity Multipurpose Avatar].

3. Import the SALSA with RandomEyes UMA Character support package.
	Select [Assets] -> [Import Package] -> [Custom Package...]
	Browse to the [SALSA_3rdPartySupport_UMA_DCS_{version}.unitypackage] file and [Open].


Quick Start Instructions 
------------------------
	1. To setup a new UMA DCS character, and add all applicable SALSA components.
		[GameObject] -> [Crazy Minnow Studio] -> [UMA DCS] -> [SalsaUmaSync 1-click setup (new DynamicCharacterAvatar)]
		OR
		To add all the applicable SALSA components to an existing UMA DCS character
		[Component] -> [Crazy Minnow Studio] -> [UMA DCS] -> [SalsaUmaSync 1-click setup (existing DynamicCharacterAvatar)]

	2. Add an AudioClip to the Salsa3D [Audio Clip] field.

	3. Optionally link in an animation controller to the SalsaUmaSync [RuntimeAnimatorController] field.

	4. Optionally use the SalsaUmaSync custom expression function to create facial expressions.
		public void SetExpression(Expression expression, float blendSpeed, float rangeOfMotion, float duration)
		public void SetExpression(Expression expression, float blendSpeed, float percentage, bool active)

	5. Play the scene.