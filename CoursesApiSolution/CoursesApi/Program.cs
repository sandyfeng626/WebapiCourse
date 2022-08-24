using CoursesApi;
using CoursesApi.Domain;
using MongoDB.Bson.Serialization.Conventions;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRouting(options =>
{
    // "{x:bsonid}"
    options.ConstraintMap.Add(ObjectIdRouteConstraint.CONSTRAINT_KEY,
        typeof(ObjectIdRouteConstraint));
});

var conventionPack = new ConventionPack()
{
    new CamelCaseElementNameConvention(),
    new IgnoreExtraElementsConvention(true),
    new EnumRepresentationConvention(MongoDB.Bson.BsonType.String)
};

ConventionRegistry.Register("Default", conventionPack, t => true);

// Add services to the container.

// It will create a new service that we can inject of type IOptions<DevelopmentFlags>
builder.Services.Configure<DevelopmentFlags>(
    builder.Configuration.GetSection(DevelopmentFlags.SectionName));

builder.Services.AddHttpClient<HrApiAdapter>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("hr-api-url"));
    client.Timeout = TimeSpan.FromSeconds(1); // Warning! This is clasroom demo time. Not this short.
    client.DefaultRequestHeaders.Add("User-Agent", "CoursesApi");
}).AddPolicyHandler(SrePolicies.BasicRetryPolicy()).AddPolicyHandler(SrePolicies.CircuitBreakerPolicy());

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//var shellPreference = builder.Configuration.GetValue<string>("Tacos:Shell");
var mongoDbConnectionString = builder.Configuration.GetConnectionString("mongodb-courses");
// Configure Adapters
builder.Services.AddSingleton<MongoDbCoursesAdapter>(_ => // "Discard"
{ 
    return new MongoDbCoursesAdapter(mongoDbConnectionString);
}); // "Lazy" but with a factor method


builder.Services.AddScoped<CourseManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
