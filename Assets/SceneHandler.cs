using CTRFramework.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using CTRFramework.Vram;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.WSA.Sharing;
using Color = UnityEngine.Color;
using Plane = UnityEngine.Plane;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace CTRFramework
{
    public class SceneHandler : MonoBehaviour
    {
        [SerializeField] private bool showCameraFrustum;
        [FormerlySerializedAs("path")] public string LevPath;
        public string VrmPath;

        public SceneHeader header;
        public MeshInfo meshinfo;

        public List<Vertex> verts = new List<Vertex>();
        public List<VertexAnim> vertanims = new List<VertexAnim>();
        public List<QuadBlock> quads = new List<QuadBlock>();
        public List<PickupHeader> pickups = new List<PickupHeader>();
        public List<VisData> visdata = new List<VisData>();
        public List<DynamicModel> dynamics = new List<DynamicModel>();
        public SkyBox skybox;
        public Nav nav;

        public List<TexMap> texmaps = new List<TexMap>();

        public UnkAdv unkadv;
        public TrialData trial;

        public List<PosAng> restartPts = new List<PosAng>();

        public Tim ctrvram;

        [SerializeField] private Camera camera;

        private GameObject levelQuads;
        private GameObject levelVisData;
        public List<Vector3s> posu2 = new List<Vector3s>();
        public List<PosAng> posu1 = new List<PosAng>();
        public List<VisiQuad> visiQuadCompId = new List<VisiQuad>();
        [FormerlySerializedAs("textures")] public Dictionary<string, Texture2D> midTextures;
        public Dictionary<string, Texture2D> lowTextures;

        [SerializeField] private GameObject fruitCrate;
        [SerializeField] private GameObject itemCrate;
        [SerializeField] private GameObject itemFruit;
        public void Start()
        {
            byte[] data = File.ReadAllBytes(LevPath);

            using (MemoryStream ms = new MemoryStream(data, 4, data.Length - 4))
            {
                using (BinaryReaderEx br = new BinaryReaderEx(ms))
                {
                    Read(br);

                    bool vram_path_is = File.Exists(VrmPath);
                    if (vram_path_is)
                    {
                        Debug.Log("VRAM found!");
                        ctrvram = CtrVrm.FromFile(VrmPath);
                        //tex = CtrVrm.buffer.ConvertedTexture;
                        Debug.Log(CtrVrm.buffer.bpp);
                        Debug.Log(CtrVrm.buffer.clutsize);
                        Debug.Log(CtrVrm.buffer.datasize);
                        Dictionary<string, TextureLayout> dictionary = GetTexturesList(Detail.Med);
                        midTextures = new Dictionary<string, Texture2D>();
                        foreach(KeyValuePair<string, TextureLayout> entry in dictionary)
                        {
                            Tim texture = CtrVrm.buffer.GetTimTexture(entry.Value);
                            Texture2D tex2D = new Texture2D(texture.region.Width*4, texture.region.Height, TextureFormat.ARGB32, false);
                            for (int y = 0; y < texture.region.Height; y++)
                            {
                                for (int x = 0; x < texture.region.Width; x++)
                                {
                                    var clutValue = texture.data[x + texture.region.Width * y];
                                    for (int clut = 0; clut < 4*4; clut+=4)
                                    {
                                        var pixelValue = texture.clutdata[(clutValue>>clut)&15];
                                
                                        float pixelRed = pixelValue & 31;
                                        pixelValue = (ushort) (pixelValue >> 5);
                                        float pixelGreen = pixelValue & 31;
                                        pixelValue = (ushort) (pixelValue >> 5);
                                        float pixelBlue = pixelValue & 31;
                                        pixelValue = (ushort) (pixelValue >> 5);
                                        int pixelAlpha = pixelValue & 1;
                                        tex2D.SetPixel(x*4 + clut/4,y,new Color(pixelRed/31f,pixelGreen/31f,pixelBlue/31f, pixelAlpha == 1 ? 0f : 1.0f));
                                    }
                                }
                            }
                            tex2D.Apply();
                            tex2D.filterMode = FilterMode.Point;
                            midTextures.Add( entry.Key, tex2D);
                        }
                        dictionary = GetTexturesList(Detail.Low);
                        lowTextures = new Dictionary<string, Texture2D>();
                        foreach(KeyValuePair<string, TextureLayout> entry in dictionary)
                        {
                            Tim texture = CtrVrm.buffer.GetTimTexture(entry.Value);
                            Texture2D tex2D = new Texture2D(texture.region.Width*4, texture.region.Height, TextureFormat.ARGB32, false);
                            for (int y = 0; y < texture.region.Height; y++)
                            {
                                for (int x = 0; x < texture.region.Width; x++)
                                {
                                    var clutValue = texture.data[x + texture.region.Width * y];
                                    for (int clut = 0; clut < 4*4; clut+=4)
                                    {
                                        var pixelValue = texture.clutdata[(clutValue>>clut)&15];
                                
                                        float pixelRed = pixelValue & 31;
                                        pixelValue = (ushort) (pixelValue >> 5);
                                        float pixelGreen = pixelValue & 31;
                                        pixelValue = (ushort) (pixelValue >> 5);
                                        float pixelBlue = pixelValue & 31;
                                        pixelValue = (ushort) (pixelValue >> 5);
                                        int pixelAlpha = pixelValue & 1;
                                        tex2D.SetPixel(x*4 + clut/4,y,new Color(pixelRed/31f,pixelGreen/31f,pixelBlue/31f, pixelAlpha == 1 ? 0f : 1.0f));
                                    }
                                }
                            }
                            tex2D.Apply();
                            tex2D.filterMode = FilterMode.Point;
                            lowTextures.Add( entry.Key, tex2D);
                        }

                        
                        /*
                        if (ctrvram != null)
                            foreach (var m in texmaps)
                            {
                                ctrvram.GetTexture(m.tl, ".\\textures\\", m.name);
                                Debug.Log(m.name);
                                Console.ReadKey();
                            }
                        */
                    }

                    GenerateVis();
                    Render();
                    ShowPickups();
                }
            }
        }


        public void Read(BinaryReaderEx br)
        {
            //data that seems to be present in every level
            header = Instance<SceneHeader>.FromReader(br, 0);

            meshinfo = Instance<MeshInfo>.FromReader(br, header.ptrMeshInfo);
            verts = InstanceList<Vertex>.FromReader(br, meshinfo.ptrVertexArray, meshinfo.cntVertex);
            restartPts = InstanceList<PosAng>.FromReader(br, header.ptrRestartPts, header.cntRestartPts);
            visdata = InstanceList<VisData>.FromReader(br, meshinfo.ptrVisDataArray, meshinfo.cntColData);
            quads = InstanceList<QuadBlock>.FromReader(br, meshinfo.ptrQuadBlockArray, meshinfo.cntQuadBlock);

            //optional stuff, can be missing
            if (header.ptrSkybox != 0) skybox = Instance<SkyBox>.FromReader(br, header.ptrSkybox);
            if (header.ptrVcolAnim != 0)
                vertanims = InstanceList<VertexAnim>.FromReader(br, header.ptrVcolAnim, header.cntVcolAnim);
            if (header.ptrAiNav != 0) nav = Instance<Nav>.FromReader(br, header.ptrAiNav);
            if (header.ptrTrialData != 0) trial = Instance<TrialData>.FromReader(br, header.ptrTrialData);

            if (header.cntSpawnPts != 0)
            {
                br.Jump(header.ptrSpawnPts);
                unkadv = new UnkAdv(br, (int) header.cntSpawnPts);
            }


            if (header.cntTrialData != 0)
            {
                br.Jump(header.ptrTrialData);

                int cnt = br.ReadInt32();
                int ptr = br.ReadInt32();

                br.Jump(ptr);

                for (int i = 0; i < cnt; i++)
                    posu1.Add(new PosAng(br));
            }


            if (header.cntu2 != 0)
            {
                br.Jump(header.ptru2);

                int cnt = br.ReadInt32();
                int ptr = br.ReadInt32();


                br.Jump(ptr);

                for (int i = 0; i < cnt; i++)
                    posu2.Add(new Vector3s(br));
            }


            /*
            //texture defs
            br.Jump(header.ptrTexArray);
            br.Skip(8);
            int ptrTexList = br.ReadInt32();
            br.Jump(ptrTexList);

            Debug.Log(ptrTexList.ToString("X8"));
            Console.ReadKey();

            texmaps = new List<TexMap>();

            TexMap mp;

            do
            {
                mp = new TexMap(br, "none");

                Debug.Log(mp.name);
                Console.ReadKey();

                if (mp.name != "")
                {
                    texmaps.Add(mp);
                }

            }
            while (mp.name != "");
            */


            /*
            for (int i = 0; i < visdata.Count; i++)
            {
                if (File.Exists($"visdata_{i}.obj"))
                    File.Delete($"visdata_{i}.obj");

                File.AppendAllText($"visdata_{i}.obj", visdata[i].ToObj());
            }
            */

            /*
             //water texture
            br.BaseStream.Position = header.ptrWater;

            List<uint> vptr = new List<uint>();
            List<uint> wptr = new List<uint>();

            for (int i = 0; i < header.cntWater; i++)
            {
                vptr.Add(br.ReadUInt32());
                wptr.Add(br.ReadUInt32());
            }

            wptr.Sort();

            foreach(uint u in wptr)
            {
                Debug.Log(u.ToString("X8"));
            }

            Console.ReadKey();
            */

            //read pickups
            for (int i = 0; i < header.numInstances; i++)
            {
                br.Jump(header.ptrPickupHeadersPtrArray + 4 * i);
                br.Jump(br.ReadUInt32());

                pickups.Add(new PickupHeader(br));
            }

            //read pickup models
            //starts out right, but anims ruin it

            br.Jump(header.ptrModelsPtr);
            int x = (int) br.BaseStream.Position;

            for (int i = 0; i < header.numModels; i++)
            {
                br.BaseStream.Position = x + 4 * i;
                br.BaseStream.Position = br.ReadUInt32();

                dynamics.Add(new DynamicModel(br));
            }

            /*
            //check why itdoesn't work in export function
            foreach (var d in dynamics)
            {
                d.Export(".\\models\\");
            }
            */

            /*
            foreach (QuadBlock qb in quads)
            {
                for (int i = 0; i < 4; i++)
                {
                    List<CTRFramework.Vertex> list = qb.GetVertexListq(verts, i);

                    foreach (CTRFramework.Vertex v in list)
                        Debug.Log(v.ToString());

                    Debug.Log(qb.unk3[i].ToString());

                    /*
                    //requires 4.6
                    System.Numerics.Vector3 a = new Vector3(list[3].coord.X - list[0].coord.X, list[3].coord.Y - list[0].coord.Y, list[3].coord.Z - list[0].coord.Z);
                    System.Numerics.Vector3 b = new Vector3(list[2].coord.X - list[0].coord.X, list[2].coord.Y - list[0].coord.Y, list[2].coord.Z - list[0].coord.Z);

                    Vector3 cross = Vector3.Cross(a, b);

                    Debug.Log(cross.Length()); 
                    */

            /*
                }

                Debug.Log();
                Console.ReadKey();
            }
            */

            StringBuilder sb = new StringBuilder();

            int max = 0;
            int min = 99999999;

            foreach (QuadBlock qb in quads)
            {
                foreach (Vector2s v in qb.unk3)
                {
                    if (v.X > max) max = v.X;
                    if (v.Y > max) max = v.Y;
                    if (v.X < min && v.X != 0) min = v.X;
                    if (v.Y < min && v.Y != 0) min = v.Y;
                }

                /*
                sb.AppendLine($"quadblock id = {qb.id}");

                foreach (int i in qb.ind)
                {
                    sb.AppendLine(verts[i].coord.ToString(VecFormat.CommaSeparated));
                }

                foreach (Vector2s v in qb.unk3)
                {
                    sb.AppendLine(v.ToString(VecFormat.CommaSeparated));
                }

                sb.AppendLine();
                */
            }

            Debug.Log($"{min}, {max}");
            // Console.ReadKey();

            //Helpers.WriteToFile(".\\normals_test.txt", sb.ToString());
        }

        private Vector3 InterpolatedPosition(float t)
        {
            if (Application.isPlaying && nav.paths != null && nav.paths.Count > 0)
            {
                int pathLen = nav.paths[0].frames.Count;
                var frames = nav.paths[0].frames.ToArray();
                Vector3s s0 = frames[(int) t % pathLen].position;
                Vector3 a0 = s0.GetVector3();
                a0.y += 2f;
                Vector3s s1 = frames[(int) (t + 1) % pathLen].position;
                Vector3 a1 = s1.GetVector3();
                a1.y += 2f;
                return Vector3.Lerp(a0, a1, Time.time % 1.0f);
            }

            return camera.transform.position;
        }

        private void ShowPickups()
        {
            foreach (var pickup in pickups)
            {
                Vector3 position = pickup.Position.GetVector3();
                Vector3 angle = pickup.Angle.GetVector3();
                Vector3 scale = pickup.Scale.GetVector3();
                scale.Scale(new Vector3(1.0f/32.0f,1.0f/32.0f,1.0f/32.0f));
                angle.Scale(new Vector3(180f,180f,180f));
                position.y += scale.y/2f;
                GameObject item;
                switch (pickup.ModelName)
                {
                    case "crate_fruit":
                        item = Instantiate(fruitCrate, position, Quaternion.Euler(angle.x,angle.y,angle.z));
                        item.transform.localScale = scale;
                        break;
                    case "crate_question":
                        item = Instantiate(itemCrate, position, Quaternion.Euler(angle.x,angle.y,angle.z));
                        item.transform.localScale = scale;
                        break;
                    case "itemFruit":
                        item = Instantiate(itemFruit, position, Quaternion.Euler(angle.x,angle.y,angle.z));
                        item.transform.localScale = scale;
                        break;
                }
            }
        }
        private List<VisData> FindCameraNodes(VisData node, List<VisData> camList, int maxDepth)
        {
            List<VisData> visArr;
            if (maxDepth > 0)
            {
                maxDepth--;
                /*
                Gizmos.color = new Color(0.0f, 1.0f, 0.0f, 0.2f);
                Vector3 bmax = new Vector3(node.bbox.Max.X / 255.0f, node.bbox.Max.Y / 255.0f,
                    -node.bbox.Max.Z / 255.0f);
                Vector3 bmin = new Vector3(node.bbox.Min.X / 255.0f, node.bbox.Min.Y / 255.0f,
                    -node.bbox.Min.Z / 255.0f);
                Vector3 bsize = new Vector3(Mathf.Abs(bmax.x - bmin.x), Mathf.Abs(bmax.y - bmin.y),
                    Mathf.Abs(bmax.z - bmin.z));
                Vector3 bpos = new Vector3((bmax.x + bmin.x) / 2.0f, (bmax.y + bmin.y) / 2.0f,
                    (bmax.z + bmin.z) / 2.0f);
                Gizmos.DrawWireCube(transform.position + bpos, bsize);*/
                VisData leftNode = visdata[node.leftChild & 0x3fff];
                VisData rightNode = visdata[node.rightChild & 0x3fff];
                if (leftNode != null && !camList.Contains(leftNode) &&
                    leftNode.bbox.IsInside(camera.transform.position))
                {
                    visArr = FindCameraNodes(leftNode, camList, maxDepth);
                    visArr.Add(leftNode);
                    return visArr;
                }

                if (rightNode != null && !camList.Contains(rightNode) &&
                    rightNode.bbox.IsInside(camera.transform.position))
                {
                    visArr = FindCameraNodes(rightNode, camList, maxDepth);
                    visArr.Add(rightNode);
                    return visArr;
                }
            }

            visArr = new List<VisData>();
            visArr.Add(node);
            return visArr;
        }

        private Plane[] planes;
        private void DrawQuads(VisData node, int depth, List<VisData> camList)
        {
            if (node.IsLeaf)
            {
                uint ptrQuadBlock = (uint) (uint) (((node.ptrQuadBlock) / LevelShift.Div)  + LevelShift.Offset);
                uint numQuadBlock = node.numQuadBlock;
                for (int i = 0; i < numQuadBlock; i++)
                {
                    long index = ptrQuadBlock + i;
                    Gizmos.color = new Color(1, 0, 1, 0.333f);
                    if (index < 0 && index > (visiQuadCompId.Count - 1)) {break;}
                    VisiQuad quad = visiQuadCompId[(int)Mathf.Min( Mathf.Max(index,0),visiQuadCompId.Count - 1)];
                    if (Vector3.Distance(quad.Position, camera.transform.position) < 32.0f)
                    {
                        Transform child = quad.transform.GetChild(1);
                        child.gameObject.SetActive(false);
                        child = quad.transform.GetChild(0);
                        child.gameObject.SetActive(true);
                    } else if (Vector3.Distance(quad.Position, camera.transform.position) < 128.0f)
                    {
                        Transform child = quad.transform.GetChild(1);
                        child.gameObject.SetActive(true);
                        child = quad.transform.GetChild(0);
                        child.gameObject.SetActive(false);
                    } else 
                    {
                        Transform child = quad.transform.GetChild(1);
                        child.gameObject.SetActive(false);
                        child = quad.transform.GetChild(0);
                        child.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                VisData leftNode = visdata[node.leftChild & 0x3fff];
                VisData rightNode = visdata[node.rightChild & 0x3fff];
                Bounds bounds = node.bbox.GetBounds();
                bool canRender = false;
                if (GeometryUtility.TestPlanesAABB(planes, bounds))
                {
                    canRender = true;
                } else if (depth > 0 && camList.Contains(node))
                {
                    canRender = true;
                    depth--;
                }
                if( canRender)
                {
                    DrawQuads(leftNode,depth - 1, camList);
                    DrawQuads(rightNode,depth - 1, camList);
                }
            }
        }
        private void QuadGO(QuadBlock qb, GameObject gameObject, bool highDetail)
        {
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> vertices = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<Vertex> vertList = qb.GetVertexList(this, highDetail);
            List<int> indices = new List<int>();
            for (int i = 0; i < vertList.Count; i++)
            {
                Vertex vertA = vertList[i];
                indices.Add(i);
                vertices.Add(new Vector3(vertA.coord.X / 255.0f, vertA.coord.Y / 255.0f, -vertA.coord.Z / 255.0f));
                uvs.Add(vertA.uv);
                colors.Add(new Color(vertA.color.X / 255.0f, vertA.color.Y / 255.0f, vertA.color.Z / 255.0f));
            }

            //Add Components
            var filter = gameObject.AddComponent<MeshFilter>();
            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            //meshRenderer.material = new Material(Shader.Find("Custom/Double-Sided"));
            meshRenderer.material = new Material(highDetail ? Shader.Find("PS1") : Shader.Find("PS1Low"));
            if (highDetail)
            {
                meshRenderer.material.SetTexture("_Tex0", midTextures[qb.tex[0].midlods[2].Tag()]);
                meshRenderer.material.SetTexture("_Tex1", midTextures[qb.tex[1].midlods[2].Tag()]);
                meshRenderer.material.SetTexture("_Tex2", midTextures[qb.tex[2].midlods[2].Tag()]);
                meshRenderer.material.SetTexture("_Tex3", midTextures[qb.tex[3].midlods[2].Tag()]);
                meshRenderer.material.SetInt("_flipRotate0", (int)qb.faceFlags[0].rotateFlipType);
                meshRenderer.material.SetInt("_flipRotate1", (int)qb.faceFlags[1].rotateFlipType);
                meshRenderer.material.SetInt("_flipRotate2", (int)qb.faceFlags[2].rotateFlipType);
                meshRenderer.material.SetInt("_flipRotate3", (int)qb.faceFlags[3].rotateFlipType);
            }
            else
            {
                var tag = qb.texlow.Tag();
                if (lowTextures.ContainsKey(tag))
                {
                    meshRenderer.material.SetTexture("_Tex", lowTextures[tag]);
                    meshRenderer.material.SetInt("_flipRotate", (int)qb.faceFlags[0].rotateFlipType);
                }
            }
            meshRenderer.material.SetInt("_invisibleTriggers", qb.quadFlags.HasFlag(QuadFlags.InvisibleTriggers)? 1 : 0);
            filter.mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                triangles = indices.ToArray(),
                colors = colors.ToArray(),
                uv = uvs.ToArray()
            };
            filter.mesh.RecalculateNormals();
            filter.mesh.RecalculateBounds();
        }

        private void Render()
        {
            levelQuads = new GameObject("levelQuads");
            levelQuads.transform.parent = transform;
            levelQuads.transform.localPosition = Vector3.zero;
            foreach (QuadBlock qb in quads)
            {
                GameObject objToSpawn = new GameObject("levObj" + qb.id);
                var visiQuadComp = objToSpawn.AddComponent<VisiQuad>();
                visiQuadCompId.Add(visiQuadComp);
                objToSpawn.transform.parent = levelQuads.transform;
                objToSpawn.transform.localPosition = Vector3.zero;
                GameObject objToSpawnHigh = new GameObject("levObjHigh" + qb.id);
                QuadGO(qb, objToSpawnHigh, true);
                objToSpawnHigh.transform.parent = objToSpawn.transform;
                objToSpawnHigh.transform.localPosition = Vector3.zero;
                GameObject objToSpawnLow = new GameObject("levObjLow" + qb.id);
                QuadGO(qb, objToSpawnLow, false);
                objToSpawnLow.transform.parent = objToSpawn.transform;
                objToSpawnLow.transform.localPosition = Vector3.zero;
                objToSpawnHigh.SetActive(false);
                objToSpawnLow.SetActive(true);

                visiQuadComp.Position = new Vector3(
                    (qb.bb.Max.X + qb.bb.Min.X) / (2.0f * 255.0f),
                    (qb.bb.Max.Y + qb.bb.Min.Y) / (2.0f * 255.0f),
                    -(qb.bb.Max.Z + qb.bb.Min.Z) / (2.0f * 255.0f));
                visiQuadComp.Qb = qb;
                //meshRenderer.material.mainTexture = ctrvram.GetTexture(qb.texlow);
            }
        }

        void VisStepBranchInto(VisData visData, Transform parent)
        {
            if (visData == null) return;
            GameObject rootObjToSpawn = new GameObject("vis" + visData.id);
            VisInstance iRootVis = rootObjToSpawn.AddComponent<VisInstance>();
            iRootVis.Visi = visData;
            iRootVis.SetSceneHandler(this);
            rootObjToSpawn.transform.parent = parent;

            if (visData.leftChild != 0)
            {
                ushort uLeftChild = (ushort) (visData.leftChild & 0x3fff);
                VisData leftChild = visdata.Find(cc => cc.id == uLeftChild);
                VisStepBranchInto(leftChild, rootObjToSpawn.transform);
            }

            if (visData.rightChild != 0)
            {
                ushort uRightChild = (ushort) (visData.rightChild & 0x3fff);
                VisData rightChild = visdata.Find(cc => cc.id == uRightChild);
                VisStepBranchInto(rightChild, rootObjToSpawn.transform);
            }
        }

        void GenerateVis()
        {
            levelVisData = new GameObject("levelVisData");
            levelVisData.transform.parent = transform;
            levelVisData.transform.localPosition = Vector3.zero;
            // Draw a semitransparent blue cube at the transforms position
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            int i = 0;
            VisData rootVis = visdata[0];
            VisStepBranchInto(rootVis, levelVisData.transform);
        }

        public Dictionary<string, TextureLayout> GetTexturesList(Detail lod)
        {
            Dictionary<string, TextureLayout> tex = new Dictionary<string, TextureLayout>();

            foreach (QuadBlock qb in quads)
            {
                switch (lod)
                {
                    case Detail.Low:
                        if (qb.ptrTexLow != 0)
                            if (!tex.ContainsKey(qb.texlow.Tag()))
                                tex.Add(qb.texlow.Tag(), qb.texlow);
                        break;
                    case Detail.Med:
                        foreach (CtrTex t in qb.tex)
                            if (t.midlods[2].Position != 0)
                                if (!tex.ContainsKey(t.midlods[2].Tag()))
                                    tex.Add(t.midlods[2].Tag(), t.midlods[2]);
                        break;
                    case Detail.High:
                        foreach (CtrTex t in qb.tex)
                        {
                            foreach (var x in t.hi)
                            {
                                if (x != null)
                                    if (!tex.ContainsKey(x.Tag()))
                                        tex.Add(x.Tag(), x);
                            }
                        }

                        break;
                }
            }

            return tex;
        }


        public Dictionary<string, TextureLayout> GetTexturesList()
        {
            Dictionary<string, TextureLayout> tex = new Dictionary<string, TextureLayout>();

            foreach (QuadBlock qb in quads)
            {
                if (qb.ptrTexLow != 0)
                {
                    if (!tex.ContainsKey(qb.texlow.Tag()))
                    {
                        tex.Add(qb.texlow.Tag(), qb.texlow);
                    }
                }

                foreach (CtrTex t in qb.tex)
                foreach (TextureLayout tl in t.midlods)
                {
                    if (!tex.ContainsKey(tl.Tag()))
                    {
                        tex.Add(tl.Tag(), tl);
                    }
                }

                foreach (CtrTex t in qb.tex)
                {
                    foreach (TextureLayout tl in t.hi)
                    {
                        if (tl != null)
                            if (!tex.ContainsKey(tl.Tag()))
                            {
                                tex.Add(tl.Tag(), tl);
                            }
                    }
                }
            }

            return tex;
        }

        public void Dispose()
        {
            header = null;
            meshinfo = null;
            verts.Clear();
            vertanims.Clear();
            quads.Clear();
            pickups.Clear();
            visdata.Clear();
            dynamics.Clear();
            skybox = null;
            nav = null;
            unkadv = null;
            restartPts.Clear();
            ctrvram = null;
        }
    }
}