using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO.Compression;
using System.Data;
using System.Data.SqlClient;
using log4net;

namespace eLearningPlayer
{
    public partial class import : System.Web.UI.Page
    {
        ILog myeventlog;
        ILog mydebuglog;
        string crse_id;
        string crse_type;
        string crse_title, queryStr;
        String path;
        string zipPath;
        string package_name;
        string extractPath, web_path;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Open log file if applicable
            string logfile = @"C:\Logs\HCIPlayer_ImportPackage.log";
            log4net.GlobalContext.Properties["GMLogFileName"] = logfile;
            log4net.Config.XmlConfigurator.Configure();
            myeventlog = log4net.LogManager.GetLogger("EventLog");
            mydebuglog = log4net.LogManager.GetLogger("GMDebugLog");
            crse_id = Request.QueryString["course"];
            crse_type = Request.QueryString["type"];

            mydebuglog.Debug("----------------------------------");
            mydebuglog.Debug(("Trace Log Started " + (DateTime.Now.ToString() + "\r\n")));
            mydebuglog.Debug(("PostBack is " + IsPostBack.ToString()));

            //ClientScript.RegisterClientScriptBlock(ClientScript.GetType(),"1", "alert('Server Side Page Load.');");

            if (!IsPostBack)
            {

                //Get Course Title from database
                if (GetCourseTitle())
                {
                    pnlImportResult.Visible = false;
                    pnlImport.Visible = true;
                    lblCrseTitle.Text = crse_title;
                    mydebuglog.Debug("Get Course Title from database: Succeed");

                }
                else
                {
                    mydebuglog.Debug("Get Course Title from database: Failed");
                }
            }
            else
            {
                mydebuglog.Debug("Post Back..." );
            }

