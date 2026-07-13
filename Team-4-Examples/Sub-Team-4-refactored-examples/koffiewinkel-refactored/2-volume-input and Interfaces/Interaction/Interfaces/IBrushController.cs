namespace Interaction.Interfaces
{
    public enum BrushHand
    {
        Primary,
        Secondary
    }

    public interface IBrushController
    {
        int BrushSize { get; }
        bool AdditiveBrush { get; }
        short SourceId { get; }
        void IncreaseBrushSize();
        void DecreaseBrushSize();
        void ResetBrushSize();
        void UndoBrushStroke(BrushHand hand);
        void RedoBrushStroke(BrushHand hand);
        void SetBrushAdditive();
        void SetBrushSubtractive();
        void AddNewSource();
        void StartEditSourceID();
    }
}
