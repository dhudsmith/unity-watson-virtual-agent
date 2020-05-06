using UnityEngine;
using UnityEditor;
using UMA;
using UMA.PoseTools;
using UMA.CharacterSystem;
using CrazyMinnow.SALSA;

namespace CrazyMinnow.SALSA.UMA_DCS
{
    [CustomEditor(typeof(SalsaUmaSetup_Existing))]
    public class SalsaUmaSetup_Exisiting_Insp : Editor
    {
        private SalsaUmaSetup_Existing instance;

        public void OnEnable()
        {
            instance = target as SalsaUmaSetup_Existing;

            DesigntimePreSetup();

            instance.Setup();

            DestroyImmediate(instance);
        }

        /// <summary>
        /// Find or create the UMA_DCS library prefab      
        /// </summary>	
        private void DesigntimePreSetup()
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
    }
}