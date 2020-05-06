using UnityEngine;
using UMA;
using UMA.PoseTools;
using UMA.CharacterSystem;
using CrazyMinnow.SALSA;

namespace CrazyMinnow.SALSA.UMA_DCS
{
    [AddComponentMenu("Crazy Minnow Studio/SALSA/Addons/UMA DCS/SalsaUmaSync 1-click setup (existing DynamicCharacterAvatar)")]
    public class SalsaUmaSetup_Existing : MonoBehaviour
    {
        /// <summary>
        /// Initializes Setup when setting up characters at runtime
        /// </summary>
        private void Awake()
        {
            Setup();
            Destroy(this);
        }

        /// <summary>
        /// Add and configure Salsa3D, RandomEyes3D, and SalsaUmaSync on an existing UMA DynamicCharacterAvatar
        /// </summary>
        public void Setup()
        {
            GameObject uma = this.gameObject;
            if (uma)
            {
                DynamicCharacterAvatar dca = uma.GetComponent<DynamicCharacterAvatar>();
                if (dca)
                {
                    if (dca.activeRace.name == null)
                        dca.activeRace.name = "HumanMaleDCS";
                    //dca.raceAnimationControllers.dynamicallyAddFromAssetBundles = true;
                    //dca.loadPathType = DynamicCharacterAvatar.loadPathTypes.FileSystem;
                    //dca.savePath = "CharacterRecipes";

                    Salsa3D salsa = uma.AddComponent<Salsa3D>();
                    RandomEyes3D re = uma.AddComponent<RandomEyes3D>();
                    SalsaUmaSync umaSync = uma.AddComponent<SalsaUmaSync>();
                    if (umaSync)
                    {
                        if (salsa)
                        {
                            salsa.blendSpeed = 12f;
                            umaSync.salsa3D = salsa;
                            AudioSource audioSource = uma.GetComponent<AudioSource>();
                            if (audioSource)
                            {
                                salsa.audioSrc = audioSource;
                                salsa.audioSrc.playOnAwake = false;
                            }
                            salsa.rangeOfMotion = 50f;
                        }
                        if (re) umaSync.randomEyes3D = re;
                    }
                }
            }
        }
    }
}