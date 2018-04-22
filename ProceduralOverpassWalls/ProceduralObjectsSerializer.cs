using ICities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

using ProceduralObjects.Classes;

namespace ProceduralObjects
{
    public class ProceduralObjectsSerializer : SerializableDataExtensionBase
    {
        private readonly string dataKey = "ProceduralObjectsDataKey";

        public override void OnSaveData()
        {
            base.OnSaveData();
            Debug.Log("[ProceduralObjects] Data saving started.");
            MemoryStream proceduralObjStream = new MemoryStream();
            if (ProceduralObjectsMod.gameLogicObject == null)
                return;
            ProceduralObjectsLogic logic = ProceduralObjectsMod.gameLogicObject.GetComponent<ProceduralObjectsLogic>();
            if (logic == null)
                return;
            BinaryFormatter bFormatter = new BinaryFormatter();
            ProceduralObjectContainer[] dataContainer = logic.GetContainerList();
            try
            {
                if (dataContainer != null)
                {
                    bFormatter.Serialize(proceduralObjStream, dataContainer);
                    serializableDataManager.SaveData(dataKey, proceduralObjStream.ToArray());
                    Debug.Log("[ProceduralObjects] Data was serialized and saved. Saved " + dataContainer.Count() + " procedural objects.");
                }
                // logic.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError("[ProceduralObjects] Data wasn't saved due to " + e.GetType().ToString() + " : \"" + e.Message + "\"");
            }
            finally
            {
                proceduralObjStream.Close();
                Debug.Log("[ProceduralObjects] Data saving ended.");
            }
        }
        public override void OnLoadData()
        {
            Debug.Log("[ProceduralObjects] Data loading started.");
            byte[] byteProceduralObjectsArray = serializableDataManager.LoadData(dataKey);
            if (byteProceduralObjectsArray != null)
            {
                MemoryStream proceduralObjStream = new MemoryStream();
                proceduralObjStream.Write(byteProceduralObjectsArray, 0, byteProceduralObjectsArray.Length);
                proceduralObjStream.Position = 0;
                try
                {
                    ProceduralObjectContainer[] data = new BinaryFormatter().Deserialize(proceduralObjStream) as ProceduralObjectContainer[];
                    if (data.Count() > 0)
                    {
                        ProceduralObjectsMod.tempContainerData = data;
                        Debug.Log("[ProceduralObjects] Data Loading : transfered " + data.Count() + " ProceduralObjectContainer instances to the ProceduralObjectsLogic.");
                    }
                    else
                        Debug.LogWarning("[ProceduralObjects] No procedural object found while loading the map.");
                }
                catch (Exception e)
                {
                    Debug.LogError("[ProceduralObjects] Data wasn't loaded due to " + e.GetType().ToString() + " : \"" + e.Message + "\"");
                }
                finally
                {
                    proceduralObjStream.Close();
                    Debug.Log("[ProceduralObjects] Data loading ended.");
                }
            }
            else
            {
                Debug.Log("[ProceduralObjects] No data was found to load!");
            }
        }
    }

    [Serializable]
    public class ProceduralObjectContainer
    {
        public int id;
        public string basePrefabName, objectType, customTextureName;
        public float scale, renderDistance;
        public bool hasCustomTexture;
        public SerializableVector3 position;
        public SerializableQuaternion rotation;
        public SerializableVector3[] vertices;

        public ProceduralObjectContainer() { }
        public ProceduralObjectContainer(ProceduralObject baseObject)
        {
            id = baseObject.id;
            basePrefabName = baseObject.basePrefabName;
            objectType = baseObject.baseInfoType;
            renderDistance = baseObject.renderDistance;
            position = new SerializableVector3(baseObject.m_position);
            rotation = new SerializableQuaternion(baseObject.m_rotation);
            scale = 1f;
            vertices = SerializableVector3.ToSerializableArray(baseObject.m_mesh.vertices);
            hasCustomTexture = !(baseObject.customTexture == null);
            if (hasCustomTexture == true)
                customTextureName = baseObject.customTexture.name;
            else
                customTextureName = string.Empty;
        }
    }
    [Serializable]
    public class SerializableVector3
    {
        public float x, y, z;
        public SerializableVector3() { }
        public SerializableVector3(Vector3 source)
        {
            x = source.x;
            y = source.y;
            z = source.z;
        }
        public static SerializableVector3[] ToSerializableArray(Vector3[] source)
        {
            var list = new List<SerializableVector3>();
            for (int i = 0; i < source.Count(); i++)
                list.Add(new SerializableVector3(source[i]));
            return list.ToArray();
        }
        public static Vector3[] ToStandardVector3Array(SerializableVector3[] source)
        {
            var list = new List<Vector3>();
            for (int i = 0; i < source.Count(); i++)
                list.Add(new Vector3(source[i].x, source[i].y, source[i].z));
            return list.ToArray();
        }
    }
    [Serializable]
    public class SerializableQuaternion
    {
        public float x, y, z, w;
        public SerializableQuaternion() { }
        public SerializableQuaternion(Quaternion source)
        {
            x = source.x;
            y = source.y;
            z = source.z;
            w = source.w;
        }
    }
} 