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
 */

namespace iDaVIE.Persistence.Domain
{
    /// <summary>
    /// Describes which sub-systems are active in the current session.
    /// Inferred at save time; drives WorkspaceStubFactory to produce the minimal stub.
    /// </summary>
    public enum WorkspaceProfile
    {
        /// <summary>FITS cube loaded, no mask, no features.</summary>
        DataOnly,

        /// <summary>FITS cube + mask loaded.</summary>
        DataWithMask,

        /// <summary>FITS cube + feature sets (no mask rendering).</summary>
        DataWithFeatures,

        /// <summary>Full session: FITS cube, mask, features, foveation.</summary>
        FullWorkspace,
    }
}
