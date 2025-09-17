using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.Services;
using System.Data;
using System.Data.SqlClient;
using log4net;
using System.Xml;
//using MelissaData;

namespace eLearningPlayer
{
    /// <summary>
    /// Summary description for Service
    /// </summary>
    [WebService(Namespace="https://your-domain.com/eLearningPlayer/")] 
    //<WebServiceBinding(ConformsTo:=WsiProfiles.None)> _
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class Service : System.Web.Services.WebService
    {

        [WebMethod]
        public XmlDocument StoreSuspendData(string SuspendData, string RegId, string UserId, string Type = "C", string Session_ended = "N", string completed = "Y", string Debug = "N")
        {
            //' This function store courde or assessment progresss data

            //' The input parameters are as follows:
            //'   RegId         - FK to CX_SESS_REG.ROW_ID or S_CRSE_TSTRUN.ROW_ID
            //
            //'   UserId        - Base64 encoded, reversed S_CONTACT.X_REGISTRATION_NUM of the
            //'                       user.
            //' Session_ended     - "Y" or "N": indicating if the client user session expired
            //
            //' Type                - An indicator to determine whether we are returning a "C"ourse
            //'                        or "A"ssessment.  Default is "C"ourse
            //
            //'   Debug	        - "Y", "N" or "T"      

            ILog myeventlog;
            ILog mydebuglog;
            // Open log file if applicable
            string logfile = @"C:\Logs\StoreSuspendData.log";
            log4net.GlobalContext.Properties["GMLogFileName"] = logfile;
            log4net.Config.XmlConfigurator.Configure();
            myeventlog = log4net.LogManager.GetLogger("EventLog");
            mydebuglog = log4net.LogManager.GetLogger("GMDebugLog");
            string sqlStr="";
            string redirectURL = "";

            XmlDocument resultXml = new XmlDocument();

            string results="", errmsg="", DecodedUserId="";
            string logging = "Y";

            //  Check parameters
            Debug = Debug.Substring(0, 1).ToUpper();
            if (((RegId == "") && (Debug == "T")))
            {
                results = "Failure";
                errmsg = (errmsg + ("\r\n" + "No parameters. "));
                resultXml.LoadXml("<result>Failure</result><desc>" + "\r\n" + "No parameters.");
                return resultXml;
            }
            if ((Debug == "T"))
            {
                RegId = "1-9ONMD";
                UserId = "==QQPZzMwMjMxEzMPRlU";
                DecodedUserId = "RTO31123036OA";
            }
            else
            {
                RegId = HttpUtility.UrlEncode(RegId).Trim();
                if (((RegId.IndexOf("%") + 1) > 0))
                {
                    RegId = HttpUtility.UrlDecode(RegId).Trim();
                }
                if (((RegId.IndexOf("%") + 1) > 0))
                {
                    RegId = RegId.Trim();
                }
                RegId = EncodeParamSpaces(RegId);
                UserId = HttpUtility.UrlEncode(UserId).Trim();
                if (((UserId.IndexOf("%") + 1) > 0))
                {
                    UserId = HttpUtility.UrlDecode(UserId).Trim();
                }
                if (((UserId.IndexOf("%") + 1) > 0))
                {
                    UserId = UserId.Trim();
                }
                DecodedUserId = FromBase64(ReverseString(UserId));
            }

            //  Open log file if applicable
            if (((Debug == "Y") || ((logging == "Y") && (Debug != "T"))))
            {
                logfile = @"C:\Logs\StoreSuspendData.log";
                try
                {
                    log4net.GlobalContext.Properties["LogFileName"] = logfile;
                    log4net.Config.XmlConfigurator.Configure();
                }
                catch (Exception ex)
                {
                    errmsg = (errmsg + ("\r\n" + "Error Opening Log. "));
                    results = "Failure";
                }

                if ((Debug == "Y"))
                {
                    mydebuglog.Debug("----------------------------------");
                    mydebuglog.Debug(("Trace Log Started " + (DateTime.Now.ToString() + "\r\n")));
                    mydebuglog.Debug("Parameters-");
                    mydebuglog.Debug(("  RegId: " + RegId));
                    mydebuglog.Debug(("  UserId: " + UserId));
                    mydebuglog.Debug(("  Decoded UserId: " + DecodedUserId));
                }
            }
            //  Verify the user
            sqlStr = "select count(1) " 
                            + "from siebeldb.dbo.CX_SESS_REG r "
	                        + "    join siebeldb.dbo.S_CONTACT c ON c.ROW_ID = r.CONTACT_ID "
                            + "where r.ROW_ID = @Reg_ID and c.X_REGISTRATION_NUM = @DecodedUserId "          
                    ;
            int result=0 ;
            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["siebeldb"].ConnectionString;
                conn.Open();
                try
                {
                    using (SqlCommand command = new SqlCommand(sqlStr, conn))
                    {
                        command.CommandType = CommandType.Text;
                        mydebuglog.Error("Insert eLearning App (course package) into DB...");
                        command.Parameters.Add("@Reg_ID", SqlDbType.VarChar, 15).Value = RegId;
                        command.Parameters.Add("@DecodedUserId", SqlDbType.VarChar, 2).Value = DecodedUserId;
                        result = (int)command.ExecuteScalar();
                    }
                }
                catch (Exception e)
                {
                    errmsg = errmsg + "\n" + @"Error: " + e.Message;
                    mydebuglog.Debug(@"Error: " + e.Message);
                    myeventlog.Error(@"Error: " + e.Message);
                    //return false;
                }
            }
            if (result == 0) //userId does not match in record.
            {
                mydebuglog.Debug(@"Invalid user. UserId: " + DecodedUserId);
                myeventlog.Error(@"Invalid user. UserId: " + DecodedUserId);
                //ErrMsg = "Invalid user. UserId: " + DecodedUserId ;
                resultXml.LoadXml("<result>Failure</result><desc>" + "Invalid user. UserId: " + DecodedUserId + "</desc>");
                return resultXml;
            }

            //Data processes
            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["siebeldb"].ConnectionString;
                conn.Open();
                try
                {
                    using (SqlCommand command = new SqlCommand(sqlStr, conn))
                    {
                        //Re-set user session access flag on 
                        if (Session_ended == "Y")
                        {
                            if (Type == "C")
                            {
                                //  Course:
                                sqlStr = "INSERT INTO siebeldb.dbo.CX_TRAIN_OFFR_ACCESS(ROW_ID, CREATED, CREATED_BY, LAST_UPD, LAST_UPD_BY, " +
                                          " MODIFICATION_NUM, CONFLICT_ID, REG_ID, ENTER_FLG, EXIT_FLG) " +
                                          " SELECT @regid+'-'+LTRIM(CAST(COUNT(*)+1 AS VARCHAR)) ,GETDATE(), '0-1', GETDATE(), " +
                                          " '0-1', 0, 0, @regid,'N','Y' " +
                                          " FROM siebeldb.dbo.CX_TRAIN_OFFR_ACCESS " +
                                          " WHERE REG_ID = @regid";
                            }
                            else
                            {
                                //Exam:
                                sqlStr = "INSERT INTO siebeldb.dbo.S_CRSE_TSTRUN_ACCESS(ROW_ID, CREATED, CREATED_BY, LAST_UPD, LAST_UPD_BY, " +
                                           " MODIFICATION_NUM, CONFLICT_ID, CRSE_TSTRUN_ID, ENTER_FLG, EXIT_FLG) " +
                                           " SELECT @regid+'-'+LTRIM(CAST(COUNT(*)+1 AS VARCHAR)), GETDATE(), '0-1', GETDATE(), " +
                                           " '0-1', 0, 0, @regid,'N','Y' " +
                                           " FROM siebeldb.dbo.S_CRSE_TSTRUN_ACCESS " +
                                           " WHERE CRSE_TSTRUN_ID =  @regid ";
                            }
                            command.Parameters.Add("@regid", SqlDbType.VarChar, 15).Value = RegId;
                            command.CommandType = CommandType.Text;
                            command.CommandText = sqlStr;
                            command.ExecuteNonQuery();
                        }

                        //Get Elearning REG_ID from ElearningRegistration table
                        sqlStr = @"select @out_reg_id = SESS_REG_ID from ElearningRegistration where SESS_REG_ID = @regid and HCI_USER_ID = @u_id;";
                        //command.Parameters.Add("@regid", SqlDbType.VarChar, 15).Value = RegId;
                        command.Parameters.Add("@u_id", SqlDbType.VarChar, 15).Value = DecodedUserId;
                        SqlParameter out_reg_id = new SqlParameter("@out_reg_id", SqlDbType.Int);
                        out_reg_id.Direction = ParameterDirection.InputOutput;
                        command.Parameters.Add(out_reg_id);
                        command.CommandType = CommandType.Text;
                        command.CommandText = sqlStr;
                        command.ExecuteNonQuery();

                        //Insert or Update Progress Data into DB
                        sqlStr = @"if @out_reg_id is not null and EXISTS(select 1 from ElearningAppItem where ELN_REG_ID = @out_reg_id ) "
                                    + @"update ElearningAppItem set PROGRESS_DATA = @suspData, LAST_UPD=getdate() where ELN_REG_ID = @out_reg_id "
                                    + @"else if @out_reg_id is not null "
                                    + @"insert ElearningAppItem "
                                    + @"select @out_reg_id, @suspData, '1-0', getdate(), null, getdate()";
                        command.Parameters.Remove(command.Parameters["@regid"]);
                        command.Parameters.Remove(command.Parameters["@u_id"]);
                        command.Parameters.Add("@suspData", SqlDbType.VarChar, 8000).Value = SuspendData;
                        
                        command.CommandText = sqlStr;
                        mydebuglog.Debug("Updating Syuspend Data into eLaerning Table");
                        command.ExecuteNonQuery();

                        //Call AcceptRollUp if corese/exam completed
                        if (completed == "Y")
                        {
                            string AccpRU_result = CallAcceptRollUp(RegId, "", "", "", "", UserId, Type, Debug);
                            if (!AccpRU_result.Contains("Error"))
                            {
                                eLearningPlayer.com.certegrity.hciscormsvc.Service svc = new eLearningPlayer.com.certegrity.hciscormsvc.Service();
                                try
                                {
                                    //Get Redirect URL from web service
                                    redirectURL = svc.GetRedirect(RegId, Type, Debug);
                                    mydebuglog.Debug("Redirect URL: " + redirectURL);
                                }
                                catch (Exception ex)
                                {
                                    errmsg = errmsg + "\n" + @"GetRedirect Error: " + ex.Message ;
                                    mydebuglog.Debug("GetRedirect Error: " + ex.Message);
                                    myeventlog.Error("GetRedirect Error: " + ex.Message);
                                }
                            }
                            else
                            {
                                errmsg =  errmsg + "\n" + @"AcceptRollUp Error: " +  AccpRU_result ;
                                mydebuglog.Debug("AcceptRollUp Error: " + AccpRU_result);
                                myeventlog.Error("AcceptRollUp Error: " + AccpRU_result);
                            }
                       }
                    }
                }
                catch (Exception e)
                {
                    errmsg = errmsg + "\n" + @"Error: " + e.Message;
                    mydebuglog.Debug(@"Error: " + e.Message);
                    myeventlog.Error(@"Error: " + e.Message);
                    resultXml.LoadXml("<result>Failure</result><desc>" + "Failed to update or insert ProgressData! " + DecodedUserId + "</desc>");
                    return resultXml;
                }
            }
            string retxml = "<UserId>"+DecodedUserId+"</UserId><RegId>"+RegId+"</RegId>";
            if (errmsg.Trim().Length == 0)
            {
                if (redirectURL.Trim().Length > 0) {retxml = retxml + "<RedirectURL>" + redirectURL + "</RedirectURL>" ;}
                resultXml.LoadXml(retxml + "<result>Success</result><desc>" + "</desc>");

            }
            {
                resultXml.LoadXml(retxml + "<result>Failure</result><desc>" + errmsg + "</desc>");
            }

