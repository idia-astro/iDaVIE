using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity.Collections
{
    public struct NativeArray<T> : IDisposable, IEnumerable<T> where T : struct
    {
        public int Length => 0;
        public T this[int index]
        {
            get => default;
            set { }
        }

        public void Dispose() { }
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

namespace Unity.Collections.LowLevel.Unsafe
{
    public static class UnsafeUtility
    {
    }

    public static class NativeArrayUnsafeUtility
    {
        public static unsafe void* GetUnsafeReadOnlyPtr<T>(this Unity.Collections.NativeArray<T> array) where T : struct => null;
    }
}

namespace Unity.Mathematics
{
    public struct half
    {
        public static implicit operator float(half value) => 0;
        public static explicit operator half(float value) => default;
    }
}

namespace UnityEngine.Serialization
{
    public sealed class FormerlySerializedAsAttribute : Attribute
    {
        public FormerlySerializedAsAttribute(string oldName)
        {
        }
    }
}

namespace Stateless
{
    public class StateMachine<TState, TTrigger>
    {
        public StateMachine(TState initialState)
        {
            State = initialState;
        }

        public TState State { get; private set; }
        public StateConfiguration Configure(TState state) => new StateConfiguration();
        public void Fire(TTrigger trigger) { }

        public sealed class StateConfiguration
        {
            public StateConfiguration Permit(TTrigger trigger, TState destinationState) => this;
            public StateConfiguration PermitIf(TTrigger trigger, TState destinationState, Func<bool> guard) => this;
            public StateConfiguration Ignore(TTrigger trigger) => this;
            public StateConfiguration IgnoreIf(TTrigger trigger, Func<bool> guard) => this;
            public StateConfiguration OnEntry(Action action) => this;
            public StateConfiguration OnEntryFrom(TTrigger trigger, Action action) => this;
            public StateConfiguration OnExit(Action action) => this;
        }
    }
}

namespace PolyAndCode.UI
{
    public interface ICell
    {
    }

    public interface IRecyclableScrollRectDataSource
    {
        int GetItemCount();
        void SetCell(ICell cell, int index);
    }

    public class RecyclableScrollRect : UnityEngine.MonoBehaviour
    {
        public IRecyclableScrollRectDataSource DataSource { get; set; }
        public void Initialize(IRecyclableScrollRectDataSource dataSource) => DataSource = dataSource;
        public void JumpToCell(int index) { }
        public void ReloadData() { }
    }
}

namespace SFB
{
    public sealed class ExtensionFilter
    {
        public ExtensionFilter(string name, params string[] extensions)
        {
        }
    }

    public static class StandaloneFileBrowser
    {
        public static void OpenFilePanelAsync(string title, string directory, ExtensionFilter[] extensions, bool multiselect, Action<string[]> cb) =>
            cb?.Invoke(Array.Empty<string>());

        public static void SaveFilePanelAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, Action<string> cb) =>
            cb?.Invoke(string.Empty);
    }
}

public class MomentMapMenuController : UnityEngine.MonoBehaviour
{
    public enum ThresholdType
    {
        Mask,
        Threshold
    }

    public enum LimitType
    {
        ZScale,
        MinMax
    }
}

public static class ToastNotification
{
    public static void Update() { }
    public static void ShowInfo(string message) { }
    public static void ShowWarning(string message) { }
    public static void ShowError(string message) { }
    public static void ShowSuccess(string message) { }
}

public class VideoRecordMenuController : UnityEngine.MonoBehaviour
{
    public void ExportToFile() { }
}

public class ExitController : UnityEngine.MonoBehaviour
{
    public VolumeInputController _volumeInputController;
    public VolumeData.VolumeDataSetRenderer _activeDataSet;
}

public class HistogramHelper : UnityEngine.MonoBehaviour
{
    public UnityEngine.Sprite CreateHistogramImg(int[] histogram, float binWidth, float min, float max, float mean, float stdDev) => new UnityEngine.Sprite();
    public UnityEngine.Sprite CreateHistogramImg(int[] histogram, float binWidth, float min, float max, float mean, float stdDev, float sigma) => new UnityEngine.Sprite();
}

public class Colorbar : UnityEngine.MonoBehaviour
{
    public VolumeData.ScalingType ScalingType { get; set; }
    public ColorMapEnum ColorMap { get; set; }
    public float ScaleMin { get; set; }
    public float ScaleMax { get; set; }
}

public class MenuBarBehaviour : UnityEngine.MonoBehaviour
{
    public UnityEngine.GameObject AboutSection { get; set; } = new UnityEngine.GameObject();
    public UnityEngine.GameObject VRViewDisplay { get; set; } = new UnityEngine.GameObject();
    public void ToggleAboutSection() { }
    public void ToggleVRViewDisplay() { }
}

public class DesktopPaintController : UnityEngine.MonoBehaviour
{
    public void StartPaintSelection() { }
}

public class UserConfirmationPopupController : UnityEngine.MonoBehaviour
{
    public void setMessageBody(string body) { }
    public void setHeaderText(string text) { }
    public void addButton(string buttonText, string hoverText, Action onClick) { }
}

public class LaserPointer : UnityEngine.MonoBehaviour
{
}

public class CustomDragHandler : UnityEngine.MonoBehaviour
{
    public void MoveDown() { }
    public void MoveUp() { }
}

public class CameraControllerTool : UnityEngine.MonoBehaviour
{
    public void OnUse() { }
}

public class VideoPosRecorder
{
    public enum videoLocRecMode
    {
        CURSOR,
        HEAD
    }

    public videoLocRecMode GetRecordingMode() => videoLocRecMode.CURSOR;
    public void addLocation(UnityEngine.Vector3 position, UnityEngine.Vector3 rotation, videoLocRecMode mode) { }
}
