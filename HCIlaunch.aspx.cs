using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using log4net;
using System.Xml;
using System.IO;
using eLearningPlayer.com.certegrity.hciscormsvc;
using System.Configuration;

namespace eLearningPlayer
{
    public partial class HCIlaunch : System.Web.UI.Page
    {
        bool exitFuncCalled = false;
        public string regId = "", userId = "", crse_id = "", crse_type = "", fst_name = "", last_name = "", encoded_uid = "", pckg_path = "";
        public string reg_status_cd = "", crse_title = "", stored_uid = "", freferrer = "";
        public string pUrl;
        public int app_item_id, cur_attempt_id; 
        string sqlStr = "", versionId = "", ret = "", ret1="";
        int eln_reg_id = 0, active_attempt_id;
        string PackName = ""; string InitUrl = ""; string errMsg = "";
        string rev_userId = "", decoded_uid = "";
        bool reTry = true;
        int numTry = 0;
        int maxReTry = Convert.ToInt32(ConfigurationManager.AppSettings["Retry_Number"]);
        int reTryPause = Convert.ToInt32(ConfigurationManager.AppSettings["Retry_Pause"]);  //In Seconds
        string sess, uid;
        public string redirectUrl = "";
        string original = "";

        //void Page_Unload()
        //{
        //    //Pause
        //    System.Threading.Thread.Sleep(1000);
        //}

        protected void Page_Load(object sender, EventArgs e)
        {

            ILog myeventlog;
            ILog mydebuglog;
            // Open log file if applicable
            string logfile = @"C:\Logs\HCILaunch.log";
            log4net.GlobalContext.Properties["GMLogFileName"] = logfile;
            log4net.Config.XmlConfigurator.Configure();
            myeventlog = log4net.LogManager.GetLogger("EventLog");
            mydebuglog = log4net.LogManager.GetLogger("GMDebugLog");

            string StartTime = DateTime.Now.ToString();

            if (!IsPostBack)
            {
                Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.Cache.SetNoStore();
            }

            mydebuglog.Debug("----------------------------------");
            mydebuglog.Debug(("HCILaunch Trace Log Started " + (DateTime.Now.ToString() + "\r\n")));

            //Detect browser and version
            try
            {
                HttpBrowserCapabilities browser = Request.Browser;
                string b = browser.Browser;
                string v = browser.Version;
                mydebuglog.Debug("... Browser: " + b);
                mydebuglog.Debug("... Browser Version: " + v);
            }
            catch (Exception ex)
            {
                throw;
            }

            try
            {
                //get cookie values
                sess = Request.Cookies["Sess"] == null ? "" : Request.Cookies["Sess"].Value;
                uid = Request.Cookies["ID"] == null ? "" : Request.Cookies["ID"].Value;

                //if (sess == null || uid == null )
                //{
                //    errMsg = @"You are not logged in and do not have access to your course/exam";
                //    //throw new Exception(errMsg);
                //    OutPutError(errMsg, "", ref mydebuglog, ref myeventlog);
                //}

                //Get referer url
                Uri referrer = HttpContext.Current.Request.UrlReferrer;
                string userAgent = Request.UserAgent;
                if (referrer != null)
                {
                    original = referrer.OriginalString.ToLower();
                } else
                {
                    try
                    {
                        freferrer = Request.Headers["Referer"] == null ? "" : Request.Headers["Referer"];
                        original = freferrer.ToLower();
                    }
                    catch
                    {
                    }
                }
                mydebuglog.Debug("  -- Cookie Variables: Sess: " + sess + "; ID: " + uid);
                mydebuglog.Debug("  -- Referer: " + original);
                mydebuglog.Debug("  -- userAgent: " + userAgent);

                string veriStr = VerifyPlayerAccess(sess, uid, original, mydebuglog);
                mydebuglog.Debug("  -- veriStr: " + veriStr);
                if (veriStr != "" && veriStr != "You are not logged in and do not have access to your course or exam")
                {
                    OutPutError(veriStr, "", ref mydebuglog, ref myeventlog);
                    //OutPutError("Please login to portal", veriStr, ref mydebuglog, ref myeventlog);
                    return;
                }
                
                //Get parameters
                regId = Request.QueryString["RegId"];
                userId = Request.QueryString["UserId"];
                crse_id = Request.QueryString["CrseId"];
                crse_type = Request.QueryString["CrseType"];
                versionId = Request.QueryString["VersionId"];
                mydebuglog.Debug(@"Input Parameters: " + Environment.NewLine +
                                "   CrseId = " + crse_id + Environment.NewLine +
                                "   CrseType = " + crse_type + Environment.NewLine +
                                "   VersionId = " + versionId + Environment.NewLine +
                                "   RegId = " + regId + Environment.NewLine +
                                "   UserId = " + userId + Environment.NewLine);

                if (regId == null
                    || userId == null
                    || crse_id == null
                    || crse_type == null
                    || versionId == null
                    )
                {
                    //mydebuglog.Debug(@"Missing parameters: missing CrseId, CrseType, VersionId, RegId, or UserId");
                    errMsg = @"Missing parameters: missing CrseId, CrseType, VersionId, RegId, or UserId";
                    throw new Exception(errMsg);
                    //return;
                }

                //else if (regId != uid)
                //{
                //    errMsg = @"You are not logged in and do not have access to your course/exam";
                //    //throw new Exception(errMsg);
                //    OutPutError(errMsg, "", ref mydebuglog, ref myeventlog);
                //}

                //Update hidden variables values
                h_RegId.Value = regId;
                h_UserId.Value = userId;
                h_CrseId.Value = crse_id;

                //Convering user_id

                if (userId != null)
                {
                    userId = HttpUtility.UrlEncode(userId).Trim();
                    if (((userId.IndexOf("%") + 1)
                                > 0))
                    {
                        userId = HttpUtility.UrlDecode(userId).Trim();
                    }

                    if (((userId.IndexOf("%") + 1)
                                > 0))
                    {
                        userId = userId.Trim();
                    }
                    //Encode user id
                    byte[] array = System.Text.Encoding.ASCII.GetBytes(userId);
                    encoded_uid = ToBase64(array);
                    //arr = encoded_uid.ToCharArray();
                    //Array.Reverse(arr);  //reverse it
                    encoded_uid = new String(encoded_uid.ToCharArray().Reverse().ToArray());  //reverse it
                    h_encoded_uid.Value = encoded_uid;
                    ////decode
                    //char[] userId_arr = userId.ToCharArray();
                    //Array.Reverse(userId_arr);
                    //rev_userId = new String(userId_arr);
                    //decoded_uid = FromBase64(rev_userId);
                }
                myeventlog.Info("HCIPlayer; Action: Start;  RegId: " + regId + "; " + " UserId: " + userId + "; " + "Encoded_uid: " + encoded_uid + "; " + DateTime.Now.ToString() + "\r\n");
                mydebuglog.Debug(("Encoded UserId: " + encoded_uid + "\n"));
                //mydebuglog.Debug(("  Decoded UserId: " + decoded_uid));

                // Get Learner Information
                mydebuglog.Debug(@"Calling GetLearnerInformation WS...");
                XmlNode resultXml;
                reTry = true;
                numTry = 0;
                while (reTry && numTry <= maxReTry)
                {
                    if (numTry > 0) 
                    {
                        mydebuglog.Debug(@"Re-Try connecting the WS. (Retry Number: " + numTry.ToString() + ")");
                        System.Threading.Thread.Sleep(reTryPause * 1000);    //Pause; In Milliseconds
                    }
                    com.certegrity.hciscormsvc.Service ws = new com.certegrity.hciscormsvc.Service();
                    try
                    {
                        resultXml = ws.GetLearnerInformation(regId, crse_type, "Y");
                        //fst_name = resultXml.SelectSingleNode("UserId").InnerText;
                        //last_name = resultXml.SelectSingleNode("UserId").InnerText;
                        // When an anonymous assessment is done, the dummy contact record used has no name.  The only way to validate access is with the user id
                        // See http://twiki.hq.local/do/view/Elearning/AnonymousSurveys2018 for details
                        stored_uid = resultXml.SelectSingleNode("UserId").InnerText;
                        if (stored_uid.Trim().Length == 0)
                        {
                            mydebuglog.Debug(@"GetLearnerInformation WS result: Failed; Returned Value: " + resultXml.OuterXml + "\n");
                            mydebuglog.Debug(@"Learner information is empty or imcomplete. user id: " + stored_uid + "\n");
                            errMsg = @"Learner information is empty or imcomplete. user id: " + stored_uid + "\n";
                            throw new Exception(errMsg);
                        }
                        mydebuglog.Debug(@"GetLearnerInformation WS result: Success; Returned Value: " + resultXml.OuterXml + "\n");
                        reTry = false;
                        ws.Dispose();
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("The underlying connection was closed:"))
                        {
                            mydebuglog.Debug(@"GetLearnerInformation WS connection failed. (Error Msg: " + ex.Message + ")");
                            reTry = true;
                            numTry = numTry + 1;
                            ws.Dispose();
                        }
                        else
                        {
                            errMsg = @"Failed to get learner information. Error Message: " + ex.Message;
                            mydebuglog.Debug(errMsg);
                            myeventlog.Error(errMsg);
                            reTry = false;
                            ws.Dispose();
                            throw new Exception(errMsg);
                            //OutPutError(errMsg, ex.Message);
                            //return;
                        }
                    }
                }

                //* Get Get Redirect URL link
                mydebuglog.Debug(@"Calling GetRedirect WS...");
                reTry = true;
                numTry = 0;
                while (reTry && numTry <= maxReTry)
                {
                    if (numTry > 0)
                    {
                        mydebuglog.Debug(@"Re-Try connecting the WS. (Retry Number: " + numTry.ToString() + ")");
                        System.Threading.Thread.Sleep(reTryPause * 1000);    //Pause; In Milliseconds
                    }
                    com.certegrity.hciscormsvc.Service ws = new com.certegrity.hciscormsvc.Service();
                    try
                    {
                        redirectUrl = ws.GetRedirect(regId, crse_type, "N");
                        mydebuglog.Debug("GetRedirect Results: " + redirectUrl);
                        if (redirectUrl.Contains("Failure"))
                        {
                            mydebuglog.Debug(@"Failed to get GetRedirect URL.");
                            throw new Exception(redirectUrl);
                            //redirectUrl = "";
                        }
                        mydebuglog.Debug(@"GetRedirect WS result: Success; Returned Value: " + redirectUrl + "\n");
                        reTry = false;
                        ws.Dispose();
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("The underlying connection was closed:"))
                        {
                            mydebuglog.Debug(@"GetRedirect WS connection failed. (Error Msg: " + ex.Message + ")");
                            reTry = true;
                            numTry = numTry + 1;
                            ws.Dispose();
                        }
                        else
                        {
                            errMsg = @"Failed to process GetRedirect WS. Error Message: " + ex.Message;
                            mydebuglog.Debug(@"Failed to process GetRedirect WS.");
                            mydebuglog.Debug(errMsg);
                            myeventlog.Error(errMsg);
                            reTry = false;
                            ws.Dispose();
                            throw new Exception(errMsg);
                            //OutPutError(errMsg, ex.Message);
                            //return;
                        }
                    }
                }

