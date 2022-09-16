using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uded
{
    [Serializable]
    public class Face
    {
        public float floorHeight = 0f;
        public float ceilingHeight = 6f;
        public List<int> Edges = new();
        public bool clockwise;
        public List<int> InteriorFaces = new();
        public Material floorMat;
        public Material ceilingMat;
        public void CopyFaceValues(Face copyFrom)
        {
            floorHeight = copyFrom.floorHeight;
            ceilingHeight = copyFrom.ceilingHeight;
            floorMat = copyFrom.floorMat;
            ceilingMat = copyFrom.ceilingMat;
        }
    }
    [Serializable]
    public class HalfEdge
    {
        public int face = -1;
        public int vertexIndex;
        public int nextId;
        public int prevId;
        public Material lowerMat;
        public Material midMat;
        public Material upperMat;
    }
    [Serializable]
    /// <summary>
    /// wrapper class for vector2 so we can store refs
    /// </summary>
    public class Vertex
    {
        public Vector2 _value;
        public static implicit operator Vector2(Vertex v) => v._value;
        public static implicit operator Vector3(Vertex v) => new(v._value.x, 0, v._value.y);

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