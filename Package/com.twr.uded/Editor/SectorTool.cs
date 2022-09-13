using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Uded
{
        [EditorTool("Sector Edit", typeof(UdedCore))]
    public class SectorEditTool : EditorTool
    {
        GUIContent m_IconContent;

        [SerializeField]
        Texture2D m_ToolIcon;
        private int currentFace = -1;
        private SectorHandle _floorHandle = new SectorHandle();
        private SectorHandle _ceilingHandle = new SectorHandle();
        static readonly int kFloorHandle		= "FloorHandle".GetHashCode();

        public override GUIContent toolbarIcon => m_IconContent;

        void OnEnable()
        {
            ToolManager.activeToolChanged += ActiveToolDidChange;
            m_IconContent = new GUIContent()
            {
                text = "Sector Tool",
                tooltip = "Sector Tool",
                image = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.twr.uded/Editor/Editor Resources/SectorTool.png")
            };
            _floorHandle.offsetAmount = 0.5f;
            _ceilingHandle.offsetAmount = -0.5f;
        }

        void OnDisable()
        {
            ToolManager.activeToolChanged -= ActiveToolDidChange;
        }
        void ActiveToolDidChange()
        {
            if (!ToolManager.IsActiveTool(this))
                return;
        }
 
        public override void OnToolGUI(EditorWindow window)
        {
            var uded = target as UdedCore;
            var handleMoved = false;
            if (currentFace >= 0 && currentFace < uded.Faces.Count)
            {
                var selectedFaceCenter = uded.GetFaceCenter(currentFace);
                var originalPosition = new Vector3(selectedFaceCenter.x, uded.Faces[currentFace].floorHeight, selectedFaceCenter.y);
                Vector3 newTargetPosition = _floorHandle.DrawHandle(originalPosition, 1.0f);
                if (!originalPosition.y.Equals(newTargetPosition.y))
                {
                    uded.Faces[currentFace].floorHeight = newTargetPosition.y;
                    uded.BuildFaceMeshes();
                }
                originalPosition = new Vector3(selectedFaceCenter.x, uded.Faces[currentFace].ceilingHeight, selectedFaceCenter.y);
                newTargetPosition = _ceilingHandle.DrawHandle(originalPosition, 1.0f);
                if (!originalPosition.y.Equals(newTargetPosition.y))
                {
                    uded.Faces[currentFace].ceilingHeight = newTargetPosition.y;
                    uded.BuildFaceMeshes();
                }
                Handles.Label(originalPosition, "face: " + currentFace);
            }

            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                var newFace = -1;
                for (int i = 0; i < uded.Faces.Count; i++)
                {
                    var face = uded.Faces[i];
                    if(face.clockwise)
                        continue;
                    var floorPlane = new Plane(Vector3.up, new Vector3(0,face.floorHeight, 0));
                    Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
                    var rayHit = floorPlane.Raycast(ray, out var enter);
                    if(rayHit)
                    {
                        var enterPoint = ray.origin+ray.direction*enter;
                        if (uded.PointInFace(new Vector2(enterPoint.x, enterPoint.z), i))
                        {
                            newFace = i;
                        }
                    }
                }
                if (newFace != currentFace)
                {    
                    currentFace = newFace;
                    SceneView.RepaintAll();
                    GUIUtility.hotControl = GUIUtility.GetControlID(kFloorHandle, FocusType.Passive);
                    e.Use();
                }
            }

            var hoveredFace = -1;
            for (int i = 0; i < uded.Faces.Count; i++)
            {
                var face = uded.Faces[i];
                if(face.clockwise)
                    continue;
                var floorPlane = new Plane(Vector3.up, new Vector3(0,face.floorHeight, 0));
                Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
                var rayHit = floorPlane.Raycast(ray, out var enter);
                if(rayHit)
                {
                    var enterPoint = ray.origin+ray.direction*enter;
                    if (uded.PointInFace(new Vector2(enterPoint.x, enterPoint.z), i))
                    {
                        hoveredFace = i;
                    }
                }
            }
            for (int i = 0; i < uded.Faces.Count; i++)
            {
                var face = uded.Faces[i];
                if (face.clockwise)
                    continue;
                for (int j = 0; j < face.Edges.Count; j++)
                {
                    var edgeIndex = face.Edges[j];
                    var edge = uded.Edges[edgeIndex];
                    if (edge.face == currentFace)
                        Handles.color = Color.yellow;
                    else if (edge.face == hoveredFace)
                        Handles.color = Color.green;
                    else
                        continue;
                    var twin = uded.GetTwin(edgeIndex);
                    var thisVert = uded.EdgeVertex(edge);
                    var nextVert = uded.EdgeVertex(twin);
                    Handles.DrawLine(new Vector3(thisVert.x, uded.Faces[edge.face].floorHeight, thisVert.y), new Vector3(nextVert.x, uded.Faces[edge.face].floorHeight, nextVert.y));
                }
            }
        }
    }

}
