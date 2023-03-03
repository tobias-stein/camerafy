using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Camerafy.Event
{
    public static class EventLibrary
    {
        public struct ClientEventHandler
        {
            public delegate Task<object> CallbackTarget(object instnace, byte[] OpCode);

            public CallbackTarget  Target;

            /// <summary>
            /// If true, event bus should not send a response.
            /// </summary>
            public bool            NoResponse;
        }

        public struct ServerEventHandler
        {
            public delegate byte[] CallbackTarget(params object[] Params);

            public CallbackTarget   Target;
        }

        /// <summary>
        /// Returns all client to server specific events.
        /// </summary>
        internal static Dictionary<string, ClientEventHandler> ClientEvents { get; private set; } = new Dictionary<string, ClientEventHandler>();

        /// <summary>
        /// Returns all server to client specific events.
        /// </summary>
        internal static Dictionary<string, ServerEventHandler> ServerEvents { get; private set; } = new Dictionary<string, ServerEventHandler>();
        
        /// <summary>
        /// Checks the entire Assembly domain for CamerafyCommand annotated methods. For each method
        /// a new proxy method will be created, which will parse an byte[] to get all parameters and
        /// invoke the target method with a provided object instance.
        /// </summary>
        public static void Build()
        {
            EventLibrary.ClientEvents.Clear();
            EventLibrary.ServerEvents.Clear();

            // Get the Camerafy assembly
            Assembly CamerafyAssembly = Assembly.GetAssembly(typeof(Application.Application));

            // Get all exported classes
            Type[] Exports = CamerafyAssembly.GetExportedTypes().Where(x => x.IsClass).ToArray();

            // Get all camerafy commands
            List<MethodInfo> CamerafyEvents = new List<MethodInfo>();
            foreach (var e in Exports)
            {
                CamerafyEvents.AddRange(EventLibrary.GetCamerafyEventHandler(e));
            }

            // Sanity check
            if (EventLibrary.CheckCollision(CamerafyEvents))
                throw new Exception();

            foreach (var e in CamerafyEvents)
            {
                CamerafyEventAttribute CamerafyEvent = e.GetCustomAttribute<CamerafyEventAttribute>();
                ParameterInfo[] ParamsInfo = e.GetParameters();

                // get the unique method address 
                string MethodAddress = EventLibrary.CalculateMethodAddress(e);

                // Client to server (C2S) ...
                // note: C2S event handler will decode an byte array and restore method parameters.
                if (CamerafyEvent.Properties.HasFlag(CamerafyEventProperty.Client))
                {
                    // generate the method implementation
                    ClientEventHandler.CallbackTarget CallbackTarget = async delegate (object instance, byte[] OpCode)
                    {
                        int Offset = 68; // skip first 68 bytes as they contain: messageid and method address id
                        List<object> Params = new List<object>();

                        foreach (var param in ParamsInfo)
                        {
                            bool isArray = param.ParameterType.IsArray;
                            Type type = isArray ? param.ParameterType.GetElementType() : param.ParameterType;

                            int N = 1;

                            // if this is an array, read its capacity first
                            if (isArray)
                            {
                                N = BitConverter.ToInt32(OpCode, Offset);
                                Offset += 4;
                            }

                            List<object> ElementBuffer = new List<object>(N);
                            for (int n = 0; n < N; ++n)
                            {
                                switch (Type.GetTypeCode(type))
                                {
                                    case TypeCode.Boolean:
                                        ElementBuffer.Add(BitConverter.ToBoolean(OpCode, Offset));
                                        Offset += 1;
                                        break;
                                    case TypeCode.Byte:
                                    case TypeCode.SByte:
                                        ElementBuffer.Add(OpCode[Offset]);
                                        Offset += 1;
                                        break;
                                    case TypeCode.Char: // UTF-16 (2 bytes)
                                        ElementBuffer.Add(BitConverter.ToChar(OpCode, Offset));
                                        Offset += 2;
                                        break;
                                    case TypeCode.Single:
                                        ElementBuffer.Add(BitConverter.ToSingle(OpCode, Offset));
                                        Offset += 4;
                                        break;
                                    case TypeCode.Double:
                                        ElementBuffer.Add(BitConverter.ToDouble(OpCode, Offset));
                                        Offset += 8;
                                        break;
                                    case TypeCode.Int16:
                                        ElementBuffer.Add(BitConverter.ToInt16(OpCode, Offset));
                                        Offset += 2;
                                        break;
                                    case TypeCode.Int32:
                                        ElementBuffer.Add(BitConverter.ToInt32(OpCode, Offset));
                                        Offset += 4;
                                        break;
                                    case TypeCode.Int64:
                                        ElementBuffer.Add(BitConverter.ToInt64(OpCode, Offset));
                                        Offset += 8;
                                        break;
                                    case TypeCode.UInt16:
                                        ElementBuffer.Add(BitConverter.ToUInt16(OpCode, Offset));
                                        Offset += 2;
                                        break;
                                    case TypeCode.UInt32:
                                        ElementBuffer.Add(BitConverter.ToUInt32(OpCode, Offset));
                                        Offset += 4;
                                        break;
                                    case TypeCode.UInt64:
                                        ElementBuffer.Add(BitConverter.ToUInt64(OpCode, Offset));
                                        Offset += 8;
                                        break;
                                    case TypeCode.String:
                                        // read string length
                                        int StringLength = BitConverter.ToInt32(OpCode, Offset);
                                        Offset += 4;
                                        ElementBuffer.Add(Encoding.UTF8.GetString(OpCode, Offset, StringLength));
                                        Offset += StringLength;
                                        break;
                                    default:
                                        Logger.Fatal("Method '{0}.{1}' parameter '{2}' type ('{3}') not supported.", e.DeclaringType.FullName, e.Name, param.Name, type.Name);
                                        throw new Exception();
                                }
                            }

                            if (isArray)
                            {
                                switch (Type.GetTypeCode(type))
                                {
                                    case TypeCode.Boolean: Params.Add(ElementBuffer.Cast<bool>().ToArray()); break;
                                    case TypeCode.Byte:
                                    case TypeCode.SByte: Params.Add(ElementBuffer.Cast<byte>().ToArray()); break;
                                    case TypeCode.Char: Params.Add(ElementBuffer.Cast<char>().ToArray()); break;
                                    case TypeCode.Single: Params.Add(ElementBuffer.Cast<float>().ToArray()); break;
                                    case TypeCode.Double: Params.Add(ElementBuffer.Cast<double>().ToArray()); break;
                                    case TypeCode.Int16: Params.Add(ElementBuffer.Cast<short>().ToArray()); break;
                                    case TypeCode.Int32: Params.Add(ElementBuffer.Cast<int>().ToArray()); break;
                                    case TypeCode.Int64: Params.Add(ElementBuffer.Cast<long>().ToArray()); break;
                                    case TypeCode.UInt16: Params.Add(ElementBuffer.Cast<ushort>().ToArray()); break;
                                    case TypeCode.UInt32: Params.Add(ElementBuffer.Cast<uint>().ToArray()); break;
                                    case TypeCode.UInt64: Params.Add(ElementBuffer.Cast<ulong>().ToArray()); break;
                                    case TypeCode.String: Params.Add(ElementBuffer.Cast<string>().ToArray()); break;
                                }
                            }
                            else
                            {
                                Params.Add(ElementBuffer[0]);
                            }
                        }

                        // Try to invoke the method with parsed commands. If method is async await its return. Return values will be 
                        // converted to json string.
                        try
                        {
                            bool IsAsync = e.GetCustomAttribute<System.Runtime.CompilerServices.AsyncStateMachineAttribute>() != null;
                            if (e.ReturnType == typeof(void) || e.ReturnType == typeof(Task))
                            {
                                if (IsAsync)
                                {
                                    await (Task)e.Invoke(instance, Params.ToArray());
                                }
                                else
                                {
                                    e.Invoke(instance, Params.ToArray());
                                }
                                return null;
                            }
                            else
                            {
                                if (IsAsync)
                                {
                                    Task task = (Task)e.Invoke(instance, Params.ToArray());
                                    await task.ConfigureAwait(false);
                                    object result = (object)((dynamic)task).Result;
                                    return result;
                                }
                                else
                                {
                                    object result = e.Invoke(instance, Params.ToArray());
                                    return result;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex.Message);
                            throw ex;
                        }
                    };

                    // store method implementation for the camerafy command
                    EventLibrary.ClientEvents.Add(
                        MethodAddress,
                        new ClientEventHandler { Target = CallbackTarget, NoResponse = CamerafyEvent.Properties.HasFlag(CamerafyEventProperty.NoReponse) });
                }
                // Server to client (S2C) ...
                // note S2C event handler will encode method parameters into a byte array.
                else if (CamerafyEvent.Properties.HasFlag(CamerafyEventProperty.Server))
                {
                    ServerEventHandler.CallbackTarget CallbackTarget = delegate (object[] InParam)
                    {
                        List<byte> OutData = new List<byte>();
                        // convert all input paramter to bytes
                        foreach (var param in InParam)
                        {
                            Type paramType = param.GetType();
                            bool isArray = paramType.IsArray;
                            Type type = isArray ? paramType.GetElementType() : paramType;

                            int N = 1;

                            // if this is an array, write its capacity first
                            if (isArray)
                            {
                                int n = Array.AsReadOnly<object>(param as object[]).Count;
                                OutData.AddRange(BitConverter.GetBytes(n));
                            }

                            for (int n = 0; n < N; ++n)
                            {
                                switch (Type.GetTypeCode(type))
                                {
                                    case TypeCode.Boolean:
                                        OutData.AddRange(BitConverter.GetBytes((bool)param));
                                        break;
                                    case TypeCode.Byte:
                                        OutData.AddRange(BitConverter.GetBytes((byte)param));
                                        break;
                                    case TypeCode.SByte:
                                        OutData.AddRange(BitConverter.GetBytes((byte)param));
                                        break;
                                    case TypeCode.Single:
                                        OutData.AddRange(BitConverter.GetBytes((float)param));
                                        break;
                                    case TypeCode.Double:
                                        OutData.AddRange(BitConverter.GetBytes((double)param));
                                        break;
                                    case TypeCode.Int16:
                                        OutData.AddRange(BitConverter.GetBytes((Int16)param));
                                        break;
                                    case TypeCode.Int32:
                                        OutData.AddRange(BitConverter.GetBytes((Int32)param));
                                        break;
                                    case TypeCode.Int64:
                                        OutData.AddRange(BitConverter.GetBytes((Int64)param));
                                        break;
                                    case TypeCode.UInt16:
                                        OutData.AddRange(BitConverter.GetBytes((UInt16)param));
                                        break;
                                    case TypeCode.UInt32:
                                        OutData.AddRange(BitConverter.GetBytes((UInt32)param));
                                        break;
                                    case TypeCode.UInt64:
                                        OutData.AddRange(BitConverter.GetBytes((UInt64)param));
                                        break;
                                    case TypeCode.String:
                                        // read string length
                                        OutData.AddRange(BitConverter.GetBytes(Encoding.UTF8.GetByteCount((string)param)));
                                        OutData.AddRange(Encoding.UTF8.GetBytes((string)param));
                                        break;
                                    default:
                                        Logger.Fatal($"Cannot serialize paramter of type '{Type.GetTypeCode(type)}'.");
                                        throw new Exception();
                                }
                            }
                        }

                        return OutData.ToArray();
                    };

                    // store method implementation for the camerafy command
                    EventLibrary.ServerEvents.Add(
                        MethodAddress, 
                        new ServerEventHandler { Target = CallbackTarget });
                }
            }
        }
     
        #region UTILITY METHODS

        /// <summary>
        /// Get all CamerafyEvent annotated methods from class.
        /// </summary>
        /// <param name="InClass"></param>
        /// <returns></returns>
        public static List<MethodInfo> GetCamerafyEventHandler(Type InClass)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
            return InClass.GetMethods(flags).Where(x => x.GetCustomAttribute(typeof(CamerafyEventAttribute)) != null).ToList();
        }

        /// <summary>
        /// Returns a unique method identifier.
        /// </summary>
        /// <param name="InMethod"></param>
        /// <returns></returns>
        public static string CalculateMethodAddress(MethodBase InMethod)
        {
            string AccumulatedName = string.Format("{0}.{1}{2}{3}",
                InMethod.DeclaringType.FullName,
                InMethod.Name,
                InMethod.GetParameters().Length > 0 ? "." : "",
                String.Join(".", InMethod.GetParameters().Select(x => x.ParameterType.Name)));

            StringBuilder sBuilder = new StringBuilder();
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(AccumulatedName));
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        /// <summary>
        /// Check if we cause any ambigouties when encoding method signatures.
        /// </summary>
        /// <param name="InMethods"></param>
        /// <returns></returns>
        public static bool CheckCollision(List<MethodInfo> InMethods)
        {
            Dictionary<string, MethodInfo> MD5s = new Dictionary<string, MethodInfo>();

            foreach (var m in InMethods)
            {
                string md5 = EventLibrary.CalculateMethodAddress(m);
                if (MD5s.ContainsKey(md5))
                {
                    MethodInfo other = MD5s[md5];

                    UnityEngine.Debug.LogWarningFormat("Collision detected between '{0}.{1}' and '{2}.{3}'", m.DeclaringType.FullName, m.Name, other.DeclaringType.FullName, other.Name);

                    //Collision!
                    return true;
                }
                else
                {
                    MD5s.Add(md5, m);
                }
            }

            return false;
        }


        #endregion
    }
}
