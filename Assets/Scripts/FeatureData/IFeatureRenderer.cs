/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * Sub-team 5: Feature System and Domain Model
 *
 * IFeatureRenderer is the rendering boundary between the feature domain layer
 * (sub-team 5) and the GPU rendering layer (WP3). The domain defines the interface
 * and WP3 supplies the concrete FeatureSetRenderer, so FeatureVisualiser depends on
 * an abstraction it owns rather than on the MonoBehaviour that drives the
 * ComputeBuffer. It follows the same split as IFeaturePersistenceService (WP7) and
 * IFeatureTableLoader (WP2): the domain owns the contract, the other sub-team owns
 * the body.
 */

using DataFeatures;

namespace iDaVIE.Domain.Feature
{
    /// <summary>
    /// Rendering boundary for a single FeatureSet.
    /// Implemented by FeatureSetRenderer (WP3, the GPU rendering layer).
    /// Only FeatureVisualiser calls it; domain objects never do.
    /// </summary>
    public interface IFeatureRenderer
    {
        /// <summary>Adds a feature so it is included in the next draw call.</summary>
        void AddFeature(Feature feature);

        /// <summary>Removes a feature from the render list.</summary>
        void RemoveFeature(Feature feature);

        /// <summary>Clears all features from the render list.</summary>
        void ClearFeatures();

        /// <summary>
        /// Marks a feature as needing a vertex-buffer update on the next frame.
        /// Pass -1 to mark all features dirty.
        /// Intended to be subscribed directly to <see cref="FeatureSet.FeatureDirty"/>.
        /// </summary>
        void SetFeatureAsDirty(int index);

        /// <summary>
        /// Display colour for all features in this set.
        /// Uses the domain colour type so this interface has no Unity dependency;
        /// the implementing class converts it to its own internal colour type.
        /// </summary>
        FeatureColor FeatureColor { get; set; }
    }
}
