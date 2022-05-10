using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Mvc;
using System.Windows.Forms;
using System.Xml.Serialization;
using TemplateEditorApplication.Areas.Admin.Models;
using TemplateEditorApplication.CommanClasses;
using TemplateEditorApplication.EVOPDF;
using TemplateEditorApplication.Models;
using TemplateEditorApplication.Utility;

namespace TemplateEditorApplication.Controllers
{
    [HasClientOrUserGroupPermission(ClientGroupPermission.Video, ClientUserGroupPermission.Video)]
    public class VideoController : BaseController
    {
        private static string _snapshotPreviewPath { get; set; }
        private static string _snapshotThumbPath { get; set; }
        public const string Video_BrochureBasePath = "/VideoLibrary/Brochures/";
        public const string Video_SnapshotBasePath = "/VideoLibrary/SizedVidThumbs/";
        public const string Video_BasePath = "/VideoLibrary/VideoFiles/";
        public readonly string SitePath = ConfigurationManager.AppSettings["SiteURL"];

        public static UserInfoModel getUserInfoModal = new UserInfoModel();

        // GET: Default Index Controller method
        public ActionResult Index()
        {
            VideosModel objVideosModel = new VideosModel();
            objVideosModel.IsMyRecording = true;
            objVideosModel.IsPreRecording = false;
            ViewData["LibraryId"] = 0;

            if (HttpContext.Request.IsAjaxRequest())
            {
                ViewData["IsLoadLayout"] = false;
            }
            else
            {
                ViewData["IsLoadLayout"] = true;
            }

            return View(objVideosModel);
        }

        /// <summary>
        /// This method is used to invite team member and load in partial view
        /// </summary>
        /// <param name="LibraryId">Library Id</param>
        /// <param name="FolderId">Folder Id</param>
        /// <param name="VideoId">Video Id</param>
        /// <returns>Partial View</returns>
        public ActionResult InviteTeamMember(int LibraryId, int FolderId, int VideoId)
        {
            TempData["LibraryId"] = LibraryId;
            TempData["FolderId"] = FolderId;
            TempData["VideoId"] = VideoId;

            if (HttpContext.Request.IsAjaxRequest())
            {
                ViewData["IsLoadLayout"] = false;
            }
            else
            {
                ViewData["IsLoadLayout"] = true;
            }

            int iClientID = Convert.ToInt32(SessionUtility.ClientID);
            getUserInfoModal.ClientID = iClientID;
            DataSet dsClientUsers = VideosDAL.GetInvitableClientUsers(iClientID, Convert.ToInt32(SessionUtility.UserID), LibraryId, FolderId, VideoId);
            if (dsClientUsers != null && dsClientUsers.Tables != null && dsClientUsers.Tables.Count == 3)
            {

                getUserInfoModal.IsClientActive = dsClientUsers.Tables[1].Rows[0].Field<bool>("IsClientActive");
                getUserInfoModal.MaxUsers = dsClientUsers.Tables[1].Rows[0]["MaxUsers"] != null ? Convert.ToString(dsClientUsers.Tables[1].Rows[0]["MaxUsers"]) : string.Empty;
                if (dsClientUsers.Tables[0].Rows.Count > 0)
                {
                    // if (dsClientUsers.Tables[2].Rows.Count > 0)
                    // {
                    DataTable dt = dsClientUsers.Tables[2];
                    // }
                    var a = dt.AsEnumerable().Select(s => s.Field<int>("UserId"));

                    getUserInfoModal.ClientUsers = dsClientUsers.Tables[0].AsEnumerable().Select(dr => new ClientUserModel()
                    {
                        ClientID = iClientID,
                        UserType = dr.Field<string>("UserType"),
                        StaffID = dr.Field<int>("EmployeeID"),
                        UserID = dr.Field<int>("UserID"),
                        Name = dr.Field<string>("UserContact"),
                        Email = dr.Field<string>("UserEmail"),
                        IsUserActive = dr.Field<bool>("IsUserActive"),
                        CreationLevelId = dr["CreationLevelId"] != System.DBNull.Value ? Convert.ToString(dr.Field<int>("CreationLevelId")) : "",
                        CreationLevelName = dr["CreationLevelName"] != System.DBNull.Value ? dr.Field<string>("CreationLevelName") : "",
                        IsInvited = dt.AsEnumerable().Any(s => s.Field<int>("UserId") == dr.Field<int>("UserID")),
                        VideoFolderAssignmentId = dt.AsEnumerable().Where(s => s.Field<int>("UserId") == dr.Field<int>("UserID")).Select(e => e.Field<int>("VideoFolderAssignmentID")).FirstOrDefault()
                    }).ToList();
                }
            }
            if (!getUserInfoModal.IsClientActive)
            {
                ViewBag.ErrorMessage = Resources.App_Messages.msg_ClientProfileDisableOrDeleted;
            }
            //model.UserCreationLevels = GetCreationLevel();
            return PartialView("_InviteTeamMember", getUserInfoModal);
        }

