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
        public Vertex(Vertex source)
        {
            this.Position = source.Position;
            this.Locked = source.Locked;
            this.IsDependent = source.IsDependent;
            this.Index = source.Index;
            this.DependencyIndex = source.DependencyIndex;
        }
        public bool IsDependent, Locked;
        public int DependencyIndex, Index;
        public Vector3 Position;

        public static Vertex[] CreateVertexList(ProceduralObject source)
        {
            var list = new List<Vertex>();
            var tempDictVPos = new Dictionary<Vector3, Vertex>();
            var sourceVertices = source.m_mesh.vertices;
            var dependencyData = source.m_material.name;
            bool loadDependencyData = false;
            if (dependencyData.Contains("[ProceduralObj]"))
            {
                dependencyData = dependencyData.Replace("[ProceduralObj]", "");
                loadDependencyData = true;
            }
            for (int i = 0; i < sourceVertices.Count(); i++)
            {
                Vector3 _vertex = sourceVertices[i];
                Vertex v = new Vertex();
                v.Position = _vertex;
                v.Index = i;
                v.Locked = false;
                if (tempDictVPos.ContainsKey(_vertex))
                {
                    v.IsDependent = true;
                    v.DependencyIndex = tempDictVPos[_vertex].Index;
                }
                else
                {
                    v.IsDependent = false;
                    tempDictVPos.Add(_vertex, v);
                }

                list.Add(v);
            }
            if (loadDependencyData)
                ProceduralObjectAssetUtils.LoadDependencies(list, dependencyData);
            return list.ToArray();
        }

        public static Vertex[] CreateVertexList(PropInfo source)
        {
            var list = new List<Vertex>();
            var tempDictVPos = new Dictionary<Vector3, Vertex>();
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
                if (tempDictVPos.ContainsKey(_vertex))
                {
                    v.IsDependent = true;
                    v.DependencyIndex = tempDictVPos[_vertex].Index;
                }
                else
                {
                    v.IsDependent = false;
                    tempDictVPos.Add(_vertex, v);
                }

                list.Add(v);
            }
            if (loadDependencyData)
                ProceduralObjectAssetUtils.LoadDependencies(list, dependencyData);
            return list.ToArray();
        }
        public static Vertex[] CreateVertexList(BuildingInfo source)
        {
            var list = new List<Vertex>();
            var tempDictVPos = new Dictionary<Vector3, Vertex>();
            var sourceVertices = source.m_mesh.vertices;
            var dependencyData = source.m_material.name;
            bool loadDependencyData = false;
            if (dependencyData.Contains("[ProceduralObj]"))
            {
                dependencyData = dependencyData.Replace("[ProceduralObj]", "");
                loadDependencyData = true;
            }
            for (int i = 0; i < sourceVertices.Count(); i++)
            {
                Vector3 _vertex = sourceVertices[i];
                Vertex v = new Vertex();
                v.Position = _vertex;
                v.Index = i;
                v.Locked = false;
                if (tempDictVPos.ContainsKey(_vertex))
                {
                    v.IsDependent = true;
                    v.DependencyIndex = tempDictVPos[_vertex].Index;
                }
                else
                {
                    v.IsDependent = false;
                    tempDictVPos.Add(_vertex, v);
                }
                list.Add(v);
            }
            if (loadDependencyData)
                ProceduralObjectAssetUtils.LoadDependencies(list, dependencyData);
            return list.ToArray();
        }

        public static Vector2[] RecalculateUVMap(ProceduralObject po, Vertex[] vertices)
        {
            if (po.basePrefabName.Contains("Cube"))
            {
                Vector2[] uvmap = new Vector2[] {
                    Vector2.zero,
                    new Vector2(Vector3.Distance(vertices[2].Position, vertices[1].Position) / po.tilingFactor, Vector3.Distance(vertices[3].Position, vertices[1].Position) / po.tilingFactor),
                    new Vector2(0, Vector3.Distance(vertices[0].Position, vertices[2].Position) / po.tilingFactor),
                    new Vector2(Vector3.Distance(vertices[0].Position, vertices[3].Position) / po.tilingFactor, 0),
                    new Vector2(0, Vector3.Distance(vertices[9].Position, vertices[4].Position) / po.tilingFactor),
                    new Vector2(Vector3.Distance(vertices[4].Position, vertices[5].Position) / po.tilingFactor, Vector3.Distance(vertices[8].Position, vertices[5].Position) / po.tilingFactor),
                    new Vector2(Vector3.Distance(vertices[11].Position, vertices[6].Position) / po.tilingFactor, Vector3.Distance(vertices[6].Position, vertices[14].Position) / po.tilingFactor),
                    new Vector2(Vector3.Distance(vertices[17].Position, vertices[7].Position) / po.tilingFactor, Vector3.Distance(vertices[7].Position, vertices[15].Position) / po.tilingFactor),
                    new Vector2(Vector3.Distance(vertices[9].Position, vertices[8].Position) / po.tilingFactor, 0), //8
                    Vector2.zero, //9
                    Vector2.zero, //10
                    new Vector2(0, Vector3.Distance(vertices[10].Position, vertices[11].Position) / po.tilingFactor),
                    new Vector2(0, Vector3.Distance(vertices[12].Position, vertices[22].Position) / po.tilingFactor),
                    new Vector2(Vector3.Distance(vertices[12].Position, vertices[13].Position) / po.tilingFactor, Vector3.Distance(vertices[13].Position, vertices[23].Position) / po.tilingFactor),
                    new Vector2(Vector3.Distance(vertices[10].Position, vertices[14].Position) / po.tilingFactor, 0),
                    new Vector2(Vector3.Distance(vertices[16].Position, vertices[15].Position) / po.tilingFactor, 0),
                    Vector2.zero, //16
                    new Vector2(0, Vector3.Distance(vertices[16].Position, vertices[17].Position) / po.tilingFactor),
                    new Vector2(Vector3.Distance(vertices[21].Position, vertices[18].Position) / po.tilingFactor, Vector3.Distance(vertices[19].Position, vertices[18].Position) / po.tilingFactor),
                    new Vector2(Vector3.Distance(vertices[20].Position, vertices[19].Position) / po.tilingFactor, 0),
                    Vector2.zero, //20
                    new Vector2(0, Vector3.Distance(vertices[20].Position, vertices[21].Position) / po.tilingFactor),
                    Vector2.zero, //22
                    new Vector2(Vector3.Distance(vertices[22].Position, vertices[23].Position) / po.tilingFactor, 0)
                };
                return uvmap;
            }
            else
            {
                Vector2[] uvmap = new Vector2[] {
                    Vector2.zero,
                    new Vector2(Vector3.Distance(vertices[2].Position, vertices[1].Position) / po.tilingFactor, Vector3.Distance(vertices[3].Position, vertices[1].Position) / po.tilingFactor),
                    new Vector2(0, Vector3.Distance(vertices[0].Position, vertices[2].Position) / po.tilingFactor),
                    new Vector2(Vector3.Distance(vertices[0].Position, vertices[3].Position) / po.tilingFactor, 0),
                    Vector2.zero,
                    new Vector2(Vector3.Distance(vertices[6].Position, vertices[5].Position) / po.tilingFactor, Vector3.Distance(vertices[5].Position, vertices[7].Position) / po.tilingFactor),
                    new Vector2(0, Vector3.Distance(vertices[6].Position, vertices[4].Position) / po.tilingFactor),
                    new Vector2(Vector3.Distance(vertices[4].Position, vertices[7].Position) / po.tilingFactor, 0)
                };
                return uvmap;
            }
        }
        public static Vector2[] DefaultUVMap(ProceduralObject po)
        {
            if (po.baseInfoType == "PROP")
                return Resources.FindObjectsOfTypeAll<PropInfo>().FirstOrDefault(p => p.name == po.basePrefabName).m_mesh.uv;
            else
                return Resources.FindObjectsOfTypeAll<BuildingInfo>().FirstOrDefault(b => b.name == po.basePrefabName).m_mesh.uv;
        }
    }

    public static class VertexUtils
    {
        public static readonly Texture2D[] vertexIcons = new Texture2D[] { TextureUtils.LoadTextureFromAssembly("vertexUnselected"), TextureUtils.LoadTextureFromAssembly("vertexSelected") };


        public static void FlattenSelection(ProceduralObject obj, List<int> editingVertexIndex, Vertex[] buffer)
        {
            obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, buffer);
            var bounds = new Bounds(buffer.First(v => v.Index == editingVertexIndex[0]).Position, Vector3.zero);
            var vertices = buffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex))));
            foreach (Vertex v in vertices)
                bounds.Encapsulate(v.Position);

            foreach (Vertex v in vertices)
            {
                v.Position.y = bounds.center.y;
            }
            obj.historyEditionBuffer.ConfirmNewStep(buffer);
        }
        public static void MergeVertices(ProceduralObject obj, List<int> editingVertexIndex, Vertex[] buffer)
        {
            obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, buffer);
            var bounds = new Bounds(buffer.First(v => v.Index == editingVertexIndex[0]).Position, Vector3.zero);
            var vertices = buffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex))));
            foreach (Vertex v in vertices)
                bounds.Encapsulate(v.Position);

            foreach (Vertex v in vertices)
            {
                v.Position = bounds.center;
            }
            obj.historyEditionBuffer.ConfirmNewStep(buffer);
        }
        public static void AlignVertices(ProceduralObject obj, List<int> editingVertexIndex, Vertex[] buffer)
        {
            obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, buffer);
            var vertices = buffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex))));
            var outmost = OutmostPoints(vertices.ToArray());
            Vector3 diffVector = outmost.Value - outmost.Key;
            foreach (var v in vertices)
            {
                v.Position = outmost.Key + Vector3.Project(v.Position - outmost.Key, diffVector);
            }
            obj.historyEditionBuffer.ConfirmNewStep(buffer);
        }
        public static void SnapEachToGround(ProceduralObject obj, List<int> editingVertexIndex, Vertex[] buffer)
        {
            obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, buffer);
            var vertices = buffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex))));

            foreach (Vertex v in vertices)
            {
                var worldPos = ProceduralUtils.NearestGroundPointVertical(ProceduralUtils.VertexWorldPosition(v, obj));
                v.Position = Quaternion.Inverse(obj.m_rotation) * (worldPos - obj.m_position);
            }

            obj.historyEditionBuffer.ConfirmNewStep(buffer);
        }
        public static void SnapSelectionToGround(ProceduralObject obj, List<int> editingVertexIndex, Vertex[] buffer)
        {
            obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, buffer);
            var bounds = new Bounds(ProceduralUtils.VertexWorldPosition(buffer.First(v => v.Index == editingVertexIndex[0]).Position, obj), Vector3.zero);
            var vertices = buffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex))));
            foreach (Vertex v in vertices)
                bounds.Encapsulate(ProceduralUtils.VertexWorldPosition(v.Position, obj));

            Vector3 bottomPoint = bounds.center;
            bottomPoint.y -= bounds.extents.y;

            var yWorldDiff = bottomPoint.y - ProceduralUtils.NearestGroundPointVertical(bottomPoint).y;

            foreach (Vertex v in vertices)
            {
                var worldPos = ProceduralUtils.VertexWorldPosition(v, obj);
                worldPos.y -= yWorldDiff;
                v.Position = Quaternion.Inverse(obj.m_rotation) * (worldPos - obj.m_position);
            }

            obj.historyEditionBuffer.ConfirmNewStep(buffer);
        }
        public static void ConformSelectionToTerrain(ProceduralObject obj, List<int> editingVertexIndex, Vertex[] buffer)
        {
            conformSelection(obj, editingVertexIndex, buffer, false);
        }
        public static void ConformSelectionToTerrainNetBuildings(ProceduralObject obj, List<int> editingVertexIndex, Vertex[] buffer)
        {
            conformSelection(obj, editingVertexIndex, buffer, true);
        }
        private static void conformSelection(ProceduralObject obj, List<int> editingVertexIndex, Vertex[] buffer, bool andNetBuildings)
        {
            obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, buffer);
            var bounds = new Bounds(ProceduralUtils.VertexWorldPosition(buffer.First(v => v.Index == editingVertexIndex[0]).Position, obj), Vector3.zero);
            var vertices = buffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex))));
            foreach (Vertex v in vertices)
                bounds.Encapsulate(ProceduralUtils.VertexWorldPosition(v.Position, obj));

            Vector3 bottomPoint = bounds.center;
            bottomPoint.y -= bounds.extents.y;
            var boundsOffset = new Vector3(0, bounds.size.y, 0);

            foreach (Vertex v in vertices)
            {
                var worldPos = ProceduralUtils.VertexWorldPosition(v, obj);
                var yDiff = worldPos.y - bottomPoint.y;
                worldPos = ProceduralUtils.NearestGroundPointVertical(worldPos + boundsOffset, andNetBuildings);
                worldPos.y += yDiff;
                v.Position = Quaternion.Inverse(obj.m_rotation) * (worldPos - obj.m_position);
            }
            obj.historyEditionBuffer.ConfirmNewStep(buffer);
        }

        public static void MirrorX(Vertex[] vertices, ProceduralObject obj)
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Position.x = -vertices[i].Position.x;
            obj.flipFaces = !obj.flipFaces;
            VertexUtils.flipFaces(obj);
        }
        public static void MirrorY(Vertex[] vertices, ProceduralObject obj)
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Position.y = -vertices[i].Position.y;
            obj.flipFaces = !obj.flipFaces;
            VertexUtils.flipFaces(obj);
        }
        public static void MirrorZ(Vertex[] vertices, ProceduralObject obj)
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Position.z = -vertices[i].Position.z;
            obj.flipFaces = !obj.flipFaces;
            VertexUtils.flipFaces(obj);
        }

        public static void StretchX(Vertex[] vertices, float factor)
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Position.x *= factor;
        }
        public static void StretchX(Vector3[] originalPositions, Vertex[] vertices, float factor)
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Position.x = originalPositions[i].x * factor;
        }
        public static void StretchY(Vertex[] vertices, float factor)
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Position.y *= factor;
        }
        public static void StretchY(Vector3[] originalPositions, Vertex[] vertices, float factor)
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Position.y = originalPositions[i].y * factor;
        }
        public static void StretchZ(Vertex[] vertices, float factor)
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Position.z *= factor;
        }
        public static void StretchZ(Vector3[] originalPositions, Vertex[] vertices, float factor)
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Position.z = originalPositions[i].z * factor;
        }

        public static Mesh GetCorrectedMeshPloppableAsph(Mesh originalMesh, Mesh correctedInstance, out Bounds realBounds)
        {
            if (correctedInstance == null)
                correctedInstance = originalMesh.InstantiateMesh();
            Bounds b = new Bounds();
            Vector3 noZFightingSurplus = new Vector3(0, 0.01f, 0);
            var vertices = new List<Vector3>();
            for (int i = 0; i < correctedInstance.vertices.Length; i++)
            {
                var vertex = originalMesh.vertices[i].PloppableAsphaltPosition() + noZFightingSurplus;
                vertices.Add(vertex);
                if (i == 0)
                    b = new Bounds(vertex, Vector3.zero);
                b.Encapsulate(vertex);
            }
            correctedInstance.SetVertices(vertices);
            realBounds = b;
            return correctedInstance;
        }
        public static void flipFaces(ProceduralObject obj)
        {
            for (int m = 0; m < obj.m_mesh.subMeshCount; m++)
            {
                int[] triangles = obj.m_mesh.GetTriangles(m);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int temp = triangles[i];
                    triangles[i] = triangles[i + 1];
                    triangles[i + 1] = temp;
                }
                obj.m_mesh.SetTriangles(triangles, m);
            }
            obj.RecalculateNormals();
        }
        public static Vertex[] CloneArray(this Vertex[] vertexArray)
        {
            var list = new List<Vertex>();
            for (int i = 0; i < vertexArray.Length; i++)
                list.Add(new Vertex(vertexArray[i]));
            return list.ToArray();
        }
        public static Vector3[] GetPositionsArray(this Vertex[] vertexArray)
        {
            var array = new Vector3[vertexArray.Length];
            for (int i = 0; i < vertexArray.Length; i++)
                array[i] = vertexArray[i].Position;
            return array;
        }
        public static Vector3[] GetPositionsArray(this ProceduralObject[] objArray)
        {
            var array = new Vector3[objArray.Length];
            for (int i = 0; i < objArray.Length; i++)
                array[i] = objArray[i].m_position;
            return array;
        }
        public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
        {
            float unsignedAngle = Vector3.Angle(from, to);

            float cross_x = from.y * to.z - from.z * to.y;
            float cross_y = from.z * to.x - from.x * to.z;
            float cross_z = from.x * to.y - from.y * to.x;
            float sign = Mathf.Sign(axis.x * cross_x + axis.y * cross_y + axis.z * cross_z);
            return unsignedAngle * sign;
        }
        public static Vector2 AsXZVector2(this Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }
        public static Vector3 NullY(this Vector3 v)
        {
            return new Vector3(v.x, 0, v.z);
        }
        public static List<Vector3> ResizeDecal(this Vector3[] list)
        {
            var newList = new List<Vector3>();
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].y >= 0)
                    newList.Add(new Vector3(list[i].x, 0.008f, list[i].z));
                else
                    newList.Add(list[i]);
            }
            return newList;
        }
        public static Quaternion Rotate(this Quaternion rot, float x, float y, float z)
        {
            return rot * Quaternion.Euler(x, y, z);
            /*
            var gObj = new GameObject("temp_obj");
            gObj.transform.rotation = rot;
            gObj.transform.Rotate(x, y, z);
            var newRot = gObj.transform.rotation;
            UnityEngine.Object.Destroy(gObj);
            return newRot; */
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
            return new Vector3(position.x, position.y, (-position.z) * 2.2173f);
        }
        public static Vector3 NegativeZ(this Vector3 v)
        {
            return new Vector3(v.x, v.y, -v.z);
        }
        public static string ToStringUnrounded(this Quaternion quat)
        {
            return "(" + quat.x.ToString("G") + ", " + quat.y.ToString("G") + ", " + quat.z.ToString("G") + ", " + quat.w.ToString("G") + ")";
        }
        public static string ToStringUnrounded(this Vector3 vector)
        {
            return "(" + vector.x.ToString("G") + ", " + vector.y.ToString("G") + ", " + vector.z.ToString("G") + ")";
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
        public static KeyValuePair<Vector3, Vector3> OutmostPoints(params Vector3[] points)
        {
            float longest = 0f;
            KeyValuePair<Vector3, Vector3> outmost = new KeyValuePair<Vector3, Vector3>();
            for (int i = 0; i < (points.Length - 1); i++)
            {
                for (int j = i + 1; j < points.Length; j++)
                {
                    float distance = Math.Abs((points[i] - points[j]).sqrMagnitude);

                    if (distance > longest)
                    {
                        outmost = new KeyValuePair<Vector3, Vector3>(points[i], points[j]);
                        longest = distance;
                    }
                }
            }
            return outmost;
        }
        public static KeyValuePair<ProceduralObject, ProceduralObject> OutmostPoints(params ProceduralObject[] points)
        {
            float longest = 0f;
            KeyValuePair<ProceduralObject, ProceduralObject> outmost = new KeyValuePair<ProceduralObject, ProceduralObject>();
            for (int i = 0; i < (points.Length - 1); i++)
            {
                for (int j = i + 1; j < points.Length; j++)
                {
                    float distance = Math.Abs((points[i].m_position - points[j].m_position).sqrMagnitude);

                    if (distance > longest)
                    {
                        outmost = new KeyValuePair<ProceduralObject, ProceduralObject>(points[i], points[j]);
                        longest = distance;
                    }
                }
            }
            return outmost;
        }
        public static KeyValuePair<Vector3, Vector3> OutmostPoints(params Vertex[] points)
        {
            float longest = 0f;
            KeyValuePair<Vector3, Vector3> outmost = new KeyValuePair<Vector3, Vector3>();
            for (int i = 0; i < (points.Length - 1); i++)
            {
                for (int j = i + 1; j < points.Length; j++)
                {
                    float distance = Math.Abs((points[i].Position - points[j].Position).sqrMagnitude);

                    if (distance > longest)
                    {
                        outmost = new KeyValuePair<Vector3, Vector3>(points[i].Position, points[j].Position);
                        longest = distance;
                    }
                }
            }
            return outmost;
        }
    }
}
