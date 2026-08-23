using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Bloons-style placement ghost: low-opacity tower mesh + range indicator.
    /// Valid = green tint, invalid = red tint (tower mesh; range keeps authored material).
    /// </summary>
    public sealed class PlacementGhostView : MonoBehaviour
    {
        static readonly Color ValidTower = new Color(0.42f, 0.79f, 0.47f, 0.45f);
        static readonly Color InvalidTower = new Color(0.88f, 0.35f, 0.35f, 0.45f);
        static readonly Color ValidRange = new Color(0.25f, 0.55f, 0.85f, 0.22f);
        static readonly Color InvalidRange = new Color(0.85f, 0.2f, 0.2f, 0.28f);

        /// <summary>
        /// Sit above greybox tile tops so a flat fallback disc is not z-fought / buried.
        /// Authored cylinder is centered at Y = prefab height (1) so its bottom sits on the cell plane.
        /// </summary>
        const float RangeDiscY = 0.45f;

        MeshRenderer[] _towerRenderers;
        MeshRenderer _rangeRenderer;
        MaterialPropertyBlock _block;
        Transform _rangeDisc;
        Transform _towerVisual;
        float _rangeWorld = 3f;
        float _rangeHeightScale = 0.02f;
        bool _rangeUsesAuthoredMaterial;

        public bool IsVisible { get; private set; }

        public void EnsureBuilt(TowerView towerPrefab, GameObject rangeIndicatorPrefab = null)
        {
            if (_towerRenderers != null && _towerRenderers.Length > 0)
                return;

            if (towerPrefab != null)
            {
                var visual = Instantiate(towerPrefab.gameObject, transform);
                visual.name = "GhostTowerVisual";
                visual.transform.localPosition = Vector3.up * 0.55f;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                _towerVisual = visual.transform;

                var towerView = visual.GetComponent<TowerView>();
                if (towerView != null)
                    Destroy(towerView);

                StripColliders(visual);
                _towerRenderers = visual.GetComponentsInChildren<MeshRenderer>(true);
            }
            else
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "GhostTowerFallback";
                cube.transform.SetParent(transform, false);
                cube.transform.localPosition = Vector3.up * 0.55f;
                cube.transform.localScale = new Vector3(0.7f, 1.1f, 0.7f);
                StripColliders(cube);
                _towerVisual = cube.transform;
                _towerRenderers = cube.GetComponentsInChildren<MeshRenderer>(true);
            }

            BuildRangeIndicator(rangeIndicatorPrefab);

            _block = new MaterialPropertyBlock();
            ApplyTransparentMaterials();
            SetVisible(false);
        }

        void BuildRangeIndicator(GameObject rangeIndicatorPrefab)
        {
            GameObject disc;
            if (rangeIndicatorPrefab != null)
            {
                disc = Instantiate(rangeIndicatorPrefab, transform);
                disc.name = "GhostRangeDisc";
                disc.transform.localRotation = Quaternion.identity;
                _rangeHeightScale = disc.transform.localScale.y;
                if (_rangeHeightScale < 0.01f)
                    _rangeHeightScale = 1f;
                _rangeUsesAuthoredMaterial = true;
            }
            else
            {
                disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "GhostRangeDisc";
                disc.transform.SetParent(transform, false);
                _rangeHeightScale = 0.02f;
                _rangeUsesAuthoredMaterial = false;
            }

            disc.transform.localPosition = RangeLocalPosition();
            StripColliders(disc);
            _rangeDisc = disc.transform;
            _rangeRenderer = disc.GetComponent<MeshRenderer>();
            if (_rangeRenderer == null)
                _rangeRenderer = disc.GetComponentInChildren<MeshRenderer>(true);
            if (_rangeRenderer == null)
                _rangeUsesAuthoredMaterial = false;
        }

        void ApplyTransparentMaterials()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                return;

            if (_towerRenderers != null)
            {
                for (var i = 0; i < _towerRenderers.Length; i++)
                {
                    var r = _towerRenderers[i];
                    if (r == null)
                        continue;
                    r.sharedMaterial = new Material(shader);
                }
            }

            if (_rangeRenderer != null && !_rangeUsesAuthoredMaterial)
                _rangeRenderer.sharedMaterial = new Material(shader);
        }

        public void SetRange(float rangeWorld)
        {
            _rangeWorld = rangeWorld > 0.1f ? rangeWorld : 0.1f;
            if (_rangeDisc == null)
                return;

            // Unity cylinder default diameter is 1, so xz scale = world diameter.
            var diameter = _rangeWorld * 2f;
            _rangeDisc.localScale = new Vector3(diameter, _rangeHeightScale, diameter);
            _rangeDisc.localPosition = RangeLocalPosition();
        }

        Vector3 RangeLocalPosition()
        {
            if (_rangeUsesAuthoredMaterial)
                return new Vector3(0f, _rangeHeightScale, 0f);
            return new Vector3(0f, RangeDiscY, 0f);
        }

        public void ShowAt(Vector3 cellWorldCenter, bool valid)
        {
            SetTowerVisualActive(true);
            SetRangeIndicatorActive(valid);
            transform.position = cellWorldCenter;
            ApplyTint(valid);
            SetVisible(true);
        }

        /// <summary>Range indicator only — used when Tower Details is open on a placed tower.</summary>
        public void ShowRangeOnlyAt(Vector3 cellWorldCenter)
        {
            SetTowerVisualActive(false);
            SetRangeIndicatorActive(true);
            transform.position = cellWorldCenter;
            ApplyTint(true);
            SetVisible(true);
        }

        void SetTowerVisualActive(bool active)
        {
            if (_towerVisual != null && _towerVisual.gameObject.activeSelf != active)
                _towerVisual.gameObject.SetActive(active);
        }

        void SetRangeIndicatorActive(bool active)
        {
            if (_rangeDisc != null && _rangeDisc.gameObject.activeSelf != active)
                _rangeDisc.gameObject.SetActive(active);
        }

        public void Hide() => SetVisible(false);

        void SetVisible(bool visible)
        {
            IsVisible = visible;
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        void ApplyTint(bool valid)
        {
            var towerColor = valid ? ValidTower : InvalidTower;

            if (_towerRenderers != null)
            {
                for (var i = 0; i < _towerRenderers.Length; i++)
                {
                    var r = _towerRenderers[i];
                    if (r == null)
                        continue;
                    r.GetPropertyBlock(_block);
                    _block.SetColor("_BaseColor", towerColor);
                    _block.SetColor("_Color", towerColor);
                    r.SetPropertyBlock(_block);
                }
            }

            if (_rangeRenderer == null || _rangeUsesAuthoredMaterial)
                return;

            var rangeColor = valid ? ValidRange : InvalidRange;
            _rangeRenderer.GetPropertyBlock(_block);
            _block.SetColor("_BaseColor", rangeColor);
            _block.SetColor("_Color", rangeColor);
            _rangeRenderer.SetPropertyBlock(_block);
        }

        static void StripColliders(GameObject root)
        {
            var cols = root.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null)
                    Destroy(cols[i]);
            }
        }
    }
}
