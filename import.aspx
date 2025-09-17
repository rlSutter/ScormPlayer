<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="import.aspx.cs" Inherits="eLearningPlayer.import" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <asp:Label ID="ImportTitle" runat="server" Font-Bold="true" Font-Size="14" Text="Label">Import course package for...</asp:Label>
    <br /><br />
    <asp:Label ID="lblCrseTitle" runat="server" Text=""></asp:Label>
    <div /><br />
    <asp:Panel ID="pnlImport" runat="server" Visible="false" Height="136px">
		<asp:Label ID="Label1" runat="server" Font-Size="12" ForeColor="Blue" Text="Label"> Select a zip file and click "Import" button: </asp:Label>
        <br />
        <br />
        <asp:FileUpload ID="zipFileUpload" runat="server" style="font-size:11px" Width="500" />
        <%--<asp:Button runat="server" id="UploadButton" text="Upload" onclick="UploadButton_Click" />--%>                    
        <div />
        <asp:Label ID="lblZipFileName" runat="server" Text=""></asp:Label>
        <br />
		<asp:Button ID="BtnImport" runat="server" Text="Import" OnClick="ImportZipPackage" />
    </asp:Panel>
    <asp:Panel ID="pnlImportResult" runat="server" Visible="false">
        <div id="ImportResult">
            <asp:Label ID="lblImportResult" runat="server" Text=""></asp:Label>
        </div>
         <div id="PacakageName">
            <asp:Label ID="lblPacakageName" runat="server" Text="Package Created at: " ForeColor="Blue"></asp:Label>
        </div>   </asp:Panel>
    </form>
</body>
</html>
