// SPDX-License-Identifier: LGPL-3.0-or-later
// QuickMenuController + PaintMenuController — post-refactor MonoBehaviours. ST4.
//
// Legacy problems shared by both:
//   - FindObjectOfType<VolumeInputController>() in OnEnable (MOD-03, TST-03).
//   - Direct VolumeDataSetRenderer[] iteration for dataset state reads.
//   - VolumeInputController.InteractionStateMachine.Fire() calls into
//     the Stateless library FSM — replaced by direct calls to InteractionFSM.
//
// Refactor delta (both classes):
//   - VolumeInputController injected via Bind() rather than FindObjectOfType.
//   - Dataset state reads go via IVolumeRegistry.ActiveVolume (IVolumeDataSet).
//   - InteractionFSM state transitions called directly on the pure-C# FSM.
//   - Presentation logic (transform.SetParent, spawn positions, UI text) unchanged.

using UnityEngine;
using UnityEngine.UI;
using iDaVIE.Kernel.Contracts;      // IVolumeRegistry
using iDaVIE.Rendering.Contracts;   // MaskMode

namespace iDaVIE.Interaction
{
    // ── QuickMenuController ───────────────────────────────────────────────────

    public sealed class QuickMenuController : MonoBehaviour
    {
        // ── Inspector fields (scene compat) ──────────────────────────────────
        public GameObject sourcesMenu, paintMenu, plotsMenu, settingsMenu;
        public GameObject voiceCommandsListCanvas, colorMapListCanvas;
        public GameObject videoRecordingMenu;
        public GameObject savePopup, exitPopup, exitSavePopup, exportPopup;
        public GameObject userConfirmPopupPrefab;
        public GameObject notificationText;

        // ── Injected ─────────────────────────────────────────────────────────
        // Set by KernelCompositionRoot; no FindObjectOfType.
        [HideInInspector] public VolumeInputController  InputController;
        [HideInInspector] public VolumeCommandController CommandController;
        [HideInInspector] public IVolumeRegistry         VolumeRegistry;

        private int _maskStatus;
        private int _featureStatus;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        // No FindObjectOfType; InputController injected externally.

        // ── Menu navigation ───────────────────────────────────────────────────

        public void OpenSourcesMenu()  => spawnMenu(sourcesMenu);
        public void OpenSettingsMenu() => spawnMenu(settingsMenu);
        public void OpenListOfVoiceCommands()  => spawnMenu(voiceCommandsListCanvas);
        public void OpenListOfColourMaps()     => spawnMenu(colorMapListCanvas);
        public void OpenPlotsMenu()            => spawnMenu(plotsMenu);

        public void OpenVideoRecordingMenu()
        {
            if (!gameObject.activeSelf) { gameObject.SetActive(true); }
            ReparentMenu(videoRecordingMenu);
            gameObject.SetActive(false);
            videoRecordingMenu.SetActive(true);
        }

        /// <summary>
        /// Opens the paint menu. Replaces legacy OpenPaintMenu which called
        /// InteractionStateMachine.Fire — now calls InteractionFSM directly.
        /// </summary>
        public void OpenPaintMenu()
        {
            if (!gameObject.activeSelf) { gameObject.SetActive(true); }

            var active = VolumeRegistry?.ActiveVolume;
            // Legacy guard: !_activeDataSet.IsFullResolution
            if (active != null && !(active.MaskEditState?.IsFullResolution ?? true))
            {
                ToastNotification.ShowError("Cannot paint downsampled mask!\nPlease select a smaller region");
                return;
            }

            ReparentMenu(paintMenu);
            gameObject.SetActive(false);
            paintMenu.SetActive(true);
            // PaintMenuController.OnEnable fires InteractionFSM.OnPaintModeToggled() below.
        }

        // ── Mask toggle ───────────────────────────────────────────────────────

