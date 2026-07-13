/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 */
using System;
using UnityEngine.Windows.Speech;

namespace VolumeData.Voice
{
    /// <summary>
    /// Windows KeywordRecognizer adapter. Only class that references Unity speech APIs directly.
    /// </summary>
    public sealed class WindowsVoiceRecogniser : IVoiceRecogniser, IDisposable
    {
        private readonly KeywordRecognizer _recognizer;

        public bool IsRunning => _recognizer != null && _recognizer.IsRunning;

        public event EventHandler<VoicePhraseRecognizedEventArgs> PhraseRecognized;

        public WindowsVoiceRecogniser(string[] keywords, ConfidenceLevel confidenceLevel)
        {
            _recognizer = new KeywordRecognizer(keywords, confidenceLevel);
            _recognizer.OnPhraseRecognized += OnPhraseRecognized;
        }

        public void Start()
        {
            _recognizer?.Start();
        }

        public void Stop()
        {
            _recognizer?.Stop();
        }

        public void Dispose()
        {
            if (_recognizer == null)
            {
                return;
            }

            _recognizer.OnPhraseRecognized -= OnPhraseRecognized;
            _recognizer.Dispose();
        }

        private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
        {
            PhraseRecognized?.Invoke(this, new VoicePhraseRecognizedEventArgs(
                args.text,
                (float)args.confidence,
                args.phraseStartTime,
                args.phraseDuration));
        }
    }
}
