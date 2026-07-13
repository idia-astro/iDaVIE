# Sub-Team 3: Rendering Engine — Requirements Document

**Date:** 20 May 2026 - Sprint 1  
**Authors:** Sub-Team 3 (Rendering)

## 1. Executive Summary

**Problem:** The VolumeDataSetRenderer is a 1,400-line monolith handling too many responsibilities: shader management, texture uploads, camera math, and foveated sampling. This prevents pipeline swapping and unit testing outside Unity.

**Solution:** Refactor into decoupled domain classes using abstract interfaces, isolating rendering math from engine wrappers.

## 2. System Invariants & Constraints

- **VR Performance:** Maintain 90 fps minimum on reference hardware to prevent motion sickness.
- **Texture Cap:** Total 3D texture memory must not exceed 4 GB (Unity limit).
- **Volume Budget:** Default volume limited to 368 MB.
- **Sampling:** Use nearest-neighbor filtering only (bilinear/trilinear distorts voxel data).
- **Foveated Rendering:** Eye-tracking based rendering must remain fully functional so the system can maintain 90 FPS at usable image quality.
- **Mask Modes:** Visual output for Apply, Inverse, and Isolate modes must match legacy system.

**Platform Constraints & Assumptions:**

- Target Unity 6 with managed C# (no native DLL expansion). Must support migration from SteamVR to Unity XR Toolkit and from Built-in Pipeline to URP/HDRP.
- This is a design-only refactor; no production code changes. Code examples are made but not implemented, CK metrics are estimates.
- Assumes functional eye-tracking hardware and pre-normalized voxel data (0.0-1.0 range) from Sub-Team 2.

## 3. Functional Requirements

- **FR01 Volume Visualization:** Ray-march through 3D texture, accumulating colour/opacity via active colour map.
- **FR02 Runtime Masking:** Support three mask modes (Apply, Inverse, Isolate) without pipeline restart.
- **FR03 Dynamic Colour Mapping:** Apply configurable colour map with single-frame visual updates.
- **FR04 Gaze-Contingent Sampling:** Adjust sample rate based on gaze direction (higher at focus, lower at periphery).
- **FR05 Cache Management:** Enforce 368 MB budget by evicting/reusing GPU texture slots.
- **FR06 Pipeline Isolation:** Core assemblies must not import URP/HDRP types directly or transitively.

## 4. Non-Functional Requirements

**CK Metric Targets (per domain class):**

- WMC ≤ 20, CBO ≤ 14, LCOM ≤ 0.5, DIT ≤ 4, RFC ≤ 50

**Design Standards:**

- Zero circular dependencies (verified via NDepend).
- All rendering logic must be unit-testable without Unity runtime.
- Adding new mask modes requires only a new IMaskMode implementation (no existing code changes).
- Switching URP to HDRP requires only replacing one adapter class.
- All internal APIs must be explicitly defined interfaces.

## 5. Integration Contracts

- **DEP01 Sub-Team 1 → 3:** Sub-Team 1 defines the kernel architecture, plug-in ABI contract, and layer dependency rules that all teams (including us) must follow.
- **DEP02 Sub-Team 2 → 3:** Sub-Team 2 provides volume data via RawVolumeData struct, specifying voxel layout, normalization ranges (0.0-1.0), and TextureFormat.
- **DEP03 Sub-Team 4 → 3:** Sub-Team 4 provides gaze tracking via IGazeProvider interface, exposing GazeDirection, GazeFocusPoint, and IsGazeAvailable status.
- **DEP04 Sub-Team 3 → 4:** Sub-Team 3 exposes camera state via VolumeCameraDriver, providing view/projection matrices and clipping planes for interaction calculations.

## 6. Future Scope

Architecture must support these without breaking changes:

- **Iso-contour Rendering:** IMaskMode must accept custom surface algorithms; IRenderPipeline must accommodate custom passes.
- **Multi-Cube Data:** VolumeTextureManager must handle multiple texture slots concurrently.
- **Time-Series Scrubbing:** Support texture data streaming/swapping per-frame without reallocation (same dimensions).
