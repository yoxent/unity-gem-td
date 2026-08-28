using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.SkillLab;
using GemTD.Gameplay.Towers;

namespace GemTD.UI
{
    public sealed class SkillLabHud : MonoBehaviour
    {
        const string EmptyLabel = "Empty";

        [SerializeField] SkillLabController lab;
        [SerializeField] TMP_Dropdown towerDropdown;
        [SerializeField] TMP_Dropdown[] gemSlotDropdowns;
        [SerializeField] Button fireButton;
        [SerializeField] Button clearButton;
        [SerializeField] Button resetPinsButton;
        [SerializeField] Button backButton;
        [SerializeField] TMP_Text hydraLabel;
        [SerializeField] TMP_Text statusLabel;
        [SerializeField] TMP_Text legendLabel;

        readonly List<GemId>[] _optionIds = new List<GemId>[8];
        readonly List<TMP_Dropdown.OptionData> _optionScratch = new List<TMP_Dropdown.OptionData>(256);
        bool _suppressDropdown;
        int _boundFingerprint;
        int _towerListFingerprint;

        void Awake()
        {
            if (lab == null) Debug.LogError("SkillLabHud: lab is not assigned.", this);
            if (towerDropdown == null) Debug.LogError("SkillLabHud: towerDropdown is not assigned.", this);
            if (gemSlotDropdowns == null || gemSlotDropdowns.Length == 0)
                Debug.LogError("SkillLabHud: gemSlotDropdowns is not assigned.", this);
            if (fireButton == null) Debug.LogError("SkillLabHud: fireButton is not assigned.", this);
            if (clearButton == null) Debug.LogError("SkillLabHud: clearButton is not assigned.", this);
            if (resetPinsButton == null) Debug.LogError("SkillLabHud: resetPinsButton is not assigned.", this);
            if (backButton == null) Debug.LogError("SkillLabHud: backButton is not assigned.", this);
            if (hydraLabel == null) Debug.LogError("SkillLabHud: hydraLabel is not assigned.", this);
            if (statusLabel == null) Debug.LogError("SkillLabHud: statusLabel is not assigned.", this);
            if (legendLabel == null) Debug.LogError("SkillLabHud: legendLabel is not assigned.", this);

            if (lab == null)
                return;
            if (towerDropdown != null) towerDropdown.onValueChanged.AddListener(OnTowerDropdownChanged);
            if (fireButton != null) fireButton.onClick.AddListener(() => lab.Fire());
            if (clearButton != null) clearButton.onClick.AddListener(() => lab.ClearOverlay());
            if (resetPinsButton != null) resetPinsButton.onClick.AddListener(() => lab.ResetPins());
            if (backButton != null) backButton.onClick.AddListener(() => lab.BackToMenu());

            if (gemSlotDropdowns != null)
            {
                for (var i = 0; i < gemSlotDropdowns.Length; i++)
                {
                    var index = i;
                    if (_optionIds[i] == null)
                        _optionIds[i] = new List<GemId>(12);
                    if (gemSlotDropdowns[i] != null)
                        gemSlotDropdowns[i].onValueChanged.AddListener(value => OnGemDropdownChanged(index, value));
                }
            }

            if (legendLabel != null)
                legendLabel.text = "White primary  Yellow pierce  Magenta fork  Orange chain  Red AoE  Cyan warp rise  Gold warp drop  Red-orange magma";
        }

        void LateUpdate()
        {
            if (lab != null)
                Bind(lab.Session);
        }

        public void Bind(SkillLabSession session)
        {
            if (session == null)
                return;
            if (statusLabel != null)
                statusLabel.text = session.Status ?? "";
            if (hydraLabel != null)
                hydraLabel.gameObject.SetActive(false);

            if (AnyDropdownExpanded())
                return;

            var fingerprint = Fingerprint(session);
            if (fingerprint == _boundFingerprint)
                return;

            BindDropdowns(session);
            _boundFingerprint = fingerprint;
        }

        void OnTowerDropdownChanged(int value)
        {
            if (_suppressDropdown || lab == null)
                return;
            lab.SelectTower(value);
            _boundFingerprint = 0;
        }

        void OnGemDropdownChanged(int slot, int value)
        {
            if (_suppressDropdown || lab == null)
                return;
            if (slot < 0 || slot >= _optionIds.Length || _optionIds[slot] == null)
                return;
            if (value < 0 || value >= _optionIds[slot].Count)
                return;
            lab.SetSocket(slot, _optionIds[slot][value]);
            _boundFingerprint = 0;
        }

