using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.Services;
using System.Xml;
//must reference CommerceBuilder and NVelocity dll
using CommerceBuilder.Orders;
using CommerceBuilder.Payments;
using CommerceBuilder.Products;
using CommerceBuilder.UI.WebControls;
using CommerceBuilder.Utility;
using CommerceBuilder.Messaging;
using CommerceBuilder.Payments.Providers;

namespace PaymentGateway_AbleCommerce
{
    //inherits from PaymentProviderBase class in order to keep up with updates on Able Commerce's part
    //class modified from sample code provided by Able Commerce
    public class PaymentClass : CommerceBuilder.Payments.Providers.PaymentProviderBase
    {
        //set all const string values to same cashflows url
        public const string DEFAULT_XML_API_LIVEURL = "https://secure.cashflows.com/gateway/remote";
        public const string DEFAULT_XML_API_TESTURL = "https://secure.cashflows.com/gateway/remote";
        public const string DEFAULT_NV_API_LIVEURL = "https://secure.cashflows.com/gateway/remote";
        public const string DEFAULT_NV_API_TESTURL = "https://secure.cashflows.com/gateway/remote";

        //must declare certain variables in inheriting class
        string _MerchantLogin;
        string _TransactionKey;
        bool _UseAuthCapture = false;
        GatewayModeOption _GatewayMode = GatewayModeOption.ProductionServerTestMode;
        bool _IsSecureSource;
        string _xmlApiLiveUrl = DEFAULT_XML_API_LIVEURL;
        string _xmlApiTestUrl = DEFAULT_XML_API_TESTURL;
        string _nvApiLiveUrl = DEFAULT_NV_API_LIVEURL;
        string _nvApiTestUrl = DEFAULT_NV_API_TESTURL;

        public string MerchantLogin
        {
            get { return _MerchantLogin; }
            set { _MerchantLogin = value; }
        }

        public string TransactionKey
        {
            get { return _TransactionKey; }
            set { _TransactionKey = value; }
        }

        public bool UseAuthCapture
        {
            get { return _UseAuthCapture; }
            set { _UseAuthCapture = value; }
        }

        public GatewayModeOption GatewayMode
        {
            get { return _GatewayMode; }
            set { _GatewayMode = value; }
        }

        public bool IsSecureSource
        {
            get { return _IsSecureSource; }
            set { _IsSecureSource = value; }
        }

        public string XmlApiLiveUrl
        {
            get { return _xmlApiLiveUrl; }
            set { _xmlApiLiveUrl = value; }
        }

        public string XmlApiTestUrl
        {
            get { return _xmlApiTestUrl; }
            set { _xmlApiTestUrl = value; }
        }

        public string NvApiLiveUrl
        {
            get { return _nvApiLiveUrl; }
            set { _nvApiLiveUrl = value; }
        }

        public string NvApiTestUrl
        {
            get { return _nvApiTestUrl; }
            set { _nvApiTestUrl = value; }
        }

        public override string Name
        {
            get { return "A Custom Payment Gateway"; }
        }

        public override string Description
        {
            get { return "Anthony's Custom Gateway is a payment interface for Able Commerce shopping cart software and uses the Cashflows API for managing payment transactions for a client."; }
        }

        public override string GetLogoUrl(ClientScriptManager cs)
        {
            if (cs != null)
                return cs.GetWebResourceUrl(this.GetType(), "PaymentGateway_AbleCommerce.AESGateway.png"); //custom logo
            return string.Empty;
        }

        public override string Version
        {
            get { return "AIM 3.1"; }
        }


        private Boolean UseTestGateway
        {
            get
            {
                return (this.GatewayMode == GatewayModeOption.TestServerLiveMode || this.GatewayMode == GatewayModeOption.TestServerTestMode);
            }
        }

        private Boolean UseTestRequest
        {
            get
            {
                return (this.GatewayMode == GatewayModeOption.ProductionServerTestMode || this.GatewayMode == GatewayModeOption.TestServerTestMode);
            }
        }

        //Includes Authorize/AuthorizeCapture/Capture/PartialRefund/Refund/Void Transactions
        //must override this property and return all transactions you will support
        public override SupportedTransactions SupportedTransactions
        {
            get
            {
                return (SupportedTransactions.Authorize | SupportedTransactions.AuthorizeCapture | SupportedTransactions.Capture | SupportedTransactions.PartialRefund | SupportedTransactions.Refund | SupportedTransactions.Void);
            }
        }

        public override bool RefundRequiresAccountData
        {
            get
            {
                return true;
            }
        }

        //Initialize method must be overridden in the inheriting class
        public override void Initialize(int PaymentGatewayId, IDictionary<string, string> ConfigurationData)
        {
            base.Initialize(PaymentGatewayId, ConfigurationData);

            //getting configuration data from ConfigForm.html that the user fills out info on
            if (ConfigurationData.ContainsKey("MerchantLogin")) MerchantLogin = ConfigurationData["MerchantLogin"];
            if (ConfigurationData.ContainsKey("TransactionKey")) TransactionKey = ConfigurationData["TransactionKey"];
            if (ConfigurationData.ContainsKey("UseAuthCapture")) UseAuthCapture = AlwaysConvert.ToBool(ConfigurationData["UseAuthCapture"], true);
            if (ConfigurationData.ContainsKey("GatewayMode")) GatewayMode = (GatewayModeOption)AlwaysConvert.ToInt(ConfigurationData["GatewayMode"]);
            if (ConfigurationData.ContainsKey("IsSecureSource")) IsSecureSource = (ConfigurationData["IsSecureSource"] == "on");

            XmlApiLiveUrl = DEFAULT_XML_API_LIVEURL;
            XmlApiTestUrl = DEFAULT_XML_API_TESTURL;
            NvApiLiveUrl = DEFAULT_NV_API_LIVEURL;
            NvApiTestUrl = DEFAULT_NV_API_TESTURL;
        }

        //BuildConfigForm method must be overridden in the inheriting class
        public override void BuildConfigForm(Control parentControl)
        {
            string idBase = parentControl.Parent.UniqueID + "$Config";
            AssemblyName aName = GetType().Assembly.GetName();
            string assemblyInfo = aName.Name.ToString() + "&nbsp;(v" + aName.Version.ToString() + ")";

            Hashtable parameters = new Hashtable();
            parameters.Add("idBase", idBase);
            parameters.Add("assemblyInfo", assemblyInfo);
            parameters.Add("MerchantLogin", this.MerchantLogin);
            parameters.Add("IsSecureSource", this.IsSecureSource);
            parameters.Add("TransactionKey", this.TransactionKey);
            parameters.Add("UseAuthCapture", this.UseAuthCapture);
            parameters.Add("GatewayMode", this.GatewayMode);
            parameters.Add("UseDebugMode", this.UseDebugMode);
            parameters.Add("XmlApiLiveUrl", this.XmlApiLiveUrl);
            parameters.Add("XmlApiTestUrl", this.XmlApiTestUrl);
            parameters.Add("NvApiLiveUrl", this.NvApiLiveUrl);
            parameters.Add("NvApiTestUrl", this.NvApiTestUrl);
            string template = string.Empty;
            using (Stream stream = Assembly.GetExecutingAssembly()
                               .GetManifestResourceStream("PaymentGateway_AbleCommerce.ConfigForm.html"))
            using (StreamReader reader = new StreamReader(stream))
            {
                template = reader.ReadToEnd();
            }

            string output = NVelocityEngine.Instance.Process(parameters, template);
            parentControl.Controls.Add(new LiteralControl(output));
        }

