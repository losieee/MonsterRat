using UnityEngine;
using System.Collections;

namespace SlimUI.ModernMenu{
	public class CheckMusicVolume : MonoBehaviour {

        public bool useMusic = true;        // True = Music, False = Effect
        void Start()
        {
            ApplyVolume();
        }

        public void UpdateVolume()
        {
            ApplyVolume();
        }

        private void ApplyVolume()
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null) return;

            if (useMusic)
            {
                audioSource.volume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
            }
            else
            {
                audioSource.volume = PlayerPrefs.GetFloat("EffectVolume", 1f);
            }
        }
    }
}