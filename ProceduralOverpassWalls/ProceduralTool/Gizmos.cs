using UnityEngine;
using ProceduralObjects.Classes;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralObjects
{
    public static class Gizmos
    {
        public static GameObject[] CreateGizmo(Vector3 position, bool deletePreviousIfExisting)
        {
            if (deletePreviousIfExisting)
                DestroyGizmo();

            Material spriteMat = new Material(Shader.Find("Sprites/Default"));

            GameObject xAxis = new GameObject("ProceduralAxis_X");
            var xCollid = xAxis.AddComponent<BoxCollider>();
            xCollid.size = new Vector3(2, 2, 2);
            LineRenderer xLineComp = xAxis.AddComponent<LineRenderer>();
            xLineComp.material = spriteMat;
            xLineComp.startColor = Color.red;
            xLineComp.endColor = Color.red;
            xLineComp.widthMultiplier = 1;
            Vector3[] xPos = new Vector3[2];
            xPos[0] = position;
            xPos[1] = new Vector3(20, 0, 0) + position;
            xLineComp.SetPositions(xPos);
            xAxis.transform.position = position;
            xAxis.transform.localScale = new Vector3(20, 0.5f, 0.5f);

            GameObject yAxis = new GameObject("ProceduralAxis_Y");
            var yCollid = yAxis.AddComponent<BoxCollider>();
            yCollid.size = new Vector3(2, 2, 2);
            LineRenderer yLineComp = yAxis.AddComponent<LineRenderer>();
            yLineComp.material = spriteMat;
            yLineComp.startColor = Color.green;
            yLineComp.endColor = Color.green;
            yLineComp.widthMultiplier = 1;
            Vector3[] yPos = new Vector3[2];
            yPos[0] = position;
            yPos[1] = new Vector3(0, 20, 0) + position;
            yLineComp.SetPositions(yPos);
            yAxis.transform.position = position;
            yAxis.transform.localScale = new Vector3(0.5f, 20f, 0.5f);

            GameObject zAxis = new GameObject("ProceduralAxis_Z");
            var zCollid = zAxis.AddComponent<BoxCollider>();
            zCollid.size = new Vector3(2, 2, 2);
            LineRenderer zLineComp = zAxis.AddComponent<LineRenderer>();
            zLineComp.material = spriteMat;
            zLineComp.startColor = Color.blue;
            zLineComp.endColor = Color.blue;
            zLineComp.widthMultiplier = 1;
            Vector3[] zPos = new Vector3[2];
            zPos[0] = position;
            zPos[1] = new Vector3(0, 0, 20) + position;
            zLineComp.SetPositions(zPos);
            zAxis.transform.position = position;
            zAxis.transform.localScale = new Vector3(0.5f, 0.5f, 20);

            _gizmo = new GameObject[] { xAxis, yAxis, zAxis };
            return _gizmo;
        }
        public static void ResizeUpdatePos(float distance, Vector3 position, LineRenderer xLineComp, LineRenderer yLineComp, LineRenderer zLineComp)
        {
            var factor = 1f;
            if (distance < 30)
                factor = .25f;
            else if (distance > 250)
                factor = 1.8f;
            else
                factor = 0.0070455f * distance + 0.0386363f;
            factor *= ProceduralObjectsMod.GizmoSize.value;
            xLineComp.widthMultiplier = factor;
            yLineComp.widthMultiplier = factor;
            zLineComp.widthMultiplier = factor;
            _gizmo[0].transform.localScale = new Vector3(20f * factor, 0.5f * factor, 0.5f * factor);
            _gizmo[1].transform.localScale = new Vector3(0.5f * factor, 20f * factor, 0.5f * factor);
            _gizmo[2].transform.localScale = new Vector3(0.5f * factor, 0.5f * factor, 20f * factor);

            Vector3[] xPos = new Vector3[2];
            xPos[0] = position;
            xPos[1] = new Vector3(20 * factor, 0, 0) + position;
            xLineComp.SetPositions(xPos);

            Vector3[] yPos = new Vector3[2];
            yPos[0] = position;
            yPos[1] = new Vector3(0, 20 * factor, 0) + position;
            yLineComp.SetPositions(yPos);

            Vector3[] zPos = new Vector3[2];
            zPos[0] = position;
            zPos[1] = new Vector3(0, 0, 20 * factor) + position;
            zLineComp.SetPositions(zPos);
        }
        public static void DestroyGizmo()
        {
            if (!Exists)
                return;
            if (_gizmo[0] != null)
                Object.Destroy(_gizmo[0]);
            if (_gizmo[1] != null)
                Object.Destroy(_gizmo[1]);
            if (_gizmo[2] != null)
                Object.Destroy(_gizmo[2]);
            _gizmo = null;
        }
        public static bool Exists
        {
            get
            {
                return _gizmo != null;
            }
        }
        public static Vector3 AxisHitPoint(Vector3 hitPoint, Vector3 originObjPosition)
        {
            return new Vector3(originObjPosition.x - hitPoint.x, originObjPosition.y - hitPoint.y, originObjPosition.z - hitPoint.z);
        }

        private static GameObject[] _gizmo;
    }

    public class RotationWizardData
    {
        public RotationWizardData() { }
        public Vector3 initialMousePosition;
        private float _initMousePosXGUI;
        public Quaternion initialRotation;
        public float clickTime;

        public float GUIMousePositionX
        {
            get
            {
                return this._initMousePosXGUI;
            }
        }
        public void UpdateMouseCoords()
        {
            this.initialMousePosition = Input.mousePosition;
            this._initMousePosXGUI = this.initialMousePosition.x;
        }
        public void IncrementStep()
        {
            clickTime += TimeUtils.deltaTime;
        }
        public static RotationWizardData GetCurrentRotationData(ProceduralObject obj)
        {
            var data = new RotationWizardData();
            data.initialMousePosition = Input.mousePosition;
            data.initialRotation = obj.m_rotation;
            data._initMousePosXGUI = data.initialMousePosition.x;
            data.clickTime = 0f;
            return data;
        }
    }

    public class VerticesWizardData
    {
        public VerticesWizardData(byte type)
        {
            secClicked = 0f;
            toolType = type;
            enableMovement = false;
            storedVertices = false;
            relativePositions = new Dictionary<Vertex, Vector3>();
        }

        private float secClicked;
        public bool enableMovement, storedVertices;
        public byte toolType;
        public Dictionary<Vertex, Vector3> relativePositions;
        public Vector3 originHitPoint;
        public Vector2 originMousePosition;
        public Bounds verticesBounds;
        public Dictionary<Vertex, Vertex> rotVertices;

        public void IncrementStep()
        {
            if (!enableMovement)
            {
                if (secClicked >= .2f)
                    enableMovement = true;
                secClicked += TimeUtils.deltaTime;
            }
        }
        public void Store(Vector3 hitPoint, Vector2 mousePos)
        {
            originHitPoint = hitPoint;
            originMousePosition = mousePos;
        }
        public void Store(Vertex[] selectedVertices)
        {
            relativePositions = new Dictionary<Vertex, Vector3>();
            foreach (var vertex in selectedVertices)
            {
                relativePositions[vertex] = vertex.Position - originHitPoint;
            }
            if (toolType == 1 || toolType == 2) // rotation or scale
            {
                rotVertices = new Dictionary<Vertex, Vertex>();
                verticesBounds = new Bounds(selectedVertices[0].Position, Vector3.zero);
                foreach (var vertex in selectedVertices)
                {
                    rotVertices[new Vertex(vertex)] = vertex;
                    verticesBounds.Encapsulate(vertex.Position);
                }
            }
            storedVertices = true;
        }
        // move vertices
        public void ApplyToNewPosition(Vector3 newHitPoint, ProceduralObject obj)
        {
            if (relativePositions == null)
                return;
            if (relativePositions.Count == 0)
                return;
            var referencial = VertexUtils.RotatePointAroundPivot(newHitPoint, originHitPoint, Quaternion.Inverse(obj.m_rotation));
            foreach (KeyValuePair<Vertex, Vector3> kvp in relativePositions)
            {
                var newpos = kvp.Value + referencial;
                kvp.Key.Position = new Vector3(newpos.x, kvp.Key.Position.y, newpos.z);
            }
        }
        // rotate vertices
        public void ApplyToNewPosition(float mousePosX)
        {
            float diff = originMousePosition.x - mousePosX;
            Quaternion rot = Quaternion.identity;
            if (diff > 0)
                rot = Quaternion.Euler(0, (diff * 370f) / Screen.width, 0);
            else
                rot = Quaternion.Euler(0, -(((-diff) * 370f) / Screen.width), 0);
            
            foreach (KeyValuePair<Vertex, Vertex> kvp in rotVertices)
            {
                kvp.Value.Position = VertexUtils.RotatePointAroundPivot(kvp.Key.Position, verticesBounds.center, rot);
            }
        }
        // scale vertices
        public void ApplyToNewPosition(Vector2 newMousePos)
        {
            float factor = 1;
            if ((newMousePos.x - originMousePosition.x) < 0)
                factor = (1.6f / Screen.width) * (newMousePos.x - originMousePosition.x) + 1;
            else
                factor = (2.4f / Screen.width) * (newMousePos.x - originMousePosition.x) + 1;

            foreach (KeyValuePair<Vertex, Vertex> kvp in rotVertices)
            {
                kvp.Value.Position = ((kvp.Key.Position - verticesBounds.center) * factor) + verticesBounds.center;
            }
        }
        public static void FlattenSelection(ProceduralObject obj, List<int> editingVertexIndex, Vertex[] buffer)
        {
            obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, buffer);
            var bounds = new Bounds(buffer.First(v => v.Index == editingVertexIndex[0]).Position, Vector3.zero);
            var vertices = buffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex))));
            foreach (Vertex v in vertices)
                bounds.Encapsulate(v.Position);

            foreach (Vertex v in vertices)
            {
                v.Position.y = bounds.center.y;
            }
            obj.historyEditionBuffer.ConfirmNewStep(buffer);
        }
    }
}
 