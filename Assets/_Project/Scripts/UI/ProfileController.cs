using System;
using System.Collections.Generic;
using ChromaLogic.Core;
using ChromaLogic.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChromaLogic.UI
{
    /// <summary>
    /// Manages the Curator Profile scene (Phase 8).
    /// <para>
    /// Owns a single <see cref="UIDocument"/> that contains two full-screen panels:
    /// <list type="bullet">
    ///   <item><description>
    ///     <b>profile-panel</b> — Curator portrait placeholder, current rank, Ledger
    ///     statistics, earned Designations. Visible by default.
    ///   </description></item>
    ///   <item><description>
    ///     <b>prestige-container</b> — Path of Prestige tier view, delegated to
    ///     <see cref="PrestigeController"/>. Hidden until the Curator taps
    ///     "Path of Prestige".
    ///   </description></item>
    /// </list>
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ProfileController : MonoBehaviour
    {
        // ── Rank lore subtitles ────────────────────────────────────────────

        /// <summary>
        /// Short lore line displayed beneath each rank name on the profile screen and
        /// inside the prestige rank card. No external source provides these strings —
        /// they are canonical here.
        /// </summary>
        public static readonly IReadOnlyDictionary<CuratorRank, string> RankSubtitles =
            new Dictionary<CuratorRank, string>
            {
                { CuratorRank.Neophyte,             "First steps in The Gallery."        },
                { CuratorRank.Scholar,              "The patterns begin to speak."        },
                { CuratorRank.Architect,            "You impose order on the void."       },
                { CuratorRank.Master,               "The Gallery bends to your will."     },
                { CuratorRank.Grandmaster,          "Few have walked this far."           },
                { CuratorRank.ArchCuratorOfTheVoid, "The Gallery is yours."              },
            };

        // ── Designation display names ──────────────────────────────────────

        private static readonly Dictionary<string, string> DesignationNames =
            new Dictionary<string, string>
            {
                { ProgressionManager.DesignationSovereignOfSymmetry, "Sovereign of Symmetry" },
                { ProgressionManager.DesignationCuratorAscended,     "Curator Ascended"      },
            };

        // ── UXML element name constants ────────────────────────────────────

        private const string NameProfilePanel      = "profile-panel";
        private const string NamePrestigeContainer = "prestige-container";
        private const string NameRankTitle         = "rank-title";
        private const string NameRankSubtitle      = "rank-subtitle";
        private const string NameLedgerLp          = "ledger-lp";
        private const string NameLedgerVessels     = "ledger-vessels";
        private const string NameLedgerRevelation  = "ledger-revelation";
        private const string NameDesignationList   = "designation-list";
        private const string NameDesignationEmpty  = "designation-empty";
        private const string NameBtnBack           = "btn-back";
        private const string NameBtnPrestige       = "btn-prestige";
        private const string NameBtnPrestigeBack   = "btn-prestige-back";

        // ── Component references ───────────────────────────────────────────

        private PrestigeController _prestige;

        // ── UXML element references ────────────────────────────────────────

        private VisualElement _profilePanel;
        private VisualElement _prestigeContainer;
        private Label         _rankTitle;
        private Label         _rankSubtitle;
        private Label         _ledgerLp;
        private Label         _ledgerVessels;
        private Label         _ledgerRevelation;
        private VisualElement _designationList;
        private Label         _designationEmpty;

        // ── Unity lifecycle ───────────────────────────────────────────────

        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            _profilePanel      = root.Q<VisualElement>(NameProfilePanel);
            _prestigeContainer = root.Q<VisualElement>(NamePrestigeContainer);
            _rankTitle         = root.Q<Label>(NameRankTitle);
            _rankSubtitle      = root.Q<Label>(NameRankSubtitle);
            _ledgerLp          = root.Q<Label>(NameLedgerLp);
            _ledgerVessels     = root.Q<Label>(NameLedgerVessels);
            _ledgerRevelation  = root.Q<Label>(NameLedgerRevelation);
            _designationList   = root.Q<VisualElement>(NameDesignationList);
            _designationEmpty  = root.Q<Label>(NameDesignationEmpty);

            root.Q<Button>(NameBtnBack)         ?.RegisterCallback<ClickEvent>(_ => GameManager.Instance?.LoadDashboard());
            root.Q<Button>(NameBtnPrestige)     ?.RegisterCallback<ClickEvent>(_ => ShowPrestige());
            root.Q<Button>(NameBtnPrestigeBack) ?.RegisterCallback<ClickEvent>(_ => ShowProfile());

            _prestige = GetComponent<PrestigeController>();
            _prestige?.Initialize(root);
        }

        private void Start()
        {
            PopulateProfile();
        }

        // ── Panel navigation ───────────────────────────────────────────────

        /// <summary>
        /// Reveals the profile panel and hides the prestige panel.
        /// Called when the Curator taps "← Profile" in the prestige view.
        /// </summary>
        public void ShowProfile()
        {
            _profilePanel?     .RemoveFromClassList("hidden");
            _prestigeContainer?.AddToClassList("hidden");
        }

        /// <summary>
        /// Reveals the prestige panel and hides the profile panel.
        /// Called when the Curator taps "Path of Prestige".
        /// </summary>
        public void ShowPrestige()
        {
            _profilePanel?     .AddToClassList("hidden");
            _prestigeContainer?.RemoveFromClassList("hidden");
            _prestige?.PopulatePrestige();
        }

        // ── Profile population ─────────────────────────────────────────────

        private void PopulateProfile()
        {
            PlayerDataManager pdm = PlayerDataManager.Instance;
            if (pdm == null) return;

            CuratorRank rank = pdm.Rank;

            if (_rankTitle != null)
                _rankTitle.text = rank.ToString();

            if (_rankSubtitle != null)
                _rankSubtitle.text = RankSubtitles.TryGetValue(rank, out string subtitle)
                    ? subtitle : string.Empty;

            if (_ledgerLp != null)
                _ledgerLp.text = pdm.LogicPoints.ToString("N0");

            if (_ledgerVessels != null)
                _ledgerVessels.text = pdm.CompletedVesselIds.Count.ToString();

            if (_ledgerRevelation != null)
                _ledgerRevelation.text = FormatRevelation(pdm.RevelationSeconds);

            PopulateDesignations();
        }

        private void PopulateDesignations()
        {
            if (_designationList == null) return;

            _designationList.Clear();

            IReadOnlyList<string> earned =
                ProgressionManager.Instance?.EarnedDesignations ?? Array.Empty<string>();

            foreach (string id in earned)
            {
                string displayName = DesignationNames.TryGetValue(id, out string name)
                    ? name : id;

                var tile = new VisualElement();
                tile.AddToClassList("designation-tile");

                var label = new Label(displayName);
                label.AddToClassList("designation-tile-name");
                tile.Add(label);

                _designationList.Add(tile);
            }

            if (_designationEmpty != null)
                _designationEmpty.style.display = earned.Count > 0
                    ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // ── Revelation formatter ───────────────────────────────────────────

        /// <summary>
        /// Converts raw seconds to a Revelation string for display.
        /// Returns <c>"Xh Ym Revelation"</c> when over an hour; <c>"Xm Revelation"</c>
        /// below one hour.
        /// </summary>
        private static string FormatRevelation(float seconds)
        {
            long h = (long)seconds / 3600;
            long m = ((long)seconds % 3600) / 60;
            return h > 0 ? $"{h}h {m}m Revelation" : $"{m}m Revelation";
        }
    }
}
