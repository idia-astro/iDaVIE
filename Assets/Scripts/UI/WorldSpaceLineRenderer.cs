using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class LineShape
    {
        public Transform Parent;
        public Color Color;
        public bool Active;
        private bool _addedToRenderer;
        public bool Destroyed { get; private set; }

        public void Destroy()
        {
            Destroyed = true;
        }

        public virtual void Activate()
        {
            Active = true;
            if (!_addedToRenderer)
            {
                _addedToRenderer = true;
                WorldSpaceLineRenderer.Instance.AddShape(this);
            }
        }

        public virtual void Deactivate()
        {
            Active = false;
        }

        public virtual void RenderShape()
        {
            
        }
    }

    public class PolyLine : LineShape
    {
        public List<Vector3> Vertices;

        public override void RenderShape()
        {
            if (Vertices == null || Vertices.Count < 2)
            {
                return;
            }

            GL.Begin(GL.LINES);
            {
                GL.Color(Color);
                foreach (var vertex in Vertices)
                {
                    GL.Vertex3(vertex.x, vertex.y, vertex.z);
                }
            }
            GL.End();
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

        private List<LineShape> _shapes = new List<LineShape>();

        public void AddShape(LineShape lineShape)
        {
            _shapes.Add(lineShape);
        }

        private void PurgeDestroyedLines()
        {
            bool requiresRebuild = false;
            foreach (var ls in _shapes)
            {
                if (ls.Destroyed)
                {
                    requiresRebuild = true;
                    break;
                }
            }

            if (requiresRebuild)
            {
                var newShapes = new List<LineShape>();
                foreach (var ls in _shapes)
                {
                    if (!ls.Destroyed)
                    {
                        newShapes.Add(ls);
                    }
                }

                _shapes = newShapes;
            }
        }

        private void OnRenderObject()
        {
            PurgeDestroyedLines();

            CreateLineMaterial();
            _lineMaterial.SetPass(0);

            foreach (var shape in _shapes)
            {
                // Skip deactivated lines and empty lines
                if (!shape.Active || shape.Destroyed)
                {
                    continue;
                }

                GL.PushMatrix();
                {
                    GL.MultMatrix(shape.Parent.localToWorldMatrix);
                    shape.RenderShape();
                }
                GL.PopMatrix();
            }
        }

        public void OnDestroy()
        {
            foreach (var ls in _shapes)
            {
                ls.Destroy();
            }

            _shapes.Clear();
        }
    }
}