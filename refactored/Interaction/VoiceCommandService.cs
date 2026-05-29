// SPDX-License-Identifier: LGPL-3.0-or-later
// VoiceCommandService — ST4 Application service. Realises IVoiceCommandStream
// (declared in IInteractionContracts.cs). Holds the VoiceCommandRegistry and
// drives recognition through a platform-neutral port; the Windows.Speech /
// SteamVR realisation is the ACL implementation (MOD-04).
//
// Replaces the keyword-driven branches in the legacy
// Assets/Scripts/VoiceCommands/ files (4 files, 384 LOC combined).

using System;

namespace iDaVIE.Interaction
{
    /// <summary>Platform-neutral phrase recogniser port; realised by the
    /// SteamVR / Windows.Speech ACL adapter.</summary>
    internal interface IPhraseRecogniser
    {
        bool IsListening { get; }
        void StartListening();
        void StopListening();
        event Action<string, float> PhraseRecognised; // (phrase, confidence)
    }

    internal sealed class VoiceCommandService : IVoiceCommandStream
    {
        private readonly IPhraseRecogniser    _recogniser;
        private readonly VoiceCommandRegistry _registry;

        public VoiceCommandService(IPhraseRecogniser recogniser,
                                   VoiceCommandRegistry registry,
                                   SpeechRecognitionConfig config)
        {
            _recogniser = recogniser; _registry = registry; Config = config;
        }

        public event Action<VoiceCommand> CommandRecognised;
        public bool                       IsListening => throw new NotImplementedException();
        public SpeechRecognitionConfig    Config      { get; }

        public void StartListening() => throw new NotImplementedException();
        public void StopListening()  => throw new NotImplementedException();
    }
}
