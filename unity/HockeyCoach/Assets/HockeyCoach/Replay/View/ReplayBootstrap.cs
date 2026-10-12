using System;
using System.Collections.Generic;
using System.IO;
using HockeyCoach.Replay.Core;
using UnityEngine;

namespace HockeyCoach.Replay.View
{
    /// <summary>
    /// Entry point of the 2D replay (D-087). Add it to an empty GameObject at the origin and set either
    /// <see cref="matchJson"/> or <see cref="matchJsonPath"/> (a <c>match --json</c> export). The presentation values come
    /// from <c>data/replay.json</c> (D-088 B: <see cref="replayConfig"/> or <see cref="replayConfigPath"/>); a missing or
    /// invalid file is a load error, never a silent default. On Start it reads the JSON with the Core
    /// (<see cref="ReplayJsonReader"/>, <see cref="TimelineBuilder"/>, <see cref="PresentationTimeline"/>,
    /// <see cref="ReplayCursor"/>) and
    /// builds everything at runtime: camera, <see cref="RinkView"/>, one <see cref="PlayerDiscView"/> per roster player,
    /// <see cref="PuckView"/>, <see cref="HudView"/> and <see cref="ReplayInput"/>. All playback state lives in the
    /// cursor; this class only advances it by the frame time and draws its sample.
    /// </summary>
    public sealed class ReplayBootstrap : MonoBehaviour
    {
        [Tooltip("Match JSON as a TextAsset. Used when set; otherwise matchJsonPath is read.")]
        [SerializeField]
        private TextAsset matchJson;

        [Tooltip("Path of a 'match --json' export (absolute, or relative to the working directory).")]
        [SerializeField]
        private string matchJsonPath = string.Empty;

        [Tooltip("data/replay.json (D-088) as a TextAsset. Used when set; otherwise replayConfigPath is read.")]
        [SerializeField]
        private TextAsset replayConfig;

        [Tooltip("Path of data/replay.json (absolute, or relative to the Unity project folder).")]
        [SerializeField]
        private string replayConfigPath = "../../data/replay.json";

        [Tooltip("Start playing as soon as the match is loaded.")]
        [SerializeField]
        private bool playOnStart = true;

        [Tooltip("Selectable speeds (multipliers of the presentation time), strictly ascending.")]
        [SerializeField]
        private float[] speeds = { 1, 2, 4, 8, 16 };

        [Tooltip("Index in speeds of the starting speed.")]
        [SerializeField]
        private int initialSpeedIndex;

        [SerializeField]
        private ReplayViewStyle style = new ReplayViewStyle();

        private readonly SortedDictionary<int, PlayerDiscView> _discs = new SortedDictionary<int, PlayerDiscView>();
        private readonly HashSet<int> _shown = new HashSet<int>();
        private ReplayGraphics _graphics;
        private GameObject _root;
        private Transform _players;
        private Camera _createdCamera;
        private float _fittedAspect;

        /// <summary>The look of the replay.</summary>
        public ReplayViewStyle Style => style;

        /// <summary>The loaded timeline, null before <see cref="Load"/>.</summary>
        public ReplayTimeline Timeline { get; private set; }

        /// <summary>The presentation time warp, null before <see cref="Load"/>.</summary>
        public PresentationTimeline Presentation { get; private set; }

        /// <summary>The playback cursor, null before <see cref="Load"/>.</summary>
        public ReplayCursor Cursor { get; private set; }

        /// <summary>The camera showing the rink.</summary>
        public Camera Camera { get; private set; }

        /// <summary>The rink.</summary>
        public RinkView Rink { get; private set; }

        /// <summary>The puck.</summary>
        public PuckView Puck { get; private set; }

        /// <summary>The HUD.</summary>
        public HudView Hud { get; private set; }

        /// <summary>The keyboard controls.</summary>
        public ReplayInput Controls { get; private set; }

        /// <summary>All player discs by id (ascending).</summary>
        public IReadOnlyDictionary<int, PlayerDiscView> Discs => _discs;

        /// <summary>The root GameObject of the built replay (child of this one), null when nothing is loaded.</summary>
        public GameObject Root => _root;

        /// <summary>The message of the last failed load from the serialized source, null when it succeeded.</summary>
        public string LoadError { get; private set; }

