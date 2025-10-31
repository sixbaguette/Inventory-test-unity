using UnityEngine;
using System.Collections.Generic;

public class InventoryAudioManager : MonoBehaviour
{
    public static InventoryAudioManager Instance;

    [System.Serializable]
    public class AudioEntry
    {
        public string key;
        public AudioClip clip;
    }

    [Header("Liste des sons disponibles")]
    public List<AudioEntry> audioEntries = new List<AudioEntry>();

    private Dictionary<string, AudioClip> soundMap = new Dictionary<string, AudioClip>();
    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // S'assure d'être root avant de rendre persistant
        if (transform.parent != null)
        {
            transform.SetParent(null); // détache du parent
        }
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D

        foreach (var entry in audioEntries)
        {
            if (!soundMap.ContainsKey(entry.key))
                soundMap.Add(entry.key, entry.clip);
        }
    }

    public void Play(string key)
    {
        if (soundMap.TryGetValue(key, out AudioClip clip) && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"[InventoryAudio] Aucun son trouvé pour '{key}'");
        }
    }
}
