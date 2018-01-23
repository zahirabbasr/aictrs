using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using RecordTransaction.Models;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Microsoft.Net.Http.Headers;

namespace AICTransactions.Controllers
{
    [Route("api/transaction")]
    public class TransactionController : Controller
    {
        /*
        1. Fix mobile number with 1 for ISD if not already
        2. Add $ with amount only if not added already
        3. Create api to return transaction with csv
        4. Put the code Git 
        */
        private string dataPath = "Data\\Data.json";
        private string configPath = "Data\\Config.json";
        private string logPath = "Data\\Log.json";

        private JObject config;

        public TransactionController()
        {
            config = JObject.Parse(System.IO.File.ReadAllText(configPath));
        }

        private List<string> MsgSources { get { return config["Config"]["MsgSources"].ToList().Select(j => j.ToString()).ToList(); } }

        [HttpGet("{project}")]
        public string GetByProject(string project)
        {
            List<Transaction> transactions = JObject.Parse(System.IO.File.ReadAllText(dataPath))["Data"]["Transactions"].ToObject<List<Transaction>>();
            return string.Join("\n", transactions.Where(t => t.Project.ToLower().Equals(project) || project.ToLower().Equals("all")).Select(tr => tr.ToCSV()).ToArray());
        }

        [HttpGet("{project}/{year}")]
        public string GetByProjectAndYear(string project, string year)
        {
            List<Transaction> transactions = JObject.Parse(System.IO.File.ReadAllText(dataPath))["Data"]["Transactions"].ToObject<List<Transaction>>();
            return string.Join("\n", transactions.Where(t => t.Project.ToLower().Equals(project) || project.ToLower().Equals("all") && DateTime.Parse(t.Timestamp).Year.ToString().Equals(year)).Select(tr => tr.ToCSV()).ToArray());
        }

        [HttpGet("{project}/{year}/{month}")]
        public string GetByProjectAndYearAndMonth(string project, string year, string month)
        {
            List<Transaction> transactions = JObject.Parse(System.IO.File.ReadAllText(dataPath))["Data"]["Transactions"].ToObject<List<Transaction>>();
            return string.Join("\n", transactions.Where(t => t.Project.ToLower().Equals(project) || project.ToLower().Equals("all") 
                                                        && DateTime.Parse(t.Timestamp).Year.ToString().Equals(year)
                                                        && DateTime.Parse(t.Timestamp).Month.ToString().Equals(month)).Select(tr => tr.ToCSV()).ToArray());
        }

        [HttpGet("config")]
        public string GetConfig()
        {
            return JObject.Parse(System.IO.File.ReadAllText(configPath))["Config"].ToString();
        }

        [HttpGet("logs")]
        public string GetLogs()
        {
            return string.Join("\n", JObject.Parse(System.IO.File.ReadAllText(logPath))["Logs"]);
        }

        // POST api/values
        [HttpPost]
        public IActionResult Post()
        {
            string From = Request.Form["From"];
            string To = Request.Form["To"];
            string Text = Request.Form["Text"];

            if (!MsgSources.Contains(From)) return BadRequest();

            string[] textBits = null;

            try
            {
                if (Text.Contains("\n"))
                {
                    textBits = Text.Split('\n');
                }
                else if (Text.Contains(","))
                {
                    textBits = Text.Split(',');
                }
                else if (Text.Contains(" "))
                {
                    textBits = Text.Split(' ');
                }
            }
            catch (Exception e)
            {
                WriteLog("Debug", "Values from Request.Form: " + "From=" + From + "&To=" + To + "&Text=" + Text, DateTime.Now.ToString("yyyy/MM/dd hh:mm tt zzz"));
                WriteLog("Error", "Exception: " + e.Message, DateTime.Now.ToString("yyyy/MM/dd hh:mm tt zzz"));
            }

            string objectResultXml = string.Empty;

            if (textBits.Length == 4)
            {
                //Cleaning the info bits
                textBits[0] = textBits[0].Replace(",", string.Empty).Replace(" ", string.Empty).ToUpper();
                textBits[1] = textBits[1].Replace(",", string.Empty).Replace(" ", string.Empty);
                textBits[2] = textBits[2].Replace(",", string.Empty).Replace(" ", string.Empty);
                textBits[3] = textBits[3].Replace(",", string.Empty).Replace(" ", string.Empty);

                //Formatting the info bits
                textBits[1] = textBits[1].StartsWith("Proj=") ? textBits[1].Replace("Proj=", string.Empty) : textBits[1];
                textBits[2] = textBits[2].StartsWith("$") ? textBits[2].Replace("$", string.Empty) : textBits[2];
                textBits[3] = textBits[3].StartsWith("1") && textBits[3].Length > 10 ? textBits[3] : "1" + textBits[3];

                Transaction transaction = new Transaction()
                {
                    MsgCenter = To,
                    MsgSource = From,

                    TransactionType = textBits[0],
                    Project = textBits[1],
                    Amount = textBits[2],
                    
                    Timestamp = DateTime.Now.ToString("yyyy/MM/dd hh:mm tt zzz")
                };

                switch (transaction.TransactionType.ToLower())
                {
                    case "mov":
                    case "dep":
                        transaction.PaidTo = textBits[3];
                        transaction.Payee = From;

                        objectResultXml = "<Response>" +
                            "<Message dst=\"" + transaction.Payee + "\" src=\"" + transaction.MsgCenter + "\">" +
                            "Thank you for moving $" + transaction.Amount + " forward to " + transaction.PaidTo + ".\n-Al Rasool Center" +
                            "</Message>" +
                            "</Response>";
                        break;
                    case "ent":
                    default:
                        transaction.Payee = textBits[3];
                        transaction.PaidTo = From;

                        objectResultXml = "<Response>" +
                            "<Message dst=\"" + transaction.Payee + "\" src=\"" + transaction.MsgCenter + "\">" +
                            "Thank you for your contribution of $" + transaction.Amount + " towards " + transaction.Project + " project.\n-Al Rasool Center" +
                            "</Message>" +
                            "</Response>";
                        break;
                }                

                JObject data = JObject.Parse(System.IO.File.ReadAllText(dataPath));
                JArray transactions = (JArray)data["Data"]["Transactions"];
                transactions.Add(JObject.FromObject(transaction));
                System.IO.File.WriteAllText(dataPath, data.ToString());
            }
            else
            {
                objectResultXml = "<Response>" +
                "<Message dst=\"" + From + "\" src=\"" + To + "\">" +
                "Something went wrong in your message text. Please check and send it again.\n-Al Rasool Center" +
                "</Message>" +
                "</Response>";
            }

            /*
             { "Text": "Values from Request.Form: From=18015995358&To=12013570984&Text=8018081746,\n$100,\nProj=P1",
                "Timestamp": "2018/01/10 04:26 AM +00:00" } 
             */

            ObjectResult or = new OkObjectResult(objectResultXml);
            or.ContentTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));

            return or;
        }

        private void WriteLog(string logType, string text, string timestamp)
        {
            JObject logs = JObject.Parse(System.IO.File.ReadAllText(logPath));
            ((JArray)logs["Logs"]).Add(JObject.Parse("{ \"Logtype\": \"" + logType + "\", \"Text\": \"" + text + "\", \"Timestamp\":\"" + timestamp + "\" }"));
            System.IO.File.WriteAllText(logPath, logs.ToString());
        }
    }
}