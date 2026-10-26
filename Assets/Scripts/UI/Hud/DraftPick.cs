using System;
using System.Collections.Generic;
using GemTD.Gameplay.Gems;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GemTD.UI
{
    public class DraftPick : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        const float RestIconScale = 1f;
        const float HoverIconScale = 1.1f;
        const float SelectedIconScale = 0.9f;
        const float PressDepth = 6f;
        const float PressDepthScale = 0.5f;
        const float UpMovePortion = 0.75f;
        const float SecondStatusBounceScale = 0.5f;
        const float StatusSquashAmount = 0.18f;
        const float StatusRecoverSeconds = 0.12f;

        [Header("Content")]
        [SerializeField] DraftElementColors elementColors;
        [SerializeField] ElementColorTargets colorTargets;
        [SerializeField] Button draftButton;
        [SerializeField] TMP_Text draftLabel;
        [SerializeField] TMP_Text draftDescription;
        [SerializeField] Transform tagsParent;
        [SerializeField] Transform tagSubParentTemplate;
        [SerializeField] TagLabel tagLabelPrefab;
        [SerializeField] List<TagLabel> tagLabels = new List<TagLabel>();
        [SerializeField] TMP_Text levelLabel;
        [SerializeField] CanvasGroup draftStatusGroup;
        [SerializeField] TMP_Text draftStatus;

        [Header("Selection")]
        [SerializeField] Transform cardPanel;
        [SerializeField] RectTransform iconMask;
        [SerializeField] CanvasGroup borderCanvasGroup;
        [SerializeField] CanvasGroup selectedGroupPanel;
        [SerializeField] Color selectedHighlightColor;

        [Header("Motion")]
        [SerializeField] float selectMoveSeconds = 0.2f;
        [SerializeField] float selectedCardY = 14f;
        [SerializeField] float iconScaleSeconds = 0.12f;
        [SerializeField] float selectedGroupFadeSeconds = 0.2f;
        [SerializeField] float fadeOutSeconds = 0f;
        [SerializeField] float statusBounceHeight = 12f;
        [SerializeField] float statusBounceSeconds = 0.28f;
        [SerializeField] float statusBounceInterval = 1.2f;

        readonly List<string> _tagNames = new List<string>(8);
        DraftElementColors.Palette _palette;
        bool _havePalette;
        bool _hasSelection;
        bool _selected;
        bool _pointerOver;
        int _iconPunch;
        int _cardPress;

        MotionHandle _cardMove;
        MotionHandle _iconScale;
        MotionHandle _selectedGroupFade;
        MotionHandle _borderFade;
        float _selectedGroupTarget = -1f;
        float _borderTarget = -1f;
        int _selectedGroupFadeId;

        MotionHandle _statusBounce;
        bool _statusWantsBounce;
        bool _statusRestCaptured;
        float _statusRestY;

        void Awake()
        {
            if (tagSubParentTemplate != null)
                tagSubParentTemplate.gameObject.SetActive(false);
            if (selectedGroupPanel != null)
                selectedGroupPanel.alpha = 0f;
        }

        void OnEnable()
        {
            if (_statusWantsBounce && !_statusBounce.IsActive())
                PlayStatusHop();
        }

        void OnDisable()
        {
            _iconPunch++;
            _cardPress++;
            _pointerOver = false;
            if (_cardMove.IsActive())
                _cardMove.Cancel();
            if (_iconScale.IsActive())
                _iconScale.Cancel();
            if (_statusBounce.IsActive())
                _statusBounce.Cancel();
            _iconPunch = 0;
            _cardPress = 0;
            HideSelectedGroupImmediate();
            SetBorderAlpha(_selected ? 0f : 1f);
            SetIconScale(RestIconScale);
            SetCardY(CardRestY());
            ResetStatusPosition();
        }

        public void UpdateLabel(string label, string description, string status, string level)
        {
            if (draftLabel != null)
                draftLabel.text = label ?? "";
            else
                Debug.LogError("DraftPick: assign draftLabel on the prefab.", this);

            if (draftDescription != null)
                draftDescription.text = description ?? "";
            else
                Debug.LogError("DraftPick: assign draftDescription on the prefab.", this);

            if (draftStatus != null)
            {
                var text = status ?? "";
                draftStatus.text = text;
                var showStatus = text.Length > 0;
                draftStatus.gameObject.SetActive(showStatus);
                if (draftStatusGroup == null)
                    Debug.LogError("DraftPick: assign draftStatusGroup on the prefab.", this);
                else
                {
                    draftStatusGroup.alpha = showStatus ? 1f : 0f;
                    SetStatusBounce(showStatus);
                }
            }
            else
                Debug.LogError("DraftPick: assign draftStatus on the prefab.", this);

            if (levelLabel != null)
            {
                var text = level ?? "";
                levelLabel.text = text;
                var badge = levelLabel.transform.parent;
                if (badge != null)
                    badge.gameObject.SetActive(text.Length > 0);
            }
            else
                Debug.LogError("DraftPick: assign levelLabel on the prefab.", this);
        }

        public void ApplyElements(GemTag tags)
        {
            if (elementColors == null)
            {
                Debug.LogError("DraftPick: assign elementColors on the prefab.", this);
                return;
            }

            _palette = elementColors.Resolve(tags);
            _havePalette = true;
            if (!colorTargets.Apply(_palette))
                Debug.LogError("DraftPick: assign colorTargets on the prefab.", this);
            if (_selected && _iconPunch == 0)
                PaintSelectionHighlight(true);
        }

        public void SetTags(GemTag tags)
        {
            if (tagsParent == null)
            {
                Debug.LogError("DraftPick: assign tagsParent on the prefab.", this);
                return;
            }

            if (tagSubParentTemplate == null)
            {
                Debug.LogError("DraftPick: assign tagSubParentTemplate on the prefab.", this);
                return;
            }

            tagSubParentTemplate.gameObject.SetActive(false);
            GemTags.CollectNames(tags, _tagNames);

            for (var i = 0; i < _tagNames.Count; i++)
            {
                var label = GetOrCreateTag(i);
                if (label == null)
                    continue;
                if (label.transform.parent != tagsParent)
                    label.transform.SetParent(tagsParent, false);
                label.transform.SetSiblingIndex(i);
                label.gameObject.SetActive(true);
                label.Bind(_tagNames[i]);
            }

            for (var i = _tagNames.Count; i < tagLabels.Count; i++)
            {
                if (tagLabels[i] != null)
                    tagLabels[i].gameObject.SetActive(false);
            }

            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)tagsParent);
        }

        TagLabel GetOrCreateTag(int index)
        {
            while (tagLabels.Count <= index)
            {
                if (tagLabelPrefab == null)
                {
                    Debug.LogError("DraftPick: assign tagLabelPrefab (DraftTagLabel) on the prefab.", this);
                    return null;
                }

                var label = Instantiate(tagLabelPrefab, tagsParent);
                label.gameObject.SetActive(false);
                tagLabels.Add(label);
            }

            return tagLabels[index];
        }

        public Button GetButton()
        {
            return draftButton;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _pointerOver = true;
            if (_iconPunch == 0 && !_selected)
                TweenIcon(HoverIconScale, null);
            if (!_selected)
                MoveCard(CardHoverY(), selectMoveSeconds, null);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _pointerOver = false;
            if (_iconPunch == 0 && !_selected)
                TweenIcon(RestIconScale, null);
            if (!_selected)
                MoveCard(CardRestY(), selectMoveSeconds, null);
        }

        public void SetSelected(bool selected)
        {
            var changed = !_hasSelection || selected != _selected;
            _selected = selected;
            _hasSelection = true;
            if (!changed)
            {
                if (_iconPunch == 0)
                {
                    if (selected)
                        TweenIcon(HoverIconScale, null);
                    PaintSelectionHighlight(selected);
                    ShowSelectedGroup(selected);
                }
                return;
            }

            if (!selected)
            {
                _iconPunch++;
                _cardPress++;
                PaintSelectionHighlight(false);
                ShowSelectedGroup(false);
                TweenIcon(RestIconScale, null);
                _iconPunch = 0;
                MoveCard(_pointerOver ? CardHoverY() : CardRestY(), selectMoveSeconds, null);
                return;
            }

            HideSelectedGroupImmediate();
            PlayPressElastic();
        }

        void ShowSelectedGroup(bool selected)
        {
            ShowSelectedGroup(selected, selected ? selectedGroupFadeSeconds : fadeOutSeconds);
        }

        void ShowSelectedGroup(bool selected, float seconds)
        {
            if (selectedGroupPanel == null)
            {
                Debug.LogError("DraftPick: assign selectedGroupPanel on the prefab.", this);
                return;
            }

            FadeSelectedGroup(selected ? 1f : 0f, seconds);
        }

        void HideSelectedGroupImmediate()
        {
            _selectedGroupFadeId++;
            _selectedGroupTarget = 0f;
            if (_selectedGroupFade.IsActive())
                _selectedGroupFade.Cancel();
            if (selectedGroupPanel == null)
                return;

            selectedGroupPanel.alpha = 0f;
            selectedGroupPanel.gameObject.SetActive(false);
        }

        void FadeSelectedGroup(float target, float seconds)
        {
            if (_selectedGroupFade.IsActive() && Mathf.Approximately(_selectedGroupTarget, target))
                return;

            var group = selectedGroupPanel;
            var fadeId = ++_selectedGroupFadeId;
            _selectedGroupTarget = target;
            if (_selectedGroupFade.IsActive())
                _selectedGroupFade.Cancel();

            var show = target > 0f;
            if (show && !group.gameObject.activeSelf)
            {
                group.alpha = 0f;
                group.gameObject.SetActive(true);
            }

            if (!show && !group.gameObject.activeSelf)
            {
                group.alpha = 0f;
                return;
            }

            if (seconds <= 0f || Mathf.Approximately(group.alpha, target))
            {
                group.alpha = target;
                if (!show)
                    group.gameObject.SetActive(false);
                return;
            }

            _selectedGroupFade = LMotion.Create(group.alpha, target, seconds)
                .WithEase(Ease.OutQuad)
                .WithOnComplete(() =>
                {
                    if (fadeId != _selectedGroupFadeId || group == null)
                        return;
                    if (target <= 0f)
                        group.gameObject.SetActive(false);
                })
                .Bind(group, static (alpha, g) =>
                {
                    if (g != null)
                        g.alpha = alpha;
                });
        }

        void PaintSelectionHighlight(bool selected)
        {
            var graphic = colorTargets.selectedHighlight;
            if (graphic == null)
                Debug.LogError("DraftPick: assign colorTargets.selectedHighlight on the prefab.", this);

            if (selected)
            {
                if (graphic != null)
                    graphic.color = selectedHighlightColor;
                FadeBorder(0f, selectedGroupFadeSeconds);
                return;
            }

            FadeBorder(1f, fadeOutSeconds);
            if (!_havePalette)
                return;

            if (graphic != null)
                graphic.color = _palette.normal;
        }

        void FadeBorder(float target, float seconds)
        {
            if (borderCanvasGroup == null)
            {
                Debug.LogError("DraftPick: assign borderCanvasGroup on the prefab.", this);
                return;
            }

            if (_borderFade.IsActive() && Mathf.Approximately(_borderTarget, target))
                return;

            var group = borderCanvasGroup;
            _borderTarget = target;
            if (_borderFade.IsActive())
                _borderFade.Cancel();

            if (seconds <= 0f || Mathf.Approximately(group.alpha, target))
            {
                group.alpha = target;
                return;
            }

            _borderFade = LMotion.Create(group.alpha, target, seconds)
                .WithEase(Ease.OutQuad)
                .Bind(group, static (alpha, g) =>
                {
                    if (g != null)
                        g.alpha = alpha;
                });
        }

        void SetBorderAlpha(float alpha)
        {
            _borderTarget = alpha;
            if (_borderFade.IsActive())
                _borderFade.Cancel();
            if (borderCanvasGroup == null)
                return;
            borderCanvasGroup.alpha = alpha;
        }

        void TweenIcon(float target, Action onComplete)
        {
            TweenIcon(target, iconScaleSeconds, onComplete);
        }

        void TweenIcon(float target, float seconds, Action onComplete)
        {
            if (iconMask == null)
            {
                Debug.LogError("DraftPick: assign iconMask on the prefab.", this);
                onComplete?.Invoke();
                return;
            }

            if (_iconScale.IsActive())
                _iconScale.Cancel();

            var from = iconMask.localScale.x;
            if (seconds <= 0f || Mathf.Approximately(from, target))
            {
                SetIconScale(target);
                onComplete?.Invoke();
                return;
            }

            _iconScale = LMotion.Create(from, target, seconds)
                .WithEase(Ease.OutQuad)
                .WithOnComplete(() => onComplete?.Invoke())
                .Bind(iconMask, static (scale, rt) =>
                {
                    if (rt == null)
                        return;
                    rt.localScale = new Vector3(scale, scale, 1f);
                });
        }

        void SetIconScale(float scale)
        {
            if (iconMask == null)
                return;
            iconMask.localScale = new Vector3(scale, scale, 1f);
        }

        void SetStatusBounce(bool bounce)
        {
            _statusWantsBounce = bounce;
            if (!bounce)
            {
                StopStatusHop();
                return;
            }

            if (!isActiveAndEnabled || _statusBounce.IsActive())
                return;

            PlayStatusHop();
        }

        void PlayStatusHop()
        {
            var rect = StatusRect();
            if (rect == null)
            {
                Debug.LogError("DraftPick: assign draftStatusGroup on the prefab.", this);
                return;
            }

            if (!_statusRestCaptured)
            {
                _statusRestY = rect.anchoredPosition.y;
                _statusRestCaptured = true;
            }

            var hop = StatusHopSeconds();
            var gap = statusBounceInterval > 0f ? statusBounceInterval : 0f;
            var period = hop * 2f + gap;
            if (_statusBounce.IsActive())
                _statusBounce.Cancel();

            _statusBounce = LMotion.Create(0f, period, period)
                .WithEase(Ease.Linear)
                .WithLoops(-1, LoopType.Restart)
                .Bind(this, static (time, pick) => pick.ApplyStatusHop(time));
        }

        void ApplyStatusHop(float time)
        {
            var rect = StatusRect();
            if (rect == null)
                return;

            var hop = StatusHopSeconds();
            var pair = hop * 2f;
            float offset;
            float squash;
            if (time < hop)
            {
                offset = HopOffset(time, hop, statusBounceHeight);
                squash = FirstHopSquash(time / hop);
            }
            else if (time < pair)
            {
                offset = HopOffset(time - hop, hop, statusBounceHeight * SecondStatusBounceScale);
                squash = SecondHopSquash((time - hop) / hop);
            }
            else
            {
                offset = 0f;
                var recover = StatusRecoverSeconds;
                var t = Mathf.Clamp01((time - pair) / recover);
                var settle = t * t * (3f - 2f * t);
                squash = Mathf.Lerp(-SecondStatusBounceScale, 0f, settle);
            }

            ApplyStatusPose(rect, offset, squash);
        }

        void ApplyStatusPose(RectTransform rect, float offset, float squash)
        {
            var amount = squash * StatusSquashAmount;
            var scaleY = 1f + amount;
            var scaleX = 1f - amount * 0.7f;
            rect.localScale = new Vector3(scaleX, scaleY, 1f);

            var pos = rect.anchoredPosition;
            pos.y = _statusRestY + offset + rect.rect.yMin * (1f - scaleY);
            rect.anchoredPosition = pos;
        }

        float StatusHopSeconds()
        {
            return statusBounceSeconds > 0.01f ? statusBounceSeconds : 0.01f;
        }

        static float HopOffset(float time, float duration, float height)
        {
            var half = duration * 0.5f;
            if (time < half)
            {
                var t = time / half;
                return height * (1f - (1f - t) * (1f - t));
            }

            var down = (time - half) / half;
            return height * (1f - down * down);
        }

        static float FirstHopSquash(float u)
        {
            if (u < 0.5f)
            {
                var t = u / 0.5f;
                return 1f - (1f - t) * (1f - t);
            }

            var down = (u - 0.5f) / 0.5f;
            return Mathf.Lerp(1f, -1f, down * down);
        }

        static float SecondHopSquash(float u)
        {
            if (u < 0.5f)
            {
                var t = u / 0.5f;
                var blend = 1f - (1f - t) * (1f - t);
                return Mathf.Lerp(-1f, SecondStatusBounceScale, blend);
            }

            var down = (u - 0.5f) / 0.5f;
            return Mathf.Lerp(SecondStatusBounceScale, -SecondStatusBounceScale, down * down);
        }

        void StopStatusHop()
        {
            if (_statusBounce.IsActive())
                _statusBounce.Cancel();
            ResetStatusPosition();
        }

        void ResetStatusPosition()
        {
            if (!_statusRestCaptured)
                return;
            var rect = StatusRect();
            if (rect == null)
                return;
            rect.localScale = Vector3.one;
            var pos = rect.anchoredPosition;
            pos.y = _statusRestY;
            rect.anchoredPosition = pos;
        }

        RectTransform StatusRect()
        {
            if (draftStatusGroup == null)
                return null;
            return draftStatusGroup.transform as RectTransform;
        }

        float CardRestY()
        {
            return -selectedCardY;
        }

        float CardHoverY()
        {
            return selectedCardY;
        }

        void PlayPressElastic()
        {
            var press = ++_cardPress;
            var punch = ++_iconPunch;
            var rest = CardRestY();
            var past = rest - Mathf.Max(PressDepth, Mathf.Abs(selectedCardY) * PressDepthScale);
            var downSeconds = selectMoveSeconds;
            var upSeconds = selectMoveSeconds * UpMovePortion;
            TweenIcon(SelectedIconScale, downSeconds, null);
            MoveCard(past, downSeconds, () =>
            {
                if (press != _cardPress || punch != _iconPunch || !_selected)
                    return;
                PaintSelectionHighlight(true);
                ShowSelectedGroup(true, upSeconds);
                TweenIcon(HoverIconScale, upSeconds, () =>
                {
                    if (punch != _iconPunch || !_selected)
                        return;
                    _iconPunch = 0;
                });
                MoveCard(rest, upSeconds, null);
            });
        }

        void MoveCard(float targetY, float seconds, Action onComplete)
        {
            if (cardPanel is not RectTransform rect)
            {
                Debug.LogError("DraftPick: assign cardPanel on the prefab.", this);
                onComplete?.Invoke();
                return;
            }

            if (_cardMove.IsActive())
                _cardMove.Cancel();

            var pos = rect.anchoredPosition;
            if (seconds <= 0f || Mathf.Approximately(pos.y, targetY))
            {
                pos.y = targetY;
                rect.anchoredPosition = pos;
                onComplete?.Invoke();
                return;
            }

            _cardMove = LMotion.Create(pos.y, targetY, seconds)
                .WithEase(Ease.OutQuad)
                .WithOnComplete(() => onComplete?.Invoke())
                .Bind(rect, static (y, rt) =>
                {
                    if (rt == null)
                        return;
                    var p = rt.anchoredPosition;
                    p.y = y;
                    rt.anchoredPosition = p;
                });
        }

        void SetCardY(float y)
        {
            if (cardPanel is not RectTransform rect)
                return;
            var pos = rect.anchoredPosition;
            pos.y = y;
            rect.anchoredPosition = pos;
        }
    }
}
