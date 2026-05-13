using System;
using System.Collections.Generic;
using ChromaLogic.Core;
using ChromaLogic.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChromaLogic.UI
{
    /// <summary>
    /// Manages the Path of Prestige panel embedded within the Curator Profile scene.
    /// <para>
    /// This component does not own a <see cref="UIDocument"/>. It receives the full
    /// UXML root via <see cref="Initialize"/> called by the sibling
    /// <see cref="ProfileController"/> during <c>Awake</c>, then queries its own
    /// subset of named elements.
    /// </para>
    /// <para>
    /// Tier nodes are built programmatically in <see cref="PopulatePrestige"/> rather
    /// than declared in UXML, because their count and state are determined at runtime
    /// from <see cref="PlayerDataManager.Rank"/>.
    /// </para>
    /// </summary>
    public sealed class PrestigeController : MonoBehaviour
    {
        // ── LP threshold table ─────────────────────────────────────────────

        /// <summary>
        /// Minimum cumulative Logic Points required to reach each rank.
        /// Values mirror <see cref="PlayerDataManager"/>'s private constants.
        /// </summary>
        private static readonly Dictionary<CuratorRank, int> RankThresholds =
            new Dictionary<CuratorRank, int>
            {
                { CuratorRank.Neophyte,               0     },
                { CuratorRank.Scholar,             1_000    },
                { CuratorRank.Architect,           5_000    },
                { CuratorRank.Master,             15_000    },
                { CuratorRank.Grandmaster,        40_000    },
                { CuratorRank.ArchCuratorOfTheVoid, 100_000 },
            };

        // ── UXML element name constants ────────────────────────────────────

        private const string NamePrestigeRankTitle    = "prestige-rank-title";
        private const string NamePrestigeRankSubtitle = "prestige-rank-subtitle";
        private const string NameProgressBarFill      = "progress-bar-fill";
        private const string NameProgressLabel        = "progress-label";
        private const string NameTierList             = "tier-list";

        // ── UXML element references ────────────────────────────────────────

        private Label         _rankTitle;
        private Label         _rankSubtitle;
        private VisualElement _progressBarFill;
        private Label         _progressLabel;
        private VisualElement _tierList;

        // ── Tier state enum ────────────────────────────────────────────────

        private enum TierState { Completed, Current, Locked }

        // ── Initialisation ─────────────────────────────────────────────────

        /// <summary>
        /// Binds this controller to the UXML elements it manages.
        /// Must be called by <see cref="ProfileController"/> before
        /// <see cref="PopulatePrestige"/> is ever invoked.
        /// </summary>
        /// <param name="root">The full <c>rootVisualElement</c> of the shared
        /// <see cref="UIDocument"/>.</param>
        public void Initialize(VisualElement root)
        {
            _rankTitle       = root.Q<Label>(NamePrestigeRankTitle);
            _rankSubtitle    = root.Q<Label>(NamePrestigeRankSubtitle);
            _progressBarFill = root.Q<VisualElement>(NameProgressBarFill);
            _progressLabel   = root.Q<Label>(NameProgressLabel);
            _tierList        = root.Q<VisualElement>(NameTierList);
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Refreshes the prestige panel with current Curator progression data.
        /// Called by <see cref="ProfileController.ShowPrestige"/> each time the
        /// panel is revealed, so the data is always up to date.
        /// </summary>
        public void PopulatePrestige()
        {
            PlayerDataManager pdm = PlayerDataManager.Instance;
            if (pdm == null) return;

            CuratorRank currentRank = pdm.Rank;
            int currentLP           = pdm.LogicPoints;

            UpdateRankCard(currentRank, currentLP);
            BuildTierNodes(currentRank);
        }

        // ── Private helpers ────────────────────────────────────────────────

        private void UpdateRankCard(CuratorRank currentRank, int currentLP)
        {
            if (_rankTitle != null)
                _rankTitle.text = currentRank.ToString();

            if (_rankSubtitle != null &&
                ProfileController.RankSubtitles.TryGetValue(currentRank, out string subtitle))
                _rankSubtitle.text = subtitle;

            UpdateProgressBar(currentRank, currentLP);
        }

        private void UpdateProgressBar(CuratorRank currentRank, int currentLP)
        {
            if (currentRank == CuratorRank.ArchCuratorOfTheVoid)
            {
                if (_progressBarFill != null)
                    _progressBarFill.style.width =
                        new StyleLength(new Length(100f, LengthUnit.Percent));
                if (_progressLabel != null)
                    _progressLabel.text = "Maximum rank attained.";
                return;
            }

            int fromLP    = RankThresholds[currentRank];
            var nextRank  = (CuratorRank)((int)currentRank + 1);
            int toLP      = RankThresholds[nextRank];
            float fraction = Mathf.Clamp01((float)(currentLP - fromLP) / (toLP - fromLP));

            if (_progressBarFill != null)
                _progressBarFill.style.width =
                    new StyleLength(new Length(fraction * 100f, LengthUnit.Percent));

            if (_progressLabel != null)
                _progressLabel.text = $"{currentLP:N0} / {toLP:N0} LP";
        }

        private void BuildTierNodes(CuratorRank currentRank)
        {
            if (_tierList == null) return;
            _tierList.Clear();

            bool first = true;
            foreach (CuratorRank rank in Enum.GetValues(typeof(CuratorRank)))
            {
                if (!first)
                    _tierList.Add(BuildConnector());
                first = false;

                TierState state = rank < currentRank  ? TierState.Completed
                                : rank == currentRank ? TierState.Current
                                :                       TierState.Locked;

                _tierList.Add(BuildTierNode(rank, state));
            }
        }

        private static VisualElement BuildConnector()
        {
            var connector = new VisualElement();
            connector.AddToClassList("tier-connector");
            return connector;
        }

        private static VisualElement BuildTierNode(CuratorRank rank, TierState state)
        {
            var node = new VisualElement();
            node.AddToClassList("tier-node");

            var icon = new VisualElement();
            icon.AddToClassList("tier-icon");
            icon.AddToClassList(state switch
            {
                TierState.Completed => "tier-icon--completed",
                TierState.Current   => "tier-icon--current",
                _                   => "tier-icon--locked",
            });

            var content = new VisualElement();
            content.AddToClassList("tier-content");

            var nameLabel = new Label(rank.ToString());
            nameLabel.AddToClassList(state switch
            {
                TierState.Completed => "tier-rank-name--completed",
                TierState.Current   => "tier-rank-name--current",
                _                   => "tier-rank-name--locked",
            });

            var lpLabel = new Label($"{RankThresholds[rank]:N0} LP");
            lpLabel.AddToClassList("tier-lp-label");

            content.Add(nameLabel);
            content.Add(lpLabel);
            node.Add(icon);
            node.Add(content);
            return node;
        }
    }
}
