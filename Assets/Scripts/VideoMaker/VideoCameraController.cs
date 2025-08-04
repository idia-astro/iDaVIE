using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Diagnostics;
using SFB;

namespace VideoMaker
{
    public class VideoCameraController : MonoBehaviour
    {
        //TODO consider phasing this out
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

        public TMP_Text VideoScriptFileText;

        // TODO use a different MonoBehaviour to manage the playback and status bar?
        public GameObject ProgressBar;
        public TMP_Text StatusText;
        public Texture2D Logo;
        private byte[] _logoBytes = new byte[0];

        private Camera _camera;

        private VideoScriptData _videoScript = null;

        private int _frameCounter = 0;
        private int _frameDigits = 3;
        private bool _captureFrames = false;
        private Queue<byte[]> _frameQueue = new();

        private Thread _exportThread;
        private bool _threadIsProcessing;
        private bool _terminateThreadWhenDone;

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
            ProgressBar.SetActive(false);
            _targetCube = GameObject.Find("TestCube");
            _targetCube.SetActive(false);

            var directory = new DirectoryInfo(Application.dataPath);
            _directoryPath = Path.Combine(directory.Parent.FullName, "Outputs/Video");

            if (!Directory.Exists(_directoryPath))
            {
                Directory.CreateDirectory(_directoryPath);
            }

            _camera = GetComponent<Camera>();
            _camera.enabled = false;
        }

        //Taken from CanvassDesktop.BrowseImageFile
        public void BrowseVideoScriptFile()
        {
            string lastPath = PlayerPrefs.GetString("LastPath");
            if (!Directory.Exists(lastPath))
                lastPath = "";
            var extensions = new[]
            {
            new ExtensionFilter("JSON Files", "json"),
            new ExtensionFilter("All Files", "*"),
        };
            StandaloneFileBrowser.OpenFilePanelAsync("Open File", lastPath, extensions, false, (string[] paths) =>
            {
                if (paths.Length == 1)
                {
                    PlayerPrefs.SetString("LastPath", Path.GetDirectoryName(paths[0]));
                    PlayerPrefs.Save();

                    LoadVideoScriptFile(paths[0]);
                    VideoScriptFileText.text = Path.GetFileName(paths[0]);
                }
            });
        }

        private void LoadVideoScriptFile(string path)
        {
            if (path == null)
            {
                return;
            }

            using (StreamReader reader = new(path))
            {
                //TODO use async?
                string jsonString = reader.ReadToEnd();
                _videoScript = VideoScriptReader.ReadVideoScript(jsonString);
            }
        }

        IEnumerator Preview()
        {
            if (_videoScript is null)
            {
                yield break;
            }

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
            if (_videoScript is null)
            {
                yield break;
            }

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

            float deltaTime = 1f / (float)_videoScript.FrameRate;
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

            UpdateActionTime(deltaTime, ref _positionTime, ref _positionAction, ref _positionQueue);
            UpdateActionTime(deltaTime, ref _directionTime, ref _directionAction, ref _directionQueue);
            UpdateActionTime(deltaTime, ref _upDirectionTime, ref _upDirectionAction, ref _upDirectionQueue);
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


            // int scriptIdx = VideoScriptDropdown.value;

            _positionQueue = new(_videoScript.PositionActions);
            _directionQueue = new(_videoScript.DirectionActions);
            _upDirectionQueue = new(_videoScript.UpDirectionActions);
            
            float duration = 0f;
            foreach (VideoCameraAction action in _videoScript.PositionActions)
            {
                duration += action.Duration;
            }
            _duration = duration;

            foreach (VideoCameraAction action in _videoScript.DirectionActions)
            {
                duration += action.Duration;
            }
            _duration = Math.Min(_duration, duration);

            foreach (VideoCameraAction action in _videoScript.UpDirectionActions)
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
                        string.Format("frame{0:d" + _frameDigits.ToString() + "}.png", _frameCounter)
                    );
                    //TODO is a FileStream better here?
                    //TODO does this work with .bmp format?
                    File.WriteAllBytes(path, _frameQueue.Dequeue());
                    _frameCounter++;
                    //TODO tick up progress for video render progress
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

            if (_videoScript.UseLogo)
            {
                overlay = string.Format("-i ..\\logo.png -filter_complex \"[1:v]scale=iw*{0:F}:-1[logo];[0:v][logo]overlay=W-w-10:H-h-10\" ", _videoScript.LogoScale);
            }
            
            // string overlay = "";

            // Save logo
            // if (_logoBytes.Length > 0)
            // {
            //     string path = Path.Combine(_directoryPath, "logo.png");
            //     File.WriteAllBytes(path, _logoBytes);
            //     overlay = string.Format("-i logo.png -filter_complex \"[1:v]scale=iw*{0:F}:-1[logo];[0:v][logo]overlay=W-w-10:H-h-10\" ", _videoScript.LogoScale);
            // }

            //TODO change ProgressBar text
            //ffmpeg -framerate 10 -i frame%03d.png -c:v libx264 -pix_fmt yuv420p  video.mp4
            //-i idavie_logo_better.png -filter_complex "[1:v]scale=iw*0.1:-1[logo];[0:v][logo]overlay=W-w-10:H-h-10"
            string command = string.Format("-framerate {0} -i frame%0{1}d.png {2}-c:v libx264 -pix_fmt yuv420p video.mp4", _videoScript.FrameRate, _frameDigits, overlay);

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
                process.OutputDataReceived += (s, e) => { if (e.Data != null) print(e.Data); }; //TODO use this to increase progress ticks for render mode
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) print(e.Data); };
                process.WaitForExit();
            }

            // _terminateThreadWhenDone = false;
            _threadIsProcessing = false;
        }
    }
}