using HealthTrack.Models;
using HealthTrack.Models.ViewModels;

namespace HealthTrack.Services;

public class AppointmentTime
{
    private readonly TimeZoneInfo _zone;
    public AppointmentTime(IConfiguration configuration)
    {
        var zoneId = configuration["Appointments:TimeZoneId"] ?? "Asia/Colombo";
        _zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
    }
    public string Label => _zone.Id;
    public DateTime ToLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(
        DateTime.SpecifyKind(utc, DateTimeKind.Utc), _zone);

    public DateTime ToUtc(DateTime local) => TimeZoneInfo.ConvertTimeToUtc(
        DateTime.SpecifyKind(local, DateTimeKind.Unspecified), _zone);

    public AppointmentViewModel ToViewModel(Appointment appointment) => new()
    {
        Id = appointment.Id,
        ProviderName = appointment.Slot.Provider.Name,
        ProviderKind = appointment.Slot.Provider.Kind == ConsultationProviderKind.Trainer
            ? "Trainer" : "Healthcare professional",
        Specialty = appointment.Slot.Provider.Specialty,
        Start = ToLocal(appointment.Slot.StartUtc),
        End = ToLocal(appointment.Slot.EndUtc),
        Status = appointment.Status,
        Notes = appointment.Notes
    };
}
