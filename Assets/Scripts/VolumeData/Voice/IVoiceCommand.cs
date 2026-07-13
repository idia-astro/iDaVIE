/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 */
namespace VolumeData.Voice
{
    public interface IVoiceCommand
    {
        void Execute(IVoiceCommandContext context);
    }
}