                //* Create a [ElearningRegistration] record in elearning DB 
                using (SqlConnection conn = new SqlConnection())
                {
                    conn.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["siebeldb"].ConnectionString;
                    conn.Open();
                    SqlDataReader result;
                    try
                    {
                        mydebuglog.Debug(@"Executing sp_LaunchElearningApp in SQL Server...");
                        using (SqlCommand command = new SqlCommand(sqlStr, conn))
                        {
                            command.CommandTimeout = 60;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "elearning.dbo.sp_LaunchElearningApp";
                            command.Parameters.Add("@crseid", SqlDbType.VarChar, 15).Value = crse_id;
                            command.Parameters.Add("@crsetype", SqlDbType.VarChar, 2).Value = crse_type;
                            command.Parameters.Add("@regid", SqlDbType.VarChar, 15).Value = regId;
                            command.Parameters.Add("@userid", SqlDbType.VarChar, 15).Value = userId;
                            command.Parameters.Add("@versionid", SqlDbType.Int).Value = versionId;
                            command.Parameters.Add("@ret", SqlDbType.VarChar, 100).Value = ret;
                            command.Parameters["@ret"].Direction = ParameterDirection.Output;
                            //command.Parameters.Add("@eln_reg_id", SqlDbType.Int).Value = eln_reg_id;
                            //command.Parameters["@eln_reg_id"].Direction = ParameterDirection.Output;
                            reTry = true;
                            numTry = 0;
                            while (reTry && numTry <= maxReTry)
                            {
                                if (numTry > 0)
                                {
                                    mydebuglog.Debug(@"Re-Try sp_LaunchElearningApp. Number of retry: " + numTry.ToString() + ")");
                                    System.Threading.Thread.Sleep(reTryPause * 1000);    //Pause; In Milliseconds
                                    if (conn.State == ConnectionState.Open) { conn.Close(); conn.Open(); } else { conn.Open(); }
                                }
                                result = command.ExecuteReader();
                                //ret = command.Parameters["@ret"].Value.ToString();
                                if (result.Read())
                                {
                                    ret = result.GetString(0);
                                }
                                else
                                {
                                    errMsg = @"sp_LaunchElearningApp returns no records";
                                    //mydebuglog.Debug(errMsg);
                                    //myeventlog.Error(errMsg);
                                    //
                                    throw new Exception(errMsg);
                                }
                                if (!ret.Contains("error"))  //when no error
                                {
                                    eln_reg_id = result.IsDBNull(1) ? 0 : result.GetInt32(1);
                                    app_item_id = result.IsDBNull(2) ? 0 : result.GetInt32(2);
                                    active_attempt_id = result.IsDBNull(3) ? 0 : result.GetInt32(3);
                                    cur_attempt_id = result.IsDBNull(4) ? 0 : result.GetInt32(4);
                                    PackName = result.IsDBNull(5) ? "" : result.GetString(5);
                                    InitUrl = result.IsDBNull(6) ? "" : result.GetString(6);
                                    reg_status_cd = result.IsDBNull(7) ? "" : result.GetString(7);
                                    crse_title = result.IsDBNull(8) ? "" : result.GetString(8);

                                    //write hidden variables
                                    h_eln_reg_id.Value = eln_reg_id.ToString();
                                    h_app_item_id.Value = app_item_id.ToString();
                                    h_attempt_id.Value = cur_attempt_id.ToString();
                                    reTry = false;
                                }
                                else if (ret.Contains("completion error"))
                                {
                                    //ret1 = ret.Substring(0, ret.IndexOf("ID:")) + "ID: " + regId + ret.Substring(ret.IndexOf(")"));
                                    mydebuglog.Debug(@" Failure in sp_LaunchElearningApp:  " + ret.Replace("completion error: ", ""));
                                    myeventlog.Error(@" Failure in sp_LaunchElearningApp:  " + ret.Replace("completion error: ", ""));
                                    reTry = false;
                                    throw new Exception(@" Failure in sp_LaunchElearningApp:  " + ret.Replace("completion error: ", ""));
                                }
                                else if (ret.Contains("deadlocked"))  //Retry when deadlocked
                                {
                                    mydebuglog.Debug(@"Sp_LaunchElearningApp returns deadlock.");
                                    reTry = true;
                                    numTry = numTry + 1;
                                }
                                else
                                {
                                    //errMsg = @" Failure in sp_LaunchElearningApp:  " + ret;
                                    errMsg = ret;
                                    //mydebuglog.Debug(errMsg);
                                    //myeventlog.Error(errMsg);
                                    reTry = false;
                                    throw new Exception(errMsg);
                                }
                            } //End While
                        }
                        mydebuglog.Debug(@"sp_LaunchElearningApp in SQL Server result: Success; Instance ID (App_Item_ID): " + app_item_id.ToString() + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        //errMsg = @"Failed to get ElearningRegistration record. Err: " + ex.Message;
                        errMsg = ex.Message;
                        //mydebuglog.Debug(errMsg);
                        //myeventlog.Error(errMsg);
                        throw new Exception(errMsg);
                        //OutPutError(errMsg, ex.Message);
                        //return;
                    }
                }

                            //Return alert if there is unclosed (active) attempt
                            if (active_attempt_id != 0)
                            {
                                string ERR = "This course or assessment has already been launched in another window";
                                string msg = @"If you believe this to be in error, please contact Technical Support for assistance ";
                    mydebuglog.Debug(@"Launch error: " + ERR + "; app_attempt_id: " + active_attempt_id.ToString() + "; RegID: " + regId);
                                //ClientScript.RegisterStartupScript(this.GetType(), "alert", "alert('" + msg + "');", true);
                                //throw new Exception(msg);
                                mydebuglog.Debug(("HCILaunch Trace Log Ended " + (DateTime.Now.ToString())));
                                mydebuglog.Debug("----------------------------------");
                                myeventlog.Error(@"Error: Failed to launch! " + ERR + " RegID: " + regId);
                                OutPutError(ERR, msg, ref mydebuglog, ref myeventlog);
                                return;
                            }
                mydebuglog.Debug(@"reg_status_cd: " + reg_status_cd);
                //2020-08-13; Ren Hou; Added to handle failed KBA (On-Hold status) to redirect to error page
                if (reg_status_cd.Trim().ToLower() == "on-hold")
                    {
                    string ERR = (crse_type == "C") ? "You cannot take this class" : "You cannot take this exam"; ;
                    string msg = (crse_type == "C") ? @"Your class registration is On Hold and you cannot take it pending a review" : @"Your exam is On Hold and you cannot take it pending a review";
                    mydebuglog.Debug(@"Launch error: " + ERR + "; app_attempt_id: " + active_attempt_id.ToString() + "; RegID: " + regId + "; error: " + msg);
                    mydebuglog.Debug(("HCILaunch Trace Log Ended " + (DateTime.Now.ToString())));
                    mydebuglog.Debug("----------------------------------");
                    myeventlog.Error(@"Error: Failed to launch! " + msg + " RegID: " + regId);
                    OutPutError(ERR, msg, ref mydebuglog, ref myeventlog);
                    return;
                }

                if (PackName.Length > 0)
                {
                    pUrl = InitUrl;
                    pckg_path = pUrl.Substring(0, pUrl.LastIndexOf("/"));
                    mydebuglog.Debug(@"Launching package: " + PackName + " with URL: " + pUrl+ " ... ");
                    mydebuglog.Debug(@"Package " + PackName + @" launched successfully!" + Environment.NewLine);
                }
                else
                {
                    errMsg = @" Missing package URL:  ";
                    throw new Exception(errMsg);
                }
            }
            catch (Exception err)
            {
                //errMsg = "Course or Assessment failed to launch!";
                
                //if (ret1=="") {
                if (!ret.Contains("completion error")) {
                    mydebuglog.Debug(@"Failed to launch! " + err.Message + Environment.NewLine); 
                    myeventlog.Error(@"Failed to launch! " + err.Message); 
                }
                string msg = "Failed to launch course/assessment"; //ErroMsg:" + err.Message;
                OutPutError(msg, err.Message, ref mydebuglog, ref myeventlog);
                mydebuglog.Debug(("Sending error email via ErrorNotice service: " + err.Message));
                string retStr = sendErrorEmail(Server.MachineName, "HCIlaunch.aspx", "HCIPLYR_01", err.Message, "admin@yourdomain.com", "Y");
                mydebuglog.Debug(("ErrorNotice results: " + retStr + Environment.NewLine));
                mydebuglog.Debug(("HCILaunch Trace Log Ended " + (DateTime.Now.ToString())));
                mydebuglog.Debug("----------------------------------");
                return;
            }

