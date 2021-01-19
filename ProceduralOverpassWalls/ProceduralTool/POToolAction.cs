using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using ProceduralObjects.Classes;
using ProceduralObjects.Localization;

namespace ProceduralObjects.Tools
{
    public class POToolAction
    {
        public POToolAction(string id, Texture2D icon, POActionType type, Action<ProceduralObject, List<int>, Vertex[]> selectionAction, Action<ProceduralObject, Vertex[]> globalAction)
        {
            if (actions == null)
                actions = new List<POToolAction>();

            this.identifier = id;
            this.ctActionType = type;
            this.icon = icon;
            if (type == POActionType.Selection)
            {
                if (selectionAction == null)
                    throw new ArgumentException("POToolAction \"" + id + "\" Selection action missing");
                this.SelectionAction = selectionAction;
            }
            else
            {
                if (globalAction == null)
                    throw new ArgumentException("POToolAction \"" + id + "\" Global action missing");
                this.GlobalAction = globalAction;
            }
            actions.Add(this);
        }

        public static List<POToolAction> actions;

        public POActionType ctActionType;
        public string identifier;
        public Texture2D icon;
        public Action<ProceduralObject, List<int>, Vertex[]> SelectionAction;
        public Action<ProceduralObject, Vertex[]> GlobalAction;

        public void ActionButton(Rect rect, ProceduralObject obj, List<int> selected, Vertex[] buffer, Action apply)
        {
            GUI.BeginGroup(rect);
            if (ctActionType == POActionType.Selection && selected.Count <= 1)
            {
                GUI.color = Color.gray;
                GUI.Box(new Rect(0, 0, rect.width, 24), string.Empty);
                if (icon != null)
                    GUI.Label(new Rect(3, 2, 20, 20), icon);
                GUI.Label(new Rect(24, 2, rect.width - 29, 22), "<i>" + LocalizationManager.instance.current["CTA_" + identifier] + "</i>");
                GUI.color = Color.white;
            }
            else
            {
                if (GUI.Button(new Rect(0, 0, rect.width, 24), string.Empty))
                {
                    ProceduralObjectsLogic.PlaySound();
                    if (ctActionType == POActionType.Selection)
                        SelectionAction.Invoke(obj, selected, buffer);
                    else if (ctActionType == POActionType.Global)
                        GlobalAction.Invoke(obj, buffer);
                    apply.Invoke();
                }
                if (icon != null)
                    GUI.Label(new Rect(4, 2, 20, 20), icon);
                GUI.Label(new Rect(23, 2, rect.width - 28, 22), LocalizationManager.instance.current["CTA_" + identifier]);
            }
            GUI.EndGroup();
        }
    }
    public enum POActionType
    {
        Selection,
        Global
    }
}
