using UnityEngine;
using ProceduralObjects.Classes;
using System.Collections.Generic;
using System;
using System.Linq;

using ProceduralObjects.UI;

namespace ProceduralObjects
{
    public static class Gizmos
    {
        public static void CreatePositionGizmo(Vector3 position, bool deletePreviousIfExisting)
        {
            if (deletePreviousIfExisting)
                DestroyGizmo();
            
            GameObject xAxis = new GameObject("ProceduralAxis_X");
            var xCollid = xAxis.AddComponent<BoxCollider>();
            xCollid.size = new Vector3(2, 2, 2);
            LineRenderer xLineComp = xAxis.AddComponent<LineRenderer>();
            xLineComp.material = spriteMat;
            xLineComp.startColor = GizmoRed;
            xLineComp.endColor = GizmoRed;
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
            yLineComp.startColor = GizmoGreen;
            yLineComp.endColor = GizmoGreen;
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
            zLineComp.startColor = GizmoBlue;
            zLineComp.endColor = GizmoBlue;
            zLineComp.widthMultiplier = 1;
            Vector3[] zPos = new Vector3[2];
            zPos[0] = position;
            zPos[1] = new Vector3(0, 0, 20) + position;
            zLineComp.SetPositions(zPos);
            zAxis.transform.position = position;
            zAxis.transform.localScale = new Vector3(0.5f, 0.5f, 20);

            _gizmo = new GameObject[] { xAxis, yAxis, zAxis };
            renderers = new LineRenderer[] { xLineComp, yLineComp, zLineComp };
        }
        public static void CreateScaleGizmo(Vector3 position, bool deletePreviousIfExisting)
        {
            if (deletePreviousIfExisting)
                DestroyGizmo();

            Material yellow = new Material(Shader.Find("GUI/Text Shader"));
            yellow.color = GizmoYellow;

            GameObject xAxis = new GameObject("ProceduralAxis_X");
            var xCollid = xAxis.AddComponent<BoxCollider>();
            xCollid.size = new Vector3(2, 2, 2);
            LineRenderer xLineComp = xAxis.AddComponent<LineRenderer>();
            xLineComp.material = spriteMat;
            xLineComp.startColor = GizmoRed;
            xLineComp.endColor = GizmoRed;
            xLineComp.widthMultiplier = 1;
            Vector3[] xPos = new Vector3[2];
            xPos[0] = position;
            xPos[1] = new Vector3(19, 0, 0) + position;
            xLineComp.SetPositions(xPos);
            xAxis.transform.position = position;
            xAxis.transform.localScale = new Vector3(20, 0.5f, 0.5f);
            GameObject xYellowCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            xYellowCube.transform.position = xPos[1];
            xYellowCube.transform.SetParent(xAxis.transform, true);
            xYellowCube.GetComponent<MeshRenderer>().material = yellow;
            GameObject.Destroy(xYellowCube.GetComponent<MeshCollider>());
          //  xYellowCube.transform.localScale = new Vector3(3.5f, 3.5f, 3.5f);

            GameObject yAxis = new GameObject("ProceduralAxis_Y");
            var yCollid = yAxis.AddComponent<BoxCollider>();
            yCollid.size = new Vector3(2, 2, 2);
            LineRenderer yLineComp = yAxis.AddComponent<LineRenderer>();
            yLineComp.material = spriteMat;
            yLineComp.startColor = GizmoGreen;
            yLineComp.endColor = GizmoGreen;
            yLineComp.widthMultiplier = 1;
            Vector3[] yPos = new Vector3[2];
            yPos[0] = position;
            yPos[1] = new Vector3(0, 19, 0) + position;
            yLineComp.SetPositions(yPos);
            yAxis.transform.position = position;
            yAxis.transform.localScale = new Vector3(0.5f, 20f, 0.5f);
            GameObject yYellowCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            yYellowCube.transform.position = yPos[1];
            yYellowCube.transform.SetParent(yAxis.transform, true);
            yYellowCube.GetComponent<MeshRenderer>().material = yellow;
          //  yYellowCube.transform.localScale = xYellowCube.transform.localScale;
            GameObject.Destroy(yYellowCube.GetComponent<MeshCollider>());

            GameObject zAxis = new GameObject("ProceduralAxis_Z");
            var zCollid = zAxis.AddComponent<BoxCollider>();
            zCollid.size = new Vector3(2, 2, 2);
            LineRenderer zLineComp = zAxis.AddComponent<LineRenderer>();
            zLineComp.material = spriteMat;
            zLineComp.startColor = GizmoBlue;
            zLineComp.endColor = GizmoBlue;
            zLineComp.widthMultiplier = 1;
            Vector3[] zPos = new Vector3[2];
            zPos[0] = position;
            zPos[1] = new Vector3(0, 0, 19) + position;
            zLineComp.SetPositions(zPos);
            zAxis.transform.position = position;
            zAxis.transform.localScale = new Vector3(0.5f, 0.5f, 20);
            GameObject zYellowCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zYellowCube.transform.position = zPos[1];
            zYellowCube.transform.SetParent(zAxis.transform, true);
            zYellowCube.GetComponent<MeshRenderer>().material = yellow;
          //  zYellowCube.transform.localScale = xYellowCube.transform.localScale;
            GameObject.Destroy(zYellowCube.GetComponent<MeshCollider>());

            _gizmo = new GameObject[] { xAxis, yAxis, zAxis };
            _scaleCubes = new GameObject[] { xYellowCube, yYellowCube, zYellowCube };
            renderers = new LineRenderer[] { xLineComp, yLineComp, zLineComp };
        }
        public static void CreateRotationGizmo(Vector3 position, bool deletePreviousIfExisting)
        {
            if (deletePreviousIfExisting)
                DestroyGizmo();

            GameObject xAxis = new GameObject("ProceduralAxis_X"); // turn around Y
           /* var xCollid = xAxis.AddComponent<CapsuleCollider>();
            xCollid.radius = 0;
            xCollid.height = 2;
            xCollid.radius = 10; */
            LineRenderer xLineComp = xAxis.AddComponent<LineRenderer>();
            xLineComp.material = spriteMat;
            xLineComp.startColor = GizmoRed;
            xLineComp.endColor = GizmoRed;
            xLineComp.useWorldSpace = false;
            xLineComp.widthMultiplier = 1;
            // Code for circle creation by Loek van den Ouweland, April 30, 2018
            // https://loekvandenouweland.com/content/use-linerenderer-in-unity-to-draw-a-circle.html
            xLineComp.positionCount = 31;
            var xPoints = new Vector3[31];
            for (int i = 0; i < 31; i++)
            {
                var rad = Mathf.Deg2Rad * (i * 360f / 30);
                xPoints[i] = new Vector3(Mathf.Sin(rad) * 3, 0, Mathf.Cos(rad) * 3);
            }
            for (int i = 0; i < 31; i += 2)
            {
                var colliderObj = new GameObject("ProceduralAxis_X_" + (i / 2));
                colliderObj.transform.SetParent(xAxis.transform);
                colliderObj.transform.localPosition = xPoints[i];
                var xCollid = colliderObj.AddComponent<BoxCollider>();
                xCollid.size = new Vector3(1.1f, 1.1f, 1.1f);
            }
            xLineComp.SetPositions(xPoints);
            xAxis.transform.position = position;
            xAxis.transform.localScale = new Vector3(4, 0.5f, 4);



            GameObject yAxis = new GameObject("ProceduralAxis_Y"); // turn around Z
           /* var yCollid = yAxis.AddComponent<CapsuleCollider>();
            yCollid.radius = 0;
            yCollid.height = 2; */
            LineRenderer yLineComp = yAxis.AddComponent<LineRenderer>();
            yLineComp.material = spriteMat;
            yLineComp.startColor = GizmoGreen;
            yLineComp.endColor = GizmoGreen;
            yLineComp.useWorldSpace = false;
            yLineComp.widthMultiplier = 1;
            // Code for circle creation by Loek van den Ouweland, April 30, 2018
            // https://loekvandenouweland.com/content/use-linerenderer-in-unity-to-draw-a-circle.html
            yLineComp.positionCount = 31;
            var yPoints = new Vector3[31];
            for (int i = 0; i < 31; i++)
            {
                var rad = Mathf.Deg2Rad * (i * 360f / 30);
                yPoints[i] = new Vector3(Mathf.Cos(rad) * 3, Mathf.Sin(rad) * 3, 0);
            }
            for (int i = 0; i < 31; i += 2)
            {
                var colliderObj = new GameObject("ProceduralAxis_Y_" + (i / 2));
                colliderObj.transform.SetParent(yAxis.transform);
                colliderObj.transform.localPosition = yPoints[i];
                var yCollid = colliderObj.AddComponent<BoxCollider>();
                yCollid.size = new Vector3(1.1f, 1.1f, 1.1f);
            }
            yLineComp.SetPositions(yPoints);
            yAxis.transform.position = position;
            yAxis.transform.localScale = new Vector3(4, 4, 0.5f);
            // pivot the collider !


            GameObject zAxis = new GameObject("ProceduralAxis_Z"); // turn around X
            /* var zCollid = zAxis.AddComponent<CapsuleCollider>();
            zCollid.radius = 0;
            zCollid.height = 2; */
            LineRenderer zLineComp = zAxis.AddComponent<LineRenderer>();
            zLineComp.material = spriteMat;
            zLineComp.startColor = GizmoBlue;
            zLineComp.endColor = GizmoBlue;
            zLineComp.useWorldSpace = false;
            zLineComp.widthMultiplier = 1;
            // Code for circle creation by Loek van den Ouweland, April 30, 2018
            // https://loekvandenouweland.com/content/use-linerenderer-in-unity-to-draw-a-circle.html
            zLineComp.positionCount = 31;
            var zPoints = new Vector3[31];
            for (int i = 0; i < 31; i++)
            {
                var rad = Mathf.Deg2Rad * (i * 360f / 30);
                zPoints[i] = new Vector3(0, Mathf.Sin(rad) * 3, Mathf.Cos(rad) * 3);
            }
            for (int i = 0; i < 31; i += 2)
            {
                var colliderObj = new GameObject("ProceduralAxis_Z_" + (i / 2));
                colliderObj.transform.SetParent(zAxis.transform);
                colliderObj.transform.localPosition = zPoints[i];
                var zCollid = colliderObj.AddComponent<BoxCollider>();
                zCollid.size = new Vector3(1.1f, 1.1f, 1.1f);
            }
            zLineComp.SetPositions(zPoints);
            zAxis.transform.position = position;
            zAxis.transform.localScale = new Vector3(0.5f, 4, 4);
            // pivot the collider !
             

            _gizmo = new GameObject[] { xAxis, yAxis, zAxis };
            renderers = new LineRenderer[] { xLineComp, yLineComp, zLineComp };
        }

