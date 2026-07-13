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

namespace iDaVIE.Persistence.Domain.Serialization
{
    /// <summary>
    /// Plain-C# replacement for UnityEngine.Vector3 for use in persistence DTOs.
    /// </summary>
    public sealed class SerializableVector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public SerializableVector3() { }

        public SerializableVector3(float x, float y, float z)
        {
            X = x; Y = y; Z = z;
        }

        public static SerializableVector3 One  => new SerializableVector3(1f, 1f, 1f);
        public static SerializableVector3 Zero => new SerializableVector3(0f, 0f, 0f);

        public bool HasNonPositiveComponent() => X <= 0f || Y <= 0f || Z <= 0f;

        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}
