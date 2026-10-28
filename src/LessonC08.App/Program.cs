using LessonC08.App.Components;
using LessonC08.App.Lab;

DotEnv.Load();

if (args.FirstOrDefault() == "setup")
{
    await SetupResources.RunAsync();
    return;
}

var options = LabOptions.FromEnvironment();
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://127.0.0.1:{options.Port}");

builder.Services.AddSingleton(options);
builder.Services.AddSingleton<SessionRunner>();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    await next();
});
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Lifetime.ApplicationStarted.Register(() => Console.WriteLine($"Open http://127.0.0.1:{options.Port}"));
app.Run();
