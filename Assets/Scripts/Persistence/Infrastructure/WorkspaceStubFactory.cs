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

using System;
using iDaVIE.Persistence.Domain;
using iDaVIE.Persistence.Domain.Dtos;

namespace iDaVIE.Persistence.Infrastructure
{
    /// <summary>
    /// Creates a minimal snapshot stub for the given session profile.
    /// Only the sub-sections required by the profile are allocated.
    /// </summary>
    public class WorkspaceStubFactory
    {
        public WorkspaceSnapshot CreateStub(WorkspaceProfile profile)
        {
            var metadata    = WorkspaceMetadata.Now(profile);
            var dataIo      = new DataIoStateDto();
            var interaction = new InteractionStateDto();

            switch (profile)
            {
                case WorkspaceProfile.DataOnly:
                    return new WorkspaceSnapshot
                    {
                        Metadata    = metadata,
                        DataIo      = dataIo,
                        Rendering   = new RenderingStateDto(),
                        Features    = null,
                        Interaction = interaction,
                        Gui         = null,
                    };

                case WorkspaceProfile.DataWithMask:
                    return new WorkspaceSnapshot
                    {
                        Metadata    = metadata,
                        DataIo      = dataIo,
                        Rendering   = new RenderingStateDto
                        {
                            Mask = new MaskStateDto { MaskMode = "Enabled" }
                        },
                        Features    = null,
                        Interaction = interaction,
                        Gui         = null,
                    };

                case WorkspaceProfile.DataWithFeatures:
                    return new WorkspaceSnapshot
                    {
                        Metadata    = metadata,
                        DataIo      = dataIo,
                        Rendering   = new RenderingStateDto(),
                        Features    = new FeatureStateDto(),
                        Interaction = interaction,
                        Gui         = new GuiStateDto(),
                    };

                case WorkspaceProfile.FullWorkspace:
                    return new WorkspaceSnapshot
                    {
                        Metadata    = metadata,
                        DataIo      = dataIo,
                        Rendering   = new RenderingStateDto
                        {
                            Mask       = new MaskStateDto { MaskMode = "Enabled" },
                            Foveation  = new FoveationStateDto(),
                            MomentMaps = new MomentMapStateDto(),
                        },
                        Features    = new FeatureStateDto(),
                        Interaction = interaction,
                        Gui         = new GuiStateDto(),
                    };

                default:
                    throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown WorkspaceProfile.");
            }
        }
    }
}