        public static Plane CollisionPlane(byte actionMode, AxisEditionState axisState, Vector3 axisHitPoint, Quaternion rotation, Camera renderCamera)
        {
            Plane p = new Plane();
            if (actionMode < 2)
            {
                if (Gizmos.referential == SpaceReferential.World && actionMode == 0)
                {
                    switch (axisState)
                    {
                        case AxisEditionState.X:
                            p = new Plane(new Vector3(0, renderCamera.transform.forward.y, renderCamera.transform.forward.z).normalized, axisHitPoint);
                            break;
                        case AxisEditionState.Y:
                            p = new Plane(new Vector3(renderCamera.transform.forward.x, 0, renderCamera.transform.forward.z).normalized, axisHitPoint);
                            break;
                        case AxisEditionState.Z:
                            p = new Plane(new Vector3(renderCamera.transform.forward.x, renderCamera.transform.forward.y, 0).normalized, axisHitPoint);
                            break;
                    }
                }
                else // if (Gizmos.referential == SpaceReferential.Local || actionMode == 1)
                {
                    switch (axisState)
                    {
                        case AxisEditionState.X:
                            p = new Plane(Vector3.ProjectOnPlane(renderCamera.transform.forward, rotation * Vector3.right).normalized, axisHitPoint);
                            break;
                        case AxisEditionState.Y:
                            p = new Plane(Vector3.ProjectOnPlane(renderCamera.transform.forward, rotation * Vector3.up).normalized, axisHitPoint);
                            break;
                        case AxisEditionState.Z:
                            p = new Plane(Vector3.ProjectOnPlane(renderCamera.transform.forward, rotation * Vector3.forward).normalized, axisHitPoint);
                            break;
                    }
                }
            }
            else // if (actionMode == 2)
            {
                if (referential == SpaceReferential.World)
                {
                    switch (axisState)
                    {
                        case AxisEditionState.X:
                            p = new Plane(axisHitPoint,
                                    new Vector3(axisHitPoint.x + 1, axisHitPoint.y, axisHitPoint.z),
                                    new Vector3(axisHitPoint.x, axisHitPoint.y, axisHitPoint.z + 1));
                            break;
                        case AxisEditionState.Y:
                            p = new Plane(axisHitPoint,
                                    new Vector3(axisHitPoint.x, axisHitPoint.y + 1, axisHitPoint.z),
                                    new Vector3(axisHitPoint.x + 1, axisHitPoint.y, axisHitPoint.z));
                            break;
                        case AxisEditionState.Z:
                            p = new Plane(axisHitPoint,
                                    new Vector3(axisHitPoint.x, axisHitPoint.y + 1, axisHitPoint.z),
                                    new Vector3(axisHitPoint.x, axisHitPoint.y, axisHitPoint.z + 1));
                            break;
                    }
                }
                else
                {
                    switch (axisState)
                    {
                        case AxisEditionState.X:
                            p = new Plane(axisHitPoint,
                                    VertexUtils.RotatePointAroundPivot(new Vector3(axisHitPoint.x + 1, axisHitPoint.y, axisHitPoint.z), axisHitPoint, rotation),
                                    VertexUtils.RotatePointAroundPivot(new Vector3(axisHitPoint.x, axisHitPoint.y, axisHitPoint.z + 1), axisHitPoint, rotation));
                            break;
                        case AxisEditionState.Y:
                            p = new Plane(axisHitPoint,
                                    VertexUtils.RotatePointAroundPivot(new Vector3(axisHitPoint.x, axisHitPoint.y + 1, axisHitPoint.z), axisHitPoint, rotation),
                                    VertexUtils.RotatePointAroundPivot(new Vector3(axisHitPoint.x + 1, axisHitPoint.y, axisHitPoint.z), axisHitPoint, rotation));
                            break;
                        case AxisEditionState.Z:
                            p = new Plane(axisHitPoint,
                                    VertexUtils.RotatePointAroundPivot(new Vector3(axisHitPoint.x, axisHitPoint.y + 1, axisHitPoint.z), axisHitPoint, rotation),
                                    VertexUtils.RotatePointAroundPivot(new Vector3(axisHitPoint.x, axisHitPoint.y, axisHitPoint.z + 1), axisHitPoint, rotation));
                            break;
                    }
                }
            }
            return p;
        }


