using Dapper;
using LFDeliveryAPP_WebApi.Class;
using LFDeliveryAPP_WebApi.Model.Dashboard;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.SQL_Object
{
    public class SQL_Dashboard : IDisposable
    {
        public string LastErrorMessage { get; set; } = string.Empty;
        SqlConnection conn;
        SqlTransaction trans;

        public void Dispose() => GC.Collect();
        string _MWdbConnectionStr;
        string _SAPdbConnectionStr;
        IConfiguration _configuration;
        public SQL_Dashboard(IConfiguration configuration, string MWdbConnectionStr, string SAPdbConnectionStr)
        {
            _SAPdbConnectionStr = SAPdbConnectionStr;
            _MWdbConnectionStr = MWdbConnectionStr;
            _configuration = configuration;
        }

        public List<DashboardM> GetDashboardResult(Cio bag)
        {
            try
            {
                using (var conn = new SqlConnection(_MWdbConnectionStr))
                {
                    string query = "EXEC DispatchDashboard @StartTime, @EndTime, @CardCode, @Status, @CompanyID";
                    var result = conn.Query<DashboardM>(query, new { StartTime = bag.QueryStartTime, EndTime = bag.QueryEndTime, CardCode = bag.QueryCardCode, Status = bag.QueryStatus, CompanyID = bag.currentDB.CompanyDB }).ToList();
                    return result;
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }

        public Cio GetDashboardResultCount(Cio bag)
        {
            try
            {
                using (var conn = new SqlConnection(_MWdbConnectionStr))
                {
                    var p = new DynamicParameters();
                    p.Add("@StartDate", bag.QueryStartTime);
                    p.Add("@EndDate", bag.QueryEndTime);
                    p.Add("@CardCode", bag.QueryCardCode);
                    p.Add("@CompanyID", bag.currentDB.CompanyDB);
                    p.Add("@LoadedCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    p.Add("@InTransitCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    p.Add("@DeliveredCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var result = conn.Execute("dbo.GetDispatchDashboardCount", p, commandType: CommandType.StoredProcedure);

                    bag.LoadedCount = p.Get<int>("@LoadedCount");
                    bag.InTransitCount = p.Get<int>("@InTransitCount");
                    bag.DeliveredCount = p.Get<int>("@DeliveredCount");

                    return bag;
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }

    }
}

#region Old Method
//public List<DashboardM> GetLoadResult(Cio bag)
//{
//    try
//    {
//        using (var conn = new SqlConnection(_MWdbConnectionStr))
//        {
//            string query = "EXEC DeliveryLoadResult @StartTime, @EndTime, @CardCode";
//            var result = conn.Query<DashboardM>(query, new { StartTime = bag.QueryStartTime, EndTime = bag.QueryEndTime, CardCode = bag.QueryCardCode }).ToList();
//            return result;
//        }
//    }
//    catch (Exception excep)
//    {
//        LastErrorMessage = excep.ToString();
//        return null;
//    }
//}

//public List<DashboardM> GetInTransitResult(Cio bag)
//{
//    try
//    {
//        using (var conn = new SqlConnection(_MWdbConnectionStr))
//        {
//            string query = "EXEC DeliveryInTransitResult @StartTime, @EndTime, @CardCode";
//            var result = conn.Query<DashboardM>(query, new { StartTime = bag.QueryStartTime, EndTime = bag.QueryEndTime, CardCode = bag.QueryCardCode }).ToList();
//            return result;
//        }
//    }
//    catch (Exception excep)
//    {
//        LastErrorMessage = excep.ToString();
//        return null;
//    }
//}

//public List<DashboardM> GetDeliveredResult(Cio bag)
//{
//    try
//    {
//        using (var conn = new SqlConnection(_MWdbConnectionStr))
//        {
//            string query = "EXEC DeliveryDeliveredResult @StartTime, @EndTime, @CardCode";
//            var result = conn.Query<DashboardM>(query, new { StartTime = bag.QueryStartTime, EndTime = bag.QueryEndTime, CardCode = bag.QueryCardCode }).ToList();
//            return result;
//        }
//    }
//    catch (Exception excep)
//    {
//        LastErrorMessage = excep.ToString();
//        return null;
//    }
//}
#endregion