        /// <summary>
        /// Reads <paramref name="json"/> with the presentation config from <see cref="replayConfig"/> or
        /// <see cref="replayConfigPath"/>; see <see cref="Load(string, string)"/>.
        /// </summary>
        /// <exception cref="ReplayFormatException">The JSON is not a replayable match export.</exception>
        /// <exception cref="PresentationConfigException">replay.json is missing or invalid.</exception>
        public void Load(string json)
        {
            ReplayJsonReader.Read(json);
            Load(json, ReadPresentationConfig());
        }

        /// <summary>
        /// Reads <paramref name="json"/> and the <c>replay.json</c> text <paramref name="presentationJson"/>, replaces any
        /// loaded replay and builds the view at the start of the match, paused, at the configured initial speed.
        /// </summary>
        /// <exception cref="ReplayFormatException">The JSON is not a replayable match export.</exception>
        /// <exception cref="PresentationConfigException">The presentation config is invalid.</exception>
        public void Load(string json, string presentationJson)
        {
            ReplayMatch match = ReplayJsonReader.Read(json);
            PresentationConfig config = PresentationConfigReader.Read(presentationJson);
            ReplayTimeline timeline = TimelineBuilder.Build(match);
            PresentationTimeline presentation = PresentationTimeline.Build(timeline, config);
            var ladder = new SpeedLadder(ToDoubles(speeds), initialSpeedIndex);

            Unload();
            Timeline = timeline;
            Presentation = presentation;
            Cursor = new ReplayCursor(presentation);
            _graphics = new ReplayGraphics();
            _root = new GameObject("Replay");
            _root.transform.SetParent(transform, false);

            Rink = Child<RinkView>("Rink");
            Rink.Build(timeline.Mapping, _graphics, style);
            _players = new GameObject("Players").transform;
            _players.SetParent(_root.transform, false);
            foreach (RosterEntry entry in match.Roster)
            {
                AddDisc(entry.Id, ReplayTeams.Parse(entry.Team), entry.Goalie, entry);
            }

            Puck = Child<PuckView>("Puck");
            Puck.Build(_graphics, style);
            GameObject controls = new GameObject("Controls");
            controls.transform.SetParent(_root.transform, false);
            Hud = controls.AddComponent<HudView>();
            Controls = controls.AddComponent<ReplayInput>();
            Controls.Bind(this, ladder);

            SetUpCamera();
            Refresh();
        }

        /// <summary>Destroys the built replay, its runtime resources and a camera this component created.</summary>
        public void Unload()
        {
            ReplayObjects.Destroy(_root);
            _root = null;
            if (_graphics != null)
            {
                _graphics.Dispose();
                _graphics = null;
            }

            if (_createdCamera != null)
            {
                ReplayObjects.Destroy(_createdCamera.gameObject);
                _createdCamera = null;
            }

            _discs.Clear();
            Camera = null;
            Rink = null;
            Puck = null;
            Hud = null;
            Controls = null;
            Timeline = null;
            Presentation = null;
            Cursor = null;
        }

        /// <summary>
        /// Draws the cursor's current sample from the presentation layer (D-088): the on-ice discs and the players still
        /// skating off to their bench gate at their drawn positions, bench players hidden, the puck and the HUD (true game
        /// clock and speed).
        /// </summary>
        public void Refresh()
        {
            if (Cursor == null)
            {
                return;
            }

            ReplaySample sample = Cursor.Sample();
            _shown.Clear();
            ShowDiscs(sample.Players);
            ShowDiscs(sample.LeavingPlayers);

            foreach (KeyValuePair<int, PlayerDiscView> pair in _discs)
            {
                if (!_shown.Contains(pair.Key))
                {
                    pair.Value.Hide();
                }
            }

            Puck.Show(sample.Puck);
            Hud.Show(Cursor, sample);
        }

        private void ShowDiscs(IReadOnlyList<PlayerSample> players)
        {
            foreach (PlayerSample p in players)
            {
                PlayerDiscView disc;
                if (!_discs.TryGetValue(p.Id, out disc))
                {
                    disc = AddDisc(p.Id, p.Team, p.IsGoalie, Timeline.FindPlayer(p.Id));
                }

                disc.Show(p.Position);
                _shown.Add(p.Id);
            }
        }

