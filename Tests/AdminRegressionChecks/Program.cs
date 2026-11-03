using System.Security.Claims;
using System.Text;
using HealthTrack.Controllers;
using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using HealthTrack.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

var databaseName = "HealthTrack_AdminChecks_" + Guid.NewGuid().ToString("N");
var connection = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";
var services = new ServiceCollection();
services.AddLogging(); services.AddMvcCore(); services.AddHttpContextAccessor();
services.AddDataProtection().UseEphemeralDataProtectionProvider();
services.AddAuthentication(IdentityConstants.ApplicationScheme).AddCookie(IdentityConstants.ApplicationScheme);
services.AddSingleton<AuditSaveChangesInterceptor>(); services.AddSingleton<ProviderFailureInterceptor>();
services.AddDbContext<HealthTrackDbContext>((sp, o) => o.UseSqlServer(connection)
    .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>(), sp.GetRequiredService<ProviderFailureInterceptor>()));
services.AddIdentityCore<Person>(o => o.Password.RequiredLength = 8).AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<HealthTrackDbContext>().AddSignInManager().AddDefaultTokenProviders();
services.AddScoped<IAuditService, AuditService>();
using var provider = services.BuildServiceProvider(); using var scope = provider.CreateScope();
var sp = scope.ServiceProvider; var db = sp.GetRequiredService<HealthTrackDbContext>();
var users = sp.GetRequiredService<UserManager<Person>>(); var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
var clock = new MutableClock { Now = new DateTimeOffset(2026, 10, 29, 14, 0, 0, TimeSpan.Zero) };
var time = new AppointmentTime(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Appointments:TimeZoneId"] = "Asia/Colombo" }).Build());
var mail = new FakeMail(); var notifications = new NotificationService(db, clock, time, mail);
var checks = 0; var ownsDatabase = false;
void Check(bool value, string name) { if (!value) throw new Exception("FAILED: " + name); checks++; }
DefaultHttpContext Http(string? id = null, string? role = null, string path = "/Admin") {
    var http = new DefaultHttpContext { RequestServices = sp };
    http.Request.Scheme = "https"; http.Request.Host = new HostString("localhost"); http.Request.Path = path;
    if (id != null) http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
        new Claim(ClaimTypes.NameIdentifier,id), new Claim(ClaimTypes.Name,"Regression actor"), new Claim(ClaimTypes.Role,role ?? AppRoles.Admin)
    },"checks"));
    sp.GetRequiredService<IHttpContextAccessor>().HttpContext = http; return http;
}
T Setup<T>(T controller, DefaultHttpContext http) where T : Controller {
    controller.ControllerContext = new ControllerContext { HttpContext = http, RouteData = new RouteData(), ActionDescriptor = new ControllerActionDescriptor() };
    controller.TempData = new TempDataDictionary(http, new MemoryTempData()); return controller;
}
AdminController Admin(string actorId, UserManager<Person>? manager = null) {
    manager ??= users;
    return Setup(new AdminController(NullLogger<AdminController>.Instance,db,manager,notifications,null!,time,clock,
        new UserImportService(manager,db,clock)),Http(actorId));
}
CreateAdminViewModel AdminInput(string email) => new() { Name="Regression administrator", Email=email,
    DateOfBirth=new DateTime(1990,1,1), Password="TestOnly@12345", ConfirmPassword="TestOnly@12345" };
