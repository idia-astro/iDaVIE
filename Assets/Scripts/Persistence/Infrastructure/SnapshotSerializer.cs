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
using System.Security.Cryptography;
using System.Text;
using iDaVIE.Persistence.Domain;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Converters;
using Valve.Newtonsoft.Json.Serialization;

namespace iDaVIE.Persistence.Infrastructure
{
    /// <summary>
    /// Serialises/deserialises WorkspaceSnapshot to/from JSON using Valve.Newtonsoft.Json
    /// (bundled with SteamVR). Uses CamelCase property names and StringEnumConverter.
    ///
    /// Checksum protocol:
    ///   Serialize → compute SHA-256 over body (with "checksum":null placeholder) →
    ///   inject result into Metadata.Checksum → re-serialize final form.
    ///   On deserialize: recompute checksum over body-minus-checksum → compare → reject on mismatch.
    /// </summary>
    public class SnapshotSerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting         = Formatting.Indented,
            NullValueHandling  = NullValueHandling.Ignore,
            Converters         = { new StringEnumConverter() },
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            ContractResolver   = new CamelCasePropertyNamesContractResolver(),
        };

        public string Serialize(WorkspaceSnapshot snapshot)
        {
            // Two-pass: first with null checksum to get stable body, then inject hash.
            var noChecksum = snapshot with
            {
                Metadata = snapshot.Metadata with { Checksum = null }
            };
            string body  = JsonConvert.SerializeObject(noChecksum, Settings);
            string hash  = ComputeChecksum(body);
            var withHash = snapshot with
            {
                Metadata = snapshot.Metadata with { Checksum = $"sha256:{hash}" }
            };
            return JsonConvert.SerializeObject(withHash, Settings);
        }

        public WorkspaceSnapshot Deserialize(string filePath)
        {
            string json = File.ReadAllText(filePath, Encoding.UTF8);

            var snapshot = JsonConvert.DeserializeObject<WorkspaceSnapshot>(json, Settings);
            if (snapshot == null)
                throw new InvalidDataException("Deserialised snapshot is null.");

            string storedChecksum = snapshot.Metadata.Checksum;
            var noChecksum = snapshot with
            {
                Metadata = snapshot.Metadata with { Checksum = null }
            };
            string body     = JsonConvert.SerializeObject(noChecksum, Settings);
            string expected = $"sha256:{ComputeChecksum(body)}";

            if (!string.Equals(storedChecksum, expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Checksum mismatch in '{filePath}': stored={storedChecksum}, expected={expected}");

            return snapshot;
        }

        private static string ComputeChecksum(string body)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(body);
            byte[] hash;
            using (var sha = SHA256.Create())
            {
                hash = sha.ComputeHash(bytes);
            }
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
