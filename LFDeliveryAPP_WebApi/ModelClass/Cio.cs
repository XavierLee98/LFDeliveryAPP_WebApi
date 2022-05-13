using DbClass;
using LFDeliveryAPP_WebApi.Class.DTOs;
using LFDeliveryAPP_WebApi.Class.SAP;
using LFDeliveryAPP_WebApi.Class.User;
using LFDeliveryAPP_WebApi.Model.Dashboard;
using LFDeliveryAPP_WebApi.Model.Dispatch;
using LFDeliveryAPP_WebApi.Model.Other;
using LFDeliveryAPP_WebApi.Model.Payment;
using LFDeliveryAPP_WebApi.Model.SQL_Ex;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Class
{
    public class Cio
    {
        public DBCommon currentDB { get; set; }
        public string request { get; set; }
        public string LastErrorMessage { get; set; }

        public SSO CurrentUser { get; set; }
        public List<SSO> userList { get; set; }

        public string username { get; set; }
        public string password { get; set; }
        public string NewPassword { get; set; }

        public SSO QuerySSO { get; set; }
        public string QueryUser { get; set; }
        public List<string> Users { get; set; }
        public List<UserGroup> UserGroups { get; set; }
        public string QueryGroup { get; set; }

        public List<DBCommon> dBCommonList { get; set; }
        public DTODispatch DTODispatch { get; set; }
        public DTOPayment DTOPayment { get; set; }
        public List<IncomingPaymentHeader> IncomingPaymentHeaders { get; set; }

        public List<DashboardM> DashboardResults { get; set; }
        public List<OINV_Ex> OINVs { get; set; }
        public List<INV1_Ex> INV1s { get; set; }
        public string attachmentListStr { get; set; }
        public List<ODSC> ODSCs { get; set; }
        public List<Guid> Guids { get; set; }
        public List<OCRD> BPResultList { get; set; }
        public int LoadedCount { get; set; }
        public int InTransitCount { get; set; }
        public int DeliveredCount { get; set; }
        public List<string> QueryDocEntries { get; set; }

        public string QueryDocEntry { get; set; }
        public string QueryGuid { get; set; }
        public string QueryDriver { get; set; }
        public DateTime QueryStartTime { get; set; }
        public DateTime QueryEndTime { get; set; }
        public string QueryCardCode { get; set; }
        public string QueryStatus { get; set; }

    }
}
