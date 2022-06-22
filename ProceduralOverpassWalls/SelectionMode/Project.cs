using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProceduralObjects.Classes;
using ProceduralObjects.Localization;
using ProceduralObjects.UI;
using UnityEngine;

namespace ProceduralObjects.SelectionMode
{
    public class Project : SelectionModeAction
    {
        Dictionary<ProceduralObject, Dictionary<Vertex, KeyValuePair<Vector3, Vector3>>> legacyAndProjectedVertices;
        float projectionForce = .95f, offset = -0.2f;
        Vector3 projectionDirection = Vector3.down;
        bool collideNetworksBuildings = true, confirmedDone = false;
        Material greenOverlay;

        public override void OnOpen(List<ProceduralObject> selection)
        {
            base.OnOpen(selection);

            objectsAsColliders = new Dictionary<ProceduralObject, GameObject>();
            greenOverlay = new Material(Shader.Find("Sprites/Default"));
            greenOverlay.color = new Color(.02f, .9f, .05f, .15f);

            InitiateProjection();
            CalculateProjection();
            ApplyProjection(projectionForce);

            Bounds b = new Bounds(selection[0].m_position, Vector3.zero);
            selection.ForEach(po => b.Encapsulate(po.m_position));
            arrowSize = 0.7f * ProceduralObjectsMod.GizmoSize.value;
            CreateArrow(b.center, projectionDirection);
        }
        public override void OnActionGUI(Vector2 uiPos)
        {
            var rect = CollisionUI(uiPos);
            if (!ProceduralObjectsMod.UseUINightMode.value)
                GUI.DrawTexture(rect, GUIUtils.bckgTex, ScaleMode.StretchToFill);
            GUI.Box(rect, string.Empty);
            // proj force
            GUI.Label(new Rect(uiPos + new Vector2(2, 2), new Vector2(260, 21)), LocalizationManager.instance.current["project_force"] + " : " + (projectionForce * 100f).ToString() + "%");
            var newProjForce = Mathf.Round(GUI.HorizontalSlider(new Rect(uiPos + new Vector2(5, 22), new Vector2(265, 8)), projectionForce * 40f, 0, 40)) / 40f;
            if (newProjForce != projectionForce)
            {
                projectionForce = newProjForce;
                CalculateProjection();
                ApplyProjection(projectionForce);
            }
            // proj offset
            GUI.Label(new Rect(uiPos + new Vector2(2, 32), new Vector2(260, 21)), LocalizationManager.instance.current["project_offset"] + " : " + (Mathf.Round(Gizmos.ConvertToDistanceUnit(offset * 100f)) / 100f).ToString() + ProceduralObjectsMod.distanceUnit);
            var newOffset = Mathf.Round(GUI.HorizontalSlider(new Rect(uiPos + new Vector2(5, 52), new Vector2(265, 8)), offset * 10f, -30f, 30f)) / 10f;
            if (newOffset != offset)
            {
                offset = newOffset;
                CalculateProjection();
                ApplyProjection(projectionForce);
            }

            // absolute directions
            DrawDirButton(uiPos + new Vector2(2, 64), "X+", Vector3.right);
            DrawDirButton(uiPos + new Vector2(44, 64), "X-", Vector3.left);
            DrawDirButton(uiPos + new Vector2(88, 64), "Y+", Vector3.up);
            DrawDirButton(uiPos + new Vector2(132, 64), "Y-", Vector3.down);
            DrawDirButton(uiPos + new Vector2(176, 64), "Z+", Vector3.forward);
            DrawDirButton(uiPos + new Vector2(220, 64), "Z-", Vector3.back);

            var newCollideNB = GUI.Toggle(new Rect(uiPos + new Vector2(2, 87), new Vector2(250, 20)), collideNetworksBuildings, LocalizationManager.instance.current["project_collideNB"]);
            if (newCollideNB != collideNetworksBuildings)
            {
                collideNetworksBuildings = newCollideNB;
                CalculateProjection();
                ApplyProjection(projectionForce);
            }

            if (GUI.Button(new Rect(uiPos.x + 2, uiPos.y + 109, 64, 22), LocalizationManager.instance.current["ok"]))
            {
                ProceduralObjectsLogic.PlaySound();
                confirmedDone = true;
                ExitAction();
                return;
            }
            if (GUI.Button(new Rect(uiPos.x + 68, uiPos.y + 109, 110, 22), LocalizationManager.instance.current["cancel"]))
            {
                ProceduralObjectsLogic.PlaySound();
                ExitAction();
                return;
            }
        }
        void DrawDirButton(Vector2 pos, string text, Vector3 dir)
        {
            if (dir == projectionDirection)
                GUI.color = Color.red;
            if (GUI.Button(new Rect(pos, new Vector2(42, 20)), text))
            {
                ProceduralObjectsLogic.PlaySound();
                projectionDirection = dir;
                UpdateArrowDirection(projectionDirection);
            }
            GUI.color = Color.white;
        }
        public override Rect CollisionUI(Vector2 uiPos)
        {
            return new Rect(uiPos, new Vector2(270, 133));
        }
        public override void OnUpdate()
        {
            UpdateArrowDirection();

            if (objectsAsColliders != null)
            {
                if (objectsAsColliders.Count == 0) return;
                foreach (var kvp in objectsAsColliders)
                {
                    var obj = kvp.Key;
                    Graphics.DrawMesh(obj.overlayRenderMesh, obj.m_position, obj.m_rotation, greenOverlay, 0, null, 0, null, true, true);
                }
            }
        }
        public override void OnSingleClick(ProceduralObject obj)
        {
            if (legacyAndProjectedVertices != null)
            {
                if (legacyAndProjectedVertices.Keys.Contains(obj))
                    return;
            }
            if (obj.isRootOfGroup && logic.selectedGroup == null)
            {
                if (objectsAsColliders.ContainsKey(obj))
                {
                    obj.group.objects.ForEach(o => { RemoveObjectAsCollider(o); });
                }
                else
                {
                    obj.group.objects.ForEach(o => { AddObjectAsCollider(o); });
                }
            }
            else
            {
                if (objectsAsColliders.ContainsKey(obj))
                    RemoveObjectAsCollider(obj);
                else
                    AddObjectAsCollider(obj);
            }
        }
        public override void ExitAction()
        {
            if (legacyAndProjectedVertices != null)
            {
                if (confirmedDone)
                    RecenterAllOrigins();
                else
                    ApplyProjection(0f);
            }
            DestroyArrow();
            DestroyAllColliders();
            base.ExitAction();
        }

