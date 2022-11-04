using Dapper;
using LFDeliveryAPP_WebApi.Class;
using LFDeliveryAPP_WebApi.Class.DTOs;
using LFDeliveryAPP_WebApi.Class.SAP;
using LFDeliveryAPP_WebApi.Model.Dispatch;
using LFDeliveryAPP_WebApi.Model.SQL_Ex;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.SQL_Object
{
    public class SQL_Dispatch : IDisposable
    {
        public string LastErrorMessage { get; set; }
        SqlConnection conn;
        SqlTransaction trans;

        public void Dispose() => GC.Collect();
        string _MWdbConnectionStr;
        DBCommon _currentDB;
        IConfiguration _configuration;
        public SQL_Dispatch(IConfiguration configuration, string MWdbConnectionStr, DBCommon currentDB)
        {
            _currentDB = currentDB;
            _MWdbConnectionStr = MWdbConnectionStr;
            _configuration = configuration;
        }

        public string GetInvoiceReportURL(DispatchDetail detail)
        {
            try
            {
                var conn = new SqlConnection(_MWdbConnectionStr);
                var selectQuery = "SELECT FilePath FROM SAPInvoice WHERE CompanyID = @CompanyID AND InvoiceDocEntry = @InvoiceDocEntry; ";

                return conn.Query<string>(selectQuery, new { CompanyId = detail.CompanyID, InvoiceDocEntry = detail.InvoiceDocEntry }).FirstOrDefault();
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }

        public string GetAttachmentListStr(string docEntries, string dbStr)
        {
            try
            {
                using (var conn = new SqlConnection(_MWdbConnectionStr))
                {
                    string query = @"SELECT AttachmentList FROM FileUpload T0 
                                     INNER JOIN DispatchDetail T1 ON T0.HeaderGuid = T1.Guid AND T0.LineGuid = T1.LineGuid
                                     WHERE T1.InvoiceDocEntry = @docentry AND CompanyID = @CompanyID";

                    return conn.Query<string>(query, new { docentry = docEntries, CompanyID = dbStr }).FirstOrDefault();
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }
        public List<INV1_Ex> GetInvoiceDetailLine(string docEntries)
        {
            try
            {
                using (var conn = new SqlConnection(_MWdbConnectionStr))
                {
                    string query = "SELECT * FROM [" + _currentDB.CompanyDB + "].[dbo].[INV1] WHERE DocEntry = @docentry";

                    return conn.Query<INV1_Ex>(query, new { docentry = docEntries }).ToList();
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }

        public List<string> GetAllDeliveredInvoice(string driverCode, DateTime startTime, DateTime endTime, string docEntry)
        {
            try
            {
                conn = new SqlConnection(_MWdbConnectionStr);
                string query = $"SELECT T0.InvoiceDocEntry FROM DispatchDetail T0 " +
                               $"INNER JOIN DispatchHeader T1 ON T0.Guid = T1.Guid "+
                               $"WHERE T1.DriverCode = @Driver AND T0.Status = 'Delivered' AND T0.CompanyID = @CompanyID "+
                               $"AND CAST(T0.DeliveredTime as date) >= CAST(@StartTime as date) AND CAST(T0.DeliveredTime as date) <= CAST(@EndTime as date)";
                if (!string.IsNullOrEmpty(docEntry))
                {
                    query = $"SELECT T0.InvoiceDocEntry FROM DispatchDetail T0 " +
                                   $"INNER JOIN DispatchHeader T1 ON T0.Guid = T1.Guid " +
                                   $"WHERE T1.DriverCode = @Driver AND T0.Status = 'Delivered' AND T0.CompanyID = @CompanyID AND T0.DocEntry = @DocEntry ";
                }

                var result = conn.Query<string>(query,new { Driver = driverCode, CompanyID = _currentDB.CompanyID, StartTime = startTime, EndTime = endTime, DocEntry = docEntry}).ToList();
                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }

        public int InsertInvoiceReporting(List<DispatchDetail> invoiceList)
        {
            try
            {
                var conn = new SqlConnection(_MWdbConnectionStr);
                int result = -1;

                string insertQuery = @"INSERT INTO [dbo].[SAPInvoice]
                                       ([CompanyId]
                                       ,[InvoiceDocEntry]
                                       ,[IsTry]
                                       ,[IsCreated])
                                        VALUES (
                                       @CompanyId,
                                       @DocEntry,
                                       0,
                                       0);";
                foreach (var invoice in invoiceList)
                {
                    result = conn.Execute(insertQuery, new
                    {
                        CompanyId = invoice.CompanyID,
                        DocEntry = invoice.InvoiceDocEntry
                    });

                }

                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }

        public int InsertAttachmentTable(DTODispatch dTODispatch)
        {
            try
            {
                var conn = new SqlConnection(_MWdbConnectionStr);
                int result = -1;

                var dictintLineGuid = dTODispatch.DispatchDetails.Select(x => x.LineGuid).Distinct().ToList();

                string insertQuery = @"INSERT INTO [dbo].[FileUpload]
                                      ([HeaderGuid]
                                      ,[LineGuid]
                                      ,[UploadDate]
                                      ,[AppUser]
                                      ,[AttachmentList])
                                        VALUES (
                                       @HeaderGuid,
                                       @LineGuid,
                                       GETDATE(),   
                                       @AppUser,
                                       @AttachmentList);";
                foreach(var lineguid in dictintLineGuid)
                {
                    result = conn.Execute(insertQuery, new
                    {
                        HeaderGuid = dTODispatch.DispatchHeader.Guid,
                        LineGuid = lineguid,
                        AppUser = dTODispatch.DispatchHeader.DriverCode,
                        AttachmentList = dTODispatch.DispatchDetails.Select(x => x.attachmentnameList).FirstOrDefault()
                    });

                }

                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }
        public int UpdateDispatchDetailStatus(DTODispatch dTODispatch)
        {
            try
            {
                var conn = new SqlConnection(_MWdbConnectionStr);
                int result = -1;

                if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                using var trans = conn.BeginTransaction();

                var dictintLineGuid = dTODispatch.DispatchDetails.Where(x=>x.Status== "InTransit").Select(x => x.LineGuid).Distinct().ToList();
                string updateLineQuery = @"UPDATE DispatchDetail SET Status = @Status, DeliveredTime = GETDATE()
                                       WHERE Guid = @Guid AND LineGuid = @LineGuid AND CompanyID = @CompanyID";
                foreach(var lineguid in dictintLineGuid)
                {
                    result = conn.Execute(updateLineQuery, new { Status = dTODispatch.QueryStatus, Guid = dTODispatch.DispatchHeader.Guid, LineGuid = lineguid, CompanyID = _currentDB.CompanyID}, trans);
                }
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

        public int CheckToUpdatedHeader(DTODispatch dTODispatch)
        {
            try
            {
                string updateHeaderQuery = null;
                int result = -1;
                var conn = new SqlConnection(_MWdbConnectionStr);

                string selectquery = "SELECT * FROM DispatchDetail WHERE Guid = @guid AND CompanyID = @CompanyID";

                var detailList = conn.Query<DispatchDetail>(selectquery, new { guid = dTODispatch.DispatchHeader.Guid, CompanyID = _currentDB.CompanyID }).ToList();

                var statusfilter = detailList.Where(x => x.Status == "Delivered").ToList();

                updateHeaderQuery = @"UPDATE DispatchHeader SET LastUpdatedDate = GETDATE()
                                          WHERE Guid = @guid AND CompanyID = @CompanyID";

                if (statusfilter.Count == detailList.Count)
                {
                    updateHeaderQuery = @"UPDATE DispatchHeader SET DocStatus = 'Delivered', LastUpdatedDate = GETDATE()
                                          WHERE Guid = @guid AND CompanyID = @CompanyID";
                }

                result = conn.Execute(updateHeaderQuery, new { Guid = dTODispatch.DispatchHeader.Guid, CompanyID = _currentDB.CompanyID});
                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }
        public List<OINV_Ex> GetInvoiceHeaders(List<string> docEntries)
        {
            try
            {
                List<OINV_Ex> oINVs = new List<OINV_Ex>();
                using (var conn = new SqlConnection(_MWdbConnectionStr))
                {
                    string query = "SELECT T0.*, T1.Name[ContactPerson], T1.Tel1[ContactNo], T2.DeliveredTime [DeliveredDate] FROM [" + _currentDB.CompanyDB +"]..[OINV] T0"+
                                   " LEFT JOIN [" + _currentDB.CompanyDB + "]..[OCPR] T1 ON T0.CntctCode = T1.CntctCode" +
                                   " INNER JOIN DispatchDetail T2 ON T0.DocEntry = T2.InvoiceDocEntry" +
                                   " WHERE T0.DocEntry = @DocEntry";

                    foreach (var docentry in docEntries)
                    {
                        var oinvLine = conn.Query<OINV_Ex>(query, new { DocEntry = docentry }).ToList();
                        oINVs.AddRange(oinvLine);
                    }
                }
                return oINVs;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }
        public List<INV1_Ex> GetInvoiceDetails(List<string> docEntries)
        {
            try
            {
                List<INV1_Ex> iNV1s = new List<INV1_Ex>();
                using (var conn = new SqlConnection(_MWdbConnectionStr))
                {
                    string query = "SELECT * FROM [" + _currentDB.CompanyDB + "].[dbo].[INV1] WHERE DocEntry = @docentry";

                    foreach (var docentry in docEntries)
                    {
                        var inv1Line = conn.Query<INV1_Ex>(query, new { docentry = docentry }).ToList();
                        iNV1s.AddRange(inv1Line);
                    }
                }
                return iNV1s;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }
        public List<OINV_Ex> CheckExistingDraft(string driver)
        {
            try
            {
                List<int> invoiceList;
                List<OINV_Ex> InvoiceResults = new List<OINV_Ex>();
                conn = new SqlConnection(_MWdbConnectionStr);

                #region CheckExistingDraft
                string query = "SELECT * FROM DispatchDetailDraft WHERE DriverCode = @driver AND CompanyID = @CompanyID";
                var result = conn.Query<DispatchDetail>(query, new { driver = driver, CompanyID = _currentDB.CompanyID }).ToList();
                if (result == null || result.Count == 0) return null;

                invoiceList = result.Select(x=>x.InvoiceDocEntry).Distinct().ToList();
                #endregion

                using (var sapconn = new SqlConnection(_MWdbConnectionStr))
                {
                    #region GetInvoiceList
                    string headerquery = "SELECT T0.*, T1.Name [ContactPerson], T1.Tel1 [ContactNo] FROM [" + _currentDB.CompanyDB + "].[dbo].[OINV] T0"+
                                         " LEFT JOIN [" + _currentDB.CompanyDB + "].[dbo].[OCPR] T1 ON T0.CntctCode = T1.CntctCode" +
                                         " WHERE DocEntry = @QueryDocEntry";

                    foreach(var line in invoiceList)
                    {
                        var invoiceresult = sapconn.Query<OINV_Ex>(headerquery, new { QueryDocEntry = line }).FirstOrDefault();
                        InvoiceResults.Add(invoiceresult);
                    }

                    foreach(var line in InvoiceResults)
                    {
                        line.PickIdNo = result.FirstOrDefault(x => x.InvoiceDocEntry == line.DocEntry).PickNo.ToString();
                    }
                    #endregion

                    return InvoiceResults;
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }
        public int RemoveDraft(string driver, string docEntry)
        {
            try
            {
                int result = -1;
                conn = new SqlConnection(_MWdbConnectionStr);
                if (docEntry == "-1" || docEntry == null)
                {
                    string deletequery = "Delete FROM DispatchDetailDraft WHERE DriverCode = @driver AND CompanyID = @CompanyID";
                    result = conn.Execute(deletequery, new { driver = driver, CompanyID =_currentDB.CompanyID });
                }
                else 
                {
                    string deletequery = "Delete FROM DispatchDetailDraft WHERE DriverCode = @driver AND PickNo = @PickNo  AND CompanyID = @CompanyID";
                    result = conn.Execute(deletequery, new { driver = driver, PickNo = docEntry, CompanyID = _currentDB.CompanyID });
                }

                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }
        public int CreateDispatchDoc(DTODispatch dTODelivery)
        {
            int count = -1;
            try
            {
                using var conn = new SqlConnection(_MWdbConnectionStr);
                string sql = "SELECT COUNT(*) FROM DispatchHeader";
                count = conn.ExecuteScalar<int>(sql);

                string draftsql = "SELECT * FROM DispatchDetailDraft WHERE DriverCode = @driver AND CompanyID = @CompanyID ";
                var draft = conn.Query<DispatchDetail>(draftsql, new { driver = dTODelivery.DispatchHeader.DriverCode, CompanyID = _currentDB.CompanyID }).ToList();

                if (draft != null)
                {
                    foreach (var line in dTODelivery.DispatchDetails)
                    {
                        line.LoadTime = draft.Where(y => y.PickNo == line.PickNo).FirstOrDefault().LoadTime;
                    }
                }

                if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                using var trans = conn.BeginTransaction();

                #region DispatchHeader
                dTODelivery.DispatchHeader.DocEntry = count + 1;
                string headerQuery = @"INSERT INTO DispatchHeader
                                                     ( [DocEntry]
                                                    ,[Guid]
                                                    ,[CompanyID]
                                                    ,[TruckNum]
                                                    ,[DriverCode]
                                                    ,[DriverName]
                                                    ,[CreatedDate]
                                                    ,[LastUpdatedDate]
                                                    ,[DocStatus] 
                                                   ) VALUES (
                                                     @DocEntry,
                                                     @Guid,
                                                     @CompanyID,
                                                     @TruckNum,
                                                     @DriverCode,
                                                     @DriverName, 
                                                     GETDATE(), 
                                                     GETDATE(),
                                                     @DocStatus );";

                conn.Execute(headerQuery, dTODelivery.DispatchHeader, trans);
                #endregion

                #region DispatchDetail
                dTODelivery.DispatchDetails.ForEach(x => x.DocEntry = count + 1);
                string detailsQuery = @"INSERT INTO DispatchDetail
                                                     ( [DocEntry]
                                                     ,[Guid]
                                                     ,[CompanyID]
                                                     ,[LineGuid]
                                                     ,[PickNo]
                                                     ,[InvoiceDocNum]
                                                     ,[InvoiceDocEntry]
                                                     ,[CardCode]
                                                     ,[CardName]
                                                     ,[Status]
                                                     ,[CreatedDate] 
                                                     ,[LoadTime]
                                                     ,[InTransitTime]
                                                     ) VALUES (
                                                      @DocEntry,
                                                      @Guid, 
                                                      @CompanyID,
                                                      @LineGuid,
                                                      @PickNo,
                                                      @InvoiceDocNum,
                                                      @InvoiceDocEntry, 
                                                      @CardCode, 
                                                      @CardName, 
                                                      @Status,
                                                      GETDATE(), 
                                                      @LoadTime,
                                                      GETDATE() );";

                var result = conn.Execute(detailsQuery, dTODelivery.DispatchDetails, trans);
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
        public int InsertDispatchLineDraft(List<OINV_Ex> OINVList, string driverCode, string driverName)
        {
            try
            {
                int result = -1;
                conn = new SqlConnection(_MWdbConnectionStr);
                string insertquery = @"INSERT INTO [DispatchDetailDraft] (
                                       [CompanyID]
                                      ,[DriverCode]
                                      ,[DriverName]
                                      ,[PickNo]
                                      ,[InvoiceDocNum]
                                      ,[InvoiceDocEntry]
                                      ,[CardCode]
                                      ,[CardName]
                                      ,[Status]
                                      ,[LoadTime]
                                      ,[CreatedDate]
                                        ) VALUES (
                                        @CompanyID,
                                        @Driver, 
                                        @DriverName,
                                        @PickNo,
                                        @InvoiceDocNum,
                                        @InvoiceDocEntry,
                                        @CardCode,
                                        @CardName,
                                        'Loaded',
                                        GetDate(),
                                        GetDate()
                                        )";

                foreach (var line in OINVList)
                {
                    result = conn.Execute(insertquery, new
                    {
                        CompanyID =_currentDB.CompanyID,
                        Driver = driverCode,
                        DriverName = driverName,
                        PickNo = line.PickIdNo,
                        InvoiceDocNum = line.DocNum,
                        InvoiceDocEntry = line.DocEntry,
                        CardCode = line.CardCode,
                        CardName = line.CardName,
                    });;
                }

                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }
        public List<OINV_Ex> GetInvoiceFromOPKL(string docEntry, string truck)
        {
            try
            {
                using (var sapconn = new SqlConnection(_MWdbConnectionStr))
                {
                    List<OINV_Ex> oINVs = new List<OINV_Ex>();
                    #region GetOINVDocEntry From PickList
                    string headerquery = "SELECT DISTINCT T0.DocEntry " +
                                         "FROM [" + _currentDB.CompanyDB + "].[dbo].[INV1] T0 " +
                                         "INNER JOIN [" + _currentDB.CompanyDB + "].[dbo].OINV T1 ON T0.DocEntry = T1.DocEntry " +
                                         "INNER JOIN [" + _currentDB.CompanyDB + "].[dbo].PKL1 T2 on T0.PickIdNo = T2.AbsEntry " +
                                         "INNER JOIN [" + _currentDB.CompanyDB + "].[dbo].OPKL T3 on T2.AbsEntry = T3.AbsEntry " +
                                         "Where T3.AbsEntry = @QueryDocEntry AND T3.U_TruckNo = @TruckNum AND T1.U_DatabaseID = @DatabaseID;";

                    var OINVDocEntryList = sapconn.Query<string>(headerquery, new { QueryDocEntry = docEntry, TruckNum = truck, DatabaseID = _currentDB.WarehouseCompanyDB }).ToList();
                    if (OINVDocEntryList == null || OINVDocEntryList.Count <= 0)
                    {
                        return null;
                    }
                    #endregion

                    #region GetOINV
                    string oINVquery = "SELECT T0.*, T1.Name [ContactPerson], T1.Tel1 [ContactNo] FROM [" + _currentDB.CompanyDB + "].[dbo].[OINV] T0" +
                                       " LEFT JOIN [" + _currentDB.CompanyDB + "].[dbo].[OCPR] T1 ON T0.CntctCode = T1.CntctCode" +
                                       " WHERE T0.DocEntry = @DocEntry";
                    //OINV List 
                    foreach (var docnoOINV in OINVDocEntryList)
                    {
                        var line = sapconn.Query<OINV_Ex>(oINVquery, new { DocEntry = docnoOINV }).FirstOrDefault();
                        oINVs.Add(line);
                    }
                    oINVs.ForEach(x => x.PickIdNo = docEntry);
                    #endregion
                    return oINVs;
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }

        public List<PKL1_Ex> GetPKL1(string docentry)
        {
            try
            {
                var conn = new SqlConnection(_MWdbConnectionStr);
                string query = $"SELECT T0.*, " +
                               $"CASE WHEN T0.BaseObject = 17 THEN T1.ItemCode WHEN T0.BaseObject = 13 THEN T2.ItemCode END AS [ItemCode], " +
                               $"CASE WHEN T0.BaseObject = 17 THEN T1.Dscription WHEN T0.BaseObject = 13 THEN T2.Dscription END AS [ItemName] " +
                               $"FROM [" + _currentDB.CompanyDB + "]..PKL1 T0 " +
                               $"LEFT JOIN [" + _currentDB.CompanyDB + "]..RDR1 T1 ON T0.OrderEntry = T1.DocEntry AND T0.OrderLine = T1.LineNum " +
                               $"LEFT JOIN [" + _currentDB.CompanyDB + "]..INV1 T2 ON T0.OrderEntry = T2.DocEntry AND T0.OrderLine = T2.LineNum " +
                               $"Where AbsEntry = @PickNo";

                var result = conn.Query<PKL1_Ex>(query, new { PickNo = docentry }).ToList();

                return result;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }

        public List<PKL1_Ex> GetPKL1s(List<string> docentries)
        {
            try
            {
                var conn = new SqlConnection(_MWdbConnectionStr);
                string query = $"SELECT T0.*, " +
                               $"CASE WHEN T0.BaseObject = 17 THEN T1.ItemCode WHEN T0.BaseObject = 13 THEN T2.ItemCode END AS [ItemCode], " +
                               $"CASE WHEN T0.BaseObject = 17 THEN T1.Dscription WHEN T0.BaseObject = 13 THEN T2.Dscription END AS [ItemName] " +
                               $"FROM [" + _currentDB.CompanyDB + "]..PKL1 T0 " +
                               $"LEFT JOIN [" + _currentDB.CompanyDB + "]..RDR1 T1 ON T0.OrderEntry = T1.DocEntry AND T0.OrderLine = T1.LineNum " +
                               $"LEFT JOIN [" + _currentDB.CompanyDB + "]..INV1 T2 ON T0.OrderEntry = T2.DocEntry AND T0.OrderLine = T2.LineNum " +
                               $"Where AbsEntry = @PickNo";
                var PKL1s = new List<PKL1_Ex>();
                foreach(var docentry in docentries)
                {
                    var result = conn.Query<PKL1_Ex>(query, new { PickNo = docentry }).ToList();
                    PKL1s.AddRange(result);
                }

                return PKL1s;
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }


        public bool CheckPickListDuplicate(string docentry)
        {
            try
            {
                var conn = new SqlConnection(_MWdbConnectionStr);
                string query = " SELECT COUNT(PickNo) FROM DispatchDetail WHERE PickNo = @PickNo AND CompanyID = @CompanyID";

                var result = conn.Query<int>(query, new { PickNo = docentry, CompanyID = _currentDB.CompanyID }).FirstOrDefault();
                if (result > 0)
                    return false;
                return true;

            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return false;
            }
        }
    }
}
