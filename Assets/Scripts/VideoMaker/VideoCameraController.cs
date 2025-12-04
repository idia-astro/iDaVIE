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
    /// <summary>
    /// This class is responsible for moving the camera during video preview, capturing video frames and calling FFmpeg to compile the frames into a video.
    /// This script should be attached to the camera directly and needs to be triggered externally by the VideoUiManager to perform its functions.
    /// </summary>
    public class VideoCameraController : MonoBehaviour
    {
        /// <summary>
        /// EventArgs class to be used with the PlaybackUpdated event.
        /// </summary>
        public class PlaybackUpdatedEventArgs : EventArgs
        {
            public float Progress { get; set; }
        }
        
        public EventHandler PlaybackFinished;
        public EventHandler<PlaybackUpdatedEventArgs> PlaybackUpdated;
        public EventHandler PlaybackUnstoppable;
        
        public enum PlayMode
        {
            //NoScript,
            Standby,
            Preview,
            PreviewPaused,
            ExportPlayback,
            ExportPaused,
            ExportCompile
        }
        
        public PlayMode playMode = PlayMode.Standby;

        public bool IsPlayModePreview => playMode is PlayMode.Preview or PlayMode.PreviewPaused;

        public bool IsPlayModeExport =>
            playMode is PlayMode.ExportPlayback or PlayMode.ExportPaused or PlayMode.ExportCompile;

        private const int FfmpegConsoleCount = 58; //From limited observation, this is how many console messages are printed by ffmpeg

        public Shader logoShader;
        public Texture2D logoTexture;
        private Material _logoMaterial;
        private float _logoAspect;
        
        public TMP_Text VideoScriptFilePath;

        private Camera _camera;

        private VideoScriptData _videoScript = null;
        /// <summary>
        /// This property represents the current VideoScriptData for the video.
        /// Setting this property also sets the logo position in the logo overlay shader.
        /// </summary>
        public VideoScriptData VideoScript
        {
            get
            {
                return _videoScript;
            }
            set
            {
                _videoScript = value;
                
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
        
        //TODO factor out or do a playMode == Standby check? May be safer to keep it...
        public bool IsPlaying { get; private set; } //TODO factor out
        
        /// <summary>
        /// Sets playback to paused or unpaused if the current playMode permits.
        /// Note this is not related to IsPlaying.
        /// </summary>
        public bool IsPaused
        {
            get
            {
                return playMode is PlayMode.PreviewPaused or PlayMode.ExportPaused;
            }
            set
            {
                playMode = (playMode, value) switch
                {
                    (PlayMode.Preview, true) => PlayMode.PreviewPaused,
                    (PlayMode.PreviewPaused, false) => PlayMode.Preview,
                    (PlayMode.ExportPlayback, true) => PlayMode.ExportPaused,
                    (PlayMode.ExportPaused, false) => PlayMode.ExportPlayback,
                    _ => playMode,
                };
            }
        }

        private float _previewQuality = 1f;
        public float PreviewQuality
        {
            get
            {
                return _previewQuality;
            }
            set
            {
                _previewQuality = value;
                if (IsPlayModePreview)
                {
                    SetVideoResolutionScale(_previewQuality);
                }
            }
        }

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

        private float _FPSWarnThreshold = 20.0f;
        
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

        void Update()
        {
            if (IsPlaying)
            {
                float FPS = 1.0f / Time.unscaledDeltaTime;
                if (FPS < _FPSWarnThreshold)
                {
                    Debug.LogWarning($"FPS is {FPS}, lower than the expected minimum of {_FPSWarnThreshold}!");
                }
            }
        }
        
        /// <summary>
        /// Sets the resolution of the render material for the camera (and thus the video preview) to a factor <c>scale</c> of the maximum resolution as defined in the VideoScriptData. </summary>
        /// <param name="scale">The fraction used to scale the resolution defined in the VideoScriptData.</param>
        private void SetVideoResolutionScale(float scale = 1f)
        {
            if (_videoScript is null)
            {
                return;
            }
            RenderTexture tex = _camera.targetTexture;
            tex.Release();
            tex.height = (int)(_videoScript.Height * scale);
            tex.width = (int)(_videoScript.Width * scale);
        }
        
        /// <summary>
        /// Stops playback for either preview or export play modes.
        /// </summary>
        /// <returns> true if playback is stopped or already in standby, false if playback could not be stopped.</returns>
        public bool StopPlayback()
        {
            if (playMode == PlayMode.ExportCompile)
            {
                return false;
            }

            playMode = PlayMode.Standby;
            return true;
        }
        
        /// <summary>
        /// Coroutine used for the desktop preview mode (the video is not recorded).
        /// The framerate of the preview matches the update loop.
        /// </summary>
        /// <returns></returns>
        IEnumerator Preview()
        {
            if (VideoScript is null)
            {
                playMode = PlayMode.Standby;
                yield break;
            }

            playMode = PlayMode.Preview;
            
            StartPlayback();

            while (_time < VideoScript.Duration)
            {
                if (playMode == PlayMode.PreviewPaused)
                {
                    yield return null;
                    continue;
                }

                if (playMode == PlayMode.Standby)
                {
                    break;
                }
                
                OnPlaybackUpdated(_time / VideoScript.Duration);
                UpdatePlayback(Time.deltaTime);
                yield return null;
            }

            EndPlayback();
        }
        
        /// <summary>
        /// Coroutine to preview and export the video.
        /// This will initiate a thread which is used to export video frames and call FFmpeg to compile the video.
        /// Each frame of the update loop is used to render a single frame for the video, thus the video preview may progress at a different speed to the output.
         /// </summary>
        /// <returns></returns>
        IEnumerator Export()
        {
            if (VideoScript is null)
            {
                playMode = PlayMode.ExportPlayback;
                yield break;
            }

            playMode = PlayMode.ExportPlayback;
            
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
                //TODO use events rather than polling?
                if (playMode == PlayMode.ExportPaused)
                {
                    yield return null;
                    continue;
                }

                if (playMode == PlayMode.Standby)
                {
                    break;
                }
                
                OnPlaybackUpdated(_frameCounter / (float)_frameTotal);
                UpdatePlayback(deltaTime);
                yield return null;
            }

            _camera.enabled = false;
            _captureFrames = false;
            _terminateThreadWhenDone = true;
            
            playMode = playMode != PlayMode.Standby ? PlayMode.ExportCompile : playMode;
            OnPlaybackUnstoppable(); //In the next few frames pause and stop won't do anything
            
            while (_threadIsProcessing)
            {
                OnPlaybackUpdated(_frameCounter / (float)_frameTotal);
                yield return null;
            }

            playMode = PlayMode.Standby;
            EndPlayback();
        }
        
        /// <summary>
        /// Updates the playback for the current video by a given time <c>deltaTime</c>.
        /// </summary>
        /// <param name="deltaTime">How much time to progress the video.</param>
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
        /// <summary>
        /// Update the given <c>action</c> by an amount of time <c>deltaTime</c> and switch to the next action in the <c>actionQueue</c> if the current action is completed.
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <param name="time"></param>
        /// <param name="action"></param>
        /// <param name="actionQueue"></param>
        /// <typeparam name="T"></typeparam>
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

        /// <summary>
        /// Set the transform of the camera given a <c>position</c> and directions.
        /// </summary>
        /// <param name="position">New position of the camera.</param>
        /// <param name="direction">Direction the camera should look in.</param>
        /// <param name="upDirection">The up direction used for the camera look-at. This will not necessarily be the exact up direction of the new camera transform.</param>
        private void UpdateTransform(Vector3 position, Vector3 direction, Vector3 upDirection)
        {
            position = _cubeTransform.TransformPoint(position);
            direction = _cubeTransform.TransformDirection(direction);
            upDirection = _cubeTransform.TransformDirection(upDirection);
            
            gameObject.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction, upDirection));
        }
        
        /// <summary>
        /// Sets various variables in preparation for video playback.
        /// </summary>
        public void StartPlayback()
        {
            SetVideoResolutionScale(IsPlayModePreview ? _previewQuality : 1f);
            
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
        
        /// <summary>
        /// Sets variables to reflect the not playing state and emits a signal.
        /// </summary>
        public void EndPlayback()
        {
            _camera.enabled = false;
            IsPlaying = false;
            OnPlaybackFinished();
        }
        
        /// <summary>
        /// Method used to invoke the <c>PlaybackUpdated</c> event.
        /// </summary>
        /// <param name="progress">The fraction of the video playback that has progressed.</param>
        protected virtual void OnPlaybackUpdated(float progress)
        {
            PlaybackUpdated?.Invoke(this, new PlaybackUpdatedEventArgs(){Progress = progress});
        }
        
        /// <summary>
        /// Method used to invoke the <c>PlaybackFinished</c> event.
        /// </summary>
        protected virtual void OnPlaybackFinished()
        {
            PlaybackFinished?.Invoke(this, EventArgs.Empty);
            // TODO: send a message back to the UI manager to configure the UI again.
            // Something like VideoUiManager.ConfigureUIForPreview(false);
        }

        /// <summary>
        /// Method used to invoke the <c>PlaybackUnstoppable</c>.
        /// </summary>
        protected virtual void OnPlaybackUnstoppable()
        {
            PlaybackUnstoppable?.Invoke(this, EventArgs.Empty);
        }
        
        /// <summary>
        /// Method used to start the <c>Preview</c> coroutine.
        /// </summary>
        public void StartPreview()
        {
            if (IsPlaying)
            {
                return;
            }
            StartCoroutine(Preview());
        }
        
        /// <summary>
        /// Method used to start the <c>Export</c> coroutine.
        /// </summary>
        public void StartExport()
        {
            if (IsPlaying)
            {
                return;
            }
            StartCoroutine(Export());
        }
        
        /// <summary>
        /// Method used to override the camera render.
        /// Places the logo on the render texture.
        /// If exporting, then a higher-fidelity, linear color setting is used for the render texture to preserve the color values on export.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
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

        /// <summary>
        /// This method is called on a separate thread when exporting.
        /// Exports frames to images on disk and compiles these into a video using FFmpeg once all frames have been captured.
        /// </summary>
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
            
            //If playback has been stopped, this must be cancelled
            if (playMode == PlayMode.ExportCompile)
            {
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
            }
            
            _threadIsProcessing = false;
            Directory.Delete(_framePath, recursive: true);
        }
    }
}