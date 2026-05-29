// SPDX-License-Identifier: LGPL-3.0-or-later
// KeypadInputAdapter — ST4 Unity ACL MonoBehaviour for the VR numeric keypad.
// Replaces Assets/Scripts/UI/KeypadController.cs (92 LOC). Consumed by the
// moment-map menu and the threshold-entry path.

using System;
using UnityEngine;

namespace iDaVIE.Interaction
{
    internal sealed class KeypadInputAdapter : MonoBehaviour
    {
        public event Action<double> NumberEntered;
        public event Action         EntryCancelled;

        public void Show()  => throw new NotImplementedException();
        public void Hide()  => throw new NotImplementedException();
    }
}
