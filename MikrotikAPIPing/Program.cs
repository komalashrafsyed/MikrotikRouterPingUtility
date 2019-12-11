using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;
using Nito.AsyncEx;
using System.IO;
using System.Threading;
using System.Net;
using System.Diagnostics;
using System.Data;
using System.Data.OleDb;
using System.Data.Odbc;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using System.ComponentModel;
using OfficeOpenXml;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Configuration;

using tik4net;
using tik4net.Objects.Tool;
using tik4net.Objects;


namespace MikrotikAPIPing
{
    class Program
    {

        private static EventHubMessageSender _eventHubMessageSender;
        private static List<EventMessageModel> _eventMessageModels;

        static private IConfiguration config;

        //private const string eventHubConnectionString = "Endpoint=sb://airjaldipingappns.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=1vr9oBgCU3vPIXiJ4QTGqXQvteBVV3njdXaphgBgFDE=";
        //private const string eventHubName = "airjaldipingapp";
        //private const string blobAccountKey = "4YyPZU5ph3pqRpYUTmPWdcXh9E4DS+YEyW7u/xeKVRWh24JIDf4CTF/vyfSAUvsC0N6VOIXF7C6eGb/dNKfVgA==";

        string fileinblob = config["FILELOCATION_BLOB"];

        static void Main(string[] args)
        {
            Console.WriteLine("Hello Welcome to Mikrotik Router Ping Utility!");
            

            config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true, true)
            .Build();

            string eventHubConnectionString = FetchSecretValueFromKeyVault(GetToken());
            string eventHubName = config["EventHub"];
            string blobAccountKey = FetchBlobKeySecretValueFromKeyVault(GetToken());


            string fileinblob = config["FILELOCATION_BLOB"];

            //if file is in blob storage get it and save it in local file storage
            if (fileinblob == "true")
            {
                string myAccountName = config["BlobStorageAccountName"];
                //string myAccountKey = config["BlobStorageAccountPrimaryKey"];
                string myAccountKey = blobAccountKey;
                string mycontainer = config["BlobStorageContainerName"];
                string myFileName = config["SELECTED_FILENAME"];
                string myFileSavePath = config["LOCAL_FILEPATH"] + "\\" + config["SELECTED_FILENAME"];


                var storageCredentials = new StorageCredentials(myAccountName, myAccountKey);
                var cloudStorageAccount = new CloudStorageAccount(storageCredentials, true);
                var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

                var container = cloudBlobClient.GetContainerReference(mycontainer);
                container.CreateIfNotExistsAsync().Wait();


                var newBlob = container.GetBlockBlobReference(myFileName);
                newBlob.DownloadToFileAsync(myFileSavePath, FileMode.Create).Wait();

            }

            for (int x = 0; x >= 0; x++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var connectionStringBuilder = new EventHubsConnectionStringBuilder(eventHubConnectionString)
                {
                    EntityPath = eventHubName
                };
                _eventHubMessageSender = new EventHubMessageSender(new EventHubConfiguration(eventHubConnectionString, eventHubName));
                _eventMessageModels = new List<EventMessageModel>();
                AsyncContext.Run(() => MainAsyncPing(args));
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Console.Write("\n Finished Ping Results in ... " + elapsedMs + " milliseconds");

                var watchtwo = System.Diagnostics.Stopwatch.StartNew();
                Console.WriteLine("\n Events Sent to Event Hub - {0} are {1} in total", eventHubName, _eventMessageModels.Count());
                AsyncContext.Run(() => MainAsyncEventHub(args));
                watchtwo.Stop();
                var elapsedMstwo = watchtwo.ElapsedMilliseconds;
                Console.Write("\n Finished sending events to Event Hub in ... " + elapsedMstwo + " milliseconds");

                Console.Write(" \n Iteration Number: #" + x + " \n");


                String sleepIntMin = config["PingFrequencyInterval"];
                int numvalSleepInt = 1;

                try
                {
                    numvalSleepInt = Convert.ToInt32(sleepIntMin);
                }
                catch (FormatException e)
                {
                    numvalSleepInt = 1;
                    Console.Write("\n Sleep Interval is not correct, please open appsettings.json to input a valid integer whole number value for minutes");
                }

                int sleepIntMs = numvalSleepInt * 60000;

                Thread.Sleep(sleepIntMs);
                Console.Write(" \n Sleeping for" + numvalSleepInt + "minutes \n");
            }

