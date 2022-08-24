# Configuration in DotNet

The Authority [Configuration in .NET](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-6.0)

## Steps with Default Builder

From highest to lowest priority (that means stuff at the top will veto stuff below, e.g. an environment variable will overwrite `appsettings.json`)

1. Command Line Arguments
2. Non-Prefixed Environment Variables
3. User Secrets
4. `appsettings.{Environment}.json` 
5. `appsettings.json`


## Examples

If you ask the `Configuration` provider for `configuration.GetValue<string>("Tacos:Shell")`, it would look for:

### Environment Variables

Called `Tacos__Shell` (two underscores between segments)

### JSON File

```json
{
  "Tacos": {
      "Shell": "Soft"
    }

}
```

### For ConnectionStrings

If you ask the `Configuration` provider for `builder.Configuration.GetConnectionString("sql")`:

### Environment Variables

`ConnectionStrings__sql`

### JSON

```json
 {
  "ConnectionStrings": {
      "sql": "server=prod;database=demo..."
   }
 }

```

## Usage Notes

You can inject the `IConfiguration` service and use it as above.

### Options

Create a strictly typed class for your options:

```csharp
public class PositionOptions
{
    public const string Position = "Position";

    public string Title { get; set; } = String.Empty;
    public string Name { get; set; } = String.Empty;
}
```

In your `program.cs` do:

```csharp


builder.Services.Configure<PositionOptions>(
    builder.Configuration.GetSection(PositionOptions.Position));
```

In your services, you can then inject:

```cscharp
public class CourseManager
{
  private readonly IOptions<PositionOptions> _options;
  
  public CourseManager(IOptions<PositionOptions> options) 
  {
    _options = options;
  }
  
  public void DoIt() 
  {
    var someTitle = _options.Title.Value;
  }

}


```

