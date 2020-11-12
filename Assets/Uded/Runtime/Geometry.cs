using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uded
{
    [Serializable]
    public class Face
    {
        public List<int> Edges = new List<int>();
        public bool clockwise;
        public List<int> InteriorFaces = new List<int>();
    }
    [Serializable]
    public class HalfEdge
    {
        public Vertex origin;
        public HalfEdge twin;
        public HalfEdge next;
        public HalfEdge prev;
        public int face;
        public int twinId;
        public int nextId;
        public int prevId;
    }
    [Serializable]
    /// <summary>
    /// wrapper class for vector2 so we can store refs
    /// </summary>
    public class Vertex
    {
        public Vector2 _value;
        public static implicit operator Vector2(Vertex v) => v._value;
        public static implicit operator Vector3(Vertex v) => new Vector3(v._value.x, 0, v._value.y);

        public Vertex(Vector2 v)
        {
            _value = v;
        }

        public Vertex(float x, float y)
        {
            _value = new Vector2(x, y);
        }
        public static implicit operator Vertex(Vector2 value)
        {
            return new Vertex(value);
        }

        public bool Equals(Vertex b)
        {
            return _value == b._value;
        }

        public float x
        {
            get => _value.x;
            set => _value.x = value;
        }

        public float y
        {
            get => _value.y;
            set => _value.y = value;
        }
    }    
}