        public override string ConfigReference
        {
            get { return "Merchant Login: " + this.MerchantLogin; }
        }

        public override Transaction DoAuthorize(AuthorizeTransactionRequest transactionRequest)
        {
            Dictionary<string, string> sensitiveData = new Dictionary<string, string>();
            //BUILD THE REQUEST
            string gatewayRequest = BuildGatewayRequest_Authorize(transactionRequest, sensitiveData);
            //RECORD REQUEST
            if (this.UseDebugMode)
            {
                //ALWAYS MASK THE CREDENTIALS
                string credentials = "";// String.Format("x_login={0}&x_tran_key={1}", this.MerchantLogin, this.TransactionKey);
                string debugCredentials = "";// "x_login=xxxxxxxx&x_tran_key=xxxxxxxx";
                sensitiveData[credentials] = debugCredentials;
                this.RecordCommunication(this.Name, CommunicationDirection.Send, gatewayRequest, sensitiveData);
            }
            //SEND REQUEST
            string response = this.SendRequestToGateway(gatewayRequest);
            //RECORD RESPONSE
            if (this.UseDebugMode) this.RecordCommunication(this.Name, CommunicationDirection.Receive, response, null);
            //PROCESS RESPONSE AND RETURN RESULT
            return this.ProcessGatewayResponse_Authorize(transactionRequest, response);
        }

        private bool IsCheckPayment(Payment payment)
        {
            if (payment != null)
            {
                PaymentMethod m = payment.PaymentMethod;
                if (m != null) return (m.PaymentInstrumentType == PaymentInstrumentType.Check);
            }
            return false;
        }

        private Transaction ProcessGatewayResponse_Authorize(AuthorizeTransactionRequest request, string authorizeResponse)
        {
            //CREATE THE TRANSACTION OBJECT
            Transaction transaction = new Transaction();
            transaction.PaymentGatewayId = this.PaymentGatewayId;
            transaction.TransactionType = (this.UseAuthCapture || IsCheckPayment(request.Payment)) ? TransactionType.AuthorizeCapture : request.TransactionType;
            //PARSE THE RESPONSE FROM ANET
            string[] responseValues = authorizeResponse.Split("|".ToCharArray());
            int responseLength = responseValues.Length;
            if (responseLength < 6)
            {
                transaction.TransactionStatus = TransactionStatus.Failed;
                transaction.ResponseMessage = authorizeResponse;
            }
            else
            {
                transaction.ProviderTransactionId = responseValues[6];
                transaction.TransactionDate = DateTime.UtcNow;
                transaction.Amount = request.Amount;
                bool successful = (AlwaysConvert.ToInt(responseValues[0]) == 1);
                transaction.TransactionStatus = (successful ? TransactionStatus.Successful : TransactionStatus.Failed);
                if (!successful)
                {
                    transaction.ResponseCode = responseValues[2];
                    transaction.ResponseMessage = responseValues[3];
                }
                transaction.AuthorizationCode = responseValues[4];
                transaction.AVSResultCode = responseValues[5];
                if (transaction.AVSResultCode.Equals("P") || transaction.AVSResultCode.Equals("B")) transaction.AVSResultCode = "U";
                transaction.CVVResultCode = responseValues[38];
                if (string.IsNullOrEmpty(transaction.CVVResultCode)) transaction.CVVResultCode = "X";
                transaction.RemoteIP = request.RemoteIP;
            }
            return transaction;
        }

        private Transaction ProcessGatewayResponse_Capture(CaptureTransactionRequest request, string captureResponse)
        {
            //CREATE THE TRANSACTION OBJECT
            Transaction transaction = new Transaction();
            transaction.PaymentGatewayId = this.PaymentGatewayId;
            transaction.TransactionType = TransactionType.Capture;
            //PARSE THE RESPONSE FROM ANET
            string[] responseValues = captureResponse.Split("|".ToCharArray());
            int responseLength = responseValues.Length;
            if (responseLength < 6)
            {
                transaction.TransactionStatus = TransactionStatus.Failed;
                transaction.ResponseMessage = captureResponse;
            }
            else
            {
                transaction.ProviderTransactionId = responseValues[6];
                transaction.TransactionDate = DateTime.UtcNow;
                transaction.Amount = request.Amount;
                bool successful = (AlwaysConvert.ToInt(responseValues[0]) == 1);
                if (successful)
                {
                    transaction.TransactionStatus = TransactionStatus.Successful;
                }
                else
                {
                    transaction.TransactionStatus = TransactionStatus.Failed;
                }
                transaction.ResponseCode = responseValues[2];
                transaction.ResponseMessage = responseValues[3];
                transaction.AuthorizationCode = responseValues[4];
                transaction.AVSResultCode = responseValues[5];
                if (transaction.AVSResultCode.Equals("P") || transaction.AVSResultCode.Equals("B")) transaction.AVSResultCode = "U";
                transaction.CVVResultCode = responseValues[38];
                transaction.RemoteIP = request.RemoteIP;
            }
            return transaction;
        }

        private Transaction ProcessGatewayResponse_Void(Payment payment, string voidResponse, VoidTransactionRequest request)
        {
            //CREATE THE TRANSACTION OBJECT
            Transaction transaction = new Transaction();
            transaction.PaymentGatewayId = this.PaymentGatewayId;
            transaction.TransactionType = TransactionType.Void;
            //PARSE THE RESPONSE FROM ANET
            string[] responseValues = voidResponse.Split("|".ToCharArray());
            int responseLength = responseValues.Length;
            if (responseLength < 6)
            {
                transaction.TransactionStatus = TransactionStatus.Failed;
                transaction.ResponseMessage = voidResponse;
            }
            else
            {
                transaction.ProviderTransactionId = responseValues[6];
                transaction.TransactionDate = DateTime.UtcNow;
                transaction.Amount = request.Amount;
                bool successful = (AlwaysConvert.ToInt(responseValues[0]) == 1);
                if (successful)
                {
                    transaction.TransactionStatus = TransactionStatus.Successful;
                }
                else
                {
                    transaction.TransactionStatus = TransactionStatus.Failed;
                }
                transaction.ResponseCode = responseValues[2];
                transaction.ResponseMessage = responseValues[3];
                transaction.AuthorizationCode = responseValues[4];
                transaction.AVSResultCode = responseValues[5];
                if (transaction.AVSResultCode.Equals("P") || transaction.AVSResultCode.Equals("B")) transaction.AVSResultCode = "U";
                transaction.CVVResultCode = responseValues[38];
                transaction.RemoteIP = request.RemoteIP;
            }
            return transaction;
        }

