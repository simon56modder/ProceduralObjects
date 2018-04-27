using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ProceduralObjects.Classes
{
    public class Vertex
    {
        public Vertex() { }
        public bool IsDependent, Locked;
        public int DependencyIndex, Index;
        public Vector3 Position;

        public static Vertex[] CreateVertexList(ProceduralObject source)
        {
            var list = new List<Vertex>();
            var sourceVertices = source.m_mesh.vertices;
            var dependencyData = source.m_material.name;
            bool loadDependencyData = false;
            if (dependencyData.Contains("[ProceduralObj]"))
            {
                dependencyData = dependencyData.Replace("[ProceduralObj]", "");
                loadDependencyData = true;
               // Debug.Log("data found for object " + source.basePrefabName + " : " + dependencyData);
            }
            for (int i = 0; i < sourceVertices.Count(); i++)
            {
                Vector3 _vertex = sourceVertices[i];
                Vertex v = new Vertex();
                v.Position = _vertex;
                v.Index = i;
                v.Locked = false;
                if (list.Any(vertex => vertex.Position == _vertex))
                {
                    v.IsDependent = true;
                    v.DependencyIndex = list.First(vertex => vertex.Position == _vertex).Index;
                }
                else
                    v.IsDependent = false;

                list.Add(v);
            }
            if (loadDependencyData)
                ProceduralObjectAssetUtils.LoadDependencies(list, dependencyData);
            return list.ToArray();
        }
        
        public static Vertex[] CreateVertexList(PropInfo source)
        {
            var list = new List<Vertex>();
            var sourceVertices = source.m_mesh.vertices;
            var dependencyData = source.m_material.name;
            bool loadDependencyData = false;
            if (dependencyData.Contains("[ProceduralObj]"))
            {
                dependencyData = dependencyData.Replace("[ProceduralObj]", "");
                loadDependencyData = true;
                Debug.Log("data found for object " + source.name + " : " + dependencyData);
            }
            for (int i = 0; i < sourceVertices.Count(); i++)
            {
                Vector3 _vertex = sourceVertices[i];
                Vertex v = new Vertex();
                v.Position = _vertex;
                v.Index = i;
                v.Locked = false;
                if (list.Any(vertex => vertex.Position == _vertex))
                {
                    v.IsDependent = true;
                    v.DependencyIndex = list.First(vertex => vertex.Position == _vertex).Index;
                }
                else
                    v.IsDependent = false;

                list.Add(v);
            }
            if (loadDependencyData)
                ProceduralObjectAssetUtils.LoadDependencies(list, dependencyData);
            return list.ToArray();
        }
        public static Vertex[] CreateVertexList(BuildingInfo source)
        {
            var list = new List<Vertex>();
            var sourceVertices = source.m_mesh.vertices;
            var dependencyData = source.m_material.name;
            bool loadDependencyData = false;
            if (dependencyData.Contains("[ProceduralObj]"))
            {
                dependencyData = dependencyData.Replace("[ProceduralObj]", "");
                loadDependencyData = true;
                Debug.Log("data found for object " + source.name + " : " + dependencyData);
            }
            for (int i = 0; i < sourceVertices.Count(); i++)
            {
                Vector3 _vertex = sourceVertices[i];
                Vertex v = new Vertex();
                v.Position = _vertex;
                v.Index = i;
                v.Locked = false;
                if (list.Any(vertex => vertex.Position == _vertex))
                {
                    v.IsDependent = true;
                    v.DependencyIndex = list.First(vertex => vertex.Position == _vertex).Index;
                }
                else
                    v.IsDependent = false;

                list.Add(v);
            }
            if (loadDependencyData)
                ProceduralObjectAssetUtils.LoadDependencies(list, dependencyData);
            return list.ToArray();
        }

        public static Vector2[] RecalculateUVMap(ProceduralObject po, Vertex[] vertices)
        {
            if (po.basePrefabName.Contains("NativeCube"))
            {
                Vector2[] uvmap = new Vector2[] {
                Vector2.zero,
                new Vector2(Vector3.Distance(vertices[2].Position, vertices[1].Position) / 8, Vector3.Distance(vertices[3].Position, vertices[1].Position) / 8),
                new Vector2(0, Vector3.Distance(vertices[0].Position, vertices[2].Position) / 8),
                new Vector2(Vector3.Distance(vertices[0].Position, vertices[3].Position) / 8, 0),
                new Vector2(0, Vector3.Distance(vertices[9].Position, vertices[4].Position) / 8),
                new Vector2(Vector3.Distance(vertices[4].Position, vertices[5].Position) / 8, Vector3.Distance(vertices[8].Position, vertices[5].Position) / 8),
                new Vector2(Vector3.Distance(vertices[11].Position, vertices[6].Position) / 8, Vector3.Distance(vertices[6].Position, vertices[14].Position) / 8),
                new Vector2(Vector3.Distance(vertices[17].Position, vertices[7].Position) / 8, Vector3.Distance(vertices[7].Position, vertices[15].Position) / 8),
                new Vector2(Vector3.Distance(vertices[9].Position, vertices[8].Position) / 8, 0), //8
                Vector2.zero, //9
                Vector2.zero, //10
                new Vector2(0, Vector3.Distance(vertices[10].Position, vertices[11].Position) / 8),
                new Vector2(0, Vector3.Distance(vertices[12].Position, vertices[22].Position) / 8),
                new Vector2(Vector3.Distance(vertices[12].Position, vertices[13].Position) / 8, Vector3.Distance(vertices[13].Position, vertices[23].Position) / 8),
                new Vector2(Vector3.Distance(vertices[10].Position, vertices[14].Position) / 8, 0),
                new Vector2(Vector3.Distance(vertices[16].Position, vertices[15].Position) / 8, 0),
                Vector2.zero, //16
                new Vector2(0, Vector3.Distance(vertices[16].Position, vertices[17].Position) / 8),
                new Vector2(Vector3.Distance(vertices[21].Position, vertices[18].Position) / 8, Vector3.Distance(vertices[19].Position, vertices[18].Position) / 8),
                new Vector2(Vector3.Distance(vertices[20].Position, vertices[19].Position) / 8, 0),
                Vector2.zero, //20
                new Vector2(0, Vector3.Distance(vertices[20].Position, vertices[21].Position) / 8),
                Vector2.zero, //22
                new Vector2(Vector3.Distance(vertices[22].Position, vertices[23].Position) / 8, 0)
            };
                return uvmap;
            }
            else
            {
                Vector2[] uvmap = new Vector2[] {
                    Vector2.zero,
                    new Vector2(Vector3.Distance(vertices[2].Position, vertices[1].Position) / 8, Vector3.Distance(vertices[3].Position, vertices[1].Position) / 8),
                    new Vector2(0, Vector3.Distance(vertices[0].Position, vertices[2].Position) / 8),
                    new Vector2(Vector3.Distance(vertices[0].Position, vertices[3].Position) / 8, 0),
                    Vector2.zero,
                    new Vector2(Vector3.Distance(vertices[6].Position, vertices[7].Position) / 8, Vector3.Distance(vertices[5].Position, vertices[7].Position) / 8),
                    new Vector2(0, Vector3.Distance(vertices[6].Position, vertices[4].Position) / 8),
                    new Vector2(Vector3.Distance(vertices[4].Position, vertices[7].Position) / 8, 0)
                };
                return uvmap;
            }
        }
    }

    public static class VertexUtils
    {
        public static Vector3[] GetPositionsArray(this Vertex[] vertexArray)
        {
            var list = new List<Vector3>();
            foreach (Vertex v in vertexArray)
                list.Add(v.Position);
            return list.ToArray();
        }
        public static Vector3 WorldToLocalVertexPosition(this Vector3 worldCoord, ProceduralObject obj)
        {
            return (worldCoord - obj.m_position);
            // Vector3 vertexWorldPosition = currentlyEditingObject.gameObject.transform.rotation * (Vector3.Scale(temp_storageVertex[editingVertexIndex[0]].Position, currentlyEditingObject.gameObject.transform.localScale)) + currentlyEditingObject.m_position;
        }
        public static Quaternion Rotate(this Quaternion rot, float x, float y, float z)
        {
            var gObj = new GameObject("temp_obj");
            gObj.transform.rotation = rot;
            gObj.transform.Rotate(x, y, z);
            var newRot = gObj.transform.rotation;
            UnityEngine.Object.Destroy(gObj);
            return newRot;
        }
        public static void Scale(this ProceduralObject obj, Vertex[] vertices, float scaleFactor)
        {
            foreach (Vertex v in vertices)
            {
                v.Position = new Vector3(v.Position.x * scaleFactor, v.Position.y * scaleFactor, v.Position.z * scaleFactor);
            }
        }
        public static Vector3 PloppableAsphaltPosition(this Vector3 position)
        {
            return new Vector3(position.x, position.y, (-position.z) * 2.14f);
        }
        public static bool IsMouseInside(this Rect rect)
        {
            return rect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
        }
        public static Quaternion ParseQuaternion(this string s)
        {
            string[] str = s.Replace("(", "").Replace(")", "").Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            return new Quaternion(
                float.Parse(str[0]),
                float.Parse(str[1]),
                float.Parse(str[2]),
                float.Parse(str[3]));
        }
        public static Vector3 ParseVector3(this string s)
        {
            string[] str = s.Replace("(", "").Replace(")", "").Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            return new Vector3(
                float.Parse(str[0]),
                float.Parse(str[1]),
                float.Parse(str[2]));
        }
        public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            return Quaternion.Euler(angles) * (point - pivot) + pivot;
        }
        public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
        {
            return rotation * (point - pivot) + pivot;
        }
    }
}
