using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Diagnostics;
using System.Xml;

namespace lmadmin_monitor
{
    /***
     * Handles connection to lmadmin and parsing alert results
     */
    class LmadminClient: IDisposable
    {
        // this is hardcoded somewhere in flexnet
        const string LMADMIN_USERNAME = "alerter";
        const string LMADMIN_PASSWORD = "alerter";

        public string LmadminHost;
        public string LastId;

        string _sessionId;
        string _serviceName;

        string[] _alertTypes;

        lmadmin_service.LicenseServerPortTypeClient client;
        System.Collections.Specialized.NameValueCollection appSettings = ConfigurationManager.AppSettings;

        public LmadminClient(string serviceName, string lastId)
        {
            LmadminHost = appSettings.Get("lmadmin_host");
            LastId = lastId;

            _alertTypes = appSettings["alert_types"].Split(',');
            // get the settings
            string port = appSettings.Get("lmadmin_port");
            string url = "http://" + LmadminHost + ":" + port + "/soap";

            // connect to lmadmin and obtain session ID
            client = new lmadmin_service.LicenseServerPortTypeClient("LicenseServer", url);
            _sessionId = client.getSessionId(LMADMIN_USERNAME, LMADMIN_PASSWORD);
            _serviceName = serviceName;

        }

        public LmadminAlert[] getAlerts()
        {
            LinkedList<LmadminAlert> alertList = new LinkedList<LmadminAlert>();
            
            // get all alerts using initialised session ID
            String alerts = client.getAlerts(_sessionId, "");
            if (alerts.StartsWith("ERROR"))
            {
                // the error is most likely an expired session - try to recover
                String logEntry = "Error occured while obtaining alerts from LMADMIN, will try to get new session: " + alerts;
                EventLog.WriteEntry(_serviceName, logEntry);
                _sessionId = client.getSessionId(LMADMIN_USERNAME, LMADMIN_PASSWORD);

                // and obtain alerts again
                alerts = client.getAlerts(_sessionId, "");
            }

            // parse the XML response
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(alerts);
            XmlNodeList alertNodes = doc.GetElementsByTagName("fnplm:alert");
            for (int i = 0; i < alertNodes.Count; i++)
            {
                XmlElement elem = (XmlElement)alertNodes.Item(i);
                String id = elem.GetAttribute("id");

                // only process newer alerts
                if (id.CompareTo(LastId) > 0)
                {

                    LastId = id;
                    string title = elem.GetAttribute("title");
                    string description = elem.GetAttribute("description");
                    string level = elem.GetAttribute("level");
                    string timestamp = elem.GetAttribute("timestamp");
                    string type = elem.GetAttribute("type");

                    // include the current alert if it matches one of the configured types or if wildcard is used
                    if (Array.IndexOf<string>(_alertTypes, type) >= 0 || Array.IndexOf<string>(_alertTypes, "*") >=0)
                    {
                        alertList.AddLast(new LmadminAlert(title, description, level, timestamp, id, type));
                    }
                }

                
            }
            return alertList.ToArray();
        }

        public void Dispose()
        {
            ((IDisposable)client).Dispose();
        }
    }

    class LmadminAlert
    {
        public string Title;
        public string Description;
        public string Level;
        public string Timestamp;
        public string Id;
        public string Type;

        public LmadminAlert(string title, string description, string level, string timestamp, string id, string type)
        {
            Title = title;
            Description = description;
            Level = level;
            Timestamp = timestamp;
            Id = id;
            Type = type;
        }

        
    }
}