        private Transaction ProcessGatewayResponse_Refund(Payment payment, string refundResponse, RefundTransactionRequest request)
        {
            //CREATE THE TRANSACTION OBJECT
            Transaction transaction = new Transaction();
            transaction.PaymentGatewayId = this.PaymentGatewayId;
            transaction.TransactionType = TransactionType.Refund;
            //PARSE THE RESPONSE FROM ANET
            string[] responseValues = refundResponse.Split("|".ToCharArray());
            int responseLength = responseValues.Length;
            if (responseLength < 6)
            {
                transaction.TransactionStatus = TransactionStatus.Failed;
                transaction.ResponseMessage = refundResponse;
            }
            else
            {
                transaction.ProviderTransactionId = responseValues[6];
                transaction.TransactionDate = DateTime.UtcNow;
                transaction.Amount = request.Amount;
                bool successful = (AlwaysConvert.ToInt(responseValues[0]) == 1);
                if (successful)
                {
                    transaction.TransactionStatus = TransactionStatus.Successful;
                }
                else
                {
                    transaction.TransactionStatus = TransactionStatus.Failed;
                }
                transaction.ResponseCode = responseValues[2];
                transaction.ResponseMessage = responseValues[3];
                transaction.AuthorizationCode = responseValues[4];
                transaction.AVSResultCode = responseValues[5];
                if (transaction.AVSResultCode.Equals("P") || transaction.AVSResultCode.Equals("B")) transaction.AVSResultCode = "U";
                transaction.CVVResultCode = responseValues[38];
                transaction.RemoteIP = request.RemoteIP;
            }
            return transaction;
        }

        private string BuildGatewayRequestPart_MerchantAccountInformation()
        {
            //need auth_id and auth_pass as part of http request parameters
            if (this.GatewayMode == GatewayModeOption.ProductionServerTestMode || this.GatewayMode == GatewayModeOption.TestServerTestMode)
            {
                return String.Format("auth_id={0}&auth_pass={1}", this.MerchantLogin, this.TransactionKey);
            }
            return String.Format("auth_id={0}&auth_pass={1}", this.MerchantLogin, this.TransactionKey);
        }

        private string BuildGatewayRequestPart_GatewayResponseConfiguration()
        {
            return "&x_delim_data=TRUE&x_delim_char=|&x_encap_char=&x_relay_response=FALSE";
        }

        private string BuildGatewayRequestPart_CustomerDetails(Order order, AccountDataDictionary accountData)
        {
            StringBuilder customerDetails = new StringBuilder();

            //ADD CUSTOMER INFORMATION

            if (accountData.ContainsKey("AccountName") && !string.IsNullOrEmpty(accountData["AccountName"]))
            {
                //add customer's name
                customerDetails.Append("&cust_name=" + HttpUtility.UrlEncode(accountData["AccountName"]));
            }
            else
            {
                // customer did not provide account name, use customer's billing account name
                customerDetails.Append("&cust_name=" + HttpUtility.UrlEncode(order.BillToFirstName) + " " + HttpUtility.UrlEncode(order.BillToLastName));
            }

            //add customer's address
            customerDetails.Append("&cust_address=" + HttpUtility.UrlEncode(order.BillToAddress1) + " " + HttpUtility.UrlEncode(order.BillToCity) + " " + HttpUtility.UrlEncode(order.BillToProvince));
            //add customer's post code
            customerDetails.Append("&cust_postcode=" + HttpUtility.UrlEncode(order.BillToPostalCode));
            //add customer's country code
            customerDetails.Append("&cust_country=" + HttpUtility.UrlEncode(order.BillToCountryCode));
            //add customer's telephone number
            customerDetails.Append("&cust_tel=" + HttpUtility.UrlEncode(order.BillToPhone));

            //APPEND ADDITIONAL CUSTOMER DATA
            HttpContext context = HttpContext.Current;
            if (context != null)
            {
                customerDetails.Append("&cust_ip=" + context.Request.ServerVariables["REMOTE_HOST"]);
            }
            //TODO: x_customer_tax_id
            customerDetails.Append("&cust_email=" + HttpUtility.UrlEncode(order.BillToEmail));
            //TODO: EMAIL_CUSTOMER AND MERCHANT_EMAIL
            return customerDetails.ToString();
        }

        private string BuildGatewayRequestPart_InvoiceInformation(Order order)
        {
            //APPEND INVOICE INFORMATION
            StringBuilder invoiceDetails = new StringBuilder();
            invoiceDetails.Append("&x_invoice_num=" + order.OrderNumber);
            invoiceDetails.Append("&x_description=" + HttpUtility.UrlEncode(StringHelper.RemoveSpecialChars(order.Store.Name) + " Order #" + order.OrderNumber));
            //TODO: ITEMIZED ORDER INFORMATION?
            return invoiceDetails.ToString();
        }

        private string BuildGatewayRequestPart_CustomerShippingAddress(OrderShipment shipment)
        {
            StringBuilder shippingAddress = new StringBuilder();
            shippingAddress.Append("&x_ship_to_first_name=" + HttpUtility.UrlEncode(shipment.ShipToFirstName));
            shippingAddress.Append("&x_ship_to_last_name=" + HttpUtility.UrlEncode(shipment.ShipToLastName));
            shippingAddress.Append("&x_ship_to_company=" + HttpUtility.UrlEncode(shipment.ShipToCompany));
            shippingAddress.Append("&x_ship_to_address=" + HttpUtility.UrlEncode(shipment.ShipToAddress1));
            shippingAddress.Append("&x_ship_to_city=" + HttpUtility.UrlEncode(shipment.ShipToCity));
            shippingAddress.Append("&x_ship_to_state=" + HttpUtility.UrlEncode(shipment.ShipToProvince));
            shippingAddress.Append("&x_ship_to_zip=" + HttpUtility.UrlEncode(shipment.ShipToPostalCode));
            shippingAddress.Append("&x_ship_to_country=" + HttpUtility.UrlEncode(shipment.ShipToCountryCode));
            return shippingAddress.ToString();
        }

