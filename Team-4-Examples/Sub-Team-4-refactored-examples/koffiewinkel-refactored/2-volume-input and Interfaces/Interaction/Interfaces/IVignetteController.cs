namespace Interaction.Interfaces
{
    public interface IVignetteController
    {
        bool IsEnabled { get; }
        void SetTarget(float intensity);
        void Update(float deltaTime);
    }
}
