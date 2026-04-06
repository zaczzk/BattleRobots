using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Renders a robot preview into a <see cref="RenderTexture"/> displayed by a
    /// <see cref="RawImage"/> in the Shop UI.
    ///
    /// Usage:
    ///   1. Place the component on a Camera GameObject dedicated to the preview.
    ///   2. Assign the pre-created RenderTexture asset (256×256 or 512×512) to
    ///      <c>_renderTexture</c> and to the <c>RawImage.texture</c> field in the
    ///      Inspector.
    ///   3. Set the Camera's Culling Mask to <c>RobotPreview</c> layer only, so it
    ///      does not render the main scene.
    ///   4. Call <see cref="Activate"/> when the shop panel opens to begin rendering
    ///      a specific robot root transform; call <see cref="Deactivate"/> on close.
    ///   5. The preview robot prefab must be placed on the <c>RobotPreview</c> layer
    ///      (layer 8 by convention — configure in Project Settings → Tags &amp; Layers).
    ///
    /// Orbit control:
    ///   • Mouse X axis (or left-stick horizontal) rotates <see cref="_targetTransform"/>
    ///     around its own Y axis. No tilt (vertical orbit) is applied.
    ///   • Orbit is only active while <see cref="IsActive"/> is true.
    ///
    /// Architecture rules observed:
    ///   • <c>BattleRobots.UI</c> namespace — no <c>BattleRobots.Physics</c> reference.
    ///   • No heap allocations in <c>Update</c>.
    ///   • <c>Camera.targetTexture</c> is set in <c>Awake</c> and cleared in
    ///     <c>OnDestroy</c> to release the RT binding without destroying the asset.
    ///
    /// Inspector wiring checklist:
    ///   □ _previewCamera      → Camera component (this or a child Camera)
    ///   □ _renderTexture      → pre-created RenderTexture asset (e.g. 512×512)
    ///   □ _rawImage           → RawImage in ShopUI that displays the preview
    ///   □ _orbitSpeed         → degrees per unit of mouse delta (default 120)
    ///   □ _targetTransform    → root Transform of the preview robot GO
    /// </summary>
    public sealed class RobotPreviewCamera : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Camera")]
        [Tooltip("The Camera that renders the robot preview. May be this GO's Camera.")]
        [SerializeField] private Camera _previewCamera;

        [Tooltip("Pre-created RenderTexture asset. Must also be assigned to the RawImage.texture.")]
        [SerializeField] private RenderTexture _renderTexture;

        [Tooltip("RawImage in the Shop UI that displays the render texture output.")]
        [SerializeField] private RawImage _rawImage;

        [Header("Orbit")]
        [Tooltip("Rotation speed in degrees per unit of Input.GetAxis(\"Mouse X\").")]
        [SerializeField, Min(0f)] private float _orbitSpeed = 120f;

        [Header("Target")]
        [Tooltip("Root Transform of the robot preview GO. Rotated around its Y axis.")]
        [SerializeField] private Transform _targetTransform;

        // ── Runtime State ─────────────────────────────────────────────────────

        /// <summary>True while the preview is active and orbit input is processed.</summary>
        public bool IsActive { get; private set; }

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            // Bind the RenderTexture to the camera.
            if (_previewCamera != null && _renderTexture != null)
                _previewCamera.targetTexture = _renderTexture;

            // Assign the texture to the RawImage so it shows up immediately.
            if (_rawImage != null && _renderTexture != null)
                _rawImage.texture = _renderTexture;

            // Start inactive until explicitly activated.
            SetCameraEnabled(false);
        }

        private void OnDestroy()
        {
            // Release the RT binding without destroying the asset.
            if (_previewCamera != null)
                _previewCamera.targetTexture = null;
        }

        private void Update()
        {
            if (!IsActive || _targetTransform == null)
                return;

            // Read orbit delta — no allocation; Input.GetAxis returns a float.
            float delta = Input.GetAxis("Mouse X");
            if (delta == 0f)
                return;

            // Rotate the preview robot around its own Y axis. Stack-only — no alloc.
            _targetTransform.Rotate(Vector3.up, -delta * _orbitSpeed * Time.deltaTime,
                Space.Self);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Starts rendering the preview for <paramref name="previewRoot"/>.
        /// Call this when the Shop panel (or any preview panel) becomes visible.
        /// </summary>
        /// <param name="previewRoot">
        /// Root Transform of the robot preview GameObject. Pass <c>null</c> to keep
        /// the current target (if any).
        /// </param>
        public void Activate(Transform previewRoot = null)
        {
            if (previewRoot != null)
                _targetTransform = previewRoot;

            IsActive = true;
            SetCameraEnabled(true);
        }

        /// <summary>
        /// Stops rendering and processing orbit input.
        /// Call this when the preview panel is hidden.
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            SetCameraEnabled(false);
        }

        /// <summary>
        /// Replaces the robot being previewed without deactivating the camera.
        /// </summary>
        public void SetTarget(Transform previewRoot)
        {
            _targetTransform = previewRoot;
        }

        /// <summary>
        /// Snaps the preview robot's Y rotation to <paramref name="angleDegrees"/>,
        /// resetting any orbit applied by the user.
        /// </summary>
        public void ResetRotation(float angleDegrees = 0f)
        {
            if (_targetTransform == null) return;

            Vector3 angles = _targetTransform.localEulerAngles;
            angles.y = angleDegrees;
            _targetTransform.localEulerAngles = angles;
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        private void SetCameraEnabled(bool enabled)
        {
            if (_previewCamera != null)
                _previewCamera.enabled = enabled;

            if (_rawImage != null)
                _rawImage.enabled = enabled;
        }
    }
}