async Task<Person> PersonAccount(Person person, string role) {
    person.UserName=person.Email; person.EmailConfirmed=true; person.DateOfBirth=new DateTime(1990,1,1);
    var result=await AccountProvisioner.CreateAsync(db,users,person,"TestOnly@12345",role);
    if(!result.Succeeded) throw new Exception(string.Join(" ",result.Errors.Select(e=>e.Description)));
    return person;
}
try {
    if(await db.Database.CanConnectAsync()) throw new Exception("Refusing to reuse an existing database.");
    ownsDatabase=true; await db.Database.EnsureCreatedAsync();
    Http(); foreach(var role in new[]{AppRoles.Admin,AppRoles.Client,AppRoles.Trainer}) await roles.CreateAsync(new IdentityRole(role));
    var actor=await PersonAccount(new Admin{Name="Regression administrator",Email="admin@example.invalid"},AppRoles.Admin);
    var client=(Client)await PersonAccount(new Client{Name="Regression client",Email="client@example.invalid",HeightCm=170},AppRoles.Client);
    var trainer=(Trainer)await PersonAccount(new Trainer{Name="Regression trainer",Email="trainer@example.invalid"},AppRoles.Trainer);
    var controller=Admin(actor.Id);
    Check(await controller.CreateAdmin(AdminInput("created@example.invalid")) is RedirectToActionResult,"administrator creation succeeds");
    var created=await users.FindByEmailAsync("created@example.invalid") ?? throw new Exception("Missing fixture");
    Check(await users.IsInRoleAsync(created,AppRoles.Admin),"created administrator has its role");
    Check(await db.AuditLogs.AnyAsync(a=>a.ActorId==actor.Id && a.Action=="Created" && a.Entity==nameof(HealthTrack.Models.Admin)),"account creation is audited");
    await controller.ToggleLock(created.Id);
    Check((await db.Users.AsNoTracking().SingleAsync(p=>p.Id==created.Id)).LockoutEnd>DateTimeOffset.UtcNow,"lock persists");
    Check(await db.AuditLogs.AnyAsync(a=>a.ActorId==actor.Id && a.Action=="Locked"),"lock is audited");
    await controller.ToggleLock(created.Id);
    Check(await db.AuditLogs.AnyAsync(a=>a.ActorId==actor.Id && a.Action=="Unlocked"),"unlock is audited");
    var lockFailures=new RejectStampManager(sp); var failureController=Admin(actor.Id,lockFailures); var auditCount=await db.AuditLogs.CountAsync();
    await failureController.ToggleLock(created.Id);
    Check((await db.Users.AsNoTracking().SingleAsync(p=>p.Id==created.Id)).LockoutEnd==null,"failed lock rolls back");
    Check(await db.AuditLogs.CountAsync()==auditCount,"failed lock leaves no success audit entries");
    controller=Admin(actor.Id); await controller.DeleteUser(created.Id);
    Check(!await db.Users.AnyAsync(p=>p.Id==created.Id),"delete persists");
    Check(await db.AuditLogs.AnyAsync(a=>a.Action=="Deleted" && a.Entity==nameof(HealthTrack.Models.Admin)),"delete is audited");
    var rejected=Admin(actor.Id,new RejectRoleManager(sp));
    Check(await rejected.CreateAdmin(AdminInput("rejected@example.invalid")) is ViewResult && !rejected.ModelState.IsValid,"role failure returns validation errors");
    Check(!await db.Users.AnyAsync(p=>p.Email=="rejected@example.invalid"),"role failure leaves no orphan account");
    var faults=sp.GetRequiredService<ProviderFailureInterceptor>(); faults.Fail=true;
    var failedTrainer=new Trainer{Name="Failed provider fixture",Email="provider-fail@example.invalid",UserName="provider-fail@example.invalid"};
    var providerResult=await AccountProvisioner.CreateAsync(db,users,failedTrainer,"TestOnly@12345",AppRoles.Trainer);
    faults.Fail=false;
    Check(!providerResult.Succeeded && !await db.Users.AnyAsync(p=>p.Email==failedTrainer.Email),"provider failure rolls back trainer and role");
    var assignmentController=Setup(new AssignmentsController(db,users,new TrainerAssignmentQueries(db),clock,notifications),Http(actor.Id));
    await assignmentController.Edit(new AssignmentForm{ClientId=client.Id,TrainerId=trainer.Id});
    Check(await db.AuditLogs.AnyAsync(a=>a.Entity==nameof(ClientTrainerAssignment) && a.ActorId==actor.Id),"assignment changes are audited");

    controller=Admin(actor.Id);
    var before=(UsageDataViewModel)((JsonResult)await controller.UsageData(7)).Value!;
    var localDay=time.ToLocal(clock.Now.UtcDateTime).Date;
    var professional=new ConsultationProvider{Name="Regression professional",Kind=ConsultationProviderKind.HealthcareProfessional,IsActive=true};
    var slot=new AvailabilitySlot{Provider=professional,StartUtc=clock.Now.UtcDateTime.AddDays(2),EndUtc=clock.Now.UtcDateTime.AddDays(2).AddHours(1),IsAvailable=true};
    db.Appointments.Add(new Appointment{ClientId=client.Id,Slot=slot,RequestedAtUtc=time.ToUtc(localDay.AddHours(1)),Status=AppointmentStatus.Pending}); await db.SaveChangesAsync();
    var after=(UsageDataViewModel)((JsonResult)await controller.UsageData(7)).Value!;
    var index=after.Labels.IndexOf(localDay.ToString("yyyy-MM-dd"));
    Check(after.Appointments[index]-before.Appointments[index]==1,"UTC appointment is grouped on its local date");

    db.Notifications.Add(new Notification{PersonId=client.Id,Type=NotificationType.Workout,Message="Automatic regression reminder"}); await db.SaveChangesAsync();
    await controller.SendNotification();
    Check(!((List<BroadcastHistoryItem>)controller.ViewData["History"]!).Any(),"automatic reminders do not appear in broadcast history");
    client=await db.Clients.SingleAsync(c=>c.Id==client.Id); client.EmailNotificationsEnabled=true; await db.SaveChangesAsync();
    var broadcasts=new AdminBroadcastService(db,time,clock);
    var future=time.ToLocal(clock.Now.UtcDateTime).AddMinutes(2);
    await broadcasts.SendAsync(actor.Id,actor.Name,client.Id,"Scheduled regression message",NotificationType.General,future);
    Check(mail.Messages.Count==0,"scheduled broadcast sends no early email");
    Check(await db.AuditLogs.AnyAsync(a=>a.Entity==nameof(AdminBroadcast) && a.ActorId==actor.Id),"broadcast is audited");
    await controller.SendNotification(); var history=(List<BroadcastHistoryItem>)controller.ViewData["History"]!;
    Check(history.Count==1 && history[0].SenderName==actor.Name && history[0].Recipients==1 && history[0].Status=="Scheduled","broadcast history records sender, recipients, and scheduled state");
    var dispatcher=new NotificationEmailDispatcher(db,mail,clock,NullLogger<NotificationEmailDispatcher>.Instance,time);
    mail.Configured=true; await dispatcher.DispatchAsync(); Check(mail.Messages.Count==0,"worker waits until due time");
    clock.Now=clock.Now.AddMinutes(3); await dispatcher.DispatchAsync();
    Check(mail.Messages.Count==1 && await db.NotificationEmails.AsNoTracking().AnyAsync(e=>e.SentAtUtc!=null),"due email is delivered and persisted");
    await dispatcher.DispatchAsync(); Check(mail.Messages.Count==1,"delivered email is not sent twice");
    await broadcasts.SendAsync(actor.Id,actor.Name,client.Id,"Duplicate title",NotificationType.General,null);
    await broadcasts.SendAsync(actor.Id,actor.Name,client.Id,"Duplicate title",NotificationType.General,null);
    await controller.SendNotification(); history=(List<BroadcastHistoryItem>)controller.ViewData["History"]!;
    Check(history.Count(h=>h.Message=="Duplicate title")==2,"identical broadcasts remain separate history entries");
    await dispatcher.DispatchAsync();
    var sentBeforeOptOut=mail.Messages.Count;
    await broadcasts.SendAsync(actor.Id,actor.Name,client.Id,"Opt-out regression",NotificationType.General,null);
    client.EmailNotificationsEnabled=false; await db.SaveChangesAsync(); await dispatcher.DispatchAsync();
    Check(mail.Messages.Count==sentBeforeOptOut && await db.NotificationEmails.AsNoTracking().AnyAsync(e=>e.CancelledAtUtc!=null),"worker respects changed email preference");
    client.EmailNotificationsEnabled=true; await db.SaveChangesAsync();
    await broadcasts.SendAsync(actor.Id,actor.Name,client.Id,"Retry regression",NotificationType.General,null);
    mail.FailuresRemaining=1; await dispatcher.DispatchAsync();
    var retry=await db.NotificationEmails.AsNoTracking().SingleAsync(e=>e.Notification.Message=="Retry regression");
    Check(retry.Attempts==1 && retry.SentAtUtc==null && retry.DueAtUtc>clock.Now.UtcDateTime,"failed send schedules a retry");
    clock.Now=clock.Now.AddMinutes(3); await dispatcher.DispatchAsync();
    Check(await db.NotificationEmails.AsNoTracking().AnyAsync(e=>e.Notification.Message=="Retry regression" && e.SentAtUtc!=null && e.Attempts==2),"transient failure succeeds on retry");
    await broadcasts.SendAsync(actor.Id,actor.Name,client.Id,"Lease regression",NotificationType.General,null);
    await db.NotificationEmails.Where(e=>e.Notification.Message=="Lease regression").ExecuteUpdateAsync(s=>s.SetProperty(e=>e.LeaseUntilUtc,clock.Now.UtcDateTime.AddMinutes(1)));
    var beforeLease=mail.Messages.Count; await dispatcher.DispatchAsync(); Check(mail.Messages.Count==beforeLease,"another worker's active lease is respected");
    clock.Now=clock.Now.AddMinutes(2); await dispatcher.DispatchAsync(); Check(mail.Messages.Count==beforeLease+1,"expired worker lease is recoverable");
    await broadcasts.SendAsync(actor.Id,actor.Name,client.Id,"Permanent failure regression",NotificationType.General,null);
    mail.FailuresRemaining=10;
    for(var attempt=0;attempt<6;attempt++){await dispatcher.DispatchAsync();clock.Now=clock.Now.AddHours(1);}
    var dead=await db.NotificationEmails.AsNoTracking().SingleAsync(e=>e.Notification.Message=="Permanent failure regression");
    Check(dead.Attempts==5 && dead.SentAtUtc==null,"permanent failure stops after five attempts"); mail.FailuresRemaining=0;
    var genericFuture=time.ToLocal(clock.Now.UtcDateTime).AddMinutes(1);
    await notifications.SendAsync(client.Id,"Generic scheduled regression",NotificationType.General,genericFuture);
    Check(await db.NotificationEmails.AnyAsync(e=>e.Notification.Message=="Generic scheduled regression"),"non-broadcast scheduled notifications also enter outbox");
    mail.Configured=false; var disabledCount=mail.Messages.Count; clock.Now=clock.Now.AddMinutes(2); await dispatcher.DispatchAsync();
    Check(mail.Messages.Count==disabledCount,"unconfigured SMTP does not attempt delivery");
    db.Notifications.AddRange(new Notification{PersonId=client.Id,Message="Legacy future schedule",ScheduledFor=time.ToLocal(clock.Now.UtcDateTime).AddHours(1)},
        new Notification{PersonId=client.Id,Message="Historical schedule",ScheduledFor=time.ToLocal(clock.Now.UtcDateTime).AddDays(-1)});
    await db.SaveChangesAsync(); await dispatcher.DispatchAsync();
    Check(await db.NotificationEmails.AnyAsync(e=>e.Notification.Message=="Legacy future schedule"),"future legacy schedules are adopted even with SMTP disabled");
    Check(!await db.NotificationEmails.AnyAsync(e=>e.Notification.Message=="Historical schedule"),"historical notifications are not replayed");

    Http(actor.Id); var import=new UserImportService(users,db,clock);
    var csv="Name,Email,Role,DateOfBirth,HeightCm,FitnessGoal,Password\nImported Client,imported@example.invalid,Client,1990-01-01,170,,\nImported Trainer,imported-trainer@example.invalid,Trainer,1990-01-01,,,\n";
    var importResult=await import.ImportAsync(new MemoryStream(Encoding.UTF8.GetBytes(csv)));
    Check(importResult.Imported.Count==2 && importResult.Rejected.Count==0,"client and trainer imports succeed");
    var imported=await users.FindByEmailAsync("imported@example.invalid") ?? throw new Exception("Missing imported fixture");
    var temporaryPassword=importResult.Imported.Single(p=>p.Email==imported.Email).TemporaryPassword!;
    Check(imported.MustChangePassword && imported.TemporaryPasswordExpiresAtUtc==clock.Now.UtcDateTime.AddHours(48),"import password expires in 48 hours and requires change");
    var importedTrainer=await users.FindByEmailAsync("imported-trainer@example.invalid");
    Check(await db.ConsultationProviders.AnyAsync(p=>p.PersonId==importedTrainer!.Id),"imported trainer has a consultation provider");
    Check(!TemporaryPasswordPolicy.IsExpired(imported,clock.Now.UtcDateTime) && TemporaryPasswordPolicy.IsExpired(imported,clock.Now.UtcDateTime.AddHours(48)),"temporary password expiry boundary is enforced");
    var middleware=new TemporaryPasswordMiddleware(_=>Task.CompletedTask);
    var restricted=Http(imported.Id,AppRoles.Client,"/Client/ProgressData"); await middleware.InvokeAsync(restricted,users,clock);
    Check(restricted.Response.Headers.Location=="/Account/ChangePassword","temporary credentials cannot access application endpoints");
    var allowed=false; var allowedMiddleware=new TemporaryPasswordMiddleware(_=>{allowed=true;return Task.CompletedTask;});
    var changeHttp=Http(imported.Id,AppRoles.Client,"/Account/ChangePassword"); await allowedMiddleware.InvokeAsync(changeHttp,users,clock); Check(allowed,"password-change route remains available");
    var signIn=sp.GetRequiredService<SignInManager<Person>>();
    AccountController Account(DefaultHttpContext http){signIn.Context=http;return Setup(new AccountController(NullLogger<AccountController>.Instance,db,users,signIn,clock),http);}
    var noReuse=Account(Http(imported.Id,AppRoles.Client,"/Account/ChangePassword"));
    Check(await noReuse.ChangePassword(new ChangePasswordViewModel{CurrentPassword=temporaryPassword,NewPassword=temporaryPassword,ConfirmPassword=temporaryPassword}) is ViewResult && !noReuse.ModelState.IsValid,"temporary password cannot be reused as permanent password");
    var account=Account(Http(imported.Id,AppRoles.Client,"/Account/ChangePassword"));
    await account.ChangePassword(new ChangePasswordViewModel{CurrentPassword=temporaryPassword,NewPassword="NewTestOnly@12345",ConfirmPassword="NewTestOnly@12345"});
    imported=await users.FindByIdAsync(imported.Id) ?? throw new Exception("Missing fixture");
    Check(!imported.MustChangePassword && imported.TemporaryPasswordExpiresAtUtc==null && await users.CheckPasswordAsync(imported,"NewTestOnly@12345"),"successful change clears restriction and temporary expiry");
    controller=Admin(actor.Id); var stamp=imported.SecurityStamp;
    await controller.ResetPassword(imported.Id,new ResetUserPasswordViewModel{Password="ResetTestOnly@12345",ConfirmPassword="ResetTestOnly@12345"});
    imported=await users.FindByIdAsync(imported.Id) ?? throw new Exception("Missing reset fixture");
    Check(imported.MustChangePassword && imported.TemporaryPasswordExpiresAtUtc==clock.Now.UtcDateTime.AddHours(48) && imported.SecurityStamp!=stamp,"admin reset renews expiry and invalidates old sessions");
    Check(await db.AuditLogs.AnyAsync(a=>a.Action=="PasswordReset" && a.ActorId==actor.Id),"password reset is audited without storing passwords");
    clock.Now=clock.Now.AddHours(49);
    var expiredLogin=Account(Http(path:"/Account/Login"));
    Check(await expiredLogin.Login(new LoginViewModel{Email=imported.Email!,Password="ResetTestOnly@12345"}) is ViewResult && !expiredLogin.ModelState.IsValid,"expired temporary password cannot sign in");
    var expiredHttp=Http(imported.Id,AppRoles.Client,"/Client"); await middleware.InvokeAsync(expiredHttp,users,clock);
    Check(expiredHttp.Response.Headers.Location=="/Account/Login?temporaryPasswordExpired=true","expired temporary session is signed out");
    Check(!await db.AuditLogs.AnyAsync(a=>a.Detail!=null && (a.Detail.Contains("TestOnly@") || a.Detail.Contains(temporaryPassword))),"audit records contain no passwords");
    Console.WriteLine($"Passed {checks} Admin regression checks. Email sender was fake; application data was not used.");
}
finally {
    if(ownsDatabase && databaseName.StartsWith("HealthTrack_AdminChecks_",StringComparison.Ordinal)) {
        sp.GetRequiredService<IHttpContextAccessor>().HttpContext=null;
        await db.Database.EnsureDeletedAsync(); Console.WriteLine("Isolated regression database removed.");
    }
}

