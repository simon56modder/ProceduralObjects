using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using ProceduralObjects.Classes;

namespace ProceduralObjects
{
    public static class ProceduralObjectAssetUtils
    {
        public static string SaveDependencyData(List<DependencyGroup> dependencyGroups, List<Vertex> lockedVertices)
        {
            string data = "[ProceduralObj]dependencyGroups ";
            foreach (DependencyGroup group in dependencyGroups)
            {
                if (group.subVertices.Count > 0)
                {
                    data += "group ";
                    data += group.mainVertex.Index.ToString() + " ";
                    foreach (Vertex v in group.subVertices)
                        data += v.Index.ToString() + " ";
                }
            }
            return data;
        }
        public static void LoadDependencies(List<Vertex> vertices, string dataString)
        {
            var groups = dataString.Replace("dependencyGroups", "").Replace("(Instance)", "").Replace("(Clone)", "").Split(new string[] { "group" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string group in groups)
            {
                var verticesString = group.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (verticesString.Count() > 0)
                {
                    int main = int.Parse(verticesString[0]);
                    for (int i = 1; i < verticesString.Count(); i++)
                    {
                        Vertex v = vertices.Single(vert => vert.Index == int.Parse(verticesString[i]));
                        foreach (Vertex vertex in vertices.Where(vert => vert.IsDependent))
                        {
                            if (vertex.DependencyIndex == int.Parse(verticesString[i]))
                            {
                                vertex.IsDependent = true;
                                vertex.DependencyIndex = main;
                            }
                        }
                        v.IsDependent = true;
                        v.DependencyIndex = main;
                    }
                }
            }
        }
    }
}