        // actual mesh projection stuff
        void InitiateProjection()
        {
            legacyAndProjectedVertices = new Dictionary<ProceduralObject, Dictionary<Vertex, KeyValuePair<Vector3, Vector3>>>();
            foreach (var obj in selection)
            {
                if (obj.isRootOfGroup && logic.selectedGroup == null)
                {
                    // when root of group
                    var bounds = obj.m_mesh.bounds;
                    foreach (var po in obj.group.objects)
                    {
                        var vertexDict = new Dictionary<Vertex, KeyValuePair<Vector3, Vector3>>();
                        foreach (Vertex v in po.vertices)
                        {
                            var kvp = new KeyValuePair<Vector3, Vector3>(v.Position, Vector3.zero);
                            vertexDict.Add(v, kvp);
                        }
                        legacyAndProjectedVertices.Add(po, vertexDict);
                    }
                }
                else
                {
                    // when inside group / not root of group
                    var bounds = obj.m_mesh.bounds;
                    var vertexDict = new Dictionary<Vertex, KeyValuePair<Vector3, Vector3>>();
                    foreach (Vertex v in obj.vertices)
                    {
                        var kvp = new KeyValuePair<Vector3, Vector3>(v.Position, Vector3.zero);
                        vertexDict.Add(v, kvp);
                    }
                    legacyAndProjectedVertices.Add(obj, vertexDict);
                }
            }
        }
        void CalculateProjection()
        {
            foreach (var obj in selection)
            {
                if (obj.isRootOfGroup && logic.selectedGroup == null)
                {
                    // when root of group
                    var bounds = obj.m_mesh.bounds;
                    foreach (var po in obj.group.objects)
                    {
                        // this should eventually take into account the global bounds of the group, not only itself,
                        // so as to project groups as structured units, not each individual PO
                        var vertexDict = legacyAndProjectedVertices[po];
                        foreach (Vertex v in po.vertices)
                        {
                            var old = vertexDict[v].Key;
                            var projected = ProjectPoint(ProceduralUtils.VertexWorldPosition(old, po), projectionDirection);
                            projected = Quaternion.Inverse(po.m_rotation) * (projected - po.m_position);
                            if (offset != 0f)
                                projected -= (projected - old).normalized * offset;
                            vertexDict[v] = new KeyValuePair<Vector3,Vector3>(old, projected);
                        }
                    }
                }
                else
                {
                    // when inside group / not root of group
                    var bounds = obj.m_mesh.bounds;
                    var vertexDict = legacyAndProjectedVertices[obj];
                    foreach (Vertex v in obj.vertices)
                    {
                        var old = vertexDict[v].Key;
                        var projected = ProjectPoint(ProceduralUtils.VertexWorldPosition(old, obj), projectionDirection);
                        projected = Quaternion.Inverse(obj.m_rotation) * (projected - obj.m_position);
                        if (offset != 0f)
                            projected -= (projected - old).normalized * offset;
                        vertexDict[v] = new KeyValuePair<Vector3, Vector3>(old, projected);
                    }
                }
            }
        }
        void ApplyProjection(float force)
        {
            foreach (var PODictPair in legacyAndProjectedVertices)
            {
                foreach (var vertexPositions in PODictPair.Value)
                {
                    vertexPositions.Key.Position = Vector3.Lerp(vertexPositions.Value.Key,
                        (PODictPair.Key.isPloppableAsphalt) ? vertexPositions.Value.Value.RevertPloppableAsphaltPosition() : vertexPositions.Value.Value,
                        force);
                }
                PODictPair.Key.MakeUniqueMesh(true);
                PODictPair.Key.ApplyModelChange();
            }
        }
        Vector3 ProjectPoint(Vector3 worldPoint, Vector3 worldDirection)
        {
            bool hasTerrainHit = false;
            Vector3 terrainHit;
            worldPoint = worldPoint - (worldDirection * 3f);
            if (ProceduralUtils.TryRaycastTerrain(worldPoint, worldDirection, out terrainHit, collideNetworksBuildings))
                hasTerrainHit = true;

            RaycastHit physicsHit;
            if (Physics.Raycast(new Ray(worldPoint, worldDirection), out physicsHit, 2000))
            {
                if (physicsHit.collider.gameObject.name.StartsWith("PO_ColliderProject"))
                {
                    var point = physicsHit.point;
                    if (hasTerrainHit)
                    {
                        if ((point - worldPoint).sqrMagnitude < (terrainHit - worldPoint).sqrMagnitude)
                            return point;
                        else
                            return terrainHit;
                    }
                    else
                        return point;
                }
            }

            if (hasTerrainHit)
                return terrainHit;
            else
                return worldPoint;
        }
        void RecenterAllOrigins()
        {
            foreach (var PODictPair in legacyAndProjectedVertices)
            {
                var po = PODictPair.Key;
                ProceduralUtils.RecenterObjOrigin(po, po.vertices);
                po.ApplyModelChange();
            }
        }

