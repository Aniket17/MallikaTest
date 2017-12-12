//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Text;
//using System.Threading.Tasks;
//using System.Web.Script.Serialization;
//using System.Text.RegularExpressions;
//using System.Configuration;
//using System.Data.Entity;
//using System.Diagnostics;

//namespace emBrokerExport
//{
//    class Program
//    {
//        #region 

//        public static string commonUrl = ConfigurationManager.AppSettings["serverUrl"];
//        public static string crDuns = ConfigurationManager.AppSettings["crDuns"];
//        public static string empowerUrl = ConfigurationManager.AppSettings["empowerUrl"];
//        public static string clientID = ConfigurationManager.AppSettings["clientID"];
//        public static string sitecodeStatusInterval = ConfigurationManager.AppSettings["sitecodeStatusInterval"];

//        #endregion

//        static void Main(string[] args)
//        {
//            #region Token

//            var tok = string.Empty;
//            string tokenUrl = commonUrl + "Token";
//            string userName = ConfigurationManager.AppSettings["adminUserID"];
//            string password = ConfigurationManager.AppSettings["adminPassword"];
//            string grantType = ConfigurationManager.AppSettings["grantType"];

//            var jsonToSend = "userName=" + userName + "&password=" + password + "&grant_type=" + grantType;

//            TokenResult result = new TokenResult();
//            try
//            {
//                byte[] postBytes = Encoding.UTF8.GetBytes(jsonToSend);

//                WebRequest request = (HttpWebRequest)WebRequest.Create(tokenUrl);
//                request.Method = "POST";
//                request.ContentType = "application/x-www-form-urlencoded";
//                request.ContentLength = postBytes.Length;

//                Stream requestStream = request.GetRequestStream();
//                requestStream.Write(postBytes, 0, postBytes.Length);
//                requestStream.Close();

//                using (var response = (HttpWebResponse)request.GetResponse())
//                {
//                    int statusCode = (int)response.StatusCode;
//                    StreamReader reader = new StreamReader(response.GetResponseStream());
//                    tok = reader.ReadToEnd();
//                    JavaScriptSerializer js = new JavaScriptSerializer();
//                    result = (TokenResult)js.Deserialize(tok, typeof(TokenResult));

//                    //   Console.WriteLine(tok);
//                }
//            }
//            catch (WebException e)
//            {
//                using (WebResponse response = e.Response)
//                {
//                    using (Stream data = response.GetResponseStream())
//                    using (var reader = new StreamReader(data))
//                    {
//                        string text = reader.ReadToEnd();
//                    }
//                }
//            }

//            #endregion

//            try
//            {
//                if (result != null && result.access_token != null)
//                {
//                    SetClient(result.access_token);

//                    #region andeler
//                    Database.SetInitializer<AndelerCorpEntities>(null);
//                    using (var context = new AndelerCorpEntities())
//                    {

//                        #region Accounts/Invoices/Payments
//                        int total = 0;
//                        #region Accounts


//                        Console.WriteLine("Accounts found : " + total);



//                        #region Invoice/Payment Preprocessing
//                        Console.WriteLine("Starting to fetch Payments and Invoices !");
//                        var start = Environment.TickCount;
//                        List<Create_emPower_emBroker_Invoices> invList = new List<Create_emPower_emBroker_Invoices>();

//                        List<Create_empower_embroker_Payment_By_AccountNumber> payListInternal = new List<Create_empower_embroker_Payment_By_AccountNumber>();
//                        try
//                        {
//                            Parallel.ForEach(context.Create_emPower_emBroker_Invoices, i =>
//                            {
//                                invList.Add(i);
//                            });
//                            foreach (var inv in invList)
//                            {

//                                try
//                                {
//                                    payListInternal.Add(new Create_empower_embroker_Payment_By_AccountNumber()
//                                    {
//                                        ReferencePaymentId = inv.ReferenceAccountNumber.ToString() + inv.ReferenceInvoiceId.ToString(),
//                                        AccountDeposit = inv.TotalInvoiceAmount,
//                                        Currency = "USD",
//                                        Description = "Empower Payment",
//                                        ActionType = "post",
//                                        TransactionDate = inv.InvoiceDate,
//                                        ReferenceAccountId = inv.ReferenceAccountNumber,
//                                        ReferenceInvoiceId = inv.ReferenceInvoiceId
//                                    });
//                                }
//                                catch (Exception ex)
//                                {
//                                    Console.WriteLine("Null reference found in invoice with number." + inv.AccountKey + ex.Message);

//                                }
//                            }

//                        }
//                        catch (Exception ex)
//                        {
//                            Console.WriteLine("Null reference found in invoice." + ex.Message);
//                        }
//                        Console.WriteLine("Invoices found : " + invList.Count);
//                        Console.WriteLine("Fetched invoices in time: {0}", Environment.TickCount - start);
//                        Console.WriteLine("Removing duplicate invoices !");
//                        start = Environment.TickCount;

//                        RemoveDuplicateInvoices(invList, result.access_token);
//                        Console.WriteLine("Removed duplicate invoices in time: {0}", Environment.TickCount - start);
//                        Console.WriteLine("Remaining invoices to be processed : " + invList.Count);
//                        #endregion

//                        #region Payments
//                        Console.WriteLine("Starting to fetch payments !");
//                        start = Environment.TickCount;
//                        List<Create_empower_embroker_Payment_By_AccountNumber> payList = new List<Create_empower_embroker_Payment_By_AccountNumber>();

//                        payList = payListInternal.ToList();

//                        Console.WriteLine("Fetched payments in time: {0}", Environment.TickCount - start);
//                        Console.WriteLine("Payments found : " + payList.Count);

//                        start = Environment.TickCount;
//                        Console.WriteLine("Removing duplicate payments");

//                        RemoveDuplicatePayments(result.access_token, payList);

//                        Console.WriteLine("Removed duplicate payments in time : {0}", Environment.TickCount - start);
//                        Console.WriteLine("Payments to be processed : {0}", payList.Count);

//                        total = payList.Count;
//                        for (int i = 0; i < payList.OrderBy(jp => jp.ReferenceInvoiceId).Count(); i++)
//                        {
//                            Console.WriteLine("({0}/ {1}) Processing payment with reference payment id : {2}, account id : {3}", i, total, payList[i].ReferencePaymentId, payList[i].ReferenceAccountId);
//                            try
//                            {
//                                CreatePaymentWithReferenceAccountId(result.access_token, payList[i]);
//                            }
//                            catch (Exception)
//                            {
//                                Console.WriteLine("Failed creating payement !!");
//                            }
//                        }

//                        Console.WriteLine("Finished processing payments");

//                        //Console.WriteLine("Mark Invoices as paid");
//                        //PayPreProcessedInvoices(100, result.access_token);
//                        //Console.ReadLine();
//                        //TriggerInvoiceSchedular(result.access_token);
//                        #endregion

//                        #region Invoice Processing

//                        total = invList.Count;
//                        for (int i = 0; i < invList.Count; i++)
//                        {
//                            try
//                            {
//                                Console.WriteLine("({0}/ {1}) Processing invoice with reference invoice id : {2}", i, total, invList[i].ReferenceInvoiceId);
//                                CreateInvoiceWithInvoiceLineItems(result.access_token, invList[i]);
//                            }
//                            catch (Exception ex)
//                            {
//                                Console.WriteLine("Failed creating invoice." + ex.Message);
//                            }
//                        }