        private string BuildGatewayRequestPart_AuthorizeTransactionData(AuthorizeTransactionRequest authorizeRequest, AccountDataDictionary accountData, Dictionary<string, string> sensitiveData)
        {
            StringBuilder transactionData = new StringBuilder();
            //APPEND AMOUNT
            transactionData.Append(String.Format("&tran_amount={0:F2}", authorizeRequest.Amount));
            //IF CURRENCY CODE IS PASSED, SET IT
            //OTHERWISE LEAVE BLANK TO USE THE DEFAULT IN ANET MERCHANT SETTINGS
            if (!string.IsNullOrEmpty(authorizeRequest.CurrencyCode))
            {
                transactionData.Append("&tran_currency=" + authorizeRequest.CurrencyCode);
            }

            transactionData.Append("&tran_testmode=0&tran_type=sale&tran_class=ecom");

            //DETERMINE METHOD AND TYPE
            Payment payment = authorizeRequest.Payment;
            PaymentInstrumentType instrument = payment.PaymentMethod.PaymentInstrumentType;
            switch (instrument)
            {
                case PaymentInstrumentType.AmericanExpress:
                case PaymentInstrumentType.Discover:
                case PaymentInstrumentType.MasterCard:
                case PaymentInstrumentType.Visa:
                    //transactionData.Append("");
                    bool capture = (this.UseAuthCapture || authorizeRequest.Capture);
                    //transactionData.Append(capture ? "&x_type=AUTH_CAPTURE" : "&x_type=AUTH_ONLY");
                    break;
                case PaymentInstrumentType.Check:
                    //transactionData.Append("&x_method=ECHECK&x_echeck_type=WEB&x_recurring_billing=NO");
                    break;
                default:
                    throw new ArgumentException("This gateway does not support the requested payment instrument: " + instrument.ToString());
            }

            //APPEND PAYMENT INSTRUMENT DETAILS
            //AccountDataDictionary accountData = new AccountDataDictionary(payment.AccountData);
            if (instrument != PaymentInstrumentType.Check)
            {
                string accountNumber = accountData.GetValue("AccountNumber");
                transactionData.Append("&card_num=" + accountNumber);
                if (this.UseDebugMode) sensitiveData[accountNumber] = MakeReferenceNumber(accountNumber);
                string expirationMonth = accountData.GetValue("ExpirationMonth");
                if (expirationMonth.Length == 1) expirationMonth.Insert(0, "0");
                string expirationYear = accountData.GetValue("ExpirationYear");
                transactionData.Append("&card_expiry=" + System.Web.HttpUtility.UrlEncode(expirationMonth + "/" + expirationYear));
                //PROCESS CREDIT CARD ACCOUNT DATA
                string securityCode = accountData.GetValue("SecurityCode");
                if (!string.IsNullOrEmpty(securityCode))
                {
                    transactionData.Append("&card_cvv=" + securityCode);
                    if (this.UseDebugMode) sensitiveData["card_cvv=" + securityCode] = "card_cvv=" + (new string('x', securityCode.Length));
                }
            }
            else
            {
                //PROCESS CHECK ACCOUNT DATA
                string routingNumber = HttpUtility.UrlEncode(accountData.GetValue("RoutingNumber"));
                //transactionData.Append("&x_bank_aba_code=" + routingNumber);
                string accountNumber = HttpUtility.UrlEncode(accountData.GetValue("AccountNumber"));
                //transactionData.Append("&x_bank_acct_num=" + accountNumber);
                if (this.UseDebugMode)
                {
                    //need to replace routing number with truncated version
                    sensitiveData[routingNumber] = MakeReferenceNumber(routingNumber);
                    sensitiveData[accountNumber] = MakeReferenceNumber(accountNumber);
                }
                string accountType = accountData.GetValue("AccountType");
                if (string.IsNullOrEmpty(accountType)) accountType = "CHECKING";
                //transactionData.Append("&x_bank_acct_type=" + HttpUtility.UrlEncode(accountType));
                //transactionData.Append("&x_bank_name=" + HttpUtility.UrlEncode(accountData.GetValue("BankName")));
                //transactionData.Append("&x_bank_acct_name=" + HttpUtility.UrlEncode(accountData.GetValue("AccountHolder")));
                //transactionData.Append("&x_echeck_type=WEB");

                //APPEND WELLS FARGO SECURE SOURCE DETAILS
                //append org type for securesource transactions
                string customerType = accountData.GetValue("CustomerType");
                if (string.IsNullOrEmpty(customerType) || (!customerType.Equals("I") && !customerType.Equals("B"))) customerType = "I";
                //transactionData.Append("&x_customer_organization_type=" + customerType);
                //look for drivers license data
                if (accountData.ContainsKey("LicenseNumber") && accountData.ContainsKey("LicenseState") && accountData.ContainsKey("BirthDate"))
                {
                    string licenseNumber = HttpUtility.UrlEncode(accountData.GetValue("LicenseNumber"));
                    string licenseState = HttpUtility.UrlEncode(accountData.GetValue("LicenseState"));
                    DateTime birthDate = DateTime.Parse(accountData.GetValue("BirthDate"));
                    //transactionData.Append("&x_drivers_license_num=" + licenseNumber);
                    //transactionData.Append("&x_drivers_license_state=" + licenseState);
                    //dob must be in the standard AbleCommerce date format of yyyy-MM-dd
                    //transactionData.Append("&x_drivers_license_dob=" + HttpUtility.UrlEncode(birthDate.ToString("yyyy/MM/dd")));
                    if (this.UseDebugMode)
                    {
                        //need to replace license number with truncated version
                        sensitiveData[licenseNumber] = MakeReferenceNumber(licenseNumber);
                        sensitiveData[HttpUtility.UrlEncode(birthDate.ToString("yyyy/MM/dd"))] = "yyyy/mm/dd";
                    }
                }
            }
            return transactionData.ToString();
        }

        private string BuildGatewayRequestPart_CaptureTransactionData(CaptureTransactionRequest captureRequest)
        {
            StringBuilder transactionData = new StringBuilder();
            //APPEND AMOUNT
            transactionData.Append(String.Format("&tran_amount={0:F2}", captureRequest.Amount));
            //IF CURRENCY CODE IS PASSED, SET IT
            //OTHERWISE LEAVE BLANK TO USE THE DEFAULT IN ANET MERCHANT SETTINGS
            if (!string.IsNullOrEmpty(captureRequest.CurrencyCode))
            {
                transactionData.Append("&tran_currency=" + captureRequest.CurrencyCode);
            }

            transactionData.Append("&tran_testmode=0&tran_class=ecom");

            //DETERMINE METHOD AND TYPE
            Payment payment = captureRequest.Payment;
            PaymentInstrumentType instrument = payment.PaymentMethod.PaymentInstrumentType;
            switch (instrument)
            {
                case PaymentInstrumentType.AmericanExpress:
                case PaymentInstrumentType.Discover:
                case PaymentInstrumentType.MasterCard:
                case PaymentInstrumentType.Visa:
                    Transaction authorizeTransaction = captureRequest.AuthorizeTransaction;
                    transactionData.Append("&tran_ref=" + HttpUtility.UrlEncode(authorizeTransaction.ProviderTransactionId));
                    break;
                default:
                    throw new ArgumentException("This gateway does not support the requested payment instrument: " + instrument.ToString());
            }
            return transactionData.ToString();
        }

