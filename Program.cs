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

    [Serializable]
    class QueryDetails
    {
        public string BusinessName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    class Program
    {
        static String kConnectionDetailsFilename = "ConnectionDetails.xml";
        static String kQueryDetailsFilename = "QueryDetails.xml";

        static bool WriteConnectionDetailsToFile(String host, int port, bool secure, String Username, String Password)
        {
            var connectionDetails = new ConnectionDetails();
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

        static bool WriteQueryDetailsToFile(String businessName, DateTime startDate, DateTime endDate)
        {
            var queryDetails = new QueryDetails();
            queryDetails.BusinessName = businessName;
            queryDetails.StartDate = startDate;
            queryDetails.EndDate = endDate;
            IFormatter formatter = new SoapFormatter();
            Stream stream = new FileStream(kQueryDetailsFilename, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, queryDetails);
            stream.Close();

            return true;
        }

        static QueryDetails ReadQueryDetailsFromFile()
        {
            IFormatter formatter = new SoapFormatter();
            Stream stream = new FileStream(kQueryDetailsFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
            QueryDetails queryDetails = (QueryDetails)formatter.Deserialize(stream);
            stream.Close();

            return queryDetails;
        }

        static void Main(string[] args)
        {
            // Load in connection details.
            var connectionDetails = ReadConnectionDetailsFromFile();

            // Load in query details.
            var queryDetails = ReadQueryDetailsFromFile();

            var manager = new Manager.RpcClient()
            {
                Host = connectionDetails.Host,
                //Port = connectionDetails.Port,
                Secure = connectionDetails.Secure,
                Username = connectionDetails.Username,
                Password = connectionDetails.Password
            };

            var response = manager.GetBusinesses();

            System.Guid? businessKey = null;
            foreach (var e in response.Businesses)
            {
                if (e.Name.CompareTo(queryDetails.BusinessName) == 0)
                    businessKey = e.Key;
            }

            if (businessKey == null)
            {
                Console.WriteLine("Could not find business {0}", queryDetails.BusinessName);
                Console.ReadLine();
                return;
            }

            var getBankAccountsRequest = new Manager.GetBankAccountsRequest();
            getBankAccountsRequest.BusinessID = businessKey.Value;
            var getBankAccountsResponse = manager.GetBankAccounts(getBankAccountsRequest);
            if (!getBankAccountsResponse.OK)
            {
                Console.WriteLine("Bad response whilst getting bank accounts");
                Console.ReadLine();
                return;
            }
            foreach (var bankAccount in getBankAccountsResponse.BankAccounts)
            {
                Console.WriteLine("Bank account : {0}", bankAccount.Name);
            }

            var getTransactionRequest = new Manager.GetTransactionsRequest()
            {
                BusinessID = businessKey.Value,
                From = queryDetails.StartDate,
                To = queryDetails.EndDate
            };
            var getTransactionsResponse = manager.GetTransactions(getTransactionRequest);
            if (!getTransactionsResponse.OK)
            {
                Console.WriteLine("Bad response whilst getting transactions");
                Console.ReadLine();
                return;
            }

            Dictionary<String, List<Manager.GetTransactionsResponse.Transaction>> categoryMap = new Dictionary<String,List<Manager.GetTransactionsResponse.Transaction>>();

            foreach ( Manager.GetTransactionsResponse.Transaction transaction in getTransactionsResponse.Transactions )
            {
                if (!categoryMap.ContainsKey(transaction.Account))
                    categoryMap.Add(transaction.Account, new List<Manager.GetTransactionsResponse.Transaction>());
                categoryMap[transaction.Account].Add(transaction);
            }

            // Acquire keys and sort them.
            var list = categoryMap.Keys.ToList();
            list.Sort();

            foreach (Manager.GetTransactionsResponse.Transaction transaction in getTransactionsResponse.Transactions)
            {
                if (!categoryMap.ContainsKey(transaction.Account))
                    categoryMap.Add(transaction.Account, new List<Manager.GetTransactionsResponse.Transaction>());
                categoryMap[transaction.Account].Add(transaction);
                String line = String.Format("{0} - {1} - {2} - {3} - {4}", transaction.Date.ToString(), transaction.Contact, transaction.Description, transaction.Reference, transaction.Amount);
                foreach ( var item in list )
                {
                    if (item.CompareTo(transaction.Account) == 0)
                    {
                        line += " - ";
                        line += transaction.Amount;
                    }
                    else
                    {
                        line += " - ";
                    }
                }

                Console.WriteLine(line);
            }
            

            Console.ReadLine();
        }
    }
}
