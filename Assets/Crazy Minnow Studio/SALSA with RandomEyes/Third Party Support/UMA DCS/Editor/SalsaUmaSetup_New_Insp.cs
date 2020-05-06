using UnityEngine;
using UnityEditor;
using UMA;
using UMA.PoseTools;
using UMA.CharacterSystem;
using CrazyMinnow.SALSA;

namespace CrazyMinnow.SALSA.UMA_DCS
{
    [CustomEditor(typeof(SalsaUmaSetup_New))]
    public class SalsaUmaSetup_New_Insp : Editor
    {
        private static SalsaUmaSetup_New instance;

        [MenuItem("GameObject/Crazy Minnow Studio/SALSA/Addons/UMA DCS/SalsaUmaSync 1-click setup (new DynamicCharacterAvatar)")]
        static void Setup()
        {
            OnEnable();
        }

        public static void OnEnable()
        {
            DesigntimePreSetup();

            DesigntimePostSetup();

            DestroyImmediate(instance);
        }

        /// <summary>
        /// Find or create the UMA_DCS library prefab      
        /// </summary>	
        private static void DesigntimePreSetup()
        {
            GameObject uma_dcs = GameObject.Find("UMA_DCS");
            if (!uma_dcs)
            {
                uma_dcs = PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/UMA/Getting Started/UMA_DCS.prefab")) as GameObject;
                uma_dcs.name = "UMA_DCS";
            }
        }

        /// <summary>
        /// Create an empty, add the DynamicCharacterAvatar, and add the SalsaUmaSetup_Existing script
        /// </summary>
        private static void DesigntimePostSetup()
        {
            GameObject uma = new GameObject("SALSA_UMA2_DCS");
            if (uma)
            {
                uma.AddComponent<DynamicCharacterAvatar>();
                uma.AddComponent<SalsaUmaSetup_Existing>();
            }
        }
    }
}