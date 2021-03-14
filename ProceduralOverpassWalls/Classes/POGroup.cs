using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProceduralObjects.Classes
{
    public class POGroup
    {
        public POGroup()
        {
            objects = new List<ProceduralObject>();
        }

        public ProceduralObject root;
        public List<ProceduralObject> objects;
        public void AddToGroup(ProceduralObject po)
        {
            if (objects.Contains(po))
                return;
            objects.Add(po);
            po.group = this;
        }
        public void Remove(ProceduralObjectsLogic logic, ProceduralObject po)
        {
            if (!objects.Contains(po))
                return;

            po.group = null;
            po.isRootOfGroup = false;
            if (objects.Count == 1)
            {
                logic.groups.Remove(this);
                if (logic.selectedGroup == this)
                    logic.selectedGroup = null;
                return;
            }
            if (objects.Contains(po))
                objects.Remove(po);
            if (root == po)
            {
                ChooseFirstAsRoot();
            }
        }
        public void ChooseFirstAsRoot()
        {
            root = objects.FirstOrDefault();
            root.isRootOfGroup = true;
        }
        public void ChooseAsRoot(ProceduralObject obj)
        {
            if (!objects.Contains(obj))
                return;
            if (obj.isRootOfGroup && obj.group == this && root == obj)
                return;
            root.isRootOfGroup = false;
            root = obj;
            obj.isRootOfGroup = true;
        }

        public static List<ProceduralObject> ExplodeGroup(ProceduralObjectsLogic logic, POGroup group)
        {
            var list = new List<ProceduralObject>();
            foreach (var obj in group.objects)
            {
                list.Add(obj);
                obj.group = null;
                obj.isRootOfGroup = false;
            }
            logic.groups.Remove(group);
            return list;
        }
        public static POGroup CreateGroupWithRoot(ProceduralObject root)
        {
            var group = new POGroup();
            group.objects.Add(root);
            root.isRootOfGroup = true;
            root.group = group;
            group.root = root;
            return group;
        }
        public static POGroup MakeGroup(ProceduralObjectsLogic logic, List<ProceduralObject> objects, ProceduralObject root)
        {
            var group = (root.group == null) ? CreateGroupWithRoot(root) : root.group;
            foreach (var obj in objects)
            {
                if (obj.isRootOfGroup && obj.group != null)
                {
                    if (logic.groups.Contains(obj.group))
                        logic.groups.Remove(obj.group);
                    foreach (var pObj in obj.group.objects)
                    {
                        pObj.group = group;
                        pObj.isRootOfGroup = false;
                        group.AddToGroup(pObj);
                    }
                }
                else
                {
                    obj.group = group;
                    obj.isRootOfGroup = false;
                    group.AddToGroup(obj);
                }
            }
            group.root = root;
            group.root.isRootOfGroup = true;
            logic.groups.Add(group);
            return group;
        }
        public static void DeleteGroup(ProceduralObjectsLogic logic, POGroup group)
        {
            foreach (ProceduralObject obj in group.objects)
            {
                logic.moduleManager.DeleteAllModules(obj);
                logic.activeIds.Remove(obj.id);
                logic.proceduralObjects.Remove(obj);
            }
            logic.groups.Remove(group);
        }
        public static List<ProceduralObject> AllObjectsInSelection(List<ProceduralObject> selection, POGroup selectedGroup)
        {
            if (selectedGroup != null)
                return selection;

            var list = new List<ProceduralObject>();
            foreach (var obj in selection)
            {
                if (obj.isRootOfGroup)
                    list.AddRange(obj.group.objects);
                else
                {
                    list.Add(obj);
                }
            }
            return list;
        }
        public static int InclusiveObjectCount(List<ProceduralObject> selection)
        {
            int count = 0;
            foreach (var obj in selection)
            {
                if (obj.isRootOfGroup)
                    count += obj.group.objects.Count;
                else
                    count += 1;
            }
            return count;
        }
    }
}
