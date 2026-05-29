// SPDX-License-Identifier: LGPL-3.0-or-later
// iDaVIE (immersive Data Visualisation Interactive Explorer)
// Copyright (C) 2024 IDIA, INAF-OACT — refactor skeleton, design-only.
//
// EnumString — cross-cutting forward-compatible enum-string parser
// (shared_interfaces.md §1.9). Lives in ST1 Kernel so every team's persistence
// capture-port DTOs can parse enum-as-string fields on Restore: a workspace saved
// by a newer build (carrying an unknown enum member) degrades gracefully to a
// fallback rather than throwing.
//
// Relocated here from ST4's InteractionStateDto to keep the dependency graph
// acyclic — ST5 and ST7 consume it, and an ST5 → ST4 reference would have cycled
// against the existing ST4 → ST5 edge (global_model.md §2).

namespace iDaVIE.Kernel.Contracts
{
    public static class EnumString
    {
        /// <summary>Returns <paramref name="fallback"/> when <paramref name="value"/>
        /// is null, empty, or not a known member of <typeparamref name="T"/>.</summary>
        public static T TryParseOrDefault<T>(string? value, T fallback) where T : struct, System.Enum
            => System.Enum.TryParse<T>(value, ignoreCase: false, out var result) ? result : fallback;
    }
}