//                        Console.WriteLine("Finished processing invoices...");
//                        #endregion

//                        #endregion

//                        #region Mark Invoices as Paid

//                        #endregion
//                    }
//                    #endregion
//                }
//                else
//                {
//                    Console.Write("Some thing is wrong with the site.. Please contact EpSolutions..");
//                }
//                #endregion
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Justy to catch error" + ex.ToString());
//                var input = "N";
//                while (input != "Y")
//                {
//                    Console.WriteLine("Found error description?");
//                    input = Console.ReadLine();
//                }
//            }
//        }

//        private static void PayPreProcessedInvoices(int noOfDays, string accessToken)
//        {
//            var jsonToSend = accessToken;
//            string url = commonUrl + "Api/InvoicePay/PayPreProcessedInvoices?noOfDays=" + noOfDays;
//            string res;
//            try
//            {
//                WebRequest request = (HttpWebRequest)WebRequest.Create(url);
//                request.Method = "GET";
//                request.ContentType = "application/json";
//                request.Headers["Authorization"] = "Bearer " + accessToken;

//                using (var response = (HttpWebResponse)request.GetResponse())
//                {
//                    int statusCode = (int)response.StatusCode;
//                    StreamReader reader = new StreamReader(response.GetResponseStream());
//                    res = reader.ReadToEnd();
//                }
//            }
//            #region Catch
//            catch (WebException e)
//            {
//                using (WebResponse response = e.Response)
//                {
//                    if (response != null)
//                        using (Stream data = response.GetResponseStream())
//                        {
//                            using (var reader = new StreamReader(data))
//                            {
//                                string text = reader.ReadToEnd();
//                                InsertIntoLog(accessToken, "Error while trying to Mark invoices as paid and Error Message: " + text, true);
//                                Console.WriteLine("Error while trying to Mark invoices as paid: " + text);
//                            }
//                        }
//                    else if (e != null & e.Message != null)
//                    {
//                        InsertIntoLog(accessToken, "Error while trying to Mark invoices as paid and Error Message: " + e.Message, true);
//                        Console.WriteLine("Error while trying to Mark invoices as paid: " + e.Message);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                if (e != null)
//                    InsertIntoLog(accessToken, "Error while trying to Mark invoices as paid and Error Message: ", true);
//            }
//            #endregion

//        }

//        private static void TriggerInvoiceSchedular(string accessToken)
//        {
//            var webJobName = ConfigurationManager.AppSettings["InvoiceSchedularWebJobName"];
//            var userName = ConfigurationManager.AppSettings["InvoiceSchedularWebJobPublishProfileUserName"];
//            var userPwd = ConfigurationManager.AppSettings["InvoiceSchedularWebJobPublishProfilePassword"];

//            string webJobUrl = ConfigurationManager.AppSettings["InvoiceSchedularWebJobUrl"] + "/api/triggeredwebjobs/" + webJobName + "/run";

//            try
//            {
//                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(webJobUrl);
//                request.Method = "POST";
//                var byteArray = Encoding.ASCII.GetBytes(userName + ":" + userPwd);
//                request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(byteArray));
//                request.ContentLength = 0;

//                using (var response = (HttpWebResponse)request.GetResponse())
//                {
//                    int statusCode = (int)response.StatusCode;
//                    StreamReader reader = new StreamReader(response.GetResponseStream());
//                    var tok = reader.ReadToEnd();

//                    InsertIntoLog(accessToken, "Data sync done. Invoice Schedular triggered at Time: " + DateTime.UtcNow, false);
//                }
//            }
//            catch (WebException e)
//            {
//                InsertIntoLog(accessToken, "Data sync done. Invoice Schedular could not be triggered", false);
//                using (WebResponse response = e.Response)
//                {
//                    using (Stream data = response.GetResponseStream())
//                    using (var reader = new StreamReader(data))
//                    {
//                        string text = reader.ReadToEnd();
//                    }
//                }
//            }
//        }

//        private static void RemoveDuplicateAccounts(List<Create_emPower_emBroker_Accounts> accList, string accessToken)
//        {
//            // get all accounts for the client
//            var accountRefIds = GetAccountReferenceIds(accessToken);

//            if (accountRefIds == null || !accountRefIds.Any()) return;
//            var accountReferences = accountRefIds.Distinct().ToList();

//            // remove all accounts that are not found
//            foreach (var item in accList.ToList())
//            {
//                if (item != null)
//                {
//                    if (accountReferences.Contains(item.ReferenceAccountId.ToString()))
//                    {
//                        accList.Remove(item);
//                    }
//                }
//            }
//        }

//        #region remove duplicate payments

//        private static void RemoveDuplicateInvoices(List<Create_emPower_emBroker_Invoices> invList, string accessToken)
//        {
//            // get all accounts for the client
//            var invoiceSearch = GetAllInvoices(accessToken);

//            if (invoiceSearch == null || invoiceSearch.Count <= 0) return;
//            var invoiceReferences = invoiceSearch.Invoices.Select(a => a.ReferenceInvoiceId).Distinct().ToList();

//            // remove all accounts that are not found 
//            invList.RemoveAll(p => invoiceReferences.Contains(p.ReferenceInvoiceId.ToString()));
//        }

//        private static void RemoveDuplicatePayments(string accessToken, List<Create_empower_embroker_Payment_By_AccountNumber> payList)
//        {
//            // get all accounts for the client
//            //var accounts = GetAllAccounts(accessToken);

//            //if (accounts == null || !accounts.Any()) return;
//            //var accountReferences = accounts.Select(a => a.ReferenceAccountId).Distinct().ToList();

//            //// remove all accounts that are not found 
//            //payList.RemoveAll(p => !accountReferences.Contains(p.ReferenceAccountId.ToString()));

//            // get all payments for client and find duplicates
//            var payments = GetAllPayments(accessToken);

//            if (payments == null || payments.Count <= 0) return;

//            var paymentReferences = payments.Payments.Select(p => p.ReferencePaymentId).Distinct().ToList();

//            payList.RemoveAll(p => paymentReferences.Contains(p.ReferencePaymentId));

//            // also remove the duplicates within paylist 

//            payList = payList.Distinct(new PaymentComparer()).ToList();
//        }

//        private static List<string> GetAccountReferenceIds(string accessToken)
//        {
//            try
//            {
//                List<string> result = new List<string>();
//                string getAccountsRefIdsUrl = commonUrl + "Api/Accounts/GetAccountReferenceIds";

//                WebRequest req = (HttpWebRequest)WebRequest.Create(getAccountsRefIdsUrl);
//                req.Method = "GET";
//                req.ContentType = "application/json";
//                req.Headers["Authorization"] = "Bearer " + accessToken;

//                using (var response = (HttpWebResponse)req.GetResponse())
//                {
//                    int statusCode = (int)response.StatusCode;
//                    StreamReader reader = new StreamReader(response.GetResponseStream());
//                    var tt = reader.ReadToEnd();
//                    JavaScriptSerializer js = new JavaScriptSerializer();
//                    js.MaxJsonLength = 2147483647;

