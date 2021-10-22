using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProceduralObjects.Classes;
using ProceduralObjects.Localization;
using UnityEngine;

namespace ProceduralObjects.SelectionMode
{
    public class RandomizeRotation : SelectionModeAction
    {
        private Dictionary<ProceduralObject, Quaternion> oldRotations;
        private Dictionary<ProceduralObject, Vector3> randomizedRotations, oldPositions;
        private bool confirmed, RandomX, RandomY, RandomZ;

        public override void OnOpen(List<ProceduralObject> selection)
        {
            base.OnOpen(selection);
            confirmed = false;
            RandomY = true;
            if (ProceduralObjectsMod.randomizer == null)
                ProceduralObjectsMod.randomizer = new System.Random();
            oldRotations = new Dictionary<ProceduralObject, Quaternion>();
            oldPositions = new Dictionary<ProceduralObject, Vector3>();
            randomizedRotations = new Dictionary<ProceduralObject, Vector3>();
            foreach (var obj in selection)
            {
                if (obj.isRootOfGroup && logic.selectedGroup == null)
                {
                    foreach (var o in obj.group.objects)
                    {
                        oldPositions.Add(o, o.m_position);
                        oldRotations.Add(o, o.m_rotation);
                    }
                }
                else
                {
                    oldPositions.Add(obj, obj.m_position);
                    oldRotations.Add(obj, obj.m_rotation);
                }
                randomizedRotations.Add(obj, RandomEuler());
            }
            ApplyRotations();
        }
        public override void OnActionGUI(Vector2 uiPos)
        {
            if (RandomX) GUI.color = Color.red;
            if (GUI.Button(new Rect(uiPos, new Vector2(32, 22)), "X"))
            {
                ProceduralObjectsLogic.PlaySound();
                RandomX = !RandomX;
                ApplyRotations();
            }
            GUI.color = Color.white;
            if (RandomY) GUI.color = Color.red;
            if (GUI.Button(new Rect(uiPos.x + 34, uiPos.y, 32, 22), "Y"))
            {
                ProceduralObjectsLogic.PlaySound();
                RandomY = !RandomY;
                ApplyRotations();
            }
            GUI.color = Color.white;
            if (RandomZ) GUI.color = Color.red;
            if (GUI.Button(new Rect(uiPos.x + 68, uiPos.y, 32, 22), "Z"))
            {
                ProceduralObjectsLogic.PlaySound();
                RandomZ = !RandomZ;
                ApplyRotations();
            }
            GUI.color = Color.white;
            if (GUI.Button(new Rect(uiPos.x, uiPos.y + 24, 40, 22), LocalizationManager.instance.current["ok"]))
            {
                ProceduralObjectsLogic.PlaySound();
                confirmed = true;
                ExitAction();
            }
            if (GUI.Button(new Rect(uiPos.x + 42, uiPos.y + 24, 58, 22), LocalizationManager.instance.current["cancel"]))
            {
                ProceduralObjectsLogic.PlaySound();
                ExitAction();
            }
        }
        public override Rect CollisionUI(Vector2 uiPos)
        {
            return new Rect(uiPos, new Vector2(100, 48));
        }
        public override void ExitAction()
        {
            if (oldRotations != null)
            {
                if (!confirmed)
                {
                    RandomX = false; RandomY = false; RandomZ = false;
                    ApplyRotations();
                }
            }
            base.ExitAction();
        }
        private Vector3 RandomEuler()
        {
            return new Vector3(((float)ProceduralObjectsMod.randomizer.Next(0, 3600)) / 10f,
                ((float)ProceduralObjectsMod.randomizer.Next(0, 3600)) / 10f,
                ((float)ProceduralObjectsMod.randomizer.Next(0, 3600)) / 10f);
        }
        private void ApplyRotations()
        {
            foreach (var po in selection)
            {
                Quaternion appliedRot = Quaternion.Euler(RandomX ? randomizedRotations[po].x : 0f,
                    RandomY ? randomizedRotations[po].y : 0f,
                    RandomZ ? randomizedRotations[po].z : 0f);
                if (po.isRootOfGroup && logic.selectedGroup == null)
                {
                    foreach (var o in po.group.objects)
                    {
                        o.SetRotation(appliedRot * oldRotations[o]);
                        if (o != po)
                            o.SetPosition(VertexUtils.RotatePointAroundPivot(oldPositions[o], po.m_position, appliedRot));
                    }
                }
                else
                {
                    po.SetRotation(appliedRot * oldRotations[po]);
                }
            }
        }
    }
}
