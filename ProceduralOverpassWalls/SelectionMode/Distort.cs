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
    public class Distort : SelectionModeAction
    {
        Dictionary<ProceduralObject, Dictionary<Vertex, KeyValuePair<Vector3, Vector3>>> legacyVertices;

        Vector3 refCenter;
        Quaternion refRot;
        bool confirmedDone = false;

        Gizmos.GrabbablePoints grabbablePoints;

        public override void OnOpen(List<ProceduralObject> selection)
        {
            base.OnOpen(selection);
            InitiateVertices();
            ChooseReferential(selection[0]);
            CreateLine();
        }
        public override void OnUpdate()
        {
            if (grabbablePoints == null)
                return;

            Action process = () => { ProcessDistortion(); UpdateLinePositions(); };

            if (!grabbablePoints.AnySelected())
                grabbablePoints.ScaleWithPgUpPgDown(process);

            grabbablePoints.Update(refCenter, refRot, process);
        }
        public override void OnActionGUI(Vector2 uiPos)
        {
            if (grabbablePoints == null)
                return;
            grabbablePoints.OnGUI(refCenter, refRot);
            
            var rect = CollisionUI(uiPos);
            if (!ProceduralObjectsMod.UseUINightMode.value)
                GUI.DrawTexture(rect, GUIUtils.bckgTex, ScaleMode.StretchToFill);
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(uiPos + new Vector2(2, 2), new Vector2(246, 79)), grabbablePoints.kbSmooth.m_fullKeys + " : " + LocalizationManager.instance.current["hold_for_smooth"] + "\n" + grabbablePoints.kbSlow.m_fullKeys + " : " + LocalizationManager.instance.current["hold_slow_together"]
                + "\n" + KeyBindingsManager.instance.GetBindingFromName("scale_scaleUp").m_fullKeys + "/" + KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").m_fullKeys + " : " + LocalizationManager.instance.current["KB_scaleUp"] + "/" + LocalizationManager.instance.current["KB_scaleDown"]);

            if (GUI.Button(new Rect(uiPos.x + 76, uiPos.y + 82, 64, 22), LocalizationManager.instance.current["ok"]))
            {
                ProceduralObjectsLogic.PlaySound();
                confirmedDone = true;
                ExitAction();
                return;
            }
            if (GUI.Button(new Rect(uiPos.x + 143, uiPos.y + 82, 108, 22), LocalizationManager.instance.current["cancel"]))
            {
                ProceduralObjectsLogic.PlaySound();
                ExitAction();
                return;
            }
        }
        public override Rect CollisionUI(Vector2 uiPos)
        {
            return new Rect(uiPos, new Vector2(250, 105));
        }
        public override void ExitAction()
        {
            if (legacyVertices != null)
            {
                if (confirmedDone)
                    RecenterAllOrigins();
                else
                    RevertEverything();
            }
            DestroyLine();
            base.ExitAction();
        }

        // initial setup / referential setup
        void InitiateVertices()
        {
            legacyVertices = new Dictionary<ProceduralObject, Dictionary<Vertex, KeyValuePair<Vector3, Vector3>>>();
            foreach (var obj in selection)
            {
                if (obj.isRootOfGroup && logic.selectedGroup == null)
                {
                    foreach (var o in obj.group.objects)
                    {
                        var dict = new Dictionary<Vertex, KeyValuePair<Vector3, Vector3>>();
                        foreach (Vertex v in o.vertices)
                        {
                            dict.Add(v, new KeyValuePair<Vector3, Vector3>(ProceduralUtils.VertexWorldPosition(v, o), Vector3.zero));
                        }
                        legacyVertices.Add(o, dict);
                    }
                }
                else
                {
                    var dict = new Dictionary<Vertex, KeyValuePair<Vector3, Vector3>>();
                    foreach (Vertex v in obj.vertices)
                    {
                        dict.Add(v, new KeyValuePair<Vector3, Vector3>(ProceduralUtils.VertexWorldPosition(v, obj), Vector3.zero));
                    }
                    legacyVertices.Add(obj, dict);
                }
            }

        }
        void ChooseReferential(ProceduralObject po)
        {
            refCenter = po.m_position;
            refRot = po.m_rotation;
            var cloneLegacy = new Dictionary<ProceduralObject, Dictionary<Vertex, KeyValuePair<Vector3, Vector3>>>(legacyVertices);
            Vector3 initialBoundsCenter = inCurrentReferential(cloneLegacy[po].Values.ToList()[0].Key);
            Vector3 minPoint = initialBoundsCenter;
            Vector3 maxPoint = initialBoundsCenter;
            foreach (var legacyKvp in cloneLegacy)
            {
                var clone = new Dictionary<Vertex, KeyValuePair<Vector3, Vector3>>(legacyKvp.Value);
                foreach (var kvp in clone)
                {
                    var worldpos = kvp.Value.Key;
                    Vector3 posInReferential = inCurrentReferential(worldpos);
                    if (posInReferential.x < minPoint.x)
                        minPoint.x = posInReferential.x;
                    if (posInReferential.y < minPoint.y)
                        minPoint.y = posInReferential.y;
                    if (posInReferential.z < minPoint.z)
                        minPoint.z = posInReferential.z;
                    if (posInReferential.x > maxPoint.x)
                        maxPoint.x = posInReferential.x;
                    if (posInReferential.y > maxPoint.y)
                        maxPoint.y = posInReferential.y;
                    if (posInReferential.z > maxPoint.z)
                        maxPoint.z = posInReferential.z;
                    legacyVertices[legacyKvp.Key][kvp.Key] = new KeyValuePair<Vector3, Vector3>(worldpos, posInReferential);
                }
            }
            Vector3 size = maxPoint - minPoint;
            cloneLegacy = new Dictionary<ProceduralObject, Dictionary<Vertex, KeyValuePair<Vector3, Vector3>>>(legacyVertices);
            foreach (var legacyKvp in cloneLegacy)
            {
                var clone = new Dictionary<Vertex, KeyValuePair<Vector3, Vector3>>(legacyKvp.Value);
                foreach (var kvp in clone)
                {
                    var worldpos = kvp.Value.Key;
                    var localpos = kvp.Value.Value - minPoint;
                    Vector3 deltaMin = new Vector3(Mathf.Clamp(localpos.x / size.x, 0f, 1f), Mathf.Clamp(localpos.y / size.y, 0f, 1f), Mathf.Clamp(localpos.z / size.z, 0f, 1f));
                    legacyVertices[legacyKvp.Key][kvp.Key] = new KeyValuePair<Vector3, Vector3>(worldpos, deltaMin);
                }
            }
            grabbablePoints = new Gizmos.GrabbablePoints(new Vector3[] { minPoint, new Vector3(maxPoint.x, minPoint.y, minPoint.z),
                new Vector3(maxPoint.x, minPoint.y, maxPoint.z), new Vector3(minPoint.x, minPoint.y, maxPoint.z),
                new Vector3(minPoint.x, maxPoint.y, minPoint.z), new Vector3(maxPoint.x, maxPoint.y, minPoint.z),
                maxPoint, new Vector3(minPoint.x, maxPoint.y, maxPoint.z) });
        }
        Vector3 inCurrentReferential(Vector3 world) {
            return Quaternion.Inverse(refRot) * (world - refCenter);
        }
        Vector3 referentialToWorld(Vector3 inRef) {
            return refRot * inRef + refCenter;
        }
        void RecenterAllOrigins()
        {
            foreach (var PODictPair in legacyVertices)
            {
                var po = PODictPair.Key;
                ProceduralUtils.RecenterObjOrigin(po, po.vertices);
                po.ApplyModelChange();
            }
        }

        // actual movement
        void ProcessDistortion()
        {
            if (legacyVertices == null) return;
            if (grabbablePoints == null) return;
            if (grabbablePoints.points.Length == 0) return;
            foreach (var kvp in legacyVertices)
            {
                foreach (var vertexKvp in kvp.Value)
                {
                    var local = Quaternion.Inverse(kvp.Key.m_rotation) * (referentialToWorld(distortedPosition(vertexKvp.Value.Value)) - kvp.Key.m_position);
                    if (kvp.Key.isPloppableAsphalt)
                        local = local.RevertPloppableAsphaltPosition();
                    vertexKvp.Key.Position = local;
                }
                kvp.Key.MakeUniqueMesh(true);
                kvp.Key.ApplyModelChange();
            }
        }
        Vector3 distortedPosition(Vector3 deltaMin)
        {
            return new Vector3(deltaMin.y * (deltaMin.z * (deltaMin.x * grabbablePoints.points[6].x + (1f - deltaMin.x) * grabbablePoints.points[7].x)
                + (1f - deltaMin.z) * (deltaMin.x * grabbablePoints.points[5].x + (1f - deltaMin.x) * grabbablePoints.points[4].x))
                + (1f - deltaMin.y) * (deltaMin.z * (deltaMin.x * grabbablePoints.points[2].x + (1f - deltaMin.x) * grabbablePoints.points[3].x)
                + (1f - deltaMin.z) * (deltaMin.x * grabbablePoints.points[1].x + (1f - deltaMin.x) * grabbablePoints.points[0].x)),
              deltaMin.x * (deltaMin.z * (deltaMin.y * grabbablePoints.points[6].y + (1f - deltaMin.y) * grabbablePoints.points[2].y)
                + (1f - deltaMin.z) * (deltaMin.y * grabbablePoints.points[5].y + (1f - deltaMin.y) * grabbablePoints.points[1].y))
                + (1f - deltaMin.x) * (deltaMin.z * (deltaMin.y * grabbablePoints.points[7].y + (1f - deltaMin.y) * grabbablePoints.points[3].y)
                + (1f - deltaMin.z) * (deltaMin.y * grabbablePoints.points[4].y + (1f - deltaMin.y) * grabbablePoints.points[0].y)),
              deltaMin.y * (deltaMin.x * (deltaMin.z * grabbablePoints.points[6].z + (1f - deltaMin.z) * grabbablePoints.points[5].z)
                + (1f - deltaMin.x) * (deltaMin.z * grabbablePoints.points[7].z + (1f - deltaMin.z) * grabbablePoints.points[4].z))
                + (1f - deltaMin.y) * (deltaMin.x * (deltaMin.z * grabbablePoints.points[2].z + (1f - deltaMin.z) * grabbablePoints.points[1].z)
                + (1f - deltaMin.x) * (deltaMin.z * grabbablePoints.points[3].z + (1f - deltaMin.z) * grabbablePoints.points[0].z)));
        }
        void RevertEverything()
        {
            if (legacyVertices == null) return;
            foreach (var kvp in legacyVertices)
            {
                foreach (var vertexKvp in kvp.Value)
                {
                    var local = Quaternion.Inverse(kvp.Key.m_rotation) * (vertexKvp.Value.Key - kvp.Key.m_position);
                    if (kvp.Key.isPloppableAsphalt)
                        local = local.RevertPloppableAsphaltPosition();
                    vertexKvp.Key.Position = local;
                }
                kvp.Key.ApplyModelChange();
            }
        }

        // line object
        GameObject lineObject;
        LineRenderer lineRenderer;
        private static Material outlineMat;
        void CreateLine()
        {
            if (outlineMat == null)
            {
                outlineMat = new Material(Shader.Find("GUI/Text Shader"));
                outlineMat.color = new Color(1, 1, 1, .3f);
            }
            lineObject = new GameObject("POCubicOutline");
            lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.material = outlineMat;
            lineRenderer.startWidth = .4f;
            lineRenderer.endWidth = .4f;
            if (grabbablePoints != null) 
                UpdateLinePositions();
        }
        void UpdateLinePositions()
        {
            lineRenderer.positionCount = 17;
            lineRenderer.SetPositions(new Vector3[] { referentialToWorld(grabbablePoints.points[0]),
              referentialToWorld(grabbablePoints.points[1]), referentialToWorld(grabbablePoints.points[2]), referentialToWorld(grabbablePoints.points[6]), referentialToWorld(grabbablePoints.points[7]),
              referentialToWorld(grabbablePoints.points[4]), referentialToWorld(grabbablePoints.points[0]), referentialToWorld(grabbablePoints.points[3]), referentialToWorld(grabbablePoints.points[7]), referentialToWorld(grabbablePoints.points[4]),
              referentialToWorld(grabbablePoints.points[5]), referentialToWorld(grabbablePoints.points[1]), referentialToWorld(grabbablePoints.points[2]), referentialToWorld(grabbablePoints.points[3]), referentialToWorld(grabbablePoints.points[7]),
              referentialToWorld(grabbablePoints.points[6]), referentialToWorld(grabbablePoints.points[5])
             });
        }
        void DestroyLine()
        {
            UnityEngine.Object.Destroy(lineObject);
        }
    }
}
