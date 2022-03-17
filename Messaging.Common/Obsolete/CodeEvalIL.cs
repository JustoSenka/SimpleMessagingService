using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace MqttServerClient.Common
{
    [Obsolete("Not yet implemented")]
    public static class CodeEvalIL
    {
        /*
        public static byte[] LambdaToBytes<TResult>(Func<TResult> lambda)
        {
            var method = lambda.Method;
            oldMethodInfo = method; // remembering info
            return method.GetMethodBody().GetILAsByteArray();
        }*/

        /*
        static MethodInfo oldMethodInfo;
        public static Func<T1, T2, TResult> CreateSumMethod<T1, T2, TResult>()
        {
            var domain = AppDomain.CurrentDomain;
            var assembly = new AssemblyName("MyDynamicAsm");
            var assemblyBuilder = domain.DefineDynamicAssembly(assembly, AssemblyBuilderAccess.RunAndSave);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MyDynamicAsm");
            var typeBuilder = moduleBuilder.DefineType("MyDynamicType", TypeAttributes.Public);

            var methodName = "name";

            AddSumMethod(typeBuilder, methodName, new Type[] { typeof(T1), typeof(T2) }, typeof(TResult));

            var type = typeBuilder.CreateType();

            var method = type.GetMethod(methodName);
            var func = (Func<T1, T2, TResult>)Delegate.CreateDelegate(typeof(Func<T1, T2, TResult>), method);

            return func;
        }
        public static void AddSumMethod(TypeBuilder myTypeBld, string mthdName, Type[] mthdParams, Type returnType)
        {
            MethodBuilder myMthdBld = myTypeBld.DefineMethod(mthdName, MethodAttributes.Public | MethodAttributes.Static, returnType, mthdParams);

            ILGenerator ilg = myMthdBld.GetILGenerator();
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldarg_1);
            ilg.Emit(OpCodes.Add);
            ilg.Emit(OpCodes.Ret);
        }

        public static Func<TResult> CreateMethodFromBytes<TResult>(byte[] bytes)
        {
            var domain = AppDomain.CurrentDomain;
            var assembly = new AssemblyName("MyDynamicAsm");
            var assemblyBuilder = domain.DefineDynamicAssembly(assembly, AssemblyBuilderAccess.RunAndSave);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MyDynamicAsm");
            var typeBuilder = moduleBuilder.DefineType("MyDynamicType", TypeAttributes.Public);

            var methodName = "name";

            AddMethodDynamically(typeBuilder, methodName, new Type[0], typeof(TResult), bytes);

            var type = typeBuilder.CreateType();

            var method = type.GetMethod(methodName);
            var func = (Func<TResult>)Delegate.CreateDelegate(typeof(Func<TResult>), method);

            return func;
        }

        public static Func<T1, T2, TResult> CreateMethodFromBytes<T1, T2, TResult>(byte[] bytes)
        {
            var domain = AppDomain.CurrentDomain;
            var assembly = new AssemblyName("MyDynamicAsm");
            var assemblyBuilder = domain.DefineDynamicAssembly(assembly, AssemblyBuilderAccess.RunAndSave);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MyDynamicAsm");
            var typeBuilder = moduleBuilder.DefineType("MyDynamicType", TypeAttributes.Public);

            var methodName = "name";

            AddMethodDynamically(typeBuilder, methodName, new Type[] { typeof(T1), typeof(T2) }, typeof(TResult), bytes);

            var type = typeBuilder.CreateType();

            var method = type.GetMethod(methodName);
            var func = (Func<T1, T2, TResult>)Delegate.CreateDelegate(typeof(Func<T1, T2, TResult>), method);

            return func;
        }
        public static void AddMethodDynamically(TypeBuilder myTypeBld, string mthdName, Type[] mthdParams, Type returnType, byte[] methodBodyIL)
        {
            var methodBuilder = myTypeBld.DefineMethod(mthdName, MethodAttributes.Public | MethodAttributes.Static, 
                CallingConventions.Standard, returnType, mthdParams);
            methodBuilder.CreateMethodBody(methodBodyIL, methodBodyIL.Length);
        }

        */
        /*
        public static Func<int, int, int> CreatDynamiMethodSum(byte[] bytes)
        {
            var m = new DynamicMethod("Nam", typeof(int), new[] { typeof(int), typeof(int) }, typeof(CodeEvalIL).Module);
            
            var ilg = m.GetILGenerator();
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldarg_1);
            ilg.Emit(OpCodes.Add);
            ilg.Emit(OpCodes.Ret);

            var del =  m.CreateDelegate(typeof(Func<int, int, int>)) as Func<int, int, int>;
            return del;
        }
        */

        /*
        public static void AddMethodDynamically(TypeBuilder myTypeBld, MethodInfo info, byte[] methodBodyIL)
        {
            var methodBuilder = myTypeBld.DefineMethod(info.Name, info.Attributes, info.CallingConvention, info.ReturnType, info.GetParameters().Select(x => x.ParameterType).ToArray());
            methodBuilder.CreateMethodBody(methodBodyIL, methodBodyIL.Length);
        }*/

            /*

        // OLD STUFF
        public static R CopyMethod<T, R>(Func<T, R> f, T t)
        {

            AppDomain currentDom = AppDomain.CurrentDomain;
            AssemblyName asm = new AssemblyName("DynamicAssembly");
            AssemblyBuilder abl = currentDom.DefineDynamicAssembly(asm, AssemblyBuilderAccess.Run);
            ModuleBuilder mbl = abl.DefineDynamicModule("Module");
            TypeBuilder tbl = mbl.DefineType("Type");
            MethodInfo info = f.GetMethodInfo();
            MethodBuilder mtbl = tbl.DefineMethod(info.Name, info.Attributes, info.CallingConvention, info.ReturnType, info.GetParameters().Select(x => x.ParameterType).ToArray());
            MethodBody mb = f.Method.GetMethodBody();
            byte[] il = mb.GetILAsByteArray();
            ILGenerator ilg = mtbl.GetILGenerator();
            foreach (var local in mb.LocalVariables)
                ilg.DeclareLocal(local.LocalType);

            var opCodes = GetOpCodes(il);
            for (int i = 0; i < opCodes.Length; ++i)
            {
                if (!opCodes[i].code.HasValue)
                    continue;
                OpCode opCode = opCodes[i].code.Value;
                if (opCode.OperandType == OperandType.InlineBrTarget)
                {
                    ilg.Emit(opCode, BitConverter.ToInt32(il, i + 1));
                    i += 4;
                    continue;
                }
                if (opCode.OperandType == OperandType.ShortInlineBrTarget)
                {
                    ilg.Emit(opCode, il[i + 1]);
                    ++i;
                    continue;
                }
                if (opCode.OperandType == OperandType.InlineType)
                {
                    Type tp = info.Module.ResolveType(BitConverter.ToInt32(il, i + 1), info.DeclaringType.GetGenericArguments(), info.GetGenericArguments());
                    ilg.Emit(opCode, tp);
                    i += 4;
                    continue;
                }
                if (opCode.FlowControl == FlowControl.Call)
                {
                    MethodInfo mi = info.Module.ResolveMethod(BitConverter.ToInt32(il, i + 1)) as MethodInfo;
                    if (mi == info)
                        ilg.Emit(opCode, mtbl);
                    else
                        ilg.Emit(opCode, mi);
                    i += 4;
                    continue;
                }
                ilg.Emit(opCode);
            }

            Type type = tbl.CreateType();
            Func<T, R> method = type.GetMethod(info.Name).CreateDelegate(typeof(Func<T, R>)) as Func<T, R>;
            return method(t);
        }

        public static OpCodeContainer[] GetOpCodes(this byte[] data)
        {
            List<OpCodeContainer> opCodes = new List<OpCodeContainer>();
            foreach (byte opCodeByte in data)
                opCodes.Add(new OpCodeContainer(opCodeByte));
            return opCodes.ToArray();
        }

        public class OpCodeContainer
        {
            public OpCode? code;
            byte data;

            public OpCodeContainer(byte opCode)
            {
                data = opCode;
                try
                {
                    code = (OpCode)typeof(OpCodes).GetFields().First(t => ((OpCode)(t.GetValue(null))).Value == opCode).GetValue(null);
                }
                catch { }
            }

            public override string ToString()
            {
                return string.Format("{0, -16}{1}", code, data);
            }
        }
        */
    }
}
