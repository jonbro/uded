using NUnit.Framework;
using UnityEngine;

namespace Uded
{
    [TestFixture]
    public class UdedEditorTests
    {
        public void SimpleSquare()
        {
            //AddRect(new Vertex(0,0), new Vertex(2,0), new Vertex(2,2), new Vertex(0,2));
        }
        [Test]
        public void SquareInsideSquare()
        {
            var uded = new GameObject("uded").AddComponent<UdedCore>();
        
            uded.AddRect(new Vertex(-1, -1), new Vertex(3,-1), new Vertex(3,3), new Vertex(-1,3));
            uded.AddRect(new Vertex(0,0), new Vertex(2,0), new Vertex(2,2), new Vertex(0,2));
            uded.Rebuild();
            Assert.That(uded.Faces.Count, Is.EqualTo(3));
        }
    }
}
