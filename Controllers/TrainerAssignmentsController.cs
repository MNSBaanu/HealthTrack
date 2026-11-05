using HealthTrack.Models;
using HealthTrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthTrack.Controllers;

// Shared read endpoint for the separately developed Trainer interface.
// JSON endpoint listing the clients assigned to the signed-in trainer.
[Authorize(Roles = AppRoles.Trainer)]
public class TrainerAssignmentsController(TrainerAssignmentQueries queries, UserManager<Person> users) : Controller
{
    // Returns the trainer's assigned clients as JSON.
    [HttpGet]
    public async Task<IActionResult> Index() => Json(await queries.ForTrainerAsync(users.GetUserId(User)!));
}
