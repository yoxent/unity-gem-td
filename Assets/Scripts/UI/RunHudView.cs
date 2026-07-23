using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using GemTD.Core;
using GemTD.Gameplay;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Run;

namespace GemTD.UI
{
    /// <summary>Greybox run HUD: lives/gold/wave/state, Start Wave, Sell, gem sockets, draft.</summary>
    public sealed class RunHudView : MonoBehaviour
    {
        const string SuppressSellGemWarningKey = "GemTD.SuppressSellGemWarning";

        [SerializeField] Text livesText;
        [SerializeField] Text goldText;
        [SerializeField] Text waveText;
        [SerializeField] Text stateText;
        [SerializeField] Text defeatText;
        [SerializeField] Button startWaveButton;
        [SerializeField] Button sellButton;
        [SerializeField] Button socketLmpButton;
        [SerializeField] Button socketChainButton;

        [Header("Sell confirm (socketed gems)")]
        [SerializeField] GameObject sellConfirmPanel;
        [SerializeField] Text sellConfirmMessage;
        [SerializeField] Toggle sellConfirmDontShowAgain;
        [SerializeField] Button sellConfirmYesButton;
        [SerializeField] Button sellConfirmNoButton;

        [Header("Draft (optional — auto-built at runtime if empty)")]
        [SerializeField] GameObject draftPanel;
        [SerializeField] Text draftTitleText;
        [SerializeField] Button draftPick0Button;
        [SerializeField] Button draftPick1Button;
        [SerializeField] Button draftPick2Button;
        [SerializeField] Button draftSkipButton;
        [SerializeField] GameObject draftReplacePanel;
        [SerializeField] Text draftReplaceMessage;
        [SerializeField] Button draftReplaceYesButton;
        [SerializeField] Button draftReplaceNoButton;

        GameCompositionRoot _root;
        bool _buttonsBound;
        bool _draftUiBuilt;
        string _lastDraftSignature;

        readonly Button[] _invButtons = new Button[10];
        readonly Text[] _invLabels = new Text[10];
        GameObject _inventoryPanel;
        Text _inventoryHint;
        string _lastInvSignature;
        bool _inventoryUiBuilt;

        void OnEnable()
        {
            BindGameEvents();
            BindButtons();
        }

        void OnDisable()
        {
            UnbindGameEvents();
            UnbindButtons();
        }

        void Start()
        {
            BindGameEvents();
            _root = GameCompositionRoot.Instance;
            HideSellConfirm();
            HideLegacySocketButtons();
            EnsureDraftUi();
            EnsureInventoryUi();
            BindButtons();
            RefreshAll();
            RefreshDraftUi(force: true);
            RefreshInventoryUi(force: true);
        }

        void Update()
        {
            if (_root == null)
                _root = GameCompositionRoot.Instance;
            if (_root == null)
                return;

            if (_root.Economy != null)
            {
                OnGoldChanged(_root.Economy.Gold);
                OnLivesChanged(_root.Economy.Lives);
            }
            OnWaveChanged(_root.CurrentWaveNumber);

            if (stateText != null && _root.States != null)
            {
                if (_root.States.Current == RunStateId.Draft && _root.Draft != null && _root.Draft.IsActive)
                {
                    stateText.text = BuildDraftStateLine();
                }
                else
                {
                    stateText.text =
                        $"State: {_root.States.Current} | Place: {_root.PlaceTowerName}\n" +
                        $"Aim: {FormatAim(_root.SelectedTargetingMode)} (R) | Scope: {FormatScope(_root.CurrentApplyScope)} (Shift+R)";
                }
            }

            if (defeatText != null)
            {
                var defeated = _root.States != null && _root.States.Current == RunStateId.Defeat;
                var victory = _root.States != null && _root.States.Current == RunStateId.VictorySummary;
                defeatText.gameObject.SetActive(defeated || victory);
                if (defeated)
                    defeatText.text = "Defeat — restart Play Mode";
                else if (victory)
                    defeatText.text = "Victory — restart Play Mode";
            }

            var plan = _root.States != null && _root.States.Current == RunStateId.Plan;
            var draft = _root.States != null && _root.States.Current == RunStateId.Draft;
            var confirmOpen = sellConfirmPanel != null && sellConfirmPanel.activeSelf;
            if (startWaveButton != null)
                startWaveButton.interactable = _root.CanStartWave && !confirmOpen && !draft;
            if (sellButton != null)
                sellButton.interactable = plan && _root.HasSelectedTower && !confirmOpen;

            // Legacy PR1 socket buttons — hidden; use inventory bar instead.
            if (socketLmpButton != null)
            {
                socketLmpButton.gameObject.SetActive(false);
                socketLmpButton.interactable = false;
            }
            if (socketChainButton != null)
            {
                socketChainButton.gameObject.SetActive(false);
                socketChainButton.interactable = false;
            }

            RefreshDraftUi(force: false);
            RefreshInventoryUi(force: false);
        }

