using Messaging.Common.Utilities;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Messaging.Common.CodeEval
{
    public class RemoteProcedureEval : ICodeEval
    {
        private const BindingFlags k_BindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private const string k_EvalBinariesPath = "EvalBinaries";

        public static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var fullName = args.Name;
            var name = new string(fullName.TakeWhile(c => c != ',').Concat(".dll".ToCharArray()).ToArray());

            var possiblePaths = new[]
            {
                name,
                Path.Combine(k_EvalBinariesPath, name)
            };

            foreach (var p in possiblePaths)
            {
                if (!File.Exists(p))
                    continue;

                var a = Assembly.Load(File.ReadAllBytes(p));
                if (a == null)
                    continue;

                return a;
            }

            return null;
        }

        [Serializable]
        public class DelegateInfo
        {
            [NonSerialized]
            public Type type;

            public string CodeBase;
            public string typeName;
            public string methodName;
            public byte[] args;

            public object Invoke()
            {
                var excMsg = "";
                AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
                MethodInfo methodInfo = null;
                object[] objArgs = null;

                try
                {
                    // First try find type from already loaded assemblies
                    var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes());
                    AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).First(t => t.FullName == typeName);
                    type = types.First(t => t.FullName == typeName);

                    objArgs = BinaryObjectIO.DeserializeFromBytes<object[]>(args);
                    methodInfo = GetMethodFromArgs(type, methodName, objArgs);
                }
                catch (Exception e)
                {
                    try
                    {
                        // If before fails, try to load new assembly from provided path
                        var asmPath = GetAssemblyPath(CodeBase);
                        var asm = Assembly.Load(File.ReadAllBytes(asmPath));
                        type = asm.GetType(typeName);

                        // Deserializing args only after loading assembly, since argument could be of time from there
                        objArgs = BinaryObjectIO.DeserializeFromBytes<object[]>(args);
                        methodInfo = GetMethodFromArgs(type, methodName, objArgs);
                    }
                    catch (Exception ex)
                    {
                        excMsg = ex.Message;
                    }

                    excMsg = e.Message;
                }

                if (type == null || methodInfo == null)
                    return "Could not load type: " + typeName + " at: " + CodeBase + " exception: " + excMsg;

                try
                {
                    return methodInfo.Invoke(null, objArgs);
                }
                finally
                {
                    AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
                }
            }

            private static MethodInfo GetMethodFromArgs(Type type, string methodName, object[] objArgs)
            {
                try
                {
                    var method = type.GetMethod(methodName, k_BindingFlags);
                    if (method == null)
                        throw new Exception();

                    return method;
                }
                catch
                {
                    if (objArgs != null && objArgs.Length > 0)
                    {
                        var types = objArgs.Select(a => a.GetType()).ToArray();
                        return type.GetMethod(methodName, k_BindingFlags, null, types, null);
                    }
                }

                return type.GetMethod(methodName); // Last resort, it's fine if it's null or exception
            }

            private static string GetAssemblyPath(string codeBase)
            {
                // Empty codebase means invoking System methods
                if (string.IsNullOrEmpty(codeBase))
                    return codeBase;

                if (File.Exists(codeBase))
                    return codeBase;

                var p = Path.GetFileName(codeBase);
                if (File.Exists(p))
                    return p;

                p = Path.Combine(k_EvalBinariesPath, p);
                if (File.Exists(p))
                    return p;

                Console.WriteLine(codeBase + " assembly was not found. Cannot load to do RemoteProcedureCall");
                return codeBase;
            }
        }

        public static DelegateInfo GetEvalRequest<T>(Action call, object[] args) => GetEvalRequest(call.Target, call.Method, args);
        public static DelegateInfo GetEvalRequest<T>(Action<T> call, object[] args) => GetEvalRequest(call.Target, call.Method, args);
        public static DelegateInfo GetEvalRequest<T, K>(Action<T, K> call, object[] args) => GetEvalRequest(call.Target, call.Method, args);
        public static DelegateInfo GetEvalRequest<T, K, J>(Action<T, K, J> call, object[] args) => GetEvalRequest(call.Target, call.Method, args);
        public static DelegateInfo GetEvalRequest<T>(Func<T> call, object[] args) => GetEvalRequest(call.Target, call.Method, args);
        public static DelegateInfo GetEvalRequest<T, K>(Func<T, K> call, object[] args) => GetEvalRequest(call.Target, call.Method, args);
        public static DelegateInfo GetEvalRequest<T, K, J>(Func<T, K, J> call, object[] args) => GetEvalRequest(call.Target, call.Method, args);
        public static DelegateInfo GetEvalRequest<T, K, J, L>(Func<T, K, J, L> call, object[] args) => GetEvalRequest(call.Target, call.Method, args);
        public static DelegateInfo GetEvalRequest(Delegate call, object[] args) => GetEvalRequest(call.Target, call.Method, args);

        public static DelegateInfo GetEvalRequest(object target, MethodInfo method, object[] args)
        {
            return GetFunctionCallParcel(target, method, args);
        }

        public static DelegateInfo GetFunctionCallParcel(object target, MethodBase method, object[] args)
        {
            var type = target != null ? target.GetType() : method.DeclaringType;
            if (type == null)
                type = typeof(void);

            var methodCodebasePath = type.Assembly.CodeBase.Replace("file:///", "", true, CultureInfo.InvariantCulture);
            if (methodCodebasePath.Contains("Microsoft.NETCore", StringComparison.InvariantCultureIgnoreCase))
                //If method is from System.something, do not copy binaries from whatever path it gives
                methodCodebasePath = "";

            return new DelegateInfo()
            {
                //Target = target,
                //TargetBytes = new BinaryObjectIO().SerializeToBytes(target),
                CodeBase = methodCodebasePath,
                type = type,
                typeName = type.FullName,
                methodName = method.Name,
                args = BinaryObjectIO.SerializeToBytes(args)
            };
        }

        public static string SerializeToJson(DelegateInfo delegateInfo)
        {
            return JsonConvert.SerializeObject(delegateInfo);
        }

        public static DelegateInfo DeserializeJson(string delegateInfoJson)
        {
            return JsonConvert.DeserializeObject<DelegateInfo>(delegateInfoJson);
        }

        public string Run(string delegateInfoJson)
        {
            var delInfo = DeserializeJson(delegateInfoJson);
            var res = delInfo.Invoke();
            return res == null ? "null" : res.ToString();
        }
    }
}
