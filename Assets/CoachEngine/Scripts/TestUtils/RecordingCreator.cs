// /*
//  * Copyright (c) 2022 AGT Group (R&D) GmbH. All rights reserved.
//  * Redistribution and use in source and binary form without the express
//  * permission of the above copyright holder is not permitted.
//  */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CoachAiEngine {

    public class RecordingCreator : MonoBehaviour {

        private struct Record {
            public double Timestamp;
            public string Json;
        }

        private List<Record> records = new List<Record>();
        private int recordingDumped;

        public void OnDecisionRequest(ActivityDecisionRequest request) {
            Debug.Log("Decision Request "+request.Id);
        }

        public void OnRawEventRecieved(string @event) {
            records.Add(new Record() {
                Timestamp = Time.realtimeSinceStartup,
                Json = @event
            });
        }

        public void WriteOutRecording() {
            // ensure we are not dumping things twice
            if (System.Threading.Interlocked.Exchange(ref recordingDumped, 1) == 1) return;

            var date = DateTime.Now.ToString("yy-MM-dd-hh-mm");
            var path = Application.persistentDataPath + $"/recording-{date}.coach";

            Debug.Log($"EventController: Write recording of session to {path}");
            StreamWriter writer = new StreamWriter(path, false);
            foreach (var record in records) {
                writer.WriteLine($"{record.Timestamp};{record.Json}");
            }
            writer.Close();
            Debug.Log($"EventController: Recording successfully written out.");
        }
    }

}