//                    if (tt != null)
//                        result = (List<string>)js.Deserialize(tt, typeof(List<string>));
//                    if (result == null) return new List<string>();
//                }
//                return result;
//            }
//            catch (WebException e)
//            {
//                using (WebResponse response = e.Response)
//                {
//                    if (response != null)
//                        using (Stream data = response.GetResponseStream())
//                        {
//                            using (var reader = new StreamReader(data))
//                            {
//                                string text = reader.ReadToEnd();
//                                InsertIntoLog(accessToken, "Error while trying to GET all accounts and Error Message: " + text, false);
//                            }
//                        }
//                    else if (e != null & e.Message != null)
//                    {
//                        InsertIntoLog(accessToken, "Error while trying to GET all accounts and Error Message: " + e.Message, false);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                if (e != null)
//                    InsertIntoLog(accessToken, "Error while trying to GET all accounts and Error Message: " + e.Message, false);
//            }
//            return null;
//        }

//        private static InvoiceSearchResultViewModel GetAllInvoices(string accessToken)
//        {
//            try
//            {
//                // get first page and the count
//                InvoiceSearchResultViewModel result = GetInvoicePage(accessToken, 0, 1);

//                // divide the invoice fetch in two sections 
//                bool needThirdPageFetch = (result.Count % 2 != 0);
//                int pageSize = result.Count / 2;

//                // get the first page 
//                var pageResult = GetInvoicePage(accessToken, 0, pageSize);
//                if (pageResult.Invoices.Any())
//                {
//                    result.Invoices.AddRange(pageResult.Invoices);
//                }

//                pageResult = GetInvoicePage(accessToken, 1, pageSize);
//                if (pageResult.Invoices.Any())
//                {
//                    result.Invoices.AddRange(pageResult.Invoices);
//                }
//                if (needThirdPageFetch)
//                {
//                    pageResult = GetInvoicePage(accessToken, 2, pageSize);
//                    if (pageResult.Invoices.Any())
//                    {
//                        result.Invoices.AddRange(pageResult.Invoices);
//                    }
//                }
//                return result;
//            }
//            catch (WebException e)
//            {
//                using (WebResponse response = e.Response)
//                {
//                    if (response != null)
//                        using (Stream data = response.GetResponseStream())
//                        {
//                            using (var reader = new StreamReader(data))
//                            {
//                                string text = reader.ReadToEnd();
//                                InsertIntoLog(accessToken, "Error while trying to GET all accounts and Error Message: " + text, false);
//                            }
//                        }
//                    else if (e != null & e.Message != null)
//                    {
//                        InsertIntoLog(accessToken, "Error while trying to GET all accounts and Error Message: " + e.Message, false);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                if (e != null)
//                    InsertIntoLog(accessToken, "Error while trying to GET all accounts and Error Message: " + e.Message, false);
//            }
//            return null;
//        }

//        private static InvoiceSearchResultViewModel GetInvoicePage(string accessToken, int pageNumber, int pageSize)
//        {
//            string getAccountBytIDUrl = string.Format(commonUrl + "Api/Invoices?pageCount={0}&pageSize={1}", pageNumber, pageSize);

//            InvoiceSearchResultViewModel result = null;

//            WebRequest req = (HttpWebRequest)WebRequest.Create(getAccountBytIDUrl);
//            req.Method = "GET";
//            req.ContentType = "application/json";
//            req.Headers["Authorization"] = "Bearer " + accessToken;

//            using (var response = (HttpWebResponse)req.GetResponse())
//            {
//                int statusCode = (int)response.StatusCode;
//                StreamReader reader = new StreamReader(response.GetResponseStream());
//                var tt = reader.ReadToEnd();
//                JavaScriptSerializer js = new JavaScriptSerializer();
//                js.MaxJsonLength = 2147483647;
//                if (tt != null)
//                    result = (InvoiceSearchResultViewModel)js.Deserialize(tt, typeof(InvoiceSearchResultViewModel));
//                return result;
//            }
//            return result;
//        }

//        private static PaymentSearchResultViewModel GetAllPayments(string accessToken)
//        {
//            try
//            {
//                PaymentSearchResultViewModel result = null;
//                string getAccountBytIDUrl = commonUrl + "Api/CreditDebitTransaction?pageCount=0&pageSize=0";

//                WebRequest req = (HttpWebRequest)WebRequest.Create(getAccountBytIDUrl);
//                req.Method = "GET";
//                req.ContentType = "application/json";
//                req.Headers["Authorization"] = "Bearer " + accessToken;

//                using (var response = (HttpWebResponse)req.GetResponse())
//                {
//                    int statusCode = (int)response.StatusCode;
//                    StreamReader reader = new StreamReader(response.GetResponseStream());
//                    var tt = reader.ReadToEnd();
//                    JavaScriptSerializer js = new JavaScriptSerializer();
//                    js.MaxJsonLength = 2147483647;
//                    if (tt != null)
//                        result = (PaymentSearchResultViewModel)js.Deserialize(tt, typeof(PaymentSearchResultViewModel));
//                }
//                return result;
//            }
//            catch (WebException e)
//            {
//                using (WebResponse response = e.Response)
//                {
//                    if (response != null)
//                        using (Stream data = response.GetResponseStream())
//                        {
//                            using (var reader = new StreamReader(data))
//                            {
//                                string text = reader.ReadToEnd();
//                                InsertIntoLog(accessToken, "Error while trying to GET all accounts and Error Message: " + text, false);
//                            }
//                        }
//                    else if (e != null & e.Message != null)
//                    {
//                        InsertIntoLog(accessToken, "Error while trying to GET all accounts and Error Message: " + e.Message, false);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                if (e != null)
//                    InsertIntoLog(accessToken, "Error while trying to GET all accounts and Error Message: " + e.Message, false);
//            }
//            return null;
//        }

//        private static void MarkAccountAsActive(string accessToken, string referenceAccountId, bool isActivated)
//        {
//            try
//            {
//                string getAccountBytIDUrl = commonUrl + "Api/Accounts?referenceAccountId=" + referenceAccountId + "&isActivated=" + isActivated;

//                WebRequest req = (HttpWebRequest)WebRequest.Create(getAccountBytIDUrl);
//                req.Method = "GET";
//                req.ContentType = "application/json";
//                req.Headers["Authorization"] = "Bearer " + accessToken;

//                using (var response = (HttpWebResponse)req.GetResponse())
//                {
//                    int statusCode = (int)response.StatusCode;
//                    StreamReader reader = new StreamReader(response.GetResponseStream());
//                    var tt = reader.ReadToEnd();

//                }
//            }
//            catch (WebException e)
//            {
//                using (WebResponse response = e.Response)
//                {
//                    if (response != null)
//                        using (Stream data = response.GetResponseStream())
//                        {
//                            using (var reader = new StreamReader(data))
//                            {
//                                string text = reader.ReadToEnd();
//                                InsertIntoLog(accessToken, "Error while trying to activate all accounts and Error Message: " + text, false);
//                            }
//                        }
//                    else if (e != null & e.Message != null)
//                    {
//                        InsertIntoLog(accessToken, "Error while trying to activate all accounts and Error Message: " + e.Message, false);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                if (e != null)
//                    InsertIntoLog(accessToken, "Error while trying to GET all accounts and Error Message: " + e.Message, false);
//            }
//        }

//        #endregion

//        #region SetClient

