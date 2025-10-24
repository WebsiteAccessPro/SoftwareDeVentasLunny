using Microsoft.AspNetCore.Identity.UI.Services;

namespace ProyectoFinalCalidad.Services
{
    public class NoOpEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            return Task.CompletedTask;
        }
    }
}
