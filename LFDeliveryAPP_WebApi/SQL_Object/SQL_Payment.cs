using Dapper;
using DbClass;
using LFDeliveryAPP_WebApi.Class.DTOs;
using LFDeliveryAPP_WebApi.Class.SAP;
using LFDeliveryAPP_WebApi.Model.Other;
using LFDeliveryAPP_WebApi.Model.Payment;
using LFDeliveryAPP_WebApi.Model.SQL_Ex;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.SQL_Object
{
    public class SQL_Payment:IDisposable
    {
        public string LastErrorMessage { get; set; }
        SqlConnection conn;
        SqlTransaction trans;

        public void Dispose() => GC.Collect();
        string _MWdbConnectionStr;
        string _SAPdbConnectionStr;
        DBCommon _currentDB;
        IConfiguration _configuration;
        public SQL_Payment(IConfiguration configuration, string MWdbConnectionStr, string SAPdbConnectionStr, DBCommon currentDB)
        {
            _currentDB = currentDB;
            _SAPdbConnectionStr = SAPdbConnectionStr;
            _MWdbConnectionStr = MWdbConnectionStr;
            _configuration = configuration;
        }

        public List<OACT> GetAcct()
        {
            try
            {
                conn = new SqlConnection(_SAPdbConnectionStr);
                var query = "SELECT * FROM ["+_currentDB.CompanyDB + "]..OACT WHERE (Finanse = 'N') AND (Postable = 'Y')";

                var result = conn.Query<OACT>(query).ToList();

                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }

        public string GetPaymentAttachment(string QueryGuid)
        {
            try
            {
                conn = new SqlConnection(_MWdbConnectionStr);
                string query = "SELECT AttachmentList FROM PaymentFileUpload WHERE Guid = @QueryGuid;";

                var result = conn.Query<string>(query, new { QueryGuid = QueryGuid }).FirstOrDefault();

                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }

        public List<PaymentMean> GetPaymentInvoiceMeans(string QueryGuid)
        {
            try
            {
                conn = new SqlConnection(_MWdbConnectionStr);
                string query = "SELECT * FROM PaymentMean WHERE Guid = @QueryGuid;";

                var result = conn.Query<PaymentMean>(query, new { QueryGuid = QueryGuid }).ToList();

                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }

        public List<IncomingPaymentDetail> GetPaymentInvoiceLines(string QueryGuid)
        {
            try
            {
                conn = new SqlConnection(_MWdbConnectionStr);
                string query = "SELECT * FROM IncomingPaymentDetail WHERE Guid = @QueryGuid;";

                var result = conn.Query<IncomingPaymentDetail>(query, new { QueryGuid = QueryGuid}).ToList();

                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }

        public int UpdateHeader(IncomingPaymentHeader header)
        {
            try
            {
                conn = new SqlConnection(_MWdbConnectionStr);
                string updateQuery = "UPDATE IncomingPaymentHeader SET IsPost = 1 WHERE Guid = @Guid AND CompanyID = @CompanyID;";

                var result = conn.Execute(updateQuery, new { Guid = header.Guid, CompanyID =_currentDB.CompanyID } );

                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }

        public int InsertPayment(DTOPayment dTOPayment)
        {
            try
            {
                conn = new SqlConnection(_MWdbConnectionStr);

                if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                using var trans = conn.BeginTransaction();

                #region Header
                string headerQuery = @"INSERT INTO [dbo].[IncomingPaymentHeader]
                                       ([Guid]
                                       ,[CompanyId]
                                       ,[CustomerCode]
                                       ,[CustomerName]
                                       ,[DriverCode]
                                       ,[DriverName]
                                       ,[CreatedDate]
                                       ,[IsPost])
                                         VALUES (
                                         @Guid,
                                         @CompanyId,
                                         @CustomerCode,
                                         @CustomerName,
                                         @DriverCode,
                                         @DriverName,
                                         GETDATE(),
                                         0)";
                var result = conn.Execute(headerQuery, dTOPayment.IncomingPaymentHeader, trans);
                #endregion

                #region Detail
                string detailQuery = @"INSERT INTO [dbo].[IncomingPaymentDetail]
                                       ([Guid]
                                       ,[LineGuid]
                                       ,[DocEntry]
                                       ,[DocNum]
                                       ,[Amount]
                                       ,[CreatedDate])
                                         VALUES (
                                         @Guid,
                                         @LineGuid,
                                         @DocEntry,
                                         @DocNum,
                                         @Amount,
                                         GETDATE());";

                result = conn.Execute(detailQuery, dTOPayment.IncomingPaymentDetails, trans);
                #endregion

                #region Payment Mean
                string paymenMeanQuery = @"INSERT INTO [dbo].[PaymentMean]
                                           ([PaymentMethod]
                                           ,[AcctCode]
                                           ,[BankName]
                                           ,[BankCode]
                                           ,[CheqNum]
                                           ,[Amount]
                                           ,[Guid]
                                           ,[DueDate]
                                           ,[Reference])
                                             VALUES (
                                            @PaymentMethod,
                                            @AcctCode,
                                            @BankName,
                                            @BankCode,
                                            @CheqNum,
                                            @Amount,
                                            @Guid,
                                            @DueDate,
                                            @Reference);";

                foreach(var line in dTOPayment.PaymentMeans)
                {
                    result = conn.Execute(paymenMeanQuery, 
                        new { PaymentMethod = line.PaymentMethod,
                              AcctCode = line.AcctCode,
                              BankName = line.BankName,
                              BankCode = line.BankCode,
                              CheqNum = line.CheqNum,
                              Amount = line.Amount,
                              Guid = line.Guid,
                              DueDate = (line.DueDate == DateTime.MinValue) ? null : line.DueDate.ToString("yyyy-MM-dd"),
                              Reference = line.Reference
                    }, trans);

                }
                #endregion

                trans.Commit();
                return result;
            }
            catch (Exception excep)
            {
                trans.Rollback();
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }

        public List<int> GetAvailableInvoiceFromPick(string QueryCardCode)
        {
            try
            {
                conn = new SqlConnection(_MWdbConnectionStr);
                string query = "SELECT InvoiceDocEntry FROM DispatchDetail WHERE CardCode = @CardCode " +
                    "AND CompanyID = @CompanyID " +
                    "AND InvoiceDocEntry not in " +
                    "(SELECT T0.DocEntry from IncomingPaymentDetail T0 " +
                    "INNER JOIN IncomingPaymentHeader T1 ON T0.Guid = T1.Guid "+
                    "WHERE T1.CompanyId = @CompanyID " +
                    "AND T1.IsPost = 1);";

                var result = conn.Query<int>(query, new { CardCode = QueryCardCode, CompanyID = _currentDB.CompanyID }).ToList();

                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }

        public List<OINV_Ex> GetInvoices(List<int> docNos)
        {
            try
            {
                List<OINV_Ex> oINVs = new List<OINV_Ex>();
                conn = new SqlConnection(_SAPdbConnectionStr);

                string query = "SELECT * FROM "+_currentDB.CompanyDB+".[dbo].[OINV] WHERE DocEntry = @docEntry";

                foreach (var docNo in docNos)
                {
                    var result = conn.Query<OINV_Ex>(query, new { docEntry = docNo }).First();
                    oINVs.Add(result);
                }
                return oINVs;

            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }

        public int InsertAttachmentTable(IncomingPaymentHeader header)
        {
            try
            {
                var conn = new SqlConnection(_MWdbConnectionStr);
                int result = -1;

                string insertQuery = @"INSERT INTO [dbo].[PaymentFileUpload]
                                      ([Guid]
                                      ,[UploadDate]
                                      ,[AppUser]
                                      ,[AttachmentList])
                                        VALUES (
                                       @Guid,
                                       GETDATE(),   
                                       @AppUser,
                                       @AttachmentList);";

                    result = conn.Execute(insertQuery, new
                    {
                        Guid = header.Guid,
                        AppUser = header.DriverCode,
                        AttachmentList = header.attachmentnameList
                    });

                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }

    }
}
