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
using JetBrains.Annotations;
using Unity.VisualScripting;
using Debug = UnityEngine.Debug;

namespace VideoMaker
{
    public class VideoCameraController : MonoBehaviour
    {
        public class PlaybackUpdatedEventArgs : EventArgs
        {
            public float Progress { get; set; }
        }
        
        public EventHandler PlaybackFinished;
        public EventHandler<PlaybackUpdatedEventArgs> PlaybackUpdated;

        private const int FfmpegConsoleCount = 58; //From limited observation, this is how many console messages are printed by ffmpeg

        public Shader logoShader;
        public Texture2D logoTexture;
        private Material _logoMaterial;
        private float _logoAspect;
        
        public TMP_Text VideoScriptFilePath;

        private Camera _camera;

        private VideoScriptData _videoScript = null;
        public VideoScriptData VideoScript
        {
            get
            {
                return _videoScript;
            }
            set
            {
                _videoScript = value;
                
                //Setting RenderMaterial properties
                RenderTexture tex = GetComponent<Camera>().targetTexture;
                tex.Release();
                tex.height = value.Height;
                tex.width = value.Width;
                
                //Setting logo properties in shader
                //Length of u compoent of logo UV. Length of V is given by LogoScale
                float logoLenV = value.LogoScale;
                float logoLenU = _logoAspect * value.LogoScale * value.Height / value.Width;
                
                //Vector components are left bottom right top
                _logoMaterial.SetVector("_LogoBounds", value.logoPosition switch
                {
                    VideoScriptData.LogoPosition.BottomLeft => new Vector4(0, 0, logoLenU, logoLenV),
                    VideoScriptData.LogoPosition.BottomCenter => new Vector4(0.5f * (1 - logoLenU), 0, 0.5f * (1 + logoLenU), logoLenV),
                    VideoScriptData.LogoPosition.BottomRight => new Vector4(1 - logoLenU, 0, 1, logoLenV),
                    VideoScriptData.LogoPosition.CenterLeft => new Vector4(0, 0.5f * (1 - logoLenV), logoLenU, 0.5f * (1 + logoLenV)),
                    VideoScriptData.LogoPosition.CenterCenter => new Vector4(0.5f * (1 - logoLenU), 0.5f * (1 - logoLenV), 0.5f * (1 + logoLenU), 0.5f * (1 + logoLenV)),
                    VideoScriptData.LogoPosition.CenterRight => new Vector4(1 - logoLenU, 0.5f * (1 - logoLenV), 1, 0.5f * (1 + logoLenV)),
                    VideoScriptData.LogoPosition.TopLeft => new Vector4(0, 1 - logoLenV, logoLenU, 1),
                    VideoScriptData.LogoPosition.TopCenter => new Vector4(0.5f * (1 - logoLenU), 1 - logoLenV, 0.5f * (1 + logoLenU), 1),
                    VideoScriptData.LogoPosition.TopRight => new Vector4(1 - logoLenU, 1 - logoLenV, 1, 1),
                    _ => new Vector4(1 - logoLenU, 0, 1, logoLenV)
                });
            }
        }

        public bool IsPlaying { get; private set; }

        private int _frameCounter = 0;
        private int _frameTotal = 0;
        private int _frameDigits = 0;
        private bool _captureFrames = false;
        private Queue<byte[]> _frameQueue = new();

        private Thread _exportThread;
        private bool _threadIsProcessing;
        private bool _terminateThreadWhenDone;

        private string _videoPath;
        private string _framePath;
        public string FfmpegPath { get; set; }
        public string VideoFileName { get; set; }

        private Queue<PositionAction> _positionQueue = new();
        private Queue<DirectionAction> _directionQueue = new();
        private Queue<DirectionAction> _upDirectionQueue = new();

        private PositionAction _positionAction;
        private DirectionAction _directionAction;
        private DirectionAction _upDirectionAction;

        private Transform _cubeTransform;

        private float _time = 0f;

        private float _positionTime = 0f;
        private float _directionTime = 0f;
        private float _upDirectionTime = 0f;

        void Awake()
        {
            _logoMaterial = new Material(logoShader);
            _logoMaterial.SetTexture("_LogoTex", logoTexture);
            _logoAspect = logoTexture.width / logoTexture.height;

            var directory = new DirectoryInfo(Application.dataPath);
            _videoPath = System.IO.Path.Combine(directory.Parent.FullName, "Outputs/Video");
            _framePath = System.IO.Path.Combine(_videoPath, "frames");

            if (!Directory.Exists(_videoPath))
            {
                Directory.CreateDirectory(_videoPath);
            }

            _camera = GetComponent<Camera>();
            _camera.enabled = false;
        }

        IEnumerator Preview()
        {
            if (VideoScript is null)
            {
                yield break;
            }

            StartPlayback();

            while (_time < VideoScript.Duration)
            {
                OnPlaybackUpdated(_time / VideoScript.Duration);
                UpdatePlayback(Time.deltaTime);
                yield return null;
            }

            EndPlayback();
        }

