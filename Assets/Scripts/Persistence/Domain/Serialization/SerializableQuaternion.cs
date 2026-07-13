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

namespace iDaVIE.Persistence.Domain.Serialization
{
    /// <summary>
    /// Plain-C# replacement for UnityEngine.Quaternion for use in persistence DTOs.
    /// </summary>
    public sealed class SerializableQuaternion
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        public SerializableQuaternion() { W = 1f; }

        public SerializableQuaternion(float x, float y, float z, float w)
        {
            X = x; Y = y; Z = z; W = w;
        }

        public static SerializableQuaternion Identity => new SerializableQuaternion(0f, 0f, 0f, 1f);

        /// <summary>Returns true if the quaternion magnitude is not approximately 1.</summary>
        public bool IsNotNormalised()
        {
            float mag = MathF.Sqrt(X * X + Y * Y + Z * Z + W * W);
            return MathF.Abs(mag - 1f) > 0.001f;
        }

        /// <summary>Returns a normalised copy, or Identity if the quaternion is zero-length.</summary>
        public SerializableQuaternion Normalised()
        {
            float mag = MathF.Sqrt(X * X + Y * Y + Z * Z + W * W);
            if (mag < 1e-6f) return Identity;
            return new SerializableQuaternion(X / mag, Y / mag, Z / mag, W / mag);
        }

        public override string ToString() => $"({X}, {Y}, {Z}, {W})";
    }
}
