using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GemTD.UI
{
    /// <summary>
    /// Lays children out left to right and starts a new line when the next child does not fit.
    /// HorizontalLayoutGroup and GridLayoutGroup cannot do this: one never wraps, the other uses fixed cells.
    /// </summary>
    public sealed class TagFlowLayout : LayoutGroup
    {
        [SerializeField] float spacingX = 10f;
        [SerializeField] float spacingY = 4f;

        readonly List<float> _widths = new List<float>(8);
        readonly List<float> _heights = new List<float>(8);
        readonly List<int> _rowStart = new List<int>(4);
        readonly List<float> _rowWidth = new List<float>(4);
        readonly List<float> _rowHeight = new List<float>(4);

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            var widest = 0f;
            for (var i = 0; i < rectChildren.Count; i++)
            {
                var width = LayoutUtility.GetPreferredWidth(rectChildren[i]);
                if (width > widest)
                    widest = width;
            }

            var preferred = padding.horizontal + widest;
            SetLayoutInputForAxis(preferred, preferred, -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            BuildRows();
            float height = padding.vertical;
            for (var i = 0; i < _rowHeight.Count; i++)
            {
                if (i > 0)
                    height += spacingY;
                height += _rowHeight[i];
            }

            SetLayoutInputForAxis(height, height, -1, 1);
        }

        public override void SetLayoutHorizontal()
        {
            BuildRows();
            for (var row = 0; row < _rowStart.Count; row++)
            {
                var start = _rowStart[row];
                var end = NextRowStart(row);
                var x = GetStartOffset(0, _rowWidth[row]);
                for (var i = start; i < end; i++)
                {
                    SetChildAlongAxis(rectChildren[i], 0, x, _widths[i]);
                    x += _widths[i] + spacingX;
                }
            }
        }

        public override void SetLayoutVertical()
        {
            BuildRows();
            var total = 0f;
            for (var i = 0; i < _rowHeight.Count; i++)
            {
                if (i > 0)
                    total += spacingY;
                total += _rowHeight[i];
            }

            var y = GetStartOffset(1, total);
            for (var row = 0; row < _rowStart.Count; row++)
            {
                var start = _rowStart[row];
                var end = NextRowStart(row);
                for (var i = start; i < end; i++)
                    SetChildAlongAxis(rectChildren[i], 1, y, _heights[i]);
                y += _rowHeight[row] + spacingY;
            }
        }

        int NextRowStart(int row)
        {
            return row + 1 < _rowStart.Count ? _rowStart[row + 1] : rectChildren.Count;
        }

        void BuildRows()
        {
            base.CalculateLayoutInputHorizontal();
            _widths.Clear();
            _heights.Clear();
            _rowStart.Clear();
            _rowWidth.Clear();
            _rowHeight.Clear();

            var inner = rectTransform.rect.width - padding.horizontal;
            if (inner < 1f)
            {
                for (var i = 0; i < rectChildren.Count; i++)
                {
                    var preferred = LayoutUtility.GetPreferredWidth(rectChildren[i]);
                    if (preferred > inner)
                        inner = preferred;
                }
            }

            var cursor = 0f;
            var rowWidth = 0f;
            var rowHeight = 0f;
            var rowOpen = false;

            for (var i = 0; i < rectChildren.Count; i++)
            {
                var width = LayoutUtility.GetPreferredWidth(rectChildren[i]);
                var height = LayoutUtility.GetPreferredHeight(rectChildren[i]);
                _widths.Add(width);
                _heights.Add(height);

                if (rowOpen && cursor + spacingX + width > inner + 0.5f)
                {
                    _rowWidth.Add(rowWidth);
                    _rowHeight.Add(rowHeight);
                    cursor = 0f;
                    rowWidth = 0f;
                    rowHeight = 0f;
                    rowOpen = false;
                }

                if (!rowOpen)
                    _rowStart.Add(i);

                if (rowOpen)
                    cursor += spacingX;
                cursor += width;
                rowWidth = cursor;
                if (height > rowHeight)
                    rowHeight = height;
                rowOpen = true;
            }

            if (rowOpen)
            {
                _rowWidth.Add(rowWidth);
                _rowHeight.Add(rowHeight);
            }
        }
    }
}
