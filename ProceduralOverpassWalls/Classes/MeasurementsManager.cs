using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using ProceduralObjects.Localization;
using ProceduralObjects.UI;

namespace ProceduralObjects.Classes
{
    public class MeasurementsManager
    {
        public MeasurementsManager(ProceduralObjectsLogic logic)
        {
            this.logic = logic;
            instance = this;
            measureMode = 0;
            window = new Rect(555, 200, 350, 280);
        }

        public static MeasurementsManager instance;

        public ProceduralObjectsLogic logic;
        public bool showWindow = false;
        public Rect window;

        private byte measureMode;
        private Vector3 pos0, pos1, pos2;
        private float Result = -1;

        private GameObject gameObject;
        private LineRenderer lineRenderer;

        public void DrawWindow()
        {
            if (showWindow)
            {
                window.height = (measureMode == 0) ? 75 : (((measureMode == 1) ? 115 : 245) + (Result == -1 ? 0 : 25));
                window = GUIUtils.ClampRectToScreen(GUIUtils.Window(95082741, window, draw, LocalizationManager.instance.current["measurmt"]));
            }
        }
        private void draw(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 298, 21));
            if (GUIUtils.CloseHelpButtons(window, "Measurements"))
            {
                CloseWindow(); return;
            }
            GUI.Label(new Rect(5, 21, 340, 21), LocalizationManager.instance.current["measurmt_desc"]);

            if (GUI.Button(new Rect(5, 42, 167, 24), LocalizationManager.instance.current["measurmt_dist"]))
            {
                ProceduralObjectsLogic.PlaySound();
                UnityEngine.Object.Destroy(lineRenderer);
                UnityEngine.Object.Destroy(gameObject);
                measureMode = 1;
                Result = -1f;
                pos0 = Vector3.down;
                pos1 = Vector3.down;
                pos2 = Vector3.down;
            }
            if (GUI.Button(new Rect(178, 42, 167, 24), LocalizationManager.instance.current["measurmt_ang"]))
            {
                ProceduralObjectsLogic.PlaySound();
                UnityEngine.Object.Destroy(lineRenderer);
                UnityEngine.Object.Destroy(gameObject);
                measureMode = 2;
                Result = -1f;
                pos0 = Vector3.down;
                pos1 = Vector3.down;
                pos2 = Vector3.down;
            }
            bool anythingSelectable = AnythingSelectable();
            switch (measureMode)
            {
                case 1:
                    GUI.DrawTexture(new Rect(25, 70, 300, 20), ProceduralObjectsMod.Icons[13]);
                    SelectAsPoint(new Rect(4, 90, 120, 20), 0, anythingSelectable);
                    SelectAsPoint(new Rect(226, 90, 120, 20), 1, anythingSelectable);
                    if (Result != -1f)
                        GUI.Label(new Rect(5, 114, 200, 23), "<size=14>" + Gizmos.ConvertToDistanceUnit(Result).ToString("n2") + ProceduralObjectsMod.distanceUnit + "</size>");
                    break;
                case 2:
                    GUI.DrawTexture(new Rect(25, 70, 300, 150), ProceduralObjectsMod.Icons[14]);
                    SelectAsPoint(new Rect(4, 90, 120, 20), 0, anythingSelectable);
                    SelectAsPoint(new Rect(105, 218, 120, 20), 2, anythingSelectable);
                    SelectAsPoint(new Rect(226, 90, 120, 20), 1, anythingSelectable);
                    if (Result != -1f)
                        GUI.Label(new Rect(5, 240, 200, 23), "<size=14>" + (Result * (ProceduralObjectsMod.AngleUnits.value == 1 ? Mathf.Deg2Rad : 1f)).ToString("n2") + ProceduralObjectsMod.angleUnit + "</size>");
                    break;
            }
        }
        private void SelectAsPoint(Rect rect, int i, bool anythingSelectable)
        {
            bool selected = false;
            if (i == 0 && pos0 != Vector3.down) selected = true;
            else if (i == 1 && pos1 != Vector3.down) selected = true;
            else if (i == 2 && pos2 != Vector3.down) selected = true;

            if (!selected) GUI.color = Color.red;
            if (GUI.Button(rect, (selected ? "<i>" : "") + LocalizationManager.instance.current[anythingSelectable ? "measurmt_select" : "measurmt_noSelection"] + (selected ? "</i>" : "")))
            {
                if (anythingSelectable)
                {
                    ProceduralObjectsLogic.PlaySound();
                    if (i == 0) pos0 = SelectedPos();
                    if (i == 1) pos1 = SelectedPos();
                    if (i == 2) pos2 = SelectedPos();
                    UpdateMeasurement();
                }
            }
            GUI.color = Color.white;
        }

        private void UpdateMeasurement()
        {
            InitiateLines();
            if (pos0 == Vector3.down || pos1 == Vector3.down)
            {
                lineRenderer.SetPositions(new Vector3[] { });
                Result = -1f;
                return;
            }
            switch (measureMode)
            {
                case 1:
                    Result = Vector3.Distance(pos0, pos1);
                    lineRenderer.positionCount = 2;
                    lineRenderer.SetPositions(new Vector3[] { pos0, pos1 });
                    break;
                case 2:
                    if (pos2 == Vector3.down)
                    {
                        lineRenderer.SetPositions(new Vector3[] { });
                        Result = -1f;
                        return;
                    }
                    Result = Vector3.Angle(pos0 - pos2, pos1 - pos2);
                    lineRenderer.positionCount = 3;
                    lineRenderer.SetPositions(new Vector3[] { pos0, pos2, pos1 });
                    break;
            }
        }
        private void InitiateLines()
        {
            if (gameObject != null && lineRenderer != null) return;
            if (gameObject == null)
                gameObject = new GameObject("PO_Measurements");
            if (lineRenderer != null)
                UnityEngine.Object.Destroy(lineRenderer);
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.5f;
            lineRenderer.startColor = Color.white;
            lineRenderer.endColor = Color.white;
            lineRenderer.endWidth = 0.5f;
            lineRenderer.material = Gizmos.spriteMat;
            lineRenderer.widthMultiplier = 1f;
        }

        public void CloseWindow()
        {
            showWindow = false;
            UnityEngine.Object.Destroy(lineRenderer);
            UnityEngine.Object.Destroy(gameObject);
            Result = -1f;
            measureMode = 0;
        }
        private bool AnythingSelectable()
        {
            if (!showWindow) return false;
            if (logic.movingWholeModel) return false;
            if (logic.currentlyEditingObject != null && !logic.editingWholeModel && logic.editingVertex)
            {
                if (logic.editingVertexIndex == null) return false;
                if (logic.editingVertexIndex.Count != 1) return false;
                return true;
            }
            if (!logic.proceduralTool && logic.chosenProceduralInfo == null)
            {
                if (logic.pObjSelection == null) return false;
                if (logic.pObjSelection.Count != 1) return false;
                return true;
            }
            return false;
        }
        private Vector3 SelectedPos()
        {
            if (logic.currentlyEditingObject != null && !logic.editingWholeModel && logic.editingVertex && logic.editingVertexIndex.Count == 1)
                return logic.currentlyEditingObject.m_position + (logic.currentlyEditingObject.m_rotation * logic.currentlyEditingObject.vertices.FirstOrDefault(v => v.Index == logic.editingVertexIndex[0]).Position);
            if (!logic.proceduralTool && logic.chosenProceduralInfo == null && logic.currentlyEditingObject == null && logic.pObjSelection.Count == 1)
                return logic.pObjSelection[0].m_position;

            return Vector3.down;
        }
    }
}