        private string BuildGatewayRequestPart_VoidTransactionData(VoidTransactionRequest voidRequest)
        {
            StringBuilder transactionData = new StringBuilder();
            //APPEND AMOUNT
            transactionData.Append(String.Format("&tran_amount={0:F2}", voidRequest.Amount));
            //IF CURRENCY CODE IS PASSED, SET IT
            //OTHERWISE LEAVE BLANK TO USE THE DEFAULT IN ANET MERCHANT SETTINGS
            if (!string.IsNullOrEmpty(voidRequest.CurrencyCode))
            {
                transactionData.Append("&tran_currency=" + voidRequest.CurrencyCode);
            }
            transactionData.Append("&tran_testmode=0&tran_class=ecom");
            //DETERMINE METHOD AND TYPE
            Payment payment = voidRequest.Payment;
            PaymentInstrumentType instrument = payment.PaymentMethod.PaymentInstrumentType;
            switch (instrument)
            {
                case PaymentInstrumentType.AmericanExpress:
                case PaymentInstrumentType.Discover:
                case PaymentInstrumentType.MasterCard:
                case PaymentInstrumentType.Visa:
                    transactionData.Append("&tran_type=void&tran_orig_id=" + HttpUtility.UrlEncode(voidRequest.AuthorizeTransaction.ProviderTransactionId));
                    break;
                default:
                    throw new ArgumentException("This gateway does not support the requested payment instrument: " + instrument.ToString());
            }
            return transactionData.ToString();
        }

        private string BuildGatewayRequestPart_RefundTransactionData(RefundTransactionRequest refundRequest, Dictionary<string, string> sensitiveData)
        {
            StringBuilder transactionData = new StringBuilder();
            //APPEND AMOUNT
            transactionData.Append(String.Format("&tran_amount={0:F2}", refundRequest.Amount));
            //IF CURRENCY CODE IS PASSED, SET IT
            //OTHERWISE LEAVE BLANK TO USE THE DEFAULT IN ANET MERCHANT SETTINGS
            if (!string.IsNullOrEmpty(refundRequest.CurrencyCode))
            {
                transactionData.Append("&tran_currency=" + refundRequest.CurrencyCode);
            }

            transactionData.Append("&tran_testmode=0&tran_type=refund&tran_class=ecom");

            //DETERMINE METHOD AND TYPE
            Payment payment = refundRequest.Payment;
            PaymentMethod method = payment.PaymentMethod;
            PaymentInstrumentType instrument = (method != null) ? method.PaymentInstrumentType : PaymentInstrumentType.Unknown;
            switch (instrument)
            {
                case PaymentInstrumentType.AmericanExpress:
                case PaymentInstrumentType.Discover:
                case PaymentInstrumentType.MasterCard:
                case PaymentInstrumentType.Visa:
                    transactionData.Append("&tran_ref=" + HttpUtility.UrlEncode(refundRequest.CaptureTransaction.ProviderTransactionId));
                    if (!string.IsNullOrEmpty(refundRequest.CardNumber))
                    {
                        transactionData.Append("&card_num=" + refundRequest.CardNumber);
                        if (this.UseDebugMode) sensitiveData[refundRequest.CardNumber] = MakeReferenceNumber(refundRequest.CardNumber);
                    }
                    break;
                default:
                    throw new ArgumentException("This gateway does not support the requested payment instrument: " + instrument.ToString());
            }
            return transactionData.ToString();
        }

        private string BuildGatewayRequestPart_Level2Data(Order order)
        {
            if (order != null)
            {
                decimal taxAmount = (decimal)order.Items.TotalPrice(OrderItemType.Tax);
                decimal shippingAmount = (decimal)order.Items.TotalPrice(OrderItemType.Shipping, OrderItemType.Handling);
                return "&x_tax=" + taxAmount.ToString("F2") + "&x_freight=" + shippingAmount.ToString("F2");
            }
            return string.Empty;
        }

        private string BuildGatewayRequest_Authorize(AuthorizeTransactionRequest request, Dictionary<string, string> sensitiveData)
        {
            //ACCESS REQUIRED DATA FOR BUILDING REQUEST
            Payment payment = request.Payment;
            if (payment == null) throw new ArgumentNullException("request.Payment");
            Order order = payment.Order;
            if (order == null) throw new ArgumentNullException("request.Payment.Order");

            AccountDataDictionary accountData = new AccountDataDictionary(payment.AccountData);

            //GENERATE REQUEST
            StringBuilder sb = new StringBuilder();
            //adding id and password of merchant account
            sb.Append(BuildGatewayRequestPart_MerchantAccountInformation());
            //sb.Append(BuildGatewayRequestPart_GatewayResponseConfiguration());

            sb.Append(BuildGatewayRequestPart_CustomerDetails(order, accountData));
            //sb.Append(BuildGatewayRequestPart_InvoiceInformation(order));
            //IF ONLY ONE SHIPMENT IN ORDER, APPEND CUSTOMER SHIPPING ADDRESS
            if (order.Shipments.Count == 1)
            {
                //sb.Append(BuildGatewayRequestPart_CustomerShippingAddress(order.Shipments[0]));
            }
            sb.Append(BuildGatewayRequestPart_AuthorizeTransactionData(request, accountData, sensitiveData));
            //sb.Append(BuildGatewayRequestPart_Level2Data(order));

            return sb.ToString();
        }

        private string BuildGatewayRequest_Capture(CaptureTransactionRequest request)
        {
            //ACCESS REQUIRED DATA FOR BUILDING REQUEST
            Payment payment = request.Payment;
            if (payment == null) throw new ArgumentNullException("transactionRequest.Payment");
            Transaction authorizeTransaction = request.AuthorizeTransaction;
            if (authorizeTransaction == null) throw new ArgumentNullException("transactionRequest.AuthorizeTransaction");
            //GENERATE REQUEST
            StringBuilder sb = new StringBuilder();
            sb.Append(BuildGatewayRequestPart_MerchantAccountInformation());
            //sb.Append(BuildGatewayRequestPart_GatewayResponseConfiguration());
            sb.Append(BuildGatewayRequestPart_CaptureTransactionData(request));
            //sb.Append(BuildGatewayRequestPart_Level2Data(payment.Order));
            return sb.ToString();
        }

