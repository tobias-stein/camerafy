using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;

namespace Camerafy.Editor.Service.JSCompiler
{
    using Event;

    public class JSCompiler
    {
        private static readonly string CamerafyJsLibName = "CamerafyEventReflector.ts";
        private static readonly string OutputFolder = Path.Combine("WebApplication", "frontend", "src", "services");

        [MenuItem("Camerafy/API/Compile JS Library")]
        public static void Compile()
        {
            // Get the Camerafy assembly
            Assembly CamerafyAssembly = Assembly.GetAssembly(typeof(Application.Application));

            // Get all exported classes
            Type[] Exports = CamerafyAssembly.GetExportedTypes().Where( x => x.IsClass).ToArray();

            // Get all camerafy commands
            List<MethodInfo> Commands = new List<MethodInfo>();
            foreach (var e in Exports)
            {
                Commands.AddRange(EventLibrary.GetCamerafyEventHandler(e));
            }

            // Sanity check
            if (EventLibrary.CheckCollision(Commands))
            {
                UnityEngine.Debug.LogFormat("Abort compiling '{0}'!", JSCompiler.CamerafyJsLibName);
                return;
            }

            // start compilation
            if (!JSCompiler.StartCompile(Commands))
            {
                UnityEngine.Debug.LogErrorFormat("Failed to compile '{0}'!", JSCompiler.CamerafyJsLibName);
                return;
            }

            UnityEngine.Debug.Log("Comilation done!");
        }

        /// <summary>
        /// Auxilary class to create hierachical module namespace structure.
        /// </summary>
        class Namespace
        {
            public string               Name  { get; set; } = null;
            public List<List<string>>   Codes { get; set; } = new List<List<string>>();
            public List<Namespace>      Child { get; set; } = new List<Namespace>();

            public Namespace(string name)
            {
                this.Name = name;
            }

            public static bool AddCode(Namespace InRoot, List<string> InCode, string Namespace, string MethodName)
            {
                Namespace NS = null;
                foreach (var elem in Namespace.Split('.'))
                {
                    if (NS == null)
                    {
                        if (InRoot.Name == elem)
                        {
                            NS = InRoot;
                        }
                        else
                        {
                            UnityEngine.Debug.LogErrorFormat("'{0}.{1}' not in root namespace 'Camerafy'.", Namespace, MethodName);
                            return false;
                        }
                    }
                    else
                    {
                        // check if child namespace reference exists
                        if (NS.Child.Select(x => x.Name).Contains(elem))
                        {
                            NS = NS.Child.Find(x => x.Name == elem);
                        }
                        // if not add new child namespace
                        else
                        {
                            Namespace child = new Namespace(elem);
                            NS.Child.Add(child);
                            NS = child;
                        }
                    }
                }

                // add code to final namespace
                NS.Codes.Add(InCode);
                return true;
            }
        }

