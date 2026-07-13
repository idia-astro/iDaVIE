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

// =============================================================================
// AFTER — Interface: IRestFrequencyController (design-level worked example)
//
// Kernel boundary contract for spectral rest-frequency management.
// Implementors: RestFrequencyService.
// ISP compliance: 4 members + 1 event.
// =============================================================================

using System;
using System.Collections.Generic;

namespace VolumeData.Refactored
{
    /// <summary>
    /// Contract for managing the list of candidate rest frequencies used to
    /// convert between frequency and velocity spectral axes.
    /// <para>
    /// Extracted from the god-class because rest-frequency logic has no
    /// shared state with the shader material pipeline or voxel painting.
    /// The <see cref="FrequencyChanged"/> event lets the WCS / spectra panel
    /// subscribe without coupling to the rendering MonoBehaviour.
    /// </para>
    /// </summary>
    public interface IRestFrequencyController
    {
        /// <summary>Returns the named frequency catalogue built from the config file.</summary>
        IReadOnlyDictionary<string, double> GetAvailableFrequencies();

        /// <summary>Selects the frequency at position <paramref name="index"/> in the catalogue.</summary>
        void SetFrequencyByIndex(int index);

        /// <summary>Sets an arbitrary override frequency in GHz.</summary>
        void SetFrequencyOverride(double freqGHz);

        /// <summary>Reverts to the FITS header rest frequency.</summary>
        void ClearOverride();

        /// <summary>Raised whenever the active rest frequency changes.</summary>
        event Action<double> FrequencyChanged;
    }
}