            return resultXml;
        }


        [WebMethod]
        public string GetSuspendData(string RegId, string UserId, string Debug)
        {
            //' This function store courde or assessment progresss data

            //' The input parameters are as follows:
            //'   RegId           - The CX_SESS_REG.ROW_ID of the attendee
            //'   UserId          - Base64 encoded, reversed S_CONTACT.X_REGISTRATION_NUM of the
            //'                       user.
            //'   Debug	        - "Y", "N" or "T"      

            ILog myeventlog;
            ILog mydebuglog;
            // Open log file if applicable
            string logfile = @"C:\Logs\GetSuspendData.log";
            log4net.GlobalContext.Properties["GMLogFileName"] = logfile;
            log4net.Config.XmlConfigurator.Configure();
            myeventlog = log4net.LogManager.GetLogger("EventLog");
            mydebuglog = log4net.LogManager.GetLogger("GMDebugLog");
            string sqlStr="";

            XmlDocument resultXml = new XmlDocument();
            String returnedSuspData = "";

            string results="", errmsg="", DecodedUserId="";
            string logging = "Y";

            //  Check parameters
            Debug = Debug.Substring(0, 1).ToUpper();
            if (((RegId == "") && (Debug == "T")))
            {
                results = "Failure";
                errmsg = (errmsg + ("\r\n" + "No parameters. "));
                resultXml.LoadXml("<result>Failure</result><desc>" + "\r\n" + "No parameters.");
                return resultXml.OuterXml;
            }
            if ((Debug == "T"))
            {
                RegId = "1-9ONMD";
                UserId = "==QQPZzMwMjMxEzMPRlU";
                DecodedUserId = "RTO31123036OA";
            }
            else
            {
                RegId = HttpUtility.UrlEncode(RegId).Trim();
                if (((RegId.IndexOf("%") + 1) > 0))
                {
                    RegId = HttpUtility.UrlDecode(RegId).Trim();
                }
                if (((RegId.IndexOf("%") + 1) > 0))
                {
                    RegId = RegId.Trim();
                }
                RegId = EncodeParamSpaces(RegId);
                UserId = HttpUtility.UrlEncode(UserId).Trim();
                if (((UserId.IndexOf("%") + 1) > 0))
                {
                    UserId = HttpUtility.UrlDecode(UserId).Trim();
                }
                if (((UserId.IndexOf("%") + 1) > 0))
                {
                    UserId = UserId.Trim();
                }
                DecodedUserId = FromBase64(ReverseString(UserId));
            }

            //  Open log file if applicable
            if (((Debug == "Y") || ((logging == "Y") && (Debug != "T"))))
            {
                logfile = @"C:\Logs\GetSuspendData.log";
                try
                {
                    log4net.GlobalContext.Properties["LogFileName"] = logfile;
                    log4net.Config.XmlConfigurator.Configure();
                }
                catch (Exception ex)
                {
                    errmsg = (errmsg + ("\r\n" + "Error Opening Log. "));
                    results = "Failure";
                }

                if ((Debug == "Y"))
                {
                    mydebuglog.Debug("----------------------------------");
                    mydebuglog.Debug(("Trace Log Started " + (DateTime.Now.ToString() + "\r\n")));
                    mydebuglog.Debug("Parameters-");
                    mydebuglog.Debug(("  RegId: " + RegId));
                    mydebuglog.Debug(("  UserId: " + UserId));
                    mydebuglog.Debug(("  Decoded UserId: " + DecodedUserId));
                }
            }
            //  Verify the user
            sqlStr = "select count(1) " 
                            + "from siebeldb.dbo.CX_SESS_REG r "
	                        + "    join siebeldb.dbo.S_CONTACT c ON c.ROW_ID = r.CONTACT_ID "
                            + "where r.ROW_ID = @Reg_ID and c.X_REGISTRATION_NUM = @DecodedUserId "          
                    ;
            int result=0 ;
            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["siebeldb"].ConnectionString;
                conn.Open();
                try
                {
                    using (SqlCommand command = new SqlCommand(sqlStr, conn))
                    {
                        command.CommandType = CommandType.Text;
                        mydebuglog.Debug("Verifying User_Id...");
                        command.Parameters.Add("@Reg_ID", SqlDbType.VarChar, 15).Value = RegId;
                        command.Parameters.Add("@DecodedUserId", SqlDbType.VarChar, 2).Value = DecodedUserId;
                        result = (int)command.ExecuteScalar();
                    }
                }
                catch (Exception e)
                {
                    mydebuglog.Debug(@"Error: " + e.Message);
                    myeventlog.Error(@"Error: " + e.Message);
                    //return false;
                }
            }
            if (result == 0) //userId does not match in record.
            {
                mydebuglog.Debug(@"Invalid user. UserId: " + DecodedUserId);
                myeventlog.Error(@"Invalid user. UserId: " + DecodedUserId);
                resultXml.LoadXml("<result>Failure</result><desc>" + "Invalid user. UserId: " + DecodedUserId + "</desc>");
                return resultXml.OuterXml;
            }

            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["siebeldb"].ConnectionString;
                conn.Open();
                try
                {
                    using (SqlCommand command = new SqlCommand(sqlStr, conn))
                    {
                        //Get ProgressData from table
                        sqlStr = @"select @suspData = i.PROGRESS_DATA
                                    from ElearningAppItem i
	                                    join ElearningRegistration r on r.ELN_REG_ID = i.ELN_REG_ID
                                    where r.SESS_REG_ID = @regid and r.HCI_USER_ID = @u_id;";
                        command.Parameters.Add("@regid", SqlDbType.VarChar, 15).Value = RegId;
                        command.Parameters.Add("@u_id", SqlDbType.VarChar, 15).Value = DecodedUserId;
                        SqlParameter out_susp_data = new SqlParameter("@suspData", SqlDbType.VarChar, 8000);
                        out_susp_data.Direction = ParameterDirection.InputOutput;
                        command.Parameters.Add(out_susp_data);
                        command.CommandType = CommandType.Text;
                        command.CommandText = sqlStr;
                        command.ExecuteNonQuery();
                        returnedSuspData = out_susp_data.Value.ToString();
                    }
                }
                catch (Exception e)
                {
                    mydebuglog.Debug(@"Error: " + e.Message);
                    myeventlog.Error(@"Error: " + e.Message);
                    resultXml.LoadXml("<result>Failure</result><desc>" + "Failed to update or insert ProgressData! " + DecodedUserId + "</desc>");
                    return resultXml.OuterXml;
                }
            }
            resultXml.LoadXml("<result>Success</result><desc>" + "" + DecodedUserId + "</desc>");
            //return resultXml;
            return returnedSuspData;
        }

        //[WebMethod]
        //public string BuildMDMatchKey(string ZIPCODE, string ADDR, string Debug = "N")
        //{
        //    //ZIPCODE = "22209";
        //    //ADDR = "1501 Wilson Blvd, Ste 500";

        //    string retv, results;
        //    string errmsg = "";
        //    // location of the MatchUp data files (default installation location specified)
        //    try
        //    {
        //        string dMUPath = System.Configuration.ConfigurationManager.AppSettings["MD_MU_DataPath"];
        //    string dMULicense = System.Configuration.ConfigurationManager.AppSettings["MD_MU_Key"];
        //        mdHybrid matchupHybObj = new mdHybrid();
        //    //********* Create Match Code using MelissaData MatchUp Object interface; Ren Hou; 9/15/2017  ************
        //    matchupHybObj.SetLicenseString(dMULicense);
        //    matchupHybObj.SetPathToMatchUpFiles(dMUPath);
        //    matchupHybObj.SetMatchcodeName("Address");
        //    matchupHybObj.InitializeDataFiles();
        //    //using "Address" as the match_code rule

        //    // Establish field mappings:
        //    matchupHybObj.ClearMappings();
        //    if (((matchupHybObj.AddMapping(mdHybrid.MatchcodeMapping.Zip5) == 0) || (matchupHybObj.AddMapping(mdHybrid.MatchcodeMapping.Address) == 0)))
        //    {
        //        errmsg = Environment.NewLine + "Incorrect AddMapping() parameter. ";
        //        results = "Failure";
        //    }

        //    // Load up the fields with the data from the incoming record:
        //    matchupHybObj.ClearFields();
        //    matchupHybObj.AddField(ZIPCODE);
        //    // Zip
        //    matchupHybObj.AddField(ADDR);
        //    // Address

        //    // build the mathckey for the incoming record
        //    matchupHybObj.BuildKey();
        //    retv = matchupHybObj.GetKey();
        //    // ***********************************************************************************

        //    return retv;
        //    }
        //    catch (ApplicationException ex)
        //    {
        //        return ex.Message;
        //    }

        //}
        //Helper functions:
        public string ReverseString(string InputString)
        {
            //  Reverses a string
            int lLen;
            int lCtr;
            string sChar;
            string sAns;
            sAns = "";
            lLen = InputString.Length;
            for (lCtr = lLen; (lCtr <= 1); lCtr = (lCtr + -1))
            {
                sChar = InputString.Substring((lCtr - 1), 1);
                sAns = (sAns + sChar);
            }

            return sAns;
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

        public string EncodeParamSpaces(string InVal)
        {
            //  If given a urlencoded parameter value, replace spaces with "+" signs
            string temp;
            int i;
            if (((InVal.IndexOf(" ") + 1)
                        > 0))
            {
                temp = "";
                for (i = 1; (i <= InVal.Length); i++)
                {
                    if ((InVal.Substring((i - 1), 1) == " "))
                    {
                        temp = (temp + "+");
                    }
                    else
                    {
                        temp = (temp + InVal.Substring((i - 1), 1));
                    }
                }
                return temp;
            }
            else
            {
                return InVal;
            }
        }

        private string CallAcceptRollUp(string RegId, string completion_desc, string satisfaction_desc, string totalTime, string rollup_score, string UID
                        , string CrseType, string DEBUG)
        {
            string result ;
            eLearningPlayer.com.certegrity.hciscormsvc.Service svc = new eLearningPlayer.com.certegrity.hciscormsvc.Service();
            try
            {
                result = svc.AcceptRollup(RegId, completion_desc, satisfaction_desc,
                                                           totalTime, rollup_score, UID, CrseType, DEBUG);
            }
            catch (Exception ex)
            {
                result = "AcceptRollUp Error: " + ex.Message;
            }
            return result;
        }






    }
}