        /// <summary>Fits the orthographic camera to the rink plus <see cref="ReplayViewStyle.cameraMargin"/> at the camera's aspect.</summary>
        public void FitCamera()
        {
            if (Camera == null || Timeline == null)
            {
                return;
            }

            RinkMapping m = Timeline.Mapping;
            float halfLength = ((float)m.LengthMetres / 2) + style.boardThickness + style.cameraMargin;
            float halfWidth = ((float)m.WidthMetres / 2) + style.boardThickness + style.cameraMargin;
            float aspect = Camera.aspect > 0 ? Camera.aspect : 16f / 9f;
            Camera.orthographic = true;
            Camera.orthographicSize = Mathf.Max(halfWidth, halfLength / aspect);
            Vector3 centre = _root.transform.TransformPoint(new Vector3((float)m.LengthMetres / 2, (float)m.WidthMetres / 2, 0));
            Camera.transform.SetPositionAndRotation(new Vector3(centre.x, centre.y, centre.z + ReplayLayers.CameraZ), Quaternion.identity);
            _fittedAspect = aspect;
        }

        private void Start()
        {
            LoadFromSource();
            if (Cursor != null && playOnStart)
            {
                Cursor.Play();
                Refresh();
            }
        }

        private void Update()
        {
            if (Cursor == null)
            {
                return;
            }

            Cursor.Advance(Mathf.Max(0, Time.deltaTime));
            Refresh();
        }

        private void LateUpdate()
        {
            if (Camera != null && !Mathf.Approximately(Camera.aspect, _fittedAspect))
            {
                FitCamera();
            }
        }

        private void OnDestroy()
        {
            Unload();
        }

        private void OnGUI()
        {
            if (LoadError != null)
            {
                GUI.Label(new Rect(10, 10, Mathf.Max(200, Screen.width - 20), Screen.height - 20), "Replay could not load the match:\n" + LoadError);
            }
        }

        private void LoadFromSource()
        {
            LoadError = null;
            try
            {
                string json;
                if (matchJson != null)
                {
                    json = matchJson.text;
                }
                else if (!string.IsNullOrWhiteSpace(matchJsonPath))
                {
                    json = File.ReadAllText(matchJsonPath.Trim().Trim('"'));
                }
                else
                {
                    throw new ReplayFormatException("Set 'Match Json' (TextAsset) or 'Match Json Path' on ReplayBootstrap.");
                }

                Load(json);
            }
            catch (Exception e) when (e is ReplayFormatException || e is PresentationConfigException || e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException)
            {
                LoadError = e.Message;
                Debug.LogError("Replay: " + e.Message, this);
            }
        }

        private string ReadPresentationConfig()
        {
            if (replayConfig != null)
            {
                return replayConfig.text;
            }

            if (string.IsNullOrWhiteSpace(replayConfigPath))
            {
                throw new PresentationConfigException("Set 'Replay Config' (TextAsset) or 'Replay Config Path' (data/replay.json) on ReplayBootstrap.");
            }

            string path = replayConfigPath.Trim().Trim('"');
            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), path));
            }

            if (!File.Exists(path))
            {
                throw new PresentationConfigException("replay.json not found at " + path + " (D-088 B). Set 'Replay Config' or 'Replay Config Path' on ReplayBootstrap.");
            }

            return File.ReadAllText(path);
        }

        private void SetUpCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = FindAnyObjectByType<Camera>();
            }

            if (cam == null)
            {
                var go = new GameObject("ReplayCamera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                _createdCamera = cam;
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = style.background;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = -ReplayLayers.CameraZ * 2;
            Camera = cam;
            FitCamera();
        }

        private PlayerDiscView AddDisc(int id, ReplayTeam team, bool goalie, RosterEntry entry)
        {
            var go = new GameObject("Player " + id);
            go.transform.SetParent(_players, false);
            PlayerDiscView disc = go.AddComponent<PlayerDiscView>();
            disc.Build(id, team, goalie, ReplayText.PlayerLabel(entry, id), _graphics, style);
            _discs[id] = disc;
            return disc;
        }

        private T Child<T>(string name)
            where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            return go.AddComponent<T>();
        }

        private static double[] ToDoubles(float[] values)
        {
            if (values == null)
            {
                return new double[0];
            }

            var result = new double[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return result;
        }
    }
}
