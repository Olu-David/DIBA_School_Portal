using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DIBA_School_Portal.Server.Configurations
{
    [Authorize]
    public class DashboardHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var user = Context.User;

            if (user?.Identity?.IsAuthenticated == true)
            {
                // ---------- Individual role groups ----------
                if (user.IsInRole("SuperAdmin"))
                    await Groups.AddToGroupAsync(Context.ConnectionId, "SuperAdmins");

                if (user.IsInRole("Principal"))
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Principals");

                if (user.IsInRole("Headmaster") || user.IsInRole("Headmistress"))
                    await Groups.AddToGroupAsync(Context.ConnectionId, "SectionHeads");

                if (user.IsInRole("VicePrincipal"))
                    await Groups.AddToGroupAsync(Context.ConnectionId, "VicePrincipals");

                if (user.IsInRole("SchoolAdmin"))
                    await Groups.AddToGroupAsync(Context.ConnectionId, "SchoolAdmins");

                if (user.IsInRole("Accountant"))
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Finance");

                if (user.IsInRole("Registrar"))
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Registrars");

                if (user.IsInRole("Teacher"))
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Teachers");

                if (user.IsInRole("FormTeacher"))
                    await Groups.AddToGroupAsync(Context.ConnectionId, "FormTeachers");

                // ---------- Personal group ----------
                var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

                // ---------- Composite groups ----------

                // AllStaff = everyone except Student & Parent
                if (user.IsInRole("SuperAdmin") || user.IsInRole("Principal")
                    || user.IsInRole("Headmaster") || user.IsInRole("Headmistress")
                    || user.IsInRole("VicePrincipal") || user.IsInRole("SchoolAdmin")
                    || user.IsInRole("Accountant"  ) || user.IsInRole("Registrar")
                    || user.IsInRole("Teacher") || user.IsInRole("FormTeacher"))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "AllStaff");
                }

                // AllAdmins = leadership + operations (no plain teacher)
                if (user.IsInRole("SuperAdmin") || user.IsInRole("Principal")
                    || user.IsInRole("Headmaster") || user.IsInRole("Headmistress")
                    || user.IsInRole("VicePrincipal") || user.IsInRole("SchoolAdmin")
                    || user.IsInRole("Accountant") || user.IsInRole("Registrar"))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "AllAdmins");
                }

                // Leadership = top of the chain (section heads included)
                if (user.IsInRole("SuperAdmin") || user.IsInRole("Principal")
                    || user.IsInRole("Headmaster") || user.IsInRole("Headmistress")
                    || user.IsInRole("VicePrincipal"))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Leadership");
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}