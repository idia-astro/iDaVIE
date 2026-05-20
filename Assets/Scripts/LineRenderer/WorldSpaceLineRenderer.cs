/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms 
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with 
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party 
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LineRenderer
{
    public class LineShape
    {
        public Transform Parent;
        public bool Active { get; private set; }
        private bool _addedToRenderer;

        public virtual Color Color
        {
            get => _color;
            set => _color = value;
        }

        protected Color _color;
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
        public bool DiscreteLines;

        public override void RenderShape()
        {
            if (Vertices == null || Vertices.Count < 2)
            {
                return;
            }

            GL.Begin(DiscreteLines? GL.LINES: GL.LINE_STRIP);
            {
                GL.Color(_color);
                foreach (var vertex in Vertices)
                {
                    GL.Vertex3(vertex.x, vertex.y, vertex.z);
                }
            }
            GL.End();
        }
    }

    public class CuboidLine : LineShape
    {
        public float Height = 1;
        public float Width = 1;
        public float Depth = 1;

        public Vector3 Bounds
        {
            get => new Vector3(Width, Height, Depth);
            set
            {
                Width = value.x;
                Height = value.y;
                Depth = value.z;
            }
        }
        
        public Vector3 Center = Vector3.zero;
        private readonly Color[] _perLineColours = new Color[12];

        public void SetExtent(Vector3 vMin, Vector3 vMax)
        {
            Center = (vMin + vMax) / 2;
            Width = Math.Abs(vMin.x - vMax.x);
            Height = Math.Abs(vMin.y - vMax.y);
            Depth = Math.Abs(vMin.z - vMax.z);
        }

        public override Color Color
        {
            get => _color;
            set
            {
                _color = value;
                for (int i = 0; i < 12; i++)
                {
                    _perLineColours[i] = value;
                }
            }
        }

        public void SetColor(Color color, int index)
        {
            if (index >= 0 && index < 12)
            {
                _perLineColours[index] = color;
            }
        }

        public override void RenderShape()
        {
            var offsets = new Vector3(Width, Height, Depth) / 2;
            Vector3[] vertices =
            {
                // Front face, clockwise from bottom left
                Center + new Vector3(-offsets.x, -offsets.y, +offsets.z),
                Center + new Vector3(-offsets.x, +offsets.y, +offsets.z),
                Center + new Vector3(+offsets.x, +offsets.y, +offsets.z),
                Center + new Vector3(+offsets.x, -offsets.y, +offsets.z),
                // Back face, clockwise from bottom left
                Center + new Vector3(-offsets.x, -offsets.y, -offsets.z),
                Center + new Vector3(-offsets.x, +offsets.y, -offsets.z),
                Center + new Vector3(+offsets.x, +offsets.y, -offsets.z),
                Center + new Vector3(+offsets.x, -offsets.y, -offsets.z),
            };

         
            
            GL.Begin(GL.LINES);
            {
                GL.Color(_perLineColours[0]);
                GL.Vertex(vertices[0]);
                GL.Vertex(vertices[1]);

                GL.Color(_perLineColours[1]);
                GL.Vertex(vertices[1]);
                GL.Vertex(vertices[2]);

                GL.Color(_perLineColours[2]);
                GL.Vertex(vertices[2]);
                GL.Vertex(vertices[3]);

                GL.Color(_perLineColours[3]);
                GL.Vertex(vertices[3]);
                GL.Vertex(vertices[0]);

                GL.Color(_perLineColours[4]);
                GL.Vertex(vertices[4]);
                GL.Vertex(vertices[5]);

                GL.Color(_perLineColours[5]);
                GL.Vertex(vertices[5]);
                GL.Vertex(vertices[6]);

                GL.Color(_perLineColours[6]);
                GL.Vertex(vertices[6]);
                GL.Vertex(vertices[7]);

                GL.Color(_perLineColours[7]);
                GL.Vertex(vertices[7]);
                GL.Vertex(vertices[4]);

                GL.Color(_perLineColours[8]);
                GL.Vertex(vertices[0]);
                GL.Vertex(vertices[4]);

                GL.Color(_perLineColours[9]);
                GL.Vertex(vertices[1]);
                GL.Vertex(vertices[5]);

                GL.Color(_perLineColours[10]);
                GL.Vertex(vertices[2]);
                GL.Vertex(vertices[6]);

                GL.Color(_perLineColours[11]);
                GL.Vertex(vertices[3]);
                GL.Vertex(vertices[7]);
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
                    GL.MultMatrix(shape.Parent ? shape.Parent.localToWorldMatrix : transform.localToWorldMatrix);
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