using System.Collections.Generic;
using HockeyCoach.Replay.Core;
using HockeyCoach.Replay.View;
using NUnit.Framework;
using UnityEngine;

namespace HockeyCoach.Replay.Tests.EditMode
{
    /// <summary>Smoke tests of the replay view on <see cref="SyntheticReplayJson"/>, in edit mode (no frames run).</summary>
    public sealed class ReplayViewSmokeTests
    {
        private const float Tolerance = 1e-4f;

        private GameObject _go;
        private ReplayBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("ReplayBootstrapTest");
            _bootstrap = _go.AddComponent<ReplayBootstrap>();
            _bootstrap.Load(SyntheticReplayJson.Build());
        }

        [TearDown]
        public void TearDown()
        {
            if (_bootstrap != null)
            {
                _bootstrap.Unload();
            }

            Object.DestroyImmediate(_go);
        }

        [Test]
        public void Load_CreatesOneDiscPerRosterPlayer_TwelveVisible_AndThePuck()
        {
            Assert.AreEqual(18, _bootstrap.Discs.Count);
            Assert.AreEqual(12, VisibleIds().Count);
            Assert.IsNotNull(_bootstrap.Puck);
            Assert.IsTrue(_bootstrap.Puck.gameObject.activeInHierarchy);
            Assert.IsNotNull(_bootstrap.Rink.GetComponent<MeshFilter>().sharedMesh);
            Assert.AreEqual(3, _bootstrap.Rink.FaceoffSpotCount);
        }

        [Test]
        public void Load_MarksGoaliesAndLabelsDiscsWithPositionAndId()
        {
            Assert.IsTrue(_bootstrap.Discs[6].IsGoalie);
            Assert.IsTrue(_bootstrap.Discs[106].IsGoalie);
            Assert.IsFalse(_bootstrap.Discs[2].IsGoalie);
            Assert.AreEqual(ReplayTeam.Away, _bootstrap.Discs[102].Team);
            Assert.AreEqual("C\n2", _bootstrap.Discs[2].Label);
        }

        [Test]
        public void DiscsAndPuck_AreDrawnAtTheSampledPositions_NearTheirPlacementNodes()
        {
            AssertSample();
            _bootstrap.Cursor.SeekEvent(1);
            _bootstrap.Refresh();
            AssertSample();
            SkaterSettings sk = _bootstrap.Presentation.Config.Skaters;
            double reach = sk.NodeOffsetRadiusMetres + (sk.SeparationMinMetres * sk.SeparationIterations) + (2 * sk.IdleAmplitudeMetres);
            RinkMapping m = _bootstrap.Timeline.Mapping;
            foreach (KeyValuePair<int, int> p in SyntheticReplayJson.PassPlacement)
            {
                if (_bootstrap.Discs[p.Key].IsGoalie)
                {
                    continue;
                }

                Vector3 at = _bootstrap.Discs[p.Key].transform.localPosition;
                RinkPoint node = m.NodePosition(p.Value);
                Assert.LessOrEqual(Vector2.Distance(new Vector2(at.x, at.y), new Vector2((float)node.X, (float)node.Y)), (float)reach + 1e-3f, "player " + p.Key);
            }
        }

        [Test]
        public void Advance_MovesDiscsAlongTheirSampledPaths()
        {
            Vector3 before = _bootstrap.Discs[2].transform.localPosition;
            SegmentTiming pass = _bootstrap.Presentation.Segments[1];
            _bootstrap.Cursor.Play();
            _bootstrap.Cursor.Advance((pass.Start + pass.EventEnd) / 2);
            _bootstrap.Refresh();
            Assert.AreEqual(1, _bootstrap.Cursor.SegmentIndex);
            AssertSample();
            Assert.Greater(Vector3.Distance(before, _bootstrap.Discs[2].transform.localPosition), 0.5f);
        }

        [Test]
        public void StepNext_MovesTheCursorAndChangesTheHud()
        {
            Assert.AreEqual(0, _bootstrap.Hud.EventIndex);
            string before = _bootstrap.Hud.EventText;
            _bootstrap.Controls.StepNext();
            Assert.AreEqual(1, _bootstrap.Cursor.SegmentIndex);
            Assert.AreEqual(1, _bootstrap.Hud.EventIndex);
            Assert.AreNotEqual(before, _bootstrap.Hud.EventText);
            StringAssert.StartsWith("Pass", _bootstrap.Hud.EventText);
            Assert.AreEqual("P1 00:05", _bootstrap.Hud.ClockText);
            _bootstrap.Controls.StepPrevious();
            Assert.AreEqual(0, _bootstrap.Hud.EventIndex);
        }