        /// <summary>
        /// This method is used to Invite Users
        /// </summary>
        /// <returns>Partial View</returns>
        public ActionResult InviteUsers()
        {
            if (HttpContext.Request.IsAjaxRequest())
            {
                ViewData["IsLoadLayout"] = false;
            }
            else
            {
                ViewData["IsLoadLayout"] = true;
            }

            int iClientID = Convert.ToInt32(SessionUtility.ClientID);
            getUserInfoModal.ClientID = iClientID;
            DataSet dsClientUsers = VideosDAL.GetInvitableUsers(iClientID, Convert.ToInt32(SessionUtility.StaffID));
            if (dsClientUsers != null && dsClientUsers.Tables != null) // && dsClientUsers.Tables.Count == 3
            {
                getUserInfoModal.IsClientActive = dsClientUsers.Tables[1].Rows[0].Field<bool>("IsClientActive");
                getUserInfoModal.MaxUsers = dsClientUsers.Tables[1].Rows[0]["MaxUsers"] != null ? Convert.ToString(dsClientUsers.Tables[1].Rows[0]["MaxUsers"]) : string.Empty;
                if (dsClientUsers.Tables[0].Rows.Count > 0)
                {
                    getUserInfoModal.IsClientActive = dsClientUsers.Tables[1].Rows[0].Field<bool>("IsClientActive");
                    getUserInfoModal.MaxUsers = dsClientUsers.Tables[1].Rows[0]["MaxUsers"] != null ? Convert.ToString(dsClientUsers.Tables[1].Rows[0]["MaxUsers"]) : string.Empty;
                    if (dsClientUsers.Tables[0].Rows.Count > 0)
                    {
                        //DataTable dt = dsClientUsers.Tables[2];

                        getUserInfoModal.ClientUsers = dsClientUsers.Tables[0].AsEnumerable().Select(dr => new ClientUserModel()
                        {
                            ClientID = iClientID,
                            UserType = dr.Field<string>("UserType"),
                            StaffID = dr.Field<int>("EmployeeID"),
                            UserID = dr.Field<int>("UserID"),
                            Name = dr.Field<string>("UserContact"),
                            Email = dr.Field<string>("UserEmail"),
                            IsUserActive = dr.Field<bool>("IsUserActive"),
                            CreationLevelId = dr["CreationLevelId"] != System.DBNull.Value ? Convert.ToString(dr.Field<int>("CreationLevelId")) : "",
                            CreationLevelName = dr["CreationLevelName"] != System.DBNull.Value ? dr.Field<string>("CreationLevelName") : "",
                        }).ToList();
                    }
                }
            }
            if (!getUserInfoModal.IsClientActive)
            {
                ViewBag.ErrorMessage = Resources.App_Messages.msg_ClientProfileDisableOrDeleted;
            }
            //model.UserCreationLevels = GetCreationLevel();
            return PartialView("_InviteUsers", getUserInfoModal);
        }


        /// <summary>
        /// This api is used to save recording of video from view page along with other video attributes
        /// </summary>
        /// <param name="VideoURL">API.Video Video URL</param>
        /// <param name="ExternalVideoId">API.Video External Video Id</param>
        /// <param name="LibraryId">Library Id</param>
        /// <param name="FolderId">Folder Id</param>
        /// <returns>Status of record save</returns>
        [HttpPost]
        public ActionResult SaveRecordingVideo(string VideoURL, string ExternalVideoId, int LibraryId, int FolderId)
        {
            var IsNewCreated = true;
            int recordedVideoId = VideosDAL.SaveVideoRecording(Convert.ToInt32(SessionUtility.ClientID), Convert.ToInt32(SessionUtility.StaffID), Convert.ToInt32(SessionUtility.UserID), VideoURL, ExternalVideoId, LibraryId, FolderId);
            var result = new { recordedVideoId = recordedVideoId, videoUrl = VideoURL, IsNewCreated = IsNewCreated, ExternalVideoId = ExternalVideoId };
            string JsonResult = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            return Json(JsonResult);
        }
        /// <summary>
        /// This api is used to remove video from api.video and from the database
        /// </summary>
        /// <param name="ExternalVideoId"></param>
        /// <returns>Delete status</returns>
        [HttpPost]
        public ActionResult DeleteRecordingVideo(string ExternalVideoId)
        {
            var IsNewCreated = true;
            int IsDeleted = VideosDAL.DeleteRecordingVideo(ExternalVideoId);
            var result = new { IsDeleted = IsDeleted };
            string JsonResult = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            return Json(JsonResult);
        }

        /// <summary>
        /// This api is used to save additional data from landing page
        /// </summary>
        /// <param name="RecordedVideoId">Video Id</param>
        /// <param name="VideoTitle">Video Title</param>
        /// <param name="VideoContentHTML">Content of description</param>
        /// <returns>Status of save recording</returns>
        [HttpPost]
        public ActionResult SaveRecordingVideoCustomData(int RecordedVideoId, string VideoTitle, string VideoContentHTML)
        {
            var IsNewCreated = true;
            int recordedVideoCustomId = VideosDAL.SaveVideoRecordingCustomData(RecordedVideoId, VideoTitle.Trim(), VideoContentHTML.Trim());
            var result = new { recordedVideoCustomId = recordedVideoCustomId, IsNewCreated = IsNewCreated };
            string JsonResult = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            return Json(JsonResult);
        }


       
}