using System;
using Interaction.Interfaces;
using VolumeData;

namespace Interaction
{
    public sealed class VignetteController : IVignetteController
    {
        private readonly Func<VolumeDataSetRenderer[]> _getDataSets;
        private readonly float _fadeSpeed;
        private readonly bool _enabled;
        private float _currentIntensity;
        private float _targetIntensity;

        public bool IsEnabled => _enabled;

        public VignetteController(Func<VolumeDataSetRenderer[]> getDataSets, bool enabled, float maxIntensity, float fadeSpeed)
        {
            _getDataSets = getDataSets;
            _enabled = enabled;
            _targetIntensity = 0f;
            _currentIntensity = 0f;
            _fadeSpeed = fadeSpeed;
            if (!_enabled)
            {
                _targetIntensity = 0f;
                _currentIntensity = 0f;
            }
        }

        public void SetTarget(float intensity)
        {
            _targetIntensity = intensity;
        }

        public void Update(float deltaTime)
        {
            if (!_enabled)
            {
                return;
            }

            float requiredChange = _targetIntensity - _currentIntensity;
            if (Math.Abs(requiredChange) <= 1e-6f)
            {
                return;
            }

            float maxChange = Math.Sign(requiredChange) * deltaTime * _fadeSpeed;
            if (Math.Abs(maxChange) > Math.Abs(requiredChange))
            {
                maxChange = requiredChange;
            }

            _currentIntensity += maxChange;
            var dataSets = _getDataSets?.Invoke();
            if (dataSets == null)
            {
                return;
            }

            for (int i = 0; i < dataSets.Length; i++)
            {
                dataSets[i].VignetteIntensity = _currentIntensity;
            }
        }
    }
}
