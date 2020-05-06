using UnityEngine;
using UMA;
using UMA.PoseTools;
using UMA.CharacterSystem;
using CrazyMinnow.SALSA;

namespace CrazyMinnow.SALSA.UMA_DCS
{    
    public class SalsaUmaSetup_New : MonoBehaviour
    {
        /// <summary>
        /// Initializes Setup when setting up characters at runtime
        /// </summary>
        private void Awake()
        {
            Setup();
            Destroy(this);
        }

        public void Setup()
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