//        private static void SetClient(string accessToken)
//        {
//            string url = commonUrl + "Api/SetClient?clientId=" + clientID;
//            try
//            {
//                WebRequest request = (HttpWebRequest)WebRequest.Create(url);
//                request.Method = "GET";
//                request.ContentType = "application/json";
//                request.Headers["Authorization"] = "Bearer " + accessToken;

//                using (var response = (HttpWebResponse)request.GetResponse())
//                {
//                    int statusCode = (int)response.StatusCode;
//                    StreamReader reader = new StreamReader(response.GetResponseStream());
//                    var tt = reader.ReadToEnd();
//                }
//            }
//            catch (WebException e)
//            {
//                using (WebResponse response = e.Response)
//                {
//                    if (response != null)
//                        using (Stream data = response.GetResponseStream())
//                        {
//                            using (var reader = new StreamReader(data))
//                            {
//                                string text = reader.ReadToEnd();
//                                InsertIntoLog(accessToken, "Error while setting the client. Message: " + text, true);
//                            }
//                        }
//                }
//            }
//            catch (Exception e)
//            {
//                if (e != null)
//                    InsertIntoLog(accessToken, "Error while setting the client. Message: " + e.Message, true);
//            }
//        }

//        #endregion

//        #region Create/Update POST

//        private static void CreateAccountsWithSitecodes(string accessToken, Create_emPower_emBroker_Accounts account)
//        {
//            int accountID = GetAccountByID(accessToken, account.ReferenceAccountId);
//            string url;

//            if (accountID == 0)
//                url = commonUrl + "Api/Accounts";
//            else
//                url = commonUrl + "api/Sitecode?accountId=" + accountID;
//            //commonUrl + "Api/Accounts/" + accountID;

//            var jsonToSend = accessToken;

//            string res;

//            #region Try
//            try
//            {
//                List<Sitecodes> SiteCodeList = new List<Sitecodes>();

//                int sitecodeId = 1;

//                using (var context = new AndelerCorpEntities())
//                {
//                    var siteCodeFound = false;
//                    foreach (var sitecode in context.Create_emPower_emBroker_Sitecodes_By_AccountKey.Where(p => p.AccountKey == account.AccountKey))
//                    {
//                        siteCodeFound = true;
//                        List<BrokerCommissionPlan> BrCommList = new List<BrokerCommissionPlan>();

//                        Sitecodes esiidList = new Sitecodes();
//                        esiidList.SitecodeId = sitecodeId;
//                        esiidList.SitecodeValue = sitecode.Sitecodevalue;
//                        esiidList.Address = sitecode.Address;
//                        esiidList.City = sitecode.City;
//                        esiidList.State = sitecode.State;
//                        esiidList.Zip = sitecode.Zip;
//                        esiidList.BrokerCommissionPlan = BrCommList;
//                        esiidList.ReferenceSiteCodeId = sitecode.ReferenceSitecodeId;
//                        esiidList.StatusId = sitecode.SitecodeStartDate >= DateTime.Today.AddDays(-Convert.ToInt16(sitecodeStatusInterval)) && String.IsNullOrEmpty(sitecode.SitecodeEndDate.ToString()) ? GetStatusId("onhold") : GetStatusId(sitecode.Status);
//                        esiidList.AccountId = accountID;
//                        esiidList.SitecodeStartDate = sitecode.SitecodeStartDate;
//                        esiidList.SitecodeEndDate = sitecode.SitecodeEndDate;

//                        SiteCodeList.Add(esiidList);
//                        sitecodeId++;
//                    }
//                    if (!siteCodeFound) return;
//                }

//                List<Sitecodes> SiteCodeListFinal = new List<Sitecodes>();

//                if (SiteCodeList.Count() > 1)
//                {
//                    List<String> distinctSitecodeList = new List<string>();
//                    foreach (var xx in SiteCodeList)
//                    {
//                        if (!distinctSitecodeList.Contains(xx.SitecodeValue))
//                            distinctSitecodeList.Add(xx.SitecodeValue);
//                    }

//                    foreach (var scode in distinctSitecodeList)
//                    {
//                        SiteCodeListFinal.Add(SiteCodeList.Where(a => a.SitecodeValue == scode).OrderByDescending(x => x.SitecodeStartDate).FirstOrDefault());
//                    }
//                }
//                else
//                    SiteCodeListFinal = SiteCodeList;

//                WebRequest request = (HttpWebRequest)WebRequest.Create(url);
//                if (accountID == 0)
//                    request.Method = "POST";
//                else
//                    request.Method = "PUT";

//                request.ContentType = "application/json";
//                request.Headers["Authorization"] = "Bearer " + accessToken;

//                // Regex emailRegex = new Regex(@"[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\.)+(?:[a-zA-Z]{2}|com|arpa|edu|firm|int|nato|nom|store|web|org|net|gov|mil|biz|info|mobi|name|aero|jobs|museum)\b");
//                //     //Regex(@"^[a-zA-Z0-9.!#$%&'*+\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$");
//                //// Regex(@"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))*$");
//                // string email = String.IsNullOrWhiteSpace(account.email) ? null : account.email;
//                // if(!emailRegex.IsMatch(email))
//                // {
//                //     email = null;
//                // }
//                string email = null;

//                if (accountID == 0)
//                    using (var streamWriter = new StreamWriter(request.GetRequestStream()))
//                    {
//                        string json = new JavaScriptSerializer().Serialize(new
//                        {
//                            AccountId = accountID,
//                            AccountName = account.AcccountName,
//                            ReferenceAccountId = account.ReferenceAccountId,
//                            StatusId = GetStatusId(account.Status),
//                            Email = email,
//                            AccountCreationDate = account.AccountStartDate,
//                            Mobile = Regex.Replace(account.Mobile, "[^.0-9]", ""),
//                            Sitecodes = SiteCodeListFinal
//                        });
//                        streamWriter.Write(json);
//                    }
//                else
//                {
//                    using (var streamWriter = new StreamWriter(request.GetRequestStream()))
//                    {
//                        string json = new JavaScriptSerializer().Serialize(SiteCodeListFinal);

//                        streamWriter.Write(json);
//                    }
//                }

//                using (var response = (HttpWebResponse)request.GetResponse())
//                {
//                    int statusCode = (int)response.StatusCode;
//                    StreamReader reader = new StreamReader(response.GetResponseStream());
//                    res = reader.ReadToEnd();
//                    sitecodeId = 1;
//                }
//            }
//            #endregion
//            #region Catch
//            catch (WebException e)
//            {
//                using (WebResponse response = e.Response)
//                {
//                    if (response != null)
//                        using (Stream data = response.GetResponseStream())
//                        {
//                            using (var reader = new StreamReader(data))
//                            {
//                                string text = reader.ReadToEnd();
//                                InsertIntoLog(accessToken, "Error while trying to insert an account with account number: " + account.ReferenceAccountId + " and Error Message: " + text + "Inner Exception" + e.InnerException, true);
//                            }
//                        }
//                    else if (e != null & e.Message != null)
//                    {
//                        InsertIntoLog(accessToken, "Error while trying to insert an account with account number: " + account.ReferenceAccountId + " and Error Message: " + e.Message, true);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                if (e != null)
//                    InsertIntoLog(accessToken, "Error while trying to insert an account with account number: " + account.ReferenceAccountId + "Message: " + e.Message + "with InnerException: " + e.InnerException, true);
//            }
//            #endregion
//        }

