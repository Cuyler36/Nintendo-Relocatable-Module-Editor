using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Nintendo_Relocatable_Module_Editor
{
    public static class GekkoAssembly
    {
        [DllImport("GekkoDisassembler.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static void Disassemble(uint Instruction, uint Current_Address, StringBuilder Builder, int Length);

        public static void Test()
        {
            StringBuilder Builder = new StringBuilder(100);
            try
            {
                Disassemble(0x90C10038, 0x80000000, Builder, 100);
                Debug.WriteLine(Builder.ToString());
            } catch
            {
                Debug.WriteLine("Test unsuccessful!");
            }
        }

        public static string[] DisassembleHex(uint[] Hex)
        {
            List<string> Assembly = new List<string>();
            StringBuilder Builder = new StringBuilder(100);
            for (int i = 0; i < Hex.Length; i++)
            {
                Disassemble(Hex[i], (uint)(0x80000000 + i * 4), Builder, 100);
                Assembly.Add(Builder.ToString());
                Debug.WriteLine(string.Format("Line {0}: {1}", i.ToString("D4"), Assembly[i]));
            }

            return Assembly.ToArray();
        }
    }
}
