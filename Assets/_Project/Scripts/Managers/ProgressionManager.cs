using System;
using System.Collections.Generic;
using ChromaLogic.Core;
using UnityEngine;

namespace ChromaLogic.Managers
{
    /// <summary>
    /// Singleton that evaluates cross-cutting progression milestones and fires C# events
    /// when they are reached. Reads Curator state from <see cref="PlayerDataManager"/>
    /// and writes back through its public API — it never owns persistent data directly,
    /// except for earned designations (see notes on <see cref="CheckAllDesignations"/>).
    /// <para>
    /// Responsibilities: pack completion detection, Grand Masterpiece unlocks,
    /// designation (title/achievement) awards, and Gallery XP rewards for jigsaw play.
    /// </para>
    /// <para>
    /// This is a <c>MonoBehaviour</c> singleton that survives scene loads via
    /// <c>DontDestroyOnLoad</c>. Place it on the same persistent GameObject as
    /// <see cref="PlayerDataManager"/>.
    /// </para>
    /// </summary>
    public sealed class ProgressionManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────

        /// <summary>The single active instance. <c>null</c> before the persistent
        /// scene has loaded.</summary>
        public static ProgressionManager Instance { get; private set; }

        // ── Designation condition thresholds ──────────────────────────────

        /// <summary>Number of unique completed Vessels required for the
        /// Sovereign of Symmetry designation.</summary>
        private const int VesselsForSovereignOfSymmetry = 500;

        // ── Designation ID strings ────────────────────────────────────────

        /// <summary>Designation awarded when the Curator completes
        /// <see cref="VesselsForSovereignOfSymmetry"/> unique Vessels.</summary>
        public const string DesignationSovereignOfSymmetry = "sovereign_of_symmetry";

        /// <summary>Designation awarded when the Curator reaches
        /// <see cref="CuratorRank.Grandmaster"/> or higher.</summary>
        public const string DesignationCuratorAscended = "curator_ascended";

        // ── Jigsaw Gallery XP rewards ─────────────────────────────────────

        private const int JigsawXpMeditative  =  10;
        private const int JigsawXpFocused     =  25;
        private const int JigsawXpChallenging =  50;
        private const int JigsawXpGrandMaster = 100;

        // ── Persistence ───────────────────────────────────────────────────

        // Phase 8 — fold CL_EarnedDesignations into UGS Cloud Save alongside
        // the rest of PlayerDataManager state.
        private const string KeyEarnedDesignations = "CL_EarnedDesignations";
        private const char   ListDelimiter          = '|';

        // ── State ─────────────────────────────────────────────────────────

        // Earned designation IDs persisted here because PlayerDataManager has no
        // designation list. All other state is read live from PlayerDataManager.
        private List<string> _earnedDesignations;

        /// <summary>
        /// Read-only view of every designation ID the Curator has earned.
        /// Designation IDs are defined as <c>public const string</c> fields on this class.
        /// </summary>
        public IReadOnlyList<string> EarnedDesignations => _earnedDesignations;

        // ── C# Events ─────────────────────────────────────────────────────

        /// <summary>
        /// Fires the first time all Vessels in a pack are completed.
        /// The pack's identifier is passed as the argument.
        /// Subscribe to trigger pack-complete presentation and Grand Masterpiece reveal.
        /// </summary>
        public event Action<string> OnPackCompleted;

        /// <summary>
        /// Fires the first time a designation condition is satisfied.
        /// The designation's string identifier is passed as the argument.
        /// Each designation fires at most once per profile lifetime.
        /// </summary>
        public event Action<string> OnDesignationEarned;

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
            LoadDesignations();
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Checks whether all Vessels in a pack have been completed by the Curator.
        /// If the pack is newly completed, records it in
        /// <see cref="PlayerDataManager.UnlockedPackIds"/>, adds the Grand Masterpiece
        /// entry (<c>packId + "_grand"</c>), fires <see cref="OnPackCompleted"/>,
        /// and triggers <see cref="CheckAllDesignations"/>.
        /// Calling this method for an already-completed pack is a safe no-op.
        /// </summary>
        /// <param name="packId">
        /// Unique identifier for the content pack. Must not be null or empty.
        /// </param>
        /// <param name="packVesselIds">
        /// All Vessel IDs that belong to this pack. Must not be null.
        /// </param>
        /// <returns>
        /// <c>true</c> if every Vessel in the pack is complete (including packs
        /// completed in previous sessions); <c>false</c> if any Vessel is still
        /// outstanding.
        /// </returns>
        public bool CheckPackCompletion(string packId, List<string> packVesselIds)
        {
            if (string.IsNullOrEmpty(packId) || packVesselIds == null)
                return false;

            PlayerDataManager pdm = PlayerDataManager.Instance;

            // Idempotency: already unlocked in a prior session.
            if (pdm.UnlockedPackIds.Contains(packId))
                return true;

            // Verify every vessel in the pack has been completed.
            List<string> completedIds = pdm.CompletedVesselIds;
            foreach (string vesselId in packVesselIds)
                if (!completedIds.Contains(vesselId))
                    return false;

            // Newly completed — record pack and Grand Masterpiece unlock.
            pdm.UnlockPack(packId);
            pdm.UnlockPack(packId + "_grand");

            OnPackCompleted?.Invoke(packId);
            CheckAllDesignations();
            return true;
        }

