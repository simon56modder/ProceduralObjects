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

        public static Dictionary<string, Type> SelectionModeActions;
        public static void InitializeSMActions()
        {
            SelectionModeActions = new Dictionary<string, Type>();
            SelectionModeActions.Add("align_heights", typeof(AlignHeights));
            SelectionModeActions.Add("align_rotations", typeof(AlignRotations));
            SelectionModeActions.Add("randomize_rot", typeof(RandomizeRotation));
            SelectionModeActions.Add("align_between2", typeof(AlignBetween2));
            SelectionModeActions.Add("equal_slope", typeof(EqualSlope));
            SelectionModeActions.Add("snapToGround", typeof(SnapToGround));
            SelectionModeActions.Add("set_render_dists", typeof(SetRenderDistances));
            SelectionModeActions.Add("replace_by_copy", typeof(ReplaceByCopy));
            SelectionModeActions.Add("color_gradient", typeof(ColorGradient));
        }
        public static void CloseAction()
        {
            if (ProceduralObjectsLogic.instance.selectionModeAction != null)
                ProceduralObjectsLogic.instance.selectionModeAction.ExitAction();
        }
        public static void DrawActionsUI(Vector2 position)
        {
            var tools = SelectionModeActions.ToList();
            for (int i = 0; i < SelectionModeActions.Count; i++)
            {
                var kvp = tools[i];
                if (GUI.Button(new Rect(position.x, position.y + i * 23, 180, 22), LocalizationManager.instance.current[kvp.Key]))
                {
                    ProceduralObjectsLogic.PlaySound();
                    var action = (SelectionModeAction)Activator.CreateInstance(kvp.Value);
                    action.logic = ProceduralObjectsLogic.instance;
                    ProceduralObjectsLogic.instance.selectionModeAction = action;
                    action.OnOpen(new List<ProceduralObject>(ProceduralObjectsLogic.instance.pObjSelection));
                }
            }
        }
        public static void CreateAction<T>() where T : SelectionModeAction
        {
            var action = (SelectionModeAction)Activator.CreateInstance<T>();
            action.logic = ProceduralObjectsLogic.instance;
            ProceduralObjectsLogic.instance.selectionModeAction = action;
            action.OnOpen(new List<ProceduralObject>(ProceduralObjectsLogic.instance.pObjSelection));
        }
        public static Vector2 ActionsSize()
        {
            return new Vector2(180, 23 * SelectionModeActions.Count);
        }
    }
}
