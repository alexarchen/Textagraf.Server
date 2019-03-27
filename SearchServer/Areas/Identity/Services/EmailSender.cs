using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
//using System.Net.Mail;
using System.Threading.Tasks;
using MailKit.Net.Smtp;

namespace SearchServer
{
    public class EmailSender :IEmailSender 
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            /*
             * using MailKit;
             */
                        var emailMessage = new MimeMessage();

                        emailMessage.From.Add(new MailboxAddress(title, this.email));
                        emailMessage.To.Add(new MailboxAddress("", email));
                        emailMessage.Subject = subject;
                        emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                        {
                          Text = htmlMessage
                        };
                        
            
            /*
                        var client = new SmtpClient(host, port)
                        {
                            Credentials = new NetworkCredential(userName, password),
                            EnableSsl = enableSSL
                        };
                        await client.SendMailAsync(// emailMessage
                            new MailMessage(userName, email, subject, htmlMessage) { IsBodyHtml = true }
                        );
            */


            using (var client = new SmtpClient())
            {
                client.Connect(host, port);


                // Note: since we don't have an OAuth2 token, disable
                // the XOAUTH2 authentication mechanism.
                client.AuthenticationMechanisms.Remove("XOAUTH2");

                // Note: only needed if the SMTP server requires authentication
                client.Authenticate(userName, password);

                client.Send(emailMessage);
                client.Disconnect(true);
            }

            Console.WriteLine($"Email sended: to {email} text: {htmlMessage}");

        }
        // Our private configuration variables
        private string host;
        private string title;
        private string email;
        private int port;
        private bool enableSSL;
        private string userName;
        private string password;

        // Get our parameterized configuration
        public EmailSender(string title,string fromemail, string host, int port, bool enableSSL, string userName, string password)
        {
            this.host = host;
            this.port = port;
            email = fromemail;
            this.title = title;
            this.enableSSL = enableSSL;
            this.userName = userName;
            this.password = password;
        }
    }
}
