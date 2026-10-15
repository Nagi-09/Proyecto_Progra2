using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using VisitasRecepcion.Data;
using VisitasRecepcion.Models;

namespace VisitasRecepcion.Services;

/// <summary>Envía el correo interno al anfitrión y guarda el resultado en la bitácora.</summary>
public class NotificacionService
{
    /// <summary>
    /// Envía el aviso de llegada. Devuelve (true, mensaje) si salió bien
    /// o (false, mensaje de error) si falló. Nunca lanza excepción hacia afuera.
    /// </summary>
    public async Task<(bool Ok, string Mensaje)> EnviarAvisoAsync(int idVisita)
    {
        using var db = new AppDbContext();

        var visita = await db.Visitas
            .Include(v => v.Visitante)
            .Include(v => v.Empleado)
            .Include(v => v.Area)
            .FirstAsync(v => v.IdVisita == idVisita);

        var log = new LogNotificacion { IdVisita = idVisita, FechaEnvio = DateTime.Now };

        try
        {
            if (string.IsNullOrWhiteSpace(visita.Empleado!.Correo))
                throw new InvalidOperationException("El anfitrión no tiene correo registrado.");

            var mensaje = new MailMessage
            {
                From = new MailAddress(Config.CorreoRemitente),
                Subject = "Tu visita ha llegado a recepción",
                Body =
                    $"Hola {visita.Empleado.Nombre},\n\n" +
                    $"{visita.Visitante!.Nombre} ha llegado a recepción para verte.\n" +
                    $"Área: {visita.Area!.Nombre}\n" +
                    $"Motivo: {visita.Motivo}\n" +
                    $"Hora de registro: {visita.FechaHoraEntrada:dd/MM/yyyy HH:mm}\n\n" +
                    "Sistema de Registro de Visitas"
            };
            mensaje.To.Add(visita.Empleado.Correo);

            using var smtp = new SmtpClient(Config.SmtpServidor, Config.SmtpPuerto)
            {
                EnableSsl = Config.SmtpUsarSsl
            };
            if (!string.IsNullOrEmpty(Config.SmtpUsuario))
                smtp.Credentials = new NetworkCredential(Config.SmtpUsuario, Config.SmtpPassword);

            await smtp.SendMailAsync(mensaje);

            log.Estado = "Enviado";
            visita.NotificacionEnviada = true;
        }
        catch (Exception ex)
        {
            log.Estado = "Error";
            log.Detalle = ex.Message;
        }

        db.LogsNotificacion.Add(log);
        await db.SaveChangesAsync();

        return log.Estado == "Enviado"
            ? (true, "Correo enviado al anfitrión.")
            : (false, "No se pudo enviar el correo: " + log.Detalle);
    }
}
