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
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

public class VRKeyboard_Text : MonoBehaviour
{
    [HideInInspector] public bool selectionActive = false;
    [HideInInspector] public int charsPerRow = 28;
    [HideInInspector] public int position = 0;
    [HideInInspector] public int columnPosition;

    private KeyboardManager vrKeyboard = null;
    private InputField inputField = null;
    private readonly SteamVR_Action_Boolean _trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("InteractUI");

    private float waitTime = 0.2f;
    private float timer = 0.0f;
    // private OVRInput.RawButton lastPressed;

    private float caretShift = 1.0f;
    private bool isShifted = false;

    private void Start()
    {
        //it must never be 0, since it represents the max length of rows in the input field
        if (charsPerRow == 0)
        {
            charsPerRow = 28;
        }

        inputField = GetComponent<InputField>();
        vrKeyboard = GameObject.FindGameObjectWithTag("VRKeyboard").GetComponent<KeyboardManager>();
    }

    public void Update()
    {
        position = inputField.caretPosition;
        if (inputField.isFocused && _trigger.stateUp)
        {
            selectionActive = !selectionActive;
        }
        /*
         * MOVE CARET LEFT OR RIGHT
        else if (inputField.isFocused && OVRInput.Get(OVRInput.RawButton.RThumbstickLeft))
        {
            if (!isShifted)
            {
                StartCoroutine("accelerateCaret");
                isShifted = true;
            }
            if (lastPressed.Equals(OVRInput.RawButton.RThumbstickLeft))
            {
                timer += Time.deltaTime;
                if (timer > waitTime)
                {
                    timer = 0.0f;
                    if (selectionActive && inputField.selectionFocusPosition > 0)
                    {
                        inputField.selectionFocusPosition -= (int)caretShift;
                    }
                    else if (inputField.caretPosition > 0)
                    {
                        inputField.caretPosition -= (int)caretShift;
                    }
                    inputField.ForceLabelUpdate();
                }
            }
            else
            {
                timer = 0.0f;
                lastPressed = OVRInput.RawButton.RThumbstickLeft;
            }

        }
        else if (inputField.isFocused && OVRInput.Get(OVRInput.RawButton.RThumbstickRight))
        {
            if (!isShifted)
            {
                StartCoroutine("accelerateCaret");
                isShifted = true;
            }
            if (lastPressed.Equals(OVRInput.RawButton.RThumbstickRight))
            {
                timer += Time.deltaTime;
                if (timer > waitTime)
                {
                    timer = 0.0f;
                    if (selectionActive && inputField.selectionFocusPosition < inputField.text.Length)
                    {
                        inputField.selectionFocusPosition += (int)caretShift;
                    }
                    else if (inputField.caretPosition < inputField.text.Length)
                    {
                        inputField.caretPosition += (int)caretShift;
                    }
                    inputField.ForceLabelUpdate();
                }
            }
            else
            {
                timer = 0.0f;
                lastPressed = OVRInput.RawButton.RThumbstickRight;
            }
        }
        */
    }

    /*
    private IEnumerator accelerateCaret()
    {
        while (OVRInput.Get(OVRInput.RawButton.RThumbstickRight) || OVRInput.Get(OVRInput.RawButton.RThumbstickLeft))
        {
            caretShift *= 1.1f;
            yield return new WaitForSeconds(.1f);
        }
        caretShift = 1.0f;
        isShifted = false;
    }
    */

    private int previousRowIndex(string text, int caretPosition)
    {
        return caretPosition;
    }

    public void OnDeselect()
    {
        vrKeyboard.disable();

        inputField.DeactivateInputField();
    }

    public void OnSelect()
    {
        inputField = GetComponent<InputField>();
        if (vrKeyboard == null)
            vrKeyboard = GameObject.FindGameObjectWithTag("VRKeyboard").GetComponent<KeyboardManager>();

        vrKeyboard.enable(this);

        inputField.Select();
        inputField.ActivateInputField();
    }

    public void resetText()
    {
        inputField.text = "";
        inputField.ForceLabelUpdate();
    }

    public void setText(string text)
    {
        this.inputField.text = text;
    }

    public string getText()
    {
        return inputField.text;
    }

    public void GoOnLastChar()
    {
        StartCoroutine("SelectInputField");
    }

    IEnumerator SelectInputField()
    {
        yield return new WaitForEndOfFrame();
        inputField.caretPosition = inputField.text.Length;
    }

    public InputField GetInputField()
    {
        return inputField;
    }
}