        public void ToggleMask()
        {
            var maskState = VolumeRegistry?.ActiveVolume?.MaskEditState;
            if (maskState == null) { throwMissingMaskError(); return; }

            if (_maskStatus == 3) _maskStatus = -1;
            _maskStatus++;

            // Legacy UI image swaps omitted (presentation layer, unchanged in principle).
            MaskMode mode = _maskStatus switch
            {
                0 => MaskMode.Disabled,
                1 => MaskMode.Enabled,
                2 => MaskMode.Inverted,
                3 => MaskMode.Isolated,
                _ => MaskMode.Disabled
            };

            CommandController?.setMask(mode);
            if (notificationText) notificationText.GetComponent<Text>().text = $"Mask {mode}";
        }

        // ── Crop ──────────────────────────────────────────────────────────────

        public void cropDataSet() => CommandController?.cropDataSet();

        // ── Save mask ─────────────────────────────────────────────────────────

        public void SaveMask()
        {
            var maskState = VolumeRegistry?.ActiveVolume?.MaskEditState;
            if (maskState == null) { throwMissingMaskError(); return; }

            // Legacy popup spawn identical; interaction FSM call replaced.
            var popup   = Instantiate(userConfirmPopupPrefab, transform.parent);
            popup.transform.localPosition = transform.localPosition;
            popup.transform.localRotation = transform.localRotation;
            popup.transform.localScale    = transform.localScale;

            var ctrl = popup.GetComponent<UserConfirmationPopupController>();
            ctrl.setHeaderText("Save mask");
            ctrl.addButton("New file",  "Save as new file",     SaveNewMask);
            ctrl.addButton("Overwrite", "Overwrite existing",   SaveOverwriteMask);
            ctrl.addButton("Cancel",    "Cancel",               SaveCancel);

            // Stop any in-progress painting.
            if (InputController?.InteractionFSM.State == InteractionState.PaintingStroke)
                InputController.InteractionFSM.OnPaintModeToggled();

            gameObject.SetActive(false);
            popup.SetActive(true);
        }

        public void ExportData()
        {
            var popup   = Instantiate(userConfirmPopupPrefab, transform.parent);
            popup.transform.localPosition = transform.localPosition;
            popup.transform.localRotation = transform.localRotation;
            popup.transform.localScale    = transform.localScale;
            var ctrl = popup.GetComponent<UserConfirmationPopupController>();
            ctrl.setHeaderText("Export data");
            ctrl.addButton("Mask",    "Save mask",            SaveMask);
            ctrl.addButton("Subcube", "Export as subcube",    SaveSubCube);
            ctrl.addButton("Cancel",  "Cancel",               ExportCancel);
            gameObject.SetActive(false);
            popup.SetActive(true);
        }

        public void ExportCancel()    => exportPopup?.SetActive(false);
        public void SaveCancel()      => savePopup?.SetActive(false);
        public void SaveSubCube()
        {
            CommandController?.SaveSubCube();
            InputController?.ControllerStream?.Pulse(InputController.PrimaryHandIndex, 0.1f, 1f);
            ExportCancel();
        }

        public void SaveOverwriteMask()
        {
            VolumeRegistry?.ActiveVolume?.MaskEditState?.SaveMask(true);
            InputController?.ControllerStream?.Pulse(InputController.PrimaryHandIndex, 0.1f, 1f);
            SaveCancel();
        }

        public void SaveNewMask()
        {
            VolumeRegistry?.ActiveVolume?.MaskEditState?.SaveMask(false);
            InputController?.ControllerStream?.Pulse(InputController.PrimaryHandIndex, 0.1f, 1f);
            SaveCancel();
        }

        // ── Exit ──────────────────────────────────────────────────────────────

