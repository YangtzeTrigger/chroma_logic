using UnityEngine;

namespace ChromaLogic.Managers
{
    /// <summary>
    /// Persistent singleton that owns all audio and haptic output for a
    /// Chroma-Logic Gallery session.
    /// <para>
    /// Survives scene loads via <c>DontDestroyOnLoad</c>. Place this component on the
    /// same persistent Bootstrap GameObject as <see cref="PlayerDataManager"/> and
    /// <see cref="ProgressionManager"/>.
    /// </para>
    /// <para>
    /// Three <c>AudioSource</c> components are created programmatically in
    /// <c>Awake</c> — do not add them manually in the Inspector. Assign all
    /// <see cref="AudioClip"/> references in the Inspector using the public fields below.
    /// </para>
    /// <para>
    /// Volume channels are persisted to <c>PlayerPrefs</c> after every change. Keys use
    /// the <c>CL_</c> prefix to avoid collisions with other packages.
    /// </para>
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────

        /// <summary>The single active instance. <c>null</c> before the Bootstrap
        /// scene has loaded.</summary>
        public static AudioManager Instance { get; private set; }

        // ── Semitone constant ─────────────────────────────────────────────

        // 2^(1/12) — the frequency ratio of one equal-temperament semitone.
        private const float SemitoneRatio = 1.05946309f;

        // ── PlayerPrefs keys ───────────────────────────────────────────────

        private const string KeyMusicVolume     = "CL_MusicVolume";
        private const string KeyTileSFXVolume   = "CL_TileSFXVolume";
        private const string KeyUISFXVolume     = "CL_UISFXVolume";
        private const string KeyHapticIntensity = "CL_HapticIntensity";

        // ── Inspector — Tile SFX ──────────────────────────────────────────

        /// <summary>First tile placement variant — glass clink.</summary>
        [Header("Tile SFX")]
        public AudioClip TilePlacementClip1;

        /// <summary>Second tile placement variant — soft stone tap.</summary>
        public AudioClip TilePlacementClip2;

        /// <summary>Third tile placement variant — crystal tap.</summary>
        public AudioClip TilePlacementClip3;

        // ── Inspector — Completion SFX ────────────────────────────────────

        /// <summary>
        /// Ascending pentatonic chime played on row completion. Pitch is raised by
        /// one semitone per consecutive completion count via
        /// <see cref="PlayRowComplete"/>.
        /// </summary>
        [Header("Completion SFX")]
        public AudioClip RowCompleteClip;

        /// <summary>Resonant tonal chime played when a 3×3 box is completed.</summary>
        public AudioClip BoxCompleteClip;

        /// <summary>
        /// Cinematic reveal swell. Reserved exclusively for Phase B full-grid reveal
        /// via <see cref="PlayRevealSwell"/>. Do not reuse for other events.
        /// </summary>
        public AudioClip RevealSwellClip;

        // ── Inspector — UI SFX ────────────────────────────────────────────

        /// <summary>Generic UI interaction sound for button presses and navigation.</summary>
        [Header("UI SFX")]
        public AudioClip UIClickClip;

        // ── Volume channels ───────────────────────────────────────────────

        private float _musicVolume;
        private float _tileSFXVolume;
        private float _uiSFXVolume;
        private float _hapticIntensity;