        public static void Update(byte actionMode, float distance, Vector3 position, Quaternion rotation, Camera cam/*, LineRenderer xLineComp, LineRenderer yLineComp, LineRenderer zLineComp */)
        {
            /*
            if (distance < 30)
                factor = .25f;
            else if (distance > 250)
                factor = 1.8f;
            else*/
            float factor = (0.0070455f * distance + 0.0386363f) * ProceduralObjectsMod.GizmoSize.value;
            float halfFactor = factor / 2f;

            renderers[0].widthMultiplier = factor;
            renderers[1].widthMultiplier = factor;
            renderers[2].widthMultiplier = factor;

            _gizmo[0].transform.position = position;
            _gizmo[1].transform.position = position;
            _gizmo[2].transform.position = position;

            if (canRegisterTyping)
                RegisterKeyTyping();

            if (referential == SpaceReferential.Local || actionMode == 1)
            {
                foreach (var obj in _gizmo)
                    obj.transform.rotation = rotation;
            }
            else
            {
                foreach (var obj in _gizmo)
                    obj.transform.rotation = Quaternion.identity;
            }

            if (actionMode < 2)
            {
                float factor20 = 20f * factor;

                _gizmo[0].transform.localScale = new Vector3(factor20, halfFactor, halfFactor);
                _gizmo[1].transform.localScale = new Vector3(halfFactor, factor20, halfFactor);
                _gizmo[2].transform.localScale = new Vector3(halfFactor, halfFactor, factor20);

                Quaternion rot = (referential == SpaceReferential.Local || actionMode == 1) ? rotation : Quaternion.identity;

                Vector3[] xPos = new Vector3[2];
                xPos[0] = position;
                xPos[1] = rot * (new Vector3((actionMode == 1 ? 19 : 20) * factor, 0, 0)) + position;
                renderers[0].SetPositions(xPos);
                
                Vector3[] yPos = new Vector3[2];
                yPos[0] = position;
                yPos[1] = rot * (new Vector3(0, (actionMode == 1 ? 19 : 20) * factor, 0)) + position;
                renderers[1].SetPositions(yPos);

                Vector3[] zPos = new Vector3[2];
                zPos[0] = position;
                zPos[1] = rot * (new Vector3(0, 0, (actionMode == 1 ? 19 : 20) * factor)) + position;
                renderers[2].SetPositions(zPos);

                var objPos = position.WorldToGuiPoint(cam);
                RightMostGUIPosGizmo = new Vector2(GUIUtils.RightMostPosition(objPos, xPos[1].WorldToGuiPoint(cam), yPos[1].WorldToGuiPoint(cam), zPos[1].WorldToGuiPoint(cam)).x + 5, objPos.y);
            }
            else if (actionMode == 2)
            {
                float factor4 = factor * 4f;
                _gizmo[0].transform.localScale = new Vector3(factor4, halfFactor, factor4);
                _gizmo[1].transform.localScale = new Vector3(factor4, factor4, halfFactor);
                _gizmo[2].transform.localScale = new Vector3(halfFactor, factor4, factor4);

                var rightPos = position + (cam.transform.rotation * Vector3.right) * 3 * factor4;
                RightMostGUIPosGizmo = new Vector2(rightPos.WorldToGuiPoint().x + 5, position.WorldToGuiPoint().y);
            }
        }
        