        IEnumerator Export()
        {
            if (VideoScript is null)
            {
                yield break;
            }

            // Kill the encoder thread if running from a previous execution
            if (_exportThread != null && (_threadIsProcessing || _exportThread.IsAlive))
            {
                _threadIsProcessing = false;
                _exportThread.Join();
            }

            _frameCounter = 0;

            _terminateThreadWhenDone = false;
            _threadIsProcessing = true;
            _exportThread = new Thread(SaveFrames);
            _exportThread.Start();
            //TODO: Does the Thread terminate when callback is complete?

            StartPlayback();

            _frameTotal = (int)((float)VideoScript.FrameRate * VideoScript.Duration);
            _frameDigits = (int)Mathf.Floor(Mathf.Log10(_frameTotal) + 1);
            _frameTotal += FfmpegConsoleCount;

            _captureFrames = true;

            float deltaTime = 1f / (float)VideoScript.FrameRate;

            while (_time < VideoScript.Duration)
            {
                OnPlaybackUpdated(_frameCounter / (float)_frameTotal);
                UpdatePlayback(deltaTime);
                yield return null;
            }

            _camera.enabled = false;
            _captureFrames = false;
            _terminateThreadWhenDone = true;

            //TODO check if status changes to Export and change text on progress bar
            while (_threadIsProcessing)
            {
                OnPlaybackUpdated(_frameCounter / (float)_frameTotal);
                yield return null;
            }
            EndPlayback();
        }

        private void UpdatePlayback(float deltaTime)
        {
            (Vector3 position, Vector3 pathForward, Vector3 pathUp) = _positionAction.GetPositionDirection(_positionTime);

            UpdateTransform(
                position,
                _directionAction.GetDirection(_directionTime, position, pathForward, pathUp),
                _upDirectionAction.GetDirection(_upDirectionTime, position, pathForward, pathUp)
            );

            _time += deltaTime;

            UpdateActionTime(deltaTime, ref _positionTime, ref _positionAction, ref _positionQueue);
            UpdateActionTime(deltaTime, ref _directionTime, ref _directionAction, ref _directionQueue);
            UpdateActionTime(deltaTime, ref _upDirectionTime, ref _upDirectionAction, ref _upDirectionQueue);
        }

        //TODO remove this as actions have been refactored to use continuous time
        private void UpdateActionTime<T>(float deltaTime, ref float time, ref T action, ref Queue<T> actionQueue) where T : Action
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
            
            gameObject.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction, upDirection));
        }

        public void StartPlayback()
        {
            _camera.enabled = true;
            IsPlaying = true;

            GameObject volume = GameObject.Find("VolumeDataSetManager");
            _cubeTransform = volume.transform.Find("CubePrefab(Clone)");

            _positionQueue = new(VideoScript.PositionActions);
            _directionQueue = new(VideoScript.DirectionActions);
            _upDirectionQueue = new(VideoScript.UpDirectionActions);

            _positionAction = _positionQueue.Dequeue();
            _directionAction = _directionQueue.Dequeue();
            _upDirectionAction = _upDirectionQueue.Dequeue();
            
            _time = 0f;
            _positionTime = 0f;
            _directionTime = 0f;
            _upDirectionTime = 0f;
        }

        public void EndPlayback()
        {
            _camera.enabled = false;
            IsPlaying = false;
            OnPlaybackFinished();
        }

        protected virtual void OnPlaybackUpdated(float progress)
        {
            PlaybackUpdated?.Invoke(this, new PlaybackUpdatedEventArgs(){Progress = progress});
        }
        
        protected virtual void OnPlaybackFinished()
        {
            PlaybackFinished?.Invoke(this,  EventArgs.Empty);
        }

        public void StartPreview()
        {
            if (IsPlaying)
            {
                return;
            }
            StartCoroutine(Preview());
        }

        public void StartExport()
        {
            if (IsPlaying)
            {
                return;
            }
            StartCoroutine(Export());
        }
        
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination, _logoMaterial);
            if (!_captureFrames)
            {
                return;
            }

            //This is necessary to preserve colors when writing out
            RenderTexture rtNew = new RenderTexture(source.width, source.height, 8, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rtNew, _logoMaterial);
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
            if (Directory.Exists(_framePath))
            {
                Directory.Delete(_framePath, recursive: true);
            }
            Directory.CreateDirectory(_framePath);
            
            while (_threadIsProcessing)
            {
                if (_frameQueue.Count > 0)
                {
                    //TODO change to bmp for better video
                    string path = System.IO.Path.Combine(
                        _framePath,
                        string.Format("frame{0:d" + _frameDigits.ToString() + "}.png", _frameCounter)
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
            
            string command = $"-framerate {VideoScript.FrameRate} -i ./frames/frame%0{_frameDigits}d.png -c:v libx264 -pix_fmt yuv420p {VideoFileName}_{DateTime.Now:yyyyMMdd_Hmmssf}.mp4";
            
            var startInfo = new ProcessStartInfo
            {
                FileName = FfmpegPath,
                Arguments = command,
                WorkingDirectory = _videoPath,
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
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) { print(e.Data); _frameCounter++; } };
                process.WaitForExit();
            }

            _threadIsProcessing = false;
            
            Directory.Delete(_framePath, recursive: true);
        }
    }
}