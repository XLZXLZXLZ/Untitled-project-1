using System;
using System.Collections.Generic;
using UnityEngine;

namespace TiltMaze
{
	[DefaultExecutionOrder(-1000)]
	public sealed class AudioManager : MonoBehaviour
	{
		[Serializable]
		private sealed class SoundEntry
		{
			public string key;

			public AudioClip[] clips = Array.Empty<AudioClip>();

			[Range(0f, 1f)]
			public float volume = 1f;

			[Range(0f, 0.2f)]
			public float pitchVariation;

			[Min(0f)]
			public float minimumInterval;
		}

		private const int SfxChannelCount = 6;

		private static AudioManager instance;

		[Header("Optional Inspector Overrides")]
		[Tooltip("Entries here override matching keys from Resources/Audio.")]
		[SerializeField]
		private SoundEntry[] sounds = Array.Empty<SoundEntry>();

		[Header("Mix")]
		[SerializeField]
		[Range(0f, 1f)]
		private float masterVolume = 1f;

		[SerializeField]
		[Range(0f, 1f)]
		private float musicVolume = 1f;

		[SerializeField]
		[Range(0f, 1f)]
		private float sfxVolume = 1f;

		private readonly Dictionary<string, SoundEntry> library = new Dictionary<string, SoundEntry>(StringComparer.OrdinalIgnoreCase);

		private readonly Dictionary<string, float> lastPlayTimes = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

		private readonly HashSet<string> reportedMissingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		private AudioSource musicSource;

		private AudioSource[] sfxSources;

		private int nextSfxChannel;

		public static AudioManager Instance
		{
			get
			{
				if (instance != null)
				{
					return instance;
				}
				instance = UnityEngine.Object.FindObjectOfType<AudioManager>();
				if (instance != null)
				{
					return instance;
				}
				instance = new GameObject("Audio Manager").AddComponent<AudioManager>();
				return instance;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			instance = null;
		}

		private void Awake()
		{
			if (instance != null && instance != this)
			{
				UnityEngine.Object.Destroy(base.gameObject);
				return;
			}
			instance = this;
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			EnsureSources();
			BuildLibrary();
		}

		public void PlaySfx(string key, float volumeScale = 1f)
		{
			if (!TryGetSound(key, out var sound) || sound.clips.Length == 0)
			{
				return;
			}
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (!lastPlayTimes.TryGetValue(key, out var value) || !(realtimeSinceStartup - value < sound.minimumInterval))
			{
				lastPlayTimes[key] = realtimeSinceStartup;
				AudioClip audioClip = sound.clips[UnityEngine.Random.Range(0, sound.clips.Length)];
				if (!(audioClip == null))
				{
					AudioSource availableSfxSource = GetAvailableSfxSource();
					availableSfxSource.clip = audioClip;
					availableSfxSource.volume = Mathf.Clamp01(masterVolume * sfxVolume * sound.volume * volumeScale);
					availableSfxSource.pitch = 1f + UnityEngine.Random.Range(0f - sound.pitchVariation, sound.pitchVariation);
					availableSfxSource.Play();
				}
			}
		}

		public void PlayMusic(string key, bool restart = false)
		{
			if (TryGetSound(key, out var sound) && sound.clips.Length != 0)
			{
				AudioClip audioClip = sound.clips[0];
				if (!(audioClip == null) && (restart || !musicSource.isPlaying || !(musicSource.clip == audioClip)))
				{
					musicSource.clip = audioClip;
					musicSource.loop = true;
					musicSource.pitch = 1f;
					musicSource.volume = Mathf.Clamp01(masterVolume * musicVolume * sound.volume);
					musicSource.Play();
				}
			}
		}

		public void StopMusic()
		{
			musicSource.Stop();
			musicSource.clip = null;
		}

		private void BuildLibrary()
		{
			library.Clear();
			AudioClip[] allClips = Resources.LoadAll<AudioClip>("Audio");
			RegisterResource("bgm", allClips, "BGM", 1f, 0f);
			RegisterResource("wall", allClips, "COLLISION", 0.45f, 0.085f);
			RegisterResource("pickup", allClips, "PICKUP", 0.6f, 0.06f);
			RegisterResource("flip", allClips, "SWEEP", 0.6f, 0.12f);
			SoundEntry[] array = sounds;
			foreach (SoundEntry soundEntry in array)
			{
				if (soundEntry != null && !string.IsNullOrWhiteSpace(soundEntry.key) && soundEntry.clips != null && soundEntry.clips.Length != 0)
				{
					library[soundEntry.key] = soundEntry;
				}
			}
		}

		private void RegisterResource(string key, IEnumerable<AudioClip> allClips, string resourceName, float volume, float minimumInterval)
		{
			AudioClip audioClip = null;
			foreach (AudioClip allClip in allClips)
			{
				if (!(allClip == null) && allClip.name.Equals(resourceName, StringComparison.OrdinalIgnoreCase))
				{
					audioClip = allClip;
					break;
				}
			}
			if (!(audioClip == null))
			{
				library[key] = new SoundEntry
				{
					key = key,
					clips = new AudioClip[1] { audioClip },
					volume = volume,
					pitchVariation = 0f,
					minimumInterval = minimumInterval
				};
			}
		}

		private bool TryGetSound(string key, out SoundEntry sound)
		{
			if (!string.IsNullOrWhiteSpace(key) && library.TryGetValue(key, out sound))
			{
				return true;
			}
			sound = null;
			string text = (string.IsNullOrWhiteSpace(key) ? "<empty>" : key);
			if (reportedMissingKeys.Add(text))
			{
				Debug.LogWarning("AudioManager has no sound registered for key '" + text + "'.", this);
			}
			return false;
		}

		private void EnsureSources()
		{
			musicSource = base.gameObject.AddComponent<AudioSource>();
			musicSource.playOnAwake = false;
			musicSource.spatialBlend = 0f;
			sfxSources = new AudioSource[6];
			for (int i = 0; i < sfxSources.Length; i++)
			{
				AudioSource audioSource = base.gameObject.AddComponent<AudioSource>();
				audioSource.playOnAwake = false;
				audioSource.spatialBlend = 0f;
				sfxSources[i] = audioSource;
			}
		}

		private AudioSource GetAvailableSfxSource()
		{
			for (int i = 0; i < sfxSources.Length; i++)
			{
				int num = (nextSfxChannel + i) % sfxSources.Length;
				if (!sfxSources[num].isPlaying)
				{
					nextSfxChannel = (num + 1) % sfxSources.Length;
					return sfxSources[num];
				}
			}
			AudioSource result = sfxSources[nextSfxChannel];
			nextSfxChannel = (nextSfxChannel + 1) % sfxSources.Length;
			return result;
		}
	}
}