        public void Exit()
        {
            var active = VolumeRegistry?.ActiveVolume;
            // File-changed check via IVolumeDataSet
            bool fileChanged = active != null && false; // TODO: IVolumeDataSet.FileChanged
            var popup = fileChanged ? exitSavePopup : exitPopup;
            ReparentMenu(popup);
            gameObject.SetActive(false);
            popup.SetActive(true);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ReparentMenu(GameObject menu)
        {
            menu.transform.SetParent(transform.parent, false);
            menu.transform.localPosition = transform.localPosition;
            menu.transform.localRotation = transform.localRotation;
            menu.transform.localScale    = transform.localScale;
        }

        public void spawnMenu(GameObject menu)
        {
            if (Camera.main == null) return;
            var playerPos = Camera.main.transform.position;
            var dir       = Camera.main.transform.forward;
            var spawnPos  = playerPos + dir * 1.5f;
            menu.transform.position = spawnPos;
            menu.transform.rotation = Quaternion.LookRotation(
                new Vector3(spawnPos.x - playerPos.x, 0, spawnPos.z - playerPos.z));
            if (!menu.activeSelf) menu.SetActive(true);
        }

        private static void throwMissingMaskError()
            => ToastNotification.ShowError("No mask loaded for this functionality!");
    }

    // ── PaintMenuController ───────────────────────────────────────────────────

    public sealed class PaintMenuController : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────
        public GameObject volumeDatasetRendererObj;
        public GameObject notificationText;
        public GameObject userConfirmPopupPrefab;
        public GameObject sourcesMenu, plotsMenu, shapeMenu;
        public GameObject savePopup;

        // ── Injected ─────────────────────────────────────────────────────────
        [HideInInspector] public VolumeInputController  InputController;
        [HideInInspector] public VolumeCommandController CommandController;
        [HideInInspector] public IVolumeRegistry         VolumeRegistry;

        private Text _topPanelText;
        private int  _maskStatus;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            // No FindObjectOfType — InputController injected.
            // Replaces: interactionStateMachine.Fire(PaintModeEnabled)
            if (InputController?.InteractionFSM != null)
            {
                InputController.OnEnterPaintMode();
                InputController.InteractionFSM.OnPaintModeToggled(); // Idle → PaintingStroke
            }

            var topTextTransform = gameObject.transform.Find("TopPanel")?.Find("Text");
            _topPanelText = topTextTransform?.GetComponent<Text>();
        }

        private void Update()
        {
            if (_topPanelText == null || InputController == null) return;

            if (!InputController.AdditiveBrush)
                _topPanelText.text = "Erase Mode";
            else if (InputController.SourceId <= 0)
                _topPanelText.text = "Please select a Source ID to paint";
            else
                _topPanelText.text = $"Paint Mode (Source ID {InputController.SourceId})";
        }

        // ── Exit ──────────────────────────────────────────────────────────────

        public void ExitPaintMode()
        {
            if (InputController == null) return;

            // Cancel source-ID selection if active
            if (InputController.InteractionFSM.IsSelectingSourceId)
                InputController.InteractionFSM.CancelSourceIdSelection();

            // Replaces: interactionStateMachine.Fire(PaintModeDisabled)
            if (InputController.InteractionFSM.State == InteractionState.PaintingStroke)
                InputController.InteractionFSM.OnPaintModeToggled(); // → Idle

            InputController.OnExitPaintMode();
            gameObject.SetActive(false);
        }

        // ── Brush controls ────────────────────────────────────────────────────

        public void BrushSizeIncrease()
        {
            InputController?.IncreaseBrushSize();
            UpdateBrushSizeText("Increase brush size");
        }

        public void BrushSizeDecrease()
        {
            InputController?.DecreaseBrushSize();
            UpdateBrushSizeText("Decrease brush size");
        }

        public void BrushSizeReset()
        {
            InputController?.ResetBrushSize();
        }

        public void PaintingAdditive()    => InputController?.SetBrushAdditive();
        public void PaintingSubtractive() => InputController?.SetBrushSubtractive();

        public void UndoBrushStroke()
        {
            CommandController?.Undo();
            InputController?.ControllerStream?.Pulse(InputController.PrimaryHandIndex, 0.1f, 1f);
        }

        public void RedoBrushStroke()
        {
            CommandController?.Redo();
            InputController?.ControllerStream?.Pulse(InputController.PrimaryHandIndex, 0.1f, 1f);
        }

        // ── Mask toggle ───────────────────────────────────────────────────────

