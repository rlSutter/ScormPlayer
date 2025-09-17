<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="WebForm1.aspx.cs" Inherits="eLearningPlayer.WebForm1" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>

    <script src="../jquery-1.2.6.min.js" type="text/javascript"></script>
    <script type="text/javascript">

        function CallPageMethod(methodName, onSuccess, onFail) {
            var args = '';
            var l = arguments.length;
            if (l > 3) {
                for (var i = 3; i < l - 1; i += 2) {
                    if (args.length != 0) args += ',';
                    args += '"' + arguments[i] + '":"' + arguments[i + 1] + '"';
                }
            }
            var loc = window.location.href;
            loc = (loc.substr(loc.length - 1, 1) == "/") ? loc + "default.aspx" : loc;
            $.ajax({
                type: "POST",
                url: loc + "/" + methodName,
                data: "{" + args + "}",
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: onSuccess,
                fail: onFail
            });
        }
    </script> 
    <script >

        function hciSaveSuspendData(SuspendData, RegId, UserId, Type, Session_ended, completed, Debug) {
            alert("hciSaveSuspendData");
            PageMethods.StoreSuspendData2(SuspendData, RegId, UserId, Type, Session_ended, completed, Debug, OnSuccessCallback, OnFailureCallback);
        }

        function hciGetSuspendData(RegId, UserId, Debug) {
            alert("hciGetSuspendData");
            PageMethods.GetSuspendData2(RegId, UserId, Debug, OnSuccessCallback, OnFailureCallback);
        }

        function hciNormalExit(label, exit_mode, success_status) {
            alert("ExitPlayer");
            PageMethods.ExitPlayer("hello", "", "", "", "", "", OnSuccessExit, OnFailureCallback);
        }

        function hciExit(label, exit_mode, success_status) {
            alert("ExitPlayer");
            PageMethods.ExitPlayer("hello", "", "", "", "", "", OnSuccessExit, OnFailureCallback);
        }

        function OnSuccessExit(res) {
            //alert(res);
            //if (res.indexOf("RedirectURL") != -1) {
                window.location.replace(res); //Re-direct to next URL
            //}
        }

        function OnSuccessCallback(res) {
            //alert(res);
            window.location.replace(res);
        }

        function OnFailureCallback(res) {
            alert("Fail");
            //window.location.replace("http://www.udn.com");
        }
    </script>

</head>
<body>
    <asp:label ID="Label1" runat="server" text="Label"></asp:label>
    <form id="form1" runat="server">
        <asp:HiddenField ID="LaunchDateTime" runat="server" value="" />
        <asp:HiddenField ID="ExitDateTime" runat="server" value="" />
    <asp:scriptmanager enablepagemethods="true" id="Scriptmanager1" runat="server"> </asp:scriptmanager>
    <div>
    </div>
    </form>
</body>
</html>
