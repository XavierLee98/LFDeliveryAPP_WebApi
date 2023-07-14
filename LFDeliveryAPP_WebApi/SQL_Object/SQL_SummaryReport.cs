using Dapper;
using LFDeliveryAPP_WebApi.Model.SummaryReport;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.SQL_Object
{
    public class SQL_SummaryReport:IDisposable
    {
        public string LastErrorMessage { get; set; } = string.Empty;
        SqlConnection conn;
        SqlTransaction trans;

        public void Dispose() => GC.Collect();
        string _MWdbConnectionStr;
        string _SAPdbConnectionStr;
        IConfiguration _configuration;
        public SQL_SummaryReport(IConfiguration configuration, string MWdbConnectionStr)
        {
            _MWdbConnectionStr = MWdbConnectionStr;
            _configuration = configuration;
        }

        public SummaryReportModel GetReportPath(SummaryReportModel report)
        {
            try
            {
                using (var conn = new SqlConnection(_MWdbConnectionStr))
                {
                    string query = "SELECT * FROM [dbo].[SummaryRequest] WHERE CompanyId = @CompanyId AND Guid = @Guid;";

                    var result = conn.Query<SummaryReportModel>(query, new { CompanyId = report.CompanyID, Guid = report.Guid}).FirstOrDefault();

                    if(result.IsPost)
                    Console.WriteLine($"Path: {(string.IsNullOrEmpty(result.Path)?"null":result.Path)}");
                    return result;
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }

        public int InsertSummaryRequest(SummaryReportModel report)
        {
            try
            {
                using (var conn = new SqlConnection(_MWdbConnectionStr))
                {
                    string query = @"INSERT INTO [dbo].[SummaryRequest]
                                    ([CompanyID]
                                    ,[Guid]
                                    ,[DocType]
                                    ,[TruckNum]
                                    ,[DriverCode]
                                    ,[StartDate]
                                    ,[EndDate]
                                    ,[Status]
                                    ,[IsPost]
                                    ,[IsTry]
                                    ,[Path]
                                    ,[CreatedDate])
                                    VALUES 
                                     ( 
                                     @CompanyID,
                                     @Guid,
                                     @DocType,
                                     @TruckNum,
                                     @DriverCode,
                                     @StartDate,
                                     @EndDate,
                                     @Status,
                                     @IsPost,
                                     @IsTry,
                                     @Path,
                                     GETDATE());";
                    var result = conn.Execute(query, report);
                    return result;
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }

    }
}
