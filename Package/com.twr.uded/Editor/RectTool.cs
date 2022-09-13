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
            ToolManager.activeToolChanged += ActiveToolDidChange;
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
            ToolManager.activeToolChanged -= ActiveToolDidChange;
            drawingStage = 0;
        }
        void ActiveToolDidChange()
        {
            if (!ToolManager.IsActiveTool(this))
            {
                drawingStage = 0;
                return;
            }
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
                if (evt.type == EventType.Repaint && drawingStage == 1)
                {
                    // draw the rect
                    Handles.DrawSolidDisc(intersectionPoint, Vector3.up, 0.1f);
                    var rectPoints = new Vector3[]
                    {
                        new Vector3(firstPoint.x, firstPoint.y, firstPoint.z),
                        new Vector3(intersectionPoint.x, firstPoint.y, firstPoint.z),
                        new Vector3(intersectionPoint.x, firstPoint.y, firstPoint.z),
                        new Vector3(intersectionPoint.x, firstPoint.y, intersectionPoint.z),
                        new Vector3(intersectionPoint.x, firstPoint.y, intersectionPoint.z),
                        new Vector3(firstPoint.x, firstPoint.y, intersectionPoint.z),
                        new Vector3(firstPoint.x, firstPoint.y, intersectionPoint.z),
                        new Vector3(firstPoint.x, firstPoint.y, firstPoint.z),
                    };
                    Handles.DrawDottedLines(rectPoints, 1);
                }
                if (evt.type == EventType.MouseDown && evt.button == 0)
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
}
