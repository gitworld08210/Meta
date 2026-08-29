using System.Collections.Generic;
using UnityEngine;

namespace MetaCricket.MotionDetection
{
    /// <summary>
    /// Debug visualization MonoBehaviour that draws a skeleton overlay on the camera feed
    /// using LineRenderer for development and debugging purposes.
    /// </summary>
    public class PoseVisualizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        [Tooltip("The pose provider to visualize.")]
        private PoseProvider _poseProvider;

        [Header("Visualization Settings")]
        [SerializeField]
        [Tooltip("Color of the skeleton lines.")]
        private Color _lineColor = Color.green;

        [SerializeField]
        [Tooltip("Width of the skeleton lines.")]
        private float _lineWidth = 0.005f;

        [SerializeField]
        [Tooltip("Size of joint markers.")]
        private float _jointMarkerSize = 0.02f;

        [SerializeField]
        [Tooltip("Color for joints with high confidence.")]
        private Color _highConfidenceColor = Color.green;

        [SerializeField]
        [Tooltip("Color for joints with low confidence.")]
        private Color _lowConfidenceColor = Color.red;

        [SerializeField]
        [Tooltip("Whether to draw bone connections.")]
        private bool _drawBones = true;

        [SerializeField]
        [Tooltip("Whether to draw joint markers.")]
        private bool _drawJoints = true;

        [SerializeField]
        [Tooltip("Whether the visualizer is active.")]
        private bool _isEnabled = true;

        [SerializeField]
        [Tooltip("Material for line rendering.")]
        private Material _lineMaterial;

        // Bone connections defining the skeleton structure (pairs of joint indices)
        private static readonly (JointType, JointType)[] BoneConnections =
        {
            (JointType.Nose, JointType.LeftShoulder),
            (JointType.Nose, JointType.RightShoulder),
            (JointType.LeftShoulder, JointType.RightShoulder),
            (JointType.LeftShoulder, JointType.LeftElbow),
            (JointType.LeftElbow, JointType.LeftWrist),
            (JointType.RightShoulder, JointType.RightElbow),
            (JointType.RightElbow, JointType.RightWrist),
            (JointType.LeftShoulder, JointType.LeftHip),
            (JointType.RightShoulder, JointType.RightHip),
            (JointType.LeftHip, JointType.RightHip)
        };

        private Dictionary<int, LineRenderer> _boneLineRenderers;
        private Dictionary<JointType, GameObject> _jointMarkers;
        private PoseData _latestPose;

        private void Awake()
        {
            _boneLineRenderers = new Dictionary<int, LineRenderer>();
            _jointMarkers = new Dictionary<JointType, GameObject>();
        }

        private void OnEnable()
        {
            if (_poseProvider != null)
            {
                _poseProvider.OnPoseUpdated += OnPoseUpdated;
            }
        }

        private void OnDisable()
        {
            if (_poseProvider != null)
            {
                _poseProvider.OnPoseUpdated -= OnPoseUpdated;
            }
        }

        /// <summary>
        /// Set the pose provider to visualize at runtime.
        /// </summary>
        public void SetPoseProvider(PoseProvider provider)
        {
            if (_poseProvider != null)
            {
                _poseProvider.OnPoseUpdated -= OnPoseUpdated;
            }

            _poseProvider = provider;

            if (_poseProvider != null)
            {
                _poseProvider.OnPoseUpdated += OnPoseUpdated;
            }
        }

        /// <summary>
        /// Toggle visualization on or off.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;

