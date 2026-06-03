using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml.Linq;
using System.Data.Common;
using System.Diagnostics;
using WindowsInput.Native;
using WindowsInput;
using System.IO;

namespace MaruthiBuildProWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Party : ControllerBase
    {
        public static string GetStockItems()
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
                                                     select $name as 'PartyName',$LedgerMobile as 'PhoneNo',$_Address1+' , '+$_Address2 as 'Address' from ledger
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

        [HttpGet]
        public IActionResult ParseXmlResponse()
        {
            try
            {
                bool status = false;

                var TallyPath = ReadTallyConfig("C:\\TallyConfig\\TallyConfig.xml");
                // string tallyProcessName = "tally.exe";

                    string pathToExe = TallyPath.TallyTargetPath;

                //ProcessStartInfo startInfo = new ProcessStartInfo
                //{
                //    FileName = "C:\\Users\\Admin\\source\\repos\\OpenTallyService\\OpenTallyService\\bin\\Debug\\OpenTallyService.exe",
                //    UseShellExecute = false,
                //    RedirectStandardOutput = true,
                //    RedirectStandardError = true,
                //    CreateNoWindow = true
                //};

                ////string taskName = "StartTallyPrime";

                ////ProcessStartInfo startInfo = new ProcessStartInfo
                ////{
                ////    FileName = "schtasks.exe",
                ////    Arguments = $"/run /tn \"{taskName}\"",
                ////    CreateNoWindow = true,
                ////    UseShellExecute = false,
                ////    RedirectStandardOutput = true,
                ////    RedirectStandardError = true
                ////};

                //using (Process process = Process.Start(startInfo))
                //{
                //    // If you need to wait for the process to exit or interact with it, implement it here.
                //    string output = process.StandardOutput.ReadToEnd();
                //    string error = process.StandardError.ReadToEnd();
                //    process.WaitForExit();

                //    // Log the output and error messages
                //    // You can use a logging framework or write to a file
                //    System.IO.File.WriteAllText("C:\\TallyConfig\\ProcessOutput.txt", output);
                //    System.IO.File.WriteAllText("C:\\TallyConfig\\ProcessError.txt", error);
                //}


                //if (IsProcessRunning(pathToExe))
                //{
                //    status = true;
                //}
                //else
                //{
                //    try
                //    {
                //        string createText = "starting...";
                //        System.IO.File.WriteAllText("E:\\error.txt", createText);

                //        ProcessStartInfo info = new ProcessStartInfo(@"C:\Users\Admin\source\repos\OpenTallyService\OpenTallyService\bin\Debug\OpenTallyService.exe");
                //        info.UseShellExecute = true;
                //        info.Verb = "runas";
                //        Process.Start(info);

                //        //Process.Start("C:\\Users\\Admin\\source\\repos\\OpenTallyService\\OpenTallyService\\bin\\Debug\\OpenTallyService.exe");
                //    }
                //    catch (Exception ex)
                //    {
                //        string createText = ex.Message + Environment.NewLine + ex.StackTrace;
                //        System.IO.File.WriteAllText("E:\\error.txt", createText);
                //    }
                //    Thread.Sleep(40000);

                //    status = IsProcessRunning(pathToExe);

                //}
                //if (status)
                //{
                    string xmlResponse = GetStockItems();
                    xmlResponse = Regex.Replace(xmlResponse, @"[^\x20-\x7E]", string.Empty); // Keep only printable ASCII

                    List<object> partyNames = new List<object>();


                    // Display the results
                    XDocument xmlDoc = XDocument.Parse(xmlResponse);

                    // Retrieve and print each row's values
                    foreach (var row in xmlDoc.Descendants("ROW"))
                    {
                        var columns = row.Elements("COL");
                        if (columns != null)
                        {
                            var party = new
                            {
                                PartyName = columns.ElementAt(0).Value,
                                PhoneNo = columns.ElementAt(1).Value,
                                Address = columns.ElementAt(2).Value

                            };
                            partyNames.Add(party);
                        }
                        //Console.WriteLine("Party Name: " + columns.ElementAt(0).Value);


                    }

                    var result = new
                    {
                        partyNames,
                    };

                    string JSONresult;
                    JSONresult = JsonConvert.SerializeObject(result);
                    return Ok(JSONresult);
                //}
                //else
                //{
                //return BadRequest("Tally Service Not Running");
                //}
            }
            catch (Exception ex) { 
            return BadRequest(ex.Message);
            }
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
            try
            {


                Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName));
                foreach (Process Proc in processes)
                {
                    if (Proc.MainWindowHandle != IntPtr.Zero || Proc.MainWindowHandle == IntPtr.Zero && !Proc.HasExited)
                    {
                       
                        return true;
                    }

                }
            }
            catch (Exception ex)
            {
                string createText = ex.Message + Environment.NewLine+ex.StackTrace;
                System.IO.File.WriteAllText("E:\\error.txt", createText);
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
