using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GemTD.Gameplay.SkillLab
{
    public static class SkillLabWorldDrag
    {
        static readonly List<RaycastResult> UiHits = new List<RaycastResult>(8);

        public static bool IsPointerOverUi()
        {
            var es = EventSystem.current;
            if (es == null || Mouse.current == null)
                return false;

            var eventData = new PointerEventData(es)
            {
                position = Mouse.current.position.ReadValue()
            };
            UiHits.Clear();
            es.RaycastAll(eventData, UiHits);
            return UiHits.Count > 0;
        }

        public static bool TryGetGroundPoint(Camera cam, out Vector3 point)
        {
            point = Vector3.zero;
            if (cam == null || Mouse.current == null)
                return false;

            var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out var dist))
                return false;
            point = ray.GetPoint(dist);
            point.y = 0f;
            return true;
        }

        public static bool TryPick(
            Camera cam,
            Transform towerView,
            SkillLabDummyView[] dummyViews,
            out bool tower,
            out int dummyIndex)
        {
            tower = false;
            dummyIndex = -1;
            if (cam == null || Mouse.current == null)
                return false;

            var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, 500f))
                return false;

            if (towerView != null && (hit.transform == towerView || hit.transform.IsChildOf(towerView)))
            {
                tower = true;
                return true;
            }

            if (dummyViews == null)
                return false;

            for (var i = 0; i < dummyViews.Length; i++)
            {
                var view = dummyViews[i];
                if (view == null)
                    continue;
                if (hit.transform == view.transform || hit.transform.IsChildOf(view.transform))
                {
                    dummyIndex = view.Index;
                    return true;
                }
            }

            return false;
        }
    }
}
