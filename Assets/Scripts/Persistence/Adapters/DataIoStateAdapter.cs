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
// Reads from VolumeDataSet (via VolumeDataSetRenderer) and AstTool attribute queries.
// Must be called on the Unity main thread.

using iDaVIE.Persistence.Application.Interfaces;
using iDaVIE.Persistence.Domain.Dtos;
using UnityEngine;
using VolumeData;

namespace iDaVIE.Persistence.Adapters
{
    /// <summary>
    /// Captures and restores Data I/O state (FITS file paths, HDU selection,
    /// subset bounds, and all spectral transform fields).
    ///
    /// NOT captured: IntPtr handles (FitsData, FitsHeader, AstFrameSet, AstAltSpecSet) —
    /// these are runtime-only and are rebuilt from the FITS header during restore.
    ///
    /// Restore ordering (mandatory):
    ///   1. Restore FileName, SelectedHdu, SubsetBounds, MaskFileName
    ///   2. Open FITS file, validate SubsetBounds against trueBounds
    ///   3. FitsReader.FitsCreateHdrPtrForAst() → AST header
    ///   4. AstTool.InitAstFrameSet() → primary spectral frame
    ///   5. Restore PrimarySpectralSystem, StandardOfRest
    ///   6. Restore AlternativeSpectralTarget, AlternativeSpectralUnit
    ///   7. CreateAltSpecFrame() → rebuild alt AST frame
    ///   8. Restore + validate TransformedSpectralValue
    /// </summary>
    public class DataIoStateAdapter : IDataIoStateAdapter
    {
        private readonly VolumeDataSetRenderer _renderer;

        public DataIoStateAdapter(VolumeDataSetRenderer renderer)
        {
            _renderer = renderer;
        }

        public DataIoStateDto Capture()
        {
            var dataset = _renderer.Data;   // VolumeDataSetRenderer.Data (line 226)
            if (dataset == null)
            {
                Debug.LogWarning("[Persistence] DataIoStateAdapter.Capture(): no dataset loaded.");
                return new DataIoStateDto();
            }

            // Read current spectral transform values from AST frame attributes
            string primarySystem = dataset.HasAstAttribute("System(3)")
                ? dataset.GetAstAttribute("System(3)") : "";
            string altSystem = dataset.GetAltSpecSystem();
            string altUnit   = dataset.GetAxisUnit(3);
            string stdOfRest = dataset.GetStdOfRest();

            return new DataIoStateDto
            {
                FileName                  = dataset.FileName,
                MaskFileName              = _renderer.MaskFileName,
                SubsetBounds              = dataset.subsetBounds != null
                                                ? (int[])dataset.subsetBounds.Clone()
                                                : null,
                SelectedHdu               = _renderer.SelectedHdu,
                Index2                    = null,   // TODO: expose from VolumeDataSet when 4D support added
                SliceDim                  = null,
                PrimarySpectralSystem     = primarySystem,
                AlternativeSpectralTarget = altSystem,
                AlternativeSpectralUnit   = altUnit,
                StandardOfRest            = stdOfRest,
                TransformedSpectralValue  = 0.0,    // Captured from CubeSlice conversion; TODO: wire up
            };
        }

        public void Restore(DataIoStateDto dto)
        {
            // Set fields on renderer for the next _startFunc() call
            _renderer.FileName    = dto.FileName ?? "";
            _renderer.MaskFileName = dto.MaskFileName ?? "";
            _renderer.SelectedHdu  = dto.SelectedHdu;

            if (dto.SubsetBounds != null && dto.SubsetBounds.Length == 6)
                _renderer.subsetBounds = (int[])dto.SubsetBounds.Clone();

            // Spectral transform fields are re-applied after _startFunc() opens the FITS file
            // and rebuilds AST frames (see RestoreOrchestrator ordering).
            Debug.Log($"[Persistence] DataIo restore queued: {dto.FileName}, HDU={dto.SelectedHdu}");
        }
    }
}
