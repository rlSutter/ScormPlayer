using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using eLearningPlayer.com.certegrity.hciscormsvc;


namespace eLearningPlayer
{
    public partial class WebForm1 : System.Web.UI.Page
    {
        bool exitFuncCalled = false;
        protected void Page_Load(object sender, EventArgs e)
        {
            Label1.Text = "Start Testing ....";
            //Server.Transfer("HtmlPage1.html");
            LaunchDateTime.Value = DateTime.Now.ToString();  
            Server.Execute("HtmlPage1.html");
        }

        protected void Page_Unload(object sender, EventArgs e)
        {
            ExitDateTime.Value = DateTime.Now.ToString();
            Page.ClientScript.RegisterClientScriptBlock(Page.GetType(), "Page unload: " + ExitDateTime.Value, "alert('{0}');", true);
            //XmlDocument ret_xml;
            //ret_xml = WebForm1.StoreSuspendData2("", "", "", "", "", "", "");
            //if (exitFuncCalled)
            //{
            //    WebForm1.ExitPlayer("bookmark", "exit_mode", "success_status", "completion_status");
            //}
        }

        public void Session_OnEnd()
        {
            ExitDateTime.Value = DateTime.Now.ToString();
            Page.ClientScript.RegisterClientScriptBlock(Page.GetType(), "Seesion End: " + ExitDateTime.Value, "alert('{0}');", true);
            //XmlDocument ret_xml;
            //ret_xml = WebForm1.StoreSuspendData2("","","","","","","");                   
        }

        [System.Web.Services.WebMethod]
        public static string ExitPlayer(string SuspendData, string RegId, string UserId, string bookmark, string exit_mode, string success_status, string completion_status)
        {
            //Call StoreSuspendData if needed
            XmlDocument ret_xml;
            ret_xml = WebForm1.StoreSuspendData2(SuspendData, RegId, UserId, "", "", "", "");  
            ////Calling AcceptRollUp WS
            //eLearningPlayer.com.certegrity.hciscormsvc.Service siebelservice = new eLearningPlayer.com.certegrity.hciscormsvc.Service();
            //string result = siebelservice.AcceptRollup(RegId, completion_status, "unknown",
            //                                           "", "", UserId, "", "Y");
            return "http://www.yahoo.com";
        }

        [System.Web.Services.WebMethod]
        public static XmlDocument StoreSuspendData2(string SuspendData, string RegId, string UserId, string Type = "C", string Session_ended = "N", string completed = "Y", string Debug = "N")
        {
            //Calling WS
            Service susp_ws = new Service();
            XmlNode resultXml = new XmlDocument();
            resultXml = susp_ws.StoreSuspendData(SuspendData, RegId, UserId, Type, Session_ended, completed, Debug);
            return (XmlDocument)resultXml;
        }
        //GetSuspendData2
        [System.Web.Services.WebMethod]
        public static string GetSuspendData2(string RegId, string UserId, string Debug = "N")
        {
            //Calling WS
            //eLearningPlayer.elearning_ws.Service susp_ws = new eLearningPlayer.elearning_ws.Service();
            Service susp_ws = new Service();
            string resultStr;
            resultStr = susp_ws.GetSuspendData(RegId, UserId, Debug);
            return resultStr;
        }
    }
}