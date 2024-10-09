/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms 
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with 
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party 
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */
using UnityEngine;

namespace DataFeatures
{
    public class FeatureAnchor : MonoBehaviour
    {
        private Material _material;
        private static readonly Color DefaultColor = Color.red;
        private static readonly Color HoverColor = new Color(0, 0.8f, 0.4f);
        private static readonly int EmissionProperty = Shader.PropertyToID("_EmissionColor");

        void Start()
        {
            _material = GetComponent<Renderer>().material;
            _material.EnableKeyword("_EMISSION");
            _material.SetColor(EmissionProperty, DefaultColor);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("cursor"))
            {
                var featureSetManager = GetComponentInParent<FeatureSetManager>();
                var inputController = FindObjectOfType<VolumeInputController>();
                _material.SetColor(EmissionProperty, HoverColor);
                inputController?.SetHoveredFeature(featureSetManager, this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("cursor"))
            {
                var featureSetManager = GetComponentInParent<FeatureSetManager>();
                var inputController = FindObjectOfType<VolumeInputController>();
                _material.SetColor(EmissionProperty, DefaultColor);
                inputController?.ClearHoveredFeature(featureSetManager, this);
            } 
        }
    }
}