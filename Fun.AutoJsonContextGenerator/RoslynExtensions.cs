using Microsoft.CodeAnalysis;

namespace Fun.AutoJsonContextGenerator
{
    public static class RoslynExtensions
    {
        // 递归获取命名空间下所有类型
        public static IEnumerable<INamedTypeSymbol> GetAllTypes(this INamespaceSymbol ns)
        {
            foreach (var member in ns.GetMembers())
            {
                if (member is INamespaceSymbol namespaceSymbol)
                {
                    foreach (var nested in namespaceSymbol.GetAllTypes())
                        yield return nested;
                }
                else if (member is INamedTypeSymbol typeSymbol)
                {
                    yield return typeSymbol;

                    foreach (var nested in typeSymbol.GetAllTypes())
                        yield return nested;
                }
            }
        }

        // 递归获取类型的所有嵌套类型
        public static IEnumerable<INamedTypeSymbol> GetAllTypes(this INamedTypeSymbol type)
        {
            foreach (var nested in type.GetTypeMembers())
            {
                yield return nested;

                foreach (var child in nested.GetAllTypes())
                    yield return child;
            }
        }
    }
}