using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        private const float Dist = 1.5f;

        private VideoPositionAction[] _positionActionArray = {
            new VideoPositionActionHold(0.5f, new Vector3(0f, 0f, -2 * Dist)),
            new VideoPositionActionPath(0.5f, new LinePath(new Vector3(0f, 0f, -2 * Dist), new Vector3(0f, 0f, -Dist))),
            new VideoPositionActionHold(0.5f, new Vector3(0f, 0f, -Dist)),
            new VideoPositionActionPath(0.5f, new LinePath(new Vector3(0f, 0f, -Dist), new Vector3(-Dist, 0f, -Dist))),
            new VideoPositionActionHold(0.5f, new Vector3(-Dist, 0f, -Dist)),
            new VideoPositionActionPath(0.5f, new LinePath(new Vector3(-Dist, 0f, -Dist), new Vector3(-Dist, 0f, 0f)))
        };
        private VideoDirectionAction[] _directionActionArray = {
            new VideoDirectionActionLookAt(4f, new Vector3(0f, 0f, 0f))
        };
        private VideoDirectionAction[] _upDirectionActionArray= { 
            new VideoDirectionActionHold(4f, Vector3.up)
        };

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

            //TODO: For preview mode use Time.deltaTime, for recordings use the defined frame time of the VideoScript
            _time += Time.deltaTime;
            _timePosition += Time.deltaTime;
            _timeDirection += Time.deltaTime;
            _timeUpDirection += Time.deltaTime;

            //TODO Reduce copy-and-paste (use Zip and refs)
            if (_timePosition > _positionActionQueue[0].Duration)
            {
                if (_positionActionQueue.Count <= 1)
                {
                    enabled = false;
                    ProgressBar.SetActive(false);
                    return;
                }
                else
                {
                    _positionActionQueue.RemoveAt(0);
                    _timePosition = 0f;
                }
            }

            if (_timeDirection > _directionActionQueue[0].Duration)
            {
                if (_directionActionQueue.Count <= 1)
                {
                    enabled = false;
                    ProgressBar.SetActive(false);
                    return;
                }
                else
                {
                    _directionActionQueue.RemoveAt(0);
                    _timeDirection = 0f;
                }
            }
            
            if (_timeUpDirection > _upDirectionActionQueue[0].Duration)
            {
                if (_upDirectionActionQueue.Count <= 1)
                {
                    enabled = false;
                    ProgressBar.SetActive(false);
                    return;
                }
                else
                {
                    _upDirectionActionQueue.RemoveAt(0);
                    _timeUpDirection = 0f;
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

        public void OnPreviewClick()
        {
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
            StatusText.text = _statusLabels[Status.PreviewPlayback];
            ProgressBar.SetActive(true);

            _time = 0f;
            _timePosition = 0f;
            _timeDirection = 0f;
            _timeUpDirection = 0f;
        }
    }
}