        // arrow setup / direction selection for projection
        GameObject[] lineObjects;
        float arrowSize;
        private static Material arrowMat;
        void CreateArrow(Vector3 centerPoint, Vector3 direction)
        {
            if (arrowMat == null)
            {
                arrowMat = new Material(Shader.Find("GUI/Text Shader"));
                arrowMat.color = new Color(1, 1, 1, .4f);
            }
            var go1 = new GameObject("POArrow1");
            var go2 = new GameObject("POArrow2");
            var line1 = go1.AddComponent<LineRenderer>();
            var line2 = go2.AddComponent<LineRenderer>();
            line1.material = arrowMat;
            line2.material = arrowMat;
            line1.SetPositions(new Vector3[] { centerPoint, centerPoint + (direction * 8f * arrowSize) });
            line2.SetPositions(new Vector3[] { centerPoint + (direction * 8f * arrowSize), centerPoint + (direction * 11f * arrowSize) });
            line1.startWidth = 2f * arrowSize;
            line1.endWidth = 2f * arrowSize;
            line2.startWidth = 5f * arrowSize;
            line2.endWidth = .01f;
            lineObjects = new GameObject[] { go1, go2 };
        }
        void UpdateArrowDirection()
        {
            if (lineObjects == null) return;
            if (lineObjects.Length == 0) return;

            if (!Input.GetMouseButtonDown(0))
                return;
            
            var line1 = lineObjects[0].GetComponent<LineRenderer>();
            var centerPoint = line1.GetPosition(0);
            var mousePos = GUIUtils.MousePos;

            if (logic.SingleHoveredObj != null || logic.IsInWindowElement(mousePos, true))
                return;

            var line2 = lineObjects[1].GetComponent<LineRenderer>();
            Plane p = new Plane(logic.renderCamera.transform.forward, centerPoint + (logic.renderCamera.transform.forward * 20f));

            var ray = logic.renderCamera.ScreenPointToRay(Input.mousePosition);
            float enter;
            if (!p.Raycast(ray, out enter))
                return;

            var hit = ray.GetPoint(enter);
            projectionDirection = (hit - centerPoint).normalized;
            line1.SetPositions(new Vector3[] { centerPoint, centerPoint + (projectionDirection * 8f * arrowSize) });
            line2.SetPositions(new Vector3[] { centerPoint + (projectionDirection * 8f * arrowSize), centerPoint + (projectionDirection * 11f * arrowSize) });
            ProceduralObjectsLogic.PlaySound();
            CalculateProjection();
            ApplyProjection(projectionForce);
        }
        void UpdateArrowDirection(Vector3 direction)
        {
            var line1 = lineObjects[0].GetComponent<LineRenderer>();
            var centerPoint = line1.GetPosition(0);
            var line2 = lineObjects[1].GetComponent<LineRenderer>();
            line1.SetPositions(new Vector3[] { centerPoint, centerPoint + (projectionDirection * 8f * arrowSize) });
            line2.SetPositions(new Vector3[] { centerPoint + (projectionDirection * 8f * arrowSize), centerPoint + (projectionDirection * 11f * arrowSize) });
            CalculateProjection();
            ApplyProjection(projectionForce);
        }
        void DestroyArrow()
        {
            if (lineObjects == null) return;
            if (lineObjects.Length == 0) return;
            foreach (var obj in lineObjects)
            {
                if (obj == null) continue;
                UnityEngine.Object.Destroy(obj);
            }
            lineObjects = null;
        }