        void HideLegacySocketButtons()
        {
            if (socketLmpButton != null)
                socketLmpButton.gameObject.SetActive(false);
            if (socketChainButton != null)
                socketChainButton.gameObject.SetActive(false);
        }

        void EnsureInventoryUi()
        {
            if (_inventoryUiBuilt && _inventoryPanel != null)
                return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;

            var panelGo = new GameObject("InventoryPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvas.transform, false);
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0f);
            panelRt.anchorMax = new Vector2(0.5f, 0f);
            panelRt.pivot = new Vector2(0.5f, 0f);
            panelRt.sizeDelta = new Vector2(720f, 88f);
            panelRt.anchoredPosition = new Vector2(0f, 12f);
            panelGo.GetComponent<Image>().color = new Color(0.07f, 0.08f, 0.1f, 0.92f);

            _inventoryHint = CreateUiText(
                panelGo.transform,
                "InvHint",
                "Inventory — click=socket (select tower) | Shift+click=discard (Plan)",
                new Vector2(0f, 32f),
                14);
            var hintRt = _inventoryHint.GetComponent<RectTransform>();
            hintRt.sizeDelta = new Vector2(700f, 22f);

            const float slotW = 64f;
            const float gap = 6f;
            var totalW = 10 * slotW + 9 * gap;
            var startX = -totalW * 0.5f + slotW * 0.5f;
            for (var i = 0; i < 10; i++)
            {
                var x = startX + i * (slotW + gap);
                var btn = CreateUiButton(panelGo.transform, "InvSlot" + i, "—", new Vector2(x, -8f), new Vector2(slotW, 48f));
                var idx = i;
                btn.onClick.AddListener(() => OnInventorySlotClicked(idx));
                _invButtons[i] = btn;
                _invLabels[i] = btn.GetComponentInChildren<Text>();
            }

            _inventoryPanel = panelGo;
            _inventoryUiBuilt = true;
        }

