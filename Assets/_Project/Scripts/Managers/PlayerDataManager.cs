using System;
using System.Collections.Generic;
using ChromaLogic.Core;
using UnityEngine;

namespace ChromaLogic.Managers
{
    /// <summary>
    /// Persistent singleton that owns all durable Curator profile state for a
    /// Chroma-Logic Gallery session.
    /// <para>
    /// Survives scene loads via <c>DontDestroyOnLoad</c>. State is persisted to
    /// <c>PlayerPrefs</c> after every mutation. UGS Cloud Save will replace
    /// <c>PlayerPrefs</c> in Phase 8 — all persistence is isolated to
    /// <see cref="Save"/> and <see cref="Load"/>.
    /// </para>
    /// </summary>
    public sealed class PlayerDataManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────

        /// <summary>The single active instance. <c>null</c> before the scene containing
        /// this component has loaded.</summary>
        public static PlayerDataManager Instance { get; private set; }

        // ── Rank promotion thresholds (Logic Points) ──────────────────────

        private const int RankThresholdScholar       =   1_000;
        private const int RankThresholdArchitect     =   5_000;
        private const int RankThresholdMaster        =  15_000;
        private const int RankThresholdGrandmaster   =  40_000;
        private const int RankThresholdArchCurator   = 100_000;

        // ── PlayerPrefs keys (prefixed CL_ to avoid collisions) ───────────

        private const string KeyRank              = "CL_Rank";
        private const string KeyLogicPoints       = "CL_LogicPoints";
        private const string KeyGemsCollected     = "CL_GemsCollected";
        private const string KeyRevelationSeconds = "CL_RevelationSeconds";
        private const string KeyGalleryXP         = "CL_GalleryXP";
        private const string KeyLastPlayedDate    = "CL_LastPlayedDate";
        private const string KeyCurrentStreak     = "CL_CurrentStreak";
        private const string KeyCompletedVessels    = "CL_CompletedVessels";
        private const string KeyUnlockedPacks       = "CL_UnlockedPacks";
        private const string KeyMeditationShapes    = "CL_MeditationShapes";
        private const string KeyMeditationColours   = "CL_MeditationColours";

        // Delimiter for serialising List<string> into a single PlayerPrefs entry.
        private const char ListDelimiter = '|';

        // ISO date format used to serialise DateTime to PlayerPrefs.
        private const string DateFormat = "yyyy-MM-dd";

        // ── Public Curator state ──────────────────────────────────────────

        /// <summary>Current Curator rank, derived from <see cref="LogicPoints"/>.</summary>
        public CuratorRank Rank { get; private set; }

        /// <summary>Cumulative Logic Points earned across all sessions.</summary>
        public int LogicPoints { get; private set; }

        /// <summary>Total gemstones collected (one per unique completed Vessel).</summary>
        public int GemsCollected { get; private set; }

        /// <summary>
        /// Total time spent solving Vessels, in seconds.
        /// Display externally as Revelation (convert to hours where appropriate) —
        /// never expose the word "hours played" in UI strings.
        /// </summary>
        public float RevelationSeconds { get; private set; }

        /// <summary>Cumulative Gallery XP earned across all sessions.</summary>
        public int GalleryXP { get; private set; }

        /// <summary>
        /// Date of the most recent session start. Stored as <c>DateTime.MinValue</c>
        /// if no session has ever been recorded.
        /// </summary>
        public DateTime LastPlayedDate { get; private set; }

        /// <summary>
        /// Number of consecutive days the Curator has opened The Gallery.
        /// Reset to 1 when a day is skipped.
        /// </summary>
        public int CurrentStreak { get; private set; }

        /// <summary>Unique identifiers of every Vessel the Curator has completed.</summary>
        public List<string> CompletedVesselIds { get; private set; }

        /// <summary>Identifiers of content packs the Curator has unlocked.</summary>
        public List<string> UnlockedPackIds { get; private set; }