        // mesh colliders setup
        Dictionary<ProceduralObject, GameObject> objectsAsColliders;
        void AddObjectAsCollider(ProceduralObject obj)
        {
            if (objectsAsColliders == null)
                objectsAsColliders = new Dictionary<ProceduralObject, GameObject>();
            GameObject go = new GameObject("PO_ColliderProject_" + obj.id.ToString());
            go.transform.position = obj.m_position;
            go.transform.rotation = obj.m_rotation;
            var collider = go.AddComponent<MeshCollider>();
            collider.convex = false;
            collider.isTrigger = false;
            collider.sharedMesh = obj.overlayRenderMesh;
            objectsAsColliders.Add(obj, go);
            CalculateProjection();
            ApplyProjection(projectionForce);
        }
        void RemoveObjectAsCollider(ProceduralObject obj)
        {
            if (objectsAsColliders == null)
            {
                objectsAsColliders = new Dictionary<ProceduralObject, GameObject>();
                return;
            }
            if (!objectsAsColliders.ContainsKey(obj)) 
                return;
            UnityEngine.Object.Destroy(objectsAsColliders[obj]);
            objectsAsColliders.Remove(obj);
            CalculateProjection();
            ApplyProjection(projectionForce);
        }
        void DestroyAllColliders()
        {
            if (objectsAsColliders == null)
                return;
            foreach (var kvp in objectsAsColliders)
            {
                UnityEngine.Object.Destroy(kvp.Value);
            }
        }
    }
}