        /// <summary>
        /// Awards Gallery XP appropriate to the completed jigsaw tier and calls
        /// <see cref="CheckAllDesignations"/> in case the XP triggers a designation.
        /// <list type="bullet">
        ///   <item><description>Meditative — 10 XP</description></item>
        ///   <item><description>Focused — 25 XP</description></item>
        ///   <item><description>Challenging — 50 XP</description></item>
        ///   <item><description>GrandMaster — 100 XP</description></item>
        /// </list>
        /// </summary>
        /// <param name="difficulty">The completed jigsaw's difficulty tier.</param>
        public void AwardJigsawXP(JigsawDifficulty difficulty)
        {
            int xp = difficulty switch
            {
                JigsawDifficulty.Meditative  => JigsawXpMeditative,
                JigsawDifficulty.Focused     => JigsawXpFocused,
                JigsawDifficulty.Challenging => JigsawXpChallenging,
                JigsawDifficulty.GrandMaster => JigsawXpGrandMaster,
                _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null)
            };

            PlayerDataManager.Instance.AddGalleryXP(xp);
            CheckAllDesignations();
        }

        /// <summary>
        /// Evaluates all known designation conditions against the current Curator profile
        /// and awards any that have been newly satisfied. Safe to call at any time —
        /// already-earned designations are silently skipped.
        /// <para>
        /// Call this after any significant player action: Vessel completion, rank change,
        /// pack completion, or jigsaw completion.
        /// </para>
        /// <para>
        /// Designations evaluated:
        /// <list type="bullet">
        ///   <item><description>
        ///     <c>sovereign_of_symmetry</c> — <see cref="VesselsForSovereignOfSymmetry"/>
        ///     unique Vessels completed.
        ///   </description></item>
        ///   <item><description>
        ///     <c>curator_ascended</c> — Curator has reached
        ///     <see cref="CuratorRank.Grandmaster"/> or higher.
        ///   </description></item>
        /// </list>
        /// </para>
        /// </summary>
        public void CheckAllDesignations()
        {
            PlayerDataManager pdm = PlayerDataManager.Instance;

            TryAwardDesignation(
                DesignationSovereignOfSymmetry,
                pdm.CompletedVesselIds.Count >= VesselsForSovereignOfSymmetry);

            TryAwardDesignation(
                DesignationCuratorAscended,
                pdm.Rank >= CuratorRank.Grandmaster);
        }

        // ── Private helpers ───────────────────────────────────────────────

        // Awards a designation if the condition is met and it has not been earned yet.
        // Persists the updated list and fires the event.
        private void TryAwardDesignation(string id, bool condition)
        {
            if (!condition) return;
            if (_earnedDesignations.Contains(id)) return;

            _earnedDesignations.Add(id);
            SaveDesignations();
            OnDesignationEarned?.Invoke(id);
        }

        // Loads the earned-designation list from PlayerPrefs.
        // Falls back to an empty list on first launch or cleared data.
        private void LoadDesignations()
        {
            string raw = PlayerPrefs.GetString(KeyEarnedDesignations, string.Empty);
            _earnedDesignations = new List<string>();

            if (string.IsNullOrEmpty(raw)) return;

            foreach (string entry in raw.Split(ListDelimiter))
                if (!string.IsNullOrEmpty(entry))
                    _earnedDesignations.Add(entry);
        }

        // Writes the earned-designation list to PlayerPrefs and flushes to disk.
        // Phase 8 — replace with UGS Cloud Save write alongside PlayerDataManager.
        private void SaveDesignations()
        {
            PlayerPrefs.SetString(
                KeyEarnedDesignations,
                string.Join(ListDelimiter, _earnedDesignations));
            PlayerPrefs.Save();
        }
    }
}
