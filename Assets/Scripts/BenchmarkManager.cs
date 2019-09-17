using System.Collections;
using System.Collections.Generic;
using VolumeData;
using UnityEngine;
using Valve.VR;

public class BenchmarkManager : MonoBehaviour
{

    public float RotationSpeed = 20;
    public int RotationTimes = 1;
    public int StartWaitSeconds = 2;

    private VolumeDataSetRenderer _testVolume;
    private float _totalAngle = 0;
    private int _numberRotations = 0;
    private int _rotationAxis = 0;
    private int _distanceSet = 0;
    private double _timeInSeconds = 0;
    private SteamVR vr;
    private Compositor_FrameTiming timing;

    private bool _running = false;

    // Start is called before the first frame update
    void Start()
    {
        //double _timeInSeconds = 0;
        vr = SteamVR.instance;
        if (vr != null)
        {
            timing = new Valve.VR.Compositor_FrameTiming();
            timing.m_nSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Valve.VR.Compositor_FrameTiming));
            vr.compositor.GetFrameTiming(ref timing, 0);

            _timeInSeconds = timing.m_flSystemTimeInSeconds;

            /*
        System.Diagnostics.PerformanceCounter perfUptimeCount = new System.Diagnostics.PerformanceCounter("System", "System Up Time");
        System.TimeSpan uptimeSpan = System.TimeSpan.FromSeconds(perfUptimeCount.NextValue());
        double unixVersion = uptimeSpan.TotalSeconds;
        */
        _testVolume = GetComponentInChildren<VolumeDataSetRenderer>();
        
        }
        Debug.Log("Rotating around Y-axis at " + _timeInSeconds + " seconds.");
        _testVolume.transform.localEulerAngles = new Vector3(0, 0, 0);
        StartCoroutine(Wait5Seconds());
    }

    // Update is called once per frame
    void Update()
    {
        if (_running)
        {
            //float prevAngle = _testVolume.transform.localRotation.eulerAngles.y;
            if (_numberRotations < RotationTimes)
            {
                float deltaAngle = Time.deltaTime * RotationSpeed;
                switch (_rotationAxis)
                {
                    case 0:
                        _testVolume.transform.Rotate(0, deltaAngle, 0, Space.Self);
                        break;
                    case 1:
                        _testVolume.transform.Rotate(0, 0, deltaAngle, Space.Self);
                        break;
                    case 2:
                        _testVolume.transform.Rotate(deltaAngle, 0, 0, Space.Self);
                        break;
                }
                //float currentAngle = _testVolume.transform.localRotation.eulerAngles.y;
                //float angleChange = Mathf.Abs(currentAngle - prevAngle);
                _totalAngle += deltaAngle;
                //Debug.Log("Rotation: " + _totalAngle);
                _numberRotations = (int)(_totalAngle / 360f);
                //Debug.Log("Rotation#: " + _numberRotations);
            }
            else
            {
                _totalAngle = 0;
                _numberRotations = 0;
                _testVolume.transform.localEulerAngles = new Vector3(0, 0, 0);
                //_testVolume.transform.Rotate(0, 0, 90, Space.Self);

                switch (_rotationAxis)
                {
                    case 0:
                        _testVolume.transform.Rotate(-90, -90, 0, Space.Self);
                        _rotationAxis++;
                        if (vr != null)
                        {
                            vr.compositor.GetFrameTiming(ref timing, 0);
                            _timeInSeconds = timing.m_flSystemTimeInSeconds;
                        }
                        Debug.Log("Rotating around Z-axis at " + _timeInSeconds + " seconds.");
                        break;

                    case 1:
                        //_testVolume.transform.Rotate(90, 0, 0, Space.Self);
                        _testVolume.transform.Rotate(0, 90, 90, Space.Self);
                        //_testVolume.transform.Rotate(0, 180, 0, Space.Self);
                        _rotationAxis++;
                        if (vr != null)
                        {
                            vr.compositor.GetFrameTiming(ref timing, 0);
                            _timeInSeconds = timing.m_flSystemTimeInSeconds;
                        }
                        Debug.Log("Rotating around X-axis at " + _timeInSeconds + " seconds.");
                        break;

                    case 2:
                        _rotationAxis = 0;
                        _testVolume.transform.localEulerAngles = new Vector3(0, 0, 0);

                        _testVolume.transform.Translate(0, 0, 1);

                        if (vr != null)
                        {
                            vr.compositor.GetFrameTiming(ref timing, 0);
                            _timeInSeconds = timing.m_flSystemTimeInSeconds;
                        }
                        Debug.Log("Rotating around Y-axis at " + _timeInSeconds + " seconds.");
                        break;
                }


            }
        }
        
    }

    IEnumerator Wait5Seconds()
    {
        yield return new WaitForSeconds(StartWaitSeconds);
        _running = true;
    }
}
