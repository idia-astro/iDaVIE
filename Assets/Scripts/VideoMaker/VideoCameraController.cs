using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace VideoMaker
{
    public class VideoCameraController : MonoBehaviour
    {
        private enum Status
        {
            Idle,
            Load,
            Export,
            ExportPlayback,
            PreviewPlayback,
        }

        //TODO What's the best way to assign a label to the enum? Description attribute seems like overkill...
        private Dictionary<Status, string> _statusLabels = new Dictionary<Status, string>
    {
        {Status.Idle, ""},
        {Status.Load, "Loading"},
        {Status.Export, "Exporting Video"},
        {Status.ExportPlayback, "Recording Video"},
        {Status.PreviewPlayback, "Preview Video"}
    };

        private Status _status = Status.Idle;

        // TODO use a different MonoBehaviour to manage the playback and status bar?
        public TMP_Dropdown VideoScriptDropdown;
        public GameObject ProgressBar;
        public TMP_Text StatusText;
        public Texture2D Logo;
        private bool _useLogo = true;
        private float _logoScale = 0.2f;
        private byte[] _logoBytes = new byte[0];

        private Camera _camera;

        private const float WaitTime = 1.5f;
        private const float PunchInTime = 1f;
        private const float CircleTime = 1.5f;
        private const float RotateTime = 1.5f;
        private const float BackWaitTime = 0.5f;
        private const float Dist = 2f;
        private Vector3 Target = Vector3.zero;//new Vector3(0.01f, 0.02f, 0.02f);
        private EasingInOut easingIO = new(2);

        private const float TurntableTime = 15f;
        private const float TurntableWaitTime = 0.5f;

        private TMP_Dropdown.OptionData[] _videoScriptOptions = {
            new ("Front-side-top"),
            new ("Turntable y"),
            new ("Turntable y/z")
        };

        private VideoPositionAction[][] _positionArrays;
        private VideoDirectionAction[][] _directionArrays;
        private VideoDirectionAction[][] _upDirectionArrays;
        
        private int _frameRate = 30;
        private int _frameCounter = 0;
        private int _frameDigits = 3;
        private bool _captureFrames = false;
        private Queue<byte[]> _frameQueue = new();

        private Thread _exportThread;
        private bool _threadIsProcessing;
        private bool _terminateThreadWhenDone;
        private bool _threadFramesProcessing;

        private string _directoryPath;

        private Queue<VideoPositionAction> _positionQueue = new();
        private Queue<VideoDirectionAction> _directionQueue = new();
        private Queue<VideoDirectionAction> _upDirectionQueue = new();

        private VideoPositionAction _positionAction;
        private VideoDirectionAction _directionAction;
        private VideoDirectionAction _upDirectionAction;

        private GameObject _targetCube;
        private Transform _cubeTransform;

        private float _duration = 0f;
        private float _time = 0f;

        private float _positionTime = 0f;
        private float _directionTime = 0f;
        private float _upDirectionTime = 0f;

        void Awake()
        {
            VideoScriptDropdown.options = new List<TMP_Dropdown.OptionData>(_videoScriptOptions);

            _positionArrays = new VideoPositionAction[_videoScriptOptions.Length][];
            _directionArrays = new VideoDirectionAction[_videoScriptOptions.Length][];
            _upDirectionArrays = new VideoDirectionAction[_videoScriptOptions.Length][];

            //Front-side-top
            CirclePath circleSide = new(
                Target + Dist * Vector3.back, Target + Dist * Vector3.left, Target,
                easing: easingIO
            );

            CirclePath circleTop = new(
                Target + Dist * Vector3.left, Target + Dist * Vector3.up, Target,
                easing: easingIO
            );

            CirclePath circleBack = new(
                Target + Dist * Vector3.up, Target + Dist * Vector3.back, Target,
                easing: easingIO
            );

            _positionArrays[0] = new VideoPositionAction[] {
                new VideoPositionActionHold(WaitTime, Target + Dist * Vector3.back),
                new VideoPositionActionPath(CircleTime, circleSide),
                new VideoPositionActionHold(WaitTime, Target + Dist * Vector3.left),
                new VideoPositionActionPath(CircleTime, circleTop),
                new VideoPositionActionHold(WaitTime + RotateTime + BackWaitTime, Target + Dist * Vector3.up),
                new VideoPositionActionPath(CircleTime, circleBack)
            };
            _directionArrays[0] = new VideoDirectionAction[] {
                new VideoDirectionActionLookAt(1000f, Target)
            };
            _upDirectionArrays[0] = new VideoDirectionAction[] {
                new VideoDirectionActionHold(2 * WaitTime + 1 * CircleTime, Vector3.up),
                new VideoDirectionActionPath(CircleTime, circleTop),
                new VideoDirectionActionHold(WaitTime, Vector3.right),
                new VideoDirectionActionTween(RotateTime, Vector3.right, Vector3.forward, easing: easingIO),
                new VideoDirectionActionHold(BackWaitTime, Vector3.forward),
                new VideoDirectionActionPath(CircleTime, circleBack, invert: true),
            };

            // Turntable y
            _positionArrays[1] = new VideoPositionAction[] {
                new VideoPositionActionPath(TurntableTime, new CirclePath(
                    Vector3.back * Dist, Vector3.right * Dist, Vector3.zero, fullRotation: true
                ))
            };
            _directionArrays[1] = new VideoDirectionAction[] {
                new VideoDirectionActionLookAt(TurntableTime, Vector3.zero)
            };
            _upDirectionArrays[1] = new VideoDirectionAction[] {
                new VideoDirectionActionHold(TurntableTime, Vector3.up)
            };

            // Turntable y/z
            EasingAccelDecel easingADFull = new(0.25f, 0.75f);
            EasingAccelDecel easingADStart = new(1f, 1f);
            EasingAccelDecel easingADEnd = new(0f, 2f / 3f);

            CirclePath circleZ = new CirclePath(
                Dist * Vector3.left, Dist * Vector3.up, Vector3.zero, easing: easingADFull, fullRotation: true
            );

            _positionArrays[2] = new VideoPositionAction[] {
                new VideoPositionActionPath(TurntableTime * 0.75f, new CirclePath(
                    Dist * Vector3.back, Dist * Vector3.left, Vector3.zero, easing: easingADEnd, largeAngleDirection: true
                )),
                new VideoPositionActionHold(TurntableWaitTime, Dist * Vector3.left),
                new VideoPositionActionPath(TurntableTime, circleZ),
                new VideoPositionActionHold(TurntableWaitTime, Dist * Vector3.left),
                new VideoPositionActionPath(TurntableTime * 0.25f, new CirclePath(
                    Dist * Vector3.left, Dist * Vector3.back, Vector3.zero, easing: easingADStart
                ))
            };
            _upDirectionArrays[2] = new VideoDirectionAction[] {
                new VideoDirectionActionHold(TurntableTime * 0.75f + TurntableWaitTime, Vector3.up),
                new VideoDirectionActionPath(TurntableTime, circleZ),
                new VideoDirectionActionHold(TurntableWaitTime + TurntableTime * 0.25f, Vector3.up)
            };
            _directionArrays[2] = new VideoDirectionAction[] {
                new VideoDirectionActionLookAt(TurntableTime * 2 + TurntableWaitTime, Vector3.zero)
            };


            ProgressBar.SetActive(false);
            _targetCube = GameObject.Find("TestCube");
            _targetCube.SetActive(false);

            var directory = new DirectoryInfo(Application.dataPath);
            _directoryPath = Path.Combine(directory.Parent.FullName, "Outputs/Video");

            if (!System.IO.Directory.Exists(_directoryPath))
            {
                System.IO.Directory.CreateDirectory(_directoryPath);
            }

            _camera = GetComponent<Camera>();
            _camera.enabled = false;

            
        }

        IEnumerator Preview()
        {
            _status = Status.PreviewPlayback;
            _camera.enabled = true;
            StartPlayback();

            while (_time < _duration)
            {
                UpdatePlayback(Time.deltaTime);
                yield return null;
            }

            _camera.enabled = false;
            ProgressBar.SetActive(false);
            _status = Status.Idle;
        }

        IEnumerator Export()
        {
            // Kill the encoder thread if running from a previous execution
            if (_exportThread != null && (_threadIsProcessing || _exportThread.IsAlive))
            {
                _threadIsProcessing = false;
                _exportThread.Join();
            }

            //Deleting existing frames and video file
            foreach (FileInfo file in new DirectoryInfo(_directoryPath).EnumerateFiles())
            {
                file.Delete();
            }

            //TODO The result from this looks really strange, I need to troubleshoot it
            // if (Logo is not null && _logoBytes.Length == 0)
            // {
            //     // _logoBytes = Logo.EncodeToPNG();

            //     //See https://stackoverflow.com/questions/44733841/how-to-make-texture2d-readable-via-script
            //     RenderTexture renderTex = RenderTexture.GetTemporary(
            //         Logo.width,
            //         Logo.height,
            //         0,
            //         RenderTextureFormat.Default,
            //         RenderTextureReadWrite.Linear);

            //     Graphics.Blit(Logo, renderTex);
            //     RenderTexture previous = RenderTexture.active;
            //     RenderTexture.active = renderTex;

            //     Texture2D tex = new Texture2D(renderTex.width, renderTex.height, TextureFormat.RGB24, false);
            //     tex.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            //     tex.Apply();
            //     // Encode texture into PNG
            //     _logoBytes = tex.EncodeToPNG();
            //     Destroy(tex);
            //     // Destroy(renderTex);
            // }

            _status = Status.ExportPlayback;
            _frameCounter = 0;
            _terminateThreadWhenDone = false;
            _threadIsProcessing = true;
            _exportThread = new Thread(SaveFrames);
            _exportThread.Start();
            //TODO: Does the Thread terminate when callback is complete?

            _camera.enabled = true;

            StartPlayback();

            _captureFrames = true;

            float deltaTime = 1f / (float)_frameRate;
            // _frameDigits = 

            while (_time < _duration)
            {
                UpdatePlayback(deltaTime);
                yield return null;
            }

            _camera.enabled = false;
            _captureFrames = false;
            _terminateThreadWhenDone = true;

            //TODO check if status changes to Export and change text on progress bar
            while (_threadIsProcessing)
            {
                yield return null;
            }

            //I'm not using this right now because I'd rather clear properly than letting garbage pile up
            // _logoBytes = new(0);

            ProgressBar.SetActive(false);
            _status = Status.Idle;
        }

        private void UpdatePlayback(float deltaTime)
        {
            ProgressBar.GetComponent<Slider>().value = _time / _duration;

            Vector3 position = _positionAction.GetPosition(_positionTime);

            UpdateTransform(
                position,
                _directionAction.GetDirection(_directionTime, position),
                _upDirectionAction.GetDirection(_upDirectionTime, position)
            );

            _time += deltaTime;

            UpdateActionTime<VideoPositionAction>(deltaTime, ref _positionTime, ref _positionAction, ref _positionQueue);
            UpdateActionTime<VideoDirectionAction>(deltaTime, ref _directionTime, ref _directionAction, ref _directionQueue);
            UpdateActionTime<VideoDirectionAction>(deltaTime, ref _upDirectionTime, ref _upDirectionAction, ref _upDirectionQueue);
        }

        private void UpdateActionTime<T>(float deltaTime, ref float time, ref T action, ref Queue<T> actionQueue) where T : VideoCameraAction
        {
            time += deltaTime;

            if (time > action.Duration)
            {
                if (actionQueue.Count == 0)
                {
                    time = action.Duration;
                }
                else
                {
                    time -= action.Duration;
                    action = actionQueue.Dequeue();
                }
            }
        }

        private void UpdateTransform(Vector3 position, Vector3 direction, Vector3 upDirection)
        {
            position = _cubeTransform.TransformPoint(position);
            direction = _cubeTransform.TransformDirection(direction);
            upDirection = _cubeTransform.TransformDirection(upDirection);

            gameObject.transform.position = position;
            gameObject.transform.LookAt(position + direction, upDirection);
        }

        public void StartPlayback()
        {
            StatusText.text = _statusLabels[_status];

            _cubeTransform = null;
            _targetCube.SetActive(false);

            GameObject volume = GameObject.Find("VolumeDataSetManager");

            if (volume is not null)
            {
                _cubeTransform = volume.transform.Find("CubePrefab(Clone)");
            }

            if (_cubeTransform is null)
            {
                _targetCube.SetActive(true);
                _cubeTransform = _targetCube.transform;
            }

            int scriptIdx = VideoScriptDropdown.value;

            _positionQueue = new(_positionArrays[scriptIdx]);
            _directionQueue = new(_directionArrays[scriptIdx]);
            _upDirectionQueue = new(_upDirectionArrays[scriptIdx]);
            
            float duration = 0f;
            foreach (VideoCameraAction action in _positionArrays[scriptIdx])
            {
                duration += action.Duration;
            }
            _duration = duration;

            foreach (VideoCameraAction action in _directionArrays[scriptIdx])
            {
                duration += action.Duration;
            }
            _duration = Math.Min(_duration, duration);

            foreach (VideoCameraAction action in _upDirectionArrays[scriptIdx])
            {
                duration += action.Duration;
            }
            _duration = Math.Min(_duration, duration);

            _positionAction = _positionQueue.Dequeue();
            _directionAction = _directionQueue.Dequeue();
            _upDirectionAction = _upDirectionQueue.Dequeue();

            ProgressBar.SetActive(true);

            _time = 0f;
            _positionTime = 0f;
            _directionTime = 0f;
            _upDirectionTime = 0f;
        }

        public void OnPreviewClick()
        {
            if (_status != Status.Idle)
            {
                return;
            }
            StartCoroutine(Preview());
        }

        public void OnRecordClick()
        {
            if (_status != Status.Idle)
            {
                return;
            }
            StartCoroutine(Export());
        }

        public void OnLogoOptionSelected(int idx)
        {
            if (idx == 0)
            {
                _useLogo = true;
            }
            else
            {
                _useLogo = false;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination);
            if (!_captureFrames)
            {
                return;
            }

            //This is necessary to preserve colors when writing out
            RenderTexture rtNew = new RenderTexture(source.width, source.height, 8, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rtNew);
            RenderTexture.active = rtNew;

            //Derived from CameraControllerTool.cs:96+
            // Make a new texture and read the active Render Texture into it.
            Texture2D tex = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);

            tex.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            tex.Apply();
            // Encode texture into PNG
            byte[] bytes = tex.EncodeToPNG();
            Destroy(tex); //Is it better to re-use the texture each frame?
            _frameQueue.Enqueue(bytes);
            
            RenderTexture.active = destination; //TODO rather set to null?
        }

        private void SaveFrames()
        {
            while (_threadIsProcessing)
            {
                if (_frameQueue.Count > 0)
                {
                    //TODO change to bmp for better video
                    string path = Path.Combine(
                        _directoryPath,
                        string.Format("frame{0:d"+ _frameDigits.ToString() + "}.png", _frameCounter)
                    );
                    //TODO is a FileStream better here?
                    //TODO does this work with .bmp format?
                    File.WriteAllBytes(path, _frameQueue.Dequeue());
                    _frameCounter++;
                }
                else
                {
                    if (_terminateThreadWhenDone)
                    {
                        break;
                    }
                    Thread.Sleep(1);
                }
            }

            string overlay = "";

            if (_useLogo)
            {
                overlay = string.Format("-i ..\\logo.png -filter_complex \"[1:v]scale=iw*{0:F}:-1[logo];[0:v][logo]overlay=W-w-10:H-h-10\" ", _logoScale);
            }
            
            // string overlay = "";

            // Save logo
            // if (_logoBytes.Length > 0)
            // {
            //     string path = Path.Combine(_directoryPath, "logo.png");
            //     File.WriteAllBytes(path, _logoBytes);
            //     overlay = string.Format("-i logo.png -filter_complex \"[1:v]scale=iw*{0:F}:-1[logo];[0:v][logo]overlay=W-w-10:H-h-10\" ", _logoScale);
            // }

            //TODO change ProgressBar text
            //ffmpeg -framerate 10 -i frame%03d.png -c:v libx264 -pix_fmt yuv420p  video.mp4
            //-i idavie_logo_better.png -filter_complex "[1:v]scale=iw*0.1:-1[logo];[0:v][logo]overlay=W-w-10:H-h-10"
            string command = string.Format("-framerate {0} -i frame%0{1}d.png {2}-c:v libx264 -pix_fmt yuv420p video.mp4", _frameRate, _frameDigits, overlay);

            print(command);

            var startInfo = new ProcessStartInfo
            {
                FileName = @"C:\Program Files\WinGet\Links\ffmpeg.exe",
                Arguments = command,
                WorkingDirectory = _directoryPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.EnableRaisingEvents = true;
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.OutputDataReceived += (s, e) => { if (e.Data != null) print(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) print(e.Data); };
                process.WaitForExit();
            }

            // _terminateThreadWhenDone = false;
            _threadIsProcessing = false;
        }
    }
}