        void RefreshInventoryUi(bool force)
        {
            if (!_inventoryUiBuilt)
                EnsureInventoryUi();
            if (_inventoryPanel == null || _root == null)
                return;

            var inv = _root.Inventory;
            var show = inv != null
                       && _root.States != null
                       && _root.States.Current != RunStateId.Boot
                       && _root.States.Current != RunStateId.Defeat
                       && _root.States.Current != RunStateId.VictorySummary;
            _inventoryPanel.SetActive(show);
            if (!show || inv == null)
                return;

            var sig = $"{_root.States.Current}|{_root.HasSelectedTower}|{inv.OccupiedCount}|";
            for (var i = 0; i < inv.Slots.Count && i < 10; i++)
            {
                var g = inv.Slots[i];
                sig += g != null ? ((int)g.Id).ToString() : "0";
                sig += ",";
            }

            if (!force && sig == _lastInvSignature)
                return;
            _lastInvSignature = sig;

            var inPlan = _root.States.Current == RunStateId.Plan;
            var canSocket = (_root.States.Current == RunStateId.Plan || _root.States.Current == RunStateId.Combat)
                            && _root.HasSelectedTower;
            var replacePick = _root.States.Current == RunStateId.Draft
                              && _root.Draft != null
                              && _root.Draft.ReplacePhase == DraftReplacePhase.AwaitingInventoryPick;

            if (_inventoryHint != null)
            {
                if (replacePick)
                    _inventoryHint.text = "Inventory — click a gem to DESTROY & take draft card";
                else if (canSocket)
                    _inventoryHint.text = $"Inventory {inv.OccupiedCount}/{inv.Capacity} — click=socket onto selected tower | Shift+click=discard (Plan)";
                else if (inPlan)
                    _inventoryHint.text = $"Inventory {inv.OccupiedCount}/{inv.Capacity} — select a tower to socket, or Shift+click to discard";
                else
                    _inventoryHint.text = $"Inventory {inv.OccupiedCount}/{inv.Capacity}";
            }

            for (var i = 0; i < 10; i++)
            {
                var btn = _invButtons[i];
                var label = _invLabels[i];
                if (btn == null)
                    continue;

                GemDefinition gem = null;
                if (i < inv.Slots.Count)
                    gem = inv.Slots[i];

                if (label != null)
                {
                    if (gem == null)
                        label.text = "—";
                    else
                        label.text = ShortGemName(gem);
                }

                var filled = gem != null;
                btn.interactable = filled && (canSocket || replacePick || (inPlan && filled));
                var img = btn.GetComponent<Image>();
                if (img != null)
                {
                    img.color = filled
                        ? new Color(0.28f, 0.42f, 0.32f, 1f)
                        : new Color(0.16f, 0.17f, 0.2f, 1f);
                }
            }
        }

        void OnInventorySlotClicked(int index)
        {
            if (_root == null)
                return;

            var kb = Keyboard.current;
            var shift = kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
            _root.RequestInventorySlotClick(index, shift);
            RefreshInventoryUi(force: true);
        }

        static string ShortGemName(GemDefinition gem)
        {
            if (gem == null)
                return "—";
            switch (gem.Id)
            {
                case GemId.Lmp: return "LMP";
                case GemId.Chain: return "Chain";
                case GemId.FasterAttacks: return "Fast";
                case GemId.IncreasedAccuracy: return "Acc";
                case GemId.SlowerProjectiles: return "Slow";
                case GemId.AttackEcho: return "Echo";
                default:
                    return string.IsNullOrEmpty(gem.DisplayName) ? gem.Id.ToString() : gem.DisplayName;
            }
        }

