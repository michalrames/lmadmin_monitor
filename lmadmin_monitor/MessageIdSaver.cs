using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lmadmin_monitor
{
    class MessageIdSaver
    {
        string persistentFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\" + System.Configuration.ConfigurationManager.AppSettings.Get("lmadmin_store_file");

        string lastId;

        public MessageIdSaver()
        {
            if (System.IO.File.Exists(persistentFile))
            {
                lastId = System.IO.File.ReadAllText(persistentFile);
            }
            else
            {
                lastId = "";
            }

        }

        public String getLastId()
        {
            return lastId;
        }

        public void setLastId(string id){
            lastId = id;
            System.IO.File.WriteAllText(persistentFile,id);
        }
    }
}