class MutableClock:TimeProvider { public DateTimeOffset Now {get;set;} public override DateTimeOffset GetUtcNow()=>Now; }
class FakeMail:IMailSender {
    public bool Configured {get;set;} public bool IsConfigured=>Configured; public int FailuresRemaining{get;set;}
    public List<string> Messages {get;}=new();
    public void QueueNotification(string address,string name,string message)=>Messages.Add(message);
    public Task SendNotificationAsync(string address,string name,string message,CancellationToken token=default){
        if(FailuresRemaining>0){FailuresRemaining--;throw new IOException("Injected delivery failure");}
        Messages.Add(message);return Task.CompletedTask;
    }
    public Task SendReportAsync(string address,string name,string subject,string message,byte[] attachment,string fileName,CancellationToken token=default){
        Messages.Add(subject);return Task.CompletedTask;
    }
}
class MemoryTempData:ITempDataProvider { public IDictionary<string,object> LoadTempData(HttpContext c)=>new Dictionary<string,object>();public void SaveTempData(HttpContext c,IDictionary<string,object> values){} }
class RejectRoleManager(IServiceProvider p):UserManager<Person>(p.GetRequiredService<IUserStore<Person>>(),p.GetRequiredService<IOptions<IdentityOptions>>(),p.GetRequiredService<IPasswordHasher<Person>>(),p.GetServices<IUserValidator<Person>>(),p.GetServices<IPasswordValidator<Person>>(),p.GetRequiredService<ILookupNormalizer>(),p.GetRequiredService<IdentityErrorDescriber>(),p,p.GetRequiredService<ILogger<UserManager<Person>>>()) {
    public override Task<IdentityResult> AddToRoleAsync(Person user,string role)=>Task.FromResult(IdentityResult.Failed(new IdentityError{Description="Injected role assignment failure"}));
}
class RejectStampManager(IServiceProvider p):UserManager<Person>(p.GetRequiredService<IUserStore<Person>>(),p.GetRequiredService<IOptions<IdentityOptions>>(),p.GetRequiredService<IPasswordHasher<Person>>(),p.GetServices<IUserValidator<Person>>(),p.GetServices<IPasswordValidator<Person>>(),p.GetRequiredService<ILookupNormalizer>(),p.GetRequiredService<IdentityErrorDescriber>(),p,p.GetRequiredService<ILogger<UserManager<Person>>>()) {
    public override Task<IdentityResult> UpdateSecurityStampAsync(Person user)=>Task.FromResult(IdentityResult.Failed(new IdentityError{Description="Injected security stamp failure"}));
}
class ProviderFailureInterceptor:SaveChangesInterceptor {
    public bool Fail{get;set;}
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data,InterceptionResult<int> result,CancellationToken token=default){
        if(Fail && data.Context!.ChangeTracker.Entries<ConsultationProvider>().Any(e=>e.State==EntityState.Added)) throw new DbUpdateException("Injected provider write failure");
        return base.SavingChangesAsync(data,result,token);
    }
}