        void EnsureDraftUi()
        {
            if (draftPanel != null)
            {
                _draftUiBuilt = true;
                return;
            }

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;

            var panelGo = new GameObject("DraftPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvas.transform, false);
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(520f, 220f);
            panelRt.anchoredPosition = Vector2.zero;
            var panelImg = panelGo.GetComponent<Image>();
            panelImg.color = new Color(0.08f, 0.09f, 0.12f, 0.94f);

            draftTitleText = CreateUiText(panelGo.transform, "DraftTitle", "Draft — pick a gem", new Vector2(0f, 80f), 22);
            draftPick0Button = CreateUiButton(panelGo.transform, "DraftPick0", "Gem A (Z)", new Vector2(-160f, 10f), new Vector2(140f, 56f));
            draftPick1Button = CreateUiButton(panelGo.transform, "DraftPick1", "Gem B (X)", new Vector2(0f, 10f), new Vector2(140f, 56f));
            draftPick2Button = CreateUiButton(panelGo.transform, "DraftPick2", "Gem C (C)", new Vector2(160f, 10f), new Vector2(140f, 56f));
            draftSkipButton = CreateUiButton(panelGo.transform, "DraftSkip", "Skip +75g (V)", new Vector2(0f, -60f), new Vector2(180f, 40f));

            var replaceGo = new GameObject("DraftReplacePanel", typeof(RectTransform), typeof(Image));
            replaceGo.transform.SetParent(panelGo.transform, false);
            var replaceRt = replaceGo.GetComponent<RectTransform>();
            replaceRt.anchorMin = new Vector2(0.5f, 0.5f);
            replaceRt.anchorMax = new Vector2(0.5f, 0.5f);
            replaceRt.sizeDelta = new Vector2(420f, 140f);
            replaceRt.anchoredPosition = new Vector2(0f, -20f);
            replaceGo.GetComponent<Image>().color = new Color(0.15f, 0.12f, 0.1f, 0.98f);
            draftReplaceMessage = CreateUiText(
                replaceGo.transform,
                "ReplaceMsg",
                "Bag full — replace an inventory gem?",
                new Vector2(0f, 30f),
                18);
            draftReplaceYesButton = CreateUiButton(replaceGo.transform, "ReplaceYes", "Yes (N)", new Vector2(-70f, -30f), new Vector2(110f, 36f));
            draftReplaceNoButton = CreateUiButton(replaceGo.transform, "ReplaceNo", "No (M)", new Vector2(70f, -30f), new Vector2(110f, 36f));
            draftReplacePanel = replaceGo;
            draftReplacePanel.SetActive(false);

            draftPanel = panelGo;
            draftPanel.SetActive(false);
            _draftUiBuilt = true;
        }

        void RefreshDraftUi(bool force)
        {
            if (!_draftUiBuilt)
                EnsureDraftUi();
            if (draftPanel == null || _root == null || _root.States == null)
                return;

            var inDraft = _root.States.Current == RunStateId.Draft
                          && _root.Draft != null
                          && _root.Draft.IsActive;
            draftPanel.SetActive(inDraft);
            if (!inDraft)
            {
                _lastDraftSignature = null;
                return;
            }

            var draft = _root.Draft;
            var sig = $"{draft.AllowSkip}|{draft.ReplacePhase}|";
            for (var i = 0; i < draft.CurrentOffer.Count; i++)
            {
                var g = draft.CurrentOffer[i];
                sig += g != null ? g.Id.ToString() : "null";
                sig += ",";
            }

            if (!force && sig == _lastDraftSignature)
            {
                UpdateReplaceVisibility(draft);
                return;
            }

            _lastDraftSignature = sig;
            if (draftTitleText != null)
            {
                draftTitleText.text = draft.AllowSkip
                    ? "Draft — pick a gem or skip for +75g"
                    : "Starter Draft — pick 1 gem (required)";
            }

            SetPickButton(draftPick0Button, draft, 0, "Z");
            SetPickButton(draftPick1Button, draft, 1, "X");
            SetPickButton(draftPick2Button, draft, 2, "C");

            if (draftSkipButton != null)
            {
                draftSkipButton.gameObject.SetActive(draft.AllowSkip);
                draftSkipButton.interactable = draft.AllowSkip && draft.ReplacePhase == DraftReplacePhase.None;
            }

            UpdateReplaceVisibility(draft);
        }

        void UpdateReplaceVisibility(DraftService draft)
        {
            if (draftReplacePanel == null)
                return;

            var awaiting = draft.ReplacePhase == DraftReplacePhase.AwaitingConfirm
                           || draft.ReplacePhase == DraftReplacePhase.AwaitingInventoryPick;
            draftReplacePanel.SetActive(awaiting);
            if (!awaiting || draftReplaceMessage == null)
                return;

            if (draft.ReplacePhase == DraftReplacePhase.AwaitingConfirm)
            {
                var name = draft.PendingReplaceGem != null ? draft.PendingReplaceGem.DisplayName : "gem";
                draftReplaceMessage.text = $"Bag full — destroy one inventory gem to take {name}?";
            }
            else
            {
                draftReplaceMessage.text = "Replace mode — click an inventory gem to destroy it, or M to cancel.";
            }
        }

        static void SetPickButton(Button button, DraftService draft, int index, string key)
        {
            if (button == null)
                return;

            var label = button.GetComponentInChildren<Text>();
            if (index < draft.CurrentOffer.Count && draft.CurrentOffer[index] != null)
            {
                var gem = draft.CurrentOffer[index];
                if (label != null)
                    label.text = $"{gem.DisplayName}\n({key})";
                button.interactable = draft.ReplacePhase == DraftReplacePhase.None;
                button.gameObject.SetActive(true);
            }
            else
            {
                button.gameObject.SetActive(false);
            }
        }

        string BuildDraftStateLine()
        {
            var draft = _root.Draft;
            var a = OfferName(draft, 0);
            var b = OfferName(draft, 1);
            var c = OfferName(draft, 2);
            var skip = draft.AllowSkip ? " | V=Skip+75" : " | no skip";
            return $"DRAFT: [Z] {a}  [X] {b}  [C] {c}{skip}";
        }

        static string OfferName(DraftService draft, int index)
        {
            if (draft == null || index >= draft.CurrentOffer.Count || draft.CurrentOffer[index] == null)
                return "?";
            return draft.CurrentOffer[index].DisplayName;
        }

        void BindGameEvents()
        {
            UnbindGameEvents();
            GameEvents.GoldChanged += OnGoldChanged;
            GameEvents.LivesChanged += OnLivesChanged;
            GameEvents.WaveChanged += OnWaveChanged;
        }

        void UnbindGameEvents()
        {
            GameEvents.GoldChanged -= OnGoldChanged;
            GameEvents.LivesChanged -= OnLivesChanged;
            GameEvents.WaveChanged -= OnWaveChanged;
        }

        void BindButtons()
        {
            if (_buttonsBound)
                UnbindButtons();

            if (startWaveButton != null)
                startWaveButton.onClick.AddListener(OnStartWave);
            if (sellButton != null)
                sellButton.onClick.AddListener(OnSell);
            if (socketLmpButton != null)
                socketLmpButton.onClick.AddListener(OnSocketLmp);
            if (socketChainButton != null)
                socketChainButton.onClick.AddListener(OnSocketChain);
            if (sellConfirmYesButton != null)
                sellConfirmYesButton.onClick.AddListener(OnSellConfirmYes);
            if (sellConfirmNoButton != null)
                sellConfirmNoButton.onClick.AddListener(OnSellConfirmNo);
            if (draftPick0Button != null)
                draftPick0Button.onClick.AddListener(OnDraftPick0);
            if (draftPick1Button != null)
                draftPick1Button.onClick.AddListener(OnDraftPick1);
            if (draftPick2Button != null)
                draftPick2Button.onClick.AddListener(OnDraftPick2);
            if (draftSkipButton != null)
                draftSkipButton.onClick.AddListener(OnDraftSkip);
            if (draftReplaceYesButton != null)
                draftReplaceYesButton.onClick.AddListener(OnDraftReplaceYes);
            if (draftReplaceNoButton != null)
                draftReplaceNoButton.onClick.AddListener(OnDraftReplaceNo);
            _buttonsBound = true;
        }

        void UnbindButtons()
        {
            if (!_buttonsBound)
                return;
            if (startWaveButton != null)
                startWaveButton.onClick.RemoveListener(OnStartWave);
            if (sellButton != null)
                sellButton.onClick.RemoveListener(OnSell);
            if (socketLmpButton != null)
                socketLmpButton.onClick.RemoveListener(OnSocketLmp);
            if (socketChainButton != null)
                socketChainButton.onClick.RemoveListener(OnSocketChain);
            if (sellConfirmYesButton != null)
                sellConfirmYesButton.onClick.RemoveListener(OnSellConfirmYes);
            if (sellConfirmNoButton != null)
                sellConfirmNoButton.onClick.RemoveListener(OnSellConfirmNo);
            if (draftPick0Button != null)
                draftPick0Button.onClick.RemoveListener(OnDraftPick0);
            if (draftPick1Button != null)
                draftPick1Button.onClick.RemoveListener(OnDraftPick1);
            if (draftPick2Button != null)
                draftPick2Button.onClick.RemoveListener(OnDraftPick2);
            if (draftSkipButton != null)
                draftSkipButton.onClick.RemoveListener(OnDraftSkip);
            if (draftReplaceYesButton != null)
                draftReplaceYesButton.onClick.RemoveListener(OnDraftReplaceYes);
            if (draftReplaceNoButton != null)
                draftReplaceNoButton.onClick.RemoveListener(OnDraftReplaceNo);
            _buttonsBound = false;
        }

        void RefreshAll()
        {
            if (_root == null)
                return;
            if (_root.Economy != null)
            {
                OnGoldChanged(_root.Economy.Gold);
                OnLivesChanged(_root.Economy.Lives);
            }
            OnWaveChanged(_root.CurrentWaveNumber);
        }

        void OnGoldChanged(int gold)
        {
            if (goldText != null)
                goldText.text = $"Gold: {gold}";
        }

        void OnLivesChanged(int lives)
        {
            if (livesText != null)
                livesText.text = $"Lives: {lives}";
        }

        void OnWaveChanged(int wave)
        {
            if (waveText != null)
                waveText.text = $"Wave: {wave}";
        }

        void OnStartWave() => _root?.RequestStartWave();

        void OnSell()
        {
            if (_root == null || !_root.HasSelectedTower)
                return;

            if (_root.SelectedHasSocketedGems && !IsSellGemWarningSuppressed())
            {
                ShowSellConfirm();
                return;
            }

            _root.RequestSellSelected();
        }

        void OnSellConfirmYes()
        {
            if (sellConfirmDontShowAgain != null && sellConfirmDontShowAgain.isOn)
                PlayerPrefs.SetInt(SuppressSellGemWarningKey, 1);

            HideSellConfirm();
            _root?.RequestSellSelected();
        }

        void OnSellConfirmNo() => HideSellConfirm();

        void ShowSellConfirm()
        {
            if (sellConfirmPanel == null)
            {
                _root?.RequestSellSelected();
                return;
            }

            if (sellConfirmMessage != null)
            {
                sellConfirmMessage.text =
                    "This tower has a socketed gem.\nSelling will unsocket the gem back to your inventory.";
            }

            if (sellConfirmDontShowAgain != null)
                sellConfirmDontShowAgain.isOn = false;

            sellConfirmPanel.SetActive(true);
        }

        void HideSellConfirm()
        {
            if (sellConfirmPanel != null)
                sellConfirmPanel.SetActive(false);
        }

        static bool IsSellGemWarningSuppressed() =>
            PlayerPrefs.GetInt(SuppressSellGemWarningKey, 0) != 0;

        void OnSocketLmp() => _root?.RequestSocket(GemId.Lmp);
        void OnSocketChain() => _root?.RequestSocket(GemId.Chain);
        void OnDraftPick0() => _root?.RequestDraftPick(0);
        void OnDraftPick1() => _root?.RequestDraftPick(1);
        void OnDraftPick2() => _root?.RequestDraftPick(2);
        void OnDraftSkip() => _root?.RequestDraftSkip();
        void OnDraftReplaceYes() => _root?.RequestDraftReplaceYes();
        void OnDraftReplaceNo() => _root?.RequestDraftReplaceNo();

        static Text CreateUiText(Transform parent, string name, string value, Vector2 anchored, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(480f, 40f);
            rt.anchoredPosition = anchored;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            return text;
        }

        static Button CreateUiButton(Transform parent, string name, string label, Vector2 anchored, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchored;
            go.GetComponent<Image>().color = new Color(0.22f, 0.28f, 0.38f, 1f);
            var text = CreateUiText(go.transform, "Label", label, Vector2.zero, 14);
            var textRt = text.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            text.fontSize = 14;
            return go.GetComponent<Button>();
        }

        static string FormatAim(TargetingMode mode)
        {
            switch (mode)
            {
                case TargetingMode.First: return "First";
                case TargetingMode.Last: return "Last";
                case TargetingMode.Closest: return "Near";
                case TargetingMode.Strongest: return "MaxHP";
                default: return mode.ToString();
            }
        }

        static string FormatScope(TargetingApplyScope scope)
        {
            switch (scope)
            {
                case TargetingApplyScope.ThisTower: return "Tower";
                case TargetingApplyScope.ThisType: return "Type";
                case TargetingApplyScope.AllTowers: return "All";
                default: return scope.ToString();
            }
        }
    }
}