            if (!enabled)
            {
                HideAllVisualization();
            }
        }

        private void OnPoseUpdated(PoseData poseData)
        {
            _latestPose = poseData;
        }

        private void LateUpdate()
        {
            if (!_isEnabled || _latestPose == null || !_latestPose.IsDetected)
            {
                HideAllVisualization();
                return;
            }

            if (_drawBones)
            {
                DrawBones();
            }

            if (_drawJoints)
            {
                DrawJointMarkers();
            }
        }

        /// <summary>
        /// Draw bone connections between joints using LineRenderers.
        /// </summary>
        private void DrawBones()
        {
            for (int i = 0; i < BoneConnections.Length; i++)
            {
                var (startJoint, endJoint) = BoneConnections[i];

                if (!_latestPose.Skeleton.HasValidJoint(startJoint) ||
                    !_latestPose.Skeleton.HasValidJoint(endJoint))
                {
                    if (_boneLineRenderers.TryGetValue(i, out LineRenderer lr))
                    {
                        lr.enabled = false;
                    }
                    continue;
                }

                LineRenderer lineRenderer = GetOrCreateLineRenderer(i);
                lineRenderer.enabled = true;

                PoseJoint start = _latestPose.Skeleton.GetJoint(startJoint);
                PoseJoint end = _latestPose.Skeleton.GetJoint(endJoint);

                Vector3 startPos = NormalizedToScreenPosition(start.Position);
                Vector3 endPos = NormalizedToScreenPosition(end.Position);

                lineRenderer.SetPosition(0, startPos);
                lineRenderer.SetPosition(1, endPos);

                // Color based on average confidence of connected joints
                float avgConfidence = (start.Confidence + end.Confidence) / 2f;
                Color boneColor = Color.Lerp(_lowConfidenceColor, _highConfidenceColor, avgConfidence);
                lineRenderer.startColor = boneColor;
                lineRenderer.endColor = boneColor;
            }
        }

        /// <summary>
        /// Draw spherical markers at each detected joint position.
        /// </summary>
        private void DrawJointMarkers()
        {
            PoseSkeleton skeleton = _latestPose.Skeleton;

            foreach (var kvp in skeleton.Joints)
            {
                JointType jointType = kvp.Key;
                PoseJoint joint = kvp.Value;

                if (!joint.IsValid)
                {
                    if (_jointMarkers.TryGetValue(jointType, out GameObject marker))
                    {
                        marker.SetActive(false);
                    }
                    continue;
                }

                GameObject jointMarker = GetOrCreateJointMarker(jointType);
                jointMarker.SetActive(true);

                Vector3 screenPos = NormalizedToScreenPosition(joint.Position);
                jointMarker.transform.position = screenPos;
                jointMarker.transform.localScale = Vector3.one * _jointMarkerSize;

                // Color based on confidence
                Renderer renderer = jointMarker.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color jointColor = Color.Lerp(_lowConfidenceColor, _highConfidenceColor, joint.Confidence);
                    renderer.material.color = jointColor;
                }
            }
        }

        /// <summary>
        /// Convert normalized (0-1) coordinates to screen world position.
        /// </summary>
        private Vector3 NormalizedToScreenPosition(Vector2 normalizedPos)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return Vector3.zero;

            // Convert normalized coordinates to viewport then to world position
            Vector3 viewportPos = new Vector3(normalizedPos.x, 1f - normalizedPos.y, cam.nearClipPlane + 0.5f);
            return cam.ViewportToWorldPoint(viewportPos);
        }

        /// <summary>
        /// Get or create a LineRenderer for a bone connection.
        /// </summary>
        private LineRenderer GetOrCreateLineRenderer(int index)
        {
            if (_boneLineRenderers.TryGetValue(index, out LineRenderer existing))
            {
                return existing;
            }

            GameObject lineObj = new GameObject($"Bone_{index}");
            lineObj.transform.SetParent(transform);

            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = _lineWidth;
            lineRenderer.endWidth = _lineWidth;
            lineRenderer.useWorldSpace = true;

            if (_lineMaterial != null)
            {
                lineRenderer.material = _lineMaterial;
            }
            else
            {
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }

            lineRenderer.startColor = _lineColor;
            lineRenderer.endColor = _lineColor;

            _boneLineRenderers[index] = lineRenderer;
            return lineRenderer;
        }

        /// <summary>
        /// Get or create a joint marker GameObject.
        /// </summary>
        private GameObject GetOrCreateJointMarker(JointType jointType)
        {
            if (_jointMarkers.TryGetValue(jointType, out GameObject existing))
            {
                return existing;
            }

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"Joint_{jointType}";
            marker.transform.SetParent(transform);
            marker.transform.localScale = Vector3.one * _jointMarkerSize;

            // Remove collider for visualization-only
            Collider col = marker.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            _jointMarkers[jointType] = marker;
            return marker;
        }

        /// <summary>
        /// Hide all visualization elements.
        /// </summary>
        private void HideAllVisualization()
        {
            foreach (var kvp in _boneLineRenderers)
            {
                if (kvp.Value != null)
                    kvp.Value.enabled = false;
            }

            foreach (var kvp in _jointMarkers)
            {
                if (kvp.Value != null)
                    kvp.Value.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            foreach (var kvp in _boneLineRenderers)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }

            foreach (var kvp in _jointMarkers)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);
            }

            _boneLineRenderers.Clear();
            _jointMarkers.Clear();
        }
    }
}