        /// <summary>
        /// The four <see cref="ShapeType"/> values used in the Daily Meditation 4×4 grid
        /// (Phase 9). Defaults to Infinity, Diamond, Star, Heart. Configurable via
        /// <see cref="SetMeditationVessels"/> in Aesthetic Calibration (Phase 10).
        /// Must always contain exactly 4 elements.
        /// </summary>
        public List<ShapeType> MeditationShapes { get; private set; }

        /// <summary>
        /// The four <see cref="ColourType"/> values used in the Daily Meditation 4×4 grid
        /// (Phase 9). Defaults to Gold, Cobalt, Crimson, Jade. Configurable via
        /// <see cref="SetMeditationVessels"/> in Aesthetic Calibration (Phase 10).
        /// Must always contain exactly 4 elements.
        /// </summary>
        public List<ColourType> MeditationColours { get; private set; }

        // ── C# Events ────────────────────────────────────────────────────

        /// <summary>
        /// Fires whenever the Curator's rank increases.
        /// The new <see cref="CuratorRank"/> is passed as the argument.
        /// Subscribe to trigger rank-up presentation in the UI.
        /// </summary>
        public event Action<CuratorRank> OnRankPromoted;

        /// <summary>
        /// Fires whenever <see cref="CurrentStreak"/> changes (either incremented or
        /// reset). The updated streak value is passed as the argument.
        /// </summary>
        public event Action<int> OnStreakUpdated;

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
            Load();
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Adds <paramref name="amount"/> Logic Points to the Curator's total and
        /// checks whether a rank promotion has been earned.
        /// </summary>
        /// <param name="amount">Points to add. Values ≤ 0 are ignored.</param>
        public void AddLogicPoints(int amount)
        {
            if (amount <= 0) return;
            LogicPoints += amount;
            CheckRankPromotion();
            Save();
        }

        /// <summary>
        /// Increments the Curator's gem count by <paramref name="count"/>.
        /// Normally called indirectly via <see cref="CompleteVessel"/>.
        /// </summary>
        /// <param name="count">Gems to add. Values ≤ 0 are ignored.</param>
        public void AddGemsCollected(int count)
        {
            if (count <= 0) return;
            GemsCollected += count;
            Save();
        }

        /// <summary>
        /// Accumulates time spent in The Gallery.
        /// Pass raw elapsed seconds from the session; conversion to Revelation
        /// display units happens in the UI layer.
        /// </summary>
        /// <param name="seconds">Seconds to add. Values ≤ 0 are ignored.</param>
        public void AddRevelationTime(float seconds)
        {
            if (seconds <= 0f) return;
            RevelationSeconds += seconds;
            Save();
        }

        /// <summary>
        /// Adds <paramref name="amount"/> Gallery XP to the Curator's total.
        /// </summary>
        /// <param name="amount">XP to add. Values ≤ 0 are ignored.</param>
        public void AddGalleryXP(int amount)
        {
            if (amount <= 0) return;
            GalleryXP += amount;
            Save();
        }

        /// <summary>
        /// Records a Vessel completion. Idempotent — completing the same Vessel more
        /// than once does not double-count gems or list entries.
        /// Awards one gem on the first completion of each unique Vessel.
        /// </summary>
        /// <param name="vesselId">
        /// Unique identifier for the completed Vessel. Must not be null or empty.
        /// </param>
        public void CompleteVessel(string vesselId)
        {
            if (string.IsNullOrEmpty(vesselId)) return;
            if (CompletedVesselIds.Contains(vesselId)) return;

            CompletedVesselIds.Add(vesselId);
            AddGemsCollected(1);
            Save();
        }

        /// <summary>
        /// Records that a content pack has been unlocked. Idempotent.
        /// </summary>
        /// <param name="packId">
        /// Unique identifier for the unlocked pack. Must not be null or empty.
        /// </param>
        public void UnlockPack(string packId)
        {
            if (string.IsNullOrEmpty(packId)) return;
            if (UnlockedPackIds.Contains(packId)) return;

            UnlockedPackIds.Add(packId);
            Save();
        }

