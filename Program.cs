using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Soap;
using System.Text;
using System.Threading.Tasks;

namespace Manager_CrossCast_API
{
    [Serializable]
    class ConnectionDetails
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public bool Secure { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    class Program
    {
        static string kConnectionDetailsFilename = "ConnectionDetails.xml";

        static bool WriteConnectionDetailsToFile(String host, int port, bool secure, String Username, String Password)
        {
            ConnectionDetails connectionDetails = new ConnectionDetails();
            connectionDetails.Host = host;
            connectionDetails.Port = port;
            connectionDetails.Secure = secure;
            connectionDetails.Username = Username;
            connectionDetails.Password = Password;
            IFormatter formatter = new SoapFormatter();
            Stream stream = new FileStream(kConnectionDetailsFilename, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, connectionDetails);
            stream.Close();

            return true;
        }

        static ConnectionDetails ReadConnectionDetailsFromFile()
        {
            IFormatter formatter = new SoapFormatter();
            Stream stream = new FileStream(kConnectionDetailsFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
            ConnectionDetails connectionDetails = (ConnectionDetails)formatter.Deserialize(stream);
            stream.Close();

            return connectionDetails;
        }

        static void Main(string[] args)
        {
            // Load in connection details.
            var connectionDetails = ReadConnectionDetailsFromFile();

            var manager = new Manager.RpcClient()
            {
                Host = connectionDetails.Host,
                //Port = connectionDetails.Port,
                Secure = connectionDetails.Secure,
                Username = connectionDetails.Username,
                Password = connectionDetails.Password
            };

            var response = manager.GetBusinesses();

            foreach (var e in response.Businesses)
            {
                Console.WriteLine(e.Name);
            }

            Console.ReadLine();
        }
    }
}
