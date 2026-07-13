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
using iDaVIE.Persistence.Application.Interfaces;
using iDaVIE.Persistence.Domain;
using iDaVIE.Persistence.Infrastructure;

namespace iDaVIE.Persistence.Application
{
    /// <summary>
    /// Orchestrates a complete save cycle: collect → stub → validate → serialize → ring-push.
    /// Called by AutosaveService (timer) and by the UI Save button.
    /// </summary>
    public class SaveWorkspaceUseCase
    {
        private readonly Func<IWorkspaceStateCollector> _collectorFactory;
        private readonly WorkspaceStubFactory           _stubFactory;
        private readonly WorkspaceValidator             _validator;
        private readonly SnapshotSerializer             _serializer;
        private readonly SnapshotRing                   _ring;

        private volatile bool _saveInProgress;

        public SaveWorkspaceUseCase(
            Func<IWorkspaceStateCollector> collectorFactory,
            WorkspaceStubFactory           stubFactory,
            WorkspaceValidator             validator,
            SnapshotSerializer             serializer,
            SnapshotRing                   ring)
        {
            _collectorFactory = collectorFactory;
            _stubFactory      = stubFactory;
            _validator        = validator;
            _serializer       = serializer;
            _ring             = ring;
        }

        /// <summary>
        /// Executes one save cycle. Returns false (and does nothing) if a save is already running.
        /// </summary>
        public bool Execute()
        {
            if (_saveInProgress) return false;
            _saveInProgress = true;
            try
            {
                return RunSave();
            }
            finally
            {
                _saveInProgress = false;
            }
        }

        private bool RunSave()
        {
            var collector = _collectorFactory();
            if (collector == null)
            {
                UnityEngine.Debug.Log("[Persistence] Save skipped — no dataset loaded.");
                return false;
            }

            var aggregate = collector.Collect();
            var profile   = aggregate.InferProfile();
            var snapshot  = _stubFactory.CreateStub(profile) with
            {
                Metadata    = WorkspaceMetadata.Now(profile),
                DataIo      = aggregate.DataIo,
                Rendering   = aggregate.Rendering,
                Features    = aggregate.Features,
                Interaction = aggregate.Interaction,
                Gui         = aggregate.Gui,
            };

            var validationResult = _validator.Validate(snapshot);
            if (validationResult.HasWarnings)
            {
                snapshot = snapshot with
                {
                    Metadata = snapshot.Metadata with { HasValidationWarnings = true }
                };
            }

            _ring.Push(snapshot, _serializer);
            return true;
        }
    }
}