        /// <summary>
        /// Sets the four shapes and four colours used in the Daily Meditation 4×4 grid.
        /// Both lists must contain exactly 4 elements. Called from Aesthetic Calibration
        /// (Phase 10) when the Curator customises their Meditation Vessel configuration.
        /// </summary>
        /// <param name="shapes">
        /// Exactly 4 <see cref="ShapeType"/> values. Must not be null.
        /// </param>
        /// <param name="colours">
        /// Exactly 4 <see cref="ColourType"/> values. Must not be null.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when either list is null or does not contain exactly 4 items.
        /// </exception>
        public void SetMeditationVessels(List<ShapeType> shapes, List<ColourType> colours)
        {
            if (shapes == null || shapes.Count != 4)
                throw new ArgumentException("shapes must contain exactly 4 ShapeType values.", nameof(shapes));
            if (colours == null || colours.Count != 4)
                throw new ArgumentException("colours must contain exactly 4 ColourType values.", nameof(colours));

            MeditationShapes  = new List<ShapeType>(shapes);
            MeditationColours = new List<ColourType>(colours);
            Save();
        }

        /// <summary>
        /// Updates the daily streak. Call once at the start of each game session.
        /// <list type="bullet">
        ///   <item><description>
        ///     First ever session → streak set to 1, <see cref="OnStreakUpdated"/> fires.
        ///   </description></item>
        ///   <item><description>
        ///     Last session was yesterday → streak incremented, event fires.
        ///   </description></item>
        ///   <item><description>
        ///     Last session was today → no change, no event (already counted).
        ///   </description></item>
        ///   <item><description>
        ///     Last session was two or more days ago → streak reset to 1, event fires.
        ///   </description></item>
        /// </list>
        /// </summary>
        public void UpdateStreak()
        {
            DateTime today = DateTime.Today;

            if (LastPlayedDate == DateTime.MinValue)
            {
                // First ever session.
                CurrentStreak = 1;
                OnStreakUpdated?.Invoke(CurrentStreak);
            }
            else
            {
                int daysDelta = (today - LastPlayedDate.Date).Days;

                if (daysDelta == 0)
                {
                    // Already called today — nothing to do.
                    return;
                }
                else if (daysDelta == 1)
                {
                    CurrentStreak++;
                    OnStreakUpdated?.Invoke(CurrentStreak);
                }
                else
                {
                    // Gap of two or more days — streak broken.
                    CurrentStreak = 1;
                    OnStreakUpdated?.Invoke(CurrentStreak);
                }
            }

            LastPlayedDate = today;
            Save();
        }

        // ── Private helpers ───────────────────────────────────────────────

        // Determines the highest rank earned for the current LogicPoints total,
        // then promotes if that rank is higher than the current one.
        private void CheckRankPromotion()
        {
            CuratorRank earned = RankForPoints(LogicPoints);
            if (earned > Rank)
            {
                Rank = earned;
                OnRankPromoted?.Invoke(Rank);
            }
        }

        // Returns the highest CuratorRank the Curator has earned given their
        // current Logic Points total. Evaluated from highest threshold downward
        // so the correct tier is always returned in a single pass.
        private static CuratorRank RankForPoints(int points)
        {
            if (points >= RankThresholdArchCurator)   return CuratorRank.ArchCuratorOfTheVoid;
            if (points >= RankThresholdGrandmaster)   return CuratorRank.Grandmaster;
            if (points >= RankThresholdMaster)        return CuratorRank.Master;
            if (points >= RankThresholdArchitect)     return CuratorRank.Architect;
            if (points >= RankThresholdScholar)       return CuratorRank.Scholar;
            return CuratorRank.Neophyte;
        }

