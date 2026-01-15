using App.Domain.Entities;
using App.Infrastructure.Services;
using App.Jobs;
using Quartz;

namespace App.Web.Services;

public sealed class CampaignScheduler
{
    private readonly ISchedulerFactory _schedulerFactory;

    public CampaignScheduler(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    public async Task ScheduleCampaignAsync(Campaign campaign, CancellationToken ct)
    {
        var scheduler = await _schedulerFactory.GetScheduler(ct);
        var jobKey = new JobKey($"campaign-{campaign.Id}");
        var triggerKey = new TriggerKey($"campaign-{campaign.Id}-trigger");

        var job = JobBuilder.Create<CampaignPublishJob>()
            .WithIdentity(jobKey)
            .UsingJobData("CampaignId", campaign.Id.ToString())
            .UsingJobData("TenantId", campaign.TenantId)
            .Build();

        var scheduleBuilder = CronScheduleBuilder.CronSchedule(campaign.ScheduleCron)
            .InTimeZone(ScheduleCalculator.ResolveTimeZone(campaign.ScheduleTimezone));

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(job)
            .WithSchedule(scheduleBuilder)
            .Build();

        if (await scheduler.CheckExists(jobKey, ct))
        {
            await scheduler.AddJob(job, true, ct);
            await scheduler.RescheduleJob(triggerKey, trigger, ct);
        }
        else
        {
            await scheduler.ScheduleJob(job, trigger, ct);
        }
    }

    public async Task UnscheduleCampaignAsync(Guid campaignId, CancellationToken ct)
    {
        var scheduler = await _schedulerFactory.GetScheduler(ct);
        var jobKey = new JobKey($"campaign-{campaignId}");
        if (await scheduler.CheckExists(jobKey, ct))
        {
            await scheduler.DeleteJob(jobKey, ct);
        }
    }
}
