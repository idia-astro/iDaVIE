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
 *
 * Additional information and disclaimers regarding liability and third-party
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */

namespace DataFeatures
{
    /// <summary>
    /// Unity-free 3D float vector used in the domain layer.
    /// Unity-layer code converts to/from UnityEngine.Vector3 at the boundary.
    /// </summary>
    public readonly struct Vec3
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        // Lowercase aliases so Unity-layer code using .x/.y/.z compiles without changes.
        public float x => X;
        public float y => Y;
        public float z => Z;

        public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }

        public static Vec3 Zero => new Vec3(0f, 0f, 0f);
        public static Vec3 One  => new Vec3(1f, 1f, 1f);

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator -(Vec3 v)          => new Vec3(-v.X, -v.Y, -v.Z);
        public static Vec3 operator *(Vec3 v, float s) => new Vec3(v.X * s, v.Y * s, v.Z * s);
        public static Vec3 operator *(float s, Vec3 v) => new Vec3(v.X * s, v.Y * s, v.Z * s);
        public static Vec3 operator /(Vec3 v, float s) => new Vec3(v.X / s, v.Y / s, v.Z / s);

        public static Vec3 Min(Vec3 a, Vec3 b) => new Vec3(
            a.X < b.X ? a.X : b.X,
            a.Y < b.Y ? a.Y : b.Y,
            a.Z < b.Z ? a.Z : b.Z);

        public static Vec3 Max(Vec3 a, Vec3 b) => new Vec3(
            a.X > b.X ? a.X : b.X,
            a.Y > b.Y ? a.Y : b.Y,
            a.Z > b.Z ? a.Z : b.Z);

        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}
