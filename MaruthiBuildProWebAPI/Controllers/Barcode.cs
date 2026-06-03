using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml.Linq;

namespace MaruthiBuildProWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Barcode : ControllerBase
    {
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
                                                    select $name as 'ItemName', $name[2].name as 'ItemCode', $parent as 'ItemGroup', $MasterId as 'ItemMasterID', $GSTDetails[Last].StateWiseDetails[1].RateDetails[3].GSTRate as 'ItemGSTRate', $Costingmethod as 'ItemCostMethod', $Base Units as 'ItemUOM',$MailingName as 'PartNo',$MRPDetails[Last].MRPRateDetails[Last].MRPRate as 'MRP' from stockitem where $MailingName = '{ItemCode}'
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

        [HttpGet("GetProductDetails/{ItemCode}")]
        public IActionResult ParseXmlResponse(string ItemCode)
        {
            string xmlResponse = GetStockItems(ItemCode);
            xmlResponse = Regex.Replace(xmlResponse, @"[^\x20-\x7E]", string.Empty); // Keep only printable ASCII

            List<object> items = new List<object>();


            // Display the results
            XDocument xmlDoc = XDocument.Parse(xmlResponse);

            // Retrieve and print each row's values
            foreach (var row in xmlDoc.Descendants("ROW"))
            {
                var columns = row.Elements("COL").ToList(); // Convert to a list for easier access
                if (columns.Count >= 2) // Ensure there are at least 2 columns
                {
                    string MRP = columns.ElementAt(8).Value;
                    int index = MRP.IndexOf('/');
                    if (index != -1)
                    {
                        MRP = MRP.Substring(0, index).Trim();
                    }
                    var item = new
                    {
                        ItemName = columns.ElementAt(0).Value,
                        ItemCode = columns.ElementAt(1).Value,
                        ItemGroup = columns.ElementAt(2).Value,
                        Unit = columns.ElementAt(6).Value,
                        PartNo = columns.ElementAt(7).Value,
                        MRPRate = MRP,
                    };
                    items.Add(item);
                }
            }

            var result = new
            {
                items
            };

            string JSONresult = JsonConvert.SerializeObject(result);
            return Ok(JSONresult);
        }

        [HttpGet("GetProductDetailsOnAlias/{Alias}")]
        public IActionResult GetProductDetailsOnAlias(string Alias)
        {
            string xmlResponse = GetStockItemsOnAlias(Alias);
            xmlResponse = Regex.Replace(xmlResponse, @"[^\x20-\x7E]", string.Empty); // Keep only printable ASCII

            List<object> items = new List<object>();


            // Display the results
            XDocument xmlDoc = XDocument.Parse(xmlResponse);

            // Retrieve and print each row's values
            foreach (var row in xmlDoc.Descendants("ROW"))
            {
                var columns = row.Elements("COL").ToList(); // Convert to a list for easier access
                if (columns.Count >= 2) // Ensure there are at least 2 columns
                {
                    string MRP = columns.ElementAt(8).Value;
                    int index = MRP.IndexOf('/');
                    if (index != -1)
                    {
                        MRP = MRP.Substring(0, index).Trim();
                    }
                    var item = new
                    {
                        ItemName = columns.ElementAt(0).Value,
                        ItemCode = columns.ElementAt(1).Value,
                        ItemGroup = columns.ElementAt(2).Value,
                        Unit = columns.ElementAt(6).Value,
                        PartNo = columns.ElementAt(7).Value,
                        MRPRate = MRP,
                    };
                    items.Add(item);
                }
            }

            var result = new
            {
                items
            };

            string JSONresult = JsonConvert.SerializeObject(result);
            return Ok(JSONresult);
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
                                                    select $name as 'ItemName', $name[2].name as 'ItemCode', $parent as 'ItemGroup', $MasterId as 'ItemMasterID', $GSTDetails[Last].StateWiseDetails[1].RateDetails[3].GSTRate as 'ItemGSTRate', $Costingmethod as 'ItemCostMethod', $Base Units as 'ItemUOM',$MailingName as 'PartNo',$MRPDetails[Last].MRPRateDetails[Last].MRPRate as 'MRP' from stockitem where $name[2].name = '{Alias}'
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
    }
}
