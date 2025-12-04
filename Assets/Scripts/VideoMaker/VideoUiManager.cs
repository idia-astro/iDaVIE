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
using System.Linq;

namespace VideoMaker
{
    /// <summary>
    /// This class is responsible for taking inputs from the desktop UI and altering the desktop UI to match the current video playback state.
    /// It is also responsible for interfacing with the VideoCameraController, and thus this script should be attached to a Camera with a VideoCameraController script attached as well.
    /// </summary>
    public class VideoUiManager : MonoBehaviour
    {
        private const string ExportMessage = "Exporting Video";
        private const string PreviewMessage = "Preview Video";
        
        public TMP_Text VideoScriptFilePath;
        
        public GameObject ProgressBar;
        private Slider _progressSlider;
        public TMP_Text StatusText;

        public GameObject PlayContainer;
        public TMP_Text PlayText;
        public GameObject PreviewButton;
        public GameObject ExportButton;
        public GameObject PauseButton;
        private Button _pauseButton;
        public TMP_Text PauseButtonText;
        public GameObject StopButton;
        private Button _stopButton;

        public GameObject PreviewQualityContainer;
        public Slider PreviewQualitySlider;

        public Button FileBrowseButton;
        public Button ReloadButton;

        public GameObject VideoView;
        private RectTransform _videoDisplayRect;
        private Vector2 _videoDisplaySizeDelta;

        private string _ffmpegPath;

        private string _currentFile = "";

        private VideoCameraController _cameraController;

        // TODO: this is the camera controller's purview, should check a state there rather than here.
        private bool _isPaused = false;

        // Left for translation space, in future? Can't recall how that works for unity.
        private const string pauseText = "Pause";
        private const string resumeText = "Resume";
        
        private const string playingText = "Playing";
        private const string pausedText = "Paused";
        
        void Awake()
        {
            ProgressBar.SetActive(false);
            _progressSlider = ProgressBar.GetComponent<Slider>();
            StatusText.gameObject.SetActive(false);

            _videoDisplayRect = VideoView.transform.Find("VideoDisplay").gameObject.GetComponent<RectTransform>();
            _videoDisplaySizeDelta = _videoDisplayRect.sizeDelta;
            
            _cameraController = GetComponent<VideoCameraController>();
            _cameraController.PlaybackUpdated  += OnPlaybackUpdated;
            _cameraController.PlaybackFinished += OnPlaybackFinshed;
            _cameraController.PlaybackUnstoppable += OnPlaybackUnstoppable;

            _pauseButton = PauseButton.GetComponent<Button>();
            _stopButton = StopButton.GetComponent<Button>();
            
            PlayContainer.SetActive(false);
            PreviewQualityContainer.SetActive(false);
            PreviewQualitySlider.value = _cameraController.PreviewQuality;
        }
        
        /// <summary>
        /// Method called when the user clicks the preview button.
        /// </summary>
        public void OnPreviewClick()
        {
            if (_cameraController.IsPlaying)
            {
                return;
            }

            StartPlayback(PreviewMessage, true);
            _cameraController.StartPreview();
        }

        /// <summary>
        /// Method called when the user clicks the export button.
        /// </summary>
        public void OnExportClick()
        {
            if (_cameraController.IsPlaying)
            {
                return;
            }
            ValidateFfmpegPath();
        }

        /// <summary>
        /// Method called when the user clicks the pause button.
        /// </summary>
        public void OnPauseClick()
        {
            _cameraController.IsPaused = !_cameraController.IsPaused;
            PauseButtonText.SetText(_cameraController.IsPaused ? resumeText : pauseText);
            PlayText.text =  _cameraController.IsPaused ? pausedText : playingText;
        }

        /// <summary>
        /// Method called when the user clicks the stop button. Stops the preview from showing.
        /// </summary>
        public void OnStopClick()
        {
            _cameraController.StopPlayback();
        }

        /// <summary>
        /// Method that is called to configure the desktop UI buttons when the preview is started or ended.
        /// </summary>
        /// <param name="isPlaying">True if the video playback is active, false if not.</param>
        /// <param name="isPreview">True if the playback is in preview mode, false if not.</param>
        public void ConfigureUIForPreview(bool isPlaying, bool isPreview)
        {
            PreviewButton.SetActive(!isPlaying);
            ExportButton.SetActive(!isPlaying);
            PauseButtonText.SetText(_cameraController.IsPaused ? resumeText : pauseText);
            PauseButton.SetActive(isPlaying);
            StopButton.SetActive(isPlaying);
            FileBrowseButton.interactable = !isPlaying;
            ReloadButton.interactable = !isPlaying;
            PreviewQualityContainer.SetActive(isPreview);
            PreviewQualitySlider.value = _cameraController.PreviewQuality;
            PlayText.text = playingText;
        }

