using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProceduralObjects.Classes;
using ProceduralObjects.Localization;
using UnityEngine;

namespace ProceduralObjects.SelectionMode
{
    public abstract class SelectionModeAction
    {
        public ProceduralObjectsLogic logic;
        public List<ProceduralObject> selection;
      //  public virtual List<ProceduralObject> ObjectsShown() { return new List<ProceduralObject>(); }

        public virtual void OnOpen(List<ProceduralObject> selection)
        {
            this.selection = selection;
        }
        public virtual void OnActionGUI(Vector2 uiPos)
        {
            if (GUI.Button(new Rect(uiPos, new Vector2(130, 22)), LocalizationManager.instance.current["cancel"]))
            {
                ProceduralObjectsLogic.PlaySound();
                ExitAction();
            }
        }
        public virtual void OnSingleClick(ProceduralObject obj) { }
        public virtual void OnUpdate() { }
        public virtual Rect CollisionUI(Vector2 uiPos)
        {
            return new Rect(uiPos, new Vector2(130, 22));
        }
        public virtual void ExitAction()
        {
            logic.selectionModeAction = null;
            logic.showMoreTools = false;
        }

        public static List<List<SMActionPrefab>> SelectionModeActions;
        public static List<SMActionPrefab> allActions;
        public static void InitializeSMActions()
        {
            if (SelectionModeActions != null) return;

            SelectionModeActions = new List<List<SMActionPrefab>>();
            var posRotActions = new List<SMActionPrefab>();
            posRotActions.Add(new SMActionPrefab("align_heights", typeof(AlignHeights)));
            posRotActions.Add(new SMActionPrefab("align_rotations", typeof(AlignRotations)));
            posRotActions.Add(new SMActionPrefab("equal_slope", typeof(EqualSlope)));
            posRotActions.Add(new SMActionPrefab("align_between2", typeof(AlignBetween2)));
            posRotActions.Add(new SMActionPrefab("snapToGround", typeof(SnapToGround)));
            posRotActions.Add(new SMActionPrefab("CTA_recenterObjOrigin", typeof(RecenterObjOrigin), KeyBindingsManager.instance.GetBindingFromName("recenterObjOrigin")));
            posRotActions.Add(new SMActionPrefab("randomize_rot", typeof(RandomizeRotation)));
            SelectionModeActions.Add(posRotActions);
            var transformActions = new List<SMActionPrefab>();
            transformActions.Add(new SMActionPrefab("distort", typeof(Distort)));
            transformActions.Add(new SMActionPrefab("project", typeof(Project)));
            SelectionModeActions.Add(transformActions);
            var miscActions = new List<SMActionPrefab>();
            miscActions.Add(new SMActionPrefab("replace_by_copy", typeof(ReplaceByCopy)));
            miscActions.Add(new SMActionPrefab("select_tex", typeof(SelectTexture)));
            miscActions.Add(new SMActionPrefab("set_render_dists", typeof(SetRenderDistances)));
            miscActions.Add(new SMActionPrefab("color_gradient", typeof(ColorGradient)));
            SelectionModeActions.Add(miscActions);
            allActions = new List<SMActionPrefab>();
            foreach (var list in SelectionModeActions)
            {
                list.ForEach(prefab => allActions.Add(prefab));
            }
        }
        public static void CloseAction()
        {
            if (ProceduralObjectsLogic.instance.selectionModeAction != null)
                ProceduralObjectsLogic.instance.selectionModeAction.ExitAction();
        }
        public static void DrawActionsUI(Vector2 position)
        {
            drawHeader(position, LocalizationManager.instance.current["position"] + " / " + LocalizationManager.instance.current["rotation"]);
            for (int i = 0; i < SelectionModeActions[0].Count; i++)
            {
                var smprefab = SelectionModeActions[0][i];
                drawButton(position + new Vector2(0, i * 23 + 19), SelectionModeActions[0][i]);
            }
            drawHeader(new Vector2(position.x + 181, position.y), LocalizationManager.instance.current["transform_actions"]);
            float y = 19;
            for (int i = 0; i < SelectionModeActions[1].Count; i++)
            {
                var smprefab = SelectionModeActions[1][i];
                drawButton(position + new Vector2(181, y), SelectionModeActions[1][i]);
                y += 23;
            }
            drawHeader(new Vector2(position.x + 181, position.y + y), LocalizationManager.instance.current["misc_actions"]);
            y += 19;
            for (int i = 0; i < SelectionModeActions[2].Count; i++)
            {
                var smprefab = SelectionModeActions[2][i];
                drawButton(position + new Vector2(181, y), SelectionModeActions[2][i]);
                y += 23;
            }
        }
        private static void drawHeader(Vector2 position, string text)
        {
            GUI.Box(new Rect(position.x, position.y, 180, 18), string.Empty);
            var align = GUI.skin.label.alignment;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(position, new Vector2(180, 19)), "<size=11>" + text + "</size>");
            GUI.skin.label.alignment = align;
        }
        private static void drawButton(Vector2 position, SMActionPrefab smprefab)
        {
            if (GUI.Button(new Rect(position, new Vector2(180, 22)), string.Empty))
            {
                ProceduralObjectsLogic.PlaySound();
                var action = (SelectionModeAction)Activator.CreateInstance(smprefab.type);
                action.logic = ProceduralObjectsLogic.instance;
                ProceduralObjectsLogic.instance.selectionModeAction = action;
                action.OnOpen(new List<ProceduralObject>(ProceduralObjectsLogic.instance.pObjSelection));
            }
            var align = GUI.skin.label.alignment;
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(position + new Vector2(3, 0), new Vector2(178, 22)), LocalizationManager.instance.current[smprefab.id] );
            if (smprefab.keyBinding != null)
            {
                if (!smprefab.keyBinding.IsEmpty())
                {
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    GUI.color = Color.gray;
                    GUI.Label(new Rect(position + new Vector2(1, 1), new Vector2(174, 20)), "<size=10><i>" + smprefab.keyBinding.m_fullKeys + "</i></size> ");
                    GUI.color = Color.white;
                }
            }
            GUI.skin.label.alignment = align;
        }
        public static Vector2 ActionsSize()
        {
            return new Vector2(361, 23 * SelectionModeActions[0].Count + 20);
        }
        public static void CreateAction<T>() where T : SelectionModeAction
        {
            var action = (SelectionModeAction)Activator.CreateInstance<T>();
            action.logic = ProceduralObjectsLogic.instance;
            ProceduralObjectsLogic.instance.selectionModeAction = action;
            action.OnOpen(new List<ProceduralObject>(ProceduralObjectsLogic.instance.pObjSelection));
        }
    }
    public class SMActionPrefab
    {
        public SMActionPrefab() { }
        public SMActionPrefab(string id, Type type, KeyBindingInfo keyBinding)
        {
            this.id = id;
            this.type = type;
            this.keyBinding = keyBinding;
        }
        public SMActionPrefab(string id, Type type)
        {
            this.id = id;
            this.type = type;
            this.keyBinding = KeyBindingsManager.instance.GetBindingFromName(id);
        }
        public string id;
        public Type type;
        public KeyBindingInfo keyBinding;
    }
}
