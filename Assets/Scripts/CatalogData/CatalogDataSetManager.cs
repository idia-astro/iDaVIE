using UnityEngine;

namespace CatalogData
{
    public class CatalogDataSetManager : MonoBehaviour
    {
        public bool DisplaySingleDataSet;

        public CatalogDataSetRendererDelegate OnActiveDataSetChanged;

        public ColorMapDelegate OnColorMapChanged;

        public bool ProximinityOpacity = false;

        public CatalogDataSetRenderer ActiveDataSet
        {
            get
            {
                if (_dataSets == null || _activeDataSetIndex < 0 || _activeDataSetIndex >= _dataSets.Length)
                {
                    return null;
                }

                return _dataSets[_activeDataSetIndex];
            }
        }

        private CatalogDataSetRenderer[] _dataSets;

        private int _activeDataSetIndex = -1;

        private void Start()
        {
            _dataSets = GetComponentsInChildren<CatalogDataSetRenderer>();
            if (_dataSets == null || _dataSets.Length == 0)
            {
                _activeDataSetIndex = -1;
            }
            else
            {
                SelectSet(0);
            }
        }

        private void Update()
        {
            if (ProximinityOpacity)
            {
                float distance;
                GameObject catalogPlayer = GameObject.Find("VRCamera");
                Vector3 playerPosition = catalogPlayer.transform.position;
                CatalogDataSetRenderer[] catalogDataSetRenderers = GetComponentsInChildren<CatalogDataSetRenderer>();
                foreach (CatalogDataSetRenderer renderer in catalogDataSetRenderers)
                {
                    Vector3 rendererPosition = renderer.transform.position;
                    float rendererScale = Vector3.Magnitude(renderer.transform.localScale);
                    distance = Vector3.Distance(rendererPosition, playerPosition);
                    float initialOpacity = renderer.GetInitialOpacity();
                    renderer.SetOpacity(Mathf.Clamp(initialOpacity / Mathf.Pow(distance,2), 0.05f, initialOpacity));
                    //Debug.Log("initial op: " + initialOpacity);

                }
            }
        }

        public void SelectNextSet()
        {
            int newIndex = (_activeDataSetIndex + 1) % _dataSets.Length;
            SelectSet(newIndex);
        }

        public void SelectPreviousSet()
        {
            int newIndex = (_activeDataSetIndex + _dataSets.Length - 1) % _dataSets.Length;
            SelectSet(newIndex);
        }

        public void SelectSet(int index)
        {
            if (ActiveDataSet)
            {
                ActiveDataSet.OnColorMapChanged -= HandleColorMapChanged;
            }

            if (_dataSets != null && index >= 0 && index < _dataSets.Length)
            {
                _activeDataSetIndex = index;
                if (DisplaySingleDataSet)
                {
                    foreach (var dataSet in _dataSets)
                    {
                        dataSet.enabled = false;
                    }
                }

                _dataSets[_activeDataSetIndex].enabled = true;
                _dataSets[_activeDataSetIndex].OnColorMapChanged += HandleColorMapChanged;
                OnActiveDataSetChanged?.Invoke(_dataSets[_activeDataSetIndex]);
            }
        }

        public void ShiftColorMap(int delta)
        {
            if (ActiveDataSet != null)
            {
                ActiveDataSet.ShiftColorMap(delta);
            }
        }

        public string GetActiveSetName()
        {
            return ActiveDataSet.name;
        }

        public void SetActiveSetVisibility(bool visible)
        {
            ActiveDataSet.SetVisibility(visible);
        }

        private void HandleColorMapChanged(ColorMapEnum colorMap)
        {
            OnColorMapChanged?.Invoke(colorMap);
        }
    }
}