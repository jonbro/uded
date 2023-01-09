using NUnit.Framework;
using UnityEngine;

namespace jbgeo
{
    [TestFixture]
    public class UdedEditorTests
    {
        private HalfEdgeMesh halfEdgeMesh;

        [SetUp]
        public void SetUp()
        {
            halfEdgeMesh = new HalfEdgeMesh();
        }

        [Test]
        public void LineAlongLine()
        {
            // add two squares - such that one of them has a split edge on its interior
            // ┌──┬────┐
            // │  │a   │
            // └──┤    │
            //    │b   │ then add a->b
            //    └────┘
            halfEdgeMesh.AddRect(new Vertex(0,0), new Vertex(4,0), new Vertex(4,4), new Vertex(0,4));
            halfEdgeMesh.Rebuild();
            halfEdgeMesh.AddRect(new Vertex(-1,0), new Vertex(0,0), new Vertex(0,3), new Vertex(-1,3));
            halfEdgeMesh.Rebuild();
            halfEdgeMesh.AddLine(new Vertex(0,1), new Vertex(0,2));
            halfEdgeMesh.Rebuild();
        }

        [Test]
        public void SimpleSquare()
        {
            halfEdgeMesh.AddRect(new Vertex(0,0), new Vertex(2,0), new Vertex(2,2), new Vertex(0,2));
            halfEdgeMesh.Rebuild();
            Assert.That(halfEdgeMesh.Faces.Count, Is.EqualTo(2));
        }
        [Test]
        public void RayToRay()
        {
            Utility.PointOnRay(new Ray(new Vector3(-1, 1, 0), Vector3.right),
                new Ray(Vector3.zero, Vector3.up), out _, out var distance);
            Assert.AreEqual(1, distance);
            Utility.PointOnRay(new Ray(new Vector3(0, 0, 0), new Vector3(1, 1, 0).normalized),
                new Ray(new Vector3(1, 0, 0), Vector3.up), out _, out distance);
            Assert.AreEqual(1, distance);
        }

        [Test]
        public void SquareInsideSquare()
        {
            halfEdgeMesh.AddRect(new Vertex(-1, -1), new Vertex(3,-1), new Vertex(3,3), new Vertex(-1,3));
            halfEdgeMesh.AddRect(new Vertex(0,0), new Vertex(2,0), new Vertex(2,2), new Vertex(0,2));
            halfEdgeMesh.Rebuild();
            // faces are doubled when we are on interiors
            Assert.That(halfEdgeMesh.Faces.Count, Is.EqualTo(4));
            // confirm that the interior face is set correctly
            Assert.AreEqual(1, halfEdgeMesh.Faces[0].InteriorFaces.Count);
            Assert.AreEqual(3, halfEdgeMesh.Faces[0].InteriorFaces[0]);
        }

        [Test]
        public void NestedInteriors()
        {
            halfEdgeMesh.AddRect(new Vertex(0, 0), new Vertex(5,0), new Vertex(5,5), new Vertex(0,5));
            halfEdgeMesh.Rebuild();
            halfEdgeMesh.AddRect(new Vertex(1, 1), new Vertex(4,1), new Vertex(4,4), new Vertex(1,4));
            halfEdgeMesh.Rebuild();
            halfEdgeMesh.AddRect(new Vertex(2, 2), new Vertex(3,2), new Vertex(3,3), new Vertex(2,3));
            halfEdgeMesh.Rebuild();

            // confirm that the interior face is set correctly
            Assert.AreEqual(1, halfEdgeMesh.Faces[0].InteriorFaces.Count);
            Assert.AreEqual(3, halfEdgeMesh.Faces[0].InteriorFaces[0]);
        }

        [Test]
        public void EdgeRayIntersectionCorrect()
        {
            halfEdgeMesh.AddRect(new Vertex(0, 0), new Vertex(5,0), new Vertex(5,5), new Vertex(0,5));
            halfEdgeMesh.Rebuild();
            halfEdgeMesh.AddRect(new Vertex(4,1), new Vertex(6,1), new Vertex(6,3), new Vertex(4,3));
            halfEdgeMesh.Rebuild();
            halfEdgeMesh.AddRect(new Vertex(1,1), new Vertex(2,1), new Vertex(2,3), new Vertex(1,3));
            halfEdgeMesh.Rebuild();
        }

        [Test]
        public void AddLineAlongExistingEdge()
        {
            // adding lines that exist along existing lines is currently broken
            halfEdgeMesh.AddRect(new Vertex(0,0), new Vertex(2,0), new Vertex(2,2), new Vertex(0,2));
            halfEdgeMesh.Rebuild();
            halfEdgeMesh.AddLine(new Vertex(1,0), new Vertex(3,0));
            halfEdgeMesh.Rebuild();
            Assert.That(halfEdgeMesh.Faces.Count, Is.EqualTo(2));
        }
    }
}
