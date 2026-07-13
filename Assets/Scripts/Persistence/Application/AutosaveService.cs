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
using System.Threading;

namespace iDaVIE.Persistence.Application
{
    /// <summary>
    /// Fires a save cycle on a System.Threading.Timer every intervalSeconds seconds.
    /// Fully Unity-independent. If a save is already running when the timer fires, that cycle is silently skipped.
    /// A user-triggered save should call TriggerNow() which also resets the timer.
    /// </summary>
    public sealed class AutosaveService : IDisposable
    {
        private readonly SaveWorkspaceUseCase _saveUseCase;
        private readonly int                 _intervalMs;
        private Timer                        _timer;
        private bool                         _disposed;

        public event Action<bool> SaveCompleted; // arg: whether save was skipped

        public AutosaveService(SaveWorkspaceUseCase saveUseCase, int intervalSeconds = 20)
        {
            _saveUseCase = saveUseCase;
            _intervalMs  = intervalSeconds * 1000;
        }

        public void Start()
        {
            if (_timer != null) _timer.Dispose();
            _timer = new Timer(OnTimerFired, null, _intervalMs, _intervalMs);
        }

        public void Stop()
        {
            if (_timer != null)
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Triggers an immediate save and resets the periodic timer.
        /// Call from Unity main thread (Save button handler).
        /// </summary>
        public void TriggerNow()
        {
            bool saved = _saveUseCase.Execute();
            if (SaveCompleted != null) SaveCompleted.Invoke(saved);
            if (_timer != null) _timer.Change(_intervalMs, _intervalMs);
        }

        private void OnTimerFired(object state)
        {
            bool saved = _saveUseCase.Execute();
            if (SaveCompleted != null) SaveCompleted.Invoke(saved);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}