        public static void ClickAxis(AxisEditionState axis)
        {
            if (axis == AxisEditionState.none || !Exists)
                return;
            if (axis != AxisEditionState.X)
            {
                renderers[0].enabled = false;
                if (_scaleCubes != null)
                    _scaleCubes[0].GetComponent<MeshRenderer>().enabled = false;
            }
            if (axis != AxisEditionState.Y)
            {
                renderers[1].enabled = false;
                if (_scaleCubes != null)
                    _scaleCubes[1].GetComponent<MeshRenderer>().enabled = false;
            }
            if (axis != AxisEditionState.Z)
            {
                renderers[2].enabled = false;
                if (_scaleCubes != null)
                    _scaleCubes[2].GetComponent<MeshRenderer>().enabled = false;
            }
            EnableKeyTyping();
        }
        public static void ReleaseAxis()
        {
            if (!Exists)
                return;
            DisableKeyTyping();
            foreach (var rend in renderers)
                rend.enabled = true;
            if (_scaleCubes == null)
                return;
            _scaleCubes[0].GetComponent<MeshRenderer>().enabled = true;
            _scaleCubes[1].GetComponent<MeshRenderer>().enabled = true;
            _scaleCubes[2].GetComponent<MeshRenderer>().enabled = true;
        }
        public static void DestroyGizmo()
        {
            DisableKeyTyping();
            if (!Exists)
                return;
            if (_gizmo[0] != null)
                UnityEngine.Object.Destroy(_gizmo[0]);
            if (_gizmo[1] != null)
                UnityEngine.Object.Destroy(_gizmo[1]);
            if (_gizmo[2] != null)
                UnityEngine.Object.Destroy(_gizmo[2]);
            initialRotationTemp = Quaternion.identity;
            tempBuffer = null;
            _gizmo = null;
            renderers = null;
            _scaleCubes = null;
            posDiffSaved = Vector3.zero;
            useLineTool = false;
        }
        public static bool Exists
        {
            get { return _gizmo != null; }
        }

        private static GameObject[] _gizmo, _scaleCubes;
        public static Vector2 RightMostGUIPosGizmo = Vector2.zero;
        public static LineRenderer[] renderers;
        public static Quaternion initialRotationTemp = Quaternion.identity;
        public static Vector3[] tempBuffer = null;
        public static SpaceReferential referential = SpaceReferential.Local;
        public static float recordingStretch;

        public static Color GizmoRed = new Color(1f, 0f, 0f, ProceduralObjectsMod.GizmoOpacity.value);
        public static Color GizmoBlue = new Color(0f, 0f, 1f, ProceduralObjectsMod.GizmoOpacity.value);
        public static Color GizmoGreen = new Color(0f, 1f, 0f, ProceduralObjectsMod.GizmoOpacity.value);
        public static Color GizmoYellow = new Color(1f, 0.92f, 0.016f, ProceduralObjectsMod.GizmoOpacity.value);

        public static Vector3 posDiffSaved = Vector3.zero;
        public static bool useLineTool = false;

        public static Material spriteMat = new Material(Shader.Find("GUI/Text Shader"));

