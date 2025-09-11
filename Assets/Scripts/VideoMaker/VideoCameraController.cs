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
        private const string ExportMessage = "Exporting Video";
        private const string PreviewMessage = "Preview Video";

        private const int FfmpegConsoleCount = 58; //From limited observation, this is how many console messages are printed by ffmpeg

        public TextAsset JsonSchema;
        private VideoScriptReader _vsReader;

        public TMP_Text VideoScriptFilePath;

        // TODO use a different MonoBehaviour to manage the playback and status bar?
        public GameObject ProgressBar;
        public TMP_Text StatusText;

        public GameObject VideoView;
        private RectTransform _videoDisplayRect;
        private Vector2 _videoDisplaySizeDelta;

        private Camera _camera;

        private VideoScriptData _videoScript = null;

        private bool _isPlaying = false;

        private int _frameCounter = 0;
        private int _frameTotal = 0;
        private int _frameDigits = 0;
        private bool _captureFrames = false;
        private Queue<byte[]> _frameQueue = new();

        private Thread _exportThread;
        private bool _threadIsProcessing;
        private bool _terminateThreadWhenDone;

        private string _framePath;
        private string _logoPath;
        private string _ffmpegPath;

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
            _vsReader = new(JsonSchema.text);

            ProgressBar.SetActive(false);
            StatusText.gameObject.SetActive(false);

            _videoDisplayRect = VideoView.transform.Find("VideoDisplay").gameObject.GetComponent<RectTransform>();
            _videoDisplaySizeDelta = _videoDisplayRect.sizeDelta;

            var directory = new DirectoryInfo(Application.dataPath);
            _framePath = System.IO.Path.Combine(directory.Parent.FullName, "Outputs/Video");
            _logoPath = System.IO.Path.GetRelativePath(_framePath, System.IO.Path.Combine(Application.streamingAssetsPath, "logo.png"));

            if (!Directory.Exists(_framePath))
            {
                Directory.CreateDirectory(_framePath);
            }

            _camera = GetComponent<Camera>();
            _camera.enabled = false;
        }

        //Taken from CanvassDesktop.BrowseImageFile
        public void BrowseVideoScriptFile()
        {
            string lastPath = PlayerPrefs.GetString("LastPathVideo");
            if (!Directory.Exists(lastPath))
            {
                lastPath = "";
            }
            var extensions = new[]{
                new ExtensionFilter("Video scripts ", "json", "idvs"),
                new ExtensionFilter("All Files ", "*"),
            };
            StandaloneFileBrowser.OpenFilePanelAsync("Open File", lastPath, extensions, false, (string[] paths) =>
            {
                if (paths.Length == 1)
                {
                    PlayerPrefs.SetString("LastPathVideo", System.IO.Path.GetDirectoryName(paths[0]));
                    PlayerPrefs.Save();

                    LoadVideoScriptFile(paths[0]);
                    VideoScriptFilePath.text = System.IO.Path.GetFileName(paths[0]);
                }
            });
        }

        private enum FfmpegTestResults
        {
            Valid,
            NoExe,
            NotFffmpeg,
            ExeError,
        }

        private FfmpegTestResults TestFfmpegExe()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _ffmpegPath,
                        Arguments = "-version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Check output for a recognizable ffmpeg version string
                if (output.StartsWith("ffmpeg version", StringComparison.OrdinalIgnoreCase))
                {
                    return FfmpegTestResults.Valid;
                }
                return FfmpegTestResults.NotFffmpeg;

            }
            catch
            {
                return FfmpegTestResults.ExeError;
            }
        }

        public void ValidateFfmpegPath()
        {
            _ffmpegPath = PlayerPrefs.GetString("FfmpegPath");

            if (File.Exists(_ffmpegPath) && TestFfmpegExe() == FfmpegTestResults.Valid)
            {

                StartCoroutine(Export());
                return;
            }
            else
            {
                _ffmpegPath = "";
            }

            var extensions = new[]{
                new ExtensionFilter("Executable Files", "exe"),
                new ExtensionFilter("All Files", "*")
            };

            StandaloneFileBrowser.OpenFilePanelAsync("Open the FFmpeg executable", "", extensions, false, (string[] paths) =>
            {
                if (paths.Length == 1)
                {
                    _ffmpegPath = paths[0];
                    
                    switch (string.IsNullOrEmpty(_ffmpegPath) ? FfmpegTestResults.NoExe : TestFfmpegExe())
                    {
                        case FfmpegTestResults.Valid:
                            StatusText.gameObject.SetActive(false);
                            break;
                        case FfmpegTestResults.NoExe:
                            StatusText.text = "No FFmpeg exe selected. Please try again.";
                            StatusText.gameObject.SetActive(true);
                            return;
                        case FfmpegTestResults.NotFffmpeg:
                            StatusText.text = "Exe selected is not FFmpeg. Please try again.";
                            StatusText.gameObject.SetActive(true);
                            return;
                        case FfmpegTestResults.ExeError:
                            StatusText.text = "Exe selected executed with errors. Please try again.";
                            StatusText.gameObject.SetActive(true);
                            return;
                    }

                    PlayerPrefs.SetString("FfmpegPath", _ffmpegPath);
                    PlayerPrefs.Save();
                    StartCoroutine(Export());
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
                // Check file extension to decide on which reader to use
                string fileExtension = System.IO.Path.GetExtension(path);
                switch (fileExtension)
                {
                    case ".json":
                        string jsonString = reader.ReadToEnd();
                        _videoScript = _vsReader.ReadJSONVideoScript(jsonString);
                        break;
                    case ".idvs":
                        _videoScript = _vsReader.ReadIDVSVideoScript(reader);
                        break;
                    default:
                        Debug.LogError("Selected file is not of an appropriate type!");
                        return;
                }
                //TODO use async?
            }

            if (_videoScript is null)
            {
                UnityEngine.Debug.LogWarning("VideoScript failed to construct actions.");
                return;
            }
            //Setting RenderMaterial properties

            RenderTexture tex = GetComponent<Camera>().targetTexture;
            tex.Release();
            tex.height = _videoScript.Height;
            tex.width = _videoScript.Width;

            if (_videoScript.Height / (float)_videoScript.Width > _videoDisplaySizeDelta.y / _videoDisplaySizeDelta.x)
            {
                _videoDisplayRect.sizeDelta = new Vector2(
                    _videoDisplaySizeDelta.x * _videoScript.Width / (float)_videoScript.Height,
                    _videoDisplaySizeDelta.y
                    );
            }
            else
            {
                _videoDisplayRect.sizeDelta = new Vector2(
                    _videoDisplaySizeDelta.x,
                    _videoDisplaySizeDelta.y * _videoScript.Height / (float)_videoScript.Width
                );
            }
        }

        IEnumerator Preview()
        {
            if (_videoScript is null)
            {
                yield break;
            }

            StartPlayback(PreviewMessage);

            while (_time < _videoScript.Duration)
            {
                ProgressBar.GetComponent<Slider>().value = _time / _videoScript.Duration;
                UpdatePlayback(Time.deltaTime);
                yield return null;
            }

            EndPlayback();
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
            foreach (FileInfo file in new DirectoryInfo(_framePath).EnumerateFiles())
            {
                file.Delete();
            }

            _frameCounter = 0;

            _terminateThreadWhenDone = false;
            _threadIsProcessing = true;
            _exportThread = new Thread(SaveFrames);
            _exportThread.Start();
            //TODO: Does the Thread terminate when callback is complete?

            StartPlayback(ExportMessage);

            _frameTotal = (int)((float)_videoScript.FrameRate * _videoScript.Duration);
            _frameDigits = (int)Mathf.Floor(Mathf.Log10(_frameTotal) + 1);
            _frameTotal += FfmpegConsoleCount;

            _captureFrames = true;

            float deltaTime = 1f / (float)_videoScript.FrameRate;
            // _frameDigits = 

            while (_time < _videoScript.Duration)
            {
                ProgressBar.GetComponent<Slider>().value = _frameCounter / (float)_frameTotal;
                UpdatePlayback(deltaTime);
                yield return null;
            }

            _camera.enabled = false;
            _captureFrames = false;
            _terminateThreadWhenDone = true;

            //TODO check if status changes to Export and change text on progress bar
            while (_threadIsProcessing)
            {
                ProgressBar.GetComponent<Slider>().value = _frameCounter / (float)_frameTotal;
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

            gameObject.transform.position = position;
            gameObject.transform.LookAt(position + direction, upDirection);
        }

        public void StartPlayback(string message)
        {
            StatusText.text = message;
            
            _camera.enabled = true;
            VideoView.SetActive(true);
            _isPlaying = true;

            GameObject volume = GameObject.Find("VolumeDataSetManager");

            //This shouldn't be possible anymore
            // if (volume is not null)
            // {
            _cubeTransform = volume.transform.Find("CubePrefab(Clone)");
            // }

            _positionQueue = new(_videoScript.PositionActions);
            _directionQueue = new(_videoScript.DirectionActions);
            _upDirectionQueue = new(_videoScript.UpDirectionActions);

            _positionAction = _positionQueue.Dequeue();
            _directionAction = _directionQueue.Dequeue();
            _upDirectionAction = _upDirectionQueue.Dequeue();

            ProgressBar.SetActive(true);
            StatusText.gameObject.SetActive(true);

            _time = 0f;
            _positionTime = 0f;
            _directionTime = 0f;
            _upDirectionTime = 0f;
        }

        public void EndPlayback()
        {
            _camera.enabled = false;
            ProgressBar.SetActive(false);
            StatusText.gameObject.SetActive(false);
            VideoView.SetActive(false);
            _isPlaying = false;
        }

        public void OnPreviewClick()
        {
            if (_isPlaying)
            {
                return;
            }
            StartCoroutine(Preview());
        }

        public void OnExportClick()
        {
            if (_isPlaying)
            {
                return;
            }
            ValidateFfmpegPath();
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

            string overlay = $"-i {_logoPath} -filter_complex \"[1:v]scale=iw*{_videoScript.LogoScale:F}:-1[logo];[0:v][logo]overlay=W-w-10:H-h-10\" ";
            
            

            string command = $"-framerate {_videoScript.FrameRate} -i frame%0{_frameDigits}d.png {overlay}-c:v libx264 -pix_fmt yuv420p video.mp4";

            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = command,
                WorkingDirectory = _framePath,
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
        }
    }
}