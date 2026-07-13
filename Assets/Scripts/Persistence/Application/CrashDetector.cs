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
using System.IO;

namespace iDaVIE.Persistence.Application
{
    /// <summary>
    /// Manages a session lock file to detect unclean shutdowns (crashes).
    ///
    /// Protocol:
    ///   On startup: check whether lock file exists.
    ///     Present → previous session crashed → trigger crash recovery.
    ///     Absent  → clean start.
    ///   Write lock file immediately after decision.
    ///   Delete lock file on clean shutdown (Dispose / explicit Release).
    /// </summary>
    public class CrashDetector : IDisposable
    {
        private readonly string _lockFilePath;
        private bool _lockHeld;

        public CrashDetector(string snapshotDirectory)
        {
            _lockFilePath = Path.Combine(snapshotDirectory, "session.lock");
        }

        /// <summary>
        /// Returns true when a previous session lock file is present (crash detected).
        /// Always writes a new lock file for this session after checking.
        /// </summary>
        public bool CheckAndAcquire()
        {
            bool crashed = File.Exists(_lockFilePath);

            int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
            File.WriteAllText(_lockFilePath,
                $"pid={pid}\nstarted={DateTime.UtcNow:O}");
            _lockHeld = true;

            return crashed;
        }

        /// <summary>Deletes the lock file — call on clean shutdown.</summary>
        public void Release()
        {
            if (!_lockHeld) return;
            if (File.Exists(_lockFilePath))
                File.Delete(_lockFilePath);
            _lockHeld = false;
        }

        public void Dispose() => Release();
    }
}
