using ICities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

using ProceduralObjects.Classes;
using ProceduralObjects.ProceduralText;

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
                    var splittedDict = SplitArray(proceduralObjStream.ToArray());
                    foreach (string key in serializableDataManager.EnumerateData())
                    {
                        if (key.Contains(dataKey) && !splittedDict.ContainsKey(key))
                        {
                            serializableDataManager.EraseData(key);
                            Debug.Log("[ProceduralObjects] Erased data array " + key + " because it wasn't used anymore");
                        }
                    }
                    Debug.Log("[ProceduralObjects] Data saving : saving " + splittedDict.Count.ToString() + " splited data array(s).");
                    foreach (KeyValuePair<string, byte[]> kvp in splittedDict)
                    {
                        serializableDataManager.SaveData(kvp.Key, kvp.Value);
                    }
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
            /*  Debug.Log("[ProceduralObjects] Data loading started.");
                var keys = serializableDataManager.EnumerateData();
                string s = " data keys :";
                foreach (string str in keys)
                    s += " " + str;
                Debug.Log(s); */
            var keys = serializableDataManager.EnumerateData().Where(key => key.Contains(dataKey)).Count();
            List<byte[]> arrays = new List<byte[]>();
            for (int i = 0; i < keys; i++)
            {
                if (i == 0)
                    arrays.Add(serializableDataManager.LoadData(dataKey));
                else
                    arrays.Add(serializableDataManager.LoadData(dataKey + i.ToString()));
            }
            Debug.Log("[ProceduralObjects] Data loading : found " + arrays.Count.ToString() + " splited data arrays.");
            long length = 0;
            for (int i = 0; i < arrays.Count; i++)
                length += arrays[i].Length;
            byte[] byteProceduralObjectsArray = new byte[length];
            long currentLength = 0;
            for (int i = 0; i < arrays.Count; i++)
            {
                Array.Copy(arrays[i], 0, byteProceduralObjectsArray, currentLength, arrays[i].Length);
                currentLength += arrays[i].Length;
            }
            if (byteProceduralObjectsArray.Length > 0)
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
                        Debug.Log("[ProceduralObjects] Data Loading : transfered " + data.Count() + " ProceduralObjectContainer instances to ProceduralObjectsLogic.");
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

        private Dictionary<string, byte[]> SplitArray(byte[] sourceArray)
        {
            var dict = new Dictionary<string, byte[]>();
            var list = new List<byte>();
            int dictNumber = 0;
            uint currentCount = 0;
            for (uint i = 0; i < sourceArray.Length; i++)
            {
                if (currentCount < 16711679)
                {
                    list.Add(sourceArray[i]);
                    currentCount += 1;
                }
                else
                {
                    string id = dataKey + ((dictNumber == 0) ? "" : dictNumber.ToString());
                    dict[id] = list.ToArray();
                    dictNumber += 1;
                    list = new List<byte>();
                    list.Add(sourceArray[i]);
                    currentCount = 1;
                }
            }
            if (list.Count > 0)
            {
                string id = dataKey + ((dictNumber == 0) ? "" : dictNumber.ToString());
                dict[id] = list.ToArray();
            }
            return dict;
        }
    }

    [Serializable]
    public class ProceduralObjectContainer
    {
        public int id;
        public string basePrefabName, objectType, customTextureName;
        public float scale, renderDistance;
        public bool hasCustomTexture, disableRecalculation;
        public SerializableVector3 position;
        public SerializableQuaternion rotation;
        public SerializableVector3[] vertices;
        public ProceduralObjectVisibility visibility;
        public TextParameters textParam;

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
            hasCustomTexture = baseObject.customTexture != null;
            visibility = baseObject.m_visibility;
            disableRecalculation = baseObject.disableRecalculation;
            if (baseObject.m_textParameters != null)
            {
                textParam = TextParameters.Clone(baseObject.m_textParameters, false);
                for (int i = 0; i < textParam.Count(); i++)
                {
                    textParam[i].serializableColor = new SerializableQuaternion(textParam[i].m_fontColor);
                }
            }
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
        public SerializableQuaternion(Color source)
        {
            x = source.r;
            y = source.g;
            z = source.b;
            w = source.a;
        }
    }
} 