        /// <summary>
        /// Background music volume in the range [0, 1]. Persisted across sessions.
        /// Setting this value immediately updates the music <c>AudioSource</c>.
        /// </summary>
        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Mathf.Clamp01(value);
                if (_musicSource != null) _musicSource.volume = _musicVolume;
                PlayerPrefs.SetFloat(KeyMusicVolume, _musicVolume);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Tile placement and completion SFX volume in the range [0, 1].
        /// Persisted across sessions.
        /// </summary>
        public float TileSFXVolume
        {
            get => _tileSFXVolume;
            set
            {
                _tileSFXVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(KeyTileSFXVolume, _tileSFXVolume);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// UI interaction SFX volume in the range [0, 1]. Persisted across sessions.
        /// </summary>
        public float UISFXVolume
        {
            get => _uiSFXVolume;
            set
            {
                _uiSFXVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(KeyUISFXVolume, _uiSFXVolume);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Haptic feedback intensity in the range [0, 1]. A value of <c>0</c>
        /// disables all haptic output. Persisted across sessions.
        /// </summary>
        public float HapticIntensity
        {
            get => _hapticIntensity;
            set
            {
                _hapticIntensity = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(KeyHapticIntensity, _hapticIntensity);
                PlayerPrefs.Save();
            }
        }

        // ── Private audio state ───────────────────────────────────────────

        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private AudioSource _rowCompleteSfxSource;

        private AudioClip[] _tileClips;
        private int _lastTileClipIndex = -1;

        // ── Unity lifecycle ───────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadVolumes();
            InitialiseAudioSources();

            _tileClips = new[] { TilePlacementClip1, TilePlacementClip2, TilePlacementClip3 };
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Plays one of the three tile placement SFX clips, chosen at random with
        /// guaranteed non-repetition — the same clip is never played twice in a row.
        /// Does nothing if <see cref="TileSFXVolume"/> is zero or all clips are unassigned.
        /// </summary>
        public void PlayTilePlacement()
        {
            if (_tileSFXVolume <= 0f) return;

            int index;
            do { index = Random.Range(0, 3); } while (index == _lastTileClipIndex);
            _lastTileClipIndex = index;

            AudioClip clip = _tileClips[index];
            if (clip == null) return;

            _sfxSource.PlayOneShot(clip, _tileSFXVolume);
        }

        /// <summary>
        /// Plays the ascending pentatonic chime for a completed row.
        /// Pitch is raised by one equal-temperament semitone for each step in
        /// <paramref name="consecutiveCount"/>, rewarding streaks of consecutive
        /// completions within a single Vessel session.
        /// </summary>
        /// <param name="consecutiveCount">
        /// Number of rows completed consecutively without an intervening error.
        /// Pass <c>0</c> for the base tone. Each increment raises pitch by
        /// <c>2^(1/12)</c> — one semitone.
        /// </param>
        public void PlayRowComplete(int consecutiveCount)
        {
            if (_tileSFXVolume <= 0f || RowCompleteClip == null) return;

            _rowCompleteSfxSource.pitch = Mathf.Pow(SemitoneRatio, consecutiveCount);
            _rowCompleteSfxSource.PlayOneShot(RowCompleteClip, _tileSFXVolume);
        }

        /// <summary>
        /// Plays the resonant tonal chime for a completed 3×3 box.
        /// Does nothing if <see cref="TileSFXVolume"/> is zero or the clip is unassigned.
        /// </summary>
        public void PlayBoxComplete()
        {
            if (_tileSFXVolume <= 0f || BoxCompleteClip == null) return;
            _sfxSource.PlayOneShot(BoxCompleteClip, _tileSFXVolume);
        }

        /// <summary>
        /// Plays the cinematic reveal swell. Reserved exclusively for Phase B — the
        /// full-grid bloom that follows a completed Vessel. Do not call from any other
        /// context; its impact depends on being heard rarely.
        /// Does nothing if <see cref="TileSFXVolume"/> is zero or the clip is unassigned.
        /// </summary>
        public void PlayRevealSwell()
        {
            if (_tileSFXVolume <= 0f || RevealSwellClip == null) return;
            _sfxSource.PlayOneShot(RevealSwellClip, _tileSFXVolume);
        }

        /// <summary>
        /// Plays the generic UI interaction sound for button presses and navigation.
        /// Does nothing if <see cref="UISFXVolume"/> is zero or the clip is unassigned.
        /// </summary>
        public void PlayUIClick()
        {
            if (_uiSFXVolume <= 0f || UIClickClip == null) return;
            _sfxSource.PlayOneShot(UIClickClip, _uiSFXVolume);
        }

        /// <summary>
        /// Starts playing <paramref name="clip"/> as looping background music,
        /// replacing any currently playing track. Does nothing if <paramref name="clip"/>
        /// is <c>null</c> or <see cref="MusicVolume"/> is zero.
        /// </summary>
        /// <param name="clip">The music clip to play.</param>
        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || _musicVolume <= 0f) return;
            _musicSource.clip = clip;
            _musicSource.Play();
        }

        /// <summary>Stops any currently playing background music immediately.</summary>
        public void StopMusic()
        {
            _musicSource.Stop();
        }

        /// <summary>
        /// Triggers a short haptic pulse for tile placement feedback.
        /// <para>
        /// The design intent is a 12 ms pulse; however, <c>Handheld.Vibrate()</c>
        /// does not accept a duration parameter — actual pulse length is device-dependent.
        /// </para>
        /// Does nothing when <see cref="HapticIntensity"/> is zero, or on platforms
        /// that do not support <c>Handheld.Vibrate()</c>.
        /// </summary>
        public void TriggerPlacementHaptic()
        {
            if (_hapticIntensity <= 0f) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        // ── Private helpers ───────────────────────────────────────────────

        private void LoadVolumes()
        {
            _musicVolume     = PlayerPrefs.GetFloat(KeyMusicVolume,     1f);
            _tileSFXVolume   = PlayerPrefs.GetFloat(KeyTileSFXVolume,   1f);
            _uiSFXVolume     = PlayerPrefs.GetFloat(KeyUISFXVolume,     1f);
            _hapticIntensity = PlayerPrefs.GetFloat(KeyHapticIntensity, 1f);
        }

        private void InitialiseAudioSources()
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop         = true;
            _musicSource.playOnAwake  = false;
            _musicSource.volume       = _musicVolume;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake    = false;

            _rowCompleteSfxSource = gameObject.AddComponent<AudioSource>();
            _rowCompleteSfxSource.playOnAwake = false;
        }
    }
}