        private string BuildGatewayRequest_Void(VoidTransactionRequest voidRequest)
        {
            //ACCESS REQUIRED DATA FOR BUILDING REQUEST
            Payment payment = voidRequest.Payment;
            if (payment == null) throw new ArgumentNullException("voidRequest.Payment");
            Transaction authorizeTransaction = voidRequest.AuthorizeTransaction;
            if (authorizeTransaction == null) throw new ArgumentNullException("voidRequest.AuthorizeTransaction");
            //GENERATE REQUEST
            StringBuilder request = new StringBuilder();
            request.Append(BuildGatewayRequestPart_MerchantAccountInformation());
            //request.Append(BuildGatewayRequestPart_GatewayResponseConfiguration());
            request.Append(BuildGatewayRequestPart_VoidTransactionData(voidRequest));
            return request.ToString();
        }

        private string BuildGatewayRequest_Refund(RefundTransactionRequest refundRequest, Dictionary<string, string> sensitiveData)
        {
            //ACCESS REQUIRED DATA FOR BUILDING REQUEST
            Payment payment = refundRequest.Payment;
            if (payment == null) throw new ArgumentNullException("refundRequest.Payment");
            Transaction captureTransaction = refundRequest.CaptureTransaction;
            if (captureTransaction == null) throw new ArgumentNullException("refundRequest.AuthorizeTransaction");
            AccountDataDictionary accountData = new AccountDataDictionary(payment.AccountData);
            //GENERATE REQUEST
            StringBuilder request = new StringBuilder();
            request.Append(BuildGatewayRequestPart_MerchantAccountInformation());
            //request.Append(BuildGatewayRequestPart_GatewayResponseConfiguration());
            Order order = payment.Order;
            //request.Append(BuildGatewayRequestPart_InvoiceInformation(order));
            request.Append(BuildGatewayRequestPart_CustomerDetails(order, accountData));
            request.Append(BuildGatewayRequestPart_RefundTransactionData(refundRequest, sensitiveData));
            return request.ToString();
        }

        private string SendRequestToGateway(string requestData)
        {

            //EXECUTE WEB REQUEST, SET RESPONSE
            string response;
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(this.UseTestGateway ? this.NvApiTestUrl : this.NvApiLiveUrl);
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            HttpContext context = HttpContext.Current;
            if (context != null)
            {
                string referer = context.Request.ServerVariables["HTTP_REFERER"];
                if (!string.IsNullOrEmpty(referer)) httpWebRequest.Referer = referer;
            }
            byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes(requestData);
            httpWebRequest.ContentLength = requestBytes.Length;
            using (Stream requestStream = httpWebRequest.GetRequestStream())
            {
                requestStream.Write(requestBytes, 0, requestBytes.Length);
                requestStream.Close();
            }
            using (StreamReader responseStream = new StreamReader(httpWebRequest.GetResponse().GetResponseStream(), System.Text.Encoding.UTF8))
            {
                response = responseStream.ReadToEnd();
                responseStream.Close();
            }
            return response;
        }

        private XmlDocument SendXmlRequestToGateway(XmlDocument requestData)
        {
            string responseData;
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(this.UseTestGateway ? this.XmlApiTestUrl : this.XmlApiLiveUrl);
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "text/xml";
            httpWebRequest.KeepAlive = true;
            HttpContext context = HttpContext.Current;
            if (context != null)
            {
                string referer = context.Request.ServerVariables["HTTP_REFERER"];
                if (!string.IsNullOrEmpty(referer)) httpWebRequest.Referer = referer;
            }
            byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes(requestData.OuterXml);
            httpWebRequest.ContentLength = requestBytes.Length;
            using (Stream requestStream = httpWebRequest.GetRequestStream())
            {
                requestStream.Write(requestBytes, 0, requestBytes.Length);
                requestStream.Close();
            }
            using (StreamReader responseDataStream = new StreamReader(httpWebRequest.GetResponse().GetResponseStream(), System.Text.Encoding.UTF8))
            {
                responseData = responseDataStream.ReadToEnd();
                responseDataStream.Close();
            }
            XmlDocument response = new XmlDocument();
            response.LoadXml(responseData);
            return response;
        }

        public override Transaction DoCapture(CaptureTransactionRequest captureRequest)
        {
            //BUILD THE REQUEST
            string gatewayRequest = BuildGatewayRequest_Capture(captureRequest);
            //RECORD REQUEST
            if (this.UseDebugMode)
            {
                //ALWAYS MASK THE CREDENTIALS
                string credentials = "";//String.Format("x_login={0}&x_tran_key={1}", this.MerchantLogin, this.TransactionKey);
                string debugCredentials = "";//"x_login=xxxxxxxx&x_tran_key=xxxxxxxx";
                this.RecordCommunication(this.Name, CommunicationDirection.Send, gatewayRequest.Replace(credentials, debugCredentials), null);
            }
            //SEND REQUEST
            string response = this.SendRequestToGateway(gatewayRequest);
            //RECORD RESPONSE
            if (this.UseDebugMode) this.RecordCommunication(this.Name, CommunicationDirection.Receive, response, null);
            //PROCESS RESPONSE AND RETURN RESULT
            return this.ProcessGatewayResponse_Capture(captureRequest, response);
        }

        public override Transaction DoVoid(VoidTransactionRequest voidRequest)
        {
            //BUILD THE REQUEST
            string gatewayRequest = BuildGatewayRequest_Void(voidRequest);
            //RECORD REQUEST
            if (this.UseDebugMode)
            {
                //ALWAYS MASK THE CREDENTIALS
                string credentials = "";// String.Format("x_login={0}&x_tran_key={1}", this.MerchantLogin, this.TransactionKey);
                string debugCredentials = "";// "x_login=xxxxxxxx&x_tran_key=xxxxxxxx";
                this.RecordCommunication(this.Name, CommunicationDirection.Send, gatewayRequest.Replace(credentials, debugCredentials), null);
            }
            //SEND REQUEST
            string response = this.SendRequestToGateway(gatewayRequest);
            //RECORD RESPONSE
            if (this.UseDebugMode) this.RecordCommunication(this.Name, CommunicationDirection.Receive, response, null);
            //PROCESS RESPONSE AND RETURN RESULT
            return this.ProcessGatewayResponse_Void(voidRequest.Payment, response, voidRequest);
        }

        public override Transaction DoRefund(RefundTransactionRequest refundRequest)
        {
            Dictionary<string, string> sensitiveData = new Dictionary<string, string>();
            //BUILD THE REQUEST
            string gatewayRequest = BuildGatewayRequest_Refund(refundRequest, sensitiveData);
            //RECORD REQUEST
            if (this.UseDebugMode)
            {
                //ALWAYS MASK THE CREDENTIALS
                string credentials = "";// String.Format("x_login={0}&x_tran_key={1}", this.MerchantLogin, this.TransactionKey);
                string debugCredentials = "";//"x_login=xxxxxxxx&x_tran_key=xxxxxxxx";
                sensitiveData[credentials] = debugCredentials;
                this.RecordCommunication(this.Name, CommunicationDirection.Send, gatewayRequest, sensitiveData);
            }
            //SEND REQUEST
            string response = this.SendRequestToGateway(gatewayRequest);
            //RECORD RESPONSE
            if (this.UseDebugMode) this.RecordCommunication(this.Name, CommunicationDirection.Receive, response, null);
            //PROCESS RESPONSE AND RETURN RESULT
            return this.ProcessGatewayResponse_Refund(refundRequest.Payment, response, refundRequest);
        }

