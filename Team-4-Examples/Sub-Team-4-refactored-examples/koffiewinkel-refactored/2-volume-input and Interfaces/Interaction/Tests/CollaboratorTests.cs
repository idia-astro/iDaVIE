using System;
using Interaction;
using Interaction.Interfaces;
using NUnit.Framework;
using UnityEngine;

namespace Interaction.Tests
{
    [TestFixture]
    public class CollaboratorTests
    {
        private static BrushController CreateBrushControllerWithNoOps()
        {
            return new BrushController(
                () => null,
                () => Vector3.zero,
                () => { },
                () => InteractionState.IdleSelecting,
                () => { },
                hand => { });
        }

        [Test]
        public void BrushController_IncreaseBrushSize_IncrementsBy2()
        {
            var controller = CreateBrushControllerWithNoOps();
            int initial = controller.BrushSize;

            controller.IncreaseBrushSize();

            Assert.That(controller.BrushSize, Is.EqualTo(initial + 2));
        }

        [Test]
        public void BrushController_DecreaseBrushSize_NeverGoesBelowOne()
        {
            var controller = CreateBrushControllerWithNoOps();
            Assert.That(controller.BrushSize, Is.EqualTo(1));

            controller.DecreaseBrushSize();
            controller.DecreaseBrushSize();
            controller.DecreaseBrushSize();

            Assert.That(controller.BrushSize, Is.EqualTo(1));
        }

        [Test]
        public void CursorInfoFormatter_FormatAngle_BelowOneDegree_ReturnsMinutesAndSeconds()
        {
            var formatter = new CursorInfoFormatter();

            string result = formatter.FormatAngle(0.5 * Math.PI / 180.0);

            Assert.That(result, Does.Contain("'"));
            Assert.That(result, Does.Not.Contain("°"));
        }
    }
}
