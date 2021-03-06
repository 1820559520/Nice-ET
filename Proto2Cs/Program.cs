﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using NiceET;

namespace NiceET
{

    public static class Program
    {
        private const string protoPath = "../protos";
        private const string clientMessagePath = "../csharp_out/";
        private static readonly char[] splitChars = { ' ', '\t' };
        private static readonly List<OpcodeInfo> msgOpcode = new List<OpcodeInfo>();


        public static void Main()
        {
            // InnerMessage.proto生成cs代码
            Proto2CSTool.Proto2CS();

            Proto2CS("NiceET", "OuterMessage.proto", clientMessagePath, "OuterOpcode", 100);

            Console.WriteLine("proto2cs succeed!");
        }

        

        public static void Proto2CS(string ns, string protoName, string outputPath, string opcodeClassName, int startOpcode, bool isClient = true)
        {
            msgOpcode.Clear();
            string proto = Path.Combine(protoPath, protoName);

            string s = File.ReadAllText(proto);

            StringBuilder sb = new StringBuilder();
            sb.Append($"namespace {ns}\n");
            sb.Append("{\n");

            bool isMsgStart = false;

            foreach (string line in s.Split('\n'))
            {
                string newline = line.Trim();

                if (newline == "")
                {
                    continue;
                }

                if (newline.StartsWith("//"))
                {
                    sb.Append($"{newline}\n");
                }

                if (newline.StartsWith("message"))
                {
                    string parentClass = "";
                    isMsgStart = true;
                    string msgName = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)[1];
                    string[] ss = newline.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries);

                    if (ss.Length == 2)
                    {
                        parentClass = ss[1].Trim();
                    }
                    else
                    {
                        parentClass = "";
                    }

                    msgOpcode.Add(new OpcodeInfo() { Name = msgName, Opcode = ++startOpcode });

                    sb.Append($"\t[Message({opcodeClassName}.{msgName})]\n");
                    sb.Append($"\tpublic partial class {msgName} ");
                    if (parentClass != "")
                    {
                        sb.Append($": {parentClass} ");
                    }

                    sb.Append("{}\n\n");
                }

                if (isMsgStart && newline == "}")
                {
                    isMsgStart = false;
                }
            }

            sb.Append("}\n");

            GenerateOpcode(ns, opcodeClassName, outputPath, sb);
            GenerateLuaIDDefine("Msg", outputPath);
            GenerateLuaIDMap("Msg", outputPath);
        }

        private static void GenerateOpcode(string ns, string outputFileName, string outputPath, StringBuilder sb)
        {
            sb.AppendLine($"namespace {ns}");
            sb.AppendLine("{");
            sb.AppendLine($"\tpublic static partial class {outputFileName}");
            sb.AppendLine("\t{");
            foreach (OpcodeInfo info in msgOpcode)
            {
                sb.AppendLine($"\t\t public const ushort {info.Name} = {info.Opcode};");
            }

            sb.AppendLine("\t}");
            sb.AppendLine("}");

            string csPath = Path.Combine(outputPath, outputFileName + ".cs");
            File.WriteAllText(csPath, sb.ToString());
        }
    
        private static void GenerateLuaIDDefine(string outputFileName, string outputPath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("local config = {");
            foreach (OpcodeInfo info in msgOpcode)
            {
                sb.AppendLine($"\t\t {info.Name.ToUpper()} = {info.Opcode} ,");
            }
            sb.AppendLine("}");
            sb.AppendLine("return config");

            string luaPath = Path.Combine(outputPath, outputFileName + "IDDefine.lua");
            File.WriteAllText(luaPath, sb.ToString());
        }

        private static void GenerateLuaIDMap(string outputFileName, string outputPath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("local config = {");
            foreach (OpcodeInfo info in msgOpcode)
            {
                sb.AppendLine($"\t\t [{info.Opcode}] = \"NiceET.{info.Name}\",");
            }
            sb.AppendLine("}");
            sb.AppendLine("return config");

            string luaPath = Path.Combine(outputPath, outputFileName + "IDMap.lua");
            File.WriteAllText(luaPath, sb.ToString());
        }
    }

    
}
