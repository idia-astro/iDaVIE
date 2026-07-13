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

using iDaVIE.Persistence.Domain.Dtos;

namespace iDaVIE.Persistence.Domain
{
    /// <summary>
    /// Immutable point-in-time value object representing a complete workspace state.
    /// Declared as a non-positional record so Newtonsoft.Json can deserialise via the
    /// parameterless constructor + property setters, while still supporting `with` expressions.
    /// </summary>
    public sealed record WorkspaceSnapshot
    {
        public WorkspaceMetadata   Metadata    { get; set; } = new WorkspaceMetadata { Profile = WorkspaceProfile.DataOnly };
        public DataIoStateDto      DataIo      { get; set; } = new DataIoStateDto();
        public RenderingStateDto   Rendering   { get; set; } = new RenderingStateDto();
        public FeatureStateDto     Features    { get; set; }
        public InteractionStateDto Interaction { get; set; } = new InteractionStateDto();
        public GuiStateDto         Gui         { get; set; }
    }
}
