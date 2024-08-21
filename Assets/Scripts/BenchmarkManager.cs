/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 Inter-University Institute for Data Intensive Astronomy
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms 
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with 
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party 
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */
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

        Debug.Log("Test starting at " + _testVolume.transform.localPosition.z + " meters away.");
        _testVolume.transform.localEulerAngles = new Vector3(0, -45, 0);
        StartCoroutine(WaitSeconds());
    }

    // Update is called once per frame
    void Update()
    {
        if (_running)
        {
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
                _totalAngle += deltaAngle;
                _numberRotations = (int)(_totalAngle / 360f);
            }
            else
            {
                _totalAngle = 0;
                _numberRotations = 0;
                _testVolume.transform.localEulerAngles = new Vector3(0, -45, 0);
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
                        _testVolume.transform.Rotate(0, 90, 90, Space.Self);
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
                        Debug.Log("Test cube now at " + _testVolume.transform.localPosition.z + " meters away.");

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

    IEnumerator WaitSeconds()
    {
        yield return new WaitForSeconds(StartWaitSeconds);

        Debug.Log("Rotating around Y-axis at " + _timeInSeconds + " seconds.");
        _running = true;
    }
}
