using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;


namespace CoachAiEngine.Animation.AvatarAnimation {

    public class AvatarPoseObject {
        private IDictionary<string, GameObject> jointCollection;
        private List<Tuple<string, string, GameObject>> jointConnection;
        private GameObject rootObject;

        public AvatarPoseObject(GameObject _rootObject = null) {
            rootObject = _rootObject;

            Color colorLeft = new Color(1f, 0f, 0f, 1f);
            Color colorRight = new Color(0f, 1f, 0f, 1f);
            jointCollection = new Dictionary<string, GameObject>();
            jointConnection = new List<Tuple<string, string, GameObject>>();
            jointConnection.Add(new Tuple<string, string, GameObject>("central nose", "left shoulder", GameObject.Instantiate(Resources.Load("Cylinder") as GameObject, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity)));
            jointConnection[jointConnection.Count - 1].Item3.GetComponent<Renderer>().material.color = colorLeft;
            jointConnection.Add(new Tuple<string, string, GameObject>("central nose", "right shoulder", GameObject.Instantiate(Resources.Load("Cylinder") as GameObject, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity)));
            jointConnection[jointConnection.Count - 1].Item3.GetComponent<Renderer>().material.color = colorRight;
            jointConnection.Add(new Tuple<string, string, GameObject>("left shoulder", "right shoulder", GameObject.Instantiate(Resources.Load("Cylinder") as GameObject, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity)));
            jointConnection.Add(new Tuple<string, string, GameObject>("left shoulder", "left hip", GameObject.Instantiate(Resources.Load("Cylinder") as GameObject, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity)));
            jointConnection[jointConnection.Count - 1].Item3.GetComponent<Renderer>().material.color = colorLeft;
            jointConnection.Add(new Tuple<string, string, GameObject>("left shoulder", "left elbow", GameObject.Instantiate(Resources.Load("Cylinder") as GameObject, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity)));
            jointConnection[jointConnection.Count - 1].Item3.GetComponent<Renderer>().material.color = colorLeft;
            jointConnection.Add(new Tuple<string, string, GameObject>("left elbow", "left wrist", GameObject.Instantiate(Resources.Load("Cylinder") as GameObject, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity)));
            jointConnection[jointConnection.Count - 1].Item3.GetComponent<Renderer>().material.color = colorLeft;
            jointConnection.Add(new Tuple<string, string, GameObject>("right shoulder", "right elbow", GameObject.Instantiate(Resources.Load("Cylinder") as GameObject, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity)));
            jointConnection[jointConnection.Count - 1].Item3.GetComponent<Renderer>().material.color = colorRight;
            jointConnection.Add(new Tuple<string, string, GameObject>("right elbow", "right wrist", GameObject.Instantiate(Resources.Load("Cylinder") as GameObject, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity)));
            jointConnection[jointConnection.Count - 1].Item3.GetComponent<Renderer>().material.color = colorRight;
            jointConnection.Add(new Tuple<string, string, GameObject>("right shoulder", "right hip", GameObject.Instantiate(Resources.Load("Cylinder") as GameObject, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity)));
            jointConnection[jointConnection.Count - 1].Item3.GetComponent<Renderer>().material.color = colorRight;
            jointConnection.Add(new Tuple<string, string, GameObject>("left hip", "right hip", GameObject.Instantiate(Resources.Load("Cylinder") as GameObject, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity)));
            jointConnection.Add(new Tuple<string, string, GameObject>("left hip", "left knee", GameObject.Instantiate(Resources.Load("Cylinder") as GameObject, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity)));
            jointConnection[jointConnection.Count - 1].Item3.GetComponent<Renderer>().material.color = colorLeft;
            jointConnection.Add(new Tuple<string, string, GameObject>("left knee", "left ankle", GameObject.Instantiate(Resources.Load("Cylinder") as GameObject, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity)));
            jointConnection[jointConnection.Count - 1].Item3.GetComponent<Renderer>().material.color = colorLeft;
            jointConnection.Add(new Tuple<string, string, GameObject>("right hip", "right knee", GameObject.Instantiate(Resources.Load("Cylinder") as GameObject, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity)));
            jointConnection[jointConnection.Count - 1].Item3.GetComponent<Renderer>().material.color = colorRight;
            jointConnection.Add(new Tuple<string, string, GameObject>("right knee", "right ankle", GameObject.Instantiate(Resources.Load("Cylinder") as GameObject, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity)));
            jointConnection[jointConnection.Count - 1].Item3.GetComponent<Renderer>().material.color = colorRight;
            foreach (var connectionItem in jointConnection) {
                GameObject limb = connectionItem.Item3;
                limb.transform.localScale = new Vector3(.1f, 1f, .1f);
                if (rootObject != null) {
                    limb.transform.SetParent(rootObject.transform);
                }
            }
        }

        public void AddJoint(string _name, Vector3 _position) {
            Assert.IsFalse(jointCollection.ContainsKey(_name));

            GameObject joint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            if(rootObject != null) {
                joint.transform.SetParent(rootObject.transform);
            }
            joint.transform.position = _position;
            joint.transform.localScale = new Vector3(.1f, .1f, .1f);
            jointCollection.Add(_name, joint);

            List<Vector3> posList = new List<Vector3>();
            posList.Add(_position);
        }

        public void UpdateJoint(string _name, Vector3 _position) {
            Assert.IsTrue(jointCollection.ContainsKey(_name));
            jointCollection[_name].transform.position = _position;
        }

        public void UpdateConnections() {
            foreach (var connectionItem in jointConnection) {
                string name1 = connectionItem.Item1;
                string name2 = connectionItem.Item2;
                GameObject limb = connectionItem.Item3;
                Vector3 pos1 = jointCollection[name1].transform.position;
                Vector3 pos2 = jointCollection[name2].transform.position;

                Vector3 pos = Vector3.Lerp(pos1, pos2, 0.5f);
                limb.transform.position = pos;
                limb.transform.up = pos2 - pos1;

                float scaleFactor = 1;
                if (rootObject) {
                    //scaleFactor = 1f/rootObject.transform.localScale.y;
                }

                Vector3 newScale = limb.transform.localScale;

                newScale.y = Vector3.Distance(pos1, pos2) * 0.5f * scaleFactor;
                limb.transform.localScale = newScale;
            }
        }
    }
}