        // keyboard typed movements
        public static void EnableKeyTyping()
        {
            registeredString = "";
            registeredFloat = 0;
            isSnappingPrevMove = false;
            screenPos = Vector2.zero;
            canRegisterTyping = true;
        }
        public static void DisableKeyTyping()
        {
            canRegisterTyping = false;
            registeredString = "";
            registeredFloat = 0;
            screenPos = Vector2.zero;
            isSnappingPrevMove = false;
        }
        public static void RegisterKeyTyping()
        {
            if (registeredString.Length == 0)
            {
                if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
                {
                    registeredString = "-";
                    screenPos = GUIUtils.MousePos;
                }
            }
            else
            {
                if (screenPos != Vector2.zero)
                {
                    if (screenPos != GUIUtils.MousePos)
                    {
                        registeredString = "";
                        registeredFloat = 0;
                        screenPos = Vector2.zero;
                    }
                }

                if (Input.GetKeyDown(KeyCode.Backspace))
                {
                    registeredString = registeredString.Remove(registeredString.Length - 1);
                    ParseRegisterFloat();
                }
            }

            if (!registeredString.Contains('.') && registeredString.Length >= (registeredString.Contains('-') ? 2 : 1))
            {
                if (Input.GetKeyDown(KeyCode.Comma) || Input.GetKeyDown(KeyCode.Period) || Input.GetKeyDown(KeyCode.KeypadPeriod))
                    registeredString += '.';
            }
            for (int i = 0; i < 10; i++)
            {
                if (Input.GetKeyDown(i.ToString()) || Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode), "Keypad" + i.ToString())))
                {
                    if (i == 0)
                    {
                        if (registeredString == "0" || registeredString == "-0")
                            return;
                    }
                    if (registeredString == "")
                        screenPos = GUIUtils.MousePos;
                    registeredString += i.ToString();
                    ParseRegisterFloat();
                }
            }
        }
        private static void ParseRegisterFloat()
        {
            if (registeredString == "")
            {
                registeredFloat = 0;
                screenPos = Vector2.zero;
                return;
            }
            var last = registeredString.GetLastChar();
            if (last == '.')
            {
                registeredFloat = float.Parse(registeredString.Remove(registeredString.Length - 1));
                return;
            }
            else if (last == '-')
            {
                registeredFloat = 0;
                return;
            }
            else
                registeredFloat = float.Parse(registeredString);
        }
        public static float GetStoredDistanceValue
        {
            get
            {
                switch (ProceduralObjectsMod.DistanceUnits.value)
                {
                    case 1: //ft
                        return registeredFloat * 0.3048f;
                    case 2: //yd
                        return registeredFloat * 0.9144f;
                    default: //m
                        return registeredFloat;
                }
            }
        }
        public static float GetStoredAngleValue
        {
            get
            {
                switch (ProceduralObjectsMod.AngleUnits.value)
                {
                    case 1: //rad
                        return registeredFloat * Mathf.Rad2Deg;
                    default: // deg
                        return registeredFloat;
                }
            }
        }
        public static bool canRegisterTyping;
        private static Vector2 screenPos = Vector2.zero;
        public static string registeredString = "";
        public static float registeredFloat;

        // line copy tool
        public static void DetectRotationKeyboard()
        {
            var logic = ProceduralObjectsLogic.instance;
            if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveUp").GetBinding())
            {
                var rot = Quaternion.Euler(2 * TimeUtils.deltaTime, 0, 0);
                ApplyRotationLineTool(rot, logic);
            }
            if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").GetBinding())
            {
                var rot = Quaternion.Euler(-2 * TimeUtils.deltaTime, 0, 0);
                ApplyRotationLineTool(rot, logic);
            }
            if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBinding())
            {
                var rot = Quaternion.Euler(0, 2 * TimeUtils.deltaTime, 0);
                ApplyRotationLineTool(rot, logic);
            }
            if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBinding())
            {
                var rot = Quaternion.Euler(0, -2 * TimeUtils.deltaTime, 0);
                ApplyRotationLineTool(rot, logic);
            }
            if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBinding())
            {
                var rot = Quaternion.Euler(0, 0, 2 * TimeUtils.deltaTime);
                ApplyRotationLineTool(rot, logic);
            }
            if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBinding())
            {
                var rot = Quaternion.Euler(0, 0, -2 * TimeUtils.deltaTime);
                ApplyRotationLineTool(rot, logic);
            }
        }
        private static void ApplyRotationLineTool(Quaternion rot, ProceduralObjectsLogic logic)
        {
            logic.currentlyEditingObject.SetRotation(rot * logic.currentlyEditingObject.m_rotation);
            Gizmos.posDiffSaved = rot * Gizmos.posDiffSaved;
            logic.gizmoOffset = rot * logic.gizmoOffset;
            logic.axisHitPoint = VertexUtils.RotatePointAroundPivot(logic.axisHitPoint, logic.currentlyEditingObject.historyEditionBuffer.prevTempPos, rot);
            logic.currentlyEditingObject.m_position = VertexUtils.RotatePointAroundPivot(logic.currentlyEditingObject.m_position, logic.currentlyEditingObject.historyEditionBuffer.prevTempPos, rot);
        }

        public static float ConvertRoundToDistanceUnit(float meters)
        {
            switch (ProceduralObjectsMod.DistanceUnits.value)
            {
                case 1: //ft
                    return Mathf.Round(meters * 3.2808f);
                case 2: //yd
                    return Mathf.Round(meters * 1.0936f);
                default: //m
                    return Mathf.Round(meters);
            }
        }
        public static float ConvertRoundBackToMeters(float value)
        {
            switch (ProceduralObjectsMod.DistanceUnits.value)
            {
                case 1: //ft
                    return Mathf.Round(value / 3.2808f);
                case 2: //yd
                    return Mathf.Round(value / 1.0936f);
                default: //m
                    return Mathf.Round(value);
            }
        }

        public static bool isSnappingPrevMove;
        public static Vector3 SnapToPreviousMove(Vector3 axisPos, AxisEditionState axisInUse, ProceduralObject obj)
        {
            if (!Input.GetKey(KeyCode.LeftControl))
                goto defReturn;
            if (obj.historyEditionBuffer.stepsDone.Count == 0)
                goto defReturn;
            var laststep = obj.historyEditionBuffer.LastStep;
            if (laststep.axisUsed == AxisEditionState.none || laststep.type != EditingStep.StepType.position)
                goto defReturn;
            if (laststep.axisUsed != axisInUse)
                goto defReturn;

            Vector3 snapPos = laststep.positions.Value + (laststep.positions.Value - laststep.positions.Key);
            if ((snapPos - axisPos).magnitude <= 1.75f)
            {
                isSnappingPrevMove = true;
                return snapPos;
            }
        defReturn:
            isSnappingPrevMove = false;
            return axisPos;
        }

        public enum SpaceReferential
        {
            World,
            Local
        }
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
        
        public bool enableMovement, storedVertices;
        public byte toolType, planeUsed;
        public Dictionary<Vertex, Vector3> relativePositions;
        public Vector3 originHitPoint;
        public Vector2 originMousePosition;
        public Bounds verticesBounds;
        public Plane movementPlane; 
        public Ray originClickRay;
        public Dictionary<Vertex, Vertex> rotVertices;

        private float secClicked;
        private LineRenderer[] axis;
        private GameObject[] linesObj;

        public void IncrementStep()
        {
            if (!enableMovement)
            {
                if (secClicked >= .16f)
                    enableMovement = true;
                secClicked += TimeUtils.deltaTime;
            }
        }
        public void Store(Vector2 mousePos)
        {
            originClickRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            originMousePosition = mousePos;
        }
        public void Store(Vertex[] selectedVertices, ProceduralObject obj)
        {
            rotVertices = new Dictionary<Vertex, Vertex>();
            verticesBounds = new Bounds(selectedVertices[0].Position, Vector3.zero);
            foreach (var vertex in selectedVertices)
            {
                rotVertices[new Vertex(vertex)] = vertex;
                verticesBounds.Encapsulate(vertex.Position);
            }
            ProceduralObjectsLogic.PlaySound(3);
            if (toolType <= 1)
            {
                var angToYZPlaneNormal = Vector3.Angle(originClickRay.direction, (obj.m_rotation * Vector3.right).normalized);
                if (angToYZPlaneNormal > 90f) angToYZPlaneNormal = 180f - angToYZPlaneNormal;
                var angToXZPlaneNormal = Vector3.Angle(originClickRay.direction, (obj.m_rotation * Vector3.up).normalized);
                if (angToXZPlaneNormal > 90f) angToXZPlaneNormal = 180f - angToXZPlaneNormal;
                var angToXYPlaneNormal = Vector3.Angle(originClickRay.direction, (obj.m_rotation * Vector3.forward).normalized);
                if (angToXYPlaneNormal > 90f) angToXYPlaneNormal = 180f - angToXYPlaneNormal;
                var min = Mathf.Min(angToXZPlaneNormal, angToXYPlaneNormal, angToYZPlaneNormal);
                if (min == angToYZPlaneNormal)
                {
                    movementPlane = new Plane((obj.m_rotation * Vector3.right).normalized, obj.m_position + obj.m_rotation * verticesBounds.min);
                    planeUsed = 0;
                }
                else if (min == angToXZPlaneNormal)
                {
                    movementPlane = new Plane((obj.m_rotation * Vector3.up).normalized, obj.m_position + obj.m_rotation * verticesBounds.min);
                    planeUsed = 1;
                }
                else if (min == angToXYPlaneNormal)
                {
                    movementPlane = new Plane((obj.m_rotation * Vector3.forward).normalized, obj.m_position + obj.m_rotation * verticesBounds.min);
                    planeUsed = 2;
                }
                else
                    movementPlane = new Plane((obj.m_rotation * Vector3.right).normalized, obj.m_position + obj.m_rotation * verticesBounds.min);

            }
            if (toolType == 0)
            {
                float enter;
                if (movementPlane.Raycast(originClickRay, out enter))
                    originHitPoint = originClickRay.GetPoint(enter);
                else
                    originHitPoint = obj.m_position;

                relativePositions = new Dictionary<Vertex, Vector3>();
                foreach (var vertex in selectedVertices)
                    relativePositions[vertex] = vertex.Position - originHitPoint;

                SetupLinesPos(obj);
                if (Input.GetKey(KeyCode.LeftControl))
                    HideLines();
            }
            else if (toolType == 1)
            {
                if (obj.normalsRecalcMode == NormalsRecalculation.None)
                {
                    ProceduralObjectsLogic.PlaySound(2);
                    obj.normalsRecalcMode = NormalsRecalculation.Default;
                    obj.RecalculateNormals();
                }
                SetupLinesRot(obj);
            }

            storedVertices = true;
        }
        // move vertices
        public void ApplyToNewPosition(Vector3 newHitPoint, ProceduralObject obj, bool snapToAxis)
        {
            if (relativePositions == null)
                return;
            if (relativePositions.Count == 0)
                return;
            
            int line = -1;
            if (snapToAxis)
            {
                Vector3 xSnappedLocal = Vector3.zero, ySnappedLocal = Vector3.zero, zSnappedLocal = Vector3.zero;
                if (planeUsed > 0)
                    xSnappedLocal = Vector3.Project(newHitPoint - originHitPoint, obj.m_rotation * Vector3.right);
                if (planeUsed == 0 || planeUsed == 2)
                    ySnappedLocal = Vector3.Project(newHitPoint - originHitPoint, obj.m_rotation * Vector3.up);
                if (planeUsed == 0 || planeUsed == 1) 
                    zSnappedLocal = Vector3.Project(newHitPoint - originHitPoint, obj.m_rotation * Vector3.forward);

                if (planeUsed == 0)
                {
                    if (ySnappedLocal.sqrMagnitude > zSnappedLocal.sqrMagnitude)
                    {
                        if (zSnappedLocal.magnitude > 3f)
                            goto finallyNotSnapAxis;
                        newHitPoint = ySnappedLocal + originHitPoint;
                        line = 1;
                    }
                    else
                    {
                        if (ySnappedLocal.magnitude > 3f)
                            goto finallyNotSnapAxis;
                        newHitPoint = zSnappedLocal + originHitPoint;
                        line = 2;
                    }
                }
                else if (planeUsed == 1)
                {
                    if (xSnappedLocal.sqrMagnitude > zSnappedLocal.sqrMagnitude)
                    {
                        if (zSnappedLocal.magnitude > 3f)
                            goto finallyNotSnapAxis;
                        newHitPoint = xSnappedLocal + originHitPoint;
                        line = 0;
                    }
                    else
                    {
                        if (xSnappedLocal.magnitude > 3f)
                            goto finallyNotSnapAxis;
                        newHitPoint = zSnappedLocal + originHitPoint;
                        line = 2;
                    }
                }
                else if (planeUsed == 2)
                {
                    if (xSnappedLocal.sqrMagnitude > ySnappedLocal.sqrMagnitude)
                    {
                        if (ySnappedLocal.magnitude > 3f)
                            goto finallyNotSnapAxis;
                        newHitPoint = xSnappedLocal + originHitPoint;
                        line = 0;
                    }
                    else
                    {
                        if (xSnappedLocal.magnitude > 3f)
                            goto finallyNotSnapAxis;
                        newHitPoint = ySnappedLocal + originHitPoint;
                        line = 1;
                    }
                }

            finallyNotSnapAxis:
                this.HighlightLine(line);
                if (line == -1)
                {
                    if (Gizmos.canRegisterTyping)
                        Gizmos.DisableKeyTyping();
                }
                else if (!Gizmos.canRegisterTyping)
                    Gizmos.EnableKeyTyping();

                if (Gizmos.canRegisterTyping)
                    Gizmos.RegisterKeyTyping();
            }
            if (Gizmos.registeredFloat != 0)
            {
                if (line == 0)
                    newHitPoint = originHitPoint + (obj.m_rotation * (Vector3.right * Gizmos.GetStoredDistanceValue));
                else if (line == 1)
                    newHitPoint = originHitPoint + (obj.m_rotation * (Vector3.up * Gizmos.GetStoredDistanceValue));
                else if (line == 2)
                    newHitPoint = originHitPoint + (obj.m_rotation * (Vector3.forward * Gizmos.GetStoredDistanceValue));
            }
            var referencial = VertexUtils.RotatePointAroundPivot(newHitPoint, originHitPoint, Quaternion.Inverse(obj.m_rotation));
            if (obj.isPloppableAsphalt)
            {
                Vector3 diff = VertexUtils.RotatePointAroundPivot(newHitPoint, obj.m_position, Quaternion.Inverse(obj.m_rotation)) - VertexUtils.RotatePointAroundPivot(originHitPoint, obj.m_position, Quaternion.Inverse(obj.m_rotation));
                referencial -= new Vector3(0, 0, 1.4672f * diff.z);
            }
            foreach (KeyValuePair<Vertex, Vector3> kvp in relativePositions)
            {
                kvp.Key.Position = kvp.Value + referencial;
            }
        }
        // rotate vertices
        public void ApplyToNewPosition(float mousePosX)
        {
            if (Gizmos.canRegisterTyping)
                Gizmos.RegisterKeyTyping();

            Quaternion rot;
            Vector3 axis = planeUsed == 0 ? Vector3.right : (planeUsed == 1 ? Vector3.up : Vector3.forward);
            if (Gizmos.registeredString != "")
                rot = Quaternion.AngleAxis(Gizmos.GetStoredAngleValue, axis);
            else
                rot = Quaternion.AngleAxis(((originMousePosition.x - mousePosX) * 370f) / Screen.width, axis);

            foreach (KeyValuePair<Vertex, Vertex> kvp in rotVertices)
            {
                kvp.Value.Position = VertexUtils.RotatePointAroundPivot(kvp.Key.Position, verticesBounds.center, rot);
            }
        }
        // scale vertices
        public void ApplyToNewPosition(Vector2 newMousePos)
        {
            if (Gizmos.canRegisterTyping)
                Gizmos.RegisterKeyTyping();

            float factor = 1;
            if (Gizmos.registeredString != "")
                factor = Gizmos.registeredFloat;
            else
            {
                if ((newMousePos.x - originMousePosition.x) < 0)
                    factor = (1.6f / Screen.width) * (newMousePos.x - originMousePosition.x) + 1;
                else
                    factor = (2.4f / Screen.width) * (newMousePos.x - originMousePosition.x) + 1;
            }
            foreach (KeyValuePair<Vertex, Vertex> kvp in rotVertices)
            {
                kvp.Value.Position = ((kvp.Key.Position - verticesBounds.center) * factor) + verticesBounds.center;
            }
        }

        // Snap to Axis methods
        public void SetupLinesPos(ProceduralObject obj)
        {
            linesObj = new GameObject[] { new GameObject("PO_verticesLineX"), new GameObject("PO_verticesLineY"), new GameObject("PO_verticesLineZ") };
            axis = new LineRenderer[] { linesObj[0].AddComponent<LineRenderer>(), linesObj[1].AddComponent<LineRenderer>(), linesObj[2].AddComponent<LineRenderer>() };

            setupAxis(0, Color.red);
            if (planeUsed > 0)
                axis[0].SetPositions(new Vector3[] { originHitPoint + (obj.m_rotation * Vector3.right) * 500f, originHitPoint + (obj.m_rotation * Vector3.left) * 500f });

            setupAxis(1, Color.green);
            if (planeUsed == 0 || planeUsed == 2)
                axis[1].SetPositions(new Vector3[] { originHitPoint + (obj.m_rotation * Vector3.up) * 500f, originHitPoint + (obj.m_rotation * Vector3.down) * 500f });

            setupAxis(2, Color.blue);
            if (planeUsed == 0 || planeUsed == 1)
                axis[2].SetPositions(new Vector3[] { originHitPoint + (obj.m_rotation * Vector3.forward) * 500f, originHitPoint + (obj.m_rotation * Vector3.back) * 500f });
        }
        public void SetupLinesRot(ProceduralObject obj)
        {
            linesObj = new GameObject[] { new GameObject("PO_verticesLineX"), new GameObject("PO_verticesLineY"), new GameObject("PO_verticesLineZ") };
            axis = new LineRenderer[] { linesObj[0].AddComponent<LineRenderer>(), linesObj[1].AddComponent<LineRenderer>(), linesObj[2].AddComponent<LineRenderer>() };
            var worldBoundsCenter = (obj.m_rotation * verticesBounds.center) + obj.m_position;

            setupAxis(0, Color.red);
            if (planeUsed == 0)
                axis[0].SetPositions(new Vector3[] { worldBoundsCenter + (obj.m_rotation * Vector3.right) * 500f, worldBoundsCenter + (obj.m_rotation * Vector3.left) * 500f });

            setupAxis(1, Color.green);
            if (planeUsed == 1)
                axis[1].SetPositions(new Vector3[] { worldBoundsCenter + (obj.m_rotation * Vector3.up) * 500f, worldBoundsCenter + (obj.m_rotation * Vector3.down) * 500f });

            setupAxis(2, Color.blue);
            if (planeUsed == 2)
                axis[2].SetPositions(new Vector3[] { worldBoundsCenter + (obj.m_rotation * Vector3.forward) * 500f, worldBoundsCenter + (obj.m_rotation * Vector3.back) * 500f });
        }
        private void setupAxis(int i, Color c)
        {
            axis[i].startColor = c;
            axis[i].endColor = c;
            axis[i].material = Gizmos.spriteMat;
            axis[i].widthMultiplier = 0.26f * AxisWidthFactor;
        }
        public void ShowLines()
        {
            if (axis == null)
                return;
            axis[0].enabled = true;
            axis[1].enabled = true;
            axis[2].enabled = true;
            Gizmos.EnableKeyTyping();
        }
        public void HideLines()
        {
            if (axis == null)
                return;
            axis[0].enabled = false;
            axis[1].enabled = false;
            axis[2].enabled = false;
            Gizmos.DisableKeyTyping();
        }
        public void DestroyLines()
        {
            if (linesObj == null)
                return;
            UnityEngine.Object.Destroy(linesObj[0]);
            UnityEngine.Object.Destroy(linesObj[1]);
            UnityEngine.Object.Destroy(linesObj[2]);
        }
        private void HighlightLine(int i)
        {
            if (axis == null)
                return;

            for (int j = 0; j < 3; j++)
            {
                if (i == j)
                {
                    var toHighlight = axis[j];
                    toHighlight.widthMultiplier = 0.26f * AxisWidthFactor;
                    toHighlight.startColor = new Color(toHighlight.startColor.r, toHighlight.startColor.g, toHighlight.startColor.b, 0.65f);
                }
                else
                {
                    var toDampen = axis[j];
                    toDampen.widthMultiplier = 0.12f * AxisWidthFactor;
                    toDampen.startColor = new Color(toDampen.startColor.r, toDampen.startColor.g, toDampen.startColor.b, 0.2f);
                }
            }
        }
        private float AxisWidthFactor
        {
            get
            {
                return ((ProceduralObjectsMod.GizmoSize.value - 1f) / 2f) + 1f;
            }
        } 
    }
}
 