using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Punk.Hotsy
{
    public class HotBuilder
    {
        public Result<Assembly> Build(BuildContext context, List<string> sourceFiles)
        {
            var provider = new Microsoft.CSharp.CSharpCodeProvider();
            var parameters = new CompilerParameters
            {
                GenerateInMemory = context.GenerateInMemory,
                OutputAssembly = context.OutputAssembly
            };

            foreach (var reference in context.References)
            {
                parameters.ReferencedAssemblies.Add(reference);
            }

            CompilerResults results = provider.CompileAssemblyFromFile(parameters, sourceFiles.ToArray());

            if (results.Errors.HasErrors)
            {
                return Result<Assembly>.Failure("Compilation failed: " + string.Join(", ", results.Errors));
            }

            return Result<Assembly>.Success(results.CompiledAssembly);
        }
    }
}

using System.Collections.Generic;

namespace Punk.Hotsy
{
    public class BuildContext
    {
        public bool GenerateInMemory { get; set; } = true;
        public string OutputAssembly { get; set; }
        public List<string> References { get; set; } = new List<string>();
    }
}
