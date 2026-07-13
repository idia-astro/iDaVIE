/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * FeatureSystemPort.cs
 * Implements IFeatureSystemPort by delegating to FeatureSetService (use-case layer)
 * and FeatureCatalog (registry). This is the only class Team 4 needs to instantiate.
 */

using System;
using System.Collections.Generic;
using DataFeatures;
using iDaVIE.Domain.Feature;

namespace iDaVIE.Application.Feature
{
    public sealed class FeatureSystemPort : IFeatureSystemPort
    {
        private readonly FeatureSetService _service;
        private readonly FeatureCatalog    _catalog;

        // Active paint strokes keyed by caller-assigned stroke ID.
        private readonly Dictionary<string, PaintStrokeState> _activeStrokes =
            new Dictionary<string, PaintStrokeState>();

        private struct PaintStrokeState
        {
            public Vec3 Min;
            public Vec3 Max;
            public bool HasPoints;
        }

        public FeatureSystemPort(FeatureSetService service, FeatureCatalog catalog)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public string CreateSelectionFeature(SelectionRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var feature = _service.CreateSelectionFeature(request.BoundsMin, request.BoundsMax);
            return feature.Id.ToString();
        }

        public void UpdateFeatureRegion(RegionEditRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            FindByStringId(request.FeatureId)?.SetBounds(request.NewBoundsMin, request.NewBoundsMax);
        }

        public void BeginPaintStroke(PaintStrokeRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            _activeStrokes[request.StrokeId] = new PaintStrokeState { HasPoints = false };
        }

        public void ApplyPaintPoint(PaintPointRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (!_activeStrokes.TryGetValue(request.StrokeId, out var state)) return;

            if (!state.HasPoints)
            {
                state.Min       = request.Point;
                state.Max       = request.Point;
                state.HasPoints = true;
            }
            else
            {
                state.Min = Vec3.Min(state.Min, request.Point);
                state.Max = Vec3.Max(state.Max, request.Point);
            }

            _activeStrokes[request.StrokeId] = state;
        }

        public void EndPaintStroke(string strokeId)
        {
            if (!_activeStrokes.TryGetValue(strokeId, out var state)) return;
            _activeStrokes.Remove(strokeId);

            if (!state.HasPoints) return;

            // Commit it as a selection feature, the same domain path as box-select.
            // The FeatureSelectionChanged event fires so Team 4 can react.
            _service.CreateSelectionFeature(state.Min, state.Max);
        }

        public void SelectFeature(string featureId)
        {
            var feature = FindByStringId(featureId);
            if (feature != null)
                _service.Select(feature);
        }

        // Searches all registered sets for a feature whose Id matches the parsed int.
        // Returns null if the ID string is non-numeric or no match is found.
        private Feature FindByStringId(string featureId)
        {
            if (!int.TryParse(featureId, out int id)) return null;
            foreach (var set in _catalog.AllSets())
                foreach (var feature in set.Features)
                    if (feature.Id == id) return feature;
            return null;
        }
    }
}