//        private static void CreateInvoiceWithInvoiceLineItems(string accessToken, Create_emPower_emBroker_Invoices invoices)
//        {
//            string invoiceUrl;
//            var jsonToSend = accessToken;
//            int invoiceID = GetInvoiceByID(accessToken, invoices.ReferenceInvoiceId);

//            if (invoiceID != 0)
//            {
//                if (invoices.Incorrect == true)
//                {
//                    invoiceUrl = commonUrl + "Api/Invoices/" + invoiceID;
//                    DeleteInvoice(accessToken, invoiceUrl, invoiceID);
//                }
//            }
//            else
//            {
//                Console.WriteLine("Creating invoice with reference invoice id : {0}", invoices.ReferenceInvoiceId);

//                invoiceUrl = commonUrl + "Api/Invoices";
//                string res;

//                #region Try
//                try
//                {
//                    List<InvoiceLineItems> InvoiceLineItemsList = new List<InvoiceLineItems>();
//                    using (var context = new AndelerCorpEntities())
//                    {
//                        foreach (var inv in context.Create_empower_embroker_Invoice_InvoiceLineitems.Where(p => p.InvoiceKey == invoices.InvoiceKey && p.AccountKey == invoices.AccountKey))
//                        {
//                            InvoiceLineItems invoiceLineItem = new InvoiceLineItems();

//                            int SiteCodeId = GetSitecodeByID(accessToken, invoices.ReferenceAccountNumber + "-" + inv.ReferenceSitecode);
//                            invoiceLineItem.SiteCodeId = SiteCodeId;
//                            invoiceLineItem.InvoiceUnits = inv.InvoiceUnits;
//                            invoiceLineItem.Address = inv.Address;
//                            invoiceLineItem.City = inv.City;
//                            invoiceLineItem.State = inv.State;
//                            invoiceLineItem.Zip = inv.Zip;
//                            invoiceLineItem.ServiceStartDate = inv.ServiceStartDate;
//                            invoiceLineItem.ServiceEndDate = string.IsNullOrEmpty(inv.ServiceEndDate.ToString()) ? DateTime.MaxValue : inv.ServiceEndDate.Value;
//                            invoiceLineItem.UsageStartDate = inv.UsageStartDate;
//                            invoiceLineItem.UsageEndDate = inv.UsageEndDate;
//                            invoiceLineItem.UsageCharges = inv.UsageCharge;
//                            InvoiceLineItemsList.Add(invoiceLineItem);
//                        }
//                    }

//                    int accountID = GetAccountByID(accessToken, invoices.ReferenceAccountNumber);
//                    WebRequest request = (HttpWebRequest)WebRequest.Create(invoiceUrl);
//                    request.Method = "POST";
//                    request.ContentType = "application/json";
//                    request.Headers["Authorization"] = "Bearer " + accessToken;

//                    decimal totalInvoiceUnits = 0.00m;
//                    foreach (var il in InvoiceLineItemsList)
//                    {
//                        if (il != null)
//                            totalInvoiceUnits += il.InvoiceUnits;
//                    }

//                    using (var streamWriter = new StreamWriter(request.GetRequestStream()))
//                    {
//                        string json = new JavaScriptSerializer().Serialize(new
//                        {
//                            AccountId = accountID,
//                            ReferenceInvoiceId = invoices.ReferenceInvoiceId,
//                            InvoiceDate = invoices.InvoiceDate,
//                            TotalInvoiceAmount = invoices.TotalInvoiceAmount,
//                            TotalInvoiceUnits = totalInvoiceUnits,
//                            Currency = invoices.Currency,
//                            IsDeleted = invoices.Incorrect,
//                            PaidInFullDate = invoices.PaidInFullDate,
//                            InvoiceLineItems = InvoiceLineItemsList
//                        });
//                        streamWriter.Write(json);
//                    }

//                    using (var response = (HttpWebResponse)request.GetResponse())
//                    {
//                        int statusCode = (int)response.StatusCode;
//                        StreamReader reader = new StreamReader(response.GetResponseStream());
//                        res = reader.ReadToEnd();
//                    }
//                }
//                #endregion
//                #region Catch
//                catch (WebException e)
//                {
//                    using (WebResponse response = e.Response)
//                    {
//                        if (response != null)
//                            using (Stream data = response.GetResponseStream())
//                            {
//                                using (var reader = new StreamReader(data))
//                                {
//                                    string text = reader.ReadToEnd();
//                                    InsertIntoLog(accessToken, "Error while trying to insert a Invoice on account with account number: " + invoices.ReferenceAccountNumber + "and invoice number" + invoices.ReferenceInvoiceId + " and Error Message: " + text, true);
//                                }
//                            }
//                        else if (e != null & e.Message != null)
//                        {
//                            InsertIntoLog(accessToken, "Error while trying to insert a Invoice on account with account number: " + invoices.ReferenceAccountNumber + "and invoice number" + invoices.ReferenceInvoiceId + " and Error Message: " + e.Message, true);
//                        }
//                    }
//                }
//                catch (Exception e)
//                {
//                    if (e != null)
//                        InsertIntoLog(accessToken, "Error while trying to insert a Invoice on account with account number: " + invoices.ReferenceAccountNumber + "Message: " + e.Message + "with InnerException: " + e.InnerException, true);
//                }
//                #endregion
//            }
//        }

//        private static void CreatePaymentWithReferenceAccountId(string accessToken, Create_empower_embroker_Payment_By_AccountNumber payment)
//        {
//            var jsonToSend = accessToken;
//            string paymentUrl = commonUrl + "Api/CreditDebitTransaction?referenceAccountId=" + payment.ReferenceAccountId;
//            string res;

//            #region Try
//            try
//            {
//                WebRequest request = (HttpWebRequest)WebRequest.Create(paymentUrl);
//                request.Method = "POST";
//                request.ContentType = "application/json";
//                request.Headers["Authorization"] = "Bearer " + accessToken;

//                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
//                {
//                    string json = new JavaScriptSerializer().Serialize(new
//                    {
//                        ActionType = payment.AccountDeposit < 0 ? "deduct" : "post",
//                        AccountDeposit = payment.AccountDeposit < 0 ? -payment.AccountDeposit : payment.AccountDeposit,
//                        Currency = payment.Currency,
//                        Description = payment.Description,
//                        ReferencePaymentId = payment.ReferencePaymentId,
//                        TransactionDate = payment.TransactionDate
//                    });
//                    streamWriter.Write(json);
//                }

