using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using System.IO;
using System.Linq;

[InitializeOnLoad]
public class CompileErrorLogger
{
    static CompileErrorLogger()
    {
        CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
    }

    private static void OnCompilationFinished(string assemblyPath, CompilerMessage[] messages)
    {
        // Only track Assembly-CSharp (User Scripts)
        if (!assemblyPath.Contains("Assembly-CSharp.dll")) return;

        var errors = messages.Where(m => m.type == CompilerMessageType.Error).ToList();
        string logPath = Path.Combine(Directory.GetCurrentDirectory(), "compile_errors.txt");

        if (errors.Count > 0)
        {
            using (StreamWriter writer = new StreamWriter(logPath, false))
            {
                writer.WriteLine($"[Compile Errors] Timestamp: {System.DateTime.Now}");
                foreach (var error in errors)
                {
                    writer.WriteLine($"{error.file}({error.line},{error.column}): error {error.message}");
                }
            }
        }
        else
        {
            // Clear the file if no errors
            if (File.Exists(logPath)) File.Delete(logPath);
        }
    }
}