        /// <summary>
        /// Method called when the value of the preview quality slider changes, used to change the UI in response.
        /// </summary>
        public void OnQualSliderValChanged()
        {
            _cameraController.PreviewQuality = PreviewQualitySlider.value;
        }
		
		/// <summary>
        /// Set-up UI elements for video playback.
        /// </summary>
        private void StartPlayback(string statusText, bool isPreview)
        {
            StatusText.text = statusText;
            VideoView.SetActive(true);
            ProgressBar.SetActive(true);
            StatusText.gameObject.SetActive(true);
            ConfigureUIForPreview(true, isPreview);
        }
        
        /// <summary>
        /// Set UI for export playback mode and start export playback.
		/// This method is called after FFmpeg validation.
        /// </summary>
        private void StartExport()
        {
            StartPlayback(ExportMessage, false);
            _cameraController.StartExport();
        }
        
        private enum FfmpegTestResults
        {
            Valid,
            NoExe,
            NotFffmpeg,
            ExeError,
        }
        
        /// <summary>
        /// Test if FFmpeg executable is actually FFmpeg and functions as expected.
        /// This method assumes the path to the FFmpeg executable is valid.
        /// </summary>
        /// <returns>Result of the test.</returns>
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
        
        /// <summary>
        /// Test if Ffmpeg is valid (first path, then executable) and start export playback if it is.
        /// </summary>
        public void ValidateFfmpegPath()
        {
            _ffmpegPath = PlayerPrefs.GetString("FfmpegPath");
            print(_ffmpegPath);
            if (File.Exists(_ffmpegPath) && TestFfmpegExe() == FfmpegTestResults.Valid)
            {
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
        
        /// <summary>
        /// Open a file browser to find and select the video script file.
        /// 
        /// Taken from CanvassDesktop.BrowseImageFile
        /// </summary>
        public void BrowseVideoScriptFile()
        {
            string lastPath = PlayerPrefs.GetString("LastPathVideo");
            if (!Directory.Exists(lastPath))
            {
                var directory = new DirectoryInfo(Application.dataPath);
                lastPath = System.IO.Path.Combine(directory.Parent.FullName, "Outputs/VideoScripts");

                if (!Directory.Exists(lastPath))
                {
                    //TODO Should I create the directory if it doesn't exist?
                    lastPath = System.IO.Path.Combine(directory.Parent.FullName, "Outputs");
                }
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
                    _currentFile = paths[0];
                    LoadVideoScriptFile(_currentFile);
                }
            });
        }
        
        /// <summary>
        /// Method called when the user presses the reload button on the desktop UI.
        /// </summary>
        public void ReloadVideoScriptFile()
        {
            Debug.Log($"Reloading file {_currentFile}.");
            // TODO: provide visual feedback for user that file was reloaded.
            // Notification text below progress bar, perhaps?
            // Prefab fading script, pointed at the text, perhaps? Gippity provided work seems solid.
            LoadVideoScriptFile(_currentFile);
        }
        
        /// <summary>
        /// Method to load a video script file given the filepath.
        /// </summary>
        /// <param name="path">Filepath of the video script file.</param>
        private void LoadVideoScriptFile(string path)
        {
            if (path == null)
            {
                return;
            }

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
            
            //TODO use proper error messaging when refactoring.
            if (videoScript is null || videoScript.Duration == 0)
            {
                UnityEngine.Debug.LogWarning("VideoScript failed to construct actions.");
                return;
            }
            
            string videoFileName = System.IO.Path.GetFileNameWithoutExtension(path);
            VideoScriptFilePath.text = System.IO.Path.GetFileName(path);
                
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
            PlayContainer.SetActive(true);
        }
        
        /// <summary>
        /// Method connected to the PlabackUpdated event of the VideoCameraController.
        /// Updates the playback progress bar.
        /// </summary>
        /// <param name="sender">Sender of the signal.</param>
        /// <param name="e">EventArgs for the signal, including the progress fraction.</param>
        private void OnPlaybackUpdated(object sender, VideoCameraController.PlaybackUpdatedEventArgs e)
        {
            _progressSlider.value = e.Progress;
        }
        
        /// <summary>
        /// Method connected to the PlaybackFinished event of the VideoCameraController.
        /// Used to update the UI state for no current video playback.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPlaybackFinshed(object sender, EventArgs e)
        {
            ProgressBar.SetActive(false);
            StatusText.gameObject.SetActive(false);
            VideoView.SetActive(false);
            ConfigureUIForPreview(false, false);
            
            //This is in case OnPlaybackUnstoppable was called. Should this go somewhere else?
            _pauseButton.interactable = true;
            _stopButton.interactable = true;
        }
        
        /// <summary>
        /// Method connected to the PlaybackUnstoppable event of the VideoCameraController.
        /// This turns of interactability for the pause/resume and stop button. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPlaybackUnstoppable(object sender, EventArgs e)
        {
            _pauseButton.interactable = false;
            _stopButton.interactable = false;
        }
    }
}