//                using (var response = (HttpWebResponse)request.GetResponse())
//                {
//                    int statusCode = (int)response.StatusCode;
//                    StreamReader reader = new StreamReader(response.GetResponseStream());
//                    res = reader.ReadToEnd();
//                }
//            }
//            #endregion
//            #region Catch
//            catch (WebException e)
//            {
//                using (WebResponse response = e.Response)
//                {
//                    if (response != null)
//                        using (Stream data = response.GetResponseStream())
//                        {
//                            using (var reader = new StreamReader(data))
//                            {
//                                string text = reader.ReadToEnd();
//                                InsertIntoLog(accessToken, "Error while trying to insert a CreditdebitTransaction on account with account number: " + payment.ReferenceAccountId + "and paymentId" + payment.ReferencePaymentId + " and Error Message: " + text, true);
//                                Console.WriteLine("Account reference Id: " + payment.ReferenceAccountId + ". Error while creating CreditdebitTransaction: " + text);
//                            }
//                        }
//                    else if (e != null & e.Message != null)
//                    {
//                        InsertIntoLog(accessToken, "Error while trying to insert a CreditdebitTransaction on account with account number: " + payment.ReferenceAccountId + "and paymentId" + payment.ReferencePaymentId + " and Error Message: " + e.Message, true);
//                        Console.WriteLine("Error while trying to insert a CreditdebitTransaction on account with account number: " + payment.ReferenceAccountId + "and paymentId" + payment.ReferencePaymentId + " and Error Message: " + e.Message);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                if (e != null)
//                    InsertIntoLog(accessToken, "Error while trying to insert a CreditdebitTransaction on account with account number: " + payment.ReferenceAccountId + "Message: " + e.Message + "with InnerException: " + e.InnerException, true);
//            }
//            #endregion
//        }

//        #endregion

//        private static void InsertIntoLog(string accessToken, string message, bool isException)
//        {
//            var jsonToSend = accessToken;
//            string logUrl = commonUrl + "Api/MigrationLog";
//            string res;

//            try
//            {
//                WebRequest request = (HttpWebRequest)WebRequest.Create(logUrl);
//                request.Method = "POST";
//                request.ContentType = "application/json";
//                request.Headers["Authorization"] = "Bearer " + accessToken;

//                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
//                {
//                    string json = new JavaScriptSerializer().Serialize(new
//                    {
//                        Message = message,
//                        IsException = isException
//                    });
//                    streamWriter.Write(json);
//                }

//                using (var response = (HttpWebResponse)request.GetResponse())
//                {
//                    int statusCode = (int)response.StatusCode;
//                    StreamReader reader = new StreamReader(response.GetResponseStream());
//                    res = reader.ReadToEnd();
//                }
//            }
//            catch
//            {
//                // Console.WriteLine("Unable to log the meggage: " + message + " into Log table. Because of the error: " + e.Message);
//            }
//        }

//        #region Update/Delete

//        private static void UpdateCommissionPlanStatus(string accessToken, string updateCommissionPlanUrl, int commPlanId)
//        {
//            try
//            {
//                WebRequest req = (HttpWebRequest)WebRequest.Create(updateCommissionPlanUrl);
//                req.Method = "GET";
//                req.ContentType = "application/json";
//                req.Headers["Authorization"] = "Bearer " + accessToken;

//                using (var response = req.GetResponse() as HttpWebResponse)
//                {
//                    int statusCode = (int)response.StatusCode;
//                    StreamReader reader = new StreamReader(response.GetResponseStream());
//                }
//            }
//            #region Catch
//            catch (WebException e)
//            {
//                using (WebResponse response = e.Response)
//                {
//                    if (response != null)
//                        using (Stream data = response.GetResponseStream())
//                        {
//                            using (var reader = new StreamReader(data))
//                            {
//                                string text = reader.ReadToEnd();
//                                InsertIntoLog(accessToken, "Error while trying to update a commissionPlan status on commission with ID: " + commPlanId + " and Error Message: " + text, true);
//                            }
//                        }
//                    else if (e != null & e.Message != null)
//                    {
//                        InsertIntoLog(accessToken, "Error while trying to update a commissionPlan status on commission with ID: " + commPlanId + " and Error Message: " + e.Message, true);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                if (e != null)
//                    InsertIntoLog(accessToken, "Error while trying to update a commissionPlan status on commission with ID: " + commPlanId + "Message: " + e.Message + "with InnerException: " + e.InnerException, true);
//            }
//            #endregion
//        }

//        private static void DeleteInvoice(string accessToken, string invoiceUrl, int invoiceID)
//        {
//            try
//            {
//                WebRequest req = (HttpWebRequest)WebRequest.Create(invoiceUrl);
//                req.Method = "DELETE";
//                req.ContentType = "application/json";
//                req.Headers["Authorization"] = "Bearer " + accessToken;

//                using (var response = req.GetResponse() as HttpWebResponse)
//                {
//                    int statusCode = (int)response.StatusCode;
//                    StreamReader reader = new StreamReader(response.GetResponseStream());
//                }
//            }
//            catch (WebException e)
//            {
//                using (WebResponse response = e.Response)
//                {
//                    if (response != null)
//                        using (Stream data = response.GetResponseStream())
//                        {
//                            using (var reader = new StreamReader(data))
//                            {
//                                string text = reader.ReadToEnd();
//                                InsertIntoLog(accessToken, "Error while trying to delete an invoice with invoiceID: " + invoiceID + " and Error Message: " + text, true);
//                            }
//                        }
//                    else if (e != null & e.Message != null)
//                    {
//                        InsertIntoLog(accessToken, "Error while trying to delete an invoice with invoiceID: " + invoiceID + " and Error Message: " + e.Message, true);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                if (e != null)
//                    InsertIntoLog(accessToken, "Error while trying to delete an invoice with invoiceID: " + invoiceID + "Message: " + e.Message + "with InnerException: " + e.InnerException, true);
//            }
//        }

//        #endregion     

//        #region GET

//        private static int GetAccountByID(string accessToken, int referenceAccountID)
//        {
//            int accountID = 0;
//            try
//            {
//                Accounts result = new Accounts();
//                string getAccountBytIDUrl = commonUrl + "Api/Accounts/?accountReferenceId=" + referenceAccountID + "&accountNo=";

//                WebRequest req = (HttpWebRequest)WebRequest.Create(getAccountBytIDUrl);
//                req.Method = "GET";
//                req.ContentType = "application/json";
//                req.Headers["Authorization"] = "Bearer " + accessToken;

//                using (var response = (HttpWebResponse)req.GetResponse())
//                {
//                    int statusCode = (int)response.StatusCode;
//                    StreamReader reader = new StreamReader(response.GetResponseStream());
//                    var tt = reader.ReadToEnd();
//                    JavaScriptSerializer js = new JavaScriptSerializer();
//                    if (tt != null)
//                        result = (Accounts)js.Deserialize(tt, typeof(Accounts));
//                    accountID = result == null ? 0 : result.AccountId;
//                }
//            }
//            catch (WebException e)
//            {
//                using (WebResponse response = e.Response)
//                {
//                    if (response != null)
//                        using (Stream data = response.GetResponseStream())
//                        {
//                            using (var reader = new StreamReader(data))
//                            {
//                                string text = reader.ReadToEnd();
//                                InsertIntoLog(accessToken, "Error while trying to GET an account with ID: " + referenceAccountID + " and Error Message: " + text, false);
//                            }
//                        }
//                    else if (e != null & e.Message != null)
//                    {
//                        InsertIntoLog(accessToken, "Error while trying to GET an account with ID: " + referenceAccountID + " and Error Message: " + e.Message, false);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                if (e != null)
//                    InsertIntoLog(accessToken, "Error while trying to GET an account with ID: " + referenceAccountID + e.Message + "with InnerException: " + e.InnerException, false);
//            }

//            return accountID;
//        }
//        public static int GetSitecodeByID(string accessToken, string referenceSitecodeID)
//        {
//            int sitecodeID = 0;
//            try
//            {
//                Sitecodes result = new Sitecodes();
//                string getSitecodeBytIDUrl = commonUrl + "Api/Sitecode?referenceSiteCodeId=" + referenceSitecodeID;