        //I am not using Recurring Transactions for this particular shopping cart site so I commented out the method

        //public override AuthorizeRecurringTransactionResponse DoAuthorizeRecurring(AuthorizeRecurringTransactionRequest authorizeRequest)
        //{
        //    //ACCESS REQUIRED DATA FOR BUILDING REQUEST
        //    Payment payment = authorizeRequest.Payment;
        //    if (payment == null) throw new ArgumentNullException("request.Payment");
        //    Order order = payment.Order;
        //    if (order == null) throw new ArgumentNullException("request.Payment.Order");
        //    Transaction errTrans;
        //    //KEEP TRACK OF SENSITIVE DATA THAT SHOULD NOT BE RECORDED
        //    Dictionary<string, string> debugReplacements = new Dictionary<string, string>();
        //    //GENERATE REQUEST
        //    XmlDocument arbRequest = new XmlDocument();
        //    arbRequest.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\"?><ARBCreateSubscriptionRequest />");
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "merchantAuthentication/name", this.MerchantLogin);
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "merchantAuthentication/transactionKey", this.TransactionKey);
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/name", authorizeRequest.SubscriptionName);
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/paymentSchedule/interval/length", authorizeRequest.PaymentFrequency.ToString());
        //    switch (authorizeRequest.PaymentFrequencyUnit)
        //    {
        //        case PaymentFrequencyUnit.Day:
        //            XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/paymentSchedule/interval/unit", "days");
        //            break;
        //        case PaymentFrequencyUnit.Month:
        //            XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/paymentSchedule/interval/unit", "months");
        //            break;
        //        default:
        //            errTrans = Transaction.CreateErrorTransaction(this.PaymentGatewayId, authorizeRequest, "U", "Unsupported payment frequency unit: " + authorizeRequest.PaymentFrequencyUnit.ToString());
        //            return new AuthorizeRecurringTransactionResponse(errTrans);

        //    }
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/paymentSchedule/startDate", LocaleHelper.LocalNow.ToString("yyyy-MM-dd"));

        //    // IF NUMBER OF PAYMENTS IS 0, ITS NEVER EXPIRING SUBSCRIPTIN. WE NEED TO PASS 9999 AS TOTALOCCURRENCES
        //    if (authorizeRequest.NumberOfPayments <= 0)
        //        XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/paymentSchedule/totalOccurrences", "9999");
        //    else
        //        XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/paymentSchedule/totalOccurrences", authorizeRequest.NumberOfPayments.ToString());

        //    //DETERMINE IF THERE IS A DIFFERENT INITIAL AND RECURRING AMOUNT
        //    if (authorizeRequest.RecurringChargeSpecified)
        //    {
        //        XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/paymentSchedule/trialOccurrences", "1");
        //        XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/amount", authorizeRequest.RecurringCharge.ToString("F2"));
        //        XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/trialAmount", authorizeRequest.Amount.ToString("F2"));
        //    }
        //    else
        //    {
        //        XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/amount", authorizeRequest.Amount.ToString("F2"));
        //    }
        //    //DETERMINE METHOD AND TYPE
        //    AccountDataDictionary accountData = new AccountDataDictionary(payment.AccountData);
        //    PaymentInstrumentType instrument = payment.PaymentMethod.PaymentInstrumentType;
        //    switch (instrument)
        //    {
        //        case PaymentInstrumentType.AmericanExpress:
        //        case PaymentInstrumentType.Discover:
        //        case PaymentInstrumentType.MasterCard:
        //        case PaymentInstrumentType.Visa:
        //            string accountNumber = accountData.GetValue("AccountNumber");
        //            XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/payment/creditCard/cardNumber", accountNumber);
        //            if (this.UseDebugMode) debugReplacements.Add(accountNumber, MakeReferenceNumber(accountNumber));
        //            string expirationMonth = accountData.GetValue("ExpirationMonth");
        //            if (expirationMonth.Length == 1) expirationMonth.Insert(0, "0");
        //            string expirationYear = accountData.GetValue("ExpirationYear");
        //            if (expirationYear.Length == 2) expirationYear = "20" + expirationYear;
        //            XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/payment/creditCard/expirationDate", expirationYear + "-" + expirationMonth);
        //            break;
        //        case PaymentInstrumentType.Check:
        //            errTrans = Transaction.CreateErrorTransaction(this.PaymentGatewayId, authorizeRequest, "E", "Check is not yet implemented!");
        //            return new AuthorizeRecurringTransactionResponse(errTrans);
        //        /*
        //        //PROCESS CHECK ACCOUNT DATA
        //        string routingNumber = HttpUtility.UrlEncode(accountData.GetValue("RoutingNumber"));
        //        transactionData.Append("&x_bank_aba_code=" + routingNumber);
        //        string accountNumber = HttpUtility.UrlEncode(accountData.GetValue("AccountNumber"));
        //        transactionData.Append("&x_bank_acct_num=" + accountNumber);
        //        if (this.UseDebugMode)
        //        {
        //        //need to replace routing number with truncated version
        //        debugReplacements.Add(routingNumber + "|" + MakeReferenceNumber(routingNumber));
        //        debugReplacements.Add(accountNumber + "|" + MakeReferenceNumber(accountNumber));
        //        }
        //        string accountType = accountData.GetValue("AccountType");
        //        if (string.IsNullOrEmpty(accountType)) accountType = "CHECKING";
        //        transactionData.Append("&x_bank_acct_type=" + HttpUtility.UrlEncode(accountType));
        //        transactionData.Append("&x_bank_name=" + HttpUtility.UrlEncode(accountData.GetValue("BankName")));
        //        transactionData.Append("&x_bank_acct_name=" + HttpUtility.UrlEncode(accountData.GetValue("AccountName")));
        //        transactionData.Append("&x_echeck_type=WEB");
        //        */
        //        default:
        //            errTrans = Transaction.CreateErrorTransaction(this.PaymentGatewayId, authorizeRequest, "E", "The requested payment instrument is not supported: " + instrument.ToString());
        //            return new AuthorizeRecurringTransactionResponse(errTrans);
        //    }

        //    //COMBINE ORDER AND PAYMENT ID TO PREVENT DUPLICATE ERRORS
        //    //WHEN MORE THAN ONE SUBSCRIPTION EXISTS FOR A SINGLE ORDER
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/order/invoiceNumber", string.Format("{0}:{1}", order.OrderNumber, payment.SubscriptionId));
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/order/description", string.Format("Order #{0}, Sub #{1}", order.OrderNumber, payment.SubscriptionId));

        //    //CUSTOMER TYPE SHOULD BE 'I' FOR INDIVIDUAL OR 'B' FOR BUSINESS
        //    string customerType = accountData.GetValue("CustomerType");
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/customer/type", ((customerType == "B") ? "business" : "individual"));
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/customer/id", order.UserId.ToString());
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/customer/email", order.BillToEmail);
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/customer/phoneNumber", order.BillToPhone);
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/customer/faxNumber", order.BillToFax, true, false);
        //    //look for drivers license data
        //    if (accountData.ContainsKey("LicenseNumber") && accountData.ContainsKey("LicenseState") && accountData.ContainsKey("BirthDate"))
        //    {
        //        string licenseNumber = HttpUtility.UrlEncode(accountData.GetValue("LicenseNumber"));
        //        string licenseState = HttpUtility.UrlEncode(accountData.GetValue("LicenseState"));
        //        DateTime birthDate = DateTime.Parse(accountData.GetValue("BirthDate"));
        //        XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/customer/driversLicense/number", licenseNumber);
        //        XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/customer/driversLicense/state", licenseState);
        //        XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/customer/driversLicense/dateOfBirth", birthDate.ToString("yyyy-MM-dd"));
        //        if (this.UseDebugMode)
        //        {
        //            //need to replace license number with truncated version
        //            debugReplacements.Add(licenseNumber, MakeReferenceNumber(licenseNumber));
        //        }
        //    }
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/billTo/firstName", StringHelper.Truncate(order.BillToFirstName, 50));
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/billTo/lastName", StringHelper.Truncate(order.BillToLastName, 50));
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/billTo/company", StringHelper.Truncate(order.BillToCompany, 50));
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/billTo/address", StringHelper.Truncate(order.BillToAddress1, 60));
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/billTo/city", StringHelper.Truncate(order.BillToCity, 40));
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/billTo/state", StringHelper.Truncate(order.BillToProvince, 40));
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/billTo/zip", StringHelper.Truncate(order.BillToPostalCode, 20));
        //    XmlUtility.SetElementValue(arbRequest.DocumentElement, "subscription/billTo/country", order.BillToCountryCode);
        //    string requestDocument = arbRequest.OuterXml;
        //    //INSERT THE NAMESPACE FOR THE DOCUMENT ELEMENT
        //    requestDocument = requestDocument.Replace("<ARBCreateSubscriptionRequest", "<ARBCreateSubscriptionRequest xmlns=\"AnetApi/xml/v1/schema/AnetApiSchema.xsd\"");
        //    //RECORD REQUEST
        //    if (this.UseDebugMode)
        //    {
        //        //ALWAYS MASK THE CREDENTIALS
        //        string credentials = String.Format("x_login={0}&x_tran_key={1}", this.MerchantLogin, this.TransactionKey);
        //        string debugCredentials = "x_login=xxxxxxxx&x_tran_key=xxxxxxxx";
        //        debugReplacements[credentials] = debugCredentials;
        //        this.RecordCommunication(this.Name, CommunicationDirection.Send, requestDocument, debugReplacements);
        //    }
        //    //SEND REQUEST
        //    arbRequest = new XmlDocument();
        //    arbRequest.LoadXml(requestDocument);
        //    XmlDocument arbResponse = this.SendXmlRequestToGateway(arbRequest);
        //    //RECORD RESPONSE
        //    if (this.UseDebugMode) this.RecordCommunication(this.Name, CommunicationDirection.Receive, arbResponse.OuterXml, null);
        //    //PROCESS RESPONSE AND RETURN RESULT
        //    XmlNamespaceManager nsmgr = new XmlNamespaceManager(arbResponse.NameTable);
        //    nsmgr.AddNamespace("ns1", "AnetApi/xml/v1/schema/AnetApiSchema.xsd");
        //    string responseCode = XmlUtility.GetElementValue(arbResponse.DocumentElement, "ns1:messages/ns1:message/ns1:code", nsmgr);
        //    string responseMessage = XmlUtility.GetElementValue(arbResponse.DocumentElement, "ns1:messages/ns1:message/ns1:text", nsmgr);
        //    if (XmlUtility.GetElementValue(arbResponse.DocumentElement, "ns1:messages/ns1:resultCode", nsmgr).ToLowerInvariant() == "ok")
        //    {
        //        //SUCCESS RESPONSE 
        //        AuthorizeRecurringTransactionResponse resp = new AuthorizeRecurringTransactionResponse();
        //        Transaction transaction;
        //        // create two transactions if there was a separate initial charge
        //        if (authorizeRequest.RecurringChargeSpecified)
        //        {
        //            transaction = new Transaction();
        //            transaction.PaymentGatewayId = this.PaymentGatewayId;
        //            transaction.TransactionType = TransactionType.Authorize;
        //            transaction.TransactionStatus = TransactionStatus.Successful;
        //            transaction.Amount = authorizeRequest.Amount;
        //            transaction.RemoteIP = authorizeRequest.RemoteIP;
        //            transaction.ResponseCode = responseCode;
        //            transaction.ResponseMessage = responseMessage;
        //            transaction.TransactionDate = LocaleHelper.LocalNow;
        //            transaction.AuthorizationCode = XmlUtility.GetElementValue(arbResponse.DocumentElement, "ns1:subscriptionId", nsmgr);
        //            resp.Transactions.Add(transaction);
        //        }
        //        transaction = new Transaction();
        //        transaction.PaymentGatewayId = this.PaymentGatewayId;
        //        transaction.TransactionType = TransactionType.AuthorizeRecurring;
        //        transaction.TransactionStatus = TransactionStatus.Successful;
        //        transaction.Amount = authorizeRequest.RecurringChargeSpecified ? authorizeRequest.RecurringCharge : authorizeRequest.Amount;
        //        transaction.RemoteIP = authorizeRequest.RemoteIP;
        //        transaction.ResponseCode = responseCode;
        //        transaction.ResponseMessage = responseMessage;
        //        transaction.TransactionDate = LocaleHelper.LocalNow;
        //        transaction.AuthorizationCode = XmlUtility.GetElementValue(arbResponse.DocumentElement, "ns1:subscriptionId", nsmgr);
        //        resp.Transactions.Add(transaction);
        //        return resp;
        //        //return new AuthorizeRecurringTransactionResponse(transaction);
        //    }
        //    //ERROR RESPONSE
        //    errTrans = Transaction.CreateErrorTransaction(this.PaymentGatewayId, TransactionType.AuthorizeRecurring, authorizeRequest.Amount, responseCode, responseMessage, authorizeRequest.RemoteIP);
        //    return new AuthorizeRecurringTransactionResponse(errTrans);
        //}

        public enum GatewayModeOption : int
        {
            ProductionServerLiveMode = 0, ProductionServerTestMode, TestServerLiveMode, TestServerTestMode
        }

    }
}
