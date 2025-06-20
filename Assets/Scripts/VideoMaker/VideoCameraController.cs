using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

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
        public GameObject ProgressBar;

        public TMP_Text StatusText;

        private Camera _camera;

        private const float Dist = 1.5f;

        private VideoPositionAction[] _positionActionArray = {
            new VideoPositionActionHold(0.5f, new Vector3(0f, 0f, -2 * Dist)),
            new VideoPositionActionPath(0.5f, new LinePath(new Vector3(0f, 0f, -2 * Dist), new Vector3(0f, 0f, -Dist))),
            new VideoPositionActionHold(0.5f, new Vector3(0f, 0f, -Dist)),
            new VideoPositionActionPath(0.5f, new LinePath(new Vector3(0f, 0f, -Dist), new Vector3(-Dist, 0f, -Dist))),
            new VideoPositionActionHold(0.5f, new Vector3(-Dist, 0f, -Dist)),
            new VideoPositionActionPath(0.5f, new LinePath(new Vector3(-Dist, 0f, -Dist), new Vector3(-Dist, 0f, 0f))),
            new VideoPositionActionHold(0.5f, new Vector3(-Dist, 0f, 0f)),
            new VideoPositionActionPath(0.5f, new LinePath(new Vector3(-Dist, 0f, 0f), new Vector3(-Dist,  Dist, 0f))),
            new VideoPositionActionHold(0.5f, new Vector3(-Dist, Dist, 0f)),
            new VideoPositionActionPath(0.5f, new LinePath(new Vector3(-Dist, Dist, 0f), new Vector3(0f, Dist, 0f)))
        };
        private VideoDirectionAction[] _directionActionArray = {
            new VideoDirectionActionLookAt(5f, new Vector3(0f, 0f, 0f))
        };
        private VideoDirectionAction[] _upDirectionActionArray = {
            new VideoDirectionActionHold(4.5f, Vector3.up),
            new VideoDirectionActionTween(0.5f, Vector3.up, new Vector3(1f, 0f, 0f))
        };

        private int _frameRate = 10;
        private int _frameCounter = 0;
        private Queue<byte[]> _frameQueue = new();

        private Thread _exportThread;
        private bool _threadIsProcessing;
        private bool _terminateThreadWhenDone;

        private string _directoryPath;

        private List<VideoPositionAction> _positionActionQueue;
        private List<VideoDirectionAction> _directionActionQueue;
        private List<VideoDirectionAction> _upDirectionActionQueue;

        private GameObject _targetCube;
        private Transform _cubeTransform;

        private float _duration = 0f;
        private float _time = 0f;
        private float _timePosition = 0f;
        private float _timeDirection = 0f;
        private float _timeUpDirection = 0f;

        void Awake()
        {
            ProgressBar.SetActive(false);
            enabled = false;
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

        void Update()
        {
            ProgressBar.GetComponent<Slider>().value = _time / _duration;

            Vector3 position = _positionActionQueue[0].GetPosition(_timePosition);

            UpdateTransform(
                position,
                _directionActionQueue[0].GetDirection(_timeDirection, position),
                _upDirectionActionQueue[0].GetDirection(_timeUpDirection, position)
            );

            float deltaTime = 0f;

            switch (_status)
            {
                case Status.PreviewPlayback:
                    deltaTime = Time.deltaTime;
                    break;
                case Status.ExportPlayback:
                    deltaTime = 1f / (float)_frameRate;
                    //Write texture to queue. Should I wait till end of frame?
                    // _camera.Render();
                    // // Make a new texture and read the active Render Texture into it.
                    // Texture2D tex = new Texture2D(_camera.targetTexture.width, _camera.targetTexture.height, TextureFormat.RGB24, false);
                    // tex.ReadPixels(new Rect(0, 0, _camera.targetTexture.width, _camera.targetTexture.height), 0, 0);
                    // tex.Apply();
                    // // Encode texture into PNG
                    // byte[] bytes = tex.EncodeToPNG();
                    // Destroy(tex); //Is it better to re-use the texture each frame?
                    // _frameQueue.Enqueue(bytes);
                    break;
            }
            
            _time += deltaTime;

            //TODO is a final frame necessary?
            if (!UpdateActionTime(ref _timePosition, ref _positionActionQueue, deltaTime))
            {
                TaskComplete();
                return;
            }

            if (!UpdateActionTime(ref _timeDirection, ref _directionActionQueue, deltaTime))
            {
                TaskComplete();
                return;
            }

            if (!UpdateActionTime(ref _timeUpDirection, ref _upDirectionActionQueue, deltaTime))
            {
                TaskComplete();
                return;
            }
        }

        //TODO how to deal with type casting so I can just define one method?
        private bool UpdateActionTime(ref float time, ref List<VideoPositionAction> actionQueue, float deltaTime)
        {
            time += deltaTime;

            if (time > actionQueue[0].Duration)
            {
                if (actionQueue.Count <= 1)
                {
                    return false;
                }
                else
                {
                    time -= actionQueue[0].Duration;
                    actionQueue.RemoveAt(0);
                }
            }
            return true;
        }

        private bool UpdateActionTime(ref float time, ref List<VideoDirectionAction> actionQueue, float deltaTime)
        {
            time += deltaTime;

            if (time > actionQueue[0].Duration)
            {
                if (actionQueue.Count <= 1)
                {
                    return false;
                }
                else
                {
                    time -= actionQueue[0].Duration;
                    actionQueue.RemoveAt(0);
                }
            }
            return true;
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

            _positionActionQueue = new(_positionActionArray);
            _directionActionQueue = new(_directionActionArray);
            _upDirectionActionQueue = new(_upDirectionActionArray);


            //Calculating the overall duration - this should be done somewhere else. Also use less copy-and-paste
            float duration = 0f;
            foreach (VideoCameraAction action in _positionActionQueue)
            {
                duration += action.Duration;
            }
            _duration = duration;

            foreach (VideoCameraAction action in _directionActionQueue)
            {
                duration += action.Duration;
            }
            _duration = Math.Min(_duration, duration);

            foreach (VideoCameraAction action in _upDirectionActionQueue)
            {
                duration += action.Duration;
            }
            _duration = Math.Min(_duration, duration);

            enabled = true;
            ProgressBar.SetActive(true);

            _time = 0f;
            _timePosition = 0f;
            _timeDirection = 0f;
            _timeUpDirection = 0f;
        }

        public void OnPreviewClick()
        {
            if (_status != Status.Idle)
            {
                return;
            }
            _status = Status.PreviewPlayback;
            _camera.enabled = true;
            StartPlayback();
        }

        public void OnRecordClick()
        {
            if (_status != Status.Idle)
            {
                return;
            }
            // Kill the encoder thread if running from a previous execution
            if (_exportThread != null && (_threadIsProcessing || _exportThread.IsAlive)) {
                _threadIsProcessing = false;
                _exportThread.Join();
            }

                _status = Status.ExportPlayback;
            _frameCounter = 0;
            _terminateThreadWhenDone = false;
            _threadIsProcessing = true;
            _exportThread = new Thread(SaveFrames);
            _exportThread.Start();
            //TODO: Does the Thread terminate when callback is complete?
            
            _camera.enabled = true;

            StartPlayback();
        }

        private void TaskComplete() {
            switch (_status)
            {
                case Status.PreviewPlayback:
                    enabled = false;
                    _camera.enabled = false;
                    ProgressBar.SetActive(false);
                    _status = Status.Idle;
                    break;
                case Status.ExportPlayback:
                    enabled = false;
                    _camera.enabled = false;
                    _terminateThreadWhenDone = true;
                    if (!_threadIsProcessing)
                    {
                        ProgressBar.SetActive(false);
                        _status = Status.Idle; //TODO move to next stage of export
                    }
                    break;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination);
            if (_status != Status.ExportPlayback)
            {
                return;
            }

            // Make a new texture and read the active Render Texture into it.
            Texture2D tex = new Texture2D(destination.width, destination.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, destination.width, destination.height), 0, 0);
            tex.Apply();
            // Encode texture into PNG
            byte[] bytes = tex.EncodeToPNG();
            Destroy(tex); //Is it better to re-use the texture each frame?
            _frameQueue.Enqueue(bytes);

            // Graphics.Blit(source, destination);
        }

        private void SaveFrames()
        {
            while (_threadIsProcessing)
            {
                if (_frameQueue.Count > 0)
                {
                    string path = Path.Combine(
                        _directoryPath,
                        "frame" + _frameCounter.ToString() + ".png" //TODO change to bmp for better video
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

            // _terminateThreadWhenDone = false;
            _threadIsProcessing = false;
            TaskComplete(); //Can only call this on the  main thread, what to do?
        }
    }
}