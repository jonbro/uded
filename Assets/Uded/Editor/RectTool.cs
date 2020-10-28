using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Rendering;

[EditorTool("Add Rect", typeof(Uded))]
public class RectTool : EditorTool
{
    private int drawingStage;
    private Vector3 firstPoint;
    private Vector3 secondPoint;
    void OnEnable()
    {
        EditorTools.activeToolChanged += ActiveToolDidChange;
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
                    var uded = target as Uded;
                    uded.AddLine(
                        new Uded.Vertex(firstPoint.x, firstPoint.z),
                        new Uded.Vertex(secondPoint.x, secondPoint.z));
                    // var bounds = new Bounds(firstPoint, Vector3.zero);
                    // bounds.Encapsulate(secondPoint);
                    // firstPoint = bounds.min;
                    // secondPoint = bounds.max;
                    // uded.AddRect(
                    //     new Uded.Vertex(firstPoint.x, firstPoint.z),
                    //     new Uded.Vertex(secondPoint.x, firstPoint.z),
                    //     new Uded.Vertex(secondPoint.x, secondPoint.z),
                    //     new Uded.Vertex(firstPoint.x, secondPoint.z));
                    drawingStage = 0;
                    // uded.Rebuild();
                }
            }

            if (evt.type == EventType.MouseUp)
            {
                Debug.Log("using up?");
            }
        }
        EditorUtility.SetDirty(target);
    }
}