//                WebRequest req = (HttpWebRequest)WebRequest.Create(getSitecodeBytIDUrl);
//                req.Method = "GET";
//                req.ContentType = "application/json";
//                req.Headers["Authorization"] = "Bearer " + accessToken;

//                using (var response = (HttpWebResponse)req.GetResponse())
//                {
//                    int statusCode = (int)response.StatusCode;
//                    StreamReader reader = new StreamReader(response.GetResponseStream());
//                    var tt = reader.ReadToEnd();
//                    JavaScriptSerializer js = new JavaScriptSerializer();
//                    if (tt != null)
//                        result = (Sitecodes)js.Deserialize(tt, typeof(Sitecodes));
//                    sitecodeID = result == null ? 0 : result.SitecodeId;
//                }
//            }
//            catch (WebException e)
//            {
//                using (WebResponse response = e.Response)
//                {
//                    if (response != null)
//                        using (Stream data = response.GetResponseStream())
//                        {
//                            using (var reader = new StreamReader(data))
//                            {
//                                string text = reader.ReadToEnd();
//                                InsertIntoLog(accessToken, "Error while trying to GET a sitecode with ID: " + referenceSitecodeID + " and Error Message: " + text, false);
//                            }
//                        }
//                    else if (e != null & e.Message != null)
//                    {
//                        InsertIntoLog(accessToken, "Error while trying to GET a sitecode with ID: " + referenceSitecodeID + " and Error Message: " + e.Message, false);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                if (e != null)
//                    InsertIntoLog(accessToken, "Error while trying to GET a sitecode with ID: " + referenceSitecodeID + e.Message + "with InnerException: " + e.InnerException, false);
//            }
//            return sitecodeID;
//        }

//        public static int GetInvoiceByID(string accessToken, int referenceInvoiceID)
//        {
//            int invoiceID = 0;
//            try
//            {
//                Invoices result = new Invoices();
//                string getInvoiceByIDUrl = commonUrl + "/Api/Invoices?referenceInvoiceId=" + referenceInvoiceID;

//                WebRequest req = (HttpWebRequest)WebRequest.Create(getInvoiceByIDUrl);
//                req.Method = "GET";
//                req.ContentType = "application/json";
//                req.Headers["Authorization"] = "Bearer " + accessToken;

//                using (var response = (HttpWebResponse)req.GetResponse())
//                {
//                    int statusCode = (int)response.StatusCode;
//                    StreamReader reader = new StreamReader(response.GetResponseStream());
//                    var tt = reader.ReadToEnd();
//                    JavaScriptSerializer js = new JavaScriptSerializer();
//                    if (tt != null)
//                        result = (Invoices)js.Deserialize(tt, typeof(Invoices));
//                    invoiceID = result == null ? 0 : result.InvoiceId;
//                }
//            }
//            catch (WebException e)
//            {
//                using (WebResponse response = e.Response)
//                {
//                    if (response != null)
//                        using (Stream data = response.GetResponseStream())
//                        {
//                            using (var reader = new StreamReader(data))
//                            {
//                                string text = reader.ReadToEnd();
//                                InsertIntoLog(accessToken, "Error while trying to GET an invoice with ID: " + referenceInvoiceID + " and Error Message: " + text, false);
//                            }
//                        }
//                    else if (e != null & e.Message != null)
//                    {
//                        InsertIntoLog(accessToken, "Error while trying to GET an invoice with ID: " + referenceInvoiceID + " and Error Message: " + e.Message, false);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                if (e != null)
//                    InsertIntoLog(accessToken, "Error while trying to GET an invoice with ID: " + referenceInvoiceID + e.Message + "with InnerException: " + e.InnerException, false);
//            }
//            return invoiceID;
//        }

//        private static int GetCreditDebitTransactionByID(string accessToken, int referenceCreditDebitTransactionID)
//        {
//            int creditDebitTransactionId = 0;
//            try
//            {
//                CreditDebitTransactions result = new CreditDebitTransactions();
//                string getPaymentByIDUrl = commonUrl + "/Api/CreditDebitTransaction?referencePaymentId=" + referenceCreditDebitTransactionID;

//                WebRequest req = (HttpWebRequest)WebRequest.Create(getPaymentByIDUrl);
//                req.Method = "GET";
//                req.ContentType = "application/json";
//                req.Headers["Authorization"] = "Bearer " + accessToken;

//                using (var response = (HttpWebResponse)req.GetResponse())
//                {
//                    int statusCode = (int)response.StatusCode;
//                    StreamReader reader = new StreamReader(response.GetResponseStream());
//                    var tt = reader.ReadToEnd();
//                    JavaScriptSerializer js = new JavaScriptSerializer();
//                    if (tt != null && tt.Count() > 0)
//                        result = (CreditDebitTransactions)js.Deserialize(tt, typeof(CreditDebitTransactions));
//                    creditDebitTransactionId = result == null ? 0 : result.CreditDebitTransactionId;
//                }
//            }
//            catch (WebException e)
//            {
//                using (WebResponse response = e.Response)
//                {
//                    if (response != null)
//                        using (Stream data = response.GetResponseStream())
//                        {
//                            using (var reader = new StreamReader(data))
//                            {
//                                string text = reader.ReadToEnd();
//                                InsertIntoLog(accessToken, "Error while trying to GET a creditDebitTransaction with ID: " + referenceCreditDebitTransactionID + " and Error Message: " + text, false);
//                            }
//                        }
//                    else if (e != null & e.Message != null)
//                    {
//                        InsertIntoLog(accessToken, "Error while trying to GET a creditDebitTransaction with ID: " + referenceCreditDebitTransactionID + " and Error Message: " + e.Message, false);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                if (e != null)
//                    InsertIntoLog(accessToken, "Error while trying to GET a creditDebitTransaction with ID: " + referenceCreditDebitTransactionID + e.Message + "with InnerException: " + e.InnerException, false);
//            }
//            return creditDebitTransactionId;
//        }

//        #endregion

//        #region Public Methods

//        public static int GetStatusId(string status)
//        {
//            switch (status.ToLower())
//            {
//                case "initial": return (int)StatusEnum.Initial;
//                case "active": return (int)StatusEnum.Active;
//                case "inactive": return (int)StatusEnum.Inactive;
//                case "onhold": return (int)StatusEnum.OnHold;

//                default: return 2;
//            }
//        }

//        #endregion

//    }

//    #region Classes

//    public enum StatusEnum
//    {
//        Initial = 1,
//        Active = 2,
//        Inactive = 3,
//        OnHold = 4,
//        Approved = 5,
//        Rejected = 6,
//        NotProcessed = 7,
//    }

//    public class TokenResult
//    {
//        public string access_token { get; set; }
//        public string token_type { get; set; }
//        public string expires_in { get; set; }
//        public string userName { get; set; }
//        public string role { get; set; }
//        public string ProfilePic { get; set; }
//        public string ClientName { get; set; }
//        public Boolean isResetRequested { get; set; }
//        public string ClientLogo { get; set; }
//        public string ClientId { get; set; }
//        public string FirstName { get; set; }
//        public DateTime issued { get; set; }
//        public DateTime expires { get; set; }
//    }

