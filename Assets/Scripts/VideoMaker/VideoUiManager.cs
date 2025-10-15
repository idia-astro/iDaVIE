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
    public class VideoUiManager : MonoBehaviour
    {
        private const string ExportMessage = "Exporting Video";
        private const string PreviewMessage = "Preview Video";
        
        public TMP_Text VideoScriptFilePath;
        
        public GameObject ProgressBar;
        public TMP_Text StatusText;

        public GameObject VideoView;
        private RectTransform _videoDisplayRect;
        private Vector2 _videoDisplaySizeDelta;
        
        private string _ffmpegPath;
        
        private VideoCameraController _cameraController;
        
        void Awake()
        {
            ProgressBar.SetActive(false);
            StatusText.gameObject.SetActive(false);

            _videoDisplayRect = VideoView.transform.Find("VideoDisplay").gameObject.GetComponent<RectTransform>();
            _videoDisplaySizeDelta = _videoDisplayRect.sizeDelta;
            
            _cameraController = GetComponent<VideoCameraController>();
            _cameraController.PlaybackUpdated  += OnPlaybackUpdated;
            _cameraController.PlaybackFinished += OnPlaybackFinshed;
        }
        
        public void OnPreviewClick()
        {
            if (_cameraController.IsPlaying)
            {
                return;
            }
            StartPlayback(PreviewMessage);
            _cameraController.StartPreview();
        }

        public void OnExportClick()
        {
            if (_cameraController.IsPlaying)
            {
                return;
            }
            ValidateFfmpegPath();
        }

        private void StartPlayback(string statusText)
        {
            StatusText.text = statusText;
            VideoView.SetActive(true);
            ProgressBar.SetActive(true);
            StatusText.gameObject.SetActive(true);
        }
        
        private void StartExport()
        {
            StartPlayback(ExportMessage);
            _cameraController.StartExport();
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
            print(_ffmpegPath);
            if (File.Exists(_ffmpegPath) && TestFfmpegExe() == FfmpegTestResults.Valid)
            {
                // StartCoroutine(Export());
                _cameraController.FfmpegPath = _ffmpegPath;
                StartExport();
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
                    _cameraController.FfmpegPath = _ffmpegPath;
                    StartExport();
                    // StartCoroutine(Export());
                }
            });
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
                new ExtensionFilter("Video scripts ", "idvs"), //excluding "json" for now
                new ExtensionFilter("All Files ", "*"),
            };
            StandaloneFileBrowser.OpenFilePanelAsync("Open File", lastPath, extensions, false, (string[] paths) =>
            {
                if (paths.Length == 1)
                {
                    PlayerPrefs.SetString("LastPathVideo", System.IO.Path.GetDirectoryName(paths[0]));
                    PlayerPrefs.Save();

                    LoadVideoScriptFile(paths[0]);
                }
            });
        }
        
        private void LoadVideoScriptFile(string path)
        {
            if (path == null)
            {
                return;
            }
            
            string videoFileName = System.IO.Path.GetFileNameWithoutExtension(path);
            VideoScriptFilePath.text = System.IO.Path.GetFileName(path);

            VideoScriptData videoScript;
            
            using (StreamReader reader = new(path))
            {
                // Check file extension to decide on which reader to use
                string fileExtension = System.IO.Path.GetExtension(path);
                switch (fileExtension)
                {
                    // case ".json":
                    //     string jsonString = reader.ReadToEnd();
                    //     _videoScript = _vsReader.ReadJSONVideoScript(jsonString);
                    //     break;
                    case ".idvs":
                        videoScript = VideoScriptReader.ReadIdvsVideoScript(reader, path);
                        break;
                    default:
                        Debug.LogError("Selected file is not of an appropriate type!");
                        return;
                }
                //TODO use async?
            }

            if (videoScript is null)
            {
                UnityEngine.Debug.LogWarning("VideoScript failed to construct actions.");
                return;
            }

            if (videoScript.Height / (float)videoScript.Width > _videoDisplaySizeDelta.y / _videoDisplaySizeDelta.x)
            {
                _videoDisplayRect.sizeDelta = new Vector2(
                    _videoDisplaySizeDelta.x * videoScript.Width / (float)videoScript.Height,
                    _videoDisplaySizeDelta.y
                    );
            }
            else
            {
                _videoDisplayRect.sizeDelta = new Vector2(
                    _videoDisplaySizeDelta.x,
                    _videoDisplaySizeDelta.y * videoScript.Height / (float)videoScript.Width
                );
            }

            _cameraController.VideoFileName = videoFileName;
            _cameraController.VideoScript = videoScript;
        }

        private void OnPlaybackUpdated(object sender, VideoCameraController.PlaybackUpdatedEventArgs e)
        {
            ProgressBar.GetComponent<Slider>().value = e.Progress;
        }

        private void OnPlaybackFinshed(object sender, EventArgs e)
        {
            ProgressBar.SetActive(false);
            StatusText.gameObject.SetActive(false);
            VideoView.SetActive(false);
        }
    }
}