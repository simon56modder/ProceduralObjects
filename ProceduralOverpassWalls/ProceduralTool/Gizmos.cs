﻿using UnityEngine;
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
        public static float recordingStretch, recordingAngle;

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
        public static float ConvertToDistanceUnit(float meters)
        {
            switch (ProceduralObjectsMod.DistanceUnits.value)
            {
                case 1: //ft
                    return meters * 3.2808f;
                case 2: //yd
                    return meters * 1.0936f;
                default: //m
                    return meters;
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

        public class GrabbablePoints
        {
            public GrabbablePoints(Vector3[] points)
            {
                this.points = points;
                this.selected = new bool[points.Length];
                for (int i = 0; i < points.Length; i++)
                    this.selected[i] = false;
                rightClickPos = Vector2.down;
                kbSlow = KeyBindingsManager.instance.GetBindingFromName("edition_smallMovements");
                kbSmooth = KeyBindingsManager.instance.GetBindingFromName("edition_smoothMovements");
                logic = ProceduralObjectsLogic.instance;
                prevActions = new List<Vector3[]>();
                nextActions = new List<Vector3[]>();
                RegisterAction();
            }
            public Vector3[] points;
            public bool[] selected;
            public KeyBindingInfo kbSmooth, kbSlow;
            private Vector2 rightClickPos;
            private ProceduralObjectsLogic logic;

            private float secClicked;
            private bool enableMovement;
            private byte planeUsed;
            public Vector3 originHitPoint;
            public Vector2 originMousePosition;
            public Plane movementPlane;
            public Ray originClickRay;
            private Vector3[] relativePositions;

            public void Update(Vector3 refCenter, Quaternion refRot, Action processMovement)
            {
                bool smooth = kbSmooth.GetBinding();
                bool slow = kbSlow.GetBinding();
                if (smooth)
                {
                    var bUp = KeyBindingsManager.instance.GetBindingFromName("position_moveUp");
                    var bDown = KeyBindingsManager.instance.GetBindingFromName("position_moveDown");
                    var bLeft = KeyBindingsManager.instance.GetBindingFromName("position_moveLeft");
                    var bRight = KeyBindingsManager.instance.GetBindingFromName("position_moveRight");
                    var bFwd = KeyBindingsManager.instance.GetBindingFromName("position_moveForward");
                    var bBwd = KeyBindingsManager.instance.GetBindingFromName("position_moveBackward");
                    if (bUp.GetBinding())
                        MovePoints(Vector3.up, slow, true, processMovement);
                    if (bDown.GetBinding())
                        MovePoints(Vector3.down, slow, true, processMovement);
                    if (bLeft.GetBinding())
                        MovePoints(Vector3.left, slow, true, processMovement);
                    if (bRight.GetBinding())
                        MovePoints(Vector3.right, slow, true, processMovement);
                    if (bFwd.GetBinding())
                        MovePoints(Vector3.forward, slow, true, processMovement);
                    if (bBwd.GetBinding())
                        MovePoints(Vector3.back, slow, true, processMovement);

                    if (bUp.GetBindingUp() || bDown.GetBindingUp() || bLeft.GetBindingUp() || bRight.GetBindingUp() || bFwd.GetBindingUp() || bBwd.GetBindingUp())
                        RegisterAction();
                }
                else
                {
                    if (KeyBindingsManager.instance.GetBindingFromName("position_moveUp").GetBindingDown())
                        MovePoints(Vector3.up, slow, false, processMovement);
                    if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBindingDown())
                        MovePoints(Vector3.down, slow, false, processMovement);
                    if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBindingDown())
                        MovePoints(Vector3.left, slow, false, processMovement);
                    if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBindingDown())
                        MovePoints(Vector3.right, slow, false, processMovement);
                    if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBindingDown())
                        MovePoints(Vector3.forward, slow, false, processMovement);
                    if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBindingDown())
                        MovePoints(Vector3.back, slow, false, processMovement);
                }

                if (Input.GetMouseButtonDown(1))
                {
                    var mouse = GUIUtils.MousePos;
                    if (!logic.IsInWindowElement(mouse))
                        rightClickPos = mouse;
                }
                else if (Input.GetMouseButtonUp(1))
                {
                    var rect = GUIUtils.RectFromCorners(rightClickPos, GUIUtils.MousePos, true);
                    var control = Input.GetKey(KeyCode.LeftControl);
                    if (!control)
                        selected = new bool[] { false, false, false, false, false, false, false, false };
                    for (int i = 0; i < points.Count(); i++)
                    {
                        if (rect.Contains((refRot * points[i] + refCenter).WorldToGuiPoint()))
                            selected[i] = control ? true : !selected[i];
                    }
                    rightClickPos = Vector2.down;
                }

                if (KeyBindingsManager.instance.GetBindingFromName("redo").GetBindingDown())
                    Redo(processMovement);
                else if (KeyBindingsManager.instance.GetBindingFromName("undo").GetBindingDown())
                    Undo(processMovement);

                if (AnySelected())
                {
                    if (Input.GetMouseButtonDown(0))
                        InitialClick();
                    else if (Input.GetMouseButtonUp(0) || (Gizmos.registeredString != "" && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))))
                    {
                        Gizmos.DisableKeyTyping();
                        VerticesWizardData.DestroyLines();
                        secClicked = 0f;
                        if (enableMovement)
                            RegisterAction();
                        enableMovement = false;
                        return;
                    }
                    else if (Input.GetMouseButton(0))
                    {
                        if (!enableMovement)
                        {
                            if (secClicked >= .16f)
                            {
                                StoreClickData(refCenter, refRot);
                                enableMovement = true;
                            }
                            secClicked += TimeUtils.deltaTime;
                        }
                        else
                        {
                            if (Input.GetKeyDown(KeyCode.LeftControl))
                                VerticesWizardData.HideLines();
                            else if (Input.GetKeyUp(KeyCode.LeftControl))
                                VerticesWizardData.ShowLines();

                            Ray ray = logic.renderCamera.ScreenPointToRay(Input.mousePosition);
                            float enter;
                            if (movementPlane.Raycast(ray, out enter))
                            {
                                var point = ray.GetPoint(enter);
                                ApplyToNewPosition(point, refRot, !Input.GetKey(KeyCode.LeftControl));
                                processMovement.Invoke();
                            }
                        }
                    }
                }
            }
            public void OnGUI(Vector3 refCenter, Quaternion refRot)
            {
                if (rightClickPos != Vector2.down)
                {
                    GUI.color = logic.uiColor;
                    GUI.Box(GUIUtils.RectFromCorners(rightClickPos, GUIUtils.MousePos, true), "");
                    GUI.color = Color.white;
                }
                for (int i = 0; i < points.Count(); i++)
                {
                    if (GUI.Button(new Rect((refRot * points[i] + refCenter).WorldToGuiPoint() + new Vector2(-10, -10), new Vector2(20, 20)), VertexUtils.vertexIcons[selected[i] ? 1 : 0], GUI.skin.label))
                    {
                        ProceduralObjectsLogic.PlaySound();
                        if (!Input.GetKey(KeyCode.LeftControl))
                        {
                            for (int j = 0; j < points.Length; j++)
                                this.selected[j] = false;
                        }
                        selected[i] = !selected[i];
                    }
                }
            }

            public bool AnySelected()
            {
                if (points == null || selected == null)
                    return false;
                for (int i = 0; i < selected.Length; i++)
                {
                    if (selected[i])
                        return true;
                }
                return false;
            }
            public void ScaleWithPgUpPgDown(Action processScale)
            {
                if (points == null || selected == null || kbSmooth == null || kbSlow == null) return;

                var kbScaleUp = KeyBindingsManager.instance.GetBindingFromName("scale_scaleUp");
                var kbScaleDown = KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown");
                if (kbSmooth.GetBinding())
                {
                    if (kbScaleUp.GetBinding())
                        ScalePoints(.3f, kbSlow.GetBinding(), true, processScale);
                    if (kbScaleDown.GetBinding())
                        ScalePoints(-.3f, kbSlow.GetBinding(), true, processScale);

                    if (kbScaleUp.GetBindingUp() || kbScaleDown.GetBindingUp())
                        RegisterAction();
                }
                else
                {
                    if (kbScaleUp.GetBindingDown())
                        ScalePoints(.2f, kbSlow.GetBinding(), false, processScale);
                    if (kbScaleDown.GetBindingDown())
                        ScalePoints(-.2f, kbSlow.GetBinding(), false, processScale);
                }
            }

            private void InitialClick()
            {
                originClickRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                originMousePosition = GUIUtils.MousePos;
            }
            private void StoreClickData(Vector3 refCenter, Quaternion refRot)
            {
                var angToYZPlaneNormal = Vector3.Angle(originClickRay.direction, (refRot * Vector3.right).normalized);
                if (angToYZPlaneNormal > 90f) angToYZPlaneNormal = 180f - angToYZPlaneNormal;
                var angToXZPlaneNormal = Vector3.Angle(originClickRay.direction, (refRot * Vector3.up).normalized);
                if (angToXZPlaneNormal > 90f) angToXZPlaneNormal = 180f - angToXZPlaneNormal;
                var angToXYPlaneNormal = Vector3.Angle(originClickRay.direction, (refRot * Vector3.forward).normalized);
                if (angToXYPlaneNormal > 90f) angToXYPlaneNormal = 180f - angToXYPlaneNormal;
                var min = Mathf.Min(angToXZPlaneNormal, angToXYPlaneNormal, angToYZPlaneNormal);
                if (min == angToYZPlaneNormal)
                {
                    movementPlane = new Plane((refRot * Vector3.right).normalized, refCenter);
                    planeUsed = 0;
                }
                else if (min == angToXZPlaneNormal)
                {
                    movementPlane = new Plane((refRot * Vector3.up).normalized, refCenter);
                    planeUsed = 1;
                }
                else if (min == angToXYPlaneNormal)
                {
                    movementPlane = new Plane((refRot * Vector3.forward).normalized, refCenter);
                    planeUsed = 2;
                }
                else
                    movementPlane = new Plane((refRot * Vector3.right).normalized, refCenter);

                float enter;
                if (movementPlane.Raycast(originClickRay, out enter))
                    originHitPoint = originClickRay.GetPoint(enter);
                else
                    originHitPoint = refCenter;
                relativePositions = new Vector3[points.Length];
                for (int i = 0; i < selected.Length; i++)
                {
                    if (!selected[i])
                        relativePositions[i] = Vector3.zero;
                    else
                        relativePositions[i] = points[i] - originHitPoint;
                }
                VerticesWizardData.SetupLinesPosition(originHitPoint, refRot, planeUsed);
                if (Input.GetKey(KeyCode.LeftControl))
                    VerticesWizardData.HideLines();
            }
            private void ApplyToNewPosition(Vector3 newHitPoint, Quaternion rot, bool snapToAxis)
            {
                if (relativePositions == null)
                    return;
                if (relativePositions.Length == 0)
                    return;
                int line = -1;
                if (snapToAxis)
                {
                    VerticesWizardData.smallAxis = Vector3.Distance(ProceduralObjectsLogic.instance.renderCamera.transform.position, originHitPoint) <= 15;
                    Vector3 xSnappedLocal = Vector3.zero, ySnappedLocal = Vector3.zero, zSnappedLocal = Vector3.zero;
                    if (planeUsed > 0)
                        xSnappedLocal = Vector3.Project(newHitPoint - originHitPoint, rot * Vector3.right);
                    if (planeUsed == 0 || planeUsed == 2)
                        ySnappedLocal = Vector3.Project(newHitPoint - originHitPoint, rot * Vector3.up);
                    if (planeUsed == 0 || planeUsed == 1)
                        zSnappedLocal = Vector3.Project(newHitPoint - originHitPoint, rot * Vector3.forward);
                    float snapThreshold = (VerticesWizardData.smallAxis) ? .7f : 3f;
                    if (planeUsed == 0)
                    {
                        if (ySnappedLocal.sqrMagnitude > zSnappedLocal.sqrMagnitude)
                        {
                            if (zSnappedLocal.magnitude > snapThreshold)
                                goto finallyNotSnapAxis;
                            newHitPoint = ySnappedLocal + originHitPoint;
                            line = 1;
                        }
                        else
                        {
                            if (ySnappedLocal.magnitude > snapThreshold)
                                goto finallyNotSnapAxis;
                            newHitPoint = zSnappedLocal + originHitPoint;
                            line = 2;
                        }
                    }
                    else if (planeUsed == 1)
                    {
                        if (xSnappedLocal.sqrMagnitude > zSnappedLocal.sqrMagnitude)
                        {
                            if (zSnappedLocal.magnitude > snapThreshold)
                                goto finallyNotSnapAxis;
                            newHitPoint = xSnappedLocal + originHitPoint;
                            line = 0;
                        }
                        else
                        {
                            if (xSnappedLocal.magnitude > snapThreshold)
                                goto finallyNotSnapAxis;
                            newHitPoint = zSnappedLocal + originHitPoint;
                            line = 2;
                        }
                    }
                    else if (planeUsed == 2)
                    {
                        if (xSnappedLocal.sqrMagnitude > ySnappedLocal.sqrMagnitude)
                        {
                            if (ySnappedLocal.magnitude > snapThreshold)
                                goto finallyNotSnapAxis;
                            newHitPoint = xSnappedLocal + originHitPoint;
                            line = 0;
                        }
                        else
                        {
                            if (xSnappedLocal.magnitude > snapThreshold)
                                goto finallyNotSnapAxis;
                            newHitPoint = ySnappedLocal + originHitPoint;
                            line = 1;
                        }
                    }

                finallyNotSnapAxis:
                    VerticesWizardData.HighlightLine(line);
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
                        newHitPoint = originHitPoint + (rot * (Vector3.right * Gizmos.GetStoredDistanceValue));
                    else if (line == 1)
                        newHitPoint = originHitPoint + (rot * (Vector3.up * Gizmos.GetStoredDistanceValue));
                    else if (line == 2)
                        newHitPoint = originHitPoint + (rot * (Vector3.forward * Gizmos.GetStoredDistanceValue));
                }
                var referencial = VertexUtils.RotatePointAroundPivot(newHitPoint, originHitPoint, Quaternion.Inverse(rot));
                for (int i = 0; i < points.Length; i++)
                {
                    if (selected[i])
                        points[i] = relativePositions[i] + referencial;
                }
            }
            private void MovePoints(Vector3 movement, bool slow, bool smooth, Action processMovement)
            {
                if (smooth)
                    movement *= TimeUtils.deltaTime * (slow ? 1f : 8f);
                else
                    movement *= slow ? .5f : 4f;
                bool changedsmth = false;
                for (int i = 0; i < points.Length; i++)
                {
                    if (!selected[i]) continue;
                    points[i] = points[i] + movement;
                    changedsmth = true;
                }
                if (changedsmth)
                {
                    processMovement.Invoke();
                    if (!smooth) RegisterAction();
                }
            }
            private void ScalePoints(float factor, bool slow, bool smooth, Action processMovement)
            {
                if (smooth)
                    factor = 1 + (TimeUtils.deltaTime * (slow ? (factor / 3f) : factor));
                else
                    factor = 1 + (slow ? (factor / 3f) : factor);
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] *= factor;
                }
                processMovement.Invoke();
                if (!smooth) RegisterAction();
            }

            private List<Vector3[]> prevActions, nextActions;
            private void RegisterAction()
            {
                if (prevActions.Count > 20)
                    prevActions.RemoveAt(0);
                prevActions.Add(points.ToArray());
                nextActions.Clear();
            }
            private void Undo(Action processMovement)
            {
                if (prevActions.Count <= 1) return;
                var p = prevActions[prevActions.Count - 2];
                nextActions.Add(points.ToArray());
                prevActions.RemoveAt(prevActions.Count - 1);
                points = p.ToArray();
                processMovement.Invoke();
            }
            private void Redo(Action processMovement)
            {
                if (nextActions.Count == 0) return;
                var p = nextActions[nextActions.Count - 1];
                prevActions.Add(p.ToArray());
                nextActions.RemoveAt(nextActions.Count - 1);
                points = p.ToArray();
                processMovement.Invoke();
            }
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
        private static  LineRenderer[] axis;
        private static GameObject[] linesObj;
        public static bool smallAxis;

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
                if (obj.normalsRecalcMode == NormalsRecalculation.None && !obj.IsPloppableAsphalt())
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
                smallAxis = Vector3.Distance(ProceduralObjectsLogic.instance.renderCamera.transform.position, originHitPoint) <= 15;
                Vector3 xSnappedLocal = Vector3.zero, ySnappedLocal = Vector3.zero, zSnappedLocal = Vector3.zero;
                if (planeUsed > 0)
                    xSnappedLocal = Vector3.Project(newHitPoint - originHitPoint, obj.m_rotation * Vector3.right);
                if (planeUsed == 0 || planeUsed == 2)
                    ySnappedLocal = Vector3.Project(newHitPoint - originHitPoint, obj.m_rotation * Vector3.up);
                if (planeUsed == 0 || planeUsed == 1) 
                    zSnappedLocal = Vector3.Project(newHitPoint - originHitPoint, obj.m_rotation * Vector3.forward);
                float snapThreshold = (smallAxis) ? .7f : 3f;
                if (planeUsed == 0)
                {
                    if (ySnappedLocal.sqrMagnitude > zSnappedLocal.sqrMagnitude)
                    {
                        if (zSnappedLocal.magnitude > snapThreshold)
                            goto finallyNotSnapAxis;
                        newHitPoint = ySnappedLocal + originHitPoint;
                        line = 1;
                    }
                    else
                    {
                        if (ySnappedLocal.magnitude > snapThreshold)
                            goto finallyNotSnapAxis;
                        newHitPoint = zSnappedLocal + originHitPoint;
                        line = 2;
                    }
                }
                else if (planeUsed == 1)
                {
                    if (xSnappedLocal.sqrMagnitude > zSnappedLocal.sqrMagnitude)
                    {
                        if (zSnappedLocal.magnitude > snapThreshold)
                            goto finallyNotSnapAxis;
                        newHitPoint = xSnappedLocal + originHitPoint;
                        line = 0;
                    }
                    else
                    {
                        if (xSnappedLocal.magnitude > snapThreshold)
                            goto finallyNotSnapAxis;
                        newHitPoint = zSnappedLocal + originHitPoint;
                        line = 2;
                    }
                }
                else if (planeUsed == 2)
                {
                    if (xSnappedLocal.sqrMagnitude > ySnappedLocal.sqrMagnitude)
                    {
                        if (ySnappedLocal.magnitude > snapThreshold)
                            goto finallyNotSnapAxis;
                        newHitPoint = xSnappedLocal + originHitPoint;
                        line = 0;
                    }
                    else
                    {
                        if (xSnappedLocal.magnitude > snapThreshold)
                            goto finallyNotSnapAxis;
                        newHitPoint = ySnappedLocal + originHitPoint;
                        line = 1;
                    }
                }

            finallyNotSnapAxis:
                HighlightLine(line);
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
            SetupLinesPosition(originHitPoint, obj.m_rotation, planeUsed);
        }
        public static void SetupLinesPosition(Vector3 hitPoint, Quaternion rot, byte planeUsed)
        {
            linesObj = new GameObject[] { new GameObject("PO_verticesLineX"), new GameObject("PO_verticesLineY"), new GameObject("PO_verticesLineZ") };
            axis = new LineRenderer[] { linesObj[0].AddComponent<LineRenderer>(), linesObj[1].AddComponent<LineRenderer>(), linesObj[2].AddComponent<LineRenderer>() };

            setupAxis(0, Color.red);
            if (planeUsed > 0)
                axis[0].SetPositions(new Vector3[] { hitPoint + (rot * Vector3.right) * 500f, hitPoint + (rot * Vector3.left) * 500f });

            setupAxis(1, Color.green);
            if (planeUsed == 0 || planeUsed == 2)
                axis[1].SetPositions(new Vector3[] { hitPoint + (rot * Vector3.up) * 500f, hitPoint + (rot * Vector3.down) * 500f });

            setupAxis(2, Color.blue);
            if (planeUsed == 0 || planeUsed == 1)
                axis[2].SetPositions(new Vector3[] { hitPoint + (rot * Vector3.forward) * 500f, hitPoint + (rot * Vector3.back) * 500f });
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
        private static void setupAxis(int i, Color c)
        {
            axis[i].startColor = c;
            axis[i].endColor = c;
            axis[i].material = Gizmos.spriteMat;
            axis[i].widthMultiplier = 0.26f * AxisWidthFactor;
        }
        public static void ShowLines()
        {
            if (axis == null)
                return;
            axis[0].enabled = true;
            axis[1].enabled = true;
            axis[2].enabled = true;
            Gizmos.EnableKeyTyping();
        }
        public static void HideLines()
        {
            if (axis == null)
                return;
            axis[0].enabled = false;
            axis[1].enabled = false;
            axis[2].enabled = false;
            Gizmos.DisableKeyTyping();
        }
        public static void DestroyLines()
        {
            if (linesObj == null)
                return;
            UnityEngine.Object.Destroy(linesObj[0]);
            UnityEngine.Object.Destroy(linesObj[1]);
            UnityEngine.Object.Destroy(linesObj[2]);
            linesObj = null;
        }
        public static void HighlightLine(int i)
        {
            if (axis == null)
                return;

            for (int j = 0; j < 3; j++)
            {
                if (i == j)
                {
                    var toHighlight = axis[j];
                    toHighlight.widthMultiplier = 0.26f * AxisWidthFactor * (smallAxis ? .25f : 1f);
                    toHighlight.startColor = new Color(toHighlight.startColor.r, toHighlight.startColor.g, toHighlight.startColor.b, 0.65f);
                }
                else
                {
                    var toDampen = axis[j];
                    toDampen.widthMultiplier = 0.12f * AxisWidthFactor * (smallAxis ? .25f : 1f);
                    toDampen.startColor = new Color(toDampen.startColor.r, toDampen.startColor.g, toDampen.startColor.b, 0.2f);
                }
            }
        }
        private static float AxisWidthFactor
        {
            get
            {
                return ((ProceduralObjectsMod.GizmoSize.value - 1f) / 2f) + 1f;
            }
        } 
    }

    public class DrawWizardData
    {
        public DrawWizardData(ProceduralObject obj)
        {
            zeroLevelPlane = new Plane(obj.m_rotation * Vector3.up, obj.m_position);
            this.obj = obj;
        }

        public Plane zeroLevelPlane;
        public List<Vector3> points;
        public ProceduralObject obj;

        public void Update()
        {
            Vector3 pos = Vector3.zero;
            if (points != null)
            {
                var ray = ProceduralObjectsLogic.instance.renderCamera.ScreenPointToRay(Input.mousePosition);
                float enter;
                if (zeroLevelPlane.Raycast(ray, out enter))
                {
                    pos = ray.GetPoint(enter);
                    SetVertices(pos, true);
                }
            }
            if (Input.GetMouseButtonDown(0))
            {
                if (points == null)
                {
                    ProceduralObjectsLogic.PlaySound();
                    obj.vertices = (obj.baseInfoType == "BUILDING") ? Vertex.CreateVertexList(obj._baseBuilding) : Vertex.CreateVertexList(obj._baseProp);
                    obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, obj.vertices);
                    points = new List<Vector3>();
                }
                else
                {
                    ProceduralObjectsLogic.PlaySound(3);
                    obj.historyEditionBuffer.ConfirmNewStep(obj.vertices);
                    points.Add(pos);
                    if (points.Count == 28)
                        Confirm();
                }
            }
            else if (points != null)
            {
                if (points.Count > 0)
                {
                    if (Input.GetMouseButtonDown(1))
                    {
                        ProceduralObjectsLogic.PlaySound(3);
                        obj.historyEditionBuffer.ConfirmNewStep(obj.vertices);
                        points.RemoveAt(points.Count - 1);
                        obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, obj.vertices);
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
                Confirm();
        }
        public void Confirm()
        {
            ProceduralObjectsLogic.PlaySound();
            SetVertices(Vector3.zero, false);
            points = null;
            obj.historyEditionBuffer.ConfirmNewStep(obj.vertices);
            ProceduralObjectsLogic.verticesToolType = 0;
            ProceduralObjectsLogic.instance.drawWizardData = null;
        }
        public void SetVertices(Vector3 worldPos, bool useEditingPos)
        {
            var pointsWithMouse = new List<Vector3>(points);
            if (useEditingPos)
                pointsWithMouse.Add(worldPos);
            if (IsClockwise(pointsWithMouse))
            {
                for (int i = 0; i < 28; i ++ )
                {
                    if (i < pointsWithMouse.Count - 1)
                    {
                        var localPos = getLocalPloppablePos(pointsWithMouse[i]);
                        foreach (Vertex v in obj.vertices.Where(v => v.Index == i || (v.IsDependent && v.DependencyIndex == i)))
                            obj.vertices[v.Index].Position = localPos;
                    }
                    else
                    {
                        var localPos = getLocalPloppablePos(pointsWithMouse[pointsWithMouse.Count - 1]);
                        foreach (Vertex v in obj.vertices.Where(v => v.Index == i || (v.IsDependent && v.DependencyIndex == i)))
                            obj.vertices[v.Index].Position = localPos;
                    }
                }
            }
            else
            {
                for (int i = 0; i < 28; i++)
                {
                    if (i < 29 - pointsWithMouse.Count)
                    {
                        var localPos = getLocalPloppablePos(pointsWithMouse[pointsWithMouse.Count - 1]);
                        foreach (Vertex v in obj.vertices.Where(v => v.Index == i || (v.IsDependent && v.DependencyIndex == i)))
                            obj.vertices[v.Index].Position = localPos;
                    }
                    else
                    {
                        var localPos = getLocalPloppablePos(pointsWithMouse[27 - i]);
                        foreach (Vertex v in obj.vertices.Where(v => v.Index == i || (v.IsDependent && v.DependencyIndex == i)))
                            obj.vertices[v.Index].Position = localPos;
                    }
                }
            }
            obj.ApplyModelChange();
            SetCenterVertex();
        }
        private void SetCenterVertex()
        {
            if (points.Count <= 2)
            {
                foreach (Vertex v in obj.vertices.Where(v => v.Index == 28 || (v.IsDependent && v.DependencyIndex == 28)))
                    obj.vertices[v.Index].Position = obj.vertices[0].Position;
                ProceduralUtils.SetObjOrigin(obj, obj.vertices, obj.vertices[0].Position, false);
            }
            else
            {
                Bounds b = new Bounds();
                for (int i = 0; i < 28; i++)
                    b.Encapsulate(obj.vertices[i].Position);
                var center = b.center;
                foreach (Vertex v in obj.vertices.Where(v => v.Index == 28 || (v.IsDependent && v.DependencyIndex == 28)))
                    obj.vertices[v.Index].Position = center;

                ProceduralUtils.SetObjOrigin(obj, obj.vertices, center, false);
            }
        }
        private bool IsClockwise(List<Vector3> vertices)
        {
            if (vertices == null) return false;
            if (vertices.Count <= 2) return true;
            float signedTotal = 0f;
            for (int i = 0; i < vertices.Count - 2; i ++ )
                signedTotal += VertexUtils.SignedAngle(getLocalPloppablePos(vertices[i]), getLocalPloppablePos(vertices[i + 1]), Vector3.up);
            return signedTotal < 0f;
        }
        private Vector3 getLocalPloppablePos(Vector3 pos)
        {
            return (Quaternion.Inverse(obj.m_rotation) * (pos - obj.m_position)).RevertPloppableAsphaltPosition();
        }
    }
}
 