            mydebuglog.Debug(("Trace Log Ended " + (DateTime.Now.ToString() + "\r\n")));
            mydebuglog.Debug("----------------------------------");

        }
        protected bool GetCourseTitle()
        {
            mydebuglog.Debug("Getting Course Title from database....");
            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["siebeldb"].ConnectionString;
                conn.Open();
                // Query course or assessment title 
                if (crse_type=="C")
                    queryStr = "select c.NAME " 
                               + "from siebeldb.dbo.S_CRSE c "
                               + "where c.ROW_ID = '" + crse_id + "'";
                else if (crse_type=="A")
                    queryStr = "select a.NAME "
                               + "from siebeldb.dbo.S_CRSE_TST a "
                               + "where a.ROW_ID = '" + crse_id + "'";
                else
                {
                    lblImportResult.Text = @"Invalid Course Type. Need to be 'C' or 'A'";
                    mydebuglog.Debug(@"Invalid Course Type. Need to be 'C' or 'A'");
                    pnlImportResult.Visible = true;
                    pnlImport.Visible = false;
                    lblPacakageName.Visible = false;
                    return false;
                }
                // Create the command
                try
                {
                    using (SqlCommand command = new SqlCommand(queryStr, conn))
                    {
                        command.CommandType = CommandType.Text;
                        mydebuglog.Debug("Query Course or Assessment Title... SQL: " + command.CommandText);
                        SqlDataReader result = command.ExecuteReader();
                        if (result.Read())
                            crse_title = result.GetString(0);
                        else
                        {
                            lblImportResult.Text = @"No record found for ID (" + crse_id + ") and Type (" + crse_type + ").";
                            mydebuglog.Debug(@"No record found for ID (" + crse_id + ") and Type (" + crse_type + ").");
                            pnlImportResult.Visible = true;
                            pnlImport.Visible = false;
                            return false;
                        }
                    }
                }
                catch (Exception e)
                {
                    lblImportResult.Text = @"Error: " + e.Message;
                    mydebuglog.Debug(@"Error: " + e.Message);
                    pnlImportResult.Visible = true;
                    pnlImport.Visible = false;                    
                    return false;
                }
            }
            return true;
        }
        protected void ImportZipPackage(object sender, EventArgs e)
        {
            mydebuglog.Debug(@"ImportZipPackage.... ");

            //Upload chosen file
            UploadFile();

            //Unzip file
            zipPath = path + "\\" + zipFileUpload.FileName;
            package_name =  zipFileUpload.FileName.Substring(0,zipFileUpload.FileName.Length - 4) + "-" + DateTime.Now.ToString("yyyyMMddHHmmssffff");
            //extractPath = System.Configuration.ConfigurationManager.AppSettings["ExtractedCoursePath"] + package_name;
            //extractPath = Server.MapPath("~/courses/") + package_name;
            extractPath = Server.MapPath("/courses/") + package_name;
            mydebuglog.Debug("Zip File Path: " + extractPath);
            mydebuglog.Debug("Unzip Extract Path: " + extractPath);
            web_path = @"/courses/" + package_name;
            //extractPath = @"C:\HCI\TEMP\" + package_name;
            System.IO.Directory.CreateDirectory(extractPath);
            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);
                lblPacakageName.Text = lblPacakageName.Text + extractPath;
                lblPacakageName.Visible = true;
                pnlImportResult.Visible = true;
                InsertIntoDB();
            }
            catch (Exception ex)
            {
                lblImportResult.Text = "Unzip failed! Error: " + ex.Message;
                mydebuglog.Debug("Unzip failed! Error: " + ex.Message);
                pnlImportResult.Visible = true;
                pnlImport.Visible = false;
            }
            try
            {
                //Clean uploaded zip file
                System.IO.File.Delete(zipPath);
            }
            catch (Exception ex)
            {
                lblImportResult.Text = "Failed to delete uploaded ZIP file! Error: " + ex.Message;
                pnlImportResult.Visible = true;
                pnlImport.Visible = false;
            }
        }

        protected void InsertIntoDB()
        {
            mydebuglog.Debug("Insert Package Records... ");
            string insertStr = "elearning.dbo.sp_InsertElearningApp ";
                               
            // Insert using SQLClient
            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["siebeldb"].ConnectionString;
                conn.Open();
                try
                {
                    using (SqlCommand command = new SqlCommand(insertStr, conn))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        //mydebuglog.Debug("Insert eLearning App (course package) into DB...");
                        command.Parameters.Add("@crse_id", SqlDbType.VarChar, 15).Value = crse_id;
                        command.Parameters.Add("@crse_type", SqlDbType.VarChar, 2).Value = crse_type;
                        command.Parameters.Add("@title", SqlDbType.VarChar, 300).Value = zipFileUpload.FileName.Substring(0, zipFileUpload.FileName.Length - 4);
                        command.Parameters.Add("@extract_path", SqlDbType.VarChar, 200).Value = extractPath;
                        command.Parameters.Add("@web_path", SqlDbType.VarChar, 200).Value = web_path;
                        mydebuglog.Debug("Insert SQL: " + command.CommandText);
                        int result = command.ExecuteNonQuery();
                        //lblImportResult.Text = @"No record found for ID (" + crse_id + ") and Type (" + crse_type + ").";
                        pnlImportResult.Visible = true;
                        pnlImport.Visible = false;
                        //return false;
                    }
                }
                catch (Exception e)
                {
                    lblImportResult.Text = @"Error: " + e.Message;
                    mydebuglog.Debug(@"Error: " + e.Message);
                    pnlImportResult.Visible = true;
                    pnlImport.Visible = false;
                    //return false;
                }
            }
        }

        protected void UploadFile()
        {

            mydebuglog.Debug("Uploading file... ");
            Boolean fileOK = false;
            path = Server.MapPath("~/Temp/");
            lblZipFileName.Visible = false;

            if (zipFileUpload.HasFile)
            {
                mydebuglog.Debug("Upload File: " + zipFileUpload.FileName + " into folder: " + path);
                String fileExtension =
                    System.IO.Path.GetExtension(zipFileUpload.FileName).ToLower();
                String[] allowedExtensions = { ".zip" };
                for (int i = 0; i < allowedExtensions.Length; i++)
                {
                    if (fileExtension == allowedExtensions[i])
                    {
                        fileOK = true;
                    }
                    else
                    {
                        lblZipFileName.Text = "Only '.zip' file is allowed!";
                        lblZipFileName.Visible = true;
                    }
                }
            }
            else
            {
                lblZipFileName.Text = "No file selected!";
                mydebuglog.Debug("No file selected!");
                lblZipFileName.Visible = true;
            }

            //Save selected file
            if (fileOK)
            {
                try
                {
                    zipFileUpload.PostedFile.SaveAs(path + zipFileUpload.FileName);
                    lblZipFileName.Text = lblZipFileName.Text + zipFileUpload.FileName;
                    mydebuglog.Debug("Upload File Succeed... file name: " + path + zipFileUpload.FileName);
                }
                catch (Exception ex)
                {
                    lblZipFileName.Text = lblZipFileName.Text + zipFileUpload.FileName + " Error: File could not be uploaded." + ex.Message;
                    mydebuglog.Debug(path + zipFileUpload.FileName + " Error: File could not be uploaded." + ex.Message);
                }
            }
            else
            {
                lblImportResult.Text = lblZipFileName.Text + zipFileUpload.FileName + "Error: Cannot accept files of this type.";
                mydebuglog.Debug(zipFileUpload.FileName + "Error: Cannot accept files of this type.");
            }
        }
    }
}