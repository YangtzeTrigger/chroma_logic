using System.Collections.Generic;
using ChromaLogic.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChromaLogic.UI
{
    /// <summary>
    /// Phase 6 Archive — completed Vessel catalogue.
    /// <para>
    /// Loaded by <see cref="GameManager.LoadArchive"/>. Displays a filterable grid of
    /// completed Vessel cards sourced from <see cref="PlayerDataManager.CompletedVesselIds"/>.
    /// </para>
    /// <para>
    /// Favourite state is persisted in <c>PlayerPrefs</c> key <c>CL_FavouriteVessels</c>
    /// (pipe-separated vessel IDs). Tapping a card is a Phase 7 stub.
    /// </para>
    /// <para>
    /// Filter tabs (All Vessels / Gemstones / Constructs) toggle the active class only;
    /// category filtering is deferred until vessel metadata lands.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ArchiveController : MonoBehaviour
    {
        // ── PlayerPrefs key ───────────────────────────────────────────────

        private const string KeyFavourites = "CL_FavouriteVessels";

        // ── UXML element name constants ───────────────────────────────────

        private const string NameBtnFilterAll        = "btn-filter-all";
        private const string NameBtnFilterGemstones  = "btn-filter-gemstones";
        private const string NameBtnFilterConstructs = "btn-filter-constructs";
        private const string NameVesselGrid          = "vessel-grid";
        private const string NameEmptyState          = "archive-empty";
        private const string NameArchiveScroll       = "archive-scroll";

        // ── USS class constants ───────────────────────────────────────────

        private const string ClassFilterActive  = "filter-tab--active";
        private const string ClassFavActive     = "btn-favourite--active";
        private const string ClassHidden        = "hidden";

        // ── Filter enum ───────────────────────────────────────────────────

        private enum FilterTab { All, Gemstones, Constructs }

        // ── Private state ─────────────────────────────────────────────────

        private UIDocument    _document;
        private VisualElement _vesselGrid;
        private ScrollView    _archiveScroll;
        private Label         _emptyLabel;
        private Button        _btnAll;
        private Button        _btnGemstones;
        private Button        _btnConstructs;

        private FilterTab _activeFilter = FilterTab.All;

        // ── Unity lifecycle ───────────────────────────────────────────────

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (_document == null)
            {
                Debug.LogError("[ArchiveController] UIDocument component missing.");
                return;
            }
            BindElements();
        }

        private void Start()
        {
            PopulateGrid(_activeFilter);
        }

        // ── Filter ────────────────────────────────────────────────────────

        private void SetFilter(FilterTab tab)
        {
            _activeFilter = tab;

            _btnAll?.RemoveFromClassList(ClassFilterActive);
            _btnGemstones?.RemoveFromClassList(ClassFilterActive);
            _btnConstructs?.RemoveFromClassList(ClassFilterActive);

            switch (tab)
            {
                case FilterTab.All:        _btnAll?.AddToClassList(ClassFilterActive);        break;
                case FilterTab.Gemstones:  _btnGemstones?.AddToClassList(ClassFilterActive);  break;
                case FilterTab.Constructs: _btnConstructs?.AddToClassList(ClassFilterActive); break;
            }

            PopulateGrid(tab);
        }

        // ── Grid population ───────────────────────────────────────────────

        private void PopulateGrid(FilterTab tab)
        {
            if (_vesselGrid == null) return;

            _vesselGrid.Clear();

            var pdm = PlayerDataManager.Instance;
            var ids = pdm?.CompletedVesselIds ?? new List<string>();

            // TODO: filter by category (Gemstones / Constructs) when vessel metadata lands.
            // All tabs currently show all vessels.

            if (ids.Count == 0)
            {
                _emptyLabel?.RemoveFromClassList(ClassHidden);
                _archiveScroll?.AddToClassList(ClassHidden);
                return;
            }

            _emptyLabel?.AddToClassList(ClassHidden);
            _archiveScroll?.RemoveFromClassList(ClassHidden);

            var favs = LoadFavourites();
            foreach (string id in ids)
                _vesselGrid.Add(BuildVesselCard(id, favs));
        }

        // ── Card builder ──────────────────────────────────────────────────

        private VisualElement BuildVesselCard(string vesselId, HashSet<string> favs)
        {
            var card = new VisualElement();
            card.AddToClassList("vessel-card");
            card.RegisterCallback<ClickEvent>(_ =>
            {
                PlayerPrefs.SetString("CL_PendingJigsawVesselId", vesselId);
                GameManager.Instance?.LoadJigsaw();
            });

            // Image placeholder
            var img = new VisualElement();
            img.AddToClassList("vessel-card-image");
            card.Add(img);

            // Body
            var body = new VisualElement();
            body.AddToClassList("vessel-card-body");

            var nameLabel = new Label(vesselId);
            nameLabel.AddToClassList("vessel-card-name");

            var packLabel = new Label("The Collection");
            packLabel.AddToClassList("vessel-card-pack");

            var dateLabel = new Label(string.Empty);
            dateLabel.AddToClassList("vessel-card-date");

            var descriptorLabel = new Label(string.Empty);
            descriptorLabel.AddToClassList("vessel-card-descriptor");

            body.Add(nameLabel);
            body.Add(packLabel);
            body.Add(dateLabel);
            body.Add(descriptorLabel);
            card.Add(body);

            // Footer with favourite button
            var footer = new VisualElement();
            footer.AddToClassList("vessel-card-footer");

            bool isFav   = favs.Contains(vesselId);
            var starBtn  = new Button { text = isFav ? "★" : "☆" };
            starBtn.AddToClassList("btn-favourite");
            if (isFav) starBtn.AddToClassList(ClassFavActive);

            starBtn.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation(); // prevent card click from firing
                ToggleFavourite(vesselId, starBtn);
            });

            footer.Add(starBtn);
            card.Add(footer);

            return card;
        }

        // ── Favourites ────────────────────────────────────────────────────

        private void ToggleFavourite(string vesselId, Button starBtn)
        {
            var favs = LoadFavourites();
            if (favs.Contains(vesselId))
            {
                favs.Remove(vesselId);
                starBtn.text = "☆";
                starBtn.RemoveFromClassList(ClassFavActive);
            }
            else
            {
                favs.Add(vesselId);
                starBtn.text = "★";
                starBtn.AddToClassList(ClassFavActive);
            }
            SaveFavourites(favs);
        }

        private static HashSet<string> LoadFavourites()
        {
            string raw = PlayerPrefs.GetString(KeyFavourites, string.Empty);
            var set    = new HashSet<string>();
            if (string.IsNullOrEmpty(raw)) return set;
            foreach (string id in raw.Split('|'))
                if (!string.IsNullOrEmpty(id)) set.Add(id);
            return set;
        }

        private static void SaveFavourites(HashSet<string> favs)
        {
            PlayerPrefs.SetString(KeyFavourites, string.Join("|", favs));
            PlayerPrefs.Save();
        }

        // ── Element binding ───────────────────────────────────────────────

        private void BindElements()
        {
            var root       = _document.rootVisualElement;
            _vesselGrid    = root.Q(NameVesselGrid);
            _archiveScroll = root.Q<ScrollView>(NameArchiveScroll);
            _emptyLabel    = root.Q<Label>(NameEmptyState);
            _btnAll        = root.Q<Button>(NameBtnFilterAll);
            _btnGemstones  = root.Q<Button>(NameBtnFilterGemstones);
            _btnConstructs = root.Q<Button>(NameBtnFilterConstructs);

            _btnAll?       .RegisterCallback<ClickEvent>(_ => SetFilter(FilterTab.All));
            _btnGemstones? .RegisterCallback<ClickEvent>(_ => SetFilter(FilterTab.Gemstones));
            _btnConstructs?.RegisterCallback<ClickEvent>(_ => SetFilter(FilterTab.Constructs));
        }
    }
}
