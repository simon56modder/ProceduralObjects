using UnityEngine;
using ProceduralObjects.Classes;

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

            return new GameObject[] { xAxis, yAxis, zAxis };
        }
        public static void UpdateLinePositions(Vector3 position, LineRenderer xLineComp, LineRenderer yLineComp, LineRenderer zLineComp)
        {
            Vector3[] xPos = new Vector3[2];
            xPos[0] = position;
            xPos[1] = new Vector3(20, 0, 0) + position;
            xLineComp.SetPositions(xPos);

            Vector3[] yPos = new Vector3[2];
            yPos[0] = position;
            yPos[1] = new Vector3(0, 20, 0) + position;
            yLineComp.SetPositions(yPos);

            Vector3[] zPos = new Vector3[2];
            zPos[0] = position;
            zPos[1] = new Vector3(0, 0, 20) + position;
            zLineComp.SetPositions(zPos);
        }
        public static void DestroyGizmo()
        {
            if (GameObject.Find("ProceduralAxis_X") != null)
                Object.Destroy(GameObject.Find("ProceduralAxis_X"));
            if (GameObject.Find("ProceduralAxis_Y") != null)
                Object.Destroy(GameObject.Find("ProceduralAxis_Y"));
            if (GameObject.Find("ProceduralAxis_Z") != null)
                Object.Destroy(GameObject.Find("ProceduralAxis_Z"));
        }
        public static bool Exists
        {
            get
            {
                return GameObject.Find("ProceduralAxis_X") != null;
            }
        }
        public static Vector3 AxisHitPoint(Vector3 hitPoint, Vector3 originObjPosition)
        {
            return new Vector3(originObjPosition.x - hitPoint.x, originObjPosition.y - hitPoint.y, originObjPosition.z - hitPoint.z);
        }
    }

    public class RotationWizardData
    {
        public RotationWizardData() { }
        public Vector3 initialMousePosition;
        private float _initMousePosXGUI;
        public Quaternion initialRotation;

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

        public static RotationWizardData GetCurrentRotationData(ProceduralObject obj)
        {
            var data = new RotationWizardData();
            data.initialMousePosition = Input.mousePosition;
            data.initialRotation = obj.gameObject.transform.rotation;
            data._initMousePosXGUI = data.initialMousePosition.x;
            return data;
        }
    }
}
 