        void BindDropdowns(SkillLabSession session)
        {
            _suppressDropdown = true;
            BindTowerDropdown(session);

            var sockets = session.Tower != null ? session.Tower.Sockets : null;
            var count = sockets != null ? sockets.Length : 0;
            if (gemSlotDropdowns == null)
            {
                _suppressDropdown = false;
                return;
            }

            for (var i = 0; i < gemSlotDropdowns.Length; i++)
            {
                var dropdown = gemSlotDropdowns[i];
                if (dropdown == null)
                    continue;
                var active = i < count;
                dropdown.gameObject.SetActive(active);
                if (!active)
                    continue;

                if (_optionIds[i] == null)
                    _optionIds[i] = new List<GemId>(12);
                _optionIds[i].Clear();
                _optionScratch.Clear();

                _optionIds[i].Add(GemId.None);
                _optionScratch.Add(new TMP_Dropdown.OptionData(EmptyLabel));

                var current = sockets[i];
                var ids = session.DraftGemIds;
                for (var g = 0; g < ids.Length; g++)
                {
                    var id = ids[g];
                    var gem = session.CatalogGem(id);
                    if (gem == null)
                        continue;
                    if (!GemTags.CanSocket(session.Tower.Def, gem))
                        continue;
                    var usedElsewhere = false;
                    for (var s = 0; s < sockets.Length; s++)
                    {
                        if (s == i)
                            continue;
                        if (!sockets[s].IsEmpty && sockets[s].Id == id)
                        {
                            usedElsewhere = true;
                            break;
                        }
                    }

                    if (usedElsewhere)
                        continue;

                    _optionIds[i].Add(id);
                    var label = string.IsNullOrEmpty(gem.DisplayName) ? id.ToString() : gem.DisplayName;
                    _optionScratch.Add(new TMP_Dropdown.OptionData(label));
                }

                dropdown.ClearOptions();
                dropdown.AddOptions(_optionScratch);

                var selected = 0;
                var currentId = current.IsEmpty ? GemId.None : current.Id;
                for (var o = 0; o < _optionIds[i].Count; o++)
                {
                    if (_optionIds[i][o] == currentId)
                    {
                        selected = o;
                        break;
                    }
                }

                dropdown.SetValueWithoutNotify(selected);
                dropdown.RefreshShownValue();
            }

            _suppressDropdown = false;
        }

        void BindTowerDropdown(SkillLabSession session)
        {
            if (towerDropdown == null)
                return;

            var towers = session.Towers;
            var listFp = TowerListFingerprint(towers);
            if (listFp != _towerListFingerprint)
            {
                _optionScratch.Clear();
                for (var i = 0; i < towers.Length; i++)
                    _optionScratch.Add(new TMP_Dropdown.OptionData(SkillLabSession.TowerLabel(towers[i])));

                towerDropdown.ClearOptions();
                towerDropdown.AddOptions(_optionScratch);
                _towerListFingerprint = listFp;
            }

            var selected = session.SelectedTowerIndex;
            if (selected < 0 || selected >= towerDropdown.options.Count)
                selected = 0;
            if (towerDropdown.options.Count > 0)
            {
                towerDropdown.SetValueWithoutNotify(selected);
                towerDropdown.RefreshShownValue();
            }
        }

        bool AnyDropdownExpanded()
        {
            if (towerDropdown != null && towerDropdown.IsExpanded)
                return true;
            if (gemSlotDropdowns == null)
                return false;
            for (var i = 0; i < gemSlotDropdowns.Length; i++)
            {
                if (gemSlotDropdowns[i] != null && gemSlotDropdowns[i].IsExpanded)
                    return true;
            }

            return false;
        }

        static int TowerListFingerprint(TowerDefinition[] towers)
        {
            var h = 17;
            if (towers == null)
                return h;
            h = h * 31 + towers.Length;
            for (var i = 0; i < towers.Length; i++)
                h = h * 31 + (towers[i] != null ? towers[i].GetInstanceID() : 0);
            return h;
        }

        static int Fingerprint(SkillLabSession session)
        {
            var h = 17;
            if (session.Tower == null || session.Tower.Def == null)
                return h;
            h = h * 31 + session.Tower.Def.GetInstanceID();
            var sockets = session.Tower.Sockets;
            if (sockets == null)
                return h;
            h = h * 31 + sockets.Length;
            for (var i = 0; i < sockets.Length; i++)
            {
                var gem = sockets[i];
                h = h * 31 + (gem.IsEmpty ? 0 : (int)gem.Id + 1);
            }

            return h;
        }
    }
}