            Console.WriteLine("\n Press ENTER to exit.");
            Console.ReadLine();

        }



        static async Task MainAsyncEventHub(string[] args)
        {
            //sending events to Event Hub
            await _eventHubMessageSender.SendAsync(_eventMessageModels);
        }



        static async Task MainAsyncPing(string[] args)
        {
            try
            {
                string filename = config["LOCAL_FILEPATH"] + "\\" + config["SELECTED_FILENAME"];

                string ipcolumn = config["SELECTED_IPCOLUMN"];
                string ip2column = config["SELECTED_IPConnectCOLUMN"];
                string usernamecol = config["SELECTED_UsernameCOLUMN"];
                string passwordcol = config["SELECTED_PasswordCOLUMN"];

                int ipColumn = Convert.ToInt32(ipcolumn);
                int ip2Column = Convert.ToInt32(ip2column);
                int usernameCol = Convert.ToInt32(usernamecol); 
                int passwordCol = Convert.ToInt32(passwordcol);

                string selectedfiletype = config["SELECTED_FILE_TYPE"];
                string filetype = "_EXCEL_FILE";

                if (selectedfiletype == "1")
                {
                    filetype = config["_FILE_TYPE1"];
                }
                if (selectedfiletype == "2")
                {
                    filetype = config["_FILE_TYPE2"];
                }

                string[] fieldOne = null;
                string[] fieldTwo = null;
                string[] fieldThree = null;
                string[] fieldFour = null;

                DataTable csvData = null;
                int totalcount = 0;

                if (filetype == "_EXCEL_FILE")
                {
                    fieldOne = ReadDataFrom(filename, ipColumn);
                    fieldTwo = ReadDataFrom(filename, ip2Column);
                    fieldThree = ReadDataFrom(filename, usernameCol);
                    fieldFour = ReadDataFrom(filename, passwordCol);

                    Console.WriteLine("Total IP(s) {0}", fieldOne.Count());

                    for (int row = 1; row < fieldOne.Count(); row++)
                    {
                        Console.WriteLine("{0}. {1}", row, fieldOne[row], fieldTwo[row], fieldThree[row], fieldFour[row]);
                    }
                }
                else if (filetype == "_CSV_FILE")
                {
                    csvData = GetDataTabletFromCSVFile(filename);
                    Console.WriteLine("Hello Welcome to ping utility!");
                    Console.WriteLine("Rows count:" + csvData.Rows.Count);

                    fieldOne = new string[csvData.Rows.Count];
                    fieldTwo = new string[csvData.Rows.Count];
                    fieldThree = new string[csvData.Rows.Count];
                    fieldFour = new string[csvData.Rows.Count];

                   

                    totalcount = csvData.Rows.Count;
                    //***********************************************************************/
                    //** Calling the print csv function to print data read from the csv **/
                    //**********************************************************************/ 
                    printIPList(csvData, ipColumn, ip2Column, usernameCol, passwordCol);
                    int ipnumber = 0;

                    //*********************************************************************************************************/
                    //** Creation of IP array list using a loop - currently being done asynchronously **/
                    //********************************************************************************************************/ 

                    foreach (DataRow dataRow in csvData.Rows)
                    {
                        string sourceaddress = dataRow[ipColumn].ToString();
                        string destaddress = dataRow[ip2Column].ToString();
                        string username = dataRow[usernameCol].ToString();
                        string password = dataRow[passwordCol].ToString();
                        
                        string sourceaddressargs= sourceaddress;
                        string destaddressargs = destaddress;
                        string usernameargs = username;
                        string passwordargs = password;

                        fieldOne[ipnumber] = sourceaddressargs; 
                        fieldTwo[ipnumber] = destaddressargs;
                        fieldThree[ipnumber] = usernameargs;
                        fieldFour[ipnumber] = passwordargs;
                        ipnumber++;
                    }
                }

                await PingMikrotikRoutersAPI(fieldOne, fieldTwo, fieldThree, fieldFour, fieldOne.Count());

                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }


        static async Task PingMikrotikRoutersAPI(IEnumerable<string> sourceAddress, IEnumerable<string> destAddress, IEnumerable<string> username, IEnumerable<string> password, int numberOfRecords)
        {
            var enumeratedsourceips = sourceAddress.ToList();
            var enumerateddestips = destAddress.ToList();
            var enumeratedusername = username.ToList();
            var enumeratedpassword = password.ToList();

            int ipid = 0;

            var tasks = new List<Task<PingReply>>();
            foreach (var ipsourceadd in enumeratedsourceips)
            {
                ITikConnection connection;
                // using (connection = ConnectionFactory.CreateConnection(TikConnectionType.Api_v2))
                using (connection = ConnectionFactory.CreateConnection(TikConnectionType.Api))
                {
                    // connection.Open(IPAddressMikrotikRouterB, "komal", "PSb*j9wv4V5I");
                    try
                    {
                        Console.WriteLine(ipid + ".");
                        Console.WriteLine("sourceIP: " + enumeratedsourceips[ipid]);
                        Console.WriteLine("username " + enumeratedusername[ipid]);
                        Console.WriteLine("password " + enumeratedpassword[ipid]);
                        Console.WriteLine("destIP " + enumerateddestips[ipid]);
                        connection.Open(enumeratedsourceips[ipid], enumeratedusername[ipid], enumeratedpassword[ipid]);

                        List<ToolPing> responseList = new List<ToolPing>();
                        Exception responseException = null;
                        //AutoResetEvent waiter = new AutoResetEvent(false);
                        
                        ITikCommand pingCommand = connection.LoadAsync<ToolPing>(
                          ping =>
                          {
                              try
                              {
                                  Console.WriteLine("*****");
                                  responseList.Add(ping);
                                  Console.WriteLine("average return time: " + ping.AvgRtt);
                                  Console.WriteLine("time to life:" + ping.TimeToLife);
                                  Console.WriteLine("packet loss: " + ping.PacketLoss);
                                  Console.WriteLine("min round trip time: " + ping.MinRtt);
                                  Console.WriteLine("max round trip time: " + ping.MaxRtt);

                                  Console.WriteLine("=====");
                              }
                              catch (Exception m)
                              {
                                  Console.WriteLine(m.InnerException.ToString());
                              }
                          }, //read callback

                          exception => responseException = exception, //exception callback
                            connection.CreateParameter("address", enumerateddestips[ipid]), connection.CreateParameter("count", 1.ToString()), connection.CreateParameter("size", "64"));

                   // connection.CreateParameter("address", enumerateddestips[ipid], connection.CreateParameter("count", 1.ToString()), connection.CreateParameter("size", "64"));



                    //connection.CreateParameter("address", enumerateddestips[ipid]), connection.CreateParameter("count", 1.ToString()), connection.CreateParameter("size", "64"));
                    //   connection.CreateParameter("address", enumerateddestips[ipid]));    
                    Thread.Sleep(100);
                        ipid++;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
                //Console.ReadLine();
               // Thread.Sleep(3000);
                connection.Close();

            }
        }

        public class PingRecord
        {
          
            public string IPOne { get; set; }
            public string IPTwo { get; set; }
            public string userName { get; set; }
            public string password { get; set; }

           
        }





        public class UserToken
        {
            public AutoResetEvent waiter { get; set; }
            public string Destination { get; set; }
            public int ipid { get; set; }
            public DateTime InitiatedTime { get; set; }
            public DateTime ReplyTime { get; set; }
        }


        private static void PingCompletedCallback(object sender, PingCompletedEventArgs e)
        {
            
            // If the operation was canceled, display a message to the user.
            if (e.Cancelled)
            {
                Console.WriteLine("Ping canceled.");

                // Let the main thread resume. 
                // UserToken is the AutoResetEvent object that the main thread 
                // is waiting for.
                ((AutoResetEvent)((UserToken)e.UserState).waiter).Set();
                //((AutoResetEvent)e.UserState).Set();
               
            }

            // If an error occurred, display the exception to the user.
            if (e.Error != null)
            {
                Console.WriteLine("Ping failed:");
                Console.WriteLine(e.Error.ToString());

                // Let the main thread resume. 
               // ((AutoResetEvent)e.UserState).Set();
                ((AutoResetEvent)((UserToken)e.UserState).waiter).Set();

            }

            string result = "test";
            PingReply reply = e.Reply;
            string ipaddress = ((UserToken)e.UserState).Destination;
            int ipid = ((UserToken)e.UserState).ipid;
            Debug.Assert(true, string.Format("Reply from {0}", ((UserToken)e.UserState).Destination));

            if (e.Error != null)
            {
                result = JsonConvert.SerializeObject(reply);
            }
            else
            {
                result = JsonConvert.SerializeObject("Ping failed");
            }

            //result = JsonConvert.SerializeObject(reply);


            //Adding the Ping Reply to the event message list
            _eventMessageModels.Add(new EventMessageModel(reply, ipaddress, ipid));

            //display the reply
            DisplayReply(reply, ipaddress, ipid);
            // Let the main thread resume.
            ((AutoResetEvent)((UserToken)e.UserState).waiter).Set();
           
        }

        //*********************************************************************************************/
        //** Function to print only IP addresss column in the given DataTable**/
        //*********************************************************************************************/ 
        private static void printIPList(DataTable dt, int ipcolumnOne, int ipcolumnTwo, int usernameCol, int passwordCol)
        {

            int number = 1;
            Console.WriteLine("-------");
            foreach (DataRow dataRow in dt.Rows)
            {
                Console.WriteLine(number + ". " + dataRow[ipcolumnOne] + " , "+ dataRow[ipcolumnTwo] + " , " + dataRow[usernameCol] + " , " + dataRow[passwordCol]);
                number++;
            }

            Console.WriteLine("-------");
            return;
        }



        public static void DisplayReply(PingReply reply, string address, int ipid)
        {
            if (reply == null)
                return;
            Console.WriteLine("===============================");
            Console.WriteLine("{0}. Address: {1}", ipid, address);
            Console.WriteLine("ping status: {0}", reply.Status);
            // Console.WriteLine("Address: {0}", reply.Address.ToString());

            if (reply.Status == IPStatus.Success)
            {
                Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
                Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
                Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);
            }
        }


        //*********************************************************************************************/
        //** Function to extract data from csv or excel files and place it in datatable 
        //** which will then be returned to the calling functions **/
        //*********************************************************************************************/ 

        /*========================================*/
        //This function reads csv file from a blob

        /// <summary>
        /// GetCSVBlobData
        /// Gets the CSV file Blob data and returns a string
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="connectionString"></param>
        /// <param name="containerName"></param>
        /// <returns></returns>
        private static string GetCSVBlobData(string filename, string connectionString, string containerName)
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            // Retrieve reference to a blob named "test.csv"
            CloudBlockBlob blockBlobReference = container.GetBlockBlobReference(filename);

            string text;
            using (var memoryStream = new MemoryStream())
            {
                //downloads blob's content to a stream
                blockBlobReference.DownloadToStreamAsync(memoryStream);
                //blockBlobReference.DownloadToStream(memoryStream);

                //puts the byte arrays to a string
                text = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
            }
            return text;
        }


        /*========================================*/
        //This function reads data from an excel file
        /*========================================*/
        static public string[] ReadDataFrom(string workbookFilePath, int IPColumn)
        {
            string[] csvData = null;

            var workbookFileInfo = new FileInfo(workbookFilePath);

            using (ExcelPackage excelPackage = new ExcelPackage(workbookFileInfo))
            {
                var totalWorksheets = excelPackage.Workbook.Worksheets.Count;

                for (int sheetIndex = 1; sheetIndex <= totalWorksheets; sheetIndex++)
                {
                    var worksheet = excelPackage.Workbook.Worksheets[sheetIndex];
                    Console.WriteLine("Worksheet Name : {0}", worksheet.Name);

                    int rowCount = worksheet.Dimension.Rows;
                    int columnCount = worksheet.Dimension.Columns;

                    csvData = new string[worksheet.Dimension.Rows];

                    for (int rowIndex = 1; rowIndex <= rowCount; rowIndex++)
                    {
                        for (int columnIndex = 1; columnIndex <= columnCount; columnIndex++)
                        {
                            if ((columnIndex == IPColumn) && (rowIndex > 1))
                            {
                                var value = worksheet.Cells[rowIndex, columnIndex].Value.ToString();
                                //csvData[rowIndex - 1] = value + port22;
                                csvData[rowIndex - 1] = value;
                                // Console.WriteLine("IPAddress is Column {0}, Row{1} = {2}", columnIndex, rowIndex, value);
                            }
                        }
                    }
                }

            }
            return csvData;
        }

        /*========================================*/
        //This function reads data from a csv file
        /*========================================*/
        private static DataTable GetDataTabletFromCSVFile(string csv_file_path)
        {
            DataTable csvData = new DataTable();
            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(csv_file_path))
                {
                    csvReader.SetDelimiters(new string[] { "," });
                    csvReader.HasFieldsEnclosedInQuotes = true;
                    string[] colFields = csvReader.ReadFields();
                    foreach (string column in colFields)
                    {
                        DataColumn datecolumn = new DataColumn(column);
                        datecolumn.AllowDBNull = true;
                        csvData.Columns.Add(datecolumn);
                    }
                    while (!csvReader.EndOfData)
                    {
                        string[] fieldData = csvReader.ReadFields();
                        //Making empty value as null
                        for (int i = 0; i < fieldData.Length; i++)
                        {
                            if (fieldData[i] == "")
                            {
                                fieldData[i] = null;
                            }
                            //Console.WriteLine(fieldData[i]);
                        }
                        csvData.Rows.Add(fieldData);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("----Could not read the csv file, make sure it is in the proper format-------");
            }
            return csvData;
        }








        //************************************************
        // Key Vault Functions - For reading data from the KeyVault
        //************************************************


        #region "Get Eventhub connectionstring"
        static string GetToken()
        {
            WebRequest request = WebRequest.Create("http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https%3A%2F%2Fvault.azure.net");
            request.Headers.Add("Metadata", "true");
            WebResponse response = request.GetResponse();
            return ParseWebResponse(response, "access_token");
        }

        static string FetchSecretValueFromKeyVault(string token)
        {
            string keyvaulturl = config["KeyVault"];
            string secret = config["Secret"];
            WebRequest kvRequest = WebRequest.Create("https://" + keyvaulturl + "/secrets/" + secret + "?api-version=2016-10-01");
            kvRequest.Headers.Add("Authorization", "Bearer " + token);
            WebResponse kvResponse = kvRequest.GetResponse();
            return ParseWebResponse(kvResponse, "value");
        }

        static string FetchBlobKeySecretValueFromKeyVault(string token)
        {
            string keyvaulturl = config["KeyVault"];
            string secret = config["SecretTwo"];
            WebRequest kvRequest = WebRequest.Create("https://" + keyvaulturl + "/secrets/" + secret + "?api-version=2016-10-01");
            kvRequest.Headers.Add("Authorization", "Bearer " + token);
            WebResponse kvResponse = kvRequest.GetResponse();
            return ParseWebResponse(kvResponse, "value");
        }

        private static string ParseWebResponse(WebResponse response, string tokenName)
        {
            string token = String.Empty;
            using (Stream stream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                String responseString = reader.ReadToEnd();

                JObject joResponse = JObject.Parse(responseString);
                JValue ojObject = (JValue)joResponse[tokenName];
                token = ojObject.Value.ToString();
            }
            return token;
        }
        #endregion



    }
}