        private static bool StartCompile(List<MethodInfo> InCommands)
        {
            StringBuilder js = new StringBuilder();
            {
                #region LIB HEADER

                js.AppendLine("/** Camerafy - Copy right. All rights reserved. */");
                js.AppendLine("/** AUTOGENERATED CODE. DO NOT TOUCH. */");
                js.AppendLine($"/** {DateTime.UtcNow.ToString()} */");
                js.AppendLine($"/* eslint-disable */");
                js.AppendLine();
                js.AppendLine();

                // imports
                js.AppendLine("import { UUID4 } from '@/api/utils/utils';");
                js.AppendLine();
                
                // camerafy event type
                js.AppendLine("export interface ICamerafyEvent");
                js.AppendLine("{");
                js.AppendLine(TAB("id   : string;"));
                js.AppendLine(TAB("data : Uint8Array;"));
                js.AppendLine("}");
                js.AppendLine();

                js.AppendLine("export enum CamerafySessionDataType");
                js.AppendLine("{");
                js.AppendLine(TAB("EventResponse,"));
                js.AppendLine(TAB("SessionEvent"));
                js.AppendLine("};");
                js.AppendLine();

                js.AppendLine("export enum CamerafyResponseStatus");
                js.AppendLine("{");
                js.AppendLine(TAB("ok,"));
                js.AppendLine(TAB("error"));
                js.AppendLine("};");
                js.AppendLine();

                js.AppendLine("export interface CamerafyResponseData");
                js.AppendLine("{");
                js.AppendLine(TAB("status : CamerafyResponseStatus;"));
                js.AppendLine(TAB("result : any;"));
                js.AppendLine("};");
                js.AppendLine();

                js.AppendLine("export interface ICamerafySessionData");
                js.AppendLine("{");
                js.AppendLine(TAB("type   				: CamerafySessionDataType;"));
                js.AppendLine(TAB("responseRefId? 		: string;"));
                js.AppendLine(TAB("responseData? 		: CamerafyResponseData;"));
                js.AppendLine(TAB("sessionEventName?	: string;"));
                js.AppendLine(TAB("sessionEventArgs?	: Array<any>;"));
                js.AppendLine("};");
                js.AppendLine();

                // String to ArrayBuffer helper 
                js.AppendLine("function EncodeString(str : string, buffer : ArrayBuffer, index : number, writeSize = true) : number");
                js.AppendLine("{");
                js.AppendLine(TAB("if(!writeSize)"));
                js.AppendLine(TAB("return (new TextEncoder('UTF-8').encodeInto(str, new Uint8Array(buffer, index))).written;", 2));
                js.AppendLine();
                js.AppendLine(TAB("const result = new TextEncoder('UTF-8').encodeInto(str, new Uint8Array(buffer, index + 4));"));
                js.AppendLine(TAB("const tmp = new DataView(buffer, index);"));
                js.AppendLine(TAB("tmp.setUint32(0, result.written, true);"));
                js.AppendLine(TAB("return result.written + 4;"));
                js.AppendLine("}");
                js.AppendLine();

                js.AppendLine("function DecodeString(InUint8Arr : Uint8Array, index : number, length : number) : string");
                js.AppendLine("{");
                js.AppendLine(TAB("return new TextDecoder('UTF-8').decode(new Uint8Array(InUint8Arr.buffer, index, length));"));
                js.AppendLine("}");
                js.AppendLine();
                
                js.AppendLine("function GetArrayOfStringByteCount(InStringArray)");
                js.AppendLine("{");
                js.AppendLine(TAB("let Count = 0;"));
                js.AppendLine(TAB("let n;"));
                js.AppendLine(TAB("for(n = 0; n < InStringArray.length; n++)"));
                js.AppendLine(TAB("{"));
                js.AppendLine(TAB("Count += InStringArray[n].length;", 2));
                js.AppendLine(TAB("}"));
                js.AppendLine(TAB("return Count;"));
                js.AppendLine("}");
                js.AppendLine();
                js.AppendLine();

                #endregion

                #region LIB CLASS BEGIN

                // Class
                js.AppendLine("class CamerafyEventReflector");
                js.AppendLine("{");

                // Constructor
                js.AppendLine(TAB("constructor()"));
                js.AppendLine(TAB("{"));
                js.AppendLine(TAB("}"));
                js.AppendLine();

                /** Deserializes received session data and returns parsed arguments. */
                js.AppendLine(TAB("public deserializeSessionData(InUint8Arr : Uint8Array) : ICamerafySessionData"));
                js.AppendLine(TAB("{"));
                js.AppendLine(TAB("const name = DecodeString(InUint8Arr, 0, 32);", 3));
                js.AppendLine();
                js.AppendLine(TAB("if(name == \"00000000000000000000000000000000\")", 3));
                js.AppendLine(TAB("{", 3));
                js.AppendLine(TAB("const data = new DataView(InUint8Arr.buffer);", 4));
                js.AppendLine(TAB("let offset = 32;", 4));
                js.AppendLine(TAB("let len = 0;", 4));
                js.AppendLine(TAB("len = data.getInt32(offset, true); offset += 4;", 4));
                js.AppendLine(TAB("const messageId = DecodeString(InUint8Arr, offset, len); offset += len;", 4));
                js.AppendLine(TAB("len = data.getInt32(offset, true); offset += 4;", 4));
                js.AppendLine(TAB("const returnJson = DecodeString(InUint8Arr, offset, len);", 4));
                js.AppendLine(TAB("return { type: CamerafySessionDataType.EventResponse, responseRefId: messageId, responseData: JSON.parse(returnJson) };", 4));
                js.AppendLine(TAB("}", 3));
                js.AppendLine();
                js.AppendLine(TAB("return { type: CamerafySessionDataType.SessionEvent, sessionEventName: name, sessionEventArgs: eval(`__${name}`)(InUint8Arr) };", 3));
                js.AppendLine();
                js.AppendLine();
                js.AppendLine(TAB("/* Server side events argument parser */", 3));

                foreach (var ArgParser in JSCompiler.GenerateServerCommandArgumentParser(InCommands))
                {
                    foreach (var line in ArgParser)
                        js.AppendLine(TAB(line, 3));
                }
                js.AppendLine(TAB("}"));
                js.AppendLine();

                js.AppendLine(TAB("private onMessageResponse(InMessageId : string, InReturnJson : string) : void", 2));
                js.AppendLine(TAB("{", 2));
                js.AppendLine(TAB("const PromiseResolve = this.PendingRequests.get(InMessageId);", 3));
                js.AppendLine(TAB("if(PromiseResolve === undefined)", 3));
                js.AppendLine(TAB("{", 3));
                js.AppendLine(TAB("console.error(\"Received response for unknown request.\");", 4));
                js.AppendLine(TAB("return;", 4));
                js.AppendLine(TAB("}", 3));
                js.AppendLine(TAB("this.PendingRequests.delete(InMessageId);", 3));
                js.AppendLine(TAB("PromiseResolve(JSON.parse(InReturnJson));", 3));
                js.AppendLine(TAB("}", 2));

                js.AppendLine();

                #endregion

                // root
                Namespace Modules = new Namespace("Camerafy");

                // pre process client to server commands
                foreach (var cmd in InCommands.Where(x => x.GetCustomAttribute<CamerafyEventAttribute>().Properties.HasFlag(CamerafyEventProperty.Client)))
                {
                    List<string> code;
                    if (!JSCompiler.GenerateClientToServerFunctionCode(cmd, out code))
                        return false;

                    if (!JSCompiler.Namespace.AddCode(Modules, code, cmd.DeclaringType.FullName, cmd.Name))
                        return false;
                }

                // compile modularized
                foreach (var module in Modules.Child)
                {
                    JSCompiler.CompileCodes(module, js, 1, true);
                }

                #region LIB CLASS END

                js.AppendLine("};");

                #endregion

                #region LIB FOOTER

                js.AppendLine();
                js.AppendLine("/* Server side events */");

                foreach (var CmdExport in JSCompiler.GetServerCommandExports(InCommands))
                {
                    js.AppendLine(CmdExport);
                }

                js.AppendLine(); 
                js.AppendLine();
                js.AppendLine("const EventReflector = new CamerafyEventReflector();");
                js.AppendLine("export default EventReflector;");
                js.AppendLine();
                #endregion
            }

            // create final output file name
            string OutputFilename = Path.Combine(UnityEngine.Application.dataPath, "..", JSCompiler.OutputFolder, JSCompiler.CamerafyJsLibName);

            try
            {
                // make sure folder hierarchy exists
                Directory.CreateDirectory(Path.GetDirectoryName(OutputFilename));

                UnityEngine.Debug.Log($"Save library code to '{OutputFilename}'...");
                File.WriteAllText(OutputFilename, js.ToString());
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e.Message);
                return false;
            }

