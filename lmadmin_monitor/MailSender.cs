using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Configuration;

namespace lmadmin_monitor
{
    /**
     * Simple mail sending utility. Does not support sending multiple emails at the same time (e.g. if two or more alerts occur at the same time), which is usually not an issue for lmadmin.
     * If multiple send operations are attempted in parallel, the service logs a warning message to the event log and continues normally.
     * */
    class MailSender: IDisposable
    {
        System.Collections.Specialized.NameValueCollection appSettings = ConfigurationManager.AppSettings;
        SmtpClient smtpClient;
        

        public MailSender()
        {
            string email_host = appSettings.Get("email_host");
            int email_port = Int32.Parse(appSettings.Get("email_port"));
            smtpClient = new SmtpClient(email_host, email_port);
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new System.Net.NetworkCredential(appSettings.Get("email_username"), appSettings.Get("email_password"));
            smtpClient.EnableSsl = Boolean.Parse(appSettings.Get("email_ssl"));
        }

        public void sendMail(string subject, string body) 
        {
            MailMessage message = new MailMessage(appSettings.Get("email_from"), appSettings.Get("email_to"), subject, body);
            smtpClient.Send(message);
        }



        public void Dispose()
        {
            if (smtpClient!=null){
                smtpClient.Dispose();
            }
        }

    }
}
