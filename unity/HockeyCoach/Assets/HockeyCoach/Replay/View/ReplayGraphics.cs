using System;
using System.Collections.Generic;
using HockeyCoach.Replay.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HockeyCoach.Replay.View
{
    /// <summary>Draw depths and sorting orders of the replay layers (the camera looks along +z, smaller z is in front).</summary>
    public static class ReplayLayers
    {
        /// <summary>Z of the rink.</summary>
        public const float RinkZ = 0f;

        /// <summary>Z of the player discs.</summary>
        public const float DiscZ = -1f;

        /// <summary>Z offset of a disc label from its disc.</summary>
        public const float LabelZ = -0.1f;

        /// <summary>Z of the puck.</summary>
        public const float PuckZ = -2f;

        /// <summary>Z of the camera.</summary>
        public const float CameraZ = -20f;

        /// <summary>Sorting order of the rink.</summary>
        public const int RinkOrder = 0;

        /// <summary>Sorting order of the discs.</summary>
        public const int DiscOrder = 10;

        /// <summary>Sorting order of the disc labels.</summary>
        public const int LabelOrder = 11;

        /// <summary>Sorting order of the puck.</summary>
        public const int PuckOrder = 20;

        /// <summary>World position of a rink point: x along the rink, y across it, both in metres (1 unit = 1 m).</summary>
        public static Vector3 ToWorld(RinkPoint point, float z)
        {
            return new Vector3((float)point.X, (float)point.Y, z);
        }
    }

    /// <summary>
    /// Runtime-only graphics resources of one loaded replay: the shared vertex-colour material, the label font and
    /// every generated mesh. No asset files are used; <see cref="Dispose"/> destroys everything it created.
    /// </summary>
    public sealed class ReplayGraphics : IDisposable
    {
        /// <summary>Shaders tried in order; all are built in and draw vertex colours without lighting.</summary>
        private static readonly string[] ShaderNames = { "Sprites/Default", "Hidden/Internal-Colored", "Unlit/Color" };

        private readonly List<Object> _owned = new List<Object>();

        /// <summary>Creates the material and loads the built-in font.</summary>
        public ReplayGraphics()
        {
            Shader shader = null;
            foreach (string name in ShaderNames)
            {
                shader = Shader.Find(name);
                if (shader != null)
                {
                    break;
                }
            }

            if (shader == null)
            {
                Debug.LogWarning("Replay: none of the shaders " + string.Join(", ", ShaderNames) + " was found; the replay is drawn without a material.");
            }
            else
            {
                Material = Own(new Material(shader) { name = "ReplayVertexColour" });
            }

            Font = LoadFont();
        }

        /// <summary>The shared material, null when no suitable shader exists.</summary>
        public Material Material { get; }

        /// <summary>The built-in font for labels, null when unavailable.</summary>
        public Font Font { get; }

        /// <summary>Registers an object to be destroyed by <see cref="Dispose"/>.</summary>
        public T Own<T>(T obj)
            where T : Object
        {
            obj.hideFlags = HideFlags.DontSave;
            _owned.Add(obj);
            return obj;
        }

        /// <summary>Builds a mesh from <paramref name="builder"/> and owns it.</summary>
        public Mesh Mesh(MeshBuilder builder, string name)
        {
            Mesh mesh = builder.Build();
            mesh.name = name;
            return Own(mesh);
        }

        /// <summary>Adds a MeshFilter and MeshRenderer with the shared material to <paramref name="go"/>.</summary>
        public MeshRenderer Attach(GameObject go, Mesh mesh, int sortingOrder)
        {
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Material;
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        /// <summary>Destroys every owned object.</summary>
        public void Dispose()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                ReplayObjects.Destroy(_owned[i]);
            }

            _owned.Clear();
        }

        private static Font LoadFont()
        {
            try
            {
                return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch (ArgumentException)
            {
                Debug.LogWarning("Replay: built-in font LegacyRuntime.ttf not found; disc labels are not drawn.");
                return null;
            }
        }
    }

    /// <summary>Destroys objects correctly in play mode and in edit mode (tests).</summary>
    public static class ReplayObjects
    {
        /// <summary>Destroy in play mode, DestroyImmediate in edit mode; ignores null.</summary>
        public static void Destroy(Object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(obj);
            }
            else
            {
                Object.DestroyImmediate(obj);
            }
        }
    }

    /// <summary>Collects vertex-coloured triangles in the XY plane (z = 0) and builds a mesh.</summary>
    public sealed class MeshBuilder
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<int> _triangles = new List<int>();

        /// <summary>Number of vertices added so far.</summary>
        public int VertexCount => _vertices.Count;

        /// <summary>Adds an axis-aligned rectangle; triangles drawn later appear on top of earlier ones.</summary>
        public MeshBuilder Rect(float xMin, float yMin, float xMax, float yMax, Color color)
        {
            int start = _vertices.Count;
            _vertices.Add(new Vector3(xMin, yMin, 0));
            _vertices.Add(new Vector3(xMin, yMax, 0));
            _vertices.Add(new Vector3(xMax, yMax, 0));
            _vertices.Add(new Vector3(xMax, yMin, 0));
            for (int i = 0; i < 4; i++)
            {
                _colors.Add(color);
            }

            _triangles.Add(start);
            _triangles.Add(start + 1);
            _triangles.Add(start + 2);
            _triangles.Add(start);
            _triangles.Add(start + 2);
            _triangles.Add(start + 3);
            return this;
        }

        /// <summary>Adds a filled circle.</summary>
        public MeshBuilder Disc(Vector2 centre, float radius, int segments, Color color)
        {
            segments = Mathf.Max(3, segments);
            int start = _vertices.Count;
            _vertices.Add(new Vector3(centre.x, centre.y, 0));
            _colors.Add(color);
            for (int i = 0; i < segments; i++)
            {
                float angle = (2f * Mathf.PI * i) / segments;
                _vertices.Add(new Vector3(centre.x + (radius * Mathf.Cos(angle)), centre.y + (radius * Mathf.Sin(angle)), 0));
                _colors.Add(color);
            }

            for (int i = 0; i < segments; i++)
            {
                _triangles.Add(start);
                _triangles.Add(start + 1 + ((i + 1) % segments));
                _triangles.Add(start + 1 + i);
            }

            return this;
        }

        /// <summary>Builds the mesh (double-sided is not needed: the camera looks along +z at clockwise triangles).</summary>
        public Mesh Build()
        {
            var mesh = new Mesh();
            mesh.SetVertices(_vertices);
            mesh.SetColors(_colors);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
