using VolumeData;

namespace Interaction.Interfaces
{
    public interface ICursorInfoFormatter
    {
        string FormatCursorInfo(VolumeDataSetRenderer dataSetRenderer, bool displayOutsideCube);
        string FormatSelectionInfo(VolumeDataSetRenderer dataSetRenderer);
        string FormatAngle(double angle);
    }
}
