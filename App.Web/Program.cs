using App.Infrastructure;
using App.Infrastructure.Data;
using App.Infrastructure.Services;
using App.Web.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

var dataRoot = Path.Combine(builder.Environment.ContentRootPath, "data");
builder.Configuration["Storage:DataRoot"] = dataRoot;

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<TenantState>();
builder.Services.AddScoped<CampaignScheduler>();

builder.Services.AddQuartz(options =>
{
});

builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

Directory.CreateDirectory(dataRoot);

await DatabaseInitializer.InitializeAsync(app.Services, app.Environment.ContentRootPath);

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var scheduler = scope.ServiceProvider.GetRequiredService<CampaignScheduler>();
    var catchUp = scope.ServiceProvider.GetRequiredService<ScheduleCatchUpService>();

    var campaigns = await db.Campaigns.AsNoTracking().ToListAsync();
    foreach (var campaign in campaigns)
    {
        await scheduler.ScheduleCampaignAsync(campaign, CancellationToken.None);
    }

    await catchUp.CatchUpAsync(CancellationToken.None);
}

app.Run();
