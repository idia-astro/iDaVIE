// SPDX-License-Identifier: LGPL-3.0-or-later
// ShapeMenuController — VR menu shell holding ShapeGestureFSM. Replaces
// Assets/Scripts/Menu/ShapeMenuController.cs (170 LOC). Per brief §6.4 ST4
// owns the menu; the gesture FSM is the testable core.

using UnityEngine;

namespace iDaVIE.Interaction
{
    internal sealed class ShapeMenuController : MonoBehaviour
    {
        private ShapeGestureFSM _fsm;

        public void Inject(ShapeGestureFSM fsm)
            => throw new System.NotImplementedException();

        public void OnSelectCube()     => throw new System.NotImplementedException();
        public void OnSelectSphere()   => throw new System.NotImplementedException();
        public void OnSelectCylinder() => throw new System.NotImplementedException();
        public void OnSelectCuboid()   => throw new System.NotImplementedException();
        public void OnApplyShape()     => throw new System.NotImplementedException();
        public void OnCancelShape()    => throw new System.NotImplementedException();
    }
}
