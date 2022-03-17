using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MqttServerClient.Common
{
    [Obsolete("No C# Interpreter on dot net core")]
    public class CSharpCodeEval
    {
        public readonly string[] DefaultCompilerReferences = new[]
        {
            "System.dll",
            "System.Core.dll"
        };

        public readonly string[] DefaultUsings = new[]
{
            "using System;",
            "using System.Linq;",
        };

        public string[] UserCompilerReferences = new string[0];
        public string[] UserUsings = new string[0];

        private const string k_UniqueReplaceable = "unique_inside_198745132_replaceable";
        private const string k_MethodWrapper = @"
        public class BinaryFunction
        {                
            public static string Function()
            {
                unique_inside_198745132_replaceable
            }
        }
    ";

        public Func<string> CreateMethod(string function)
        {
            /*var finalCode = k_MethodWrapper.Replace(k_UniqueReplaceable, function);
            AddUsings(ref finalCode);

            var compilerParams = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true
            };

            AddReferences(compilerParams);

            var results = new CSharpCodeProvider().CompileAssemblyFromSource(compilerParams, finalCode);

            if (results.Errors.HasErrors)
            {
                var result = "Compilation Failed:";
                foreach (CompilerError error in results.Errors)
                {
                    var line1 = string.Format("({0}): {1}", error.ErrorNumber, error.ErrorText);
                    var line2 = string.Format("at {0} {1} : {2}", error.FileName, error.Line, error.Column);
                    Debug.WriteLine(line1);
                    Debug.WriteLine(line2);
                    result += Environment.NewLine + line1 + Environment.NewLine + line2;
                }
                Debug.WriteLine("Scripts have compilation errors.");
                return () => { return result; };
            }

            var binaryFunction = results.CompiledAssembly.GetType("BinaryFunction");
            var methodInfo = binaryFunction.GetMethod("Function");
            return (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), methodInfo);*/
            return () => "";
        }
        /*
        private void AddReferences(CompilerParameters CompilerParams)
        {
            var refs = new HashSet<string>();
            foreach (var r in DefaultCompilerReferences)
                refs.Add(r);
            foreach (var r in UserCompilerReferences)
                refs.Add(r);

            CompilerParams.ReferencedAssemblies.AddRange(refs.ToArray());
        }*/

        private void AddUsings(ref string code)
        {
            var refs = new HashSet<string>();
            foreach (var r in DefaultUsings)
                refs.Add(r);
            foreach (var r in UserUsings)
                refs.Add(r);

            var allUsings = string.Join("\n", refs.ToArray());
            code = allUsings + "\n" + code;
        }
    }
}