            return true;
        }

        private static List<List<string>> GenerateServerCommandArgumentParser(List<MethodInfo> InCommands)
        {
            List<List<string>> ParserMethods = new List<List<string>>();

            foreach (var ServerCmd in InCommands.Where(x => x.GetCustomAttribute<CamerafyEventAttribute>().Properties.HasFlag(CamerafyEventProperty.Server)))
            {
                List<string> Lines = new List<string>();

                Lines.Add($"function __{EventLibrary.CalculateMethodAddress(ServerCmd)}(InUint8Arr : Uint8Array)");
                Lines.Add("{");
                Lines.Add(TAB("let Args = new Array<any>();"));

                if (ServerCmd.GetParameters().Length > 0)
                {
                    Lines.Add(TAB("let data = new DataView(InUint8Arr.buffer);"));
                    Lines.Add(TAB("let offset = 32; // skip first bytes, as they hold the method address"));
                    Lines.Add(TAB("let n = 0;"));
                    Lines.Add(TAB("let N = 1;"));
                    Lines.Add(TAB("let len = 0;"));
                    Lines.Add("");

                    int ArgC = 0;
                    foreach(var param in ServerCmd.GetParameters())
                    {
                        Lines.Add(TAB($"/* {param.Name} */"));
                        Lines.Add(TAB($"let arg{ArgC} = [];"));

                        bool isArray = param.ParameterType.IsArray;
                        Type type =  isArray ? param.ParameterType.GetElementType() : param.ParameterType;

                        if (isArray)
                        {
                            Lines.Add(TAB($"N = data.getInt32(offset, true); offset += 4;"));
                            Lines.Add(TAB("for(n = 0; n < N; n++)"));
                            Lines.Add(TAB("{"));
                        }
                        
                        switch (Type.GetTypeCode(type))
                        {
                            case TypeCode.Boolean:
                            case TypeCode.Byte:
                            {
                                if (isArray)
                                    Lines.Add(TAB($"arg{ArgC}.push(data.getInt8(offset)); offset += 1;", 2));
                                else
                                    Lines.Add(TAB($"arg{ArgC} = data.getInt8(offset); offset += 1;", 1));
                                break;
                            }
                            case TypeCode.SByte:
                            {
                                if (isArray)
                                    Lines.Add(TAB($"arg{ArgC}.push(data.getUInt8(offset)); offset += 1;", 2));
                                else
                                    Lines.Add(TAB($"arg{ArgC} = data.getUInt8(offset); offset += 1;", 1));
                                break;
                            }
                            case TypeCode.Char:
                            {
                                if (isArray)
                                    Lines.Add(TAB($"arg{ArgC}.push(data.getInt16(offset, true)); offset += 2;", 2));
                                else
                                    Lines.Add(TAB($"arg{ArgC} = data.getInt16(offset, true); offset += 2;", 1));
                                break;
                            }
                            case TypeCode.Single:
                            {
                                if (isArray)
                                    Lines.Add(TAB($"arg{ArgC}.push(data.getFloat32(offset, true)); offset += 4;", 2));
                                else
                                    Lines.Add(TAB($"arg{ArgC} = data.getFloat32(offset, true); offset += 4;", 1));
                                break;
                            }
                            case TypeCode.Double:
                            {
                                if (isArray)
                                    Lines.Add(TAB($"arg{ArgC}.push(data.getFloat64(offset, true)); offset += 8;", 2));
                                else
                                    Lines.Add(TAB($"arg{ArgC} = data.getFloat64(offset, true); offset += 8;", 1));
                                break;
                            }
                            case TypeCode.Int16:
                            {
                                if (isArray)
                                    Lines.Add(TAB($"arg{ArgC}.push(data.getInt16(offset, true)); offset += 2;", 2));
                                else
                                    Lines.Add(TAB($"arg{ArgC} = data.getInt16(offset, true); offset += 2;", 1));
                                break;
                            }
                            case TypeCode.Int32:
                            {
                                if (isArray)
                                    Lines.Add(TAB($"arg{ArgC}.push(data.getInt32(offset, true)); offset += 4;", 2));
                                else
                                    Lines.Add(TAB($"arg{ArgC} = data.getInt32(offset, true); offset += 4;", 1));
                                break;
                            }
                            case TypeCode.Int64:
                            {
                                if (isArray)
                                    Lines.Add(TAB($"arg{ArgC}.push(data.getBigInt64(offset, true)); offset += 8;", 2));
                                else
                                    Lines.Add(TAB($"arg{ArgC} = data.getBigInt64(offset, true); offset += 8;", 1));
                                break;
                            }
                            case TypeCode.UInt16:
                            {
                                if (isArray)
                                    Lines.Add(TAB($"arg{ArgC}.push(data.getUint16(offset, true)); offset += 2;", 2));
                                else
                                    Lines.Add(TAB($"arg{ArgC} = data.getUint16(offset, true); offset += 2;", 1));
                                break;
                            }
                            case TypeCode.UInt32:
                            {
                                if (isArray)
                                    Lines.Add(TAB($"arg{ArgC}.push(data.getUint32(offset, true)); offset += 4;", 2));
                                else
                                    Lines.Add(TAB($"arg{ArgC} = data.getUint32(offset, true); offset += 4;", 1));
                                break;
                            }
                            case TypeCode.UInt64:
                            {
                                if (isArray)
                                    Lines.Add(TAB($"arg{ArgC}.push(data.getBigUint64(offset, true)); offset += 8;", 2));
                                else
                                    Lines.Add(TAB($"arg{ArgC} = data.getBigUint64(offset, true); offset += 8;", 1));
                                break;
                            }
                            case TypeCode.String:
                            {
                                Lines.Add(TAB("len = data.getInt32(offset, true); offset += 4;", isArray ? 2 : 1));
                                if (isArray)
                                    Lines.Add(TAB($"arg{ArgC}.push(DecodeString(InUint8Arr, offset, len)); offset += len;", 2));
                                else
                                    Lines.Add(TAB($"arg{ArgC} = DecodeString(InUint8Arr, offset, len); offset += len;", 1));
                                break;
                            }
                        }

                        if(isArray)
                            Lines.Add(TAB("}"));

                        Lines.Add(TAB($"Args.push(arg{ArgC});"));
                        Lines.Add("");
                        ArgC++;
                    }
                }

                Lines.Add(TAB("return Args;"));
                Lines.Add("}");
                Lines.Add("");

                ParserMethods.Add(Lines);
            }

            return ParserMethods;
        }

