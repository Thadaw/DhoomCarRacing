
//#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.InteropServices;


[ExecuteInEditMode]
public class CoordDebug : MonoBehaviour
{
    public string path;
    public string path2;
    public bool coords_extract;
    public bool apply;
    public Transform UnsortedParent;
    public bool PutBackUnsorted;
    public bool LoadParticles;
    public Transform ParticleParent;
    public bool RemoveDuplicates;

    public bool RenameModels;

    public bool CalculateHashes;

    // 4D410300
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BarrierSpline
    {
        public float X1;
        public float Z1;
        public float X2;
        public float Z2;
        public byte BarrierEnabled;
        public byte padding;
        public byte PlayerBarrier;
        public byte LeftHanded;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] NameHash;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SceneryInstanceStruct
    {
        public float MinX;
        public float MinZ;
        public float MinY;
        public float MaxX;
        public float MaxZ;
        public float MaxY;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Huita;
        public float PosX;
        public float PosZ;
        public float PosY;
        public short Rot00;
        public short Rot01;
        public short Rot02;
        public short Rot10;
        public short Rot11;
        public short Rot12;
        public short Rot20;
        public short Rot21;
        public short Rot22;
        public ushort SceneryInfoNumber;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SceneryInfoStruct
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
        public string Name;

        public uint NameHash0;
        public uint NameHash1;
        public uint NameHash2;
        public uint NameHash3;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] ModelPtrs;

