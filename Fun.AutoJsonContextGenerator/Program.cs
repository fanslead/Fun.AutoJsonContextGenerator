using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Text;

namespace Fun.AutoJsonContextGenerator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        try
        {
            // ===== 1. 环境变量检测（进程级防递归） =====
            if (Environment.GetEnvironmentVariable("AUTOJSON_RUNNING") == "true")
            {
                Console.WriteLine("[AutoJson] Recursive invocation detected via env var, skipping.");
                return 0; // 返回成功，不阻塞编译
            }
            Environment.SetEnvironmentVariable("AUTOJSON_RUNNING", "true");

            // ===== 2. 锁文件检测（跨进程防递归） =====
            string outputDir = args.Length > 2 ? args[2] : Path.GetTempPath();
            if (string.IsNullOrWhiteSpace(outputDir))
                outputDir = Path.GetTempPath();

            string lockFile = Path.Combine(outputDir, "AutoJson.lock");

            if (File.Exists(lockFile))
            {
                Console.WriteLine("[AutoJson] Recursive invocation detected via lock file, skipping.");
                return 0; // 返回成功
            }

            // 创建锁文件
            Directory.CreateDirectory(outputDir);
            File.WriteAllText(lockFile, DateTime.UtcNow.ToString("O"));

            try
            {
                // ===== 3. 生成器核心逻辑 =====
                Console.WriteLine("[AutoJson] Generator started.");
                await RunGenerator(args);
                Console.WriteLine("[AutoJson] Generator finished.");
            }
            finally
            {
                // ===== 4. 清理锁文件 =====
                try
                {
                    if (File.Exists(lockFile))
                        File.Delete(lockFile);
                }
                catch
                {
                    // 忽略锁文件清理失败
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[AutoJson] Error: " + ex);
            return 1; // 返回失败
        }
    }

    static async Task<int> RunGenerator(string[] args)
    {
        try
        {
            var csprojPath = args[0];
            var rootNamespace = args[1];
            var outputDir = args[2].Trim().Trim('"').TrimEnd('\\', '/');
            if (args.Length < 3)
            {
                Console.Error.WriteLine("用法: Fun.AutoJsonContextGenerator <项目路径.csproj> <RootNamespace> <输出目录>");
                return 1;
            }


            if (!MSBuildLocator.IsRegistered)
            {
                try
                {
                    // 优先用 Visual Studio / 全局 MSBuild
                    MSBuildLocator.RegisterDefaults();
                    Console.WriteLine("✅ 使用 Visual Studio / 全局 MSBuild");
                }
                catch
                {
                    // 用 dotnet --info 获取 SDK 路径
                    var psi = new System.Diagnostics.ProcessStartInfo("dotnet", "--info")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    };
                    var proc = System.Diagnostics.Process.Start(psi);
                    var output = proc!.StandardOutput.ReadToEnd();
                    proc.WaitForExit();

                    var sdkBasePath = output
                        .Split('\n')
                        .Select(l => l.Trim())
                        .FirstOrDefault(l => l.StartsWith("Base Path", StringComparison.OrdinalIgnoreCase))
                        ?.Split(':', 2)[1]
                        .Trim();

                    if (string.IsNullOrEmpty(sdkBasePath) || !Directory.Exists(sdkBasePath))
                        throw new DirectoryNotFoundException($".NET SDK Base Path 未找到: {sdkBasePath}");

                    MSBuildLocator.RegisterMSBuildPath(sdkBasePath);
                    Console.WriteLine($"✅ 使用 .NET SDK MSBuild: {sdkBasePath}");
                }
            }

            var projectDir = Path.GetDirectoryName(csprojPath) ?? Directory.GetCurrentDirectory();

            var config = GeneratorConfig.Load(projectDir);

            using var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(csprojPath);
            var compilation = await project.GetCompilationAsync();

            var autoJsonAttrSymbol = compilation!.GetTypeByMetadataName($"{rootNamespace}.AutoJsonSerializableAttribute")
                ?? compilation.GetTypeByMetadataName("Fun.AutoJsonContextGenerator.AutoJsonSerializableAttribute");

            if (autoJsonAttrSymbol == null)
            {
                Console.Error.WriteLine("❌ 未找到 AutoJsonSerializableAttribute");
                return 1;
            }

            // 优化的类型筛选逻辑
            var targetTypes = compilation.GlobalNamespace.GetAllTypes()
                .Where(type => IsValidTargetType(type, autoJsonAttrSymbol, config))
                .ToHashSet(SymbolEqualityComparer.Default);

            var sb = new StringBuilder();
            sb.AppendLine("using System.Text.Json.Serialization;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {rootNamespace}");
            sb.AppendLine("{");

            foreach (var type in targetTypes.OrderBy(t => t!.Name))
            {
                var typeName = type!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                foreach (var template in config.CollectionTemplates)
                {
                    sb.AppendLine($"    [JsonSerializable(typeof({template.Replace("{0}", typeName)}))]");
                }
            }

            sb.AppendLine("    internal partial class AutoJsonContext : JsonSerializerContext");
            sb.AppendLine("    {");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            Directory.CreateDirectory(outputDir);
            var outputPath = Path.Combine(outputDir, "AutoJsonContext.g.cs");

            var newContent = sb.ToString();

            if (File.Exists(outputPath))
            {
                var oldContent = File.ReadAllText(outputPath);
                if (oldContent == newContent)
                {
                    Console.WriteLine("No changes detected. Skipping write.");
                    return 0;
                }
            }

            File.WriteAllText(outputPath, newContent, Encoding.UTF8);

            Console.WriteLine($"✅ 已生成 {outputPath}，共 {targetTypes.Count} 个类型。");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("❌ 生成器运行失败：" + ex);
            return -1;
        }
    }

    static bool HasAutoJsonAttribute(INamedTypeSymbol type, INamedTypeSymbol attrSymbol)
    {
        return type.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attrSymbol));
    }

    static bool InheritsOrImplementsWithAttribute(INamedTypeSymbol type, INamedTypeSymbol attrSymbol)
    {
        foreach (var baseType in type.AllInterfaces.Concat(GetBaseTypes(type)))
        {
            if (HasAutoJsonAttribute(baseType, attrSymbol))
                return true;
        }
        return false;
    }

    static IEnumerable<INamedTypeSymbol> GetBaseTypes(INamedTypeSymbol type)
    {
        var current = type.BaseType;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }

    static bool IsValidTargetType(INamedTypeSymbol type, INamedTypeSymbol autoJsonAttrSymbol, GeneratorConfig config)
    {
        // 只处理类和结构体
        if (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct)
            return false;

        // 检查命名空间过滤
        if (config.Namespaces.Count != 0 && !config.Namespaces.Contains(type.ContainingNamespace.ToDisplayString()))
            return false;

        // 检查是否有AutoJson特性或继承/实现了带有该特性的类型
        return HasAutoJsonAttribute(type, autoJsonAttrSymbol) ||
               (config.IncludeBaseTypes && InheritsOrImplementsWithAttribute(type, autoJsonAttrSymbol));
    }
}