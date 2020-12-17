using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Rendering;

namespace Uded
{
    public class SnapControl
    {
        // todo: add support for snapping to vert
        // todo: add support for snapping to edge
        // todo: add support for changing grid size and anchor position 
        
        public static Vector3 SnapToGrid(Vector3 input)
        {
            return new Vector3(Mathf.Round(input.x), Mathf.Round(input.y), Mathf.Round(input.z));
        }
    }
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
            EditorTools.activeToolChanged += ActiveToolDidChange;
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
            EditorTools.activeToolChanged -= ActiveToolDidChange;
        }
        void ActiveToolDidChange()
        {
            if (!EditorTools.IsActiveTool(this))
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
            for (int i = 0; i < uded.Faces.Count; i++)
            {
                var face = uded.Faces[i];
                if (face.clockwise)
                    continue;
                for (int j = 0; j < face.Edges.Count; j++)
                {
                    var edgeIndex = face.Edges[j];
                    var edge = uded.Edges[edgeIndex];
                    if (edge.face != currentFace)
                        continue;
                    var twin = uded.GetTwin(edgeIndex);
                    Handles.color = Color.yellow;
                    var thisVert = uded.EdgeVertex(edge);
                    var nextVert = uded.EdgeVertex(twin);
                    Handles.DrawLine(new Vector3(thisVert.x, uded.Faces[edge.face].floorHeight, thisVert.y), new Vector3(nextVert.x, uded.Faces[edge.face].floorHeight, nextVert.y));
                }
            }
        }
        
    }
    [EditorTool("Add Rect", typeof(UdedCore))]
    public class RectTool : EditorTool
    {
        private int drawingStage;
        private Vector3 firstPoint;
        private Vector3 secondPoint;
        [SerializeField]
        Texture2D m_ToolIcon;
        GUIContent m_IconContent;

        public override GUIContent toolbarIcon => m_IconContent;

        void OnEnable()
        {
            EditorTools.activeToolChanged += ActiveToolDidChange;
            drawingStage = 0;
            m_IconContent = new GUIContent()
            {
                text = "Add Rect",
                tooltip = "Add Rect",
                image = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.twr.uded/Editor/Editor Resources/RectTool.png")
            };

        }

        void OnDisable()
        {
            EditorTools.activeToolChanged -= ActiveToolDidChange;
        }
        void ActiveToolDidChange()
        {
            if (!EditorTools.IsActiveTool(this))
                return;

        }
        static readonly int kDrawRectModeHash		= "DrawRectMode".GetHashCode();

        public override void OnToolGUI(EditorWindow window)
        {
            var sceneView = window as SceneView;
            if (sceneView == null)
                return;
            var dragArea = sceneView.position;

            var evt = Event.current;
            Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
            float rayEnter;
            if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out rayEnter))
            {
                var intersectionPoint = ray.GetPoint(rayEnter);
                Handles.DrawSolidDisc(intersectionPoint, Vector3.up, 0.1f);
                var defaultID = GUIUtility.GetControlID(kDrawRectModeHash, FocusType.Keyboard, dragArea);
                HandleUtility.AddDefaultControl(defaultID);
                if (evt.type == EventType.MouseDown)
                {
                    if (drawingStage == 0)
                    {
                        firstPoint = intersectionPoint;
                        drawingStage++;
                    }
                    else if(drawingStage == 1)
                    {
                        secondPoint = intersectionPoint;
                        var bounds = new Bounds(firstPoint, Vector3.zero);
                        bounds.Encapsulate(secondPoint);
                        firstPoint = bounds.min;
                        secondPoint = bounds.max;
                        var uded = target as UdedCore;
                        Undo.RegisterCompleteObjectUndo(uded, "Add Rect");
                        uded.AddRect(
                            new Vertex(firstPoint.x, firstPoint.z),
                            new Vertex(secondPoint.x, firstPoint.z),
                            new Vertex(secondPoint.x, secondPoint.z),
                            new Vertex(firstPoint.x, secondPoint.z));
                        drawingStage = 0;
                        uded.Rebuild();
                    }
                }
            }
            EditorUtility.SetDirty(target);
        }
    }
    
    [EditorTool("Add Line", typeof(UdedCore))]
    public class LineTool : EditorTool
    {
        private List<Vector3> linePoints = new List<Vector3>(); 
        GUIContent m_IconContent;

        [SerializeField]
        Texture2D m_ToolIcon;

        public override GUIContent toolbarIcon => m_IconContent;

        void OnEnable()
        {
            EditorTools.activeToolChanged += ActiveToolDidChange;
            m_IconContent = new GUIContent()
            {
                text = "Add Line",
                tooltip = "Add Line",
                image = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.twr.uded/Editor/Editor Resources/LineTool.png")
            };
        }

        void OnDisable()
        {
            EditorTools.activeToolChanged -= ActiveToolDidChange;
        }
        void ActiveToolDidChange()
        {
            if (!EditorTools.IsActiveTool(this))
                return;

        }
 
        public override void OnToolGUI(EditorWindow window)
        {
            var sceneView = window as SceneView;
            if (sceneView == null)
                return;
            var dragArea = sceneView.position;
            var evt = Event.current;
            Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
            float rayEnter;
            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Return)
            {
                SubmitCurrentPoints();
            }
            if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out rayEnter))
            {
                var intersectionPoint = SnapControl.SnapToGrid(ray.GetPoint(rayEnter));
                Handles.DrawSolidDisc(intersectionPoint, Vector3.up, 0.1f);
                var defaultID = GUIUtility.GetControlID(FocusType.Keyboard, dragArea);
                HandleUtility.AddDefaultControl(defaultID);
                if (evt.type == EventType.MouseDown && evt.button == 0)
                {
                    if (linePoints.Count == 0)
                    {
                        linePoints = new List<Vector3>();
                        linePoints.Add(intersectionPoint);
                    }
                    else if(intersectionPoint.Equals(linePoints[0]))
                    {
                        // should also submit points if the intersection point is equal to an existing vertex
                        // or lands or a border line
                        SubmitCurrentPoints();
                    }
                    else
                    {
                        linePoints.Add(intersectionPoint);
                    }
                }

                if (evt.type == EventType.Repaint && linePoints.Count > 0)
                {
                    for (int i = 0; i < linePoints.Count-1; i++)
                    {
                        var firstPoint = linePoints[i];
                        var secondPoint = linePoints[i + 1];
                        Handles.DrawLine(firstPoint, secondPoint);
                    }
                    Handles.DrawLine(linePoints[linePoints.Count-1], intersectionPoint);
                }
            }
            EditorUtility.SetDirty(target);
        }

        private void SubmitCurrentPoints()
        {
            if (linePoints.Count < 2)
            {
                linePoints.Clear();
                return;
            }
            var uded = target as UdedCore;
            Undo.RegisterCompleteObjectUndo(uded, "Add Lines");
            for (int i = 0; i < linePoints.Count; i++)
            {
                var firstPoint = linePoints[i];
                var secondPoint = linePoints[(i + 1) % linePoints.Count];
                uded.AddLine(
                    new Uded.Vertex(firstPoint.x, firstPoint.z),
                    new Uded.Vertex(secondPoint.x, secondPoint.z));
            }

            linePoints.Clear();
            uded.Rebuild();
        }
    }
    

}
