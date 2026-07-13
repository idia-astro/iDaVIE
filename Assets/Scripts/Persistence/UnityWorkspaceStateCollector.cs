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

namespace iDaVIE.Persistence
{
    /// <summary>
    /// Aggregates all five adapter captures into a single WorkspaceAggregate.
    /// Must be called on the Unity main thread.
    /// </summary>
    public class UnityWorkspaceStateCollector : IWorkspaceStateCollector
    {
        private readonly IDataIoStateAdapter      _dataIo;
        private readonly IRenderingStateAdapter   _rendering;
        private readonly IFeatureStateAdapter     _feature;
        private readonly IInteractionStateAdapter _interaction;
        private readonly IGuiStateAdapter         _gui;

        public UnityWorkspaceStateCollector(
            IDataIoStateAdapter      dataIo,
            IRenderingStateAdapter   rendering,
            IFeatureStateAdapter     feature,
            IInteractionStateAdapter interaction,
            IGuiStateAdapter         gui)
        {
            _dataIo      = dataIo;
            _rendering   = rendering;
            _feature     = feature;
            _interaction = interaction;
            _gui         = gui;
        }

        public WorkspaceAggregate Collect()
        {
            return new WorkspaceAggregate
            {
                DataIo      = _dataIo.Capture(),
                Rendering   = _rendering.Capture(),
                Features    = _feature.Capture(),
                Interaction = _interaction.Capture(),
                Gui         = _gui.Capture(),
            };
        }
    }
}