        private static List<string> GetServerCommandExports(List<MethodInfo> InCommands)
        {
            List<string> Exports = new List<string>();

            foreach (var ServerCmd in InCommands.Where(x => x.GetCustomAttribute<CamerafyEventAttribute>().Properties.HasFlag(CamerafyEventProperty.Server)))
            {
                Exports.Add($"export const {ServerCmd.Name}ServerEvent = \"{EventLibrary.CalculateMethodAddress(ServerCmd)}\";");
            }

            return Exports;
        }

        private static bool GenerateClientToServerFunctionCode(MethodInfo InMethod, out List<string> Lines)
        {
            string                      Name            = InMethod.Name;
            ParameterInfo[]             Params          = InMethod.GetParameters();
            CamerafyEventProperty       Properties      = InMethod.GetCustomAttribute<CamerafyEventAttribute>().Properties;  
                
            // unique code identifying this method
            string                      Addess          = EventLibrary.CalculateMethodAddress(InMethod);

            Lines = new List<string>();

            Lines.Add(string.Format("{0}: ({1}) : ICamerafyEvent =>", Name, String.Join(", ", Params.Select(x => { return EncodeParameter(x); }))));
            Lines.Add("{");
            // we add the fixed 68 in front for the message id (36bytes) + address id (32 bytes) = 68 bytes
            Lines.Add(TAB($"let OpCode = new ArrayBuffer(68{JSCompiler.DetermineRequiredArrayBufferSize(Params)});"));
            Lines.Add(TAB("let data = new DataView(OpCode);"));
            Lines.Add(TAB("let offset = 0;"));
            Lines.Add(TAB("let n = 0;"));
            Lines.Add(TAB("let N = 1;"));
            Lines.Add(TAB("let len = 0;"));
            Lines.Add(TAB("const messageId = UUID4();"));
            Lines.Add("");

            // add message id
            Lines.Add(TAB($"offset += EncodeString(messageId, OpCode, offset, false);"));
            // add method address
            Lines.Add(TAB($"offset += EncodeString(\"{Addess}\", OpCode, offset, false);"));
            Lines.Add("");

            foreach(var param in Params)
            {
                Lines.Add(TAB($"/* {param.Name} */"));

                bool isArray = param.ParameterType.IsArray;
                Type type =  isArray ? param.ParameterType.GetElementType() : param.ParameterType;

                if (isArray)
                {
                    Lines.Add(TAB($"N = {param.Name}.length; data.setUint32(offset, N, true); offset += 4;"));
                    Lines.Add(TAB("for(n = 0; n < N; n++)"));
                    Lines.Add(TAB("{"));
                }
                
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                    case TypeCode.Byte:
                    {
                        if (isArray)
                            Lines.Add(TAB($"data.setInt8(offset, {param.Name}[n]); offset += 1;", 2));
                        else
                            Lines.Add(TAB($"data.setInt8(offset, {param.Name}); offset += 1;", 1));
                        break;
                    }
                    case TypeCode.SByte:
                    {
                        if (isArray)
                            Lines.Add(TAB($"data.setUInt8(offset, {param.Name}[n]); offset += 1;", 2));
                        else
                            Lines.Add(TAB($"data.setUInt8(offset, {param.Name}); offset += 1;", 1));
                        break;
                    }
                    case TypeCode.Char:
                    {
                        if (isArray)
                            Lines.Add(TAB($"data.setInt16(offset, {param.Name}[n], true); offset += 2;", 2));
                        else
                            Lines.Add(TAB($"data.setInt16(offset, {param.Name}, true); offset += 2;", 1));
                        break;
                    }
                    case TypeCode.Single:
                    {
                        if (isArray)
                            Lines.Add(TAB($"data.setFloat32(offset, {param.Name}[n], true); offset += 4;", 2));
                        else
                            Lines.Add(TAB($"data.setFloat32(offset, {param.Name}, true); offset += 4;", 1));
                        break;
                    }
                    case TypeCode.Double:
                    {
                        if (isArray)
                            Lines.Add(TAB($"data.setFloat64(offset, {param.Name}[n], true); offset += 8;", 2));
                        else
                            Lines.Add(TAB($"data.setFloat64(offset, {param.Name}, true); offset += 8;", 1));
                        break;
                    }
                    case TypeCode.Int16:
                    {
                        if (isArray)
                            Lines.Add(TAB($"data.setInt16(offset, {param.Name}[n], true); offset += 2;", 2));
                        else
                            Lines.Add(TAB($"data.setInt16(offset, {param.Name}, true); offset += 2;", 1));
                        break;
                    }
                    case TypeCode.Int32:
                    {
                        if (isArray)
                            Lines.Add(TAB($"data.setInt32(offset, {param.Name}[n], true); offset += 4;", 2));
                        else
                            Lines.Add(TAB($"data.setInt32(offset, {param.Name}, true); offset += 4;", 1));
                        break;
                    }
                    case TypeCode.Int64:
                    {
                        if (isArray)
                            Lines.Add(TAB($"data.setBigInt64(offset, {param.Name}[n], true); offset += 8;", 2));
                        else
                            Lines.Add(TAB($"data.setBigInt64(offset, {param.Name}, true); offset += 8;", 1));
                        break;
                    }
                    case TypeCode.UInt16:
                    {
                        if (isArray)
                            Lines.Add(TAB($"data.setUint16(offset, {param.Name}[n], true); offset += 2;", 2));
                        else
                            Lines.Add(TAB($"data.setUint16(offset, {param.Name}, true); offset += 2;", 1));
                        break;
                    }
                    case TypeCode.UInt32:
                    {
                        if (isArray)
                            Lines.Add(TAB($"data.setUint32(offset, {param.Name}[n], true); offset += 4;", 2));
                        else
                            Lines.Add(TAB($"data.setUint32(offset, {param.Name}, true); offset += 4;", 1));
                        break;
                    }
                    case TypeCode.UInt64:
                    {
                        if (isArray)
                            Lines.Add(TAB($"data.setBigUint64(offset, {param.Name}[n], true); offset += 8;", 2));
                        else
                            Lines.Add(TAB($"data.setBigUint64(offset, {param.Name}, true); offset += 8;", 1));
                        break;
                    }
                    case TypeCode.String:
                    {
                        if (isArray)
                            Lines.Add(TAB($"offset += EncodeString({param.Name}[n], OpCode, offset);", 2));
                        else
                            Lines.Add(TAB($"offset += EncodeString({param.Name}, OpCode, offset);", 1));
                        break;
                    }
                }

                if(isArray)
                    Lines.Add(TAB("}"));
                Lines.Add("");
            }

