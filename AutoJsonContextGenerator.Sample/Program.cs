using AutoJsonContextGenerator.Sample;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    //options.SerializerOptions.TypeInfoResolverChain.Insert(0, AutoJsonContext.Default);
});

var app = builder.Build();

app.Run();