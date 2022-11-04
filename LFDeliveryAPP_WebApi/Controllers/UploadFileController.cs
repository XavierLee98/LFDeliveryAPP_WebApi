using Dapper;
using LFDeliveryAPP_WebApi.Class;
using LFDeliveryAPP_WebApi.Model.Other;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LFDeliveryAPP_WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UploadFileController : ControllerBase
    {
        readonly string _attachment = "AttachmentsFolder";  
        string _lastErrorMessage { get; set; } = string.Empty;
        string _attachmentPath = string.Empty;
        string _dbMWConnectionStr = string.Empty;
        string _dbSAPConnectionStr = string.Empty;
        string _dbMWName = "DatabaseDeliveryAppMw";
        string _dbSAPName = "DatabaseSAP";
        int _height = 0;
        int _weight = 0;
        FileLogger _fileLogger = new FileLogger();

        ILogger _logger;
        readonly IConfiguration _configuration;
        public UploadFileController(IConfiguration configuration, ILogger<UploadFileController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _dbMWConnectionStr = _configuration.GetConnectionString(_dbMWName);
            _dbSAPConnectionStr = _configuration.GetConnectionString(_dbSAPName);
            _attachmentPath = _configuration.GetSection(_attachment).Value;
            _height = Convert.ToInt32(_configuration.GetSection("PictureHeight").Value);
            _weight = Convert.ToInt32(_configuration.GetSection("PictureWidth").Value);

        }

        [HttpPost("/UploadFile")]
        public async Task<IActionResult> FileUpload(IFormFile file)
        {
            try
            {
                if (file == null || file.Length <= 0)
                {
                    return Content("file not selected");
                }

                HttpContext.Request.Headers.TryGetValue("User", out StringValues userValue);
                HttpContext.Request.Headers.TryGetValue("Guid", out StringValues guidValue);

                if (!Directory.Exists(_attachmentPath))
                {
                    Directory.CreateDirectory(_attachmentPath);
                }

                var path = Path.Combine(_attachmentPath, file.FileName);
                //using (var stream = new FileStream(path, FileMode.Create))
                //{
                //    await file.CopyToAsync(stream);     
                //}
                using var image = Image.Load(file.OpenReadStream());
                image.Mutate(x => x.Resize(_weight, _height));
                image.Save(path);

                FileInfo finfo = new FileInfo(path);
                if (finfo.Exists)
                {
                    return Ok(finfo.Name);
                }

                return BadRequest("Uploaded file not exits");


                //return BadRequest("File saved wirh error");
            }
            catch (Exception excep)
            {
                Log($"{excep}", null);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Logging error to log
        /// </summary>
        /// <param name="message"></param>
        /// <param name="obj"></param>
        void Log(string message, Cio bag)
        {
            _logger?.LogError(message, bag);
            _fileLogger.WriteLog(message);
        }
    }
}

//string insertSql = @"INSERT INTO [dbo].[FileUpload]
//               ([HeaderGuid]
//               ,[UploadDate]
//               ,[AppUser]
//               ,[ServerSavedPath])
//                VALUES (
//                @HeaderGuid,
//                GETDATE(),
//                @AppUser,
//                @ServerSavedPath
//                )";

//var insertResult = -1;

//    insertResult = new SqlConnection(_dbMWConnectionStr)
//                   .Execute(insertSql,
//                   new
//                   {
//                       HeaderGuid = guidValue,
//                       AppUser = userValue,
//                       ServerSavedPath = path
//                   });

//    if (insertResult == -1)
//    {
//        Log("File details insert db fail", null);
//        return Content("File details insert db fail");
//    }

//[HttpPost("/UploadFile")]
//public async Task<IActionResult> FileUpload(Cio bag)
//{
//    var upload = bag.uploadFile;
//    try
//    {
//        if (upload.files == null || upload.files.Count <= 0)
//        {
//            return Content("file not selected");
//        }


//        if (!Directory.Exists(_attachmentPath))
//        {
//            Directory.CreateDirectory(_attachmentPath);
//        }

//        foreach (var file in upload.files)
//        {
//            var path = Path.Combine(_attachmentPath, file.FileName);
//            using (var stream = new FileStream(path, FileMode.Create))
//            {
//                await file.CopyToAsync(stream);
//            }

//            string insertSql = @"INSERT INTO [dbo].[FileUpload]
//                                   ([HeaderGuid]
//                                   ,[UploadDate]
//                                   ,[AppUser]
//                                   ,[ServerSavedPath])
//                                    VALUES (
//                                    @HeaderGuid,
//                                    GETDATE(),
//                                    @AppUser,
//                                    @ServerSavedPath
//                                    )";

//            var insertResult = -1;
//            foreach (var guid in upload.Guids)
//            {
//                insertResult = new SqlConnection(_dbMWConnectionStr)
//                               .Execute(insertSql,
//                               new
//                               {
//                                   HeaderGuid = guid,
//                                   AppUser = upload.User,
//                                   ServerSavedPath = path
//                               });

//                if (insertResult == -1)
//                {
//                    Log("File details insert db fail", null);
//                    return Content("File details insert db fail");
//                }
//            }

//        }
//        return Ok();
//    }
//    catch (Exception excep)
//    {
//        Log($"{excep}", null);
//        return BadRequest($"{excep}");
//    }
//}