            //Log to SVC_MON
            //mydebuglog.Debug("----------------------------------");
            mydebuglog.Debug(("HCILaunch calling LogPerformance WSTrace Log Started " + (DateTime.Now.ToString())));
            reTry = true;
            numTry = 0;
            while (reTry && numTry <= maxReTry)
            {
                if (numTry > 0)
                {
                    mydebuglog.Debug(@"Re-Try connecting the WS. (Retry Number: " + numTry.ToString() + ")");
                    System.Threading.Thread.Sleep(reTryPause * 1000);    //Pause; In Milliseconds
                }
                    com.certegrity.cloudsvc.basic.Service LogService = new com.certegrity.cloudsvc.basic.Service();
                    try
                    {
                        //com.certegrity.cloudsvc.basic.Service LogService = new com.certegrity.cloudsvc.basic.Service();
                        //LogService.LogPerformanceDataAsync(System.Environment.MachineName, "HCIPlayer", StartTime, "N");
                        string VersionNum  = "1";
                        LogService.LogPerformanceData2Async(System.Environment.MachineName, "HCIPlayer", StartTime, VersionNum, "N");
                        LogService.Dispose();/* TODO Change to default(_) if this is not a reference type */
                        mydebuglog.Debug(@"Success - calling LogPerformance WS." + Environment.NewLine);
                        reTry = false;
                        LogService.Dispose();
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("The underlying connection was closed:"))
                        {
                            mydebuglog.Debug(@"LogPerformanceData WS connection failed. (Error Msg: " + ex.Message + ")");
                            reTry = true;
                            numTry = numTry + 1;
                            LogService.Dispose();
                        }
                        else
                        {
                            mydebuglog.Debug(@"Failed to call LogPerformance WS. (Error Msg: " + ex.Message + ")");
                            myeventlog.Error("HCIPlayer; Action: HCIPlayer page load: RegID: " + regId + " UserId: " + userId + "; " + "Encoded_uid: " + encoded_uid + "; Result: " + "Failed: " + ex.Message);
                            mydebuglog.Debug("HCIPlayer; Action: HCIPlayer page load: RegID: " + regId + " UserId: " + userId + "; " + "Encoded_uid: " + encoded_uid + "; Result: " + "Failed: " + ex.Message + "\n");
                            mydebuglog.Debug(("Trace Log Ended " + (DateTime.Now.ToString())));
                            mydebuglog.Debug("----------------------------------");
                            reTry = false;
                            LogService.Dispose();
                        }
                    }
                }

