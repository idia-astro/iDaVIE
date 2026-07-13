public sealed class MockGazeProvider : IGaze
{
    public Vector3    GazeOrigin        { get; set; } = Vector3.zero;
    public Vector3    GazeDirection     { get; set; } = Vector3.forward;
    public Quaternion GazeRotation      { get; set; } = Quaternion.identity;
    public Vector2    GazeFocusPoint    { get; set; } = new Vector2(0.5f, 0.5f);
    public float      GazeConfidence    { get; set; } = 0.5f;
    public bool       IsTracking        { get; set; } = true;
    public Vector3    GazeFixationPoint { get; set; } = Vector3.zero;
}