using Interaction.Gaze;
using Interaction.Interfaces;
using NUnit.Framework;
using UnityEngine;

namespace Interaction.Gaze.Tests
{
    [TestFixture]
    public class GazeProviderTests
    {
        [Test]
        public void GazeFixationPoint_IsOriginPlusNormalisedDirectionTimesDistance()
        {
            var mock = new MockGazeProvider
            {
                GazeOrigin = new Vector3(0, 1, 0),
                GazeDirection = new Vector3(0, 0, 1),
                GazeFixationPoint = new Vector3(0, 1, 3)
            };

            Assert.That(mock.GazeFixationPoint, Is.EqualTo(new Vector3(0, 1, 3)));
            Assert.That(mock.GazeFixationPoint, Is.EqualTo(mock.GazeOrigin + mock.GazeDirection.normalized * 3f));
        }

        [Test]
        public void GazeRay_OriginAndDirectionMatchGazeProperties()
        {
            var mock = new MockGazeProvider
            {
                GazeOrigin = new Vector3(1, 2, 3),
                GazeDirection = new Vector3(1, 1, 0).normalized
            };

            Assert.That(mock.GazeRay.origin, Is.EqualTo(mock.GazeOrigin));
            Assert.That(mock.GazeRay.direction, Is.EqualTo(mock.GazeDirection));
        }

        [Test]
        public void WhenNotTracking_RendererShouldFallBackToScreenCentre()
        {
            var mock = new MockGazeProvider
            {
                IsGazeAvailable = false,
                IsTracking = false,
                GazeFocusPoint = new Vector2(0.2f, 0.8f),
                GazeConfidence = 0.0f
            };

            Vector2 centre = ComputeFoveationCentre(mock);

            Assert.That(centre, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(mock.GazeConfidence, Is.EqualTo(0.0f));
            Assert.That(mock.IsGazeAvailable, Is.False);
        }

        /// <summary>Matches Team 3 FoveatedSamplingPolicy: use focus when available, else screen centre.</summary>
        private static Vector2 ComputeFoveationCentre(IGaze gaze) =>
            gaze.IsTracking ? gaze.GazeFocusPoint : new Vector2(0.5f, 0.5f);
    }
}
