using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Controllers
{
    public class HealthMetricsController : ClientBaseController
    {
        private readonly ILogger<HealthMetricsController> _logger;

        public HealthMetricsController(ILogger<HealthMetricsController> logger, HealthTrackDbContext context, UserManager<Person> userManager)
            : base(context, userManager)
        {
            _logger = logger;
        }

        private Task<HealthMetric?> FindOwnedAsync(int id)
        {
            return Context.HealthMetrics
                .Include(m => m.Client)
                .FirstOrDefaultAsync(m => m.MetricId == id && m.ClientId == CurrentUserId);
        }

        private void ValidateBloodPressure(HealthMetric metric)
        {
            if (metric.SystolicBp.HasValue != metric.DiastolicBp.HasValue)
                ModelState.AddModelError(metric.SystolicBp.HasValue ? nameof(HealthMetric.DiastolicBp) : nameof(HealthMetric.SystolicBp),
                    "Enter both blood pressure values, or leave both empty.");
            else if (metric.SystolicBp <= metric.DiastolicBp)
                ModelState.AddModelError(nameof(HealthMetric.SystolicBp), "Systolic pressure must be higher than diastolic pressure.");
        }

        public async Task<IActionResult> Index(DateTime? from = null, DateTime? to = null)
        {
            if (!SetJournalFilters(from, to)) return View(new List<HealthMetric>());
            var metrics = await Context.HealthMetrics
                .Include(m => m.Client)
                .Where(m => m.ClientId == CurrentUserId)
                .Where(m => (!from.HasValue || m.RecordedAt.Date >= from.Value.Date)
                    && (!to.HasValue || m.RecordedAt.Date <= to.Value.Date))
                .OrderByDescending(m => m.RecordedAt)
                .ThenByDescending(m => m.MetricId)
                .ToListAsync();
            return View(metrics);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var last = await Context.HealthMetrics.AsNoTracking()
                .Where(m => m.ClientId == CurrentUserId)
                .OrderByDescending(m => m.RecordedAt)
                .ThenByDescending(m => m.MetricId)
                .FirstOrDefaultAsync();
            return View(new HealthMetric
            {
                RecordedAt = LocalNow,
                WeightKg = last?.WeightKg ?? 0,
                BodyFatPercent = last?.BodyFatPercent,
                RestingHeartRate = last?.RestingHeartRate,
                SystolicBp = last?.SystolicBp,
                DiastolicBp = last?.DiastolicBp,
                Steps = last?.Steps,
                SleepHours = last?.SleepHours
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("WeightKg,BodyFatPercent,RestingHeartRate,SystolicBp,DiastolicBp,Steps,SleepHours,RecordedAt")] HealthMetric metric)
        {
            metric.ClientId = CurrentUserId;
            ModelState.Remove(nameof(HealthMetric.ClientId));
            ValidateNotInFuture(metric.RecordedAt, nameof(metric.RecordedAt));
            ValidateBloodPressure(metric);
            if (!ModelState.IsValid)
            {
                return View(metric);
            }

            Context.HealthMetrics.Add(metric);
            await Context.SaveChangesAsync();
            _logger.LogInformation("Client {ClientId} recorded a health metric", metric.ClientId);
            TempData["Success"] = "Measurement saved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var metric = await FindOwnedAsync(id);
            return metric == null ? NotFound() : View(metric);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("WeightKg,BodyFatPercent,RestingHeartRate,SystolicBp,DiastolicBp,Steps,SleepHours,RecordedAt")] HealthMetric input)
        {
            var metric = await FindOwnedAsync(id);
            if (metric == null)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(HealthMetric.ClientId));
            ValidateNotInFuture(input.RecordedAt, nameof(input.RecordedAt));
            ValidateBloodPressure(input);
            if (!ModelState.IsValid)
            {
                input.MetricId = id;
                return View(input);
            }

            metric.WeightKg = input.WeightKg;
            metric.BodyFatPercent = input.BodyFatPercent;
            metric.RestingHeartRate = input.RestingHeartRate;
            metric.SystolicBp = input.SystolicBp;
            metric.DiastolicBp = input.DiastolicBp;
            metric.Steps = input.Steps;
            metric.SleepHours = input.SleepHours;
            metric.RecordedAt = input.RecordedAt;
            await Context.SaveChangesAsync();
            TempData["Success"] = "Measurement updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var metric = await FindOwnedAsync(id);
            return metric == null ? NotFound() : View(metric);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var metric = await FindOwnedAsync(id);
            if (metric == null)
            {
                return NotFound();
            }

            Context.HealthMetrics.Remove(metric);
            await Context.SaveChangesAsync();
            TempData["Success"] = "Measurement deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
