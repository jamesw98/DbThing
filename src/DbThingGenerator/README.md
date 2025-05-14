# DbThing Source Generator
If you don't want to manually write the `Initialize` method described in `IDbModel`, you can include the `DbThingGenerator` project as an `Analyzer`.  
  
## How to use:
### Setup
Insert the following into your `csproj` file:
```xml
<ItemGroup>
    <ProjectReference Include="<path>\DbThing\src\DbThingGenerator\DbThingGenerator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false"/>
</ItemGroup>
```

Optional:  
```xml
<PropertyGroup>
    ...
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

### Creating models
Here's an example model with the preprocess attributes.
```csharp
using Attributes;
using DbThing;

namespace ApiExample;

public partial class Person : IDbPreProcessModel, IDbModel
{
    [DbColumn("BusinessEntityID", Required = true)]
    public int PersonId { get; set; }
    
    [DbColumn("HireDate", Required = true)]
    public DateTime HireDate { get; set; }

    [DbColumn("FirstName", Required = true)]
    public string FirstName { get; set; } = string.Empty;
    
    [DbColumn("LastName", Required = true)]
    public string LastName { get; set; } = string.Empty;
    
    [DbColumn("Title")]
    public string? Title { get; set; }
}
```