//    public class CommissionPlans
//    {
//        public int CommissionPlanId { get; set; }
//        public Guid ClientID { get; set; }
//        public string CommissionPlanName { get; set; }
//        public DateTime StartDate { get; set; }
//        public DateTime EndDate { get; set; }
//        public Boolean IsPaidUpstream { get; set; }
//        public Boolean IsCreatedByClient { get; set; }
//        public Boolean IsActive { get; set; }
//        public int? ReferenceCommissionPlanId { get; set; }
//        public List<CommissionPlanItems> CommissionPlanItemList { get; set; }
//    }

//    public class CommissionPlansList
//    {
//        public List<CommissionPlans> CommissionPlans { get; set; }
//    }

//    public class BrokersList
//    {
//        public List<Brokers> Brokers { get; set; }
//        public int Count { get; set; }
//    }

//    public class CommissionPlanItems
//    {
//        public decimal PercentageOff { get; set; }
//        public decimal FlatOff { get; set; }
//        public int ApplicableOnId { get; set; }
//        public string Client { get; set; }
//        public int EventTypeId { get; set; }
//    }


//    public class Brokers
//    {
//        public int BrokerId { get; set; }
//        public Guid ClientID { get; set; }
//        public string FirstName { get; set; }
//        public string LastName { get; set; }
//        public string BrokerName { get; set; }
//        public string Email { get; set; }
//        public Boolean IsDeleted { get; set; }
//        public Boolean IsExternal { get; set; }
//        public Boolean IsActive { get; set; }
//        public string PrimaryPhone { get; set; }
//        public string SecondaryPhone { get; set; }
//        public string PreferredContactMethod { get; set; }
//        public string AddressLine1 { get; set; }
//        public string City { get; set; }
//        public string State { get; set; }
//        public string Zip { get; set; }
//        public string ReferenceBrokerId { get; set; }
//        public string Description { get; set; }
//    }

//    public class Accounts
//    {
//        public int AccountId { get; set; }
//        public Guid ClientID { get; set; }
//        public string AccountName { get; set; }
//        public string Email { get; set; }
//        public string Mobile { get; set; }
//        public Decimal Units { get; set; }
//        //public DateTime StartDate { get; set; }
//        //public DateTime AccountCreationDate { get; set; }
//        //public Decimal BalanceAmount { get; set; }
//        //public Decimal UnpaidInvoiceTotal { get; set; }
//        //public string Currency { get; set; }
//        //public Boolean IsDeleted { get; set; }
//        public string ReferenceAccountId { get; set; }
//        //public int StatusId { get; set; }
//        //public Status Status { get; set; }
//        //public Boolean IsActivateOnCreation { get; set; }
//        // public string Sitecodes { get; set; }
//    }

//    public class Invoices
//    {
//        public int InvoiceId { get; set; }
//        public int AccountId { get; set; }
//        public string AccoutName { get; set; }
//        public string ReferenceInvoiceId { get; set; }
//        public string ReferenceAccountId { get; set; }
//        public DateTime InvoiceDate { get; set; }
//        public decimal TotalInvoiceAmount { get; set; }
//        public decimal TotalInvoiceUnits { get; set; }
//    }

//    public class Status
//    {
//        public int StatusId { get; set; }
//        public string StatusName { get; set; }
//        public string Description { get; set; }
//    }

//    public class Sitecodes
//    {
//        public string SitecodeValue { get; set; }
//        public int StatusId { get; set; }
//        public int SitecodeId { get; set; }
//        public string Address { get; set; }
//        public string City { get; set; }
//        public string State { get; set; }
//        public string Zip { get; set; }
//        public string RatePlanName { get; set; }
//        public List<BrokerCommissionPlan> BrokerCommissionPlan { get; set; }
//        public string ReferenceSiteCodeId { get; set; }
//        public int AccountId { get; set; }
//        public DateTime SitecodeStartDate { get; set; }
//        public DateTime? SitecodeEndDate { get; set; }
//        public DateTime AssociatedStartDate { get; set; }
//        public DateTime? AssociatedEndDate { get; set; }
//    }

//    public class InvoiceLineItems
//    {
//        public Decimal InvoiceUnits { get; set; }
//        public int SiteCodeId { get; set; }
//        public string Address { get; set; }
//        public string City { get; set; }
//        public string State { get; set; }
//        public string Zip { get; set; }
//        //  public string Status { get; set; }
//        public DateTime ServiceStartDate { get; set; }
//        public DateTime ServiceEndDate { get; set; }
//        public DateTime UsageStartDate { get; set; }
//        public DateTime UsageEndDate { get; set; }
//        public Decimal UsageCharges { get; set; }
//    }

//    public class CreditDebitTransactions
//    {
//        public int CreditDebitTransactionId { get; set; }
//        public decimal AccountDeposit { get; set; }
//        //// public string Currency { get; set; }
//        public int ReferencePaymentId { get; set; }
//        // public string Description { get; set; }
//        // public DateTime TransactionDate { get; set; }
//        // public string ActionType { get; set; }
//        public int AccountId { get; set; }
//    }

//    public class RecurringPayments
//    {
//        public int RecurringPaymentEvent { get; set; }
//        public decimal RecurringPaymentEventAmount { get; set; }
//        public decimal RecurringPaymentEventRatePerKWH { get; set; }
//        public decimal RecurringPaymentEventPercentageUsageCharge { get; set; }
//    }

//    public class NonRecurringPayments
//    {
//        public int NonRecurringPaymentEvent { get; set; }
//        public decimal NonRecurringPaymentEventAmount { get; set; }
//    }

//    public class BrokerCommissionPlan
//    {
//        public int BrokerID { get; set; }
//        public int CommissionPlanID { get; set; }
//    }


//    public class PaymentSearchResultViewModel
//    {
//        public IEnumerable<CreditDebitTransactionViewModel> Payments { get; set; }
//        public int Count { get; set; }
//    }

//    public class InvoiceSearchResultViewModel
//    {
//        public List<Invoices> Invoices { get; set; }
//        public int Count { get; set; }
//    }


//    public class CreditDebitTransactionViewModel
//    {
//        public int CreditDebitTransactionId { get; set; }
//        public decimal AccountDeposit { get; set; }
//        public string Currency { get; set; }
//        public string ReferencePaymentId { get; set; }
//        public string Description { get; set; }
//        public DateTime TransactionDate { get; set; }
//        //this can be Deposit/Withdrawal/Override payment
//        public string ActionType { get; set; }
//        public Guid ClientId { get; set; }
//        public int AccountId { get; set; }
//        public string AccountReferenceId { get; set; }
//    }

//    public class PaymentComparer : IEqualityComparer<Create_empower_embroker_Payment_By_AccountNumber>
//    {
//        public bool Equals(Create_empower_embroker_Payment_By_AccountNumber x, Create_empower_embroker_Payment_By_AccountNumber y)
//        {
//            if (string.IsNullOrEmpty(x.ReferencePaymentId) || string.IsNullOrEmpty(y.ReferencePaymentId)) return false;

//            return x.ReferencePaymentId == y.ReferencePaymentId;
//        }

//        public int GetHashCode(Create_empower_embroker_Payment_By_AccountNumber obj)
//        {
//            if (string.IsNullOrEmpty(obj.ReferencePaymentId)) return obj.GetHashCode();
//            var hash = obj.ReferencePaymentId.GetHashCode();

//            return hash;
//        }
//    }
//    #endregion
//}