        // Writes every field to PlayerPrefs and flushes to disk.
        // All persistence lives here — swap to UGS Cloud Save in Phase 8
        // by replacing only this method and Load().
        private void Save()
        {
            PlayerPrefs.SetInt(KeyRank,              (int)Rank);
            PlayerPrefs.SetInt(KeyLogicPoints,       LogicPoints);
            PlayerPrefs.SetInt(KeyGemsCollected,     GemsCollected);
            PlayerPrefs.SetFloat(KeyRevelationSeconds, RevelationSeconds);
            PlayerPrefs.SetInt(KeyGalleryXP,         GalleryXP);
            PlayerPrefs.SetInt(KeyCurrentStreak,     CurrentStreak);

            PlayerPrefs.SetString(KeyLastPlayedDate,
                LastPlayedDate == DateTime.MinValue
                    ? string.Empty
                    : LastPlayedDate.ToString(DateFormat));

            PlayerPrefs.SetString(KeyCompletedVessels,
                string.Join(ListDelimiter, CompletedVesselIds));

            PlayerPrefs.SetString(KeyUnlockedPacks,
                string.Join(ListDelimiter, UnlockedPackIds));

            PlayerPrefs.SetString(KeyMeditationShapes,
                string.Join(ListDelimiter, MeditationShapes));

            PlayerPrefs.SetString(KeyMeditationColours,
                string.Join(ListDelimiter, MeditationColours));

            PlayerPrefs.Save();
        }

        // Reads all fields from PlayerPrefs, falling back to safe defaults for
        // any key that has not been written yet (first launch, or cleared data).
        private void Load()
        {
            Rank              = (CuratorRank)PlayerPrefs.GetInt(KeyRank, (int)CuratorRank.Neophyte);
            LogicPoints       = PlayerPrefs.GetInt(KeyLogicPoints,       0);
            GemsCollected     = PlayerPrefs.GetInt(KeyGemsCollected,     0);
            RevelationSeconds = PlayerPrefs.GetFloat(KeyRevelationSeconds, 0f);
            GalleryXP         = PlayerPrefs.GetInt(KeyGalleryXP,         0);
            CurrentStreak     = PlayerPrefs.GetInt(KeyCurrentStreak,     0);

            string dateStr = PlayerPrefs.GetString(KeyLastPlayedDate, string.Empty);
            LastPlayedDate = string.IsNullOrEmpty(dateStr)
                ? DateTime.MinValue
                : DateTime.ParseExact(dateStr, DateFormat,
                    System.Globalization.CultureInfo.InvariantCulture);

            CompletedVesselIds = ParseList(PlayerPrefs.GetString(KeyCompletedVessels, string.Empty));
            UnlockedPackIds    = ParseList(PlayerPrefs.GetString(KeyUnlockedPacks,    string.Empty));

            MeditationShapes  = ParseEnumList(
                PlayerPrefs.GetString(KeyMeditationShapes,  string.Empty),
                DefaultMeditationShapes());

            MeditationColours = ParseEnumList(
                PlayerPrefs.GetString(KeyMeditationColours, string.Empty),
                DefaultMeditationColours());
        }

        // Splits a pipe-delimited string into a List<string>, discarding empty entries.
        private static List<string> ParseList(string raw)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(raw)) return list;

            foreach (string entry in raw.Split(ListDelimiter))
                if (!string.IsNullOrEmpty(entry))
                    list.Add(entry);

            return list;
        }

        // Parses a pipe-delimited string of enum names into a typed list.
        // Returns fallback when the raw string is absent, empty, or does not yield
        // exactly 4 valid values — ensuring the Meditation grid is always fully configured.
        private static List<T> ParseEnumList<T>(string raw, List<T> fallback) where T : struct, Enum
        {
            if (string.IsNullOrEmpty(raw)) return fallback;

            var list = new List<T>(4);
            foreach (string entry in raw.Split(ListDelimiter))
                if (!string.IsNullOrEmpty(entry) && Enum.TryParse(entry, out T value))
                    list.Add(value);

            return list.Count == 4 ? list : fallback;
        }

        private static List<ShapeType> DefaultMeditationShapes() => new List<ShapeType>
        {
            ShapeType.Infinity, ShapeType.Diamond, ShapeType.Star, ShapeType.Heart
        };

        private static List<ColourType> DefaultMeditationColours() => new List<ColourType>
        {
            ColourType.Gold, ColourType.Cobalt, ColourType.Crimson, ColourType.Jade
        };
    }
}