            // return reflected event data
            Lines.Add("");
            if (Properties.HasFlag(CamerafyEventProperty.NoReponse))
            {
                Lines.Add(TAB("return {id: \"\" /* AWAITS NO RESPONSE */, data: new Uint8Array(OpCode) };"));
            }
            else
            {
                Lines.Add(TAB("return {id: messageId, data: new Uint8Array(OpCode) };"));
            }
            Lines.Add("},");

            return true;
        }

        /// <summary>
        /// https://developer.mozilla.org/en-US/docs/Web/JavaScript/Data_structures
        /// </summary>
        /// <param name="InParams"></param>
        /// <returns></returns>
        private static string DetermineRequiredArrayBufferSize(ParameterInfo[] InParams)
        {
            List<string> Terms = new List<string>();

            foreach (var param in InParams)
            {
                bool isArray = param.ParameterType.IsArray;
                Type type = isArray ? param.ParameterType.GetElementType() : param.ParameterType;

                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    {
                        if (isArray)
                            Terms.Add(TAB($"(4 + {param.Name}.length)"));
                        else
                            Terms.Add("1");
                        break;
                    }
                    case TypeCode.Char: // UTF-16 (2 bytes)
                    {
                        if (isArray)
                            Terms.Add(TAB($"(4 + ({param.Name}.length * 2))"));
                        else
                            Terms.Add("2");
                        break;
                    }
                    case TypeCode.Single:
                    {
                        if (isArray)
                            Terms.Add(TAB($"(4 + ({param.Name}.length * 4))"));
                        else
                            Terms.Add("4");
                        break;
                    }
                    case TypeCode.Double:
                    {
                        if (isArray)
                            Terms.Add(TAB($"(4 + ({param.Name}.length) * 8)"));
                        else
                            Terms.Add("8");
                        break;
                    }
                    case TypeCode.UInt16:
                    case TypeCode.Int16:
                    {
                        if (isArray)
                            Terms.Add(TAB($"(4 + ({param.Name}.length * 2))"));
                        else
                            Terms.Add("2");
                        break;
                    }
                    case TypeCode.UInt32:
                    case TypeCode.Int32:
                    {
                        if (isArray)
                            Terms.Add(TAB($"(4 + ({param.Name}.length * 4))"));
                        else
                            Terms.Add("4");
                        break;
                    }
                    case TypeCode.UInt64:
                    case TypeCode.Int64:
                    {
                        if (isArray)
                            Terms.Add(TAB($"(4 + ({param.Name}.length * 8))"));
                        else
                            Terms.Add("8");
                        break;
                    }
                    case TypeCode.String:
                    {
                        if (isArray)
                            Terms.Add(TAB($"(4 + GetArrayOfStringByteCount({param.Name}))"));
                        else
                            Terms.Add($"(4 + {param.Name}.length)");
                        break;
                    }
                }
            }

            return Terms.Count > 0 ? " + " + String.Join(" + ", Terms) : "";
        }

        private static void CompileCodes(Namespace InModule, StringBuilder js, int indent, bool root = false)
        {
            if (root)
            {
                js.AppendLine(TAB($"get {InModule.Name}()", indent));
                js.AppendLine(TAB("{", indent));

                indent++;
                js.AppendLine(TAB("return {", indent));
            }
            else
            {
                js.AppendLine(TAB($"{InModule.Name}:", indent));
                js.AppendLine(TAB("{", indent));
            }

            foreach (var code in InModule.Codes)
            {
                foreach (var line in code) { js.AppendLine(TAB(line, indent + 1)); }
                js.AppendLine();
            }

            foreach (var module in InModule.Child)
            {
                JSCompiler.CompileCodes(module, js, indent + 1);
            }

            if (root)
            {
                js.AppendLine(TAB("}", indent));

                indent--;
                js.AppendLine(TAB("}", indent));
            }
            else
            {
                js.AppendLine(TAB("},", indent));
            }

            js.AppendLine();
        }

        private static string EncodeParameter(ParameterInfo InParam)
        {
            bool isArray = InParam.ParameterType.IsArray;

            switch (Type.GetTypeCode(InParam.ParameterType))
            {
                case TypeCode.Boolean:
                    return isArray ? $"{InParam.Name} : Array<boolean>" : $"{InParam.Name} : boolean";
                case TypeCode.Char:
                case TypeCode.String:
                    return isArray ? $"{InParam.Name} : Array<string>" : $"{InParam.Name} : string";
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return isArray ? $"{InParam.Name} : Array<number>" : $"{InParam.Name} : number";
            }

            return isArray ? $"{InParam.Name} : Array<any>" : $"{InParam.Name} : any";
        }

        private static string TAB(string InString, int count = 1)
        {
            return $"{new String('\t', count)}{InString}";
        }
    }
}
