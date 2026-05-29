// SPDX-License-Identifier: LGPL-3.0-or-later
// VoiceCommandRegistry — keyword vocabulary value object held by
// VoiceCommandService. Replaces the hard-coded keyword tables on the legacy
// VoiceCommandListCreator + ColourMapListCreator (Assets/Scripts/VoiceCommands/).
// Plain C# — no UnityEngine, no Windows.Speech (MOD-04).

using System.Collections.Generic;

namespace iDaVIE.Interaction
{
    public sealed class VoiceCommandRegistry
    {
        /// <summary>Keyword → command lookup, case-insensitive.</summary>
        public IReadOnlyDictionary<string, VoiceCommand> Keywords { get; init; }
            = new Dictionary<string, VoiceCommand>();

        /// <summary>Returns the command for the recognised phrase, or null on no match.</summary>
        public VoiceCommand? Resolve(string phrase) => throw new System.NotImplementedException();
    }
}
