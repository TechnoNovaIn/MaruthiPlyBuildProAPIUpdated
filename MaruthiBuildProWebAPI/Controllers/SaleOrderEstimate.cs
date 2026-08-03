using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Data;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Data.SqlClient;
using WindowsInput;
using WindowsInput.Native;

namespace MaruthiBuildProWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SaleOrderEstimate : ControllerBase
    {
        private readonly string _connectionString;
        public SaleOrderEstimate(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("defaultConnection1");
        }

        [HttpPost]
        public string Post(List<OrderData> orderDataList)
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
            //    Process.Start("C:\\Users\\Admin\\source\\repos\\OpenTallyService\\OpenTallyService\\bin\\Debug\\OpenTallyService.exe");
            //    Thread.Sleep(40000);


            //    status = IsProcessRunning(pathToExe);

            //}


            //if (status)
            //{


                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    int orderID;
                    using (SqlCommand getOrderIDCmd = new SqlCommand("SELECT MAX(OrderID) FROM SaleOrderEstimate", con))
                    {
                        object result = getOrderIDCmd.ExecuteScalar();
                        if (result != DBNull.Value)
                        {
                            orderID = Convert.ToInt32(result) + 1;
                        }
                        else
                        {
                            orderID = 1; // if no orders exist yet
                        }
                    }

                    foreach (var orderData in orderDataList)
                    {
                        string Party = CheckPartyLedger(orderData.Party);
                        if (string.IsNullOrEmpty(Party))
                        {
                            string PartyName = orderData.Party.Replace("&", "&amp;");

                            var response = CreatePartyLedger(orderData.Party);

                        }

                        using (SqlCommand cmd = new SqlCommand("InsertSaleOrderEstimate", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.CommandTimeout = 300;

                            cmd.Parameters.AddWithValue("@OrderID", orderID);
                            cmd.Parameters.AddWithValue("@Date", orderData.Date);
                            cmd.Parameters.AddWithValue("@Party", orderData.Party);
                            cmd.Parameters.AddWithValue("@ItemCode", orderData.ItemCode);
                            cmd.Parameters.AddWithValue("@ItemName", orderData.ItemName);
                            cmd.Parameters.AddWithValue("@Rate", orderData.Rate);
                            cmd.Parameters.AddWithValue("@Quantity", orderData.Quantity);
                            cmd.Parameters.AddWithValue("@Amount", orderData.Amount);
                            cmd.Parameters.AddWithValue("@CB", orderData.CB);
                            cmd.Parameters.AddWithValue("@Reference", orderData.Reference);
                            cmd.Parameters.AddWithValue("@PhoneNo", orderData.PhoneNo);
                            cmd.Parameters.AddWithValue("@Address", orderData.Address);
                            cmd.Parameters.AddWithValue("@Discount", orderData.Discount);
                            cmd.Parameters.AddWithValue("@ItemAlias", orderData.ItemAlias);


                        cmd.ExecuteNonQuery();
                        }

                    }
                    con.Close();
                    status = CreateSaleOrder(orderID.ToString());
                }


                if (status)
                {
                    return "Successfully Inserted Values";
                }
                else
                { return "Failed"; }
            //}
            //else
            //{
            //    return "Tally Service Not Running";
            //}
        }

        public static string CheckPartyLedger(string PartyName)
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
                                                     select $name as 'PartyName' from ledger where $name={PartyName}
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
                    var response = client.PostAsync("http://localhost:9000", content).Result;
                    response.EnsureSuccessStatusCode();
                    var Response = response.Content.ReadAsStringAsync().Result;
                    Response = Regex.Replace(Response, @"[^\x20-\x7E]", string.Empty);

                    string Party = "";


                    // Display the results
                    XDocument xmlDoc = XDocument.Parse(Response);

                    // Retrieve and print each row's values
                    foreach (var row in xmlDoc.Descendants("ROW"))
                    {
                        var columns = row.Elements("COL");
                        Party = columns.ElementAt(0).Value;
                    }
                    return Party;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return null;
                }
            }
        }


        //        public static string CreatePartyLedger(string PartyName)
        //        {
        //            // Construct the XML request
        //            string xmlRequest = $@"

        //<ENVELOPE Action="">
        //  <HEADER>
        //    <VERSION>1</VERSION>
        //    <TALLYREQUEST>IMPORT</TALLYREQUEST>
        //    <TYPE>DATA</TYPE>
        //    <ID>All Masters</ID>
        //  </HEADER>
        //  <BODY>
        //    <DESC>
        //      <STATICVARIABLES />
        //    </DESC>
        //    <DATA>
        //      <TALLYMESSAGE>
        //        <LEDGER Action="">
        //          <NAME>{PartyName}</NAME>
        //          <PARENT>Sundry Debtors</PARENT>
        //          <TAXTYPE>Others</TAXTYPE>
        //          <GSTREGISTRATIONTYPE />
        //          <ADDRESS.LIST />
        //          <LANGUAGENAME.LIST>
        //            <NAME.LIST>
        //              <NAME>{PartyName}</NAME>
        //            </NAME.LIST>
        //          </LANGUAGENAME.LIST>
        //        </LEDGER>
        //      </TALLYMESSAGE>
        //    </DATA>
        //  </BODY>
        //</ENVELOPE>";

        //            // Create HTTP client and send the request
        //            using (var client = new HttpClient())
        //            {
        //                var content = new StringContent(xmlRequest, Encoding.UTF8, "application/xml");

        //                try
        //                {
        //                    var response = client.PostAsync("http://localhost:9000", content).Result;
        //                    response.EnsureSuccessStatusCode();
        //                    var Response = response.Content.ReadAsStringAsync().Result;
        //                    return Response;
        //                }
        //                catch (Exception ex)
        //                {
        //                    Console.WriteLine("Error: " + ex.Message);
        //                    return null;
        //                }
        //            }
        //        }

        private static string CreatePartyLedger(string PartyName)
        {
            var xmlData = $@"
<ENVELOPE Action=''>
  <HEADER>
    <VERSION>1</VERSION>
    <TALLYREQUEST>IMPORT</TALLYREQUEST>
    <TYPE>DATA</TYPE>
    <ID>All Masters</ID>
  </HEADER>
  <BODY>
    <DESC>
      <STATICVARIABLES />
    </DESC>
    <DATA>
      <TALLYMESSAGE>
        <LEDGER Action=''>
          <NAME>{PartyName}</NAME>
          <PARENT>Sundry Debtors</PARENT>
          <TAXTYPE>Others</TAXTYPE>
          <GSTREGISTRATIONTYPE />
          <ADDRESS.LIST />
          <LANGUAGENAME.LIST>
            <NAME.LIST>
              <NAME>{PartyName}</NAME>
            </NAME.LIST>
          </LANGUAGENAME.LIST>
        </LEDGER>
      </TALLYMESSAGE>
    </DATA>
  </BODY>
</ENVELOPE>";

            var httpClient = new HttpClient();

            var content = new StringContent(xmlData, Encoding.UTF8, "application/xml");
            HttpResponseMessage response = httpClient.PostAsync("http://localhost:9000/", content).Result; // Synchronously wait for the response

            response.EnsureSuccessStatusCode(); // Throw if not a success code.

            return response.Content.ReadAsStringAsync().Result; // Synchronously read the response content
        }


        public class OrderData
        {
            public string? Date { get; set; }

            public string? Party { get; set; }

            public string? ItemCode { get; set; }

            public string? ItemName { get; set; }

            public string? Rate { get; set; }

            public string? Quantity { get; set; }

            public string? Amount { get; set; }

            public string? CB { get; set; }

            public string? Reference { get; set; }
            public string? PhoneNo { get; set; }
            public string? Address { get; set; }
            public string? Discount { get; set; }

            public string? ItemAlias { get; set; }



        }

        public static bool CreateSaleOrder(string Order)
        {
            bool status = false;
            //GET CurrentCompany From Tally
            var company = FetchFromTally("localhost");

            //Read and Fetch Current Company from FetchFromTally Method
            string currentCompany = ExtractAndPrintCurrentCompany(company);

            //string connectionString = "Data Source=MARUTHIBUILDPRO\\SQLEXPRESS;Initial Catalog=Maruthi_Ply; User ID=user;Password=MaruthiBuildPro@2026; TrustServerCertificate=True;";
            string connectionString = "Data Source=35.207.234.121;Initial Catalog=Maruthi_Ply;Persist Security Info=True;User ID=user;Password=Mk@5942;";

            DataTable OrderTable = new DataTable();


            try
            {


                SqlConnection con = new SqlConnection(connectionString);
                SqlCommand cmd = new SqlCommand("GetEstimateOrderIDforTally", con);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@OrderID", Order);


                con.Open();
                SqlDataReader reader1;
                reader1 = cmd.ExecuteReader();
                OrderTable.Load(reader1);
                con.Close();

                Console.WriteLine(OrderTable);
            }
            catch (SqlException ex)
            {
                // Handle SQL exceptions
                Console.WriteLine("SQL Error: " + ex.Message);
            }
            catch (Exception ex)
            {
                // Handle other potential exceptions
                Console.WriteLine("Error: " + ex.Message);
            }


            if (OrderTable.Rows.Count > 0)
            {


                foreach (DataRow orderID in OrderTable.Rows)
                {

                    string OrderID = orderID["OrderID"].ToString();
                    //string Location = orderID["Location_Name"].ToString();
                    string Party_Name = orderID["PartyID"].ToString();
                    string GSTIN = "";
                    DateTime Date = Convert.ToDateTime(orderID["Date"]);

                    decimal SumofItemAmt = 0;

                    decimal TotalSGSTAmt = 0;
                    decimal TotalCGSTAmt = 0;

                    string OrderNo = "";


                    ArrayList ItemArray = new ArrayList();


                    DataTable resultTable = new DataTable();

                    try
                    {

                        SqlConnection con = new SqlConnection(connectionString);
                        SqlCommand cmd = new SqlCommand("SearchOnOrderDetailsByID", con);

                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@OrderID", OrderID);

                        con.Open();
                        SqlDataReader reader1;
                        reader1 = cmd.ExecuteReader();
                        resultTable.Load(reader1);
                        con.Close();

                        Console.WriteLine(resultTable);
                    }
                    catch (SqlException ex)
                    {
                        // Handle SQL exceptions
                        Console.WriteLine("SQL Error: " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        // Handle other potential exceptions
                        Console.WriteLine("Error: " + ex.Message);
                    }


                    if (resultTable.Rows.Count > 0)
                    {
                        foreach (DataRow row in resultTable.Rows)
                        {
                            // Extract date and bill number from the current row
                            OrderNo = row["OrderID"].ToString();
                            string ItemName = row["ItemName"].ToString();
                            decimal itemAmount = Convert.ToDecimal(row["TotalAmount"]);
                            string itemQty = row["Quantity"].ToString();
                            DateTime OrderDate = Convert.ToDateTime(row["Date"]);
                            //CustomerName = row["Party_Name"].ToString();
                            string ExclRate = row["ExclRate"].ToString();
                            string MRPPrice = row["MRP"].ToString();
                            decimal SGST = Convert.ToDecimal(row["SGST"]);
                            decimal CGST = Convert.ToDecimal(row["CGST"]);
                            string Discount = row["Discount"].ToString();


                            //string ItemRate = row["Rate"].ToString();

                            //HSN Number Required from Table
                            SumofItemAmt += itemAmount;

                            TotalSGSTAmt += SGST;

                            TotalCGSTAmt += CGST;

                            string FormatOrderDate = OrderDate.ToString("dd-MM-yyyy");


                            string MaterialDetails = @$"
                              <ALLINVENTORYENTRIES.LIST>
       <STOCKITEMNAME>{ItemName}</STOCKITEMNAME>
       <GSTOVRDNISREVCHARGEAPPL>&#4; Not Applicable</GSTOVRDNISREVCHARGEAPPL>
       <GSTOVRDNSTOREDNATURE/>
       <GSTOVRDNTYPEOFSUPPLY>Goods</GSTOVRDNTYPEOFSUPPLY>
       <GSTRATEINFERAPPLICABILITY>As per Masters/Company</GSTRATEINFERAPPLICABILITY>
       <GSTHSNINFERAPPLICABILITY>As per Masters/Company</GSTHSNINFERAPPLICABILITY>
       <ISDEEMEDPOSITIVE>No</ISDEEMEDPOSITIVE>
       <ISGSTASSESSABLEVALUEOVERRIDDEN>No</ISGSTASSESSABLEVALUEOVERRIDDEN>
       <STRDISGSTAPPLICABLE>No</STRDISGSTAPPLICABLE>
       <CONTENTNEGISPOS>No</CONTENTNEGISPOS>
       <ISLASTDEEMEDPOSITIVE>No</ISLASTDEEMEDPOSITIVE>
       <ISAUTONEGATE>No</ISAUTONEGATE>
       <ISCUSTOMSCLEARANCE>No</ISCUSTOMSCLEARANCE>
       <ISTRACKCOMPONENT>No</ISTRACKCOMPONENT>
       <ISTRACKPRODUCTION>No</ISTRACKPRODUCTION>
       <ISPRIMARYITEM>No</ISPRIMARYITEM>
       <ISSCRAP>No</ISSCRAP>
       <RATE>{ExclRate}</RATE>
       <DISCOUNT>{Discount}</DISCOUNT>
       <AMOUNT>{itemAmount}</AMOUNT>
       <ACTUALQTY>{itemQty}</ACTUALQTY>
       <BILLEDQTY> {itemQty}</BILLEDQTY>
       <INCLVATRATE>{MRPPrice}</INCLVATRATE>
       <BATCHALLOCATIONS.LIST>
        <BATCHNAME>Primary Batch</BATCHNAME>
        <INDENTNO>&#4; Not Applicable</INDENTNO>
        <ORDERNO>{OrderNo}</ORDERNO>
        <TRACKINGNUMBER>&#4; Not Applicable</TRACKINGNUMBER>
        <DYNAMICCSTISCLEARED>No</DYNAMICCSTISCLEARED>
        <AMOUNT>{itemAmount}</AMOUNT>
        <ACTUALQTY>{itemQty}</ACTUALQTY>
        <BILLEDQTY>{itemQty}</BILLEDQTY>
        <INCLVATRATE>{MRPPrice}</INCLVATRATE>
        <ORDERDUEDATE JD=""45534"" P=""{OrderDate.ToString("dd-MMM-yy")}"">{OrderDate.ToString("dd-MMM-yy")}</ORDERDUEDATE>
        <ADDITIONALDETAILS.LIST>        </ADDITIONALDETAILS.LIST>
        <VOUCHERCOMPONENTLIST.LIST>        </VOUCHERCOMPONENTLIST.LIST>
       </BATCHALLOCATIONS.LIST>
        <ACCOUNTINGALLOCATIONS.LIST>
        <OLDAUDITENTRYIDS.LIST TYPE=""Number"">
         <OLDAUDITENTRYIDS>-1</OLDAUDITENTRYIDS>
        </OLDAUDITENTRYIDS.LIST>
        <LEDGERNAME></LEDGERNAME>
        <GSTCLASS>&#4; Not Applicable</GSTCLASS>
        <ISDEEMEDPOSITIVE>No</ISDEEMEDPOSITIVE>
        <LEDGERFROMITEM>No</LEDGERFROMITEM>
        <REMOVEZEROENTRIES>No</REMOVEZEROENTRIES>
        <ISPARTYLEDGER>No</ISPARTYLEDGER>
        <GSTOVERRIDDEN>No</GSTOVERRIDDEN>
        <ISGSTASSESSABLEVALUEOVERRIDDEN>No</ISGSTASSESSABLEVALUEOVERRIDDEN>
        <STRDISGSTAPPLICABLE>No</STRDISGSTAPPLICABLE>
        <STRDGSTISPARTYLEDGER>No</STRDGSTISPARTYLEDGER>
        <STRDGSTISDUTYLEDGER>No</STRDGSTISDUTYLEDGER>
        <CONTENTNEGISPOS>No</CONTENTNEGISPOS>
        <ISLASTDEEMEDPOSITIVE>No</ISLASTDEEMEDPOSITIVE>
        <ISCAPVATTAXALTERED>No</ISCAPVATTAXALTERED>
        <ISCAPVATNOTCLAIMED>No</ISCAPVATNOTCLAIMED>
        <AMOUNT>{itemAmount}</AMOUNT>
        <SERVICETAXDETAILS.LIST>        </SERVICETAXDETAILS.LIST>
        <BANKALLOCATIONS.LIST>        </BANKALLOCATIONS.LIST>
        <BILLALLOCATIONS.LIST>        </BILLALLOCATIONS.LIST>
        <INTERESTCOLLECTION.LIST>        </INTERESTCOLLECTION.LIST>
        <OLDAUDITENTRIES.LIST>        </OLDAUDITENTRIES.LIST>
        <ACCOUNTAUDITENTRIES.LIST>        </ACCOUNTAUDITENTRIES.LIST>
        <AUDITENTRIES.LIST>        </AUDITENTRIES.LIST>
        <INPUTCRALLOCS.LIST>        </INPUTCRALLOCS.LIST>
        <DUTYHEADDETAILS.LIST>        </DUTYHEADDETAILS.LIST>
        <EXCISEDUTYHEADDETAILS.LIST>        </EXCISEDUTYHEADDETAILS.LIST>
        <RATEDETAILS.LIST>        </RATEDETAILS.LIST>
        <SUMMARYALLOCS.LIST>        </SUMMARYALLOCS.LIST>
        <CENVATDUTYALLOCATIONS.LIST>        </CENVATDUTYALLOCATIONS.LIST>
        <STPYMTDETAILS.LIST>        </STPYMTDETAILS.LIST>
        <EXCISEPAYMENTALLOCATIONS.LIST>        </EXCISEPAYMENTALLOCATIONS.LIST>
        <TAXBILLALLOCATIONS.LIST>        </TAXBILLALLOCATIONS.LIST>
        <TAXOBJECTALLOCATIONS.LIST>        </TAXOBJECTALLOCATIONS.LIST>
        <TDSEXPENSEALLOCATIONS.LIST>        </TDSEXPENSEALLOCATIONS.LIST>
        <VATSTATUTORYDETAILS.LIST>        </VATSTATUTORYDETAILS.LIST>
        <COSTTRACKALLOCATIONS.LIST>        </COSTTRACKALLOCATIONS.LIST>
        <REFVOUCHERDETAILS.LIST>        </REFVOUCHERDETAILS.LIST>
        <INVOICEWISEDETAILS.LIST>        </INVOICEWISEDETAILS.LIST>
        <VATITCDETAILS.LIST>        </VATITCDETAILS.LIST>
        <ADVANCETAXDETAILS.LIST>        </ADVANCETAXDETAILS.LIST>
        <TAXTYPEALLOCATIONS.LIST>        </TAXTYPEALLOCATIONS.LIST>
       </ACCOUNTINGALLOCATIONS.LIST>
       <DUTYHEADDETAILS.LIST>       </DUTYHEADDETAILS.LIST>
      <RATEDETAILS.LIST>
        <GSTRATEDUTYHEAD>CGST</GSTRATEDUTYHEAD>
        <GSTRATEVALUATIONTYPE>Based on Value</GSTRATEVALUATIONTYPE>
        <GSTRATE> 9</GSTRATE>
       </RATEDETAILS.LIST>
       <RATEDETAILS.LIST>
        <GSTRATEDUTYHEAD>SGST/UTGST</GSTRATEDUTYHEAD>
        <GSTRATEVALUATIONTYPE>Based on Value</GSTRATEVALUATIONTYPE>
        <GSTRATE> 9</GSTRATE>
       </RATEDETAILS.LIST>
       <RATEDETAILS.LIST>
        <GSTRATEDUTYHEAD>IGST</GSTRATEDUTYHEAD>
        <GSTRATEVALUATIONTYPE>Based on Value</GSTRATEVALUATIONTYPE>
        <GSTRATE> 18</GSTRATE>
       </RATEDETAILS.LIST>
       <RATEDETAILS.LIST>
        <GSTRATEDUTYHEAD>Cess</GSTRATEDUTYHEAD>
        <GSTRATEVALUATIONTYPE>&#4; Not Applicable</GSTRATEVALUATIONTYPE>
       </RATEDETAILS.LIST>
       <RATEDETAILS.LIST>
        <GSTRATEDUTYHEAD>State Cess</GSTRATEDUTYHEAD>
        <GSTRATEVALUATIONTYPE>Based on Value</GSTRATEVALUATIONTYPE>
       </RATEDETAILS.LIST>
       <SUPPLEMENTARYDUTYHEADDETAILS.LIST>       </SUPPLEMENTARYDUTYHEADDETAILS.LIST>
       <TAXOBJECTALLOCATIONS.LIST>       </TAXOBJECTALLOCATIONS.LIST>
       <REFVOUCHERDETAILS.LIST>       </REFVOUCHERDETAILS.LIST>
       <EXCISEALLOCATIONS.LIST>       </EXCISEALLOCATIONS.LIST>
       <EXPENSEALLOCATIONS.LIST>       </EXPENSEALLOCATIONS.LIST>
      </ALLINVENTORYENTRIES.LIST>
      ";

                            ItemArray.Add(MaterialDetails);
                        }

                        string CombinedItemList = string.Join("", ItemArray.ToArray());

                        var result = sendRequestToTally(CombinedItemList, currentCompany, SumofItemAmt, OrderID, Party_Name, "MBP TECNO", Date, GSTIN, TotalSGSTAmt, TotalCGSTAmt);

                        var xml = XDocument.Parse(result);
                        var created = (int?)xml.Root.Element("CREATED") ?? 0;
                        var altered = (int?)xml.Root.Element("ALTERED") ?? 0;

                        // Check if either CREATED or ALTERED is 1
                        if (created == 1 || altered == 1)
                        {
                            return status= true;
                        }
                        else
                        {
                            sendMailWithoutAttachment("Error Details: ", xml.ToString());
                            return status=false; 
                        }
                    }
                    else
                    {
                        return status= false;
                    }
                }
                return status;
            }
            else
            {
                return status = false;
            }
        }

        private static string ExtractAndPrintCurrentCompany(string responseBody)
        {
            try
            {
                // Load the XML response into an XDocument
                XDocument doc = XDocument.Parse(responseBody);

                string currentCompanyValue = doc.Descendants("CURRENTCOMPANY")
                                      .Where(cc => cc.Attribute("TYPE")?.Value == "String")
                                      .Select(cc => cc.Value)
                                      .FirstOrDefault();

                return currentCompanyValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting and printing CurrentCompany: {ex.Message}");
                sendMailWithoutAttachment("Error Getting Current Company: ", ex.StackTrace);
                return null; // or throw an exception if needed
            }
        }

        private static void sendMailWithoutAttachment(string subject, string body)
        {
            NetworkCredential basicCredential = new NetworkCredential("ppdbalaji@gmail.com", "uxrsfstcesqhkjca");
            var fromAddress = new MailAddress("ppdbalaji@gmail.com", "Maruthi Build Pro APIs Error");
            var toAddress = new MailAddress("deeresh@technonova.in", "Deeresh G P");
            

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = basicCredential
            };

            var message = new MailMessage(fromAddress, toAddress)
            {

                Subject = subject,
                Body = body
            };
            //message.CC.Add("tumkur.smtc@gmail.com");



            smtp.Send(message);
        }


        public static string GetVoucherTypeByLocation(string location)
        {
            if (string.IsNullOrEmpty(location))
            {
                return "Location not provided";
            }


            switch (location)
            {
                case "AMARAPURA":
                    return "MBP TECNO";
                case "Chikkamangalore":
                    return "MBP TECNO";
                case "HASSAN":
                    return "MBP TECNO";
                //case "HIRIYUR& SIRA":
                //    return "Tamil Nadu";
                case "KUNIGAL":
                    return "MBP TECNO";
                //case "NELAMANGALA":
                //    return "Delhi";
                //case "PAVAGADA, BANGALORE":
                //    return "Delhi";
                case "TIPTUR":
                    return "MBP TECNO";
                case "TUMKUR EAST":
                    return "MBP TECNO";
                //case "TUMKUR RE":
                //    return "Delhi";
                case "TUMKUR WEST":
                    return "MBP TECNO";
                case "MADHIGIRI":
                    return "MBP TECNO";

                default:
                    return "MBP TECNO";
            }
        }



        private static string FetchFromTally(string ip)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"http://{ip}:9000/");
                    request.Content = new StringContent(
                        @"<ENVELOPE>
                <HEADER>
                    <VERSION>1</VERSION>
                    <TALLYREQUEST>Export</TALLYREQUEST>
                    <TYPE>Collection</TYPE>
                    <ID>CompanyInfo</ID>
                </HEADER>
                <BODY>
                    <DESC>
                        <STATICVARIABLES />
                        <TDL>
                            <TDLMESSAGE>
                                <OBJECT NAME=""CurrentCompany"">
                                    <LOCALFORMULA>CurrentCompany:##SVCURRENTCOMPANY</LOCALFORMULA>
                                </OBJECT>
                                <COLLECTION NAME=""CompanyInfo"">
                                    <OBJECTS>CurrentCompany</OBJECTS>
                                </COLLECTION>
                            </TDLMESSAGE>
                        </TDL>
                    </DESC>
                </BODY>
            </ENVELOPE>");

                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

                    HttpResponseMessage response = client.SendAsync(request).Result;
                    response.EnsureSuccessStatusCode();

                    string responseBody = response.Content.ReadAsStringAsync().Result;

                    return responseBody;
                }
            }
            catch (Exception ex)
            {
                sendMailWithoutAttachment("Error Fetching Data", ex.StackTrace);
                return null;
            }

        }
        public class config
        {
            public string SVCURRENTCOMPANY { get; set; }
            public string CMPGSTIN { get; set; }

            public string IPADDRESS { get; set; }
        }

        private static  string sendRequestToTally(string ProductList, string CurrentComapny, decimal TotalAmount, string OrderNo, string CustomerName, string OrderType, DateTime OrderDate, string CustomerGST, decimal SGST, decimal CGST)
        {

            //string OrderDate,string OrderNo,string CustomerName,string CustomerGST,string TotalAmount
            try
            {
                string inputString = CurrentComapny;
                string outputString = inputString.Replace("&", "&amp;");

                string party = CustomerName;
                string PartyName = party.Replace("&", "&amp;");

                HttpClient client = new HttpClient();
                string xmlRequest = $@"<ENVELOPE>
 <HEADER>
  <TALLYREQUEST>Import Data</TALLYREQUEST>
 </HEADER>
 <BODY>
  <IMPORTDATA>
   <REQUESTDESC>
    <REPORTNAME>Vouchers</REPORTNAME>
    <STATICVARIABLES>
     <SVCURRENTCOMPANY>{outputString}</SVCURRENTCOMPANY>
    </STATICVARIABLES>
   </REQUESTDESC>
   <REQUESTDATA>
    <TALLYMESSAGE xmlns:UDF=""TallyUDF"">
     <VOUCHER REMOTEID=""{OrderNo}"" VCHKEY=""eab3453b-6b86-4194-9a30-d918c6391838-0000b1a2:00000008"" VCHTYPE=""{OrderType}"" ACTION=""Create"" OBJVIEW=""Invoice Voucher View"">
      
      <OLDAUDITENTRYIDS.LIST TYPE=""Number"">
       <OLDAUDITENTRYIDS>-1</OLDAUDITENTRYIDS>
      </OLDAUDITENTRYIDS.LIST>
      <DATE>{OrderDate.ToString("yyyyMMdd")}</DATE>
      <VCHSTATUSDATE>{OrderDate.ToString("yyyyMMdd")}</VCHSTATUSDATE>
      <GUID>{OrderNo}</GUID>
      <GSTREGISTRATIONTYPE>Regular</GSTREGISTRATIONTYPE>
      <VATDEALERTYPE>Regular</VATDEALERTYPE>
      <STATENAME>Karnataka</STATENAME>
    
      <COUNTRYOFRESIDENCE>India</COUNTRYOFRESIDENCE>
      <PARTYGSTIN>{CustomerGST}</PARTYGSTIN>
      <PLACEOFSUPPLY>Karnataka</PLACEOFSUPPLY>
      <PARTYNAME>{PartyName}</PARTYNAME>
      <GSTREGISTRATION TAXTYPE=""GST"" TAXREGISTRATION="""">Karnataka Registration</GSTREGISTRATION>
      <VOUCHERTYPENAME>{OrderType}</VOUCHERTYPENAME>
      <PARTYLEDGERNAME>{PartyName}</PARTYLEDGERNAME>
      <VOUCHERNUMBER>{OrderNo}</VOUCHERNUMBER>
      <BASICBUYERNAME>{PartyName}</BASICBUYERNAME>
      <CMPGSTREGISTRATIONTYPE>Regular</CMPGSTREGISTRATIONTYPE>
      <REFERENCE>{OrderNo}</REFERENCE>
      <PARTYMAILINGNAME>{PartyName}</PARTYMAILINGNAME>
      <CONSIGNEEGSTIN>{CustomerGST}</CONSIGNEEGSTIN>
      <CONSIGNEEMAILINGNAME>{PartyName}</CONSIGNEEMAILINGNAME>
      <CONSIGNEESTATENAME>Karnataka</CONSIGNEESTATENAME>
      <CMPGSTSTATE>Karnataka</CMPGSTSTATE>
      <CONSIGNEECOUNTRYNAME>India</CONSIGNEECOUNTRYNAME>
      <BASICBASEPARTYNAME>{PartyName}</BASICBASEPARTYNAME>
      <NUMBERINGSTYLE>Manual</NUMBERINGSTYLE>
      <CSTFORMISSUETYPE>&#4; Not Applicable</CSTFORMISSUETYPE>
      <CSTFORMRECVTYPE>&#4; Not Applicable</CSTFORMRECVTYPE>
      <FBTPAYMENTTYPE>Default</FBTPAYMENTTYPE>
      <PERSISTEDVIEW>Invoice Voucher View</PERSISTEDVIEW>
      <VCHSTATUSTAXADJUSTMENT>Default</VCHSTATUSTAXADJUSTMENT>
      <VCHSTATUSVOUCHERTYPE>{OrderType}</VCHSTATUSVOUCHERTYPE>
      <VCHSTATUSTAXUNIT>Karnataka Registration</VCHSTATUSTAXUNIT>
      <VCHGSTCLASS>&#4; Not Applicable</VCHGSTCLASS>
      <DIFFACTUALQTY>No</DIFFACTUALQTY>
      <ISMSTFROMSYNC>No</ISMSTFROMSYNC>
      <ISDELETED>No</ISDELETED>
      <ISSECURITYONWHENENTERED>No</ISSECURITYONWHENENTERED>
      <ASORIGINAL>No</ASORIGINAL>
      <AUDITED>No</AUDITED>
      <ISCOMMONPARTY>No</ISCOMMONPARTY>
      <FORJOBCOSTING>No</FORJOBCOSTING>
      <ISOPTIONAL>No</ISOPTIONAL>
      <EFFECTIVEDATE>{OrderDate.ToString("yyyyMMdd")}</EFFECTIVEDATE>
      <USEFOREXCISE>No</USEFOREXCISE>
      <ISFORJOBWORKIN>No</ISFORJOBWORKIN>
      <ALLOWCONSUMPTION>No</ALLOWCONSUMPTION>
      <USEFORINTEREST>No</USEFORINTEREST>
      <USEFORGAINLOSS>No</USEFORGAINLOSS>
      <USEFORGODOWNTRANSFER>No</USEFORGODOWNTRANSFER>
      <USEFORCOMPOUND>No</USEFORCOMPOUND>
      <USEFORSERVICETAX>No</USEFORSERVICETAX>
      <ISREVERSECHARGEAPPLICABLE>No</ISREVERSECHARGEAPPLICABLE>
      <ISSYSTEM>No</ISSYSTEM>
      <ISFETCHEDONLY>No</ISFETCHEDONLY>
      <ISGSTOVERRIDDEN>No</ISGSTOVERRIDDEN>
      <ISCANCELLED>No</ISCANCELLED>
      <ISONHOLD>No</ISONHOLD>
      <ISSUMMARY>No</ISSUMMARY>
      <ISECOMMERCESUPPLY>No</ISECOMMERCESUPPLY>
      <ISBOENOTAPPLICABLE>No</ISBOENOTAPPLICABLE>
      <ISGSTSECSEVENAPPLICABLE>No</ISGSTSECSEVENAPPLICABLE>
      <IGNOREEINVVALIDATION>No</IGNOREEINVVALIDATION>
      <CMPGSTISOTHTERRITORYASSESSEE>No</CMPGSTISOTHTERRITORYASSESSEE>
      <PARTYGSTISOTHTERRITORYASSESSEE>No</PARTYGSTISOTHTERRITORYASSESSEE>
      <IRNJSONEXPORTED>No</IRNJSONEXPORTED>
      <IRNCANCELLED>No</IRNCANCELLED>
      <IGNOREGSTCONFLICTINMIG>No</IGNOREGSTCONFLICTINMIG>
      <ISOPBALTRANSACTION>No</ISOPBALTRANSACTION>
      <IGNOREGSTFORMATVALIDATION>No</IGNOREGSTFORMATVALIDATION>
      <ISELIGIBLEFORITC>No</ISELIGIBLEFORITC>
      <UPDATESUMMARYVALUES>No</UPDATESUMMARYVALUES>
      <ISEWAYBILLAPPLICABLE>No</ISEWAYBILLAPPLICABLE>
      <ISDELETEDRETAINED>No</ISDELETEDRETAINED>
      <ISNULL>No</ISNULL>
      <ISEXCISEVOUCHER>No</ISEXCISEVOUCHER>
      <EXCISETAXOVERRIDE>No</EXCISETAXOVERRIDE>
      <USEFORTAXUNITTRANSFER>No</USEFORTAXUNITTRANSFER>
      <ISEXER1NOPOVERWRITE>No</ISEXER1NOPOVERWRITE>
      <ISEXF2NOPOVERWRITE>No</ISEXF2NOPOVERWRITE>
      <ISEXER3NOPOVERWRITE>No</ISEXER3NOPOVERWRITE>
      <IGNOREPOSVALIDATION>No</IGNOREPOSVALIDATION>
      <EXCISEOPENING>No</EXCISEOPENING>
      <USEFORFINALPRODUCTION>No</USEFORFINALPRODUCTION>
      <ISTDSOVERRIDDEN>No</ISTDSOVERRIDDEN>
      <ISTCSOVERRIDDEN>No</ISTCSOVERRIDDEN>
      <ISTDSTCSCASHVCH>No</ISTDSTCSCASHVCH>
      <INCLUDEADVPYMTVCH>No</INCLUDEADVPYMTVCH>
      <ISSUBWORKSCONTRACT>No</ISSUBWORKSCONTRACT>
      <ISVATOVERRIDDEN>No</ISVATOVERRIDDEN>
      <IGNOREORIGVCHDATE>No</IGNOREORIGVCHDATE>
      <ISVATPAIDATCUSTOMS>No</ISVATPAIDATCUSTOMS>
      <ISDECLAREDTOCUSTOMS>No</ISDECLAREDTOCUSTOMS>
      <VATADVANCEPAYMENT>No</VATADVANCEPAYMENT>
      <VATADVPAY>No</VATADVPAY>
      <ISCSTDELCAREDGOODSSALES>No</ISCSTDELCAREDGOODSSALES>
      <ISVATRESTAXINV>No</ISVATRESTAXINV>
      <ISSERVICETAXOVERRIDDEN>No</ISSERVICETAXOVERRIDDEN>
      <ISISDVOUCHER>No</ISISDVOUCHER>
      <ISEXCISEOVERRIDDEN>No</ISEXCISEOVERRIDDEN>
      <ISEXCISESUPPLYVCH>No</ISEXCISESUPPLYVCH>
      <GSTNOTEXPORTED>No</GSTNOTEXPORTED>
      <IGNOREGSTINVALIDATION>No</IGNOREGSTINVALIDATION>
      <ISGSTREFUND>No</ISGSTREFUND>
      <OVRDNEWAYBILLAPPLICABILITY>No</OVRDNEWAYBILLAPPLICABILITY>
      <ISVATPRINCIPALACCOUNT>No</ISVATPRINCIPALACCOUNT>
      <VCHSTATUSISVCHNUMUSED>No</VCHSTATUSISVCHNUMUSED>
      <VCHGSTSTATUSISINCLUDED>No</VCHGSTSTATUSISINCLUDED>
      <VCHGSTSTATUSISUNCERTAIN>No</VCHGSTSTATUSISUNCERTAIN>
      <VCHGSTSTATUSISEXCLUDED>No</VCHGSTSTATUSISEXCLUDED>
      <VCHGSTSTATUSISAPPLICABLE>No</VCHGSTSTATUSISAPPLICABLE>
      <VCHGSTSTATUSISGSTR2BRECONCILED>No</VCHGSTSTATUSISGSTR2BRECONCILED>
      <VCHGSTSTATUSISGSTR2BONLYINPORTAL>No</VCHGSTSTATUSISGSTR2BONLYINPORTAL>
      <VCHGSTSTATUSISGSTR2BONLYINBOOKS>No</VCHGSTSTATUSISGSTR2BONLYINBOOKS>
      <VCHGSTSTATUSISGSTR2BMISMATCH>No</VCHGSTSTATUSISGSTR2BMISMATCH>
      <VCHGSTSTATUSISGSTR2BINDIFFPERIOD>No</VCHGSTSTATUSISGSTR2BINDIFFPERIOD>
      <VCHGSTSTATUSISRETEFFDATEOVERRDN>No</VCHGSTSTATUSISRETEFFDATEOVERRDN>
      <VCHGSTSTATUSISOVERRDN>No</VCHGSTSTATUSISOVERRDN>
      <VCHGSTSTATUSISSTATINDIFFDATE>No</VCHGSTSTATUSISSTATINDIFFDATE>
      <VCHGSTSTATUSISRETINDIFFDATE>No</VCHGSTSTATUSISRETINDIFFDATE>
      <VCHGSTSTATUSMAINSECTIONEXCLUDED>No</VCHGSTSTATUSMAINSECTIONEXCLUDED>
      <VCHGSTSTATUSISBRANCHTRANSFEROUT>No</VCHGSTSTATUSISBRANCHTRANSFEROUT>
      <VCHGSTSTATUSISSYSTEMSUMMARY>No</VCHGSTSTATUSISSYSTEMSUMMARY>
      <VCHSTATUSISUNREGISTEREDRCM>No</VCHSTATUSISUNREGISTEREDRCM>
      <VCHSTATUSISOPTIONAL>No</VCHSTATUSISOPTIONAL>
      <VCHSTATUSISCANCELLED>No</VCHSTATUSISCANCELLED>
      <VCHSTATUSISDELETED>No</VCHSTATUSISDELETED>
      <VCHSTATUSISOPENINGBALANCE>No</VCHSTATUSISOPENINGBALANCE>
      <VCHSTATUSISFETCHEDONLY>No</VCHSTATUSISFETCHEDONLY>
      <PAYMENTLINKHASMULTIREF>No</PAYMENTLINKHASMULTIREF>
      <ISSHIPPINGWITHINSTATE>No</ISSHIPPINGWITHINSTATE>
      <ISOVERSEASTOURISTTRANS>No</ISOVERSEASTOURISTTRANS>
      <ISDESIGNATEDZONEPARTY>No</ISDESIGNATEDZONEPARTY>
      <HASCASHFLOW>No</HASCASHFLOW>
      <ISPOSTDATED>No</ISPOSTDATED>
      <USETRACKINGNUMBER>No</USETRACKINGNUMBER>
      <ISINVOICE>No</ISINVOICE>
      <MFGJOURNAL>No</MFGJOURNAL>
      <HASDISCOUNTS>No</HASDISCOUNTS>
      <ASPAYSLIP>No</ASPAYSLIP>
      <ISCOSTCENTRE>No</ISCOSTCENTRE>
      <ISSTXNONREALIZEDVCH>No</ISSTXNONREALIZEDVCH>
      <ISEXCISEMANUFACTURERON>No</ISEXCISEMANUFACTURERON>
      <ISBLANKCHEQUE>No</ISBLANKCHEQUE>
      <ISVOID>No</ISVOID>
      <ORDERLINESTATUS>No</ORDERLINESTATUS>
      <VATISAGNSTCANCSALES>No</VATISAGNSTCANCSALES>
      <VATISPURCEXEMPTED>No</VATISPURCEXEMPTED>
      <ISVATRESTAXINVOICE>No</ISVATRESTAXINVOICE>
      <VATISASSESABLECALCVCH>No</VATISASSESABLECALCVCH>
      <ISVATDUTYPAID>Yes</ISVATDUTYPAID>
      <ISDELIVERYSAMEASCONSIGNEE>No</ISDELIVERYSAMEASCONSIGNEE>
      <ISDISPATCHSAMEASCONSIGNOR>No</ISDISPATCHSAMEASCONSIGNOR>
      <ISDELETEDVCHRETAINED>No</ISDELETEDVCHRETAINED>
      <CHANGEVCHMODE>No</CHANGEVCHMODE>
      <RESETIRNQRCODE>No</RESETIRNQRCODE>
      <MASTERID> 3820{OrderNo}</MASTERID>
      <VOUCHERKEY>195309342818312</VOUCHERKEY>
      <VOUCHERRETAINKEY>5</VOUCHERRETAINKEY>
      <VOUCHERNUMBERSERIES>Manual</VOUCHERNUMBERSERIES>
      <EWAYBILLDETAILS.LIST>      </EWAYBILLDETAILS.LIST>
      <EXCLUDEDTAXATIONS.LIST>      </EXCLUDEDTAXATIONS.LIST>
      <OLDAUDITENTRIES.LIST>      </OLDAUDITENTRIES.LIST>
      <ACCOUNTAUDITENTRIES.LIST>      </ACCOUNTAUDITENTRIES.LIST>
      <AUDITENTRIES.LIST>      </AUDITENTRIES.LIST>
      <DUTYHEADDETAILS.LIST>      </DUTYHEADDETAILS.LIST>
      <GSTADVADJDETAILS.LIST>      </GSTADVADJDETAILS.LIST>
      {ProductList}
       <CONTRITRANS.LIST>      </CONTRITRANS.LIST>
      <EWAYBILLERRORLIST.LIST>      </EWAYBILLERRORLIST.LIST>
      <IRNERRORLIST.LIST>      </IRNERRORLIST.LIST>
      <HARYANAVAT.LIST>      </HARYANAVAT.LIST>
      <SUPPLEMENTARYDUTYHEADDETAILS.LIST>      </SUPPLEMENTARYDUTYHEADDETAILS.LIST>
      <INVOICEDELNOTES.LIST>      </INVOICEDELNOTES.LIST>
      <INVOICEORDERLIST.LIST>      </INVOICEORDERLIST.LIST>
      <INVOICEINDENTLIST.LIST>      </INVOICEINDENTLIST.LIST>
      <ATTENDANCEENTRIES.LIST>      </ATTENDANCEENTRIES.LIST>
      <ORIGINVOICEDETAILS.LIST>      </ORIGINVOICEDETAILS.LIST>
      <INVOICEEXPORTLIST.LIST>      </INVOICEEXPORTLIST.LIST>
      <LEDGERENTRIES.LIST>
       <OLDAUDITENTRYIDS.LIST TYPE=""Number"">
        <OLDAUDITENTRYIDS>-1</OLDAUDITENTRYIDS>
       </OLDAUDITENTRYIDS.LIST>
       <APPROPRIATEFOR>&#4; Not Applicable</APPROPRIATEFOR>
       <LEDGERNAME>{PartyName}</LEDGERNAME>
       <GSTCLASS>&#4; Not Applicable</GSTCLASS>
       <ISDEEMEDPOSITIVE>Yes</ISDEEMEDPOSITIVE>
       <LEDGERFROMITEM>No</LEDGERFROMITEM>
       <REMOVEZEROENTRIES>No</REMOVEZEROENTRIES>
       <ISPARTYLEDGER>Yes</ISPARTYLEDGER>
       <GSTOVERRIDDEN>No</GSTOVERRIDDEN>
       <ISGSTASSESSABLEVALUEOVERRIDDEN>No</ISGSTASSESSABLEVALUEOVERRIDDEN>
       <STRDISGSTAPPLICABLE>No</STRDISGSTAPPLICABLE>
       <STRDGSTISPARTYLEDGER>No</STRDGSTISPARTYLEDGER>
       <STRDGSTISDUTYLEDGER>No</STRDGSTISDUTYLEDGER>
       <CONTENTNEGISPOS>No</CONTENTNEGISPOS>
       <ISLASTDEEMEDPOSITIVE>Yes</ISLASTDEEMEDPOSITIVE>
       <ISCAPVATTAXALTERED>No</ISCAPVATTAXALTERED>
       <ISCAPVATNOTCLAIMED>No</ISCAPVATNOTCLAIMED>
       <AMOUNT>-{TotalAmount}</AMOUNT>
       <SERVICETAXDETAILS.LIST>       </SERVICETAXDETAILS.LIST>
       <BANKALLOCATIONS.LIST>       </BANKALLOCATIONS.LIST>
       <BILLALLOCATIONS.LIST>       </BILLALLOCATIONS.LIST>
       <INTERESTCOLLECTION.LIST>       </INTERESTCOLLECTION.LIST>
       <OLDAUDITENTRIES.LIST>       </OLDAUDITENTRIES.LIST>
       <ACCOUNTAUDITENTRIES.LIST>       </ACCOUNTAUDITENTRIES.LIST>
       <AUDITENTRIES.LIST>       </AUDITENTRIES.LIST>
       <INPUTCRALLOCS.LIST>       </INPUTCRALLOCS.LIST>
       <DUTYHEADDETAILS.LIST>       </DUTYHEADDETAILS.LIST>
       <EXCISEDUTYHEADDETAILS.LIST>       </EXCISEDUTYHEADDETAILS.LIST>
       <RATEDETAILS.LIST>       </RATEDETAILS.LIST>
       <SUMMARYALLOCS.LIST>       </SUMMARYALLOCS.LIST>
       <CENVATDUTYALLOCATIONS.LIST>       </CENVATDUTYALLOCATIONS.LIST>
       <STPYMTDETAILS.LIST>       </STPYMTDETAILS.LIST>
       <EXCISEPAYMENTALLOCATIONS.LIST>       </EXCISEPAYMENTALLOCATIONS.LIST>
       <TAXBILLALLOCATIONS.LIST>       </TAXBILLALLOCATIONS.LIST>
       <TAXOBJECTALLOCATIONS.LIST>       </TAXOBJECTALLOCATIONS.LIST>
       <TDSEXPENSEALLOCATIONS.LIST>       </TDSEXPENSEALLOCATIONS.LIST>
       <VATSTATUTORYDETAILS.LIST>       </VATSTATUTORYDETAILS.LIST>
       <COSTTRACKALLOCATIONS.LIST>       </COSTTRACKALLOCATIONS.LIST>
       <REFVOUCHERDETAILS.LIST>       </REFVOUCHERDETAILS.LIST>
       <INVOICEWISEDETAILS.LIST>       </INVOICEWISEDETAILS.LIST>
       <VATITCDETAILS.LIST>       </VATITCDETAILS.LIST>
       <ADVANCETAXDETAILS.LIST>       </ADVANCETAXDETAILS.LIST>
       <TAXTYPEALLOCATIONS.LIST>       </TAXTYPEALLOCATIONS.LIST>
      </LEDGERENTRIES.LIST>
      <LEDGERENTRIES.LIST>
       <OLDAUDITENTRYIDS.LIST TYPE=""Number"">
        <OLDAUDITENTRYIDS>-1</OLDAUDITENTRYIDS>
       </OLDAUDITENTRYIDS.LIST>
       <APPROPRIATEFOR>&#4; Not Applicable</APPROPRIATEFOR>
       <ROUNDTYPE>&#4; Not Applicable</ROUNDTYPE>
       <LEDGERNAME>OUTPUT CGST 9%</LEDGERNAME>
       <GSTCLASS>&#4; Not Applicable</GSTCLASS>
       <ISDEEMEDPOSITIVE>No</ISDEEMEDPOSITIVE>
       <LEDGERFROMITEM>No</LEDGERFROMITEM>
       <REMOVEZEROENTRIES>No</REMOVEZEROENTRIES>
       <ISPARTYLEDGER>No</ISPARTYLEDGER>
       <GSTOVERRIDDEN>No</GSTOVERRIDDEN>
       <ISGSTASSESSABLEVALUEOVERRIDDEN>No</ISGSTASSESSABLEVALUEOVERRIDDEN>
       <STRDISGSTAPPLICABLE>No</STRDISGSTAPPLICABLE>
       <STRDGSTISPARTYLEDGER>No</STRDGSTISPARTYLEDGER>
       <STRDGSTISDUTYLEDGER>No</STRDGSTISDUTYLEDGER>
       <CONTENTNEGISPOS>No</CONTENTNEGISPOS>
       <ISLASTDEEMEDPOSITIVE>No</ISLASTDEEMEDPOSITIVE>
       <ISCAPVATTAXALTERED>No</ISCAPVATTAXALTERED>
       <ISCAPVATNOTCLAIMED>No</ISCAPVATNOTCLAIMED>
       <AMOUNT>{CGST}</AMOUNT>
       <VATEXPAMOUNT>{CGST}</VATEXPAMOUNT>
       <SERVICETAXDETAILS.LIST>       </SERVICETAXDETAILS.LIST>
       <BANKALLOCATIONS.LIST>       </BANKALLOCATIONS.LIST>
       <BILLALLOCATIONS.LIST>       </BILLALLOCATIONS.LIST>
       <INTERESTCOLLECTION.LIST>       </INTERESTCOLLECTION.LIST>
       <OLDAUDITENTRIES.LIST>       </OLDAUDITENTRIES.LIST>
       <ACCOUNTAUDITENTRIES.LIST>       </ACCOUNTAUDITENTRIES.LIST>
       <AUDITENTRIES.LIST>       </AUDITENTRIES.LIST>
       <INPUTCRALLOCS.LIST>       </INPUTCRALLOCS.LIST>
       <DUTYHEADDETAILS.LIST>       </DUTYHEADDETAILS.LIST>
       <EXCISEDUTYHEADDETAILS.LIST>       </EXCISEDUTYHEADDETAILS.LIST>
       <RATEDETAILS.LIST>       </RATEDETAILS.LIST>
       <SUMMARYALLOCS.LIST>       </SUMMARYALLOCS.LIST>
       <CENVATDUTYALLOCATIONS.LIST>       </CENVATDUTYALLOCATIONS.LIST>
       <STPYMTDETAILS.LIST>       </STPYMTDETAILS.LIST>
       <EXCISEPAYMENTALLOCATIONS.LIST>       </EXCISEPAYMENTALLOCATIONS.LIST>
       <TAXBILLALLOCATIONS.LIST>       </TAXBILLALLOCATIONS.LIST>
       <TAXOBJECTALLOCATIONS.LIST>       </TAXOBJECTALLOCATIONS.LIST>
       <TDSEXPENSEALLOCATIONS.LIST>       </TDSEXPENSEALLOCATIONS.LIST>
       <VATSTATUTORYDETAILS.LIST>       </VATSTATUTORYDETAILS.LIST>
       <COSTTRACKALLOCATIONS.LIST>       </COSTTRACKALLOCATIONS.LIST>
       <REFVOUCHERDETAILS.LIST>       </REFVOUCHERDETAILS.LIST>
       <INVOICEWISEDETAILS.LIST>       </INVOICEWISEDETAILS.LIST>
       <VATITCDETAILS.LIST>       </VATITCDETAILS.LIST>
       <ADVANCETAXDETAILS.LIST>       </ADVANCETAXDETAILS.LIST>
       <TAXTYPEALLOCATIONS.LIST>       </TAXTYPEALLOCATIONS.LIST>
      </LEDGERENTRIES.LIST>
      <LEDGERENTRIES.LIST>
       <OLDAUDITENTRYIDS.LIST TYPE=""Number"">
        <OLDAUDITENTRYIDS>-1</OLDAUDITENTRYIDS>
       </OLDAUDITENTRYIDS.LIST>
       <APPROPRIATEFOR>&#4; Not Applicable</APPROPRIATEFOR>
       <ROUNDTYPE>&#4; Not Applicable</ROUNDTYPE>
       <LEDGERNAME>OUTPUT SGST 9%</LEDGERNAME>
       <GSTCLASS>&#4; Not Applicable</GSTCLASS>
       <ISDEEMEDPOSITIVE>No</ISDEEMEDPOSITIVE>
       <LEDGERFROMITEM>No</LEDGERFROMITEM>
       <REMOVEZEROENTRIES>No</REMOVEZEROENTRIES>
       <ISPARTYLEDGER>No</ISPARTYLEDGER>
       <GSTOVERRIDDEN>No</GSTOVERRIDDEN>
       <ISGSTASSESSABLEVALUEOVERRIDDEN>No</ISGSTASSESSABLEVALUEOVERRIDDEN>
       <STRDISGSTAPPLICABLE>No</STRDISGSTAPPLICABLE>
       <STRDGSTISPARTYLEDGER>No</STRDGSTISPARTYLEDGER>
       <STRDGSTISDUTYLEDGER>No</STRDGSTISDUTYLEDGER>
       <CONTENTNEGISPOS>No</CONTENTNEGISPOS>
       <ISLASTDEEMEDPOSITIVE>No</ISLASTDEEMEDPOSITIVE>
       <ISCAPVATTAXALTERED>No</ISCAPVATTAXALTERED>
       <ISCAPVATNOTCLAIMED>No</ISCAPVATNOTCLAIMED>
       <AMOUNT>{SGST}</AMOUNT>
       <VATEXPAMOUNT>{SGST}</VATEXPAMOUNT>
       <SERVICETAXDETAILS.LIST>       </SERVICETAXDETAILS.LIST>
       <BANKALLOCATIONS.LIST>       </BANKALLOCATIONS.LIST>
       <BILLALLOCATIONS.LIST>       </BILLALLOCATIONS.LIST>
       <INTERESTCOLLECTION.LIST>       </INTERESTCOLLECTION.LIST>
       <OLDAUDITENTRIES.LIST>       </OLDAUDITENTRIES.LIST>
       <ACCOUNTAUDITENTRIES.LIST>       </ACCOUNTAUDITENTRIES.LIST>
       <AUDITENTRIES.LIST>       </AUDITENTRIES.LIST>
       <INPUTCRALLOCS.LIST>       </INPUTCRALLOCS.LIST>
       <DUTYHEADDETAILS.LIST>       </DUTYHEADDETAILS.LIST>
       <EXCISEDUTYHEADDETAILS.LIST>       </EXCISEDUTYHEADDETAILS.LIST>
       <RATEDETAILS.LIST>       </RATEDETAILS.LIST>
       <SUMMARYALLOCS.LIST>       </SUMMARYALLOCS.LIST>
       <CENVATDUTYALLOCATIONS.LIST>       </CENVATDUTYALLOCATIONS.LIST>
       <STPYMTDETAILS.LIST>       </STPYMTDETAILS.LIST>
       <EXCISEPAYMENTALLOCATIONS.LIST>       </EXCISEPAYMENTALLOCATIONS.LIST>
       <TAXBILLALLOCATIONS.LIST>       </TAXBILLALLOCATIONS.LIST>
       <TAXOBJECTALLOCATIONS.LIST>       </TAXOBJECTALLOCATIONS.LIST>
       <TDSEXPENSEALLOCATIONS.LIST>       </TDSEXPENSEALLOCATIONS.LIST>
       <VATSTATUTORYDETAILS.LIST>       </VATSTATUTORYDETAILS.LIST>
       <COSTTRACKALLOCATIONS.LIST>       </COSTTRACKALLOCATIONS.LIST>
       <REFVOUCHERDETAILS.LIST>       </REFVOUCHERDETAILS.LIST>
       <INVOICEWISEDETAILS.LIST>       </INVOICEWISEDETAILS.LIST>
       <VATITCDETAILS.LIST>       </VATITCDETAILS.LIST>
       <ADVANCETAXDETAILS.LIST>       </ADVANCETAXDETAILS.LIST>
       <TAXTYPEALLOCATIONS.LIST>       </TAXTYPEALLOCATIONS.LIST>
      </LEDGERENTRIES.LIST>
      <GST.LIST>      </GST.LIST>
      <STKJRNLADDLCOSTDETAILS.LIST>      </STKJRNLADDLCOSTDETAILS.LIST>
      <PAYROLLMODEOFPAYMENT.LIST>      </PAYROLLMODEOFPAYMENT.LIST>
      <ATTDRECORDS.LIST>      </ATTDRECORDS.LIST>
      <GSTEWAYCONSIGNORADDRESS.LIST>      </GSTEWAYCONSIGNORADDRESS.LIST>
      <GSTEWAYCONSIGNEEADDRESS.LIST>      </GSTEWAYCONSIGNEEADDRESS.LIST>
      <TEMPGSTRATEDETAILS.LIST>      </TEMPGSTRATEDETAILS.LIST>
      <TEMPGSTADVADJUSTED.LIST>      </TEMPGSTADVADJUSTED.LIST>
     </VOUCHER>
    </TALLYMESSAGE>
   </REQUESTDATA>
  </IMPORTDATA>
 </BODY>
</ENVELOPE>";

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"http://localhost:9000/");
                request.Content = new StringContent(xmlRequest, Encoding.UTF8, "application/xml");
                HttpResponseMessage response = client.SendAsync(request).Result;
                response.EnsureSuccessStatusCode();
                string responseBody =  response.Content.ReadAsStringAsync().Result;
                Console.WriteLine("Response from Tally after sending the request:");
                Console.WriteLine(responseBody);

                return responseBody;
            }
            catch (Exception ex)
            {
                sendMailWithoutAttachment("Error Sending Request to Tally", ex.StackTrace);
                return null;
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

                    return status=true;

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

                    return status=false;

                }
            }

            catch (Exception ex)
            {

                Console.WriteLine($"Error: {ex.Message}");
            return  false;
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
