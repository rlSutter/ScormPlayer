<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="HCIlaunch.aspx.cs" Inherits="eLearningPlayer.HCIlaunch" Async="true" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml" style="height:100%;width:100%;">
<head id="Head1" runat="server">
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title><%=crse_title %></title>
    
    <script src="https://ajax.googleapis.com/ajax/libs/jquery/1.4.4/jquery.min.js" type="text/javascript"></script>
    <script type="text/javascript">
        function CallPageMethod(methodName, jsonParamList, onSuccess, onFail) {
            //var paramList = '';
            //if (paramArray.length > 0) {
            //    for (var i = 0; i < paramArray.length; i += 2) {
            //        if (paramList.length > 0) paramList += ',';
            //        paramList += '"' + paramArray[i] + '":"' + paramArray[i + 1] + '"';
            //    }
            //}
            var loc = window.location.href;
            loc = loc.substr(0, loc.lastIndexOf("?"));
            loc = (loc.substr(loc.length - 1, 1) == "/") ? loc + "default.aspx" : loc;
            //jsonParamList = "{" + paramList + "}";
            //Fix IE \" issue
            //jsonParamList = jsonParamList.replace("\\", "");

            $.ajax({
                type: "POST",
                url: loc + "/" + methodName,
                data: jsonParamList,
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: onSuccess,
                error: onFail,
                //async: false
                async: true
            });
            //if (methodName == "ExitPlayer") {
            //    //Pause
            //    setTimeout(function() {
            //    console.log('Call ExitPlayer WS...');
            //     }, 500);
            //}
        }
    </script> 
    <script type="text/javascript">
        //Create global var for this window
        //window.pf = this.window;
    
        //OS detection
        // This script sets OSName variable as follows:
        // "Windows"    for all versions of Windows
        // "MacOS"      for all versions of Macintosh OS
        // "Linux"      for all versions of Linux
        // "UNIX"       for all other UNIX flavors 
        // "Unknown OS" indicates failure to detect the OS

        var OSName = "Unknown OS";
        if (navigator.appVersion.indexOf("Win") != -1) OSName = "Windows";
        if (navigator.appVersion.indexOf("Mac") != -1) OSName = "MacOS";
        if (navigator.appVersion.indexOf("X11") != -1) OSName = "UNIX";
        if (navigator.appVersion.indexOf("Linux") != -1) OSName = "Linux";

        //Browser Detection
        var nVer = navigator.appVersion;
        var nAgt = navigator.userAgent;
        var browserName = navigator.appName;
        var fullVersion = '' + parseFloat(navigator.appVersion);
        var majorVersion = parseInt(navigator.appVersion, 10);
        var nameOffset, verOffset, ix;

        // In Opera, the true version is after "Opera" or after "Version"
        if ((verOffset = nAgt.indexOf("Opera")) != -1) {
            browserName = "Opera";
            fullVersion = nAgt.substring(verOffset + 6);
            if ((verOffset = nAgt.indexOf("Version")) != -1)
                fullVersion = nAgt.substring(verOffset + 8);
        }
            // In MSIE, the true version is after "MSIE" in userAgent
        else if ((verOffset = nAgt.indexOf("MSIE")) != -1) {
            browserName = "Microsoft Internet Explorer";
            fullVersion = nAgt.substring(verOffset + 5);
        }
            // In Chrome, the true version is after "Chrome" 
        else if ((verOffset = nAgt.indexOf("Chrome")) != -1) {
            browserName = "Chrome";
            fullVersion = nAgt.substring(verOffset + 7);
        }
            // In Safari, the true version is after "Safari" or after "Version" 
        else if ((verOffset = nAgt.indexOf("Safari")) != -1) {
            browserName = "Safari";
            fullVersion = nAgt.substring(verOffset + 7);
            if ((verOffset = nAgt.indexOf("Version")) != -1)
                fullVersion = nAgt.substring(verOffset + 8);
        }
            // In Firefox, the true version is after "Firefox" 
        else if ((verOffset = nAgt.indexOf("Firefox")) != -1) {
            browserName = "Firefox";
            fullVersion = nAgt.substring(verOffset + 8);
        }
            // In most other browsers, "name/version" is at the end of userAgent 
        else if ((nameOffset = nAgt.lastIndexOf(' ') + 1) <
                  (verOffset = nAgt.lastIndexOf('/'))) {
            browserName = nAgt.substring(nameOffset, verOffset);
            fullVersion = nAgt.substring(verOffset + 1);
            if (browserName.toLowerCase() == browserName.toUpperCase()) {
                browserName = navigator.appName;
            }
        }
        // trim the fullVersion string at semicolon/space if present
        if ((ix = fullVersion.indexOf(";")) != -1)
            fullVersion = fullVersion.substring(0, ix);
        if ((ix = fullVersion.indexOf(" ")) != -1)
            fullVersion = fullVersion.substring(0, ix);

        majorVersion = parseInt('' + fullVersion, 10);
        if (isNaN(majorVersion)) {
            fullVersion = '' + parseFloat(navigator.appVersion);
            majorVersion = parseInt(navigator.appVersion, 10);
        }
    </script>

    <script type="text/javascript">
        window.loc = window.location.href;
        loc = loc.substr(0, loc.lastIndexOf("?"));
        loc = (loc.substr(loc.length - 1, 1) == "/") ? loc + "default.aspx" : loc;
        window.jsonObj;
        window.params;
        window.hciExitDone = 0;

        //window.addEventListener("beforeunload", function (e) {
        //    var dialogText = 'Dialog text here';
        //    e.returnValue = dialogText;
        //    return dialogText;
        //});

        //window.addEventListener("unload", exitPlayer, false);
        //function exitPlayer() {
        //    if (hciExitDone == 0){
        //        navigator.sendBeacon(loc + "/ExitPlayer", params);
        //    }
        //    //alert('Confirm to exit exam?');
        //}

        ////use LifeCycle API
        ////terminationEvent = 'onpagehide' in self ? 'pagehide' : 'unload';
        //terminationEvent = 'beforeunload' //'pagehide'
        //if (browserName == "Firefox") {
        //    //terminationEvent = 'pagehide'; //'visibilitychange';
        //    terminationEvent = 'onpagehide' in self ? 'pagehide' : 'unload';
        //}
        //else if (browserName == "Chrome") {
        //    terminationEvent = 'beforeunload';
        //}
        //else
        //{
        //    terminationEvent = 'beforeunload';
        //}

        //window.addEventListener(terminationEvent, function(event) {
        //    if (event.persisted) {
        //        // If the event's persisted property is `true` the page is about
        //        // to enter the page navigation cache, which is also in the frozen state.
        //        alert ("State will be Frozon." );
        //        //logStateChange('frozen');
        //    } 
        //    else {
        //        // If the event's persisted property is not `true` the page is
        //        // about to be unloaded.
        //        if (hciExitDone == 0){
        //            var fmCrse_main = document.getElementById("crse_main");
        //            if (fmCrse_main){
        //                fmCrse_main.contentWindow.exitOnUnload();
        //            }
        //            //navigator.sendBeacon(loc + "/ExitPlayer", params);
        //        }
        //        //logStateChange('terminated');
        //    }
        //}, {capture: true});

        ////Function that getting information from child iframe content
        //function exit() {
        //    var iframe = document.getElementById("crse_main");
        //    if (iframe){
        //        iframe.contentWindow;
        //    }
        //    //NOTE: We can't save to the LMS at this point because LMSFinish or LMSTerminate will have already been called

        //    //if the rendering screen is still visible, don't perform any suspend data actions to prevent the user's data being tainted/erased
        //    if (isRenderingScreenVisible())
        //        return;
        //    //jevonpackws
        //    //if using ws instead of SCORM (useWsInsteadOfSCORM), check if user exited with the Logout button, was forced exit, or exited using the "done lesson" button on final content page
        //    if (useWsInsteadOfSCORM)
        //    {
        //        if (!usedLogoutButton)
        //        {
        //            updateSuspendData();
        //            //updateSuspendData(true);//removed optional session_ended here bc it's now handled by hciExit() and hciNormalExit()

        //            //call the hciExit Fxn
        //            //ADDITION: !usedLogoutButton if clause now surrounds hciExit call below; If LOGOUT btn has aready been used, hciNormalExit will have already been called and hciExit should not (results in a double call)

        //            //hciExit(progress_data, location, exit_mode, success_status, enter_time, exit_time, user_id, reg_id, type)
        //            pf.hciExit(
        //                suspendData,
        //                String(currPageIndex),
        //                getWsExitMode(),
        //                getWsSuccessStatus(),
        //                lessonEntryDateTime,
        //                getCurrentDateTime(),
        //                userId,
        //                regId,
        //                sessionType,
        //                hciExitOnCallbackSuccess,
        //                hciExitOnCallbackFailure
        //            );
        //        }
        //    }
        //    else
        //    {
        //        //save the most recent suspend data before exiting IF exited lesson without LOGOUT/EXIT button (used 'X')
        //        if (!usedLogoutButton)
        //            updateSuspendData();
        //    }
        //}
        //window.onunload = function(e) {
        //    var dialogText = 'Dialog text here';
        //    e.returnValue = dialogText;
        //    return dialogText;
        //};


        //HCI variables
        //window.hciVars = {} ;
        window.hciRegId = "<%=regId %>";
        window.hciEncodedUserId = "<%=encoded_uid %>";
        window.hciCrseId = "<%=crse_id %>";
        window.hciCrseType = "<%=crse_type %>";
        window.hci_app_item_id = "<%=app_item_id %>";
        window.hci_attempt_id = "<%=cur_attempt_id %>";
        window.hciFstName = "<%=fst_name %>";
        window.hciLastName = "<%=last_name %>";
        window.pckgPath = "<%=pckg_path %>";
        window.hciRedirectURL = "<%=redirectUrl %>";
        window.hciKBAExitRedirectURL = document.referrer;
        window.hciRegStatusCode = "<%=reg_status_cd%>";

        if (hciKBAExitRedirectURL === undefined || hciKBAExitRedirectURL == null || hciKBAExitRedirectURL.trim() == "") {
            hciKBAExitRedirectURL = "https://your-domain.com/mobile/index.html#reg";
        }
        function RegStatusCheck() {
            //alert("Show referer: " + hciKBAExitRedirectURL + "; Status_CD = " + hciRegStatusCode);
            if (hciRegStatusCode == "On-Hold") {
                location.replace(hciKBAExitRedirectURL);
            }
        }
        //RegStatusCheck();
        //if (window.performance && window.performance.navigation.type == window.performance.navigation.TYPE_BACK_FORWARD) {
        //    alert('Got here using the browser "Back" or "Forward" button.');
        //}

        //HCI Fuctions
        function hciSaveSuspendData(progress_data, OnSuccessCallback, OnFailureCallback) {
            //Call PageMethod using jQuery
            //var jsonObj = {
            jsonObj = {
                "app_item_id": hci_app_item_id, "attempt_id": hci_attempt_id, "progress_data": progress_data, "reg_id": hciRegId, "type": hciCrseType
            };
            //var params = "\"app_item_id\":" + hci_app_item_id + ",\"attempt_id\":" + hci_attempt_id + ",\"progress_data\":\"" + SuspendData + "\"";
            //var params = JSON.stringify(jsonObj);
            params = JSON.stringify(jsonObj);
            CallPageMethod("StoreSuspendData2", params, OnSuccessCallback, OnFailureCallback);
        }

            function hciGetSuspendData(OnSuccessCallback, OnFailureCallback) {
                //PageMethods.GetSuspendData2(hci_app_item_id, OnSuccessCallback, OnFailureCallback);
                //Call PageMethod using jQuery
                //var jsonObj = {
                jsonObj = {
                "app_item_id": hci_app_item_id, "reg_id": hciRegId, "type": hciCrseType
            };
                //var params = "\"app_item_id\":" + hci_app_item_id;
                //var params = JSON.stringify(jsonObj);
                params = JSON.stringify(jsonObj);
                CallPageMethod("GetSuspendData2", params, OnSuccessCallback, OnFailureCallback);
            }

            function hciNormalExit(progress_data, location, completion_status, exit_mode, success_status, enter_time, exit_time, encoded_user_id, reg_id, type, score_scaled, OnSuccessExit, OnFailureCallback) {
                //Optional Parameter
                if (typeof score_scaled === 'undefined' || score_scaled == null) { score_scaled = -0.0000001; }
                if (typeof enter_time === 'undefined') { enter_time = " "; }

                if (enter_time == undefined || enter_time == "") enter_time = " ";
                //Call PageMethod using jQuery
                //var jsonObj = {
                jsonObj = {
                            "normal_exit": "Y"
                                , "app_item_id": hci_app_item_id
                                , "attempt_id": hci_attempt_id
                                , "progress_data": progress_data
                                , "location": location
                                , "completion_status": completion_status
                                , "exit_mode": String(exit_mode)
                                , "success_status": String(success_status)
                                , "enter_time": enter_time
                                , "exit_time": exit_time
                                , "encoded_user_id": encoded_user_id
                                , "reg_id": reg_id
                                , "type": type
                                , "score_scaled": score_scaled
                };
                //var params = JSON.stringify(jsonObj);
                params = JSON.stringify(jsonObj);
                CallPageMethod("ExitPlayer", params, OnSuccessExit, OnFailureCallback);
            hciExitDone = 1;
            }

            function hciExit(progress_data, location, exit_mode, success_status, enter_time, exit_time, encoded_user_id, reg_id, type, OnSuccessExit, OnFailureCallback) {
            //confirm("Confirm to exit COurse/Exam?");
                if (enter_time == undefined || enter_time == "") enter_time = " ";
                //Call PageMethod using jQuery
                //var jsonObj = {
                jsonObj = {
                    "normal_exit": "N"
                    , "app_item_id": hci_app_item_id
                    , "attempt_id": hci_attempt_id
                    , "progress_data": progress_data
                    , "location": location
                    , "completion_status": "N"
                    , "exit_mode": String(exit_mode)
                    , "success_status": String(success_status)
                    , "enter_time": enter_time
                    , "exit_time": exit_time
                    , "encoded_user_id": encoded_user_id
                    , "reg_id": reg_id
                    , "type": type
                    , "score_scaled": -0.0000001
                };
                //var params = JSON.stringify(jsonObj);
                params = JSON.stringify(jsonObj);
                CallPageMethod("ExitPlayer", params, OnSuccessExit, OnFailureCallback);
            hciExitDone = 1;
            ////Pause
            //setTimeout(function() {
            //    CallPageMethod("ExitPlayer", params, OnSuccessExit, OnFailureCallback);
            //    }, 500);
            }
            //}
            function OnSuccessExit(data) {
                //alert(data.d);
            }
            function OnSuccessCallback(data) {
                //alert(data.d);
            }
            function OnFailureCallback(jqxhr, status, exception) {
                //    console.log("AJAX error: " + status + ' : ' + exception);
            }
        //function sleep(ms) {
        //    return new Promise(resolve => setTimeout(resolve, ms));
        //}

    </script>
