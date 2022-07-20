/*
 * Copyright (c) 2021 AGT Group (R&D) GmbH. All rights reserved.
 * Redistribution and use in source and binary form without the express
 * permission of the above copyright holder is not permitted.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System;

namespace CoachAiEngine.Animation.AvatarAnimation {

    class GraphNode {
        protected GraphNode parentNode;
        protected List<GraphNode> children;

        public GraphNode() {
            parentNode = null;
            children = new List<GraphNode>();
        }

        ~GraphNode() {
            children.Clear();
        }

        public virtual void Update() {
            foreach (GraphNode node in children) {
                node.Update();
            }
        }

        public void AddChild(GraphNode _node) {
            children.Add(_node);
            _node.setParent(this);
        }

        public bool IsLeaf() {
            return children.Count == 0;
        }

        public bool IsRoot() {
            return parentNode == null;
        }

        public GraphNode GetFirstChild() {
            Assert.IsFalse(IsLeaf());
            return children[0];
        }

        protected void setParent(GraphNode _node) {
            parentNode = _node;
        }

        virtual public string ToString(string _preString, string _format = null) {
            string s = "";
            return s;
        }
    }

    class DOFNode : GraphNode {
        protected Vector3 globalPosition;

        protected Quaternion rotation;
        protected Vector3 position;

        public DOFNode() {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            globalPosition = Vector3.zero;
        }

        public DOFNode(Quaternion _rotation) {
            position = Vector3.zero;
            rotation = _rotation;
            globalPosition = Vector3.zero;
        }

        public DOFNode(Vector3 _position) {
            position = _position;
            rotation = Quaternion.identity;
            globalPosition = Vector3.zero;
        }

        public DOFNode(Vector3 _position, Quaternion _rotation) {
            position = _position;
            rotation = _rotation;
            globalPosition = Vector3.zero;
        }

        ~DOFNode() { }

        public void SetPosition(Vector3 _position) {
            position = _position;
        }

        public Vector3 GetPosition() {
            return position;
        }

        public void SetRotation(Quaternion _rotation) {
            rotation = _rotation;
        }

        public void SetGlobalPosition(Vector3 _globalPosition) {
            globalPosition = _globalPosition;
        }

        public Vector3 GetGlobalPosition() {
            return globalPosition;
        }

        public Quaternion GetRotation() {
            return rotation;
        }

        public void UpdateGlobalPosition(Vector3 _offset) {
            Quaternion rot = Quaternion.identity;
            if (!IsRoot()) {
                rot = ((DOFNode) parentNode).GetRotation();
            }

            globalPosition = _offset + rot * position;
            foreach (DOFNode node in children) {
                node.UpdateGlobalPosition(globalPosition);
            }
        }

        override public string ToString(string _preString, string _format = null) {
            string s = "";
            s += _preString + "\tPosition: " + GetPosition().ToString(_format) +
                 "\tGlobalPosition: " + GetGlobalPosition().ToString(_format) +
                 "\tRotation: " + GetRotation().eulerAngles.ToString(_format) + "\n";
            foreach (GraphNode node in children) {
                s += node.ToString(_preString + "\t", _format);
            }

            return s;
        }
    }

    class JointNode : DOFNode {
        private string name;
        private Vector3 initialDirectionToFirstChild;
        private Vector3 targetPosition;

        public JointNode(string _name) {
            name = _name;
            initialDirectionToFirstChild = Vector3.zero;
            targetPosition = Vector3.zero;
        }

        public JointNode(string _name, Vector3 _position, Vector3 _direction) {
            name = _name;
            targetPosition = _position;
            initialDirectionToFirstChild = _direction;
        }

        public void ComputeNodeLocals() {
            TranslateNodeAndAllChildren(targetPosition);
            Assert.IsFalse(IsLeaf());
            Quaternion r = Quaternion.FromToRotation(initialDirectionToFirstChild,
                ((JointNode) GetFirstChild()).targetPosition);
            SetRotation(r);
            RotateNodeAndAllChildren(Quaternion.Inverse(r));
            foreach (JointNode node in children) {
                if (!node.IsLeaf()) {
                    node.ComputeNodeLocals();
                }
            }

            UpdateGlobalPosition(Vector3.zero);
        }

        public void TranslateNodeAndAllChildren(Vector3 _t) {
            targetPosition = targetPosition - _t;
            SetPosition(_t);
            foreach (JointNode node in children) {
                node.TranslateNodeAndAllChildren(_t);
            }
        }

        public void RotateNodeAndAllChildren(Quaternion _r) {
            targetPosition = _r * targetPosition;
            foreach (JointNode node in children) {
                node.RotateNodeAndAllChildren(_r);
            }
        }

        override public string ToString(string _preString, string _format = null) {
            string s = "";
            s += _preString + "name: " + name + "\tPosition: " + GetPosition().ToString(_format) +
                 "\tGlobalPosition: " + GetGlobalPosition().ToString(_format) +
                 "\tRotation: " + GetRotation().eulerAngles.ToString(_format) +
                 "\tinitDirection: " + initialDirectionToFirstChild.ToString(_format) +
                 "\ttargetPosition: " + targetPosition.ToString(_format) + "\n";
            foreach (GraphNode node in children) {
                s += node.ToString(_preString + "\t", _format);
            }

            return s;
        }
    }

    struct Auxiliary {
        public static Tuple<Vector3, Quaternion, Vector3> GetTRSFromMatrix4x4(Matrix4x4 _matrix) {
            Assert.IsTrue(_matrix.ValidTRS());

            // Extract new local position
            Vector3 position = _matrix.GetColumn(3);

            // Extract new local rotation
            Quaternion rotation = Quaternion.LookRotation(
                _matrix.GetColumn(2),
                _matrix.GetColumn(1)
            );

            // Extract new local scale
            Vector3 scale = new Vector3(
                _matrix.GetColumn(0).magnitude,
                _matrix.GetColumn(1).magnitude,
                _matrix.GetColumn(2).magnitude
            );

            return new Tuple<Vector3, Quaternion, Vector3>(position, rotation, scale);
        }
    }

}