            mydebuglog.Debug(("HCILaunch Trace Log Ended " + (DateTime.Now.ToString() )));
            mydebuglog.Debug("----------------------------------");
        }

        //***** Helper functions  *************************
        private void OutPutError(string errMsg, string errDetails, ref ILog dlog, ref ILog elog)
        {

            string domain = "", langcd = "";
            string last_inst = "";
            dlog.Debug(" > Cookie Variables: Sess: " + sess + "; ID: " + uid);
            String redUrl = "";

            if (sess != "" && uid != "")
            {
                //get last_inst from CX_SUB_CON
                using (SqlConnection conn = new SqlConnection())
                {
                    conn.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["siebeldb"].ConnectionString;
                    conn.Open();
                    try
                    {
                        using (SqlCommand command = new SqlCommand("", conn))
                        {
                            command.CommandType = CommandType.Text;
                            command.CommandText = "SELECT TOP 1 S.DOMAIN, C.X_PR_LANG_CD, SC.LAST_INST "
                                                + "FROM siebeldb.dbo.CX_SUB_CON_HIST H "
                                                + "LEFT OUTER JOIN siebeldb.dbo.CX_SUB_CON SC ON SC.ROW_ID=H.SUB_CON_ID "
                                                + "LEFT OUTER JOIN siebeldb.dbo.CX_SUBSCRIPTION S ON S.ROW_ID=SC.SUB_ID "
                                                + "LEFT OUTER JOIN siebeldb.dbo.S_CONTACT C ON C.ROW_ID=SC.CON_ID "
                                                + "WHERE USER_ID='" + uid + "' AND SESSION_ID='" + sess + "'";

                            dlog.Debug("  ... Get LAST_INST from CX_SUB_CON: " + command.CommandText);
                            DataTable dt = new DataTable();
                            using (var adapter = new SqlDataAdapter(command))
                            {
                                adapter.Fill(dt);
                            }
                            if (dt.Rows.Count > 0)
                            {
                                domain = dt.Rows[0].IsNull(0) ? "" : dt.Rows[0].Field<string>(0);
                                langcd = dt.Rows[0].IsNull(0) ? "" : dt.Rows[0].Field<string>(1);
                                last_inst = dt.Rows[0].IsNull(0) ? "" : dt.Rows[0].Field<string>(2);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        dlog.Debug("Failed to get info from CX_SUB_CON_HIST: " + ex.Message);
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }
            dlog.Debug("  > CX_SUB_CON columns. DOMAIN: " + domain + ";X_PR_LANG_CD: " + langcd + "; LAST_INST: " + last_inst);
            redUrl = redUrl + "https://your-domain.com/PlayerError.html?";
            redUrl = redUrl + "ERR=" + HttpUtility.UrlEncode(errMsg);
            redUrl = redUrl + "&EDTL=" + HttpUtility.UrlEncode(errDetails);
            redUrl = redUrl + "&UID=" + HttpUtility.UrlEncode(uid);
            redUrl = redUrl + "&SES=" + HttpUtility.UrlEncode(sess);
            redUrl = redUrl + "&LANG=" + HttpUtility.UrlEncode(langcd);
            redUrl = redUrl + "&PP=" + HttpUtility.UrlEncode(domain);
            redUrl = redUrl + "&RTN=" + HttpUtility.UrlEncode(last_inst);
            dlog.Debug("  ... Re-directing to error page: " + redUrl + Environment.NewLine);
            Response.Redirect(redUrl, false);
        }

        public string ToBase64(byte[] data)
        {
            //  Encode a Base64 string
            if ((data == null))
            {
                throw new ArgumentNullException("data");
            }

            return Convert.ToBase64String(data);
        }

        public string FromBase64(string base64)
        {
            //  Decode a Base64 string
            string results;
            if ((base64 == null))
            {
                throw new ArgumentNullException("base64");
            }
            results = System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(base64));
            return results;
        }

        private static string savePlayerData(int app_item_id, int attempt_id, string progress_data, string location, string completion_status, string exit_mode, string success_status, string enter_time, string exit_time, decimal score_scaled)
        {
            string ret="", sqlStr = "";
            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["siebeldb"].ConnectionString;
                conn.Open();
                try
                {
                    using (SqlCommand command = new SqlCommand(sqlStr, conn))
                    {
                        command.CommandTimeout = 60;
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "elearning.dbo.sp_UpdatetElearningAppItemAttempt";
                        //mydebuglog.Error("Insert eLearning App (course package) into DB...");
                        command.Parameters.Add("@app_item_id", SqlDbType.Int).Value = app_item_id;
                        command.Parameters.Add("@attempt_id", SqlDbType.Int).Value = attempt_id;
                        command.Parameters.Add("@progress_data", SqlDbType.NVarChar, -1).Value = progress_data;
                        command.Parameters.Add("@location", SqlDbType.VarChar, 30).Value = location;
                        command.Parameters.Add("@completion_status", SqlDbType.VarChar, 2).Value = completion_status;
                        //command.Parameters.Add("@entry", SqlDbType.VarChar, 2).Value = entry;
                        command.Parameters.Add("@exit_mode", SqlDbType.VarChar, 2).Value = exit_mode;
                        command.Parameters.Add("@success_status", SqlDbType.VarChar, 2).Value = success_status;
                        command.Parameters.Add("@enter_time", SqlDbType.VarChar, 30).Value = enter_time;
                        command.Parameters.Add("@exit_time", SqlDbType.VarChar, 30).Value = exit_time;
                        command.Parameters.Add("@score_scaled", SqlDbType.Decimal,10).Value = score_scaled;
                        command.Parameters["@score_scaled"].Precision = 10;
                        command.Parameters["@score_scaled"].Scale = 7;
                        command.Parameters.Add("@ret", SqlDbType.VarChar, 300).Value = ret;
                        command.Parameters["@ret"].Direction = ParameterDirection.Output;
                        //command.Parameters.Add("@eln_reg_id", SqlDbType.Int).Value = eln_reg_id;
                        //command.Parameters["@eln_reg_id"].Direction = ParameterDirection.Output;
                        int result = command.ExecuteNonQuery();
                        ret = command.Parameters["@ret"].Value.ToString();
                        if (command.Parameters["@ret"].Value.ToString().Contains("error"))
                        {
                            throw new Exception(@"Failed to save user interaction data from shell. SQL Error: " + ret);
                        }
                    }
                    return "success";
                }
                catch (Exception ex)
                {
                    throw new Exception("error: " + ex.Message);
                }
            }
            //return ret;
        }

        private String sendErrorEmail(String hostName, String serviceName, String errorCode, String errorText, string errorEmailRecipient, String debug)
        {
            // Call ErrorNotice
            String ErrorNoticeURL = "https://your-cloud-service.com/basic/service.asmx/ErrorNotice";

            System.Collections.Specialized.NameValueCollection parameters = new System.Collections.Specialized.NameValueCollection();
            parameters.Add("HOSTNAME", hostName);
            parameters.Add("SERVICENAME", serviceName);
            parameters.Add("ERRORCODE", errorCode);
            parameters.Add("ERRORTEXT", errorText);
            parameters.Add("RECIPIENT", errorEmailRecipient);
            parameters.Add("debug", debug);

            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                try
                {
                    byte[] responseArray = client.UploadValues(ErrorNoticeURL, parameters);
                    return System.Text.Encoding.ASCII.GetString(responseArray);
                }
                catch (System.Net.WebException e)
                {
                    return "Error; Msg:" + e.ToString();
                }
            }
        }
        private string VerifyPlayerAccess(string sess, string uid, string original, log4net.ILog mydebuglog)
        {
            string retStr = "", origSess="", origUid="", tmpStr="", LoggedIn="N";
            original = original.ToLower();
            sess = sess.ToLower();
            uid = uid.ToLower();

            mydebuglog.Debug(Environment.NewLine + "VerifyPlayerAccess");
            mydebuglog.Debug("... original: " + original);
            mydebuglog.Debug("... sess: " + sess);
            mydebuglog.Debug("... uid: " + uid);

            // Error out if no authentication cookies or referer found
            if (original == "" || sess == "" || uid == "")
            {
                retStr="You are not logged in and do not have access to your course or exam";
            }
            // Check to see if the cookies match what is in the referer string
            //else if (original.Contains("certegrity.com/") && (original.Contains("hcilaunch.aspx") || original.Contains("opensclass") || original.Contains("class.html") || original.Contains("assessment.html") || original.Contains("openclass.html")))
            else if (original.Contains("certegrity.com/"))
            {
                if (original.IndexOf("uid=") != -1 && original.IndexOf("ses=") != -1)
               {
                   tmpStr = original.Substring(original.IndexOf("uid=") + 4);
                   origUid = tmpStr.Substring(0, tmpStr.IndexOf("&"));
                   tmpStr = original.Substring(original.IndexOf("ses=") + 4);
                   origSess = tmpStr.Substring(0, tmpStr.IndexOf("&"));
                   //retStr = " : " + uid + " =" + origUid + " : " + origSess + " =" + sess;

                   if (sess != origSess || uid != origUid)
                   {
                       //retStr = @"Your session may have expired, please login to portal again. (" + sess + "=" + origSess + "|" + uid + "=" + origUid + ")";
                       retStr = @"Your session may have expired.  Please login to the portal to continue your course or exam.";
                   }
               }
            }
            // Launched an exam from the old portal
            else if (original.Contains("openinstrument") || original.Contains("(queryresults)") || original.Contains("(validatetrainer)"))
            {
                // The id and sess values are not part of the referrer
            }
            else {
                retStr = @"You need to start your course or exam from your portal.";
            }

            // Verify that the user is currently logged in - the cookies could be out-of-date for some reason
            if (retStr=="")
            {
                using (SqlConnection conn = new SqlConnection())
                {
                    conn.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["siebeldb"].ConnectionString;
                    conn.Open();
                    try
                    {
                        using (SqlCommand command = new SqlCommand("", conn))
                        {
                            command.CommandTimeout = 60;
                            command.CommandType = CommandType.Text;
                            command.CommandText = "SELECT TOP 1 (SELECT CASE WHEN H.LOGOUT_DT IS NULL THEN 'Y' ELSE 'N' END) AS LOGGED_IN FROM siebeldb.dbo.S_CONTACT C INNER JOIN siebeldb.dbo.CX_SUB_CON_HIST H ON H.USER_ID=C.X_REGISTRATION_NUM WHERE C.X_REGISTRATION_NUM=@uid AND H.SESSION_ID=@sess";
                            command.Parameters.Add("@uid", SqlDbType.VarChar, 15).Value = uid.ToUpper();
                            command.Parameters.Add("@sess", SqlDbType.VarChar, 15).Value = sess.ToUpper();
                            mydebuglog.Debug("\r\n Get logged in value: " + command.CommandText);
                            LoggedIn = Convert.ToString(command.ExecuteScalar());
                            LoggedIn = LoggedIn.Trim();
                            mydebuglog.Debug("... LoggedIn: " + LoggedIn + Environment.NewLine);
                            if (LoggedIn == "N" || LoggedIn == "" || LoggedIn == null)
                            {
                                retStr = @"Your credentials have expired.  Please login to the portal again to continue your course or exam.";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        mydebuglog.Debug("Failed to get LoggedIn: " + ex.Message);
                        retStr = @"Unable to locate your credentials.  Please login to the portal again to continue your course or exam.";
                    }
                }
            }

            return retStr;
        }
        //***** ************ *****

        //***** Web Methods  *****
        [System.Web.Services.WebMethod]
        public static string ExitPlayer(string normal_exit, int app_item_id, int attempt_id, string progress_data, string location, string completion_status, string exit_mode, string success_status, string enter_time
            , string exit_time, string encoded_user_id, string reg_id, string type, decimal? score_scaled)
        {
            ILog myeventlog;
            ILog mydebuglog;
            // Open log file if applicable
            //string logfile = @"C:\Logs\ELN_player.log";
            string logfile = @"C:\Logs\HCILaunch_ExitPlayer.log";
            log4net.GlobalContext.Properties["GMLogFileName"] = logfile;
            log4net.Config.XmlConfigurator.Configure();
            myeventlog = log4net.LogManager.GetLogger("EventLog");
            mydebuglog = log4net.LogManager.GetLogger("GMDebugLog");

            mydebuglog.Debug("----------------------------------");
            mydebuglog.Debug(("ExitPlayer Log Started " + (DateTime.Now.ToString() + "\r\n")));

            bool reTry = true;
            int numTry = 0;
            int maxReTry = Convert.ToInt32(ConfigurationManager.AppSettings["Retry_Number"]);
            int reTryPause = Convert.ToInt32(ConfigurationManager.AppSettings["Retry_Pause"]);  //In Seconds

            string StartTime = DateTime.Now.ToString();

            //*** Get UID for CrseType = 'A'
            if (type == "A")
            {
                string newUid = "";
                HCIlaunch HCILaunchPageRef = new HCIlaunch();
                using (SqlConnection conn = new SqlConnection())
                {
                    conn.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["siebeldb"].ConnectionString;
                    conn.Open();
                    try
                    {
                        using (SqlCommand command = new SqlCommand("", conn))
                        {
                            command.CommandTimeout = 60;
                            command.CommandType = CommandType.Text;
                            command.CommandText = "SELECT COALESCE(C.X_REGISTRATION_NUM,'') FROM siebeldb.dbo.S_CRSE_TSTRUN R JOIN siebeldb.dbo.S_CONTACT C ON C.ROW_ID=R.PERSON_ID WHERE R.ROW_ID=@conID";
                            command.Parameters.Add("@conID", SqlDbType.VarChar, 15).Value = reg_id;
                            mydebuglog.Debug("... Get X_REGISTRATION_NUM for Assessment CrseType: " + command.CommandText);
                            newUid = Convert.ToString(command.ExecuteScalar());
                            newUid = newUid.Trim();
                            if (newUid.Length > 0) 
                            {
                                mydebuglog.Debug("... New User_Id from X_REGISTRATION_NUM: " + newUid);
                                byte[] array = System.Text.Encoding.ASCII.GetBytes(newUid);
                                encoded_user_id = HCILaunchPageRef.ToBase64(array);
                                encoded_user_id = new String(encoded_user_id.ToCharArray().Reverse().ToArray());  //reverse it
                                mydebuglog.Debug("... New Enccoded and resvered User_Id from X_REGISTRATION_NUM: " + encoded_user_id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        mydebuglog.Debug("Failed to get X_REGISTRATION_NUM userId for Assessment: " + ex.Message);
                    }
                    finally
                    {
                        HCILaunchPageRef.Dispose();
                        conn.Close();
                    }
                }
            }

            //progress_data,location,completion_status,entry,exit_mode,success_status,enter_time,exit_time,user_id,crse_id,type
            ////Debugging
            mydebuglog.Debug("reg_id: " + reg_id);
            mydebuglog.Debug("type: " + type);
            mydebuglog.Debug("normal_exit: " + normal_exit);
            mydebuglog.Debug("app_item_id: " + app_item_id.ToString());
            mydebuglog.Debug("attempt_id: " + attempt_id.ToString());
            //mydebuglog.Debug("progress_data: " + progress_data);
            mydebuglog.Debug("location: " + location);
            mydebuglog.Debug("completion_status: " + completion_status);
            mydebuglog.Debug("exit_mode: " + exit_mode);
            mydebuglog.Debug("success_status: " + success_status);
            mydebuglog.Debug("enter_time: " + enter_time);
            mydebuglog.Debug("exit_time: " + exit_time);
            mydebuglog.Debug("encoded_user_id: " + encoded_user_id);
            mydebuglog.Debug("score_scaled: " + (score_scaled.HasValue ? "null" : score_scaled.ToString()));
            //mydebuglog.Debug("redirect URL: " + redirectUrl);
            
            //1. Save values to Elearning Player DB
            try 
            {
                string resl = savePlayerData(app_item_id, attempt_id, progress_data, location, completion_status, exit_mode, success_status, enter_time, exit_time, (decimal)score_scaled);
                mydebuglog.Debug("savePlayerData results: " + resl);
                if (resl.Contains("error"))
                {
                    mydebuglog.Debug(@"Failed on savePlayerData(). Error Msg: " + resl);
                    throw new Exception(resl);
                }
            }
            catch (Exception ex) 
            {
                mydebuglog.Debug(@"Failed to save shell parameters into database (Error Msg: " + ex.Message + ")");
                myeventlog.Error("HCIPlayer; Action: Exit Player; RegId: " + reg_id + "; " + "Encoded_uid: " + encoded_user_id + "; Result: " + "Failed to save shell parameters into database: " + ex.Message);
                mydebuglog.Debug(("Trace Log Ended " + (DateTime.Now.ToString() + "\r\n")));
                mydebuglog.Debug("----------------------------------");
                return "error: Failed to save user interaction data. Msg:" + ex.Message;
            }

            ////2. Call StoreSuspendData
            //string ret_str;
            //try
            //{
            //    //ret_str = HCIlaunch.StoreSuspendData2(progress_data, reg_id, user_id, type, "Y", completion_status, "N");
            //    ret_str = HCIlaunch.StoreSuspendData2(app_item_id, attempt_id, progress_data);
            //}
            //catch (Exception e)
            //{
            //    mydebuglog.Debug(@"Failed to save progress data into database (Error Msg: " + e.Message);
            //    myeventlog.Error(@"Failed to save shell parameters into database: " + e.Message);
            //    return "error: Failed to save progress data. Msg:" + e.Message;
            //}

            //3. Calling AcceptRollUp WS
            //if (normal_exit == "Y" || success_status.Trim() == "4") //Call AcceptRollUp only if it is a Normal Exit or if it is KBA forced exit;
            //{
                string ws_completion_status = "";
                if (completion_status == "Y") { ws_completion_status = "complete"; }
                else if (completion_status == "N") { ws_completion_status = "incomplete"; }
                //else if (completion_status == "E") { ws_completion_status = "take_exam"; }
                else if (completion_status == "E") { ws_completion_status = "complete"; }
                else { ws_completion_status = "unknown"; }
                reTry = true;
                numTry = 0;
                while (reTry && numTry <= maxReTry)
                {
                    if (numTry > 0)
                    {
                        mydebuglog.Debug(@"Re-Try connecting the WS. (Retry Number: " + numTry.ToString() + ")");
                        System.Threading.Thread.Sleep(reTryPause * 1000);    //Pause; In Milliseconds
                    }
                    eLearningPlayer.com.certegrity.hciscormsvc.Service siebelservice = new eLearningPlayer.com.certegrity.hciscormsvc.Service();
                    try
                    {
                        string result = siebelservice.AcceptRollup(reg_id, ws_completion_status, "unknown", "", "", encoded_user_id, type, "Y");
                        mydebuglog.Debug("AcceptRollUp Results: " + result);
                        if (result.Contains("Failure"))
                        {
                            //mydebuglog.Debug(@"Calling AcceptRollUp WS returns failure.");
                            throw new Exception(@"AcceptRollUp WS returns error.");
                        }
                        myeventlog.Info("HCIPlayer; Action: Calling AcceptRollUp; RegId: " + reg_id + "; Result: Success " + DateTime.Now.ToString() + "\r\n");
                        reTry = false;
                        siebelservice.Dispose();
                    }
                    catch (Exception e)
                    {
                        if (e.Message.Contains("The underlying connection was closed:") || e.Message.Contains("Thread was being aborted"))
                        {
                            mydebuglog.Debug(@"AcceptRollUp WS connection failed. (Error Msg: " + e.Message + ")");
                            reTry = true;
                            numTry = numTry + 1;
                            siebelservice.Dispose();
                        }
                        else
                        {
                            mydebuglog.Debug(@"Failed to process AcceptRollUp WS. (Error Msg: " + e.Message + ")");
                            myeventlog.Error("HCIPlayer; Action: Calling AcceptRollUp; RegId: " + reg_id + "; " + "Encoded_uid: " + encoded_user_id + "; Result: " + "Failed on calling AcceptRollup: " + e.Message);
                            mydebuglog.Debug(("Trace Log Ended " + (DateTime.Now.ToString() + "\r\n")));
                            mydebuglog.Debug("----------------------------------");
                            reTry = false;
                            siebelservice.Dispose();
                            return "error: Failed to process AcceptRollUp WS. Msg:" + e.Message;
                        }
                    }
                }
            //}
            ////4. Call GetRedirect if it is a normal exit
            //if (normal_exit == "Y")
            //{
            //    string redirectUrl = "";
            //    try
            //    {
            //        eLearningPlayer.com.certegrity.hciscormsvc.Service siebelservice = new eLearningPlayer.com.certegrity.hciscormsvc.Service();
            //        redirectUrl = siebelservice.GetRedirect(reg_id, type, "N");
            //        mydebuglog.Debug("GetRedirect Results: " + redirectUrl);
            //        if (redirectUrl.Contains("Failure"))
            //        {
            //            mydebuglog.Debug(@"Failed to Get Redirect URL.");
            //            throw new Exception(redirectUrl);
            //        }
            //        else
            //        {
            //            return "RedirectURL: " + redirectUrl;
            //        }
            //    }
            //    catch (Exception e)
            //    {
            //        mydebuglog.Debug(@"Failed to process GetRedirect WS.");
            //        return "error: Failed to process GetRedirect WS. Msg:" + e.Message;
            //    }
            //}

            //Log to SVC_MON
            reTry = true;
            numTry = 0;
            while (reTry && numTry <= maxReTry)
            {
                if (numTry > 0)
                {
                    mydebuglog.Debug(@"Re-Try connecting the WS. (Retry Number: " + numTry.ToString() + ")");
                    System.Threading.Thread.Sleep(reTryPause * 1000);    //Pause; In Milliseconds
                }
                com.certegrity.cloudsvc.basic.Service LogService = new com.certegrity.cloudsvc.basic.Service();
                try
                {
                    //LogService.LogPerformanceDataAsync(System.Environment.MachineName, "HCIPlayer-ExitPlayer", StartTime, "N");
                    string VersionNum = "1";
                    LogService.LogPerformanceData2Async(System.Environment.MachineName, "HCIPlayer-ExitPlayer", StartTime, VersionNum, "N");
                    reTry = false;
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("The underlying connection was closed:"))
                    {
                        mydebuglog.Debug(@"LogPerformance WS connection failed. (Error Msg: " + e.Message + ")");
                        reTry = true;
                        numTry = numTry + 1;
                    }
                    else
                    {
                        mydebuglog.Debug(@"Failed to call LogPerformance WS. (Error Msg: " + e.Message + ")");
                        //myeventlog.Error("HCIPlayer; Action: Calling AcceptRollUp; RegId: " + reg_id + "; Result: " + "Failed on calling AcceptRollup: " + e.Message);
                        mydebuglog.Debug(("Trace Log Ended " + (DateTime.Now.ToString() + "\r\n")));
                        mydebuglog.Debug("----------------------------------");
                        reTry = false;
                        return "error: Failed to call LogPerformance WS. Msg:" + e.Message;
                    }
                }
                finally
                {
                    LogService.Dispose();
                }
            }
            myeventlog.Info("HCIPlayer; Action: Exit Player; RegId: " + reg_id + "; Normal Exit: " + normal_exit + "; Completion Status: " + completion_status + "; Result: Success " + DateTime.Now.ToString() + "\r\n");
            mydebuglog.Debug(("Trace Log Ended " + (DateTime.Now.ToString() + "\r\n")));
            mydebuglog.Debug("----------------------------------");
            return "success";
        }

        [System.Web.Services.WebMethod]
        //public static XmlDocument StoreSuspendData2(string SuspendData, string RegId, string UserId, string Type = "C", string Session_ended = "N", string completed = "Y", string Debug = "N")
        public static string StoreSuspendData2(int app_item_id, int attempt_id, string progress_data, string reg_id, string type)
        {
            ILog myeventlog;
            ILog mydebuglog;
            // Open log file if applicable
            //string logfile = @"C:\Logs\ELN_player.log";
            string logfile = @"C:\Logs\HCILaunch_StoreSuspData.log";
            log4net.GlobalContext.Properties["SPLogFileName"] = logfile;
            log4net.Config.XmlConfigurator.Configure();
            myeventlog = log4net.LogManager.GetLogger("EventLog");
            mydebuglog = log4net.LogManager.GetLogger("SPDebugLog");

            //progress_data,location,completion_status,entry,exit_mode,success_status,enter_time,exit_time,user_id,crse_id,type
            ////Debugging
            mydebuglog.Debug("----------------------------------");
            mydebuglog.Debug(("StoreSuspendData2 Log Started " + (DateTime.Now.ToString() + "\r\n")));
            mydebuglog.Debug("reg_id: " + reg_id);
            mydebuglog.Debug("type: " + type);
            mydebuglog.Debug("app_item_id: " + app_item_id);
            mydebuglog.Debug("attempt_id: " + attempt_id.ToString());
            //mydebuglog.Debug("progress_data: " + progress_data);
 
            string StartTime = DateTime.Now.ToString();

            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["siebeldb"].ConnectionString;
                conn.Open();
                try
                {
                    string sqlStr = "", ret = "";
                    using (SqlCommand command = new SqlCommand(sqlStr, conn))
                    {
                        command.CommandTimeout = 60;
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "elearning.dbo.sp_SaveProgressData ";
                        //mydebuglog.Error("Insert eLearning App (course package) into DB...");
                        command.Parameters.Add("@app_item_id", SqlDbType.Int).Value = app_item_id;
                        command.Parameters.Add("@attempt_id", SqlDbType.Int).Value = attempt_id;
                        command.Parameters.Add("@progress_data", SqlDbType.NVarChar, -1).Value = progress_data;
                        command.Parameters.Add("@ret", SqlDbType.VarChar, 100).Value = ret;
                        command.Parameters["@ret"].Direction = ParameterDirection.Output;
                        //command.Parameters.Add("@eln_reg_id", SqlDbType.Int).Value = eln_reg_id;
                        //command.Parameters["@eln_reg_id"].Direction = ParameterDirection.Output;
                        int result = command.ExecuteNonQuery();
                        ret = command.Parameters["@ret"].Value.ToString();
                        mydebuglog.Debug("sp_SaveProgressData results: " + ret);
                        if (ret.Contains("error"))
                        {
                            throw new Exception(ret);
                        }
                    }
                    myeventlog.Info("HCIPlayer; Action: Store SuspendData; RegId: " + reg_id + "; Result: Success " + DateTime.Now.ToString() + "\n");

                    //Log to SVC_MON
                    bool reTry = true;
                    int numTry = 0;
                    int maxReTry = Convert.ToInt32(ConfigurationManager.AppSettings["Retry_Number"]);
                    int reTryPause = Convert.ToInt32(ConfigurationManager.AppSettings["Retry_Pause"]);  //In Seconds
                    while (reTry && numTry <= maxReTry)
                    {
                        if (numTry > 0)
                        {
                            mydebuglog.Debug(@"Re-Try connecting the WS. (Retry Number: " + numTry.ToString() + ")");
                            System.Threading.Thread.Sleep(reTryPause * 1000);    //Pause; In Milliseconds
                        }
                        com.certegrity.cloudsvc.basic.Service LogService = new com.certegrity.cloudsvc.basic.Service();
                        try
                        {
                            //LogService.LogPerformanceDataAsync(System.Environment.MachineName, "HCIPlayer-StoreSuspendData", StartTime, "N");
                            string VersionNum = "1";
                            LogService.LogPerformanceData2Async(System.Environment.MachineName, "HCIPlayer-StoreSuspendData", StartTime, VersionNum, "N");
                            reTry = false;
                        }
                        catch (Exception e)
                        {
                            if (e.Message.Contains("The underlying connection was closed:"))
                            {
                                mydebuglog.Debug(@"LogPerformance WS connection failed. (Error Msg: " + e.Message + ")");
                                reTry = true;
                                numTry = numTry + 1;
                            }
                            else
                            {
                                mydebuglog.Debug(@"Failed to call LogPerformance WS. (Error Msg: " + e.Message + ")");
                                //myeventlog.Error("HCIPlayer; Action: Calling AcceptRollUp; RegId: " + reg_id + "; Result: " + "Failed on calling AcceptRollup: " + e.Message);
                                reTry = false;
                                throw new Exception("error: Failed to call LogPerformance WS. Msg:" + e.Message); ;
                            }
                        }
                        finally
                        {
                            LogService.Dispose();/* TODO Change to default(_) if this is not a reference type */
                        }
                    }
                    // End - Log to SVC_MON

                    return "success";
                }
                catch (Exception ex)
                {
                    myeventlog.Info("HCIPlayer; Action: Store SuspendData; RegId: " + reg_id + "; Result: Failed" + ex.Message + " ;" + DateTime.Now.ToString() + "\n");
                    return @"error: Failed to save user porgress data from shell. Msg: " + ex.Message;
                }
                finally
                {
                    mydebuglog.Debug(("Trace Log Ended " + (DateTime.Now.ToString() + "\r\n")));
                    mydebuglog.Debug("----------------------------------");
                }
            }
            ////Calling WS
            //Service susp_ws = new Service();
            //XmlNode resultXml = new XmlDocument();
            //resultXml = susp_ws.StoreSuspendData(SuspendData, RegId, UserId, Type, Session_ended, completed, Debug);
            //return (XmlDocument)resultXml;
        }

        [System.Web.Services.WebMethod]
        //public static string GetSuspendData2(string RegId, string UserId, string Debug = "N")
        public static string GetSuspendData2(int app_item_id, string reg_id, string type)
        {
            ILog myeventlog;
            ILog mydebuglog;
            // Open log file if applicable
            //string logfile = @"C:\Logs\ELN_player.log";
            string logfile = @"C:\Logs\HCILaunch_GetSuspData.log";
            log4net.GlobalContext.Properties["GMLogFileName"] = logfile;
            log4net.Config.XmlConfigurator.Configure();
            myeventlog = log4net.LogManager.GetLogger("EventLog");
            mydebuglog = log4net.LogManager.GetLogger("GMDebugLog");
            //Debugging
            mydebuglog.Debug("----------------------------------");
            mydebuglog.Debug(("GetSuspendData2 Log Started " + (DateTime.Now.ToString() + "\r\n")));
            mydebuglog.Debug("reg_id: " + reg_id);
            mydebuglog.Debug("type: " + type);
            mydebuglog.Debug("app_item_id: " + app_item_id);

            string StartTime = DateTime.Now.ToString();

            //sp_GetProgressData
            string progress_data = "no record";
            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["siebeldb"].ConnectionString;
                conn.Open();
                try
                {
                    string sqlStr = "";
                    using (SqlCommand command = new SqlCommand(sqlStr, conn))
                    {
                        command.CommandTimeout = 60;
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "elearning.dbo.sp_GetProgressData ";
                        command.Parameters.Add("@app_item_id", SqlDbType.Int).Value = app_item_id;
                        command.Parameters.Add("@progress_data", SqlDbType.NVarChar, -1);
                        command.Parameters["@progress_data"].Direction = ParameterDirection.Output;
                        int result = command.ExecuteNonQuery();
                        //if (command.Parameters["@ret"].Value.ToString().Contains("error"))
                        //{
                        //    throw new Exception(@"Failed to retrieve user porgress data from shell.");
                        //}
                        progress_data = command.Parameters["@progress_data"].Value.ToString();
                        //mydebuglog.Debug("progress_data returned: " + progress_data);
                        if (progress_data.Contains("error:"))
                        {
                            throw new Exception(progress_data);
                        }
                    }

                    //Log to SVC_MON
                    bool reTry = true;
                    int numTry = 0;
                    int maxReTry = Convert.ToInt32(ConfigurationManager.AppSettings["Retry_Number"]);
                    int reTryPause = Convert.ToInt32(ConfigurationManager.AppSettings["Retry_Pause"]);  //In Seconds
                    while (reTry && numTry <= maxReTry)
                    {
                        if (numTry > 0)
                        {
                            mydebuglog.Debug(@"Re-Try connecting the WS. (Retry Number: " + numTry.ToString() + ")");
                            System.Threading.Thread.Sleep(reTryPause * 1000);    //Pause; In Milliseconds
                        }
                        com.certegrity.cloudsvc.basic.Service LogService = new com.certegrity.cloudsvc.basic.Service();
                        try
                        {
                            //LogService.LogPerformanceDataAsync(System.Environment.MachineName, "HCIPlayer-GetSuspendData", StartTime, "N");
                            string VersionNum = "1";
                            LogService.LogPerformanceData2Async(System.Environment.MachineName, "HCIPlayer-GetSuspendData", StartTime, VersionNum, "N");
                            //LogService.Dispose();/* TODO Change to default(_) if this is not a reference type */
                            reTry = false;
                        }
                        catch (Exception e)
                        {
                            if (e.Message.Contains("The underlying connection was closed:"))
                            {
                                mydebuglog.Debug(@"LogPerformance WS connection failed. (Error Msg: " + e.Message + ")");
                                reTry = true;
                                numTry = numTry + 1;
                            }
                            else
                            {
                                mydebuglog.Debug(@"Failed to call LogPerformance WS. (Error Msg: " + e.Message + ")");
                                //myeventlog.Error("HCIPlayer; Action: Calling AcceptRollUp; RegId: " + reg_id + "; Result: " + "Failed on calling AcceptRollup: " + e.Message);
                                reTry = false;
                                throw new Exception("error: Failed to call LogPerformance WS. Msg:" + e.Message); 
                            }
                        }
                        finally
                        {
                            LogService.Dispose();
                        }
                    }
                    //End - Log to SVC_MON

                    return progress_data;
                }
                catch (Exception ex)
                {
                    mydebuglog.Debug("failed to retrieve user progress data. Msg:" + ex.Message);
                    myeventlog.Info("HCIPlayer; Action: Get SuspendData; RegId: " + reg_id + "; Result: Failed" + ex.Message + " ;" + DateTime.Now.ToString() + "\n");
                    return @"error: failed to retrieve user porgress data. Msg:" + ex.Message;
                }
                finally
                {
                    mydebuglog.Debug(("Trace Log Ended " + (DateTime.Now.ToString() + "\r\n")));
                    mydebuglog.Debug("----------------------------------");
                    myeventlog.Info("HCIPlayer; Action: Get SuspendData; RegId: " + reg_id + "; Result: Success" + DateTime.Now.ToString() + "\n");
                }
            }
            ////Calling WS
            ////eLearningPlayer.elearning_ws.Service susp_ws = new eLearningPlayer.elearning_ws.Service();
            //Service susp_ws = new Service();
            //string resultStr;
            //resultStr = susp_ws.GetSuspendData(RegId, UserId, Debug);
            //return resultStr;
        }
        //***** ************ *****

    }
}
