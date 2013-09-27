﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace WowMoPObjMgrTest
{
    [StructLayout(LayoutKind.Sequential)]
    struct Descriptor
    {
        public IntPtr m_name; // ptr
        public uint m_size;
        public MIRROR_FLAGS m_flags;
    }

    //[StructLayout(LayoutKind.Sequential)]
    //struct DynamicDescriptor
    //{
    //    public IntPtr m_name; // ptr
    //    public MIRROR_FLAGS m_flags;
    //}

    class DescriptorsDumper
    {
        // offsets for 5.0.4.16016
        //const int g_baseObjDescriptors = 0x01029260;
        //const int g_baseItemDescriptors = 0x01028F28;
        //const int g_baseContainerDescriptors = 0x01028868;
        //const int g_baseUnitDescriptors = 0x01027AF0;
        //const int g_basePlayerDescriptors = 0x01021E78;
        //const int g_baseGameObjectDescriptors = 0x0101C158;
        //const int g_baseDynamicObjectDescriptors = 0x0101C018;
        //const int g_baseCorpseDescriptors = 0x0101BE28;
        //const int g_baseAreaTriggerDescriptors = 0x0101BBB4;
        //const int g_baseSceneObjectDescriptors = 0x0101BB84;

        // offsets for 5.2.0.16826
        //const int g_baseObjDescriptors = 0x010F0E48 - 0x400000;
        //const int g_baseItemDescriptors = 0x010F0B10 - 0x400000;
        //const int g_baseContainerDescriptors = 0x010F0448 - 0x400000;
        //const int g_baseUnitDescriptors = 0x010EF6B8 - 0x400000;
        //const int g_basePlayerDescriptors = 0x010E99F0 - 0x400000;
        //const int g_baseGameObjectDescriptors = 0x010E3C90 - 0x400000;
        //const int g_baseDynamicObjectDescriptors = 0x010E3B50 - 0x400000;
        //const int g_baseCorpseDescriptors = 0x010E3960 - 0x400000;
        //const int g_baseAreaTriggerDescriptors = 0x010E36EC - 0x400000;
        //const int g_baseSceneObjectDescriptors = 0x010E36BC - 0x400000;

        // offsets for 5.4.0.17345
        const int g_baseObjDescriptors = 0x010E73E8 - 0x400000;
        const int g_baseItemDescriptors = 0x010E70A8 - 0x400000;
        const int g_baseContainerDescriptors = 0x010E69C8 - 0x400000;
        const int g_baseUnitDescriptors = 0x010E5C00 - 0x400000;
        const int g_basePlayerDescriptors = 0x010DFEB8 - 0x400000;
        const int g_baseGameObjectDescriptors = 0x010DA0C0 - 0x400000;
        const int g_baseDynamicObjectDescriptors = 0x010D9F78 - 0x400000;
        const int g_baseCorpseDescriptors = 0x010D9D80 - 0x400000;
        const int g_baseAreaTriggerDescriptors = 0x010D9B88 - 0x400000;
        const int g_baseSceneObjectDescriptors = 0x010D9AA4 - 0x400000;

        //const int g_baseItemDynamicDescriptors = 0x01028BEC;
        //const int g_baseUnitDynamicDescriptors = 0x010273B8;
        //const int g_basePlayerDynamicDescriptors = 0x0101C200;

        int[] descriptors =
        {
            g_baseObjDescriptors,
            g_baseItemDescriptors,
            g_baseContainerDescriptors,
            g_baseUnitDescriptors,
            g_basePlayerDescriptors,
            g_baseGameObjectDescriptors,
            g_baseDynamicObjectDescriptors,
            g_baseCorpseDescriptors,
            g_baseAreaTriggerDescriptors,
            g_baseSceneObjectDescriptors
        };

        //int[] dynamicDescriptors =
        //{
        //    g_baseItemDynamicDescriptors,
        //    g_baseUnitDynamicDescriptors,
        //    g_basePlayerDynamicDescriptors
        //};

        Dictionary<string, string> baseDescriptors = new Dictionary<string, string>
        {
            { "CGObjectData", ""},
            { "CGItemData", "CGObjectData.End"},
            { "CGContainerData", "CGItemData.End"},
            { "CGUnitData", "CGObjectData.End"},
            { "CGPlayerData", "CGUnitData.End"},
            { "CGGameObjectData", "CGObjectData.End"},
            { "CGDynamicObjectData", "CGObjectData.End"},
            { "CGCorpseData", "CGObjectData.End"},
            { "CGAreaTriggerData", "CGObjectData.End"},
            { "CGSceneObjectData", "CGObjectData.End"},
        };

        public DescriptorsDumper()
        {
            DumpDescriptors<Descriptor>(descriptors);

            //DumpDescriptors<DynamicDescriptor>(dynamicDescriptors);
        }

        private void DumpDescriptors<T>(int[] offsets) where T : struct
        {
            var sw = new StreamWriter("descriptors.txt");

            foreach (int address in offsets)
            {
                int i = 0;

                string currentPrefix = String.Empty;

                while (true)
                {
                    dynamic d = Memory.Read<T>(Memory.BaseAddress + (address + i * Marshal.SizeOf(typeof(T))));

                    string n = Memory.ReadString(d.m_name, 255);

                    if (currentPrefix == String.Empty)
                    {
                        currentPrefix = Regex.Match(n, @"[a-zA-Z]+(?=::)").Value;
                        //currentPrefix = Regex.Match(n, @".+(?=::)").Value;
                        sw.WriteLine("enum {0}", currentPrefix);
                        sw.WriteLine("{");
                    }

                    string memberName = Regex.Match(n, @"(?<=::)[0-9a-zA-Z_.]+").Value;
                    //string memberName = Regex.Match(n, @"(?<=::).+").Value;

                    if (memberName.StartsWith("m_"))
                        memberName = memberName.Remove(0, 2);

                    if (memberName.StartsWith("local."))
                        memberName = memberName.Remove(0, 6);

                    if (memberName != String.Empty && char.IsLower(memberName, 0))
                        memberName = char.ToUpper(memberName[0]) + memberName.Substring(1);

                    // hack
                    if (n == "CGUnitData::npcFlags[UMNW0]")
                        d.m_size = 2;

                    if (currentPrefix != String.Empty && !n.StartsWith(currentPrefix))
                    {
                        if (baseDescriptors[currentPrefix] != String.Empty)
                            sw.WriteLine("    End = {0} + {1}", baseDescriptors[currentPrefix], i);
                        else
                            sw.WriteLine("    End = {0}", i);

                        sw.WriteLine("}");
                        break;
                    }

                    if (baseDescriptors[currentPrefix] != String.Empty)
                        sw.WriteLine("    {0} = {1} + {2}, // size {3}, flags {4}", memberName, baseDescriptors[currentPrefix], i, d.m_size, d.m_flags);
                    else
                        sw.WriteLine("    {0} = {1}, // size {2}, flags {3}", memberName, i, d.m_size, d.m_flags);

                    i += d.m_size;
                }

                currentPrefix = String.Empty;

                sw.WriteLine();
            }

            sw.Close();
        }
    }
}
