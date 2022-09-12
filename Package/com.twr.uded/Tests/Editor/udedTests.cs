using NUnit.Framework;
using UnityEngine;

namespace Uded
{
    [TestFixture]
    public class UdedEditorTests
    {
        private UdedCore _uded;
        
        [SetUp]
        public void SetUp()
        {
            _uded =new GameObject("uded").AddComponent<UdedCore>(); 
        }
        
        [Test]
        public void SimpleSquare()
        {
            _uded.AddRect(new Vertex(0,0), new Vertex(2,0), new Vertex(2,2), new Vertex(0,2));
            _uded.Rebuild();
            Assert.That(_uded.Faces.Count, Is.EqualTo(2));
        }
        
        [Test]
        public void SquareInsideSquare()
        {
            _uded.AddRect(new Vertex(-1, -1), new Vertex(3,-1), new Vertex(3,3), new Vertex(-1,3));
            _uded.AddRect(new Vertex(0,0), new Vertex(2,0), new Vertex(2,2), new Vertex(0,2));
            _uded.Rebuild();
            // faces are doubled when we are on interiors
            Assert.That(_uded.Faces.Count, Is.EqualTo(4));
            // confirm that the interior face is set correctly
            Assert.AreEqual(1, _uded.Faces[0].InteriorFaces.Count);
            Assert.AreEqual(3, _uded.Faces[0].InteriorFaces[0]);
        }
        
        [Test]
        public void EdgeRayIntersectionCorrect()
        {
            _uded.AddRect(new Vertex(0, 0), new Vertex(5,0), new Vertex(5,5), new Vertex(0,5));
            _uded.Rebuild();
            _uded.AddRect(new Vertex(4,1), new Vertex(6,1), new Vertex(6,3), new Vertex(4,3));
            _uded.Rebuild();
            _uded.AddRect(new Vertex(1,1), new Vertex(2,1), new Vertex(2,3), new Vertex(1,3));
            _uded.Rebuild();
        }
        
        [Test]
        public void RebuildDoesntClearExistingFaceData()
        {
            _uded.AddRect(new Vertex(0,0), new Vertex(2,0), new Vertex(2,2), new Vertex(0,2));
            _uded.Rebuild();
            _uded.Faces[0].floorHeight = 2;
            _uded.AddRect(new Vertex(2,2), new Vertex(4,2), new Vertex(4,4), new Vertex(2,4));
            _uded.Rebuild();
            Assert.That(_uded.Faces[0].floorHeight, Is.EqualTo(2));
        }
        [Test]
        public void AddLineAlongExistingEdge()
        {
            // adding lines that exist along existing lines is currently broken
            _uded.AddRect(new Vertex(0,0), new Vertex(2,0), new Vertex(2,2), new Vertex(0,2));
            _uded.Rebuild();
            _uded.AddLine(new Vertex(1,0), new Vertex(3,0));
            _uded.Rebuild();
            Assert.That(_uded.Faces.Count, Is.EqualTo(2));
        }
    }
}
