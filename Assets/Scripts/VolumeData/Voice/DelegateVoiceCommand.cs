/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 */
using System;

namespace VolumeData.Voice
{
    public sealed class DelegateVoiceCommand : IVoiceCommand
    {
        private readonly Action<IVoiceCommandContext> _execute;

        public DelegateVoiceCommand(Action<IVoiceCommandContext> execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public void Execute(IVoiceCommandContext context)
        {
            _execute(context);
        }
    }
}
