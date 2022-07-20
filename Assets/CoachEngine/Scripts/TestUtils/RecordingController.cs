// /*
//  * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using System;
using System.Collections;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;

namespace CoachAiEngine {

    /**
     * Allows to replay a previously generated recording of an event stream for testing within the Unity editor.
     *
     * <see cref="ActivityEventController"/> for creating a recording.
     */
    [RequireComponent(typeof(CoachActivityController))]
    public class RecordingController : MonoBehaviour {

        [Tooltip("The recording to replay")]
        [SerializeField]
        private TextAsset recording;

        [Tooltip("Whether to loop back to the start of the recording on finish.")]
        [SerializeField] private bool loop = true;

        [Tooltip("Whether to automatically start playing once loaded.")]
        [SerializeField] private bool playOnStart = true;

        [Tooltip("Whether to also play the recording on a device (outside of the editor)")]
        [SerializeField]
#pragma warning disable 414
        private bool playOnDevice = false;
#pragma warning restore 414

        private CoachActivityRuntime _activityRuntime;
        private bool _isPlaying;
        private bool _isFirst = true;
        private float _currentTime = 0;

        private StringReader _reader;

        private void Start() {
            if (playOnStart) StartReplay();
        }

        /**
         * Explicitly trigger the start of a replay.
         */
        public void StartReplay() {
#if !UNITY_EDITOR
            if (!playOnDevice) return;
#endif
            if (null != recording && _isPlaying) return;

            _activityRuntime = GetComponent<CoachActivityController>()?.Runtime;

            if (null == _activityRuntime) {
                Debug.LogWarning($"{nameof(RecordingController)}: Could not find CoachActivityController or runtime is null");
                return;
            }

            _isPlaying = true;

            _reader = new StringReader(recording.text);
            _isFirst = true;
            _currentTime = 0;

            StartCoroutine(PlayRecording());
        }

        public IEnumerator PlayRecording() {
            var line = "";
            while ((line = _reader.ReadLine()) != null) {
                var parsed = Parse(line);
                if (null == parsed) {
                    yield return null;
                } else {
                    var time = parsed.Item1;
                    var json = parsed.Item2;

                    if (_isFirst) {
                        _isFirst = false;
                    } else {
                        var waitFor = time - _currentTime;
                        if (waitFor > 0) {
                            yield return new WaitForSeconds(time - _currentTime);
                        }
                    }

                    _currentTime = time;
                    _activityRuntime.InjectEvent(json);
                }
            }

            _reader.Close();
            _isPlaying = false;

            if (loop) {
                StartReplay();
            }
        }

        [CanBeNull]
        public static Tuple<float, string> Parse(string line) {
            var split = line.Split(new [] { ';' }, 2, StringSplitOptions.None);
            float time;
            if (FloatUtils.TryParseSane(split[0], out time)) {
                return new Tuple<float, string>(time, split[1]);
            }

            Debug.LogWarning($"RecordingController: Could not parse recording line #{line}");

            return null;
        }
    }

}
