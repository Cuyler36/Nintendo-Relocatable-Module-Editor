using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Nintendo_Relocatable_Module_Editor
{
    public enum RelocationType : byte
    {
        R_PPC_NONE = 0,
        R_PPC_ADDR32 = 1,
        R_PPC_ADDR24 = 2,
        R_PPC_ADDR16 = 3,
        R_PPC_ADDR16_LO = 4,
        R_PPC_ADDR16_HI = 5,
        R_PPC_ADDR16_HA = 6,
        R_PPC_ADDR14 = 7,
        R_PPC_ADDR14_BRTAKEN = 8,
        R_PPC_ADDR14_BRNTAKEN = 9,
        R_PPC_REL24 = 10,
        R_PPC_REL14 = 11,
        // 12 and 13 have something to do with R_PPC_REL14 (probably BRTAKEN & BRNTAKEN)

        R_DOLPHIN_NOP = 201,
        R_DOLPHIN_SECTION = 202,
        R_DOLPHIN_END = 203,
        R_DOLPHIN_MAKEREF = 204
    }

    public class REL_Header
    {
        public uint Id;
        public uint Previous;
        public uint Next;
        public uint Section_Count;

        public uint Section_Table_Offset;
        public uint Name_Offset;
        public uint Name_Size;
        public uint Version;

        public uint BSS_Size;
        public uint Relocation_Offset;
        public uint Import_Offset;
        public uint Import_Size;

        public byte Prolog_Section;
        public byte Epilog_Section;
        public byte Unresolved_Section;
        public byte BSS_Section;
        public uint Prolog_Offset;
        public uint Epilog_Offset;
        public uint Unresolved_Offset;

        public uint Alignment;
        public uint BSS_Alignment;
        public uint Fix_Size;

        public REL_Header(byte[] Buffer)
        {
            if (Buffer.Length >= 0x4C) // TODO: Replace with Reverse extension methods
            {
                Id = BitConverter.ToUInt32(Buffer.Skip(0x00).Take(0x04).Reverse().ToArray(), 0);
                Previous = BitConverter.ToUInt32(Buffer.Skip(0x04).Take(0x04).Reverse().ToArray(), 0);
                Next = BitConverter.ToUInt32(Buffer.Skip(0x08).Take(0x04).Reverse().ToArray(), 0);
                Section_Count = BitConverter.ToUInt32(Buffer.Skip(0x0C).Take(0x04).Reverse().ToArray(), 0);

                Section_Table_Offset = BitConverter.ToUInt32(Buffer.Skip(0x10).Take(0x04).Reverse().ToArray(), 0);
                Name_Offset = BitConverter.ToUInt32(Buffer.Skip(0x14).Take(0x04).Reverse().ToArray(), 0);
                Name_Size = BitConverter.ToUInt32(Buffer.Skip(0x18).Take(0x04).Reverse().ToArray(), 0);
                Version = BitConverter.ToUInt32(Buffer.Skip(0x1C).Take(0x04).Reverse().ToArray(), 0);

                BSS_Size = BitConverter.ToUInt32(Buffer.Skip(0x20).Take(0x04).Reverse().ToArray(), 0);
                Relocation_Offset = BitConverter.ToUInt32(Buffer.Skip(0x24).Take(0x04).Reverse().ToArray(), 0);
                Import_Offset = BitConverter.ToUInt32(Buffer.Skip(0x28).Take(0x04).Reverse().ToArray(), 0);
                Import_Size = BitConverter.ToUInt32(Buffer.Skip(0x2C).Take(0x04).Reverse().ToArray(), 0);

                Prolog_Section = Buffer[0x30];
                Epilog_Section = Buffer[0x31];
                Unresolved_Section = Buffer[0x32];
                BSS_Section = Buffer[0x33];
                Prolog_Offset = BitConverter.ToUInt32(Buffer.Skip(0x34).Take(0x04).Reverse().ToArray(), 0);
                Epilog_Offset = BitConverter.ToUInt32(Buffer.Skip(0x38).Take(0x04).Reverse().ToArray(), 0);
                Unresolved_Offset = BitConverter.ToUInt32(Buffer.Skip(0x3C).Take(0x04).Reverse().ToArray(), 0);

                if (Version > 1)
                {
                    Alignment = BitConverter.ToUInt32(Buffer.Skip(0x40).Take(0x04).Reverse().ToArray(), 0);
                    BSS_Alignment = BitConverter.ToUInt32(Buffer.Skip(0x44).Take(0x04).Reverse().ToArray(), 0);
                }

                if (Version > 2)
                {
                    Fix_Size = BitConverter.ToUInt32(Buffer.Skip(0x48).Take(0x04).Reverse().ToArray(), 0);
                }
            }
        }

        public int GetHeaderSize()
        {
            switch(Version)
            {
                case 1:
                    return 0x40;
                case 2:
                    return 0x48;
                case 3:
                    return 0x4C;
                default:
                    return 0x4C;
            }
        }
    }

    public class Section_Entry
    {
        public uint Offset;
        public byte Unknown;
        public byte Executable;
        public uint Size;

        // Top two bits of offset are flags
        public Section_Entry(uint offset, uint size)
        {
            Offset = offset & 0x3FFFFFFE; // Seems like bit 1 may be the executable flag
            Unknown = (byte)((offset & 0x40000000) >> 30);
            Executable = (byte)(offset & 1);
            Size = size;
        }
    }

    public class Import_Entry // Specifies that a file should be imported from this module if the current relocation entry's offset is greater than or equal to the Offset
    {
        public uint Module_Id; // 0 means refer main.dol/boot.dol
        public uint Offset;
        public string Name;

        public Import_Entry(uint id, uint offset, string name = null)
        {
            Module_Id = id;
            Offset = offset;

            Name = name ?? id.ToString();
            Debug.WriteLine(string.Format("Loaded Import Module #{0} | Offset: 0x{2} | Name: {1}", Module_Id, Name, Offset.ToString("X")));
        }
    }

    public class Relocation_Entry
    {
        public ushort Offset;
        public byte Type_Value;
        public byte Section;
        public uint Addend; // Offset of the symbol to relocate against (Absolute Address)
        public RelocationType Type;
        public string Description = "";

        public Relocation_Entry(ushort offset, byte type, byte section, uint addend)
        {
            Offset = offset;
            Type_Value = type;
            Section = section;
            Addend = addend;

            Type = (RelocationType)type;
        }

        public bool IsEnd()
        {
            return Type == RelocationType.R_DOLPHIN_END;
        }
    }

    // TODO: .bss is set to 0 because it's zero'ed data that is allocated when the program is started. Remove the .bss logic.
    class REL
    {
        public REL_Header Header;
        public Section_Entry[] Section_Entries;
        public Import_Entry[] Import_Entries;
        public Relocation_Entry[] Relocation_Entries;
        Dictionary<uint, string> Module_Names;
        Dictionary<uint, Section_Entry[]> External_Sections;
        string Location;

        public REL(byte[] Rel_Buffer, string Rel_Location, bool Load_Externals = true)
        {
            Debug.WriteLine(string.Format("============= Loading REL {0} =============", Path.GetFileNameWithoutExtension(Rel_Location)));
            Header = new REL_Header(Rel_Buffer);
            Location = Rel_Location;

            // Load Section Entries
            Section_Entries = new Section_Entry[Header.Section_Count];
            for (int i = 0; i < Header.Section_Count; i++)
            {
                int Section_Offset = (int)(Header.Section_Table_Offset + i * 8);
                if (Section_Offset >= Rel_Buffer.Length)
                {
                    Debug.WriteLine(string.Format("Section {0} could not be loaded as the file ended while parsing it!", i));
                    continue;
                }

                Section_Entry Entry = new Section_Entry(BitConverter.ToUInt32(Rel_Buffer, Section_Offset).Reverse(),
                    BitConverter.ToUInt32(Rel_Buffer, Section_Offset + 4).Reverse());

                // Sanity Checks
                if (Entry.Offset == 0 && Entry.Size != 0) // .bss Section
                {
                    // Set .bss offset
                    if (i > 0)
                    {
                        Entry.Offset = Section_Entries[i - 1].Offset + Section_Entries[i - 1].Size;
                    }

                    if (Entry.Size != Header.BSS_Size)
                    {
                        Debug.WriteLine(string.Format("Section {0} seems to be the .bss section, but its size does not match the header's size. This size: 0x{1} | Header size: 0x{2}",
                            i, Entry.Size.ToString("X"), Header.BSS_Size.ToString("X")));
                    }
                    else if (Entry.Offset == Header.Import_Offset || Entry.Offset == Header.Relocation_Offset)
                    {
                        Debug.WriteLine(string.Format("Section {0} seemst o be the .bss section, but it overlaps with the import or relocation section. It's likely not in the file.",
                            i));
                    }
                    else
                    {
                        Debug.WriteLine(string.Format("Successfully loaded the .bss Section: {0} | Offset: {1} | Size: {2}", i, Entry.Offset.ToString("X"), Entry.Size.ToString("X")));
                    }
                }
                else if (Entry.Offset != 0 && Entry.Size != 0)
                {
                    if (Entry.Offset < Header.GetHeaderSize())
                    {
                        Debug.WriteLine("Section {0} seems to be inside the REL header! Offset: 0x{1} | Expected Minimum Offset: 0x{2}", i, Entry.Offset.ToString("X"),
                            Header.GetHeaderSize().ToString("X"));
                    }
                    else if (Entry.Offset + Entry.Size >= Rel_Buffer.Length)
                    {
                        Debug.WriteLine("Section {0} seems to go past the end of the file! End Offset: 0x{1} | Maximum End Offset: 0x{2}", i,
                            (Entry.Offset + Entry.Size).ToString("X"), Rel_Buffer.Length.ToString("X"));
                    }
                    else
                    {
                        Debug.WriteLine(string.Format("Successfully loaded Section {0}! Offset: 0x{1} | Size: 0x{2} | End Offset: 0x{3}",
                            i, Entry.Offset.ToString("X"), Entry.Size.ToString("X"), (Entry.Offset + Entry.Size).ToString("X")));
                    }
                }
                else
                {
                    Debug.WriteLine(string.Format("Section {0} has a zeroed Offset and Size! It's probably an unused section.", i));
                }

                //Debug.WriteLine(string.Format("Added section #{0} with a start address of 0x{1}", i, Entry.Offset.ToString("X8")));
                Section_Entries[i] = Entry;
            }

            // Load Import Table Entries
            LoadModuleNames(Rel_Location, Load_Externals);
            int Import_Table_Count = (int)(Header.Import_Size / 8); //(int)(Header.Relocation_Offset - Header.Import_Offset) / 8;
            Import_Entries = new Import_Entry[Import_Table_Count];

            for (int i = 0; i < Import_Table_Count; i++)
            {
                int Current_Offset = (int)(Header.Import_Offset + i * 8);
                uint Id = BitConverter.ToUInt32(Rel_Buffer, Current_Offset).Reverse();
                Import_Entries[i] = new Import_Entry(Id,
                    BitConverter.ToUInt32(Rel_Buffer, Current_Offset + 4).Reverse(), Module_Names.ContainsKey(Id) ? Module_Names[Id] : null);
            }

            // Load and Patch Relocation Table Entries
            //PatchRELEntries(Rel_Buffer);
        }

        private void LoadModuleNames(string Location, bool Load_Externals = true)
        {
            string Folder = Path.GetDirectoryName(Location);
            if (Directory.Exists(Folder))
            {
                Module_Names = new Dictionary<uint, string>();
                External_Sections = new Dictionary<uint, Section_Entry[]>();
                string[] Files = Directory.GetFiles(Folder);
                for (int i = 0; i < Files.Length; i++)
                {
                    string Extension = Path.GetExtension(Files[i]);
                    string File_Name = Path.GetFileNameWithoutExtension(Files[i]);
                    byte[] Buff;
                    if (Extension == ".rel" && !Files[i].Equals(Location))
                    {
                        //Debug.WriteLine("Loading rel: " + File_Name);
                        Buff = File.ReadAllBytes(Files[i]);
                        uint Id = BitConverter.ToUInt32(Buff.Take(4).Reverse().ToArray(), 0);

                        // Don't Load a REL file with the same id as the current one
                        if (Id == Header.Id)
                        {
                            Debug.WriteLine(string.Format("Skipping loading {0} - the id was the same as the current rel!", File_Name));
                            continue;
                        }

                        if (Id == 0)
                        {
                            Debug.WriteLine(string.Format("Module Loading Error: A REL file has an ID of 0! File Name: {0}", File_Name));
                            continue;
                        }
                        else if (Module_Names.ContainsKey(Id))
                        {
                            Debug.WriteLine(string.Format("Module Loading Error: A REL file has an ID that was previously found! File Name: {0}", File_Name));
                            continue;
                        }
                        Module_Names.Add(Id, File_Name);
                        if (Load_Externals)
                        {
                            REL External_Rel = new REL(Buff, Files[i], false);
                            External_Sections.Add(Id, External_Rel.Section_Entries);
                        }
                    }
                    else if (Extension == ".dol" && !Module_Names.ContainsKey(0))
                    {
                        Module_Names.Add(0, File_Name);
                    }
                }
            }
        }

        private uint GetSectionAddress(uint Section, uint Offset = 0)
        {
            return Section_Entries[Section].Offset + Offset;
        }

        private void PatchRELEntries(byte[] File_Buffer)
        {
            List<Relocation_Entry> Relocation_Entries_List = new List<Relocation_Entry>();
            List<string> Relocated_Output = new List<string>();
            for (int i = 0; i < Import_Entries.Length; i++)
            {
                Import_Entry Current_Import_Entry = Import_Entries[i];
                uint Current_Section = 0;
                uint Current_Offset = 0;
                uint Value = 0;
                uint Where = 0;
                uint Orig = 0;
                int Index = 0;

                if (Current_Import_Entry.Module_Id == Header.Id) // These relocations are inside our own file
                {
                    while (true)
                    {
                        int Rel_Offset = (int)(Current_Import_Entry.Offset + Index * 8);
                        Relocation_Entry Entry = new Relocation_Entry(BitConverter.ToUInt16(File_Buffer, Rel_Offset).Reverse(),
                            File_Buffer[Rel_Offset + 2], File_Buffer[Rel_Offset + 3], BitConverter.ToUInt32(File_Buffer, Rel_Offset + 4).Reverse());
                        Relocation_Entries_List.Add(Entry);
                        //Debug.WriteLine(Entry.Offset.ToString("X4") + " | " + Entry.Type_Value.ToString());

                        if (Entry.IsEnd())
                            break;

                        Current_Offset += Entry.Offset;
                        uint OverwriteAddress = 0;
                        uint OverwriteValue = 0;
                        switch (Entry.Type)
                        {
                            case RelocationType.R_DOLPHIN_SECTION:
                                Current_Section = Entry.Section;
                                Current_Offset = 0;
                                //Debug.WriteLine("DOLPHIN_SECTION Set Section to " + Current_Section);
                                //Relocated_Output.Add("DOLPHIN_SECTION Set Section to " + Current_Section);
                                break;
                            case RelocationType.R_DOLPHIN_NOP:
                                break;
                            case RelocationType.R_PPC_ADDR32:
                                OverwriteAddress = GetSectionAddress(Current_Section, Current_Offset);
                                OverwriteValue = GetSectionAddress(Entry.Section, Entry.Addend);
                                //Debug.WriteLine(string.Format("ADDR32 Offset 0x{0} had its value patched to address 0x{1}", OverwriteAddress.ToString("X"), OverwriteValue.ToString("X")));
                                //Relocated_Output.Add(string.Format("ADDR32 Offset 0x{0} had its value patched to address 0x{1}", OverwriteAddress.ToString("X"), OverwriteValue.ToString("X")));
                                break;
                            case RelocationType.R_PPC_ADDR16_LO:
                                OverwriteAddress = GetSectionAddress(Current_Section, Current_Offset);
                                OverwriteValue = GetSectionAddress(Entry.Section, Entry.Addend) & 0xFFFF;
                                //Debug.WriteLine(string.Format("ADDR16_LO Offset 0x{0} had its value patched to address 0x{1}", OverwriteAddress.ToString("X"), OverwriteValue.ToString("X")));
                                //Relocated_Output.Add(string.Format("ADDR16_LO Offset 0x{0} had its value patched to address 0x{1}", OverwriteAddress.ToString("X"), OverwriteValue.ToString("X")));
                                break;
                            case RelocationType.R_PPC_ADDR16_HA:
                                OverwriteAddress = GetSectionAddress(Current_Section, Current_Offset);
                                OverwriteValue = GetSectionAddress(Entry.Section, Entry.Addend);
                                if ((OverwriteValue & 0x8000) == 0x8000)
                                    OverwriteValue += 0x00010000;
                                //OverwriteValue += 0x80000000;
                                OverwriteValue = (OverwriteValue >> 16) & 0xFFFF;
                                //Debug.WriteLine(string.Format("ADDR16_HA Offset 0x{0} had its value patched to address 0x{1}", OverwriteAddress.ToString("X"), OverwriteValue.ToString("X")));
                                //Relocated_Output.Add(string.Format("ADDR16_HA Offset 0x{0} had its value patched to address 0x{1}", OverwriteAddress.ToString("X"), OverwriteValue.ToString("X")));
                                break;
                            case RelocationType.R_PPC_REL24:
                                Where = GetSectionAddress(Current_Section, Current_Offset);
                                Value = GetSectionAddress(Entry.Section, Entry.Addend);
                                Value -= Where;
                                Orig = BitConverter.ToUInt32(File_Buffer, (int)Where).Reverse();
                                Orig &= 0xFC000003;
                                Orig |= Value & 0x03FFFFFC;
                                //Debug.WriteLine("REL24 Offset 0x{0} is now 0x{1}", Where.ToString("X"), Orig.ToString("X"));
                                //Relocated_Output.Add(string.Format("REL24 Offset 0x{0} is now 0x{1}", Where.ToString("X"), Orig.ToString("X")));
                                break;
                            default:
                                Debug.WriteLine(string.Format("Unsupported Relocation Type: {0}", Entry.Type));
                                break;
                        }
                        Index++;
                    }
                }
                else // Move on to external relocations
                {
                    /*while (true)
                    {
                        int Rel_Offset = (int)(Current_Import_Entry.Offset + Index * 8);
                        Relocation_Entry Entry = new Relocation_Entry(BitConverter.ToUInt16(File_Buffer.Skip(Rel_Offset).Take(2).Reverse().ToArray(), 0),
                            File_Buffer[Rel_Offset + 2], File_Buffer[Rel_Offset + 3], BitConverter.ToUInt32(File_Buffer.Skip(Rel_Offset + 4).Take(4).Reverse().ToArray(), 0));
                        Relocation_Entries_List.Add(Entry);

                        if (Entry.IsEnd())
                            break;

                        //if (Entry.Type == RelocationType.R_DOLPHIN_SECTION || Entry.Type == RelocationType.R_DOLPHIN_NOP)
                            //Debug.WriteLine("Extenral Entry called an unneeded opcode: " + Entry.Type.ToString());

                        Index++;
                    }*/
                }
            }

            using (TextWriter Writer = File.CreateText(Path.GetDirectoryName(Location) + "\\Relocation_Output.txt"))
            {
                foreach (string s in Relocated_Output)
                    Writer.WriteLine(s);

                Writer.Flush();
            }
        }
    }
}