        [Test]
        public void Controls_ChangeSpeedAndPlayState()
        {
            Assert.AreEqual("1x", _bootstrap.Hud.SpeedText);
            Assert.AreEqual("Paused", _bootstrap.Hud.StateText);
            _bootstrap.Controls.Faster();
            _bootstrap.Controls.Faster();
            Assert.AreEqual(4, _bootstrap.Cursor.Speed);
            Assert.AreEqual("4x", _bootstrap.Hud.SpeedText);
            _bootstrap.Controls.Slower();
            Assert.AreEqual(2, _bootstrap.Cursor.Speed);
            _bootstrap.Controls.TogglePlay();
            Assert.IsTrue(_bootstrap.Cursor.IsPlaying);
            Assert.AreEqual("Playing", _bootstrap.Hud.StateText);
            _bootstrap.Controls.StepNext();
            _bootstrap.Controls.Restart();
            Assert.AreEqual(0, _bootstrap.Cursor.SegmentIndex);
            Assert.AreEqual(0, _bootstrap.Cursor.Clock);
        }

        [Test]
        public void LineChange_ShowsTheLeavingPlayersUntilTheGate_ThenOnlyTheNewTwelve()
        {
            _bootstrap.Cursor.SeekEvent(4);
            _bootstrap.Refresh();
            ReplaySample atChange = _bootstrap.Cursor.Sample();
            Assert.AreEqual(12, atChange.Players.Count);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, IdsOf(atChange.LeavingPlayers));
            AssertSample();

            _bootstrap.Cursor.SeekEvent(5);
            _bootstrap.Refresh();
            List<int> visible = VisibleIds();
            Assert.AreEqual(12, visible.Count);
            foreach (int id in new[] { 1, 2, 3, 107, 108, 109 })
            {
                Assert.IsFalse(_bootstrap.Discs[id].IsOnIce, "player " + id + " should be off the ice");
            }

            foreach (int id in new[] { 7, 8, 9 })
            {
                Assert.IsTrue(_bootstrap.Discs[id].IsOnIce, "player " + id + " should be on the ice");
            }

            AssertSample();
        }

        [Test]
        public void Camera_IsOrthographicAndShowsTheWholeRink()
        {
            Camera cam = _bootstrap.Camera;
            Assert.IsNotNull(cam);
            Assert.IsTrue(cam.orthographic);
            RinkMapping m = _bootstrap.Timeline.Mapping;
            Assert.GreaterOrEqual(cam.orthographicSize, (float)m.WidthMetres / 2);
            Assert.GreaterOrEqual(cam.orthographicSize * cam.aspect, (float)m.LengthMetres / 2);
            Assert.AreEqual((float)m.LengthMetres / 2, cam.transform.position.x, Tolerance);
            Assert.AreEqual((float)m.WidthMetres / 2, cam.transform.position.y, Tolerance);
        }

        [Test]
        public void Load_RejectsInvalidJson_AndKeepsTheLoadedReplay()
        {
            ReplayCursor cursor = _bootstrap.Cursor;
            Assert.Throws<ReplayFormatException>(() => _bootstrap.Load("{}"));
            Assert.AreSame(cursor, _bootstrap.Cursor);
        }

        [Test]
        public void Unload_DestroysTheBuiltReplay()
        {
            GameObject root = _bootstrap.Root;
            Assert.IsNotNull(root);
            _bootstrap.Unload();
            Assert.IsTrue(root == null);
            Assert.AreEqual(0, _bootstrap.Discs.Count);
            Assert.IsNull(_bootstrap.Cursor);
            Assert.AreEqual(0, _go.transform.childCount);
        }

        private List<int> VisibleIds()
        {
            var ids = new List<int>();
            foreach (KeyValuePair<int, PlayerDiscView> pair in _bootstrap.Discs)
            {
                if (pair.Value.IsOnIce)
                {
                    ids.Add(pair.Key);
                }
            }

            return ids;
        }

        private void AssertSample()
        {
            ReplaySample sample = _bootstrap.Cursor.Sample();
            foreach (PlayerSample p in sample.Players)
            {
                Assert.IsTrue(_bootstrap.Discs[p.Id].IsOnIce, "player " + p.Id + " should be on the ice");
                AssertNear(p.Position, _bootstrap.Discs[p.Id].transform.localPosition);
            }

            foreach (PlayerSample p in sample.LeavingPlayers)
            {
                Assert.IsTrue(_bootstrap.Discs[p.Id].IsOnIce, "leaving player " + p.Id + " should be drawn");
                AssertNear(p.Position, _bootstrap.Discs[p.Id].transform.localPosition);
            }

            AssertNear(sample.Puck, _bootstrap.Puck.transform.localPosition);
        }

        private static List<int> IdsOf(IReadOnlyList<PlayerSample> players)
        {
            var ids = new List<int>();
            foreach (PlayerSample p in players)
            {
                ids.Add(p.Id);
            }

            return ids;
        }

        private static void AssertNear(RinkPoint expected, Vector3 actual)
        {
            Assert.AreEqual((float)expected.X, actual.x, Tolerance);
            Assert.AreEqual((float)expected.Y, actual.y, Tolerance);
        }
    }
}
