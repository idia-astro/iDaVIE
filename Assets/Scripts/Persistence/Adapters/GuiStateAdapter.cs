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

// Unity-side adapter — only layer permitted to reference UnityEngine.
// Reads VolumeDataSetRenderer.CubeDepthAxis (line 127) only.
// Must be called on the Unity main thread.

using iDaVIE.Persistence.Application.Interfaces;
using iDaVIE.Persistence.Domain.Dtos;
using UnityEngine;
using VolumeData;

namespace iDaVIE.Persistence.Adapters
{
    /// <summary>
    /// Captures and restores the minimal GUI-only state (CubeDepthAxis).
    ///
    /// All other fields that appear in the GUI state contract (FileName, SelectedHdu,
    /// SubsetBounds, MaskFileName, trueBounds) are authoritative in DataIoStateDto and
    /// must not be duplicated here. trueBounds is always recomputed from the FITS header.
    ///
    /// GUI restore is last in the RestoreOrchestrator sequence — it derives all other
    /// values from domain state via MVVM bindings.
    /// </summary>
    public class GuiStateAdapter : IGuiStateAdapter
    {
        private readonly VolumeDataSetRenderer _renderer;

        public GuiStateAdapter(VolumeDataSetRenderer renderer)
        {
            _renderer = renderer;
        }

        public GuiStateDto? Capture()
        {
            if (!_renderer.started) return null;
            return new GuiStateDto { CubeDepthAxis = _renderer.CubeDepthAxis };
        }

        public void Restore(GuiStateDto dto)
        {
            _renderer.CubeDepthAxis = dto.CubeDepthAxis;
            Debug.Log($"[Persistence] GuiState restored: CubeDepthAxis={dto.CubeDepthAxis}");
        }
    }
}
