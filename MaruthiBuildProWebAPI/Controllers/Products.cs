using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml.Linq;
using System.Text.Json.Serialization;
using System.Data;
using System.Net.Http.Json;
using System.Diagnostics;
using WindowsInput.Native;
using WindowsInput;
using Newtonsoft.Json;
using System.Net.Http;

namespace MaruthiBuildProWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Products : ControllerBase


    {
        private readonly HttpClient _httpClient;

        public Products(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public static string GetStockItems(string ItemCode)
        {
            // Construct the XML request
            string xmlRequest = $@"
                                <ENVELOPE>
                                    <HEADER>
                                        <TALLYREQUEST>Export Data</TALLYREQUEST>
                                    </HEADER>
                                    <BODY>
                                        <EXPORTDATA>
                                            <REQUESTDESC>
                                                <REPORTNAME>ODBC Report</REPORTNAME>
                                                <SQLREQUEST TYPE='General' METHOD='SQLExecute'>
                                                    select $name as 'ItemName', $Base Units as 'ItemUOM',$MailingName as 'PartNo',$MRPDetails[Last].MRPRateDetails[Last].MRPRate as 'MRP',$_ClosingBalance as 'AvailableQty',$name[2].name as 'ItemCode' from stockitem where $MailingName = '{ItemCode}'
                                                </SQLREQUEST>
                                                <STATICVARIABLES>
                                                    <SVEXPORTFORMAT>$$SysName:XML</SVEXPORTFORMAT>
                                                </STATICVARIABLES>
                                            </REQUESTDESC>
                                            <REQUESTDATA></REQUESTDATA>
                                        </EXPORTDATA>
                                    </BODY>
                                </ENVELOPE>";

            // Create HTTP client and send the request
            using (var client = new HttpClient())
            {
                var content = new StringContent(xmlRequest, Encoding.UTF8, "application/xml");

                try
                {
                    var response = client.PostAsync("http://localhost:9000", content).Result; // Synchronously wait for the response
                    response.EnsureSuccessStatusCode();
                    return response.Content.ReadAsStringAsync().Result; // Synchronously wait for the content
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return null;
                }
            }
        }

        [HttpGet("GetProductMRP/{ItemCode}")]
        public async Task<IActionResult> ParseXmlResponse(string ItemCode)
        {
            bool status = false;

            var TallyPath = ReadTallyConfig("C:\\TallyConfig\\TallyConfig.xml");
            // string tallyProcessName = "tally.exe";

            string pathToExe = TallyPath.TallyTargetPath;

            //if (IsProcessRunning(pathToExe))
            //{
            //    status = true;
            //}
            //else
            //{
            //     Process.Start("C:\\Users\\Admin\\source\\repos\\OpenTallyService\\OpenTallyService\\bin\\Debug\\OpenTallyService.exe");
            //    Thread.Sleep(40000);

            //    status = IsProcessRunning(pathToExe);


            //}
            //if (status)
            //{
                string xmlResponse = GetStockItems(ItemCode);
                xmlResponse = Regex.Replace(xmlResponse, @"[^\x20-\x7E]", string.Empty); // Keep only printable ASCII

                string MRP = "";
                string ItemName = "";
                string AvailableQty = "";
                string PartNo = "";
                string ItemAlias = "";

            // Display the results
            XDocument xmlDoc = XDocument.Parse(xmlResponse);

                // Retrieve and print each row's values
                foreach (var row in xmlDoc.Descendants("ROW"))
                {
                    var columns = row.Elements("COL");


                    MRP = columns.ElementAt(3).Value;
                    ItemName = columns.ElementAt(0).Value;
                    ItemAlias = columns.ElementAt(5).Value;
                    AvailableQty = columns.ElementAt(4).Value;
                    PartNo = columns.ElementAt(2).Value;


            }

            int index = MRP.IndexOf('/');
                if (index != -1)
                {
                    MRP = MRP.Substring(0, index).Trim();
                }

            var result = new
            {
                Name = ItemName,
                Alias = ItemAlias,
                PartNo = PartNo,
                MRP = MRP,
                AvailableQuantity = AvailableQty,
            };

            string JSONresult;
                JSONresult = JsonConvert.SerializeObject(result);
                return Ok(JSONresult);
            //}
            //else
            //{
            //    return BadRequest("Tally Service Not Running");
            //}
        }


        public static string GetStockItemsOnAlias(string Alias)
        {
            // Construct the XML request
            string xmlRequest = $@"
                                <ENVELOPE>
                                    <HEADER>
                                        <TALLYREQUEST>Export Data</TALLYREQUEST>
                                    </HEADER>
                                    <BODY>
                                        <EXPORTDATA>
                                            <REQUESTDESC>
                                                <REPORTNAME>ODBC Report</REPORTNAME>
                                                <SQLREQUEST TYPE='General' METHOD='SQLExecute'>
                                                    select $name as 'ItemName',$name[2].name as 'ItemCode', $Base Units as 'ItemUOM',$MailingName as 'PartNo',$MRPDetails[Last].MRPRateDetails[Last].MRPRate as 'MRP',$_ClosingBalance as 'AvailableQty' from stockitem where $name[2].name = '{System.Net.WebUtility.UrlDecode( Alias)}'
                                                </SQLREQUEST>
                                                <STATICVARIABLES>
                                                    <SVEXPORTFORMAT>$$SysName:XML</SVEXPORTFORMAT>
                                                </STATICVARIABLES>
                                            </REQUESTDESC>
                                            <REQUESTDATA></REQUESTDATA>
                                        </EXPORTDATA>
                                    </BODY>
                                </ENVELOPE>";

            // Create HTTP client and send the request
            using (var client = new HttpClient())
            {
                var content = new StringContent(xmlRequest, Encoding.UTF8, "application/xml");

                try
                {
                    var response = client.PostAsync("http://localhost:9000", content).Result; // Synchronously wait for the response
                    response.EnsureSuccessStatusCode();
                    return response.Content.ReadAsStringAsync().Result; // Synchronously wait for the content
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return null;
                }
            }
        }


        [HttpGet("GetProductDetails/{Alias}")]
        public async Task<IActionResult> GetProductDetailsOnAlias(string Alias)
        {
            bool status = false;

            var TallyPath = ReadTallyConfig("C:\\TallyConfig\\TallyConfig.xml");
            // string tallyProcessName = "tally.exe";

            string pathToExe = TallyPath.TallyTargetPath;

            //if (IsProcessRunning(pathToExe))
            //{
            //    status = true;
            //}
            //else
            //{
            //     Process.Start("C:\\Users\\Admin\\source\\repos\\OpenTallyService\\OpenTallyService\\bin\\Debug\\OpenTallyService.exe");
            //    Thread.Sleep(40000);

            //    status = IsProcessRunning(pathToExe);


            //}
            //if (status)
            //{
            string xmlResponse = GetStockItemsOnAlias(Alias);
            xmlResponse = Regex.Replace(xmlResponse, @"[^\x20-\x7E]", string.Empty); // Keep only printable ASCII

            string MRP = "";
            string ItemName = "";
            string AvailableQty = "";
            string PartNo = "";
            string ItemAlias = "";

            // Display the results
            XDocument xmlDoc = XDocument.Parse(xmlResponse);

            // Retrieve and print each row's values
            foreach (var row in xmlDoc.Descendants("ROW"))
            {
                var columns = row.Elements("COL");


                MRP = columns.ElementAt(4).Value;
                ItemName = columns.ElementAt(0).Value;
                ItemAlias = columns.ElementAt(1).Value;
                AvailableQty = columns.ElementAt(5).Value;
                PartNo = columns.ElementAt(3).Value;
            }

            int index = MRP.IndexOf('/');
            if (index != -1)
            {
                MRP = MRP.Substring(0, index).Trim();
            }

            var result = new
            {
                Name = ItemName,
                Alias = ItemAlias,
                PartNo = PartNo,
                MRP = MRP,
                AvailableQuantity = AvailableQty,
            };

            string JSONresult;
            JSONresult = JsonConvert.SerializeObject(result);
            return Ok(JSONresult);
            //}
            //else
            //{
            //    return BadRequest("Tally Service Not Running");
            //}
        }

        public static string GetMasterStockItems()
        {
            // Construct the XML request
            string xmlRequest = $@"
                                <ENVELOPE>
                                    <HEADER>
                                        <TALLYREQUEST>Export Data</TALLYREQUEST>
                                    </HEADER>
                                    <BODY>
                                        <EXPORTDATA>
                                            <REQUESTDESC>
                                                <REPORTNAME>ODBC Report</REPORTNAME>
                                                <SQLREQUEST TYPE='General' METHOD='SQLExecute'>
                                                    select $name as 'ItemName',$MailingName as 'PartNo',$name[2].name as 'ItemCode' from stockitem
                                                </SQLREQUEST>
                                                <STATICVARIABLES>
                                                    <SVEXPORTFORMAT>$$SysName:XML</SVEXPORTFORMAT>
                                                </STATICVARIABLES>
                                            </REQUESTDESC>
                                            <REQUESTDATA></REQUESTDATA>
                                        </EXPORTDATA>
                                    </BODY>
                                </ENVELOPE>";

            // Create HTTP client and send the request
            using (var client = new HttpClient())
            {
                var content = new StringContent(xmlRequest, Encoding.UTF8, "application/xml");

                try
                {
                    var response = client.PostAsync("http://localhost:9000", content).Result; // Synchronously wait for the response
                    response.EnsureSuccessStatusCode();
                    return response.Content.ReadAsStringAsync().Result; // Synchronously wait for the content
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return ex.Message+Environment.NewLine+ex.StackTrace;
                }
            }
        }

        [HttpGet("GetMasterProducts")]
        public IActionResult ParseProductXmlResponse()
        {
            return Ok(JsonConvert.SerializeObject(GetMasterStockItems()));
            //try
            //{
            //    string xmlResponse = GetMasterStockItems();
            //    xmlResponse = Regex.Replace(xmlResponse, @"[^\x20-\x7E]", string.Empty); // Keep only printable ASCII

            //    List<object> items = new List<object>();

            //    // Display the results
            //    XDocument xmlDoc = XDocument.Parse(xmlResponse);

            //    // Retrieve and print each row's values
            //    foreach (var row in xmlDoc.Descendants("ROW"))
            //    {
            //        var columns = row.Elements("COL").ToList(); // Convert to a list for easier access
            //        if (columns.Count >= 2) // Ensure there are at least 2 columns
            //        {
            //            var item = new
            //            {
            //                ItemName = columns.ElementAt(0).Value,
            //                ItemAlias = columns.ElementAt(2).Value,
            //                PartNo = columns.ElementAt(1).Value

            //            };
            //            items.Add(item);
            //        }
            //    }

            //    var result = new
            //    {
            //        items,
            //    };

            //    string JSONresult = JsonConvert.SerializeObject(result);
            //    return Ok(JSONresult);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("Error: " + ex.Message);
            //    return Ok(JsonConvert.SerializeObject(ex.Message + Environment.NewLine + ex.StackTrace));
            //} 
        }

        [HttpGet("company")]
        public async Task<IActionResult> GetCompanyData()
        {
            string url = "http://localhost:9000";

            string xmlRequest = @"
            <ENVELOPE>
                <HEADER>
                    <TALLYREQUEST>Export Data</TALLYREQUEST>
                </HEADER>
                <BODY>
                    <EXPORTDATA>
                        <REQUESTDESC>
                            <REPORTNAME>ODBC Report</REPORTNAME>
                            <SQLREQUEST TYPE='General' METHOD='SQLExecute'>
                                select $name from company
                            </SQLREQUEST>
                            <STATICVARIABLES>
                                <SVEXPORTFORMAT>$$SysName:XML</SVEXPORTFORMAT>
                            </STATICVARIABLES>
                        </REQUESTDESC>
                        <REQUESTDATA></REQUESTDATA>
                    </EXPORTDATA>
                </BODY>
            </ENVELOPE>";

            try
            {
                var content = new StringContent(xmlRequest, Encoding.UTF8, "application/xml");
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                string resultXml = await response.Content.ReadAsStringAsync();
                var jsonResponse = ParseTallyResponse(resultXml);

                return Ok(jsonResponse); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private object ParseTallyResponse(string xmlData)
        {
            try
            {
                XDocument xmlDoc = XDocument.Parse(xmlData);

                var companyNames = xmlDoc.Descendants("ROW")
                    .Select(row => row.Element("COL")?.Value)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList();

                return new { Companies = companyNames };
            }
            catch (Exception ex)
            {
                return new { error = "Failed to parse XML", details = ex.Message };
            }
        }

        [HttpGet("stock-items")]
        public async Task<IActionResult> GetStockItems()
        {
            string url = "http://localhost:9000";

            string xmlRequest = @"
            <ENVELOPE>
                <HEADER>
                    <TALLYREQUEST>Export Data</TALLYREQUEST>
                </HEADER>
                <BODY>
                    <EXPORTDATA>
                        <REQUESTDESC>
                            <REPORTNAME>ODBC Report</REPORTNAME>
                            <SQLREQUEST TYPE='General' METHOD='SQLExecute'>
                                SELECT $Name, $Parent FROM StockItem
                            </SQLREQUEST>
                            <STATICVARIABLES>
                                <SVEXPORTFORMAT>$$SysName:XML</SVEXPORTFORMAT>
                                <SVCURRENTCOMPANY>Croma POS Billing</SVCURRENTCOMPANY>
                            </STATICVARIABLES>
                        </REQUESTDESC>
                        <REQUESTDATA></REQUESTDATA>
                    </EXPORTDATA>
                </BODY>
            </ENVELOPE>";

            try
            {
                var content = new StringContent(xmlRequest, Encoding.UTF8, "application/xml");
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                string resultXml = await response.Content.ReadAsStringAsync();
                var jsonResponse = ParseTallyStockItems(resultXml);

                return Ok(jsonResponse); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private object ParseTallyStockItems(string xmlData)
        {
            try
            {
                XDocument xmlDoc = XDocument.Parse(xmlData);

                var stockItems = xmlDoc.Descendants("ROW")
                    .Select(row => new
                    {
                        Name = row.Elements("COL").ElementAtOrDefault(0)?.Value,
                        Parent = row.Elements("COL").ElementAtOrDefault(1)?.Value
                    })
                    .Where(item => !string.IsNullOrEmpty(item.Name))
                    .ToList();

                return new { StockItems = stockItems };
            }
            catch (Exception ex)
            {
                return new { error = "Failed to parse XML", details = ex.Message };
            }
        }

        [HttpGet("ledger-bank-accounts")]
        public async Task<IActionResult> GetLedgerBankAccounts()
        {
            string url = "http://localhost:9000";

            string xmlRequest = @"
            <ENVELOPE>
                <HEADER>
                    <TALLYREQUEST>Export Data</TALLYREQUEST>
                </HEADER>
                <BODY>
                    <EXPORTDATA>
                        <REQUESTDESC>
                            <REPORTNAME>ODBC Report</REPORTNAME>
                            <SQLREQUEST TYPE='General' METHOD='SQLExecute'>
                                SELECT $Name, $Parent, $_PrimaryGroup FROM Ledger WHERE $_PrimaryGroup LIKE '%Bank Accounts%'
                            </SQLREQUEST>
                            <STATICVARIABLES>
                                <SVEXPORTFORMAT>$$SysName:XML</SVEXPORTFORMAT>
                                <SVCURRENTCOMPANY>Croma POS Billing</SVCURRENTCOMPANY>
                            </STATICVARIABLES>
                        </REQUESTDESC>
                        <REQUESTDATA></REQUESTDATA>
                    </EXPORTDATA>
                </BODY>
            </ENVELOPE>";

            try
            {
                var content = new StringContent(xmlRequest, Encoding.UTF8, "application/xml");
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                string resultXml = await response.Content.ReadAsStringAsync();
                var jsonResponse = ParseLedgerBankAccounts(resultXml);

                return Ok(jsonResponse); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private object ParseLedgerBankAccounts(string xmlData)
        {
            try
            {
                XDocument xmlDoc = XDocument.Parse(xmlData);

                var ledgerAccounts = xmlDoc.Descendants("ROW")
                    .Select(row => new
                    {
                        Name = row.Elements("COL").ElementAtOrDefault(0)?.Value,
                        Parent = row.Elements("COL").ElementAtOrDefault(1)?.Value,
                        PrimaryGroup = row.Elements("COL").ElementAtOrDefault(2)?.Value
                    })
                    .Where(item => !string.IsNullOrEmpty(item.Name))
                    .ToList();

                return new { BankLedgers = ledgerAccounts };
            }
            catch (Exception ex)
            {
                return new { error = "Failed to parse XML", details = ex.Message };
            }
        }

        [HttpGet("ledger-sundry-debtors")]
        public async Task<IActionResult> GetLedgerSundryDebtors()
        {
            string url = "http://localhost:9000";

            string xmlRequest = @"
            <ENVELOPE>
                <HEADER>
                    <TALLYREQUEST>Export Data</TALLYREQUEST>
                </HEADER>
                <BODY>
                    <EXPORTDATA>
                        <REQUESTDESC>
                            <REPORTNAME>ODBC Report</REPORTNAME>
                            <SQLREQUEST TYPE='General' METHOD='SQLExecute'>
                                SELECT $Name, $Parent, $_PrimaryGroup FROM Ledger WHERE $_PrimaryGroup LIKE '%Sundry Debtors%'
                            </SQLREQUEST>
                            <STATICVARIABLES>
                                <SVEXPORTFORMAT>$$SysName:XML</SVEXPORTFORMAT>
                                <SVCURRENTCOMPANY>Croma POS Billing</SVCURRENTCOMPANY>
                            </STATICVARIABLES>
                        </REQUESTDESC>
                        <REQUESTDATA></REQUESTDATA>
                    </EXPORTDATA>
                </BODY>
            </ENVELOPE>";

            try
            {
                var content = new StringContent(xmlRequest, Encoding.UTF8, "application/xml");
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                string resultXml = await response.Content.ReadAsStringAsync();
                var jsonResponse = ParseLedgerSundryDebtors(resultXml);

                return Ok(jsonResponse); // Return JSON instead of XML
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private object ParseLedgerSundryDebtors(string xmlData)
        {
            try
            {
                XDocument xmlDoc = XDocument.Parse(xmlData);

                var ledgerAccounts = xmlDoc.Descendants("ROW")
                    .Select(row => new
                    {
                        Name = row.Elements("COL").ElementAtOrDefault(0)?.Value,
                        Parent = row.Elements("COL").ElementAtOrDefault(1)?.Value,
                        PrimaryGroup = row.Elements("COL").ElementAtOrDefault(2)?.Value
                    })
                    .Where(item => !string.IsNullOrEmpty(item.Name))
                    .ToList();

                return new { SundryDebtors = ledgerAccounts };
            }
            catch (Exception ex)
            {
                return new { error = "Failed to parse XML", details = ex.Message };
            }
        }
        [HttpPost("add-ledger")]
        public async Task<IActionResult> AddLedger([FromBody] LedgerModel ledger)
        {
            string url = "http://localhost:9000";

            string xmlRequest = $@"
        <ENVELOPE>
            <HEADER>
                <TALLYREQUEST>Import Data</TALLYREQUEST>
            </HEADER>
            <BODY>
                <IMPORTDATA>
                    <REQUESTDESC>
                        <REPORTNAME>All Masters</REPORTNAME>
                    </REQUESTDESC>
                    <REQUESTDATA>
                        <TALLYMESSAGE xmlns:UDF='TallyUDF'>
                            <LEDGER NAME='{ledger.Name}' ACTION='Create'>
                                <NAME.LIST>
                                    <NAME>{ledger.Name}</NAME>
                                </NAME.LIST>
                                <PARENT>{ledger.ParentGroup}</PARENT>
                                <OPENINGBALANCE>{ledger.OpeningBalance}</OPENINGBALANCE>
                                <TAXCLASSIFICATIONNAME></TAXCLASSIFICATIONNAME>
                                <ISBILLWISEON>Yes</ISBILLWISEON>
                                <AFFECTSSTOCK>No</AFFECTSSTOCK>
                            </LEDGER>
                        </TALLYMESSAGE>
                    </REQUESTDATA>
                </IMPORTDATA>
            </BODY>
        </ENVELOPE>";

            try
            {
                var content = new StringContent(xmlRequest, Encoding.UTF8, "application/xml");
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                string resultXml = await response.Content.ReadAsStringAsync();
                return Ok(new { message = "Ledger added successfully", response = resultXml });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        public class LedgerModel
    {
        public string Name { get; set; }
        public string ParentGroup { get; set; }
        public string OpeningBalance { get; set; }
    }

    public static bool startTallyServer()
        {
            try
            {
                bool status = false;
                var conf = ReadTallyConfig("C:\\TallyConfig\\TallyConfig.xml");

                string pathToExe = conf.TallyTargetPath;

                if (conf.CmpSelectFromServer.ToLower() == "yes")
                {
                    Console.WriteLine("Selecting Company From Server");
                    Console.WriteLine(conf.TallyPath);
                    // Start Tally process
                    Process.Start(pathToExe);
                    Console.WriteLine($"Successfully started '{pathToExe}'.");
                    Task.Delay(5000);



                    // Use AutoIt to send username and password to Tally login dialog
                    // Use InputSimulator to send username and password to Tally login dialog
                    var inputSimulator = new InputSimulator();

                    async Task KeyPressWithDelay(WindowsInput.Native.VirtualKeyCode keyCode)
                    {
                        inputSimulator.Keyboard.KeyPress(keyCode);
                        await Task.Delay(5000);
                    }

                    // Send keys to navigate to the "Specify Path" button
                    KeyPressWithDelay(WindowsInput.Native.VirtualKeyCode.UP);
                    KeyPressWithDelay(WindowsInput.Native.VirtualKeyCode.UP);
                    KeyPressWithDelay(WindowsInput.Native.VirtualKeyCode.UP);
                    KeyPressWithDelay(WindowsInput.Native.VirtualKeyCode.UP);
                    KeyPressWithDelay(WindowsInput.Native.VirtualKeyCode.UP);


                    // Click on the "Specify Path" button (modify coordinates based on your UI)
                    KeyPressWithDelay(WindowsInput.Native.VirtualKeyCode.RETURN);

                    // Simulate entering the path manually (modify coordinates based on your UI)
                    inputSimulator.Keyboard.TextEntry($"{conf.CurrentCompany}");
                    KeyPressWithDelay(WindowsInput.Native.VirtualKeyCode.RETURN);

                    // Simulate pressing the Enter key

                    // Continue with entering username and password
                    foreach (char usernameChar in conf.TallyUser)
                    {
                        inputSimulator.Keyboard.TextEntry(usernameChar.ToString());
                    }

                    inputSimulator.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.TAB);

                    foreach (char passwordChar in conf.TallyPassword)
                    {
                        inputSimulator.Keyboard.TextEntry(passwordChar.ToString());
                    }

                    inputSimulator.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.RETURN);

                    return status = true;

                }
                else
                {
                    Console.WriteLine("Selecting Company from TallyPath");
                    // Start Tally process
                    Process.Start(pathToExe);
                    Console.WriteLine($"Successfully started '{pathToExe}'.");
                    Task.Delay(5000);



                    // Use AutoIt to send username and password to Tally login dialog
                    // Use InputSimulator to send username and password to Tally login dialog
                    var inputSimulator = new InputSimulator();

                    async Task KeyPressWithDelay(WindowsInput.Native.VirtualKeyCode keyCode)
                    {
                        inputSimulator.Keyboard.KeyPress(keyCode);
                        await Task.Delay(5000); // Adjust the delay time as needed
                    }

                    // Send keys to navigate to the "Specify Path" button
                    KeyPressWithDelay(WindowsInput.Native.VirtualKeyCode.UP);
                    KeyPressWithDelay(WindowsInput.Native.VirtualKeyCode.UP);
                    KeyPressWithDelay(WindowsInput.Native.VirtualKeyCode.UP);
                    KeyPressWithDelay(WindowsInput.Native.VirtualKeyCode.UP);


                    // Click on the "Specify Path" button (modify coordinates based on your UI)
                    KeyPressWithDelay(WindowsInput.Native.VirtualKeyCode.RETURN);
                    string TallyDataPath = conf.TallyPath;
                    // Simulate entering the path manually (modify coordinates based on your UI)
                    foreach (char c in TallyDataPath)
                    {
                        if (c == ' ')
                        {
                            // Simulate pressing the space key
                            inputSimulator.Keyboard.KeyPress(VirtualKeyCode.SPACE);
                        }
                        else
                        {
                            // Simulate pressing other keys
                            inputSimulator.Keyboard.TextEntry(c);
                        }
                        // Delay between key presses to ensure they are processed correctly
                        Task.Delay(10);
                    }
                    KeyPressWithDelay(WindowsInput.Native.VirtualKeyCode.RETURN);

                    // Simulate pressing the Enter key

                    // Continue with entering username and password
                    foreach (char usernameChar in conf.TallyUser)
                    {
                        inputSimulator.Keyboard.TextEntry(usernameChar.ToString());
                    }

                    KeyPressWithDelay(WindowsInput.Native.VirtualKeyCode.TAB);

                    foreach (char passwordChar in conf.TallyPassword)
                    {
                        inputSimulator.Keyboard.TextEntry(passwordChar.ToString());
                    }

                    inputSimulator.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.RETURN);

                    return status = false;

                }
            }

            catch (Exception ex)
            {

                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        private static bool IsProcessRunning(string processName)
        {
            Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName));
            foreach (Process Proc in processes)
            {
                if (Proc.MainWindowHandle != IntPtr.Zero || Proc.MainWindowHandle == IntPtr.Zero && !Proc.HasExited)
                {
                    return true;
                }

            }
            return false;
        }

        private static EmailConfig ReadTallyConfig(string filePath)
        {
            try
            {

                var xml = XDocument.Load(filePath);

                return new EmailConfig
                {
                    CurrentCompany = xml.Element("TallyConfig")?.Element("CurrentCompany")?.Value,
                    TallyTargetPath = xml.Element("TallyConfig")?.Element("TallyTargetPath")?.Value,
                    TallyUser = xml.Element("TallyConfig")?.Element("TallyUser")?.Value,
                    TallyPassword = xml.Element("TallyConfig")?.Element("TallyPassword")?.Value,
                    TallyPath = xml.Element("TallyConfig")?.Element("TallyPath")?.Value,
                    CmpSelectFromServer = xml.Element("TallyConfig")?.Element("CmpSelectFromServer")?.Value,

                };
            }
            catch (Exception ex)
            {
                // Handle exception (e.g., log it)
                Console.WriteLine($"Error reading external config file: {ex.Message}");
                return null;
            }
        }

        public class EmailConfig
        {
            private string _encryptedTallyPassword;

            public string CurrentCompany { get; set; }

            public string TallyTargetPath { get; set; }

            public string TallyUser { get; set; }

            public string TallyPassword { get; set; }

            public string TallyPath { get; set; }

            public string CmpSelectFromServer { get; set; }
        }


        private static void CreateDefaultExternalConfig(string filePath)
        {
            try
            {
                if (!Directory.Exists(new FileInfo(filePath).DirectoryName))
                {
                    Directory.CreateDirectory(new FileInfo(filePath).DirectoryName);
                }

                var defaultConfig = new XDocument(
                    new XElement("TallyConfig",
                        new XElement("Subject", ""),
                        new XElement("Body", ""),
                        new XElement("SVCurrentCompany", ""),
                        new XElement("FromDate", ""),
                        new XElement("ToDate", ""),
                        new XElement("FilePath", ""),
                        new XElement("ccMail", ""),
                        new XElement("Username", ""),
                        new XElement("Password", ""),
                        new XElement("Error_ToMail", ""),
                        new XElement("TallyTargetPath", ""),
                        new XElement("TallyUser", ""),
                        new XElement("TallyPassword", ""),
                        new XElement("TallyPath", ""),
                        new XElement("MachineDate", ""),
                        new XElement("CmpSelectFromServer", ""))
                );

                defaultConfig.Save(filePath);

                Console.WriteLine($"Default external config file created at: {filePath}");
            }
            catch (Exception ex)
            {
                // Handle exception (e.g., log it)
                Console.WriteLine($"Error creating default external config file: {ex.Message}");
            }
        }
    }
}
