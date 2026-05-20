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

namespace UI
{
    [RequireComponent(typeof(RectTransform))]
    public class UserSelectableItem : MonoBehaviour
    {
        private BoxCollider _boxCollider;
        private RectTransform _rectTransform;
        public bool IsDragHandle = false;
        public UserDraggableMenu MenuRoot;
        public bool isPressed;


        private void Start()
        {
            if (MenuRoot == null)
            {
                MenuRoot = GetComponentInParent<UserDraggableMenu>();
            }

            _rectTransform = GetComponent<RectTransform>();
            _boxCollider = GetComponent<BoxCollider>();
            if (_boxCollider == null)
            {
                _boxCollider = gameObject.AddComponent<BoxCollider>();
            }

            var rect = _rectTransform.rect;
            Vector2 pivot = _rectTransform.pivot;
            _boxCollider.size = new Vector3(rect.width, rect.height, 1f);
            _boxCollider.center = new Vector3(rect.width / 2 - rect.width * pivot.x, rect.height / 2 - rect.height * pivot.y, _rectTransform.anchoredPosition3D.z);
        }
    }
}