using UnityEngine;

namespace PointData
{
    public class PointDataSetManager : MonoBehaviour
    {
        public bool DisplaySingleDataSet;

        public PointDataSet ActiveDataSet
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

        private PointDataSet[] _dataSets;

        private int _activeDataSetIndex;

        private void Start()
        {
            _dataSets = GetComponentsInChildren<PointDataSet>();
            if (_dataSets == null || _dataSets.Length == 0)
            {
                _activeDataSetIndex = -1;
            }
            else
            {
                SelectSet(0);
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
            }
        }

        public void ShiftColorMap(int delta)
        {
            if (ActiveDataSet != null)
            {
                ActiveDataSet.ShiftColorMap(delta);
            }
        }
    }
}