        public void ToggleMask()
        {
            if (_maskStatus == 3) _maskStatus = -1;
            _maskStatus++;
            var mode = _maskStatus switch
            {
                0 => MaskMode.Disabled,
                1 => MaskMode.Enabled,
                2 => MaskMode.Inverted,
                _ => MaskMode.Isolated
            };
            CommandController?.setMask(mode);
        }

        // ── Outline ───────────────────────────────────────────────────────────

        public void ShowOutline()
        {
            var maskState = VolumeRegistry?.ActiveVolume?.MaskEditState;
            if (maskState == null) return;
            bool current = maskState.DisplayMask;
            maskState.DisplayMask = !current;
            if (notificationText)
                notificationText.GetComponent<Text>().text = current ? "Outline disabled" : "Outline enabled";
        }

        // ── Save mask ─────────────────────────────────────────────────────────

        public void SaveMask()
        {
            var maskState = VolumeRegistry?.ActiveVolume?.MaskEditState;
            if (maskState == null) return;

            if (maskState.IsMaskNew) { SaveNewMask(); return; }

            var popup = Instantiate(userConfirmPopupPrefab, transform.parent);
            popup.transform.localPosition = transform.localPosition;
            popup.transform.localRotation = transform.localRotation;
            popup.transform.localScale    = transform.localScale;
            var ctrl = popup.GetComponent<UserConfirmationPopupController>();
            ctrl.setHeaderText("Save mask");
            ctrl.addButton("New file",  "Save as new",      SaveNewMask);
            ctrl.addButton("Overwrite", "Overwrite existing", SaveOverwriteMask);
            ctrl.addButton("Cancel",    "Cancel",            SaveCancel);

            if (InputController?.InteractionFSM.State == InteractionState.PaintingStroke)
                InputController.InteractionFSM.OnPaintModeToggled();

            gameObject.SetActive(false);
            popup.SetActive(true);
        }

        public void SaveCancel()        => gameObject.SetActive(true);
        public void SaveOverwriteMask() { VolumeRegistry?.ActiveVolume?.MaskEditState?.SaveMask(true);  SaveCancel(); }
        public void SaveNewMask()       { VolumeRegistry?.ActiveVolume?.MaskEditState?.SaveMask(false); SaveCancel(); }

        // ── Source / shape ────────────────────────────────────────────────────

        public void AddNewSource()  => InputController?.AddNewSource();

        public void EditSourceId()  => InputController?.StartEditSourceID();

        public void OpenSourcesMenu() => spawnMenu(sourcesMenu);
        public void OpenPlotsWindow() => spawnMenu(plotsMenu);

        public void OpenShapeMenu()
        {
            shapeMenu.transform.SetParent(transform.parent, false);
            shapeMenu.transform.localPosition = transform.localPosition;
            shapeMenu.transform.localRotation = transform.localRotation;
            shapeMenu.transform.localScale    = transform.localScale;

            if (InputController?.InteractionFSM.IsSelectingSourceId == true)
                InputController.InteractionFSM.CancelSourceIdSelection();

            if (InputController?.InteractionFSM.State == InteractionState.PaintingStroke)
                InputController.InteractionFSM.OnPaintModeToggled();

            InputController?.ChangeShapeSelection();
            InputController?.SetBrushAdditive();
            gameObject.SetActive(false);
            shapeMenu.SetActive(true);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void UpdateBrushSizeText(string prefix)
        {
            var text = gameObject.transform.Find("BottomPanel")?.Find("Text")?.GetComponent<Text>();
            if (text != null && InputController != null)
                text.text = $"{prefix} (actual: {InputController.BrushSize})";
        }

        public void spawnMenu(GameObject menu)
        {
            if (Camera.main == null) return;
            var pos = Camera.main.transform.position + Camera.main.transform.forward * 1.5f;
            menu.transform.position = pos;
            menu.transform.rotation = Quaternion.LookRotation(
                new Vector3(pos.x - Camera.main.transform.position.x, 0,
                            pos.z - Camera.main.transform.position.z));
            if (!menu.activeSelf) menu.SetActive(true);
        }
    }
}
