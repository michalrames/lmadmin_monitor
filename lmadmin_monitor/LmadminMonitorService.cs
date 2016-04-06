using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Timers;
using System.Configuration;

namespace lmadmin_monitor
{

    /*
     * This is the main service class. The main purpose is to write to event log. Following types of events can occur:
     * Information - Service start and stop
     * */
    public partial class LmadminMonitorService : ServiceBase
    {
        Timer timer;

        MessageIdSaver saver = new MessageIdSaver();
        System.Collections.Specialized.NameValueCollection appSettings = ConfigurationManager.AppSettings;
        MailSender mailSender;
        LmadminClient client;

        public LmadminMonitorService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {

            // create timer object
            timer = new Timer(Double.Parse(appSettings.Get("lmadmin_interval")));
            timer.Elapsed += new ElapsedEventHandler(timer_elapsed);
            timer.Enabled = true;

            // read last alert ID from persistent store
            string lastId = saver.getLastId();

            // create new lmadmin client
            client = new LmadminClient(this.ServiceName, lastId);

            // optionally create email notification sender
            if (appSettings.Get("send_email").ToLower().Equals("true")) mailSender = new MailSender();

            // make sure event source is registered
            if (!EventLog.SourceExists(this.ServiceName))
                EventLog.CreateEventSource(this.ServiceName, "Application");

        }

        protected override void OnStop()
        {
            // disable timer and persist updated last alert ID
            timer.Enabled = false;
            saver.setLastId(client.LastId);

            // not required ...
            // EventLog.DeleteEventSource("lmadmin_custom");
        }

        void timer_elapsed(object sender, ElapsedEventArgs e)
        {
            // get all alerts - the client makes sure only the latest alers are returned
            LmadminAlert[] alerts = client.getAlerts();

            // iterate over alerts
            for (int i = 0; i < alerts.Length; i++)
            {
                // create human readable representation of the alert
                string logEntry = "Title : " + alerts[i].Title + "\n Description:" + alerts[i].Description + " \n Level: " + 
                    alerts[i].Level + "\n timestamp: " + alerts[i].Timestamp + "\n type: " + alerts[i].Type;
                string emailBody = alerts[i].Description + " \n Level: " + alerts[i].Level + "\n timestamp: " + alerts[i].Timestamp;
                
                // log the event as an Error 
                EventLog.WriteEntry(this.ServiceName, logEntry, EventLogEntryType.Error);

                // optionally send email notification
                if (appSettings.Get("send_email").ToLower().Equals("true"))
                {
                    try
                    {
                        mailSender.sendMail("FlexNet Alert (" + client.LmadminHost + "):" + alerts[i].Title, emailBody);
                    }
                    catch (Exception ex)
                    {
                        // if email sending fails, write a Warning to the Event log
                        EventLog.WriteEntry(this.ServiceName, "Failed to send email notification: " + ex, EventLogEntryType.Warning);
                    }


                }
            }


        }


    }

    public class LmadminMonitorConfigSection : ConfigurationSection
    {

        [ConfigurationProperty("lmadminClient")]
        public LmadminClientConfigElement LmadminClient
        {
            get
            {
                return (LmadminClientConfigElement)this["lmadminClient"];
            }
            set
            {
                this["lmadminClient"] = value;
            }
        }

        [ConfigurationProperty("emailNotification", IsRequired=true)]
        public EmailNotificationElement EmailNotification {
            get
            {
                return (EmailNotificationElement)this["emailNotification"];
            }
            set
            {
                this["emailNotification"] = value;
            }
        }

       
    }

    public class LmadminClientConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("lmadminHost", IsRequired = true)]
        public String LmadminHost
        {
            get
            {
                return (String)this["lmadminHost"];
            }
            set
            {
                this["lmadminHost"] = value;
            }
        }

        [ConfigurationProperty("lmadminPort", IsRequired = true)]
        public String LmadminPort
        {
            get
            {
                return (String)this["lmadminPort"];
            }
            set
            {
                this["lmadminPort"] = value;
            }
        }

        [ConfigurationProperty("lmadminStoreFile", IsRequired = true)]
        public String LmadminStoreFile
        {
            get
            {
                return (String)this["lmadminStoreFile"];
            }
            set
            {
                this["lmadminStoreFile"] = value;
            }
        }

        [ConfigurationProperty("lmadminInterval", IsRequired = true)]
        [IntegerValidator(ExcludeRange = false,  MinValue = 1000)]
        public int LmadminInterval
        {
            get
            {
                return (int)this["lmadminInterval"];
            }
            set
            {
                this["lmadminInterval"] = value;
            }
        }

        [ConfigurationProperty("alertType", IsRequired = true)]
        [ConfigurationCollection(typeof(String), AddItemName="add")]
        public String alertType
        {
            get
            {
                return (String)base["alertType"];
            }
     
        }

    }

    public class EmailNotificationElement : ConfigurationElement
    {
        [ConfigurationProperty("sendEmail", IsRequired=true)]
        public Boolean SendEmail
        {
            get
            {
                return (Boolean)this["sendEmail"];
            }
            set
            {
                this["sendEmail"] = value;
            }
        }

        [ConfigurationProperty("emailHost", IsRequired=true)]
        public String EmailHost
        {
            get
            {
                return (String)this["emailHost"];
            }
            set
            {
                this["emailHost"] = value;
            }
        }

        [ConfigurationProperty("emailPort", IsRequired=true)]
        public String EmailPort { 
            get 
            { 
                return (String)this["emailPort"];
            }
            set
            {
                this["emailPort"] = value ;
            }
        }

        [ConfigurationProperty("emailFrom", IsRequired = true)]
        public String EmailFrom
        {
            get
            {
                return (String)this["emailFrom"];
            }
            set
            {
                this["emailFrom"] = value;
            }
        }

        [ConfigurationProperty("emailTo", IsRequired = true)]
        public String EmailTo
        {
            get
            {
                return (String)this["emailTo"];
            }
            set
            {
                this["emailTo"] = value;
            }
        }

        [ConfigurationProperty("emailUsername", IsRequired = false, DefaultValue="")]
        public String EmailUsername
        {
            get
            {
                return (String)this["emailUsername"];
            }
            set
            {
                this["emailUsername"] = value;
            }
        }

        [ConfigurationProperty("emailPassword", IsRequired = false, DefaultValue="")]
        public String EmailPassword
        {
            get
            {
                return (String)this["emailPassword"];
            }
            set
            {
                this["emailPassword"] = value;
            }
        }

        [ConfigurationProperty("emailSsl", IsRequired = false, DefaultValue = "false")]
        public Boolean EmailSsl
        {
            get
            {
                return (Boolean)this["emailSsl"];
            }
            set
            {
                this["emailSsl"] = value;
            }
        }

     
    }
}