</head>
<body style="height:100%;width:100%;" onpageshow="" onload="">
    <form id="form_player" runat="server" style="height:100%;width:100%;">

<%--    <asp:scriptmanager enablepagemethods="true" id="Scriptmanager1" runat="server"> </asp:scriptmanager>--%>
    <%--<div style="height:100%;width:100%;text-align:center;margin-top:-80px;">--%> 
    <div style="height:100%;width:100%;text-align:center;margin-top:-60px;">
        <asp:Label ID="Label1" Visible="false" runat="server" ForeColor="blue" Text=""></asp:Label><br /><br />
<%--        <asp:Panel ID="crse_main" runat="server"><asp:Literal ID="crse_content" runat="server"></asp:Literal></asp:Panel>--%>
        <asp:HiddenField ID="h_RegId" runat="server" value="" Visible="false" />
        <asp:HiddenField ID="h_UserId" runat="server" value="" Visible="false" />
        <asp:HiddenField ID="h_CrseId" runat="server" value="" Visible="false" />
        <asp:HiddenField ID="h_CrseType" runat="server" value="" Visible="false" />
        <asp:HiddenField ID="h_encoded_uid" runat="server" value="" Visible="false" /> 
        <asp:HiddenField ID="h_fst_name" runat="server" value="" />
        <asp:HiddenField ID="h_last_name" runat="server" value="" />
        <asp:HiddenField ID="h_eln_reg_id" runat="server" value="" Visible="false" />
        <asp:HiddenField ID="h_app_item_id" runat="server" value="" />
        <asp:HiddenField ID="h_attempt_id" runat="server" value=""/>
        <p><asp:Button ID="Button1" Visible="false" runat="server" Text="Back" OnClientClick="JavaScript:window.history.back(1); return false;" /></p>
    <iframe id="crse_main" name="crse_main" style="border:none;height:100%;width:100%;text-align:center" src="<%=pUrl%>" > </iframe>   
    </div>
    </form>
</body>
</html>
