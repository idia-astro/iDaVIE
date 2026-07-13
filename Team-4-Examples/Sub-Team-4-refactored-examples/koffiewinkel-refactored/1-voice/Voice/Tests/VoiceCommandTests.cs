using System;
using System.Collections.Generic;
using NUnit.Framework;
using VolumeData.Voice;

namespace VolumeData.Voice.Tests
{
    [TestFixture]
    public class VoiceCommandTests
    {
        private sealed class VoiceCommandRegistry
        {
            private readonly Dictionary<string, IVoiceCommand> _commands =
                new Dictionary<string, IVoiceCommand>(StringComparer.Ordinal);

            public void Register(string keyword, IVoiceCommand command)
            {
                _commands[keyword] = command;
            }

            public IVoiceCommand Lookup(string keyword)
            {
                return _commands.TryGetValue(keyword, out var command) ? command : null;
            }
        }

        private sealed class MockVoiceCommandContext : IVoiceCommandContext
        {
            public VolumeDataSetRenderer ActiveDataSet => null;
            public IReadOnlyList<VolumeDataSetRenderer> DataSets => Array.Empty<VolumeDataSetRenderer>();
            public VolumeInputController VolumeInput => null;
            public QuickMenuController QuickMenu => null;
            public PaintMenuController PaintMenu => null;
            public FeatureMenuController FeatureMenu => null;
            public VideoRecordMenuController VideoRecordMenu => null;

            public void RefreshActiveDataSet() { }

            public bool RequireMask() => false;

            public void ShowMissingMaskError() { }
        }

        [Test]
        public void VoiceCommandRegistry_RegisterAndExecute_CallsCorrectCommand()
        {
            var registry = new VoiceCommandRegistry();
            bool wasCalled = false;
            registry.Register("paint mode", new DelegateVoiceCommand(_ => wasCalled = true));

            IVoiceCommand command = registry.Lookup("paint mode");
            command.Execute(new MockVoiceCommandContext());

            Assert.That(wasCalled, Is.True);
        }

        [Test]
        public void VoiceCommandRegistry_UnknownKeyword_ReturnsNull()
        {
            var registry = new VoiceCommandRegistry();

            IVoiceCommand result = registry.Lookup("unknown command");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void DelegateVoiceCommand_Execute_ForwardsContextToDelegate()
        {
            var mockContext = new MockVoiceCommandContext();
            IVoiceCommandContext captured = null;
            var command = new DelegateVoiceCommand(context => captured = context);

            command.Execute(mockContext);

            Assert.That(captured, Is.SameAs(mockContext));
        }
    }
}
