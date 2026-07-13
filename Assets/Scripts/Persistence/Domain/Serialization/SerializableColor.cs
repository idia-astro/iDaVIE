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
    /// Plain-C# replacement for UnityEngine.Color for use in persistence DTOs.
    /// </summary>
    public sealed class SerializableColor
    {
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }
        public float A { get; set; } = 1f;

        public SerializableColor() { }

        public SerializableColor(float r, float g, float b, float a = 1f)
        {
            R = r; G = g; B = b; A = a;
        }

        public static SerializableColor White => new SerializableColor(1f, 1f, 1f);
        public static SerializableColor Gray  => new SerializableColor(0.5f, 0.5f, 0.5f, 0.2f);

        public override string ToString() => $"rgba({R:F3}, {G:F3}, {B:F3}, {A:F3})";
    }
}
