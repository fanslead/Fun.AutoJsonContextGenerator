# Fun.AutoJsonContextGenerator

一个专为 .NET AOT 编译设计的 JSON 序列化代码生成器，能够自动生成 `JsonSerializerContext` 以支持原生 AOT 编译。

## 🚀 功能特性

- ✅ **AOT 友好**：专为 .NET AOT 编译优化，无反射依赖
- ✅ **自动发现**：自动扫描带有 `AutoJsonSerializableAttribute` 特性的类型
- ✅ **继承支持**：支持基类和接口继承的类型自动识别
- ✅ **集合类型**：自动包含类型本身及其集合类型（List<T>、Array等）
- ✅ **配置灵活**：支持 `autojsonconfig.json` 配置文件
- ✅ **增量生成**：支持部分类扩展，不影响用户自定义配置

## 📦 安装

```xml
<PackageReference Include="Fun.AutoJsonContextGenerator" Version="1.0.0">
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

## 🛠️ 使用方法

### 1. 标记需要序列化的类型

```csharp
using Fun.AutoJsonContextGenerator;

[AutoJsonSerializable]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

[AutoJsonSerializable]
public class Product
{
    public string Title { get; set; }
    public decimal Price { get; set; }
}
```

### 2. 自动生成的代码

生成器会自动创建 `AutoJsonContext.g.cs` 文件：

```csharp
// 自动生成的文件
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(List<User>))]
[JsonSerializable(typeof(User[]))]
[JsonSerializable(typeof(Product))]
[JsonSerializable(typeof(List<Product>))]
[JsonSerializable(typeof(Product[]))]
public partial class AutoJsonContext : JsonSerializerContext
{
}
```

### 3. 使用生成的上下文

```csharp
// 序列化
var user = new User { Id = 1, Name = "张三" };
string json = JsonSerializer.Serialize(user, AutoJsonContext.Default.User);

// 反序列化
User deserializedUser = JsonSerializer.Deserialize<User>(json, AutoJsonContext.Default.User);

// 集合序列化
var users = new List<User> { user };
string usersJson = JsonSerializer.Serialize(users, AutoJsonContext.Default.ListUser);
```

## ⚙️ 配置文件

创建 `autojsonconfig.json` 文件来自定义生成行为：

```json
{
  "contextClassName": "MyJsonContext",
  "namespace": "MyApp.Serialization",
  "includeCollections": true,
  "collectionTypes": [
    "List<{0}>",
    "{0}[]",
    "IEnumerable<{0}>",
    "IList<{0}>"
  ],
  "excludeTypes": [
    "MyApp.Models.InternalModel"
  ]
}
```

### 配置说明

| 配置项 | 默认值 | 说明 |
|--------|--------|------|
| `contextClassName` | `AutoJsonContext` | 生成的上下文类名 |
| `namespace` | 当前项目根命名空间 | 生成类的命名空间 |
| `includeCollections` | `true` | 是否自动包含集合类型 |
| `collectionTypes` | `["List<{0}>", "{0}[]"]` | 要生成的集合类型模板 |
| `excludeTypes` | `[]` | 要排除的类型全名列表 |

## 🔧 高级配置

如果需要配置 `JsonSourceGenerationOptions`，可以创建部分类：

```csharp
// AutoJsonContext.Options.cs
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class AutoJsonContext : JsonSerializerContext
{
}
```

## 🌟 继承支持示例

```csharp
// 基类
[AutoJsonSerializable]
public abstract class Animal
{
    public string Name { get; set; }
}

// 继承类会自动包含
public class Dog : Animal
{
    public string Breed { get; set; }
}

// 接口
[AutoJsonSerializable]
public interface IVehicle
{
    string Model { get; set; }
}

// 实现类会自动包含
public class Car : IVehicle
{
    public string Model { get; set; }
    public int Year { get; set; }
}
```

## 🎯 AOT 发布

在 AOT 发布时，生成的代码确保了 JSON 序列化的零反射依赖：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <PublishAot>true</PublishAot>
    <InvariantGlobalization>true</InvariantGlobalization>
  </PropertyGroup>
</Project>
```

## 📝 注意事项

1. **增量生成**：每次构建时会重新生成 `AutoJsonContext.g.cs`，用户自定义的部分类不会被覆盖
2. **类型发现**：只会包含当前项目中标记了 `AutoJsonSerializableAttribute` 的类型
3. **命名空间**：生成的类型会使用项目的默认命名空间，可通过配置文件修改
4. **性能优化**：生成的代码针对 AOT 编译优化，运行时性能更佳

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

MIT License