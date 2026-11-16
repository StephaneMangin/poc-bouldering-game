using Project.Core.Events;
using Project.Core.Input;
using Project.Features.Camera.Presentation;
using Project.Features.Climbing.Domain;
using Project.Features.Climbing.Presentation;
using Project.Features.Gym.Domain;
using Project.Features.Gym.Presentation;
using Project.Features.Session.Domain;
using Project.Features.Session.Presentation;
using Project.Infrastructure.Persistence;
using UnityEngine;

namespace Project.App
{
    /// <summary>
    /// Composition root of the Block'Out Rennes scene: loads the layout and the baked problems, builds
    /// the gym for the current level design pass, assembles the climber and wires the session.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private const string RecordsFile = "climb_records.json";
        private const string IgnoreRaycastLayer = "Ignore Raycast";
        private const float ClimberHeight = 1.75f;
        private const float ClimberRadius = 0.28f;

        [SerializeField] private GameAssets assets;
        [SerializeField] private UnityEngine.Camera viewCamera;
        [SerializeField] private Light sun;
        [SerializeField] private VisualPass startPass = VisualPass.Whitebox;

        private readonly JsonSaveService _saves = new();
        private MaterialLibrary _materials;
        private Transform _gymRoot;
        private Transform _holdsRoot;
        private HoldMarkers _markers;
        private int _climberIndex;
        private GameObject _climberRoot;

        public GymLayout Layout { get; private set; }

        public GymContent Content { get; private set; }

        public VisualPass Pass { get; private set; }

        public ClimberController Climber { get; private set; }

        public AimFeedback Aim { get; private set; }

        public SessionController Session { get; private set; }

        public OrbitCamera Orbit { get; private set; }

        public IEventBus EventBus { get; } = new InMemoryEventBus();

        public string ClimberName => assets.climbers[_climberIndex].displayName;

        private void Awake()
        {
            Layout = GymLayoutFactory.Build(JsonUtility.FromJson<LayoutDefinition>(assets.layout.text));
            var circuits = Circuit.CreateAll(assets.gymTuning.circuits);
            Content = ContentBaking.FromDefinition(Layout, JsonUtility.FromJson<ContentDefinition>(assets.problems.text), circuits);
            _materials = new MaterialLibrary(assets.materials);
            _markers = new HoldMarkers(transform, _materials);
            Orbit = viewCamera.GetComponent<OrbitCamera>();
            Session = GetComponent<SessionController>();
            BuildVisuals(startPass);
            SpawnClimber(0);
        }

        public void CyclePass() => BuildVisuals((VisualPass)(((int)Pass + 1) % 3));

        public void SwitchClimber() => SpawnClimber((_climberIndex + 1) % assets.climbers.Length);

        public Color HoldColor(string circuitId) => Layout.ColorOf("hold_" + circuitId);

        public void SaveRecords(ClimbRecords records) => _saves.Save(RecordsFile, records);

        /// <summary>Sector whose climbing faces are nearest to a floor position.</summary>
        public GymSector NearestSector(Vector3 position)
        {
            GymSector nearest = null;
            var best = float.MaxValue;
            foreach (var face in Layout.Faces)
            {
                var foot = face.FloorPointInFront(Mathf.Clamp(face.AlongCoordinate(position), 0f, face.Width), 0f);
                var distance = Vector3.Distance(new Vector3(position.x, 0f, position.z), foot);
                if (distance < best)
                {
                    best = distance;
                    nearest = face.Sector;
                }
            }

            return nearest;
        }

        private void BuildVisuals(VisualPass pass)
        {
            Pass = pass;
            if (_gymRoot != null)
            {
                Destroy(_gymRoot.gameObject);
                Destroy(_holdsRoot.gameObject);
            }

            _gymRoot = new GymView(_materials, new PanelTextureFactory(_materials)).Build(Layout, Content, pass, transform);
            _holdsRoot = new HoldsView(_materials).Build(Content, Layout, pass, transform);
            HallLighting.Apply(sun, pass);
        }

        private void SpawnClimber(int index)
        {
            if (_climberRoot != null)
            {
                Destroy(_climberRoot);
            }

            _climberIndex = index;
            _climberRoot = CreateClimberRoot();
            var input = _climberRoot.AddComponent<InputReader>();
            var avatar = CreateAvatar(assets.climbers[index].model, _climberRoot.transform);
            var targeting = _climberRoot.AddComponent<HoldTargeting>();
            targeting.Initialize(viewCamera, input);
            var locomotion = _climberRoot.AddComponent<ClimberLocomotion>();
            locomotion.Initialize(input, avatar.Animator, viewCamera.transform, assets.tuning);
            Climber = _climberRoot.AddComponent<ClimberController>();
            Climber.Initialize(new ClimberSetup
            {
                Input = input,
                Targeting = targeting,
                Avatar = avatar,
                Locomotion = locomotion,
                Tuning = assets.tuning,
                EventBus = EventBus,
                Anatomy = GameplayAnatomy(avatar.Measure()),
                HoldById = Content.HoldById,
            });
            Aim = _climberRoot.AddComponent<AimFeedback>();
            Aim.Initialize(targeting, Climber, _markers);
            WireSession(input, targeting);
        }

        private GameObject CreateClimberRoot()
        {
            var root = new GameObject("Climber") { layer = LayerMask.NameToLayer(IgnoreRaycastLayer) };
            root.transform.SetPositionAndRotation(Layout.SpawnPosition, Quaternion.Euler(0f, Layout.SpawnYawDeg, 0f));
            var body = root.AddComponent<CharacterController>();
            body.height = ClimberHeight;
            body.radius = ClimberRadius;
            body.center = Vector3.up * (ClimberHeight * 0.5f);
            return root;
        }

        private ClimberAvatar CreateAvatar(GameObject model, Transform root)
        {
            var instance = Instantiate(model, root);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            foreach (var child in instance.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = root.gameObject.layer;
            }

            var animator = instance.GetComponentInChildren<Animator>();
            animator.runtimeAnimatorController = assets.locomotion;
            animator.applyRootMotion = false;
            return animator.gameObject.AddComponent<ClimberAvatar>();
        }

        /// <summary>
        /// The default climber scaled to the rig's reach, kept within the sizes the problems were
        /// validated for so every problem stays climbable.
        /// </summary>
        private ClimberAnatomy GameplayAnatomy(ClimberAnatomy measured)
        {
            var reference = ClimberAnatomy.Default;
            var measuredScale = Mathf.Min(measured.ArmReach / reference.ArmReach, measured.LegReach / reference.LegReach);
            var scale = Mathf.Clamp(measuredScale, assets.gymTuning.smallestClimberScale, assets.gymTuning.largestClimberScale);
            Debug.Log($"[Climber] {ClimberName}: arm {measured.ArmReach:0.00} m, leg {measured.LegReach:0.00} m, reach scale {measuredScale:0.00} played as {scale:0.00}");
            return reference.Scaled(scale);
        }

        private void WireSession(InputReader input, HoldTargeting targeting)
        {
            var records = _saves.TryLoad<ClimbRecords>(RecordsFile, out var saved) ? saved : new ClimbRecords();
            Session.Initialize(new SessionSetup
            {
                Climber = Climber,
                Targeting = targeting,
                Camera = Orbit,
                Markers = _markers,
                Input = input,
                EventBus = EventBus,
                Records = records,
                Save = SaveRecords,
            });
            Orbit.Initialize(input, assets.tuning, Climber.CameraTarget);
            Orbit.Snap(_climberRoot.transform.forward);
        }
    }
}