        public float Radius;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] Unknown1;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ModelInfoStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Unknown;

        public uint Hash;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 44)]
        public byte[] Unknown2;

        public float m0_0;
        public float m0_1;
        public float m0_2;
        public float m0_3;

        public float m1_0;
        public float m1_1;
        public float m1_2;
        public float m1_3;

        public float m2_0;
        public float m2_1;
        public float m2_2;
        public float m2_3;

        public float m3_0;
        public float m3_1;
        public float m3_2;
        public float m3_3;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] Unknown3;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
        public string Name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ModelGroupStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public byte[] Unknown;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 56)]
        public string Name;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
        public string Chunk;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BloomTrigger
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Unknown;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Name;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] Unknown2;

        public float PosX;
        public float PosZ;
        public float PosY;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public float[] Params;

        public float Unknown3;
        public float Unknown4;

        public float BigNumber;

        public float Unknown5;
        public float Unknown6;
        public float Unknown7;

        public float One;

        public float Unknown8;
        public float Unknown9;
        public float Unknown10;
    }

    public class ModelSimplified
    {
        public ModelSimplified(ModelGroupStruct _group, ModelInfoStruct _info)
        {
            group = _group;
            info = _info;
        }

        public ModelGroupStruct group;
        public ModelInfoStruct info;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StreamingSection
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
        public string SectionName; // 44363600 00000000
        public short SectionNumber; // D201
        public byte WasRendered; // 00
        public byte CurrentlyVisible; // 00 
        public int Status; // 00000000 
        public int FileType; // 01000000 
        public int FileOffset; // 00886C0C 
        public int Size; // 30720000 
        public int CompressedSize; // 30720000 
        public int PermSize; // 30720000 
        public int SectionPriority; // E2280000 
        public float CentreX; // A6450245 
        public float CentreZ; // 18E7D943 
        public float Radius; // 43442743 
        public uint Checksum; // 32E140A3 
        public int LastNeededTimestamp; // 00000000 
        public uint UnactivatedFrameCount; // 00000000 
        public int LoadedTime; // 00000000 
        public int BaseLoadingPriority; // 00000000 
        public int LoadingPriority; // 00000000 
        public int pMemory; // 00000000 
        public int pDiscBundle; // 00000000 
        public int LoadedSize; // 00000000 
        public int pBoundary; // 00000000
    }

    public static object RawDeserialize(byte[] rawData, int position, Type anyType)
    {
        int rawsize = Marshal.SizeOf(anyType);
        IntPtr buffer = Marshal.AllocHGlobal(rawsize);
        Marshal.Copy(rawData, position, buffer, rawsize);
        object retobj = Marshal.PtrToStructure(buffer, anyType);
        Marshal.FreeHGlobal(buffer);
        return retobj;
    }

    public static byte[] RawSerialize(object obj)
    {
        int size = Marshal.SizeOf(obj);
        byte[] arr = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(obj, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);

        return arr;
    }
    public class StreamingSectionInfo
    {
        public List<SceneryInstanceStruct> instanceArray;
        public List<SceneryInfoStruct> infoArray;
        public List<int> actualIndex;
    }

    private List<ModelSimplified> modelArray;

    private List<StreamingSection> streamSections;
    private List<StreamingSectionInfo> streamSectionsInfo;

    public static void LoadModelData(Dictionary<uint, ModelSimplified> modelArray, BinaryReader f)
    {
        int s3 = Marshal.SizeOf(typeof(ModelInfoStruct));
        int s4 = Marshal.SizeOf(typeof(ModelGroupStruct));
        ModelGroupStruct group = new ModelGroupStruct();
        ModelInfoStruct modelInfo;
        byte[] bytes;
        while (f.BaseStream.Position < f.BaseStream.Length)
        {
            int b = f.ReadByte();
            int b2 = f.ReadByte();
            int b3 = f.ReadByte();
            int b4 = f.ReadByte();
            if (b == 0x01 && b2 == 0x40 && b3 == 0x13 && b4 == 0x80)
            {
                f.ReadInt32();
                bytes = f.ReadBytes(s4);
                group = (ModelGroupStruct)RawDeserialize(bytes, 0, typeof(ModelGroupStruct));
                while (f.BaseStream.Position % 4 != 0)
                    f.ReadByte();
            }
            else if (b == 0x11 && b2 == 0x40 && b3 == 0x13 && b4 == 0x00)
            {
                f.ReadInt32();
                do
                {
                    b = f.ReadByte();
                    b2 = f.ReadByte();
                    b3 = f.ReadByte();
                    b4 = f.ReadByte();
                } while (b == 0x11 && b2 == 0x11 && b3 == 0x11 && b4 == 0x11);
                f.BaseStream.Position -= 4;
                bytes = f.ReadBytes(s3);
                modelInfo = (ModelInfoStruct)RawDeserialize(bytes, 0, typeof(ModelInfoStruct));
                if (!modelArray.ContainsKey(modelInfo.Hash))
                    modelArray.Add(modelInfo.Hash, new ModelSimplified(group, modelInfo));
                while (f.BaseStream.Position % 4 != 0)
                    f.ReadByte();
            }
        }
    }

    public static void LoadData(List<StreamingSection> streamSections, List<StreamingSectionInfo> streamSectionsInfo, byte[] fSmol)
    {
        int sectionLength = Marshal.SizeOf(typeof(StreamingSection));
        StreamingSection curSection;
        StreamingSectionInfo curInfo;
        for (int j = 0; j < fSmol.Length; j += 4)
        {
            if (fSmol[j] == 0x10 && fSmol[j + 1] == 0x41 && fSmol[j + 2] == 0x03 && fSmol[j + 3] == 0x00)
            {
                j += 4;
                int metaEnd = j + BitConverter.ToInt32(fSmol, j);
                j += 4;
                while (j < metaEnd - 8)
                {
                    curSection = (StreamingSection)RawDeserialize(fSmol, j, typeof(StreamingSection));
                    curInfo = new StreamingSectionInfo();
                    streamSections.Add(curSection);
                    streamSectionsInfo.Add(curInfo);
                    j += sectionLength;
                }
                break;
            }
        }
    }

    public static void LoadSectionData(BinaryReader f, StreamingSection section, out List<SceneryInstanceStruct> instanceArray, out List<SceneryInfoStruct> infoArray, out List<int> actualIndex)
    {
        int end;
        instanceArray = new List<SceneryInstanceStruct>();
        infoArray = new List<SceneryInfoStruct>();
        actualIndex = new List<int>();
        int s1 = Marshal.SizeOf(typeof(SceneryInstanceStruct));
        int s2 = Marshal.SizeOf(typeof(SceneryInfoStruct));
        f.BaseStream.Seek(section.FileOffset, SeekOrigin.Begin);
        byte[] bytes;
        while (f.BaseStream.Position < section.FileOffset + section.Size)
        {
            int b = f.ReadByte();
            int b2 = f.ReadByte();
            int b3 = f.ReadByte();
            int b4 = f.ReadByte();
            if (b2 != 0x41 || b3 != 0x03 || b4 != 0x00)
            {
                continue;
            }
            switch (b)
            {
                case 0x03:
                    end = (int)f.BaseStream.Position + f.ReadInt32();
                    f.ReadInt32();
                    f.ReadInt32();
                    f.ReadInt32();
                    while (f.BaseStream.Position < end - 8)
                    {
                        bytes = f.ReadBytes(s1);
                        instanceArray.Add((SceneryInstanceStruct)RawDeserialize(bytes, 0, typeof(SceneryInstanceStruct)));
                        actualIndex.Add(infoArray.Count + instanceArray[instanceArray.Count - 1].SceneryInfoNumber);
                    }
                    while (f.BaseStream.Position % 4 != 0)
                        f.ReadByte();
                    break;
                case 0x02:
                    end = (int)f.BaseStream.Position + f.ReadInt32();
                    while (f.BaseStream.Position < end - 8)
                    {
                        bytes = f.ReadBytes(s2);
                        infoArray.Add((SceneryInfoStruct)RawDeserialize(bytes, 0, typeof(SceneryInfoStruct)));
                    }
                    while (f.BaseStream.Position % 4 != 0)
                        f.ReadByte();
                    break;
            }
        }
    }
}
//#endif