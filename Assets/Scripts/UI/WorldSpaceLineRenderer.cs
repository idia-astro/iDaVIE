using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class PolyLine
    {
        public Transform Parent;
        public List<Vector3> Vertices;
        public Color Color;
        public bool Active;
        
        public bool AutoDrawing { get; private set; }

        public bool Destroyed { get; private set; }

        public void Destroy()
        {
            Destroyed = true;
        }

        public void Activate()
        {
            Active = true;
            if (!AutoDrawing)
            {
                AutoDrawing = true;
                WorldSpaceLineRenderer.Instance.AddLine(this);
            }
        }

        public void Deactivate()
        {
            Active = false;
        }
    }

    public class WorldSpaceLineRenderer : MonoBehaviour
    {
        private static WorldSpaceLineRenderer _instance;
        private static Material _lineMaterial;

        static void CreateLineMaterial()
        {
            if (_lineMaterial)
            {
                return;
            }

            var shader = Shader.Find("Hidden/Internal-Colored");
            _lineMaterial = new Material(shader);
            _lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            // TODO: see if these settings are needed. We might want ZWriting for non-transparent lines
            _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _lineMaterial.SetInt("_ZWrite", 0);
        }

        public static WorldSpaceLineRenderer Instance
        {
            get
            {
                if (!_instance)
                {
                    var gameObject = new GameObject
                    {
                        name = "WorldSpaceLineRendererObject"
                    };
                    _instance = gameObject.AddComponent<WorldSpaceLineRenderer>();
                }

                return _instance;
            }
        }

        private List<PolyLine> _lines = new List<PolyLine>();

        public void AddLine(PolyLine polyLine)
        {
            _lines.Add(polyLine);
        }

        private void PurgeDestroyedLines()
        {
            bool requiresRebuild = false;
            foreach (var ls in _lines)
            {
                if (ls.Destroyed)
                {
                    requiresRebuild = true;
                    break;
                }
            }

            if (requiresRebuild)
            {
                var newLines = new List<PolyLine>();
                foreach (var ls in _lines)
                {
                    if (!ls.Destroyed)
                    {
                        newLines.Add(ls);
                    }
                }

                _lines = newLines;
            }
        }

        private void OnRenderObject()
        {
            PurgeDestroyedLines();

            CreateLineMaterial();
            _lineMaterial.SetPass(0);

            foreach (var polyLine in _lines)
            {
                // Skip deactivated lines and empty lines
                if (!polyLine.Active || polyLine.Destroyed || polyLine.Vertices == null || polyLine.Vertices.Count < 2)
                {
                    continue;
                }

                GL.PushMatrix();
                {
                    GL.MultMatrix(polyLine.Parent.localToWorldMatrix);
                    GL.Begin(GL.LINES);
                    {
                        GL.Color(polyLine.Color);
                        foreach (var vertex in polyLine.Vertices)
                        {
                            GL.Vertex3(vertex.x, vertex.y, vertex.z);
                        }
                    }
                    GL.End();
                }
                GL.PopMatrix();
            }
        }

        public void OnDestroy()
        {
            foreach (var ls in _lines)
            {
                ls.Destroy();
            }
            
            _lines.Clear();
        }
    }
}