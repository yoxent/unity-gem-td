using System.Collections.Generic;
using GemTD.Core;
using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Bloons-style placement ghost: low-opacity tower mesh + range indicator.
    /// Valid = cyan/teal tint, invalid = red tint (tower mesh; range keeps authored material).
    /// The shell is reused for the run; dynamic tower visuals are pooled by source prefab.
    /// </summary>
    public sealed class PlacementGhostView : MonoBehaviour
    {
        [Header("Ghost Visual")]
        [SerializeField] Transform towerVisualRoot;
        [SerializeField] Transform rangeDisc;
        [SerializeField] MeshRenderer rangeRenderer;
        [SerializeField] Color validTowerColor = new Color(0.2f, 0.8f, 0.95f, 0.6f);
        [SerializeField] Color invalidTowerColor = new Color(0.88f, 0.35f, 0.35f, 0.45f);
        [SerializeField] Color validRangeColor = new Color(0.25f, 0.55f, 0.85f, 0.22f);
        [SerializeField] Color invalidRangeColor = new Color(0.85f, 0.2f, 0.2f, 0.28f);

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
        TowerView _sourcePrefab;
        float _rangeWorld = 3f;
        float _rangeHeightScale = 0.02f;
        bool _rangeUsesAuthoredMaterial;
        bool _rangeInitialized;
        readonly Dictionary<TowerView, ViewObjectPool<Transform>> _towerVisualPools =
            new Dictionary<TowerView, ViewObjectPool<Transform>>(8);
        readonly HashSet<Transform> _preparedTowerVisuals = new HashSet<Transform>();
        ViewObjectPool<Transform> _activeTowerVisualPool;

        public bool IsVisible { get; private set; }

        public void EnsureBuilt(TowerView towerPrefab, GameObject rangeIndicatorPrefab = null)
        {
            if (!_rangeInitialized)
            {
                _rangeDisc = rangeDisc;
                _rangeRenderer = rangeRenderer;
                if (_rangeDisc == null)
                    BuildRangeIndicator(rangeIndicatorPrefab);
                else
                    ConfigureAuthoredRangeIndicator();
                _rangeInitialized = true;
            }

            if (_towerVisual != null && _sourcePrefab == towerPrefab)
            {
                if (_block == null)
                    _block = new MaterialPropertyBlock();
                return;
            }

            if (_towerVisual != null)
                ReleaseTowerVisual();

            _sourcePrefab = towerPrefab;
            _towerRenderers = null;

            if (towerPrefab != null)
            {
                var pool = GetOrCreateTowerVisualPool(towerPrefab);
                _activeTowerVisualPool = pool;
                _towerVisual = pool.Get();
                if (_preparedTowerVisuals.Add(_towerVisual))
                {
                    PrepareTowerVisual(_towerVisual);
                    _towerRenderers = _towerVisual.GetComponentsInChildren<MeshRenderer>(false);
                    ApplyTransparentMaterials();
                }
                else
                {
                    _towerRenderers = _towerVisual.GetComponentsInChildren<MeshRenderer>(false);
                }
            }
            else
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "GhostTowerFallback";
                var visualParent = towerVisualRoot != null ? towerVisualRoot : transform;
                cube.transform.SetParent(visualParent, false);
                cube.transform.localPosition = Vector3.zero;
                cube.transform.localScale = new Vector3(0.7f, 1.1f, 0.7f);
                StripColliders(cube);
                _towerVisual = cube.transform;
                TowerPadSnap.ApplyFootOnParentOrigin(_towerVisual);
                _towerRenderers = cube.GetComponentsInChildren<MeshRenderer>(true);
                ApplyTransparentMaterials();
            }

            _block ??= new MaterialPropertyBlock();
            SetVisible(false);
        }

        ViewObjectPool<Transform> GetOrCreateTowerVisualPool(TowerView prefab)
        {
            if (_towerVisualPools.TryGetValue(prefab, out var pool))
                return pool;

            var parent = towerVisualRoot != null ? towerVisualRoot : transform;
            pool = new ViewObjectPool<Transform>(prefab.transform, parent);
            _towerVisualPools.Add(prefab, pool);
            return pool;
        }

        void PrepareTowerVisual(Transform visual)
        {
            visual.name = "GhostTowerVisual";
            visual.localRotation = Quaternion.identity;

            var towerView = visual.GetComponent<TowerView>();
            HideOccupants(visual.gameObject);
            StripColliders(visual.gameObject);
            TowerPadSnap.ApplyFootOnParentOrigin(visual);
            if (towerView != null)
                towerView.enabled = false;
        }

        void ReleaseTowerVisual()
        {
            if (_activeTowerVisualPool != null)
                _activeTowerVisualPool.Release(_towerVisual);
            else
                DestroySafe(_towerVisual.gameObject);

            _activeTowerVisualPool = null;
            _towerVisual = null;
            _towerRenderers = null;
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

        void ConfigureAuthoredRangeIndicator()
        {
            _rangeHeightScale = _rangeDisc.localScale.y;
            if (_rangeHeightScale < 0.01f)
                _rangeHeightScale = 1f;
            _rangeUsesAuthoredMaterial = true;
            _rangeDisc.localPosition = RangeLocalPosition();
            if (_rangeRenderer == null)
                _rangeRenderer = _rangeDisc.GetComponentInChildren<MeshRenderer>(true);
            StripColliders(_rangeDisc.gameObject);
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
            var towerColor = valid ? validTowerColor : invalidTowerColor;

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

            var rangeColor = valid ? validRangeColor : invalidRangeColor;
            _rangeRenderer.GetPropertyBlock(_block);
            _block.SetColor("_BaseColor", rangeColor);
            _block.SetColor("_Color", rangeColor);
            _rangeRenderer.SetPropertyBlock(_block);
        }

        static void HideOccupants(GameObject visual)
        {
            var animatorView = visual.GetComponent<TowerAnimatorView>();
            if (animatorView != null && animatorView.OccupantRoot != null)
            {
                TowerPadSnap.UniformizeLocalScale(animatorView.OccupantRoot);
                animatorView.SetOccupantVisible(false);
                animatorView.enabled = false;
                return;
            }

            if (animatorView != null)
                animatorView.enabled = false;

            var animators = visual.GetComponentsInChildren<Animator>(true);
            for (var i = 0; i < animators.Length; i++)
            {
                var anim = animators[i];
                if (anim == null || anim.gameObject == visual)
                    continue;
                TowerPadSnap.UniformizeLocalScale(anim.transform);
                anim.gameObject.SetActive(false);
            }
        }

        static void StripColliders(GameObject root)
        {
            var cols = root.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null)
                    DestroySafe(cols[i]);
            }
        }

        static void DestroySafe(Object obj)
        {
            if (obj == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
        }
    }
}
