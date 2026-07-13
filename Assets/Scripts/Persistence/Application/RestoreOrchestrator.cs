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

using iDaVIE.Persistence.Application.Interfaces;
using iDaVIE.Persistence.Domain;

namespace iDaVIE.Persistence.Application
{
    /// <summary>
    /// Enforces the strict subsystem restore ordering required to avoid dependency violations:
    ///   1. Data I/O   — open FITS, validate SubsetBounds, init AST frames
    ///   2. Rendering  — apply RenderingState
    ///   3. Features   — restore identity fields, trigger async recomputation
    ///   4. Interaction — apply after dataset validated
    ///   5. GUI        — last (derives from domain state)
    ///
    /// All adapter Restore() methods must be called on the Unity main thread.
    /// </summary>
    public class RestoreOrchestrator
    {
        private readonly IDataIoStateAdapter      _dataIoAdapter;
        private readonly IRenderingStateAdapter   _renderingAdapter;
        private readonly IFeatureStateAdapter     _featureAdapter;
        private readonly IInteractionStateAdapter _interactionAdapter;
        private readonly IGuiStateAdapter         _guiAdapter;

        public RestoreOrchestrator(
            IDataIoStateAdapter      dataIoAdapter,
            IRenderingStateAdapter   renderingAdapter,
            IFeatureStateAdapter     featureAdapter,
            IInteractionStateAdapter interactionAdapter,
            IGuiStateAdapter         guiAdapter)
        {
            _dataIoAdapter      = dataIoAdapter;
            _renderingAdapter   = renderingAdapter;
            _featureAdapter     = featureAdapter;
            _interactionAdapter = interactionAdapter;
            _guiAdapter         = guiAdapter;
        }

        /// <summary>
        /// Executes the full restore sequence. Throws if a critical sub-restore fails.
        /// All steps must run on the Unity main thread.
        /// </summary>
        public void Restore(WorkspaceSnapshot snapshot)
        {
            _dataIoAdapter.Restore(snapshot.DataIo);
            _renderingAdapter.Restore(snapshot.Rendering);

            if (snapshot.Features != null)
                _featureAdapter.Restore(snapshot.Features);

            _interactionAdapter.Restore(snapshot.Interaction);

            if (snapshot.Gui != null)
                _guiAdapter.Restore(snapshot.Gui);
        }
    }
}
