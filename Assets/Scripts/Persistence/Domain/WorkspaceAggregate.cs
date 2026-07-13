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
    /// Aggregate root that holds the live sub-DTOs during a session.
    /// </summary>
    public class WorkspaceAggregate
    {
        public DataIoStateDto      DataIo      { get; set; } = new DataIoStateDto();
        public RenderingStateDto   Rendering   { get; set; } = new RenderingStateDto();
        public FeatureStateDto     Features    { get; set; }
        public InteractionStateDto Interaction { get; set; } = new InteractionStateDto();
        public GuiStateDto         Gui         { get; set; }

        /// <summary>
        /// Infers the WorkspaceProfile from which sub-states are populated.
        /// </summary>
        public WorkspaceProfile InferProfile()
        {
            bool hasMask     = Rendering.Mask != null && Rendering.Mask.MaskMode != null && Rendering.Mask.MaskMode != "Disabled";
            bool hasFeatures = Features != null && Features.FeatureSets != null && Features.FeatureSets.Count > 0;
            bool hasFoveat   = Rendering.Foveation != null && Rendering.Foveation.FoveatedRendering == true;

            if (hasMask && hasFeatures) return WorkspaceProfile.FullWorkspace;
            if (hasFeatures)            return WorkspaceProfile.DataWithFeatures;
            if (hasMask)                return WorkspaceProfile.DataWithMask;
            return WorkspaceProfile.DataOnly;
        }

        /// <summary>
        /// Produces an immutable snapshot from the current aggregate state.
        /// </summary>
        public WorkspaceSnapshot Capture(WorkspaceProfile? profileOverride = null)
        {
            var profile = profileOverride ?? InferProfile();
            return new WorkspaceSnapshot
            {
                Metadata    = WorkspaceMetadata.Now(profile),
                DataIo      = DataIo,
                Rendering   = Rendering,
                Features    = Features,
                Interaction = Interaction,
                Gui         = Gui,
            };
        }
    }
}
