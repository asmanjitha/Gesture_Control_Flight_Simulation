/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;
using System.Text.RegularExpressions;

namespace CoachAiEngine.Animation.AvatarAnimation {

    public class AvatarPose {
        private IDictionary<string, Tuple<float, Vector3>> jointCollection;
        private IDictionary<string, Tuple<float, Vector3>> jointCollectionSmooth;
        private IDictionary<string, List<Tuple<float, Vector3>>> jointHistoryCollection;

        private const int maxEntriesList = 5;
        private const float spatialRadius = 0.5f;
        public AvatarPose() {
            jointCollection = new Dictionary<string, Tuple<float, Vector3>>();
            jointCollectionSmooth = new Dictionary<string, Tuple<float, Vector3>>();
            jointHistoryCollection = new Dictionary<string, List<Tuple<float, Vector3>>>();
        }

        public void AddJoint(string _name, Vector3 _position, float _score) {
            Assert.IsFalse(jointCollection.ContainsKey(_name));
            jointCollection.Add(_name, new Tuple<float, Vector3>(_score, _position));

            Assert.IsFalse(jointHistoryCollection.ContainsKey(_name));
            Assert.IsFalse(jointCollectionSmooth.ContainsKey(_name));
            List<Tuple<float, Vector3>> posTupleList = new List<Tuple<float, Vector3>>();
            posTupleList.Add(new Tuple<float, Vector3>(_score, _position));
            jointHistoryCollection.Add(_name, posTupleList);
            jointCollectionSmooth.Add(_name, new Tuple<float, Vector3>(_score, _position));
        }

        public void UpdateJoint(string _name, Vector3 _position, float _score) {
            Assert.IsTrue(jointCollection.ContainsKey(_name));
            jointCollection[_name] = new Tuple<float, Vector3>(_score, _position);

            Assert.IsTrue(jointHistoryCollection.ContainsKey(_name));
            Assert.IsTrue(jointCollectionSmooth.ContainsKey(_name));
            jointHistoryCollection[_name].Add(new Tuple<float, Vector3>(_score, _position));
            if (jointHistoryCollection[_name].Count > maxEntriesList) {
                jointHistoryCollection[_name].RemoveAt(0);
            }

            int cnt = 0;
            Vector3 sumVec = Vector3.zero;
            float sumScore = 0f;
            foreach (Tuple<float, Vector3> pos in jointHistoryCollection[_name]) {
                if (Vector3.Distance(pos.Item2, _position) < spatialRadius) {
                    sumVec += pos.Item2;
                    sumScore += pos.Item1;
                    cnt += 1;
                }
            }

            Assert.IsFalse(cnt == 0);
            Assert.IsFalse(cnt > maxEntriesList);
            jointCollectionSmooth[_name] = new Tuple<float, Vector3>(sumScore / (float) cnt, sumVec / (float) cnt);
        }

        public Vector3 GetJoint(string _name) {
            Assert.IsTrue(jointCollection.ContainsKey(_name));
            Assert.IsTrue(jointCollectionSmooth.ContainsKey(_name));
            return jointCollectionSmooth[_name].Item2;
        }
    }

}
