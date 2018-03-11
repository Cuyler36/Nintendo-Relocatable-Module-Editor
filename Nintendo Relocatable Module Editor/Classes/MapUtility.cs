using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nintendo_Relocatable_Module_Editor
{
    public class Map_Info
    {
        public uint Offset;
        public uint Size;
        public string File_Name;
        public string Parent_Name;
        public string Section_Name;
        public byte[] Data;
    }

    class MapUtility
    {
        public static Dictionary<string, List<Map_Info>> ParseMapFile(string[] Map_File_Lines, byte[] File_Buffer, Section_Entry[] Entries, int BSS_Section = 0) // int File_Start = 0x60
        {
            // Section => Dictionary<File_Offset > File_Name>
            Dictionary<string, List<Map_Info>> Parsed_Map_Data = new Dictionary<string, List<Map_Info>>();
            Dictionary<string, int> Section_Start_Offsets = new Dictionary<string, int>();
            int Index = -1;
            int Current_Index = -1;

            string Memory_Map = Map_File_Lines.FirstOrDefault(o => o.Contains("Memory map:"));
            if (!string.IsNullOrEmpty(Memory_Map))
            {
                int Memory_Map_Index = Array.IndexOf(Map_File_Lines, Memory_Map);
                if (Memory_Map_Index > -1)
                {
                    int i = Memory_Map_Index + 3;
                    while (i < Map_File_Lines.Length && !string.IsNullOrEmpty(Map_File_Lines[i]))
                    {
                        string Section_Info = Map_File_Lines[i].TrimStart();
                        string Section_Name = Regex.Match(Section_Info, @"^[^ ]*").Value;
                        string Section_Offsets = Section_Info.Substring(Section_Name.Length + 11);
                        string Section_Size = Regex.Match(Section_Offsets, @"^[^ ]*").Value;
                        string Section_Offset = Section_Offsets.Substring(Section_Size.Length + 1, 8);
                        if (int.TryParse(Section_Offset, NumberStyles.AllowHexSpecifier, null, out int Offset)
                            && int.TryParse(Section_Size, NumberStyles.AllowHexSpecifier, null, out int Size))
                        {
                            if (Offset != 0 && Size != 0)
                            {
                                Section_Start_Offsets.Add(Section_Name, Offset);
                                Parsed_Map_Data.Add(Section_Name, new List<Map_Info>());
                            }
                        }
                        i++;
                    }

                    string Current_Section = "";
                    for (i = 0; i < Memory_Map_Index; i++)
                    {
                        string Line = Map_File_Lines[i];
                        if (!string.IsNullOrEmpty(Line))
                        {
                            if (Line.Contains(" section layout"))
                            {
                                i += 3; // Skip column text
                                Current_Section = Regex.Match(Line.TrimStart(), @"^[^ ]*").Value;
                                Console.WriteLine("Switched to section: " + Current_Section);
                                if (Section_Start_Offsets.ContainsKey(Current_Section))
                                {
                                    Index++;
                                    if (!Current_Section.Equals(".bss") && Index == BSS_Section)
                                        Index++;

                                    if (Current_Section.Equals(".bss"))
                                    {
                                        Entries[BSS_Section].Offset = Entries[Index - 1].Offset + Entries[Index - 1].Size;
                                        Current_Index = BSS_Section;
                                    }
                                    else
                                        Current_Index = Index;
                                    Console.WriteLine("Section start offset: 0x" + Entries[Current_Index].Offset.ToString("X8"));
                                }
                                //Console.ReadKey();
                            }
                            else if (!string.IsNullOrEmpty(Current_Section))
                            {
                                if (Line.Contains(@"..."))
                                {
                                    //Console.WriteLine("Contained ... : " + Line);
                                    continue;
                                }

                                Line = Line.Trim(); // Clear Leading/Trailing Whitespace
                                Line = Line.Replace("\t", " "); // Confirm all tabs get turned into a space
                                Line = Regex.Replace(Line, @"\s+", " "); // Turn multiple spaces/tabs to one space
                                string[] Line_Data = Line.Split(' ');
                                int Offset = ((int)Entries[Current_Index].Offset + int.Parse(Line_Data[0], NumberStyles.AllowHexSpecifier));
                                int Size = int.Parse(Line_Data[1], NumberStyles.AllowHexSpecifier);
                                bool IsObject = Line_Data[3].Equals("1");
                                string Method_Name = Line_Data[4];
                                string Object_Name = Line_Data[5];

                                //TODO: Add Subsections for objects
                                if (!IsObject && Parsed_Map_Data.ContainsKey(Current_Section))
                                {
                                    Map_Info Current_Info = new Map_Info();
                                    Current_Info.Offset = (uint)Offset;
                                    Current_Info.Size = (uint)Size;
                                    Current_Info.File_Name = Method_Name;
                                    Current_Info.Parent_Name = Object_Name;
                                    Current_Info.Section_Name = Current_Section;
                                    Parsed_Map_Data[Current_Section].Add(Current_Info); // TODO: Sort into object sections
                                }
                            }
                        }
                    }
                }
            }
            return Parsed_Map_Data;
        }
    }
}
