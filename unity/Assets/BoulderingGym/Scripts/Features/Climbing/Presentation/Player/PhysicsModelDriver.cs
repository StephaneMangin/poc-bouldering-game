using Project.Core.Domain;
using Project.Core.Events;
using Project.Features.Climbing.Domain;
using Project.Features.Climbing.Domain.PhysicsModel;
using UnityEngine;
using ContactPoint = Project.Features.Climbing.Domain.PhysicsModel.ContactPoint;
using EventType = Project.Core.Events.EventType;

namespace Project.Features.Climbing.Presentation
{
    /// <summary>
    /// MonoBehaviour driver that bridges <see cref="ClimbingPhysicsModel"/> domain logic
    /// with the Unity scene. Collects contact points each FixedUpdate, computes the
    /// physics snapshot, and exposes it for other systems.
    /// </summary>
    public sealed class PhysicsModelDriver : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TuningConfig tuningConfig;
        [SerializeField] private PlayerHandsController playerHands;
        [SerializeField] private ClimbingStateMachineDriver driver;
        [SerializeField] private Transform pelvisReference;
        [SerializeField] private Transform wallNormalReference;

        [Header("Foot Targets")]
        [SerializeField] private Transform leftFootTarget;
        [SerializeField] private Transform rightFootTarget;

        [Header("Wall Detection")]
        [SerializeField] private LayerMask wallLayerMask = ~0;
        [SerializeField] private float wallDetectionRange = 3f;

        private ClimbingPhysicsModel _model;
        private IEventBus _eventBus;
        private WallAngleCategory _lastWallCategory;
        private bool _lastStable = true;
        private bool _lastBodyContact;

        public PhysicsSnapshot CurrentSnapshot { get; private set; }

        public WallPanelComponent ActiveWallPanel { get; private set; }

        public void Initialize(
            TuningConfig config,
            PlayerHandsController hands,
            ClimbingStateMachineDriver driverRef,
            Transform pelvis,
            Transform wallRef,
            IEventBus eventBus)
        {
            tuningConfig = config;
            playerHands = hands;
            driver = driverRef;
            pelvisReference = pelvis;
            wallNormalReference = wallRef;
            _eventBus = eventBus;
            _model = new ClimbingPhysicsModel(config);
        }

        private void Awake()
        {
            if (tuningConfig != null)
            {
                _model = new ClimbingPhysicsModel(tuningConfig);
            }
        }

        private void FixedUpdate()
        {
            if (_model == null || pelvisReference == null) return;

            ActiveWallPanel = DetectActiveWallPanel();
            var contacts = CollectContactPoints();
            var pelvisPos = pelvisReference.position;
            var wallNormal = GetWallNormal();

            var wallCategory = ActiveWallPanel != null
                ? ActiveWallPanel.Category
                : WallAngleCategory.Vertical;

            var wallAngleDeg = ActiveWallPanel != null
                ? ActiveWallPanel.AngleFromVertical
                : 90f;

            var bodyWallDist = ComputeBodyWallDistance(pelvisPos, wallNormal);

            CurrentSnapshot = _model.ComputeSnapshot(
                contacts, pelvisPos, wallNormal, wallCategory, wallAngleDeg, bodyWallDist);

            PublishEvents(CurrentSnapshot);
        }

        private ContactPoint[] CollectContactPoints()
        {
            var points = new ContactPoint[4];
            var idx = 0;

            // Hands
            if (playerHands != null)
            {
                var leftHold = playerHands.LeftCurrentHold;
                var rightHold = playerHands.RightCurrentHold;

                points[idx++] = new ContactPoint(
                    playerHands.LeftHandAnchor != null
                        ? playerHands.LeftHandAnchor.position
                        : pelvisReference.position + Vector3.left * 0.3f,
                    isHand: true,
                    isActive: leftHold != null,
                    side: HandSide.Left);

                points[idx++] = new ContactPoint(
                    playerHands.RightHandAnchor != null
                        ? playerHands.RightHandAnchor.position
                        : pelvisReference.position + Vector3.right * 0.3f,
                    isHand: true,
                    isActive: rightHold != null,
                    side: HandSide.Right);
            }

            // Feet (use foot IK targets if available)
            if (leftFootTarget != null)
            {
                points[idx++] = new ContactPoint(
                    leftFootTarget.position,
                    isHand: false,
                    isActive: IsFootOnWall(leftFootTarget),
                    side: null);
            }

            if (rightFootTarget != null)
            {
                points[idx++] = new ContactPoint(
                    rightFootTarget.position,
                    isHand: false,
                    isActive: IsFootOnWall(rightFootTarget),
                    side: null);
            }

            // Trim to actual count
            var result = new ContactPoint[idx];
            System.Array.Copy(points, result, idx);
            return result;
        }

        private bool IsFootOnWall(Transform footTarget)
        {
            if (driver == null) return false;

            // Feet are "on wall" when the climber is in a climbing state (not grounded/falling)
            var state = driver.StateMachine?.CurrentState?.Id;
            return state is ClimbingStateId.GripStable or ClimbingStateId.Reach;
        }

        private WallPanelComponent DetectActiveWallPanel()
        {
            if (pelvisReference == null) return null;

            var wallNormal = GetWallNormal();
            var origin = pelvisReference.position;
            var direction = -wallNormal;

            if (Physics.Raycast(origin, direction, out var hit, wallDetectionRange, wallLayerMask))
            {
                var panel = hit.collider.GetComponent<WallPanelComponent>();
                if (panel != null) return panel;
            }

            // Fallback: check grabbed hold's parent for wall panel
            var heldHold = playerHands != null ? playerHands.LeftCurrentHold ?? playerHands.RightCurrentHold : null;
            if (heldHold != null)
            {
                var holdPanel = heldHold.GetComponentInParent<WallPanelComponent>();
                if (holdPanel != null) return holdPanel;
            }

            return null;
        }

        private float ComputeBodyWallDistance(Vector3 pelvisPos, Vector3 wallNormal)
        {
            var direction = -wallNormal;

            if (Physics.Raycast(pelvisPos, direction, out var hit, wallDetectionRange, wallLayerMask))
            {
                return hit.distance;
            }

            return float.MaxValue;
        }

        private Vector3 GetWallNormal()
        {
            if (ActiveWallPanel != null) return ActiveWallPanel.WallNormal;
            return wallNormalReference != null ? wallNormalReference.forward : Vector3.back;
        }

        private void PublishEvents(PhysicsSnapshot snapshot)
        {
            if (_eventBus == null) return;

            // Wall angle changed
            if (snapshot.WallCategory != _lastWallCategory)
            {
                _eventBus.Publish(new GameEvent(EventType.WallAngleChanged));
                _lastWallCategory = snapshot.WallCategory;
            }

            // COG stability changed
            if (!snapshot.COG.IsStable && _lastStable)
            {
                _eventBus.Publish(new GameEvent(EventType.CogUnstable));
            }
            _lastStable = snapshot.COG.IsStable;

            // Body-wall contact changed
            if (snapshot.HasBodyWallContact != _lastBodyContact)
            {
                _eventBus.Publish(new GameEvent(EventType.BodyWallContact));
                _lastBodyContact = snapshot.HasBodyWallContact;
            }
        }
    }
}
