using Dapper;
using LFDeliveryAPP_WebApi.Class.SAP;
using LFDeliveryAPP_WebApi.Model.Payment;
using Microsoft.Extensions.Configuration;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.SAP_DIAPI
{
    public class Payment_Diapi : IDisposable
    {
        public void Dispose() => GC.Collect();

        string _MWdbConnectionStr;
        string _SAPdbConnectionStr;

        IConfiguration _configuration;
        Company oCom { get; set; }
        public string LastErrorMessage { get; private set; }

        public IncomingPaymentHeader PaymentHeader { get; set; } = null;
        public List<IncomingPaymentDetail> PaymentDetails { get; set; } = null;
        public List<PaymentMean> PaymentMeans { get; set; } = null;
        public string CompanyDB { get; set; } = null;


        public Payment_Diapi(IConfiguration configuration, string MWdbConnectionStr, string SAPdbConnectionStr)
        {
            _SAPdbConnectionStr = SAPdbConnectionStr;
            _MWdbConnectionStr = MWdbConnectionStr;
            _configuration = configuration;
        }

        public int SetSAPCompany()
        {
            try
            {
                var conn = new SqlConnection(_MWdbConnectionStr);
                string companyQuery = "SELECT * FROM DBCommon WHERE CompanyDB = @CompanyDB";

                var companyResult = conn.Query<DBCommon>(companyQuery, new { @CompanyDB = CompanyDB }).FirstOrDefault();

                if(companyResult == null)
                {
                    LastErrorMessage = "Company not found in DBCommon";
                    return -1;
                }

                oCom = new SAPbobsCOM.Company();
                oCom.Server = companyResult.Server;
                oCom.CompanyDB = companyResult.CompanyDB;
                oCom.DbUserName = companyResult.DbUserName;
                oCom.DbPassword = companyResult.DbPassword;
                oCom.DbServerType = (SAPbobsCOM.BoDataServerTypes)companyResult.DbServerType;
                oCom.UserName = companyResult.UserName;
                oCom.Password = companyResult.Password;
                oCom.SLDServer = companyResult.SLDServer;
                oCom.Connect();
                if (oCom.Connected)
                    return 0;

                LastErrorMessage = oCom.GetLastErrorDescription();
                return -1;
            }
            catch (Exception e)
            {
                LastErrorMessage = "Fail to Connect SAP";
                Console.WriteLine(e);
                return -1;
            }

        }

        public int PostToPaymentDoc()
        {
            try
            {
                int chequecount = 0;
                int IsSAPConnect = SetSAPCompany();

                if (IsSAPConnect < 0) return -1;
                
                Payments py = (Payments)oCom.GetBusinessObject(BoObjectTypes.oPaymentsDrafts);
                py.DocObjectCode = BoPaymentsObjectType.bopot_IncomingPayments;
                var curSapTableName = "OPDF";
                py.CardCode = PaymentHeader.CustomerCode;
                py.DocTypte = BoRcptTypes.rCustomer;
                py.DocDate = DateTime.Now;

                for (int i = 0; i < PaymentMeans.Count; i++)
                {
                    if(PaymentMeans[i].PaymentMethod == "Cash")
                    {
                        py.CashSum = (RounToTwoDecimalPlace((double)PaymentMeans[i].Amount));
                    }
                    else if(PaymentMeans[i].PaymentMethod == "Bank In")
                    {
                        var transferAcc = GetBankCodeGLAccount(PaymentMeans[i].BankCode);
                        if (transferAcc != null && transferAcc.Length > 0)
                        {
                            py.TransferAccount = $"{transferAcc}";
                        }
                        py.TransferDate = PaymentMeans[i].DueDate;
                        py.TransferReference = PaymentMeans[i].Reference;
                        py.TransferSum = RounToTwoDecimalPlace((double)PaymentMeans[i].Amount);
                    }
                    else if(PaymentMeans[i].PaymentMethod == "Cheque")
                    {
                        if (chequecount > 0)
                        {
                            py.Checks.Add();
                        }
                        bool isNumeric = Int32.TryParse(PaymentMeans[i].CheqNum, out int result);
                        if (isNumeric)
                        {
                            py.Checks.CheckNumber = result;
                        }
                        py.Checks.CheckSum = RounToTwoDecimalPlace((double)PaymentMeans[i].Amount);
                        py.Checks.Details = "App Cheque Collected";
                        py.Checks.DueDate = PaymentMeans[i].DueDate;
                        chequecount++;
                    }
                }

                for (int i = 0; i < PaymentDetails.Count; i++)
                {
                    if(i>0)
                        py.Invoices.Add();

                    py.Invoices.SetCurrentLine(i);
                    py.Invoices.DocEntry = PaymentDetails[i].DocEntry;
                    py.Invoices.SumApplied = RounToTwoDecimalPlace((double)PaymentDetails[i].Amount);
                    py.Invoices.AppliedFC = 0;
                    py.Invoices.DiscountPercent = 0;
                }

                int addResult = py.Add();
                if (addResult == 0)
                {
                    var newKey = Convert.ToInt32(oCom.GetNewObjectKey());
                    var result = newKey.ToString(); 
                    var docNum = GetDocNumberbyDoEntry(result, curSapTableName);

                    return (docNum);
                }
                else
                {
                    LastErrorMessage = $"{oCom.GetLastErrorCode()}\n{oCom.GetLastErrorDescription()}";
                }

                return addResult;
            }
            catch (Exception excep)
            { 
                LastErrorMessage = "Fail to Post SAP.";
                LastErrorMessage += excep.ToString();
                return -1;
            }
        }

        double RounToTwoDecimalPlace(double mathVal)
        {
            return Math.Round(mathVal, 2);
        }

        string GetBankCodeGLAccount(string BankCode)
        {
            try
            {
                string query = "SELECT GLAccount " +
                    "FROM "+ CompanyDB + ".[dbo].[ODSC] t0 INNER JOIN " + CompanyDB + ".[dbo].DSC1 t1 ON t0.BankCode = t1.BankCode  " +
                    "WHERE t0.BankCode = @BankCode";

                return new SqlConnection(_SAPdbConnectionStr).ExecuteScalar<string>(query, new { BankCode });
            }
            catch (Exception e)
            {
                LastErrorMessage = e.ToString();
                return string.Empty;
            }
        }

        int GetDocNumberbyDoEntry(string docEntry, string tableName)
        {
            try
            {
                string query = $"SELECT DocNum " +
                                $"FROM " + CompanyDB + $".[dbo].{tableName} " +
                                $"WHERE DocEntry=@docEntry";

                var result = new SqlConnection(_SAPdbConnectionStr).ExecuteScalar<int>(query, new { docEntry });
                return result;
            }
            catch (Exception e)
            {
                //Log($"{e.Message}\n{e.StackTrace}");
                return -1;
            }
        }

    }
}
