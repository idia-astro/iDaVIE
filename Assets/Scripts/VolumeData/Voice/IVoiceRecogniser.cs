/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 */
using System;
using UnityEngine.Windows.Speech;

namespace VolumeData.Voice
{
    public sealed class VoicePhraseRecognizedEventArgs : EventArgs
    {
        public string Text { get; }
        public ConfidenceLevel Confidence { get; }
        public DateTime PhraseStartTime { get; }
        public TimeSpan PhraseDuration { get; }

        public VoicePhraseRecognizedEventArgs(string text, ConfidenceLevel confidence, DateTime phraseStartTime, TimeSpan phraseDuration)
        {
            Text = text;
            Confidence = confidence;
            PhraseStartTime = phraseStartTime;
            PhraseDuration = phraseDuration;
        }
    }

    /// <summary>
    /// Abstraction over platform speech recognition (Windows KeywordRecognizer today).
    /// </summary>
    public interface IVoiceRecogniser
    {
        bool IsRunning { get; }
        void Start();
        void Stop();
        event EventHandler<VoicePhraseRecognizedEventArgs> PhraseRecognized;
    }
}
