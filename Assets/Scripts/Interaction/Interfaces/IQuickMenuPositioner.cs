using UnityEngine;

namespace Interaction.Interfaces
{
    public interface IQuickMenuPositioner
    {
        void Show(int handIndex, Transform handTransform);
        void Hide();
    }
}
