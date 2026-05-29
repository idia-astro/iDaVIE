// SPDX-License-Identifier: LGPL-3.0-or-later
// IBrushStrokeHistory — ST2 cross-team contract. Realised by MaskEditService
// alongside IMaskMutationService. Owns the undo/redo stacks; the legacy
// BrushStrokeHistory / BrushStrokeRedoQueue lists on VolumeDataSet move here.

namespace iDaVIE.Data.Contracts
{
    public interface IBrushStrokeHistory
    {
        bool CanUndo { get; }
        bool CanRedo { get; }

        void Undo();
        void Redo();

        /// <summary>Clears both stacks (e.g. on dataset unload).</summary>
        void Clear();
    }
}
