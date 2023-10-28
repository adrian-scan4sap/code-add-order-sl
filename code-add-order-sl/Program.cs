using code_add_order_sl.Model;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using code_add_order_sl.Model.Response;
using Newtonsoft.Json.Linq;

namespace code_add_order_sl
{
    internal class Program
    {
        private static string baseUrl = "http://localhost:50001/b1s/v1"; // Replace with your SAP Service Layer base URL


        static void Main(string[] args)
        {
            var sessionId = Login();

            if (!string.IsNullOrEmpty(sessionId))
            {
                AddOrder(sessionId);
                Logout(sessionId);
            }
            else
            {
                Console.WriteLine("Login failed.");
            }

            Console.ReadLine();
        }

        private static string Login()
        {
            // Request details
            string url = $"{baseUrl}/Login";
            LoginRequest loginRequest = new LoginRequest()
            {
                UserName = "manager",
                Password = "corexray",
                CompanyDB = "SBODemoUS"
            };

            // Serialize request body to JSON
            string jsonRequestBody = JsonConvert.SerializeObject(loginRequest);

            // Make the request
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            httpWebRequest.KeepAlive = true;
            httpWebRequest.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            httpWebRequest.ServicePoint.Expect100Continue = false;

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(jsonRequestBody);
            }

            try
            {
                // Call Service Layer
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();

                    // Deserialize success response
                    var responseInstance = JsonConvert.DeserializeObject<LoginResponse>(result);

                    Console.WriteLine("Logged in successfully.");

                    return responseInstance.SessionId;
                }
            }
            catch (Exception ex)
            {
                // Unauthorized, etc.
                Console.WriteLine("Unexpected: " + ex.Message);
            }

            return null;
        }

        private static void AddOrder(string sessionId)
        {
            string postingUrl = $"{baseUrl}/Orders";

            // Create a JSON payload for the new Business Partner
            JObject payload = new JObject
            {
                { "CardCode", "C20000" },
                { "DocDueDate", "2023-11-01" },
                { "DocumentLines", new JArray()
                    { new JObject()
                        {
                            { "LineNum", 0 },
                            { "ItemCode", "A00001" },
                            { "Quantity", 1.0 }
                            // Add more fields as required
                        }
                    }
                }
                // Add more fields as required
            };

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(postingUrl);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.Headers.Add("Cookie", $"B1SESSION={sessionId}");

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(payload.ToString());
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    var result = "";
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        result = streamReader.ReadToEnd();
                    }
                    
                    if (response.StatusCode == HttpStatusCode.Created)
                    {
                        JObject jsonObject = JObject.Parse(result);

                        // Extract the meaningful values
                        int docEntry = (int)jsonObject["DocEntry"];
                        int docNum = (int)jsonObject["DocNum"];

                        Console.WriteLine(string.Format("Sales Order added successfully with DocEntry = {0} and DocNum = {1}", docEntry, docNum));
                    }
                    else
                    {
                        Console.WriteLine("Failed to add Sales Order. Status code: " + response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while adding the Sales Order: " + ex.Message);
            }
        }

        private static void Logout(string sessionId)
        {
            string logoutUrl = $"{baseUrl}/Logout";

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(logoutUrl);
                request.Method = "POST";
                request.Accept = "application/json";
                request.Headers.Add("Cookie", $"B1SESSION={sessionId}");

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.NoContent
                        || response.StatusCode == HttpStatusCode.OK)
                    {
                        Console.WriteLine("Logged out successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Logout failed. Status code: " + response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred during logout: " + ex.Message);
            }
        }
    }
}
