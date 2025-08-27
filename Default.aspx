<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="Default.aspx.vb" Inherits="KC868A8_Controller._Default" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>KC868-A8 Smart Controller</title>
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css" rel="stylesheet" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/flatpickr/dist/flatpickr.min.css" />
    <link rel="stylesheet" href="Content/site.css" />
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/js/bootstrap.bundle.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/flatpickr"></script>
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePartialRendering="true">
        </asp:ScriptManager>

        <nav class="navbar navbar-expand-lg navbar-dark bg-primary">
            <div class="container-fluid">
                <a class="navbar-brand" href="#">KC868-A8 Controller</a>
                <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav">
                    <span class="navbar-toggler-icon"></span>
                </button>
                <div class="collapse navbar-collapse" id="navbarNav">
                    <ul class="navbar-nav">
                        <li class="nav-item">
                            <a class="nav-link active" href="#dashboard">Dashboard</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link" href="#schedules">Schedules</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link" href="#automation">Automation</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link" href="#settings">Settings</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link" href="#alexa">Alexa Integration</a>
                        </li>
                    </ul>
                </div>
                <div class="d-flex">
                    <asp:Label ID="lblConnectionStatus" runat="server" CssClass="connection-status" Text="Disconnected"></asp:Label>
                </div>
            </div>
        </nav>

        <div class="container mt-4">
            <!-- Dashboard Section -->
            <div id="dashboard" class="section active">
                <div class="row">
                    <div class="col-md-6">
                        <div class="card">
                            <div class="card-header">
                                <h5>Relay Control</h5>
                            </div>
                            <div class="card-body">
                                <asp:UpdatePanel ID="UpdatePanelRelays" runat="server">
                                    <ContentTemplate>
                                        <div class="row">
                                            <asp:Repeater ID="rptRelays" runat="server">
                                                <ItemTemplate>
                                                    <div class="col-md-6 mb-3">
                                                        <div class="form-check form-switch">
                                                            <input class="form-check-input relay-switch" type="checkbox" 
                                                                id='relay<%# Eval("Id").ToString() %>' 
                                                                <%# If(Convert.ToBoolean(Eval("State")), "checked", "") %> 
                                                                data-relay-id='<%# Eval("Id").ToString() %>' />
                                                            <label class="form-check-label" for='relay<%# Eval("Id").ToString() %>'>
                                                                <%# Eval("Name") %>
                                                            </label>
                                                        </div>
                                                    </div>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </div>
                                        <div class="mt-3">
                                            <asp:Button ID="btnAllOn" runat="server" Text="All ON" CssClass="btn btn-success me-2" OnClick="btnAllOn_Click" />
                                            <asp:Button ID="btnAllOff" runat="server" Text="All OFF" CssClass="btn btn-danger" OnClick="btnAllOff_Click" />
                                        </div>
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="card">
                            <div class="card-header">
                                <h5>Input Status</h5>
                            </div>
                            <div class="card-body">
                                <asp:UpdatePanel ID="UpdatePanelInputs" runat="server">
                                    <ContentTemplate>
                                        <div class="row">
                                            <asp:Repeater ID="rptInputs" runat="server">
                                                <ItemTemplate>
                                                    <div class="col-md-6 mb-3">
                                                        <div class="input-status">
                                                            <span class="input-indicator <%# If(Convert.ToBoolean(Eval("State")), "active", "") %>"></span>
                                                            <span><%# Eval("Name") %></span>
                                                        </div>
                                                    </div>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </div>
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </div>
                        </div>

                        <div class="card mt-4">
                            <div class="card-header">
                                <h5>Analog Inputs</h5>
                            </div>
                            <div class="card-body">
                                <asp:UpdatePanel ID="UpdatePanelAnalog" runat="server">
                                    <ContentTemplate>
                                        <div class="row">
                                            <asp:Repeater ID="rptAnalogInputs" runat="server">
                                                <ItemTemplate>
                                                    <div class="col-md-6 mb-3">
                                                        <label><%# Eval("Name") %></label>
                                                        <div class="progress">
                                                            <div class="progress-bar" role="progressbar" 
                                                                 style='width: <%# (Convert.ToInt32(Eval("Value")) * 100 / 4095) %>%' 
                                                                 aria-valuenow='<%# Eval("Value") %>' aria-valuemin="0" 
                                                                 aria-valuemax="4095">
                                                                <%# Eval("Value") %>
                                                            </div>
                                                        </div>
                                                    </div>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </div>
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="row mt-4">
                    <div class="col-md-12">
                        <div class="card">
                            <div class="card-header">
                                <h5>System Status</h5>
                            </div>
                            <div class="card-body">
                                <asp:UpdatePanel ID="UpdatePanelStatus" runat="server">
                                    <ContentTemplate>
                                        <div class="row">
                                            <div class="col-md-4">
                                                <div class="status-item">
                                                    <span class="status-label">Current Time:</span>
                                                    <asp:Label ID="lblCurrentTime" runat="server" CssClass="status-value"></asp:Label>
                                                </div>
                                            </div>
                                            <div class="col-md-4">
                                                <div class="status-item">
                                                    <span class="status-label">Device IP:</span>
                                                    <asp:Label ID="lblDeviceIP" runat="server" CssClass="status-value"></asp:Label>
                                                </div>
                                            </div>
                                            <div class="col-md-4">
                                                <div class="status-item">
                                                    <span class="status-label">Connection:</span>
                                                    <asp:Label ID="lblConnectionType" runat="server" CssClass="status-value"></asp:Label>
                                                </div>
                                            </div>
                                        </div>
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Schedules Section -->
            <div id="schedules" class="section">
                <div class="card">
                    <div class="card-header d-flex justify-content-between align-items-center">
                        <h5>Scheduled Tasks</h5>
                        <button type="button" class="btn btn-primary" data-bs-toggle="modal" data-bs-target="#scheduleModal">
                            Add Schedule
                        </button>
                    </div>
                    <div class="card-body">
                        <asp:UpdatePanel ID="UpdatePanelSchedules" runat="server">
                            <ContentTemplate>
                                <asp:GridView ID="gvSchedules" runat="server" CssClass="table table-striped" AutoGenerateColumns="false"
                                    OnRowCommand="gvSchedules_RowCommand" DataKeyNames="Id">
                                    <Columns>
                                        <asp:BoundField DataField="Name" HeaderText="Name" />
                                        <asp:BoundField DataField="Time" HeaderText="Time" DataFormatString="{0:HH:mm}" />
                                        <asp:BoundField DataField="Days" HeaderText="Days" />
                                        <asp:BoundField DataField="Action" HeaderText="Action" />
                                        <asp:BoundField DataField="Target" HeaderText="Target" />
                                        <asp:TemplateField HeaderText="Enabled">
                                            <ItemTemplate>
                                                <div class="form-check form-switch">
                                                    <input class="form-check-input schedule-switch" type="checkbox" 
                                                        id='schedule<%# Eval("Id").ToString() %>' 
                                                        <%# If(Convert.ToBoolean(Eval("Enabled")), "checked", "") %> 
                                                        data-schedule-id='<%# Eval("Id").ToString() %>' />
                                                </div>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Actions">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="btnEdit" runat="server" CssClass="btn btn-sm btn-primary"
                                                    CommandName="EditSchedule" CommandArgument='<%# Eval("Id").ToString() %>'>
                                                    <i class="bi bi-pencil"></i> Edit
                                                </asp:LinkButton>
                                                <asp:LinkButton ID="btnDelete" runat="server" CssClass="btn btn-sm btn-danger"
                                                    CommandName="DeleteSchedule" CommandArgument='<%# Eval("Id").ToString() %>'
                                                    OnClientClick="return confirm('Are you sure you want to delete this schedule?');">
                                                    <i class="bi bi-trash"></i> Delete
                                                </asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                            </ContentTemplate>
                        </asp:UpdatePanel>
                    </div>
                </div>
            </div>

            <!-- Automation Section -->
            <div id="automation" class="section">
                <div class="card">
                    <div class="card-header d-flex justify-content-between align-items-center">
                        <h5>Automation Rules</h5>
                        <button type="button" class="btn btn-primary" data-bs-toggle="modal" data-bs-target="#automationModal">
                            Add Rule
                        </button>
                    </div>
                    <div class="card-body">
                        <asp:UpdatePanel ID="UpdatePanelAutomation" runat="server">
                            <ContentTemplate>
                                <asp:GridView ID="gvAutomation" runat="server" CssClass="table table-striped" AutoGenerateColumns="false"
                                    OnRowCommand="gvAutomation_RowCommand" DataKeyNames="Id">
                                    <Columns>
                                        <asp:BoundField DataField="Name" HeaderText="Name" />
                                        <asp:BoundField DataField="Condition" HeaderText="Condition" />
                                        <asp:BoundField DataField="Action" HeaderText="Action" />
                                        <asp:TemplateField HeaderText="Timer">
                                            <ItemTemplate>
                                                <%# If(Convert.ToBoolean(Eval("UseTimer")), Eval("TimerType").ToString() & " " & Eval("TimerDuration").ToString() & "ms", "None") %>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Enabled">
                                            <ItemTemplate>
                                                <div class="form-check form-switch">
                                                    <input class="form-check-input automation-switch" type="checkbox" 
                                                        id='automation<%# Eval("Id").ToString() %>' 
                                                        <%# If(Convert.ToBoolean(Eval("Enabled")), "checked", "") %> 
                                                        data-automation-id='<%# Eval("Id").ToString() %>' />
                                                </div>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Actions">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="btnEdit" runat="server" CssClass="btn btn-sm btn-primary"
                                                    CommandName="EditAutomation" CommandArgument='<%# Eval("Id").ToString() %>'>
                                                    <i class="bi bi-pencil"></i> Edit
                                                </asp:LinkButton>
                                                <asp:LinkButton ID="btnDelete" runat="server" CssClass="btn btn-sm btn-danger"
                                                    CommandName="DeleteAutomation" CommandArgument='<%# Eval("Id").ToString() %>'
                                                    OnClientClick="return confirm('Are you sure you want to delete this rule?');">
                                                    <i class="bi bi-trash"></i> Delete
                                                </asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                            </ContentTemplate>
                        </asp:UpdatePanel>
                    </div>
                </div>
            </div>

            <!-- Settings Section -->
            <div id="settings" class="section">
                <div class="row">
                    <div class="col-md-6">
                        <div class="card">
                            <div class="card-header">
                                <h5>Network Settings</h5>
                            </div>
                            <div class="card-body">
                                <asp:UpdatePanel ID="UpdatePanelNetwork" runat="server">
                                    <ContentTemplate>
                                        <div class="mb-3">
                                            <label for="txtDeviceIP" class="form-label">Device IP</label>
                                            <asp:TextBox ID="txtDeviceIP" runat="server" CssClass="form-control" placeholder="192.168.1.100"></asp:TextBox>
                                        </div>
                                        <div class="mb-3">
                                            <label for="ddlConnectionType" class="form-label">Connection Type</label>
                                            <asp:DropDownList ID="ddlConnectionType" runat="server" CssClass="form-select">
                                                <asp:ListItem Value="wifi">WiFi</asp:ListItem>
                                                <asp:ListItem Value="ethernet">Ethernet</asp:ListItem>
                                            </asp:DropDownList>
                                        </div>
                                        <div class="mb-3">
                                            <label for="txtWifiSSID" class="form-label">WiFi SSID</label>
                                            <asp:TextBox ID="txtWifiSSID" runat="server" CssClass="form-control"></asp:TextBox>
                                        </div>
                                        <div class="mb-3">
                                            <label for="txtWifiPassword" class="form-label">WiFi Password</label>
                                            <asp:TextBox ID="txtWifiPassword" runat="server" CssClass="form-control" TextMode="Password"></asp:TextBox>
                                        </div>
                                        <asp:Button ID="btnSaveNetwork" runat="server" Text="Save Network Settings" CssClass="btn btn-primary" OnClick="btnSaveNetwork_Click" />
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="card">
                            <div class="card-header">
                                <h5>Device Settings</h5>
                            </div>
                            <div class="card-body">
                                <asp:UpdatePanel ID="UpdatePanelDevice" runat="server">
                                    <ContentTemplate>
                                        <div class="mb-3">
                                            <label for="txtDeviceName" class="form-label">Device Name</label>
                                            <asp:TextBox ID="txtDeviceName" runat="server" CssClass="form-control"></asp:TextBox>
                                        </div>
                                        <div class="mb-3">
                                            <label for="txtPollingInterval" class="form-label">Polling Interval (seconds)</label>
                                            <asp:TextBox ID="txtPollingInterval" runat="server" CssClass="form-control" TextMode="Number"></asp:TextBox>
                                        </div>
                                        <div class="mb-3">
                                            <label class="form-label">Date & Time Settings</label>
                                            <div class="form-check mb-2">
                                                <asp:RadioButton ID="rbAutoTime" runat="server" GroupName="timeSettings" Checked="true" />
                                                <label class="form-check-label" for="rbAutoTime">
                                                    Sync with NTP Server
                                                </label>
                                            </div>
                                            <div class="form-check">
                                                <asp:RadioButton ID="rbManualTime" runat="server" GroupName="timeSettings" />
                                                <label class="form-check-label" for="rbManualTime">
                                                    Set Manually
                                                </label>
                                            </div>
                                            <div class="mt-2" id="manualTimeControls" style="display: none;">
                                                <asp:TextBox ID="txtManualDateTime" runat="server" CssClass="form-control" placeholder="Select date and time"></asp:TextBox>
                                            </div>
                                        </div>
                                        <asp:Button ID="btnSaveDevice" runat="server" Text="Save Device Settings" CssClass="btn btn-primary" OnClick="btnSaveDevice_Click" />
                                        <asp:Button ID="btnRebootDevice" runat="server" Text="Reboot Device" CssClass="btn btn-warning ms-2" OnClick="btnRebootDevice_Click" OnClientClick="return confirm('Are you sure you want to reboot the device?');" />
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- USB Serial Communication Settings -->
                <div class="row mt-4">
                    <div class="col-md-12">
                        <div class="card">
                            <div class="card-header">
                                <h5>USB Serial Communication</h5>
                            </div>
                            <div class="card-body">
                                <asp:UpdatePanel ID="UpdatePanelUSB" runat="server">
                                    <ContentTemplate>
                                        <div class="mb-3 form-check form-switch">
                                            <asp:CheckBox ID="chkEnableUsb" runat="server" CssClass="form-check-input" />
                                            <label class="form-check-label" for="chkEnableUsb">Enable USB Serial Communication</label>
                                        </div>
                                        
                                        <div class="mb-3">
                                            <label for="txtComPort" class="form-label">COM Port</label>
                                            <asp:TextBox ID="txtComPort" runat="server" CssClass="form-control" placeholder="COM3"></asp:TextBox>
                                            <div class="form-text">Enter the COM port where your KC868-A8 is connected (e.g. COM3, COM4)</div>
                                        </div>
                                        
                                        <div class="mb-3">
                                            <label for="ddlBaudRate" class="form-label">Baud Rate</label>
                                            <asp:DropDownList ID="ddlBaudRate" runat="server" CssClass="form-select">
                                                <asp:ListItem Value="9600">9600</asp:ListItem>
                                                <asp:ListItem Value="19200">19200</asp:ListItem>
                                                <asp:ListItem Value="38400">38400</asp:ListItem>
                                                <asp:ListItem Value="57600">57600</asp:ListItem>
                                                <asp:ListItem Value="115200" Selected="True">115200</asp:ListItem>
                                            </asp:DropDownList>
                                        </div>
                                        
                                        <div class="mb-3">
                                            <label class="form-label">Available Commands:</label>
                                            <div class="card">
                                                <div class="card-body">
                                                    <p>The following commands can be sent directly to the device via USB:</p>
                                                    <ul>
                                                        <li><code>RELAY STATUS</code> - Show status of all relays</li>
                                                        <li><code>RELAY &lt;id&gt; ON/OFF/TOGGLE</code> - Control relay</li>
                                                        <li><code>RELAY ALL ON/OFF</code> - Control all relays</li>
                                                        <li><code>INPUT STATUS</code> - Show status of all inputs</li>
                                                        <li><code>ANALOG STATUS</code> - Show all analog input values</li>
                                                        <li><code>STATUS</code> - Show device status</li>
                                                        <li><code>CONFIG GET</code> - Show current configuration</li>
                                                        <li><code>CONFIG SET &lt;setting&gt; &lt;value&gt;</code> - Change a setting</li>
                                                    </ul>
                                                </div>
                                            </div>
                                        </div>
                                        
                                        <asp:Button ID="btnSaveUsbSettings" runat="server" Text="Save USB Settings" CssClass="btn btn-primary" OnClick="btnSaveUsbSettings_Click" />
                                        <asp:Button ID="btnTestUsbConnection" runat="server" Text="Test Connection" CssClass="btn btn-info ms-2" OnClick="btnTestUsbConnection_Click" />
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="row mt-4">
                    <div class="col-md-12">
                        <div class="card">
                            <div class="card-header">
                                <h5>I/O Configuration</h5>
                            </div>
                                                        <div class="card-body">
                                <asp:UpdatePanel ID="UpdatePanelIOConfig" runat="server">
                                    <ContentTemplate>
                                        <ul class="nav nav-tabs" role="tablist">
                                            <li class="nav-item" role="presentation">
                                                <button class="nav-link active" data-bs-toggle="tab" data-bs-target="#relayConfig" type="button" role="tab">Relay Outputs</button>
                                            </li>
                                            <li class="nav-item" role="presentation">
                                                <button class="nav-link" data-bs-toggle="tab" data-bs-target="#inputConfig" type="button" role="tab">Digital Inputs</button>
                                            </li>
                                            <li class="nav-item" role="presentation">
                                                <button class="nav-link" data-bs-toggle="tab" data-bs-target="#analogConfig" type="button" role="tab">Analog Inputs</button>
                                            </li>
                                        </ul>
                                        <div class="tab-content p-3 border border-top-0 rounded-bottom">
                                            <div class="tab-pane fade show active" id="relayConfig" role="tabpanel">
                                                <asp:Repeater ID="rptRelayConfig" runat="server">
                                                    <ItemTemplate>
                                                        <div class="mb-3 row">
                                                            <label class="col-sm-2 col-form-label">Relay <%# Eval("Id").ToString() %></label>
                                                            <div class="col-sm-4">
                                                                <asp:TextBox ID="txtRelayName" runat="server" CssClass="form-control" 
                                                                    Text='<%# Eval("Name") %>' data-relay-id='<%# Eval("Id").ToString() %>'></asp:TextBox>
                                                            </div>
                                                            <div class="col-sm-3">
                                                                <div class="form-check">
                                                                    <asp:CheckBox ID="chkInvertLogic" runat="server" CssClass="form-check-input"
                                                                        Checked='<%# Eval("InvertLogic") %>' data-relay-id='<%# Eval("Id").ToString() %>' />
                                                                    <label class="form-check-label" for='<%# "chkInvertLogic" + Eval("Id").ToString() %>'>
                                                                        Invert Logic
                                                                    </label>
                                                                </div>
                                                            </div>
                                                            <div class="col-sm-3">
                                                                <div class="form-check">
                                                                    <asp:CheckBox ID="chkRememberState" runat="server" CssClass="form-check-input"
                                                                        Checked='<%# Eval("RememberState") %>' data-relay-id='<%# Eval("Id").ToString() %>' />
                                                                    <label class="form-check-label" for='<%# "chkRememberState" + Eval("Id").ToString() %>'>
                                                                        Remember State
                                                                    </label>
                                                                </div>
                                                            </div>
                                                        </div>
                                                    </ItemTemplate>
                                                </asp:Repeater>
                                            </div>
                                            <div class="tab-pane fade" id="inputConfig" role="tabpanel">
                                                <asp:Repeater ID="rptInputConfig" runat="server">
                                                    <ItemTemplate>
                                                        <div class="mb-3 row">
                                                            <label class="col-sm-2 col-form-label">Input <%# Eval("Id").ToString() %></label>
                                                            <div class="col-sm-4">
                                                                <asp:TextBox ID="txtInputName" runat="server" CssClass="form-control" 
                                                                    Text='<%# Eval("Name") %>' data-input-id='<%# Eval("Id").ToString() %>'></asp:TextBox>
                                                            </div>
                                                            <div class="col-sm-3">
                                                                <div class="form-check">
                                                                    <asp:CheckBox ID="chkInputInvert" runat="server" CssClass="form-check-input"
                                                                        Checked='<%# Eval("InvertLogic") %>' data-input-id='<%# Eval("Id").ToString() %>' />
                                                                    <label class="form-check-label" for='<%# "chkInputInvert" + Eval("Id").ToString() %>'>
                                                                        Invert Logic
                                                                    </label>
                                                                </div>
                                                            </div>
                                                            <div class="col-sm-3">
                                                                <asp:DropDownList ID="ddlInputMode" runat="server" CssClass="form-select"
                                                                    SelectedValue='<%# Eval("Mode") %>' data-input-id='<%# Eval("Id").ToString() %>'>
                                                                    <asp:ListItem Value="normal">Normal</asp:ListItem>
                                                                    <asp:ListItem Value="toggle">Toggle</asp:ListItem>
                                                                    <asp:ListItem Value="push">Push Button</asp:ListItem>
                                                                </asp:DropDownList>
                                                            </div>
                                                        </div>
                                                    </ItemTemplate>
                                                </asp:Repeater>
                                            </div>
                                            <div class="tab-pane fade" id="analogConfig" role="tabpanel">
                                                <asp:Repeater ID="rptAnalogConfig" runat="server">
                                                    <ItemTemplate>
                                                        <div class="mb-3 row">
                                                            <label class="col-sm-2 col-form-label">Analog <%# Eval("Id").ToString() %></label>
                                                            <div class="col-sm-4">
                                                                <asp:TextBox ID="txtAnalogName" runat="server" CssClass="form-control" 
                                                                    Text='<%# Eval("Name") %>' data-analog-id='<%# Eval("Id").ToString() %>'></asp:TextBox>
                                                            </div>
                                                            <div class="col-sm-3">
                                                                <asp:DropDownList ID="ddlAnalogMode" runat="server" CssClass="form-select"
                                                                    SelectedValue='<%# Eval("Mode") %>' data-analog-id='<%# Eval("Id").ToString() %>'>
                                                                    <asp:ListItem Value="raw">Raw Value</asp:ListItem>
                                                                    <asp:ListItem Value="voltage">Voltage</asp:ListItem>
                                                                    <asp:ListItem Value="percent">Percentage</asp:ListItem>
                                                                    <asp:ListItem Value="custom">Custom</asp:ListItem>
                                                                </asp:DropDownList>
                                                            </div>
                                                            <div class="col-sm-3">
                                                                <asp:TextBox ID="txtAnalogUnit" runat="server" CssClass="form-control" 
                                                                    Text='<%# Eval("Unit") %>' placeholder="Unit" data-analog-id='<%# Eval("Id").ToString() %>'></asp:TextBox>
                                                            </div>
                                                        </div>
                                                    </ItemTemplate>
                                                </asp:Repeater>
                                            </div>
                                        </div>
                                        <div class="mt-3">
                                            <asp:Button ID="btnSaveIOConfig" runat="server" Text="Save I/O Configuration" CssClass="btn btn-primary" OnClick="btnSaveIOConfig_Click" />
                                        </div>
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Alexa Integration Section -->
            <div id="alexa" class="section">
                <div class="card">
                    <div class="card-header">
                        <h5>Amazon Alexa Integration</h5>
                    </div>
                    <div class="card-body">
                        <asp:UpdatePanel ID="UpdatePanelAlexa" runat="server">
                            <ContentTemplate>
                                <div class="mb-3 form-check form-switch">
                                    <input class="form-check-input" type="checkbox" id="chkEnableAlexa" runat="server" />
                                    <label class="form-check-label" for="chkEnableAlexa">Enable Alexa Integration</label>
                                </div>
                                
                                <div class="mb-3">
                                    <label for="txtAlexaDeviceName" class="form-label">Alexa Device Name</label>
                                    <asp:TextBox ID="txtAlexaDeviceName" runat="server" CssClass="form-control"></asp:TextBox>
                                    <div class="form-text">This is the name you will use to control the device with Alexa (e.g., "Smart Controller")</div>
                                </div>
                                
                                <div class="card mb-3">
                                    <div class="card-header">
                                        <h6>Alexa Discoverable Devices</h6>
                                    </div>
                                    <div class="card-body">
                                        <asp:Repeater ID="rptAlexaDevices" runat="server">
                                            <ItemTemplate>
                                                <div class="mb-2 form-check">
                                                    <asp:CheckBox ID="chkAlexaDevice" runat="server" CssClass="form-check-input"
                                                        Checked='<%# Eval("AlexaEnabled") %>' data-device-id='<%# Eval("Id").ToString() %>' />
                                                    <label class="form-check-label" for='<%# "chkAlexaDevice" + Eval("Id").ToString() %>'>
                                                        <%# Eval("Name") %> (<%# Eval("Type") %>)
                                                    </label>
                                                </div>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </div>
                                </div>
                                
                                <div class="mb-3">
                                    <label class="form-label">Alexa Voice Commands</label>
                                    <ul class="list-group">
                                        <li class="list-group-item">"Alexa, turn on [device name]"</li>
                                        <li class="list-group-item">"Alexa, turn off [device name]"</li>
                                        <li class="list-group-item">"Alexa, is [device name] on?"</li>
                                        <li class="list-group-item">"Alexa, set [device name] to 50%"</li>
                                    </ul>
                                </div>
                                
                                <asp:Button ID="btnSaveAlexa" runat="server" Text="Save Alexa Settings" CssClass="btn btn-primary" OnClick="btnSaveAlexa_Click" />
                                <asp:Button ID="btnDiscoverDevices" runat="server" Text="Test Discovery" CssClass="btn btn-info ms-2" OnClick="btnDiscoverDevices_Click" />
                            </ContentTemplate>
                        </asp:UpdatePanel>
                    </div>
                </div>
            </div>
        </div>

        <!-- Schedule Modal -->
        <div class="modal fade" id="scheduleModal" tabindex="-1" aria-hidden="true">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="scheduleModalLabel">Add Schedule</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <asp:UpdatePanel ID="UpdatePanelScheduleModal" runat="server">
                            <ContentTemplate>
                                <asp:HiddenField ID="hdnScheduleId" runat="server" Value="" />
                                <div class="mb-3">
                                    <label for="txtScheduleName" class="form-label">Schedule Name</label>
                                    <asp:TextBox ID="txtScheduleName" runat="server" CssClass="form-control"></asp:TextBox>
                                </div>
                                <div class="mb-3">
                                    <label for="txtScheduleTime" class="form-label">Time</label>
                                    <asp:TextBox ID="txtScheduleTime" runat="server" CssClass="form-control time-picker"></asp:TextBox>
                                </div>
                                <div class="mb-3">
                                    <label class="form-label">Days</label>
                                    <div class="d-flex flex-wrap">
                                        <div class="form-check me-3">
                                            <asp:CheckBox ID="chkMonday" runat="server" CssClass="form-check-input" />
                                            <label class="form-check-label" for="chkMonday">Monday</label>
                                        </div>
                                        <div class="form-check me-3">
                                            <asp:CheckBox ID="chkTuesday" runat="server" CssClass="form-check-input" />
                                            <label class="form-check-label" for="chkTuesday">Tuesday</label>
                                        </div>
                                        <div class="form-check me-3">
                                            <asp:CheckBox ID="chkWednesday" runat="server" CssClass="form-check-input" />
                                            <label class="form-check-label" for="chkWednesday">Wednesday</label>
                                        </div>
                                        <div class="form-check me-3">
                                            <asp:CheckBox ID="chkThursday" runat="server" CssClass="form-check-input" />
                                            <label class="form-check-label" for="chkThursday">Thursday</label>
                                        </div>
                                        <div class="form-check me-3">
                                            <asp:CheckBox ID="chkFriday" runat="server" CssClass="form-check-input" />
                                            <label class="form-check-label" for="chkFriday">Friday</label>
                                        </div>
                                        <div class="form-check me-3">
                                            <asp:CheckBox ID="chkSaturday" runat="server" CssClass="form-check-input" />
                                            <label class="form-check-label" for="chkSaturday">Saturday</label>
                                        </div>
                                        <div class="form-check me-3">
                                            <asp:CheckBox ID="chkSunday" runat="server" CssClass="form-check-input" />
                                            <label class="form-check-label" for="chkSunday">Sunday</label>
                                        </div>
                                    </div>
                                </div>
                                <div class="mb-3">
                                    <label for="ddlActionType" class="form-label">Action</label>
                                    <asp:DropDownList ID="ddlActionType" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlActionType_SelectedIndexChanged">
                                        <asp:ListItem Value="relay">Control Relay</asp:ListItem>
                                        <asp:ListItem Value="scene">Activate Scene</asp:ListItem>
                                        <asp:ListItem Value="notification">Send Notification</asp:ListItem>
                                    </asp:DropDownList>
                                </div>
                                
                                <asp:Panel ID="pnlRelayAction" runat="server">
                                    <div class="mb-3">
                                        <label for="ddlRelayTarget" class="form-label">Select Relay</label>
                                        <asp:DropDownList ID="ddlRelayTarget" runat="server" CssClass="form-select"></asp:DropDownList>
                                    </div>
                                    <div class="mb-3">
                                        <label for="ddlRelayState" class="form-label">State</label>
                                        <asp:DropDownList ID="ddlRelayState" runat="server" CssClass="form-select">
                                            <asp:ListItem Value="on">Turn ON</asp:ListItem>
                                            <asp:ListItem Value="off">Turn OFF</asp:ListItem>
                                            <asp:ListItem Value="toggle">Toggle</asp:ListItem>
                                        </asp:DropDownList>
                                    </div>
                                </asp:Panel>
                                
                                <asp:Panel ID="pnlSceneAction" runat="server" Visible="false">
                                    <div class="mb-3">
                                        <label for="ddlSceneTarget" class="form-label">Select Scene</label>
                                        <asp:DropDownList ID="ddlSceneTarget" runat="server" CssClass="form-select"></asp:DropDownList>
                                    </div>
                                </asp:Panel>
                                
                                <asp:Panel ID="pnlNotificationAction" runat="server" Visible="false">
                                    <div class="mb-3">
                                        <label for="txtNotificationMessage" class="form-label">Message</label>
                                        <asp:TextBox ID="txtNotificationMessage" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3"></asp:TextBox>
                                    </div>
                                </asp:Panel>
                                
                                <div class="form-check form-switch mb-3">
                                    <asp:CheckBox ID="chkScheduleEnabled" runat="server" CssClass="form-check-input" Checked="true" />
                                    <label class="form-check-label" for="chkScheduleEnabled">Enabled</label>
                                </div>
                            </ContentTemplate>
                        </asp:UpdatePanel>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <asp:Button ID="btnSaveSchedule" runat="server" Text="Save Schedule" CssClass="btn btn-primary" OnClick="btnSaveSchedule_Click" />
                    </div>
                </div>
            </div>
        </div>

        <!-- Enhanced Automation Modal with Logic & Timer Support -->
        <div class="modal fade" id="automationModal" tabindex="-1" aria-hidden="true">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="automationModalLabel">Add Automation Rule</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <asp:UpdatePanel ID="UpdatePanelAutomationModal" runat="server">
                            <ContentTemplate>
                                <asp:HiddenField ID="hdnAutomationId" runat="server" Value="" />
                                <div class="mb-3">
                                    <label for="txtAutomationName" class="form-label">Rule Name</label>
                                    <asp:TextBox ID="txtAutomationName" runat="server" CssClass="form-control"></asp:TextBox>
                                </div>
                                
                                <!-- Conditions Section -->
                                <div class="card mb-3">
                                    <div class="card-header d-flex justify-content-between align-items-center">
                                        <h6>Conditions</h6>
                                        <button type="button" class="btn btn-sm btn-primary" id="btnAddCondition">
                                            Add Condition
                                        </button>
                                    </div>
                                    <div class="card-body">
                                        <div id="conditionsContainer">
                                            <!-- Condition 1 -->
                                            <div class="condition-group mb-3">
                                                <div class="d-flex justify-content-between align-items-center mb-2">
                                                    <h6>Condition 1</h6>
                                                    <button type="button" class="btn btn-sm btn-danger btn-remove-condition">Remove</button>
                                                </div>
                                                
                                                <div class="mb-3">
                                                    <label for="ddlConditionType1" class="form-label">Condition Type</label>
                                                    <asp:DropDownList ID="ddlConditionType1" runat="server" CssClass="form-select condition-type" 
                                                        AutoPostBack="true" OnSelectedIndexChanged="ddlConditionType_SelectedIndexChanged" data-condition-index="1">
                                                        <asp:ListItem Value="digital">Digital Input</asp:ListItem>
                                                        <asp:ListItem Value="analog">Analog Threshold</asp:ListItem>
                                                    </asp:DropDownList>
                                                </div>
                                                
                                                <!-- Digital Input condition -->
                                                <asp:Panel ID="pnlDigitalCondition1" runat="server" CssClass="digital-condition" data-condition-index="1">
                                                    <div class="mb-3">
                                                        <label for="ddlDigitalInput1" class="form-label">Select Input</label>
                                                        <asp:DropDownList ID="ddlDigitalInput1" runat="server" CssClass="form-select digital-input" data-condition-index="1"></asp:DropDownList>
                                                    </div>
                                                    <div class="mb-3">
                                                        <label for="ddlDigitalState1" class="form-label">State</label>
                                                        <asp:DropDownList ID="ddlDigitalState1" runat="server" CssClass="form-select digital-state" data-condition-index="1">
                                                            <asp:ListItem Value="high">HIGH</asp:ListItem>
                                                            <asp:ListItem Value="low">LOW</asp:ListItem>
                                                            <asp:ListItem Value="rising">Rising Edge</asp:ListItem>
                                                            <asp:ListItem Value="falling">Falling Edge</asp:ListItem>
                                                            <asp:ListItem Value="change">Any Change</asp:ListItem>
                                                        </asp:DropDownList>
                                                    </div>
                                                </asp:Panel>
                                                
                                                <!-- Analog Input condition -->
                                                <asp:Panel ID="pnlAnalogCondition1" runat="server" CssClass="analog-condition" data-condition-index="1" Visible="false">
                                                    <div class="mb-3">
                                                        <label for="ddlAnalogInput1" class="form-label">Select Analog Input</label>
                                                        <asp:DropDownList ID="ddlAnalogInput1" runat="server" CssClass="form-select analog-input" data-condition-index="1"></asp:DropDownList>
                                                    </div>
                                                    <div class="mb-3">
                                                        <label for="ddlAnalogCondition1" class="form-label">Condition</label>
                                                        <asp:DropDownList ID="ddlAnalogCondition1" runat="server" CssClass="form-select analog-condition-type" data-condition-index="1">
                                                            <asp:ListItem Value="gt">Greater Than</asp:ListItem>
                                                            <asp:ListItem Value="lt">Less Than</asp:ListItem>
                                                            <asp:ListItem Value="eq">Equal To</asp:ListItem>
                                                            <asp:ListItem Value="between">Between</asp:ListItem>
                                                        </asp:DropDownList>
                                                    </div>
                                                    <div class="mb-3">
                                                        <label for="txtAnalogValue1" class="form-label">Value</label>
                                                        <asp:TextBox ID="txtAnalogValue1" runat="server" CssClass="form-control analog-threshold1" TextMode="Number" data-condition-index="1"></asp:TextBox>
                                                    </div>
                                                    <div class="mb-3 analog-threshold2-container" id="analogValue2Container1">
                                                        <label for="txtAnalogValue2_1" class="form-label">Upper Value</label>
                                                        <asp:TextBox ID="txtAnalogValue2_1" runat="server" CssClass="form-control analog-threshold2" TextMode="Number" data-condition-index="1"></asp:TextBox>
                                                    </div>
                                                </asp:Panel>
                                            </div>
                                        </div>
                                        
                                        <!-- Logic Operator -->
                                        <div class="mb-3" id="logicOperatorContainer">
                                            <label for="ddlLogicOperator" class="form-label">Logic Between Conditions</label>
                                            <asp:DropDownList ID="ddlLogicOperator" runat="server" CssClass="form-select">
                                                <asp:ListItem Value="AND">AND (All conditions must be true)</asp:ListItem>
                                                <asp:ListItem Value="OR">OR (Any condition can be true)</asp:ListItem>
                                            </asp:DropDownList>
                                        </div>
                                    </div>
                                </div>
                                
                                <!-- Timer Settings -->
                                <div class="card mb-3">
                                    <div class="card-header">
                                        <h6>Timer Settings</h6>
                                    </div>
                                    <div class="card-body">
                                        <div class="form-check form-switch mb-3">
                                            <asp:CheckBox ID="chkUseTimer" runat="server" CssClass="form-check-input" />
                                            <label class="form-check-label" for="chkUseTimer">Use Timer</label>
                                        </div>
                                        
                                        <div id="timerSettingsContainer" style="display: none;">
                                            <div class="mb-3">
                                                <label for="ddlTimerType" class="form-label">Timer Type</label>
                                                <asp:DropDownList ID="ddlTimerType" runat="server" CssClass="form-select">
                                                    <asp:ListItem Value="ondelay">ON Delay (Delay before activation)</asp:ListItem>
                                                    <asp:ListItem Value="offdelay">OFF Delay (Delay before deactivation)</asp:ListItem>
                                                </asp:DropDownList>
                                                <div class="form-text">
                                                    ON Delay: Wait before executing the action<br>
                                                    OFF Delay: Execute immediately, then revert after delay
                                                </div>
                                            </div>
                                            
                                            <div class="mb-3">
                                                <label for="txtTimerDuration" class="form-label">Duration (milliseconds)</label>
                                                <asp:TextBox ID="txtTimerDuration" runat="server" CssClass="form-control" TextMode="Number" Text="1000"></asp:TextBox>
                                                <div class="form-text">
                                                    1000ms = 1 second, 60000ms = 1 minute
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                                        
                                <!-- Action Settings -->
                                <div class="mb-3">
                                    <label for="ddlAutomationAction" class="form-label">Action Type</label>
                                    <asp:DropDownList ID="ddlAutomationAction" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="ddlAutomationAction_SelectedIndexChanged">
                                        <asp:ListItem Value="relay">Control Relay</asp:ListItem>
                                        <asp:ListItem Value="scene">Activate Scene</asp:ListItem>
                                        <asp:ListItem Value="notification">Send Notification</asp:ListItem>
                                    </asp:DropDownList>
                                </div>
                                
                                <asp:Panel ID="pnlAutomationRelayAction" runat="server">
                                    <div class="mb-3">
                                        <label for="ddlAutoRelayTarget" class="form-label">Select Relay</label>
                                        <asp:DropDownList ID="ddlAutoRelayTarget" runat="server" CssClass="form-select"></asp:DropDownList>
                                    </div>
                                    <div class="mb-3">
                                        <label for="ddlAutoRelayState" class="form-label">State</label>
                                        <asp:DropDownList ID="ddlAutoRelayState" runat="server" CssClass="form-select">
                                            <asp:ListItem Value="on">Turn ON</asp:ListItem>
                                            <asp:ListItem Value="off">Turn OFF</asp:ListItem>
                                            <asp:ListItem Value="toggle">Toggle</asp:ListItem>
                                        </asp:DropDownList>
                                    </div>
                                </asp:Panel>
                                
                                <asp:Panel ID="pnlAutomationSceneAction" runat="server" Visible="false">
                                    <div class="mb-3">
                                        <label for="ddlAutoSceneTarget" class="form-label">Select Scene</label>
                                        <asp:DropDownList ID="ddlAutoSceneTarget" runat="server" CssClass="form-select"></asp:DropDownList>
                                    </div>
                                </asp:Panel>
                                
                                <asp:Panel ID="pnlAutomationNotificationAction" runat="server" Visible="false">
                                    <div class="mb-3">
                                        <label for="txtAutoNotificationMessage" class="form-label">Message</label>
                                        <asp:TextBox ID="txtAutoNotificationMessage" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3"></asp:TextBox>
                                    </div>
                                </asp:Panel>
                                
                                <div class="form-check form-switch mb-3">
                                    <asp:CheckBox ID="chkAutomationEnabled" runat="server" CssClass="form-check-input" Checked="true" />
                                    <label class="form-check-label" for="chkAutomationEnabled">Enabled</label>
                                </div>
                            </ContentTemplate>
                        </asp:UpdatePanel>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <asp:Button ID="btnSaveAutomation" runat="server" Text="Save Rule" CssClass="btn btn-primary" OnClick="btnSaveAutomation_Click" />
                    </div>
                </div>
            </div>
        </div>
    </form>

    <script type="text/javascript">
        $(document).ready(function () {
            // Navigation handling
            $('.navbar-nav .nav-link').on('click', function (e) {
                e.preventDefault();

                // Get target section
                var target = $(this).attr('href');

                // Hide all sections
                $('.section').removeClass('active');

                // Show target section
                $(target).addClass('active');

                // Update active nav link
                $('.navbar-nav .nav-link').removeClass('active');
                $(this).addClass('active');
            });

            // Initialize time picker
            flatpickr('.time-picker', {
                enableTime: true,
                noCalendar: true,
                dateFormat: "H:i",
                time_24hr: true
            });

            // Initialize datetime picker
            flatpickr('#txtManualDateTime', {
                enableTime: true,
                dateFormat: "Y-m-d H:i",
                time_24hr: true
            });

            // Toggle manual time settings
            $('#rbManualTime').change(function () {
                if ($(this).is(':checked')) {
                    $('#manualTimeControls').show();
                }
            });

            $('#rbAutoTime').change(function () {
                if ($(this).is(':checked')) {
                    $('#manualTimeControls').hide();
                }
            });

            // Initialize condition index counter
            let conditionIndex = 1;

            // Timer checkbox toggle
            $('#chkUseTimer').change(function () {
                if ($(this).is(':checked')) {
                    $('#timerSettingsContainer').show();
                } else {
                    $('#timerSettingsContainer').hide();
                }
            });

            // Add condition button
            $('#btnAddCondition').click(function () {
                conditionIndex++;

                // Clone the first condition group
                const newCondition = $('.condition-group').first().clone();

                // Update IDs and labels
                newCondition.find('h6').text('Condition ' + conditionIndex);

                // Update data attributes
                newCondition.find('[data-condition-index]').attr('data-condition-index', conditionIndex);

                // Clear any selected values
                newCondition.find('select').each(function () {
                    $(this).prop('selectedIndex', 0);
                });

                newCondition.find('input[type="number"]').val('');

                // Add to container
                $('#conditionsContainer').append(newCondition);

                // Show logic operator if we have more than one condition
                if (conditionIndex > 1) {
                    $('#logicOperatorContainer').show();
                }

                // Initialize analog condition visibility
                newCondition.find('.analog-condition').hide();
                newCondition.find('.digital-condition').show();

                // Re-attach event handlers
                attachConditionEvents();
            });

            // Initial setup
            function init() {
                // Show/hide logic operator based on condition count
                if ($('.condition-group').length > 1) {
                    $('#logicOperatorContainer').show();
                } else {
                    $('#logicOperatorContainer').hide();
                }

                // Initialize timer container visibility
                if ($('#chkUseTimer').is(':checked')) {
                    $('#timerSettingsContainer').show();
                } else {
                    $('#timerSettingsContainer').hide();
                }

                // Attach events
                attachConditionEvents();
            }

            // Attach events to condition controls
            function attachConditionEvents() {
                // Remove condition button
                $('.btn-remove-condition').off('click').on('click', function () {
                    if ($('.condition-group').length > 1) {
                        $(this).closest('.condition-group').remove();

                        // Update condition numbers
                        $('.condition-group').each(function (index) {
                            $(this).find('h6').text('Condition ' + (index + 1));
                        });

                        // Hide logic operator if only one condition left
                        if ($('.condition-group').length <= 1) {
                            $('#logicOperatorContainer').hide();
                        }
                    }
                });

                // Condition type change
                $('.condition-type').off('change').on('change', function () {
                    const index = $(this).data('condition-index');
                    const type = $(this).val();

                    if (type === 'digital') {
                        $('.digital-condition[data-condition-index="' + index + '"]').show();
                        $('.analog-condition[data-condition-index="' + index + '"]').hide();
                    } else if (type === 'analog') {
                        $('.digital-condition[data-condition-index="' + index + '"]').hide();
                        $('.analog-condition[data-condition-index="' + index + '"]').show();

                        // Check analog condition type for "between"
                        const analogCondType = $('.analog-condition-type[data-condition-index="' + index + '"]').val();
                        if (analogCondType === 'between') {
                            $('#analogValue2Container' + index).show();
                        } else {
                            $('#analogValue2Container' + index).hide();
                        }
                    }
                });

                // Analog condition type change
                $('.analog-condition-type').off('change').on('change', function () {
                    const index = $(this).data('condition-index');
                    const type = $(this).val();

                    if (type === 'between') {
                        $('#analogValue2Container' + index).show();
                    } else {
                        $('#analogValue2Container' + index).hide();
                    }
                });
            }

            // Run initialization
            init();

            // Handle post-async update re-initialization
            var prm = Sys.WebForms.PageRequestManager.getInstance();
            prm.add_endRequest(function () {
                init();
            });

            // Relay switches
            $(document).on('change', '.relay-switch', function () {
                var relayId = $(this).data('relay-id');
                var state = $(this).prop('checked');

                // Send AJAX request to toggle relay
                $.ajax({
                    type: "POST",
                    url: "Default.aspx/ToggleRelay",
                    data: JSON.stringify({ 'relayId': relayId, 'state': state }),
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function (response) {
                        console.log("Relay " + relayId + " toggled to " + state);
                    },
                    error: function (xhr, status, error) {
                        console.error("Error toggling relay: " + error);
                        // Revert the checkbox state on error
                        $(this).prop('checked', !state);
                    }
                });
            });

            // Schedule switches
            $(document).on('change', '.schedule-switch', function () {
                var scheduleId = $(this).data('schedule-id');
                var enabled = $(this).prop('checked');

                // Send AJAX request to toggle schedule
                $.ajax({
                    type: "POST",
                    url: "Default.aspx/ToggleSchedule",
                    data: JSON.stringify({ 'scheduleId': scheduleId, 'enabled': enabled }),
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function (response) {
                        console.log("Schedule " + scheduleId + " toggled to " + enabled);
                    },
                    error: function (xhr, status, error) {
                        console.error("Error toggling schedule: " + error);
                        // Revert the checkbox state on error
                        $(this).prop('checked', !enabled);
                    }
                });
            });

            // Automation switches
            $(document).on('change', '.automation-switch', function () {
                var automationId = $(this).data('automation-id');
                var enabled = $(this).prop('checked');

                // Send AJAX request to toggle automation
                $.ajax({
                    type: "POST",
                    url: "Default.aspx/ToggleAutomation",
                    data: JSON.stringify({ 'automationId': automationId, 'enabled': enabled }),
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function (response) {
                        console.log("Automation " + automationId + " toggled to " + enabled);
                    },
                    error: function (xhr, status, error) {
                        console.error("Error toggling automation: " + error);
                        // Revert the checkbox state on error
                        $(this).prop('checked', !enabled);
                    }
                });
            });

            // Initialize data refresh
            function refreshData() {
                // Use PageMethods to refresh data
                PageMethods.RefreshData(function (response) {
                    // Update relay states
                    $.each(response.Relays, function (index, relay) {
                        $('#relay' + relay.Id).prop('checked', relay.State);
                    });

                    // Update input states
                    $.each(response.Inputs, function (index, input) {
                        $('.input-indicator').eq(index).toggleClass('active', input.State);
                    });

                    // Update analog values
                    $.each(response.AnalogInputs, function (index, analog) {
                        var progressBar = $('.progress-bar').eq(index);
                        var percentage = (analog.Value * 100 / 4095);
                        progressBar.css('width', percentage + '%');
                        progressBar.text(analog.Value);
                    });

                    // Update system status
                    $('#lblCurrentTime').text(response.CurrentTime);
                    $('#lblDeviceIP').text(response.DeviceIP);
                    $('#lblConnectionType').text(response.ConnectionType);
                    $('#lblConnectionStatus').text(response.Connected ? 'Connected' : 'Disconnected');
                    $('#lblConnectionStatus').toggleClass('connected', response.Connected);

                    setTimeout(refreshData, 5000); // Refresh every 5 seconds
                }, function (error) {
                    console.error("Error refreshing data: " + error.get_message());
                    setTimeout(refreshData, 10000); // Try again in 10 seconds
                });
            }

            // Start the refresh cycle
            setTimeout(refreshData, 5000);
        });

        // Automation Rule Client-Side Handling
        function collectAutomationRuleData() {
            const ruleData = {
                id: $('#hdnAutomationId').val() || null,
                name: $('#txtAutomationName').val(),
                enabled: $('#chkAutomationEnabled').is(':checked'),
                conditions: [],
                logicOperator: $('#ddlLogicOperator').val() || 'AND',
                useTimer: $('#chkUseTimer').is(':checked'),
                timerType: $('#ddlTimerType').val(),
                timerDuration: parseInt($('#txtTimerDuration').val()),
                action: $('#ddlAutomationAction').val(),
                targetId: null,
                targetState: null,
                message: null
            };

            // Collect conditions
            $('.condition-group').each(function (index) {
                const conditionIndex = $(this).find('[data-condition-index]').first().data('condition-index');
                const conditionType = $(this).find('.condition-type').val();

                const condition = {
                    type: conditionType
                };

                if (conditionType === 'digital') {
                    condition.sourceId = parseInt($(this).find('.digital-input').val());
                    condition.condition = $(this).find('.digital-state').val();
                } else if (conditionType === 'analog') {
                    condition.sourceId = parseInt($(this).find('.analog-input').val());
                    condition.condition = $(this).find('.analog-condition-type').val();
                    condition.threshold1 = parseInt($(this).find('.analog-threshold1').val());

                    if (condition.condition === 'between') {
                        condition.threshold2 = parseInt($(this).find('.analog-threshold2').val());
                    }
                }

                ruleData.conditions.push(condition);
            });

            // Collect action details
            if (ruleData.action === 'relay') {
                ruleData.targetId = parseInt($('#ddlAutoRelayTarget').val());
                ruleData.targetState = $('#ddlAutoRelayState').val();
            } else if (ruleData.action === 'scene') {
                ruleData.targetId = parseInt($('#ddlAutoSceneTarget').val());
            } else if (ruleData.action === 'notification') {
                ruleData.message = $('#txtAutoNotificationMessage').val();
            }

            return ruleData;
        }

        function populateAutomationRuleForm(rule) {
            // Clear existing conditions
            $('.condition-group:not(:first)').remove();

            // Set basic fields
            $('#hdnAutomationId').val(rule.id);
            $('#txtAutomationName').val(rule.name);
            $('#chkAutomationEnabled').prop('checked', rule.enabled);

            // Set conditions
            if (rule.conditions && rule.conditions.length > 0) {
                // Set first condition
                const firstCondition = rule.conditions[0];
                const $firstGroup = $('.condition-group:first');

                $firstGroup.find('.condition-type').val(firstCondition.type);

                if (firstCondition.type === 'digital') {
                    $firstGroup.find('.digital-input').val(firstCondition.sourceId);
                    $firstGroup.find('.digital-state').val(firstCondition.condition);
                    $firstGroup.find('.digital-condition').show();
                    $firstGroup.find('.analog-condition').hide();
                } else if (firstCondition.type === 'analog') {
                    $firstGroup.find('.analog-input').val(firstCondition.sourceId);
                    $firstGroup.find('.analog-condition-type').val(firstCondition.condition);
                    $firstGroup.find('.analog-threshold1').val(firstCondition.threshold1);

                    if (firstCondition.condition === 'between') {
                        $firstGroup.find('.analog-threshold2').val(firstCondition.threshold2);
                        $firstGroup.find('.analog-threshold2-container').show();
                    } else {
                        $firstGroup.find('.analog-threshold2-container').hide();
                    }

                    $firstGroup.find('.digital-condition').hide();
                    $firstGroup.find('.analog-condition').show();
                }

                // Add additional conditions
                for (let i = 1; i < rule.conditions.length; i++) {
                    $('#btnAddCondition').click(); // This creates a new condition group

                    const condition = rule.conditions[i];
                    const $newGroup = $('.condition-group').eq(i);

                    $newGroup.find('.condition-type').val(condition.type);

                    if (condition.type === 'digital') {
                        $newGroup.find('.digital-input').val(condition.sourceId);
                        $newGroup.find('.digital-state').val(condition.condition);
                        $newGroup.find('.digital-condition').show();
                        $newGroup.find('.analog-condition').hide();
                    } else if (condition.type === 'analog') {
                        $newGroup.find('.analog-input').val(condition.sourceId);
                        $newGroup.find('.analog-condition-type').val(condition.condition);
                        $newGroup.find('.analog-threshold1').val(condition.threshold1);

                        if (condition.condition === 'between') {
                            $newGroup.find('.analog-threshold2').val(condition.threshold2);
                            $newGroup.find('.analog-threshold2-container').show();
                        } else {
                            $newGroup.find('.analog-threshold2-container').hide();
                        }

                        $newGroup.find('.digital-condition').hide();
                        $newGroup.find('.analog-condition').show();
                    }
                }

                // Set logic operator if multiple conditions
                if (rule.conditions.length > 1) {
                    $('#ddlLogicOperator').val(rule.logicOperator || 'AND');
                    $('#logicOperatorContainer').show();
                } else {
                    $('#logicOperatorContainer').hide();
                }
            }

            // Set timer settings
            $('#chkUseTimer').prop('checked', rule.useTimer || false);
            if (rule.useTimer) {
                $('#ddlTimerType').val(rule.timerType || 'ondelay');
                $('#txtTimerDuration').val(rule.timerDuration || 1000);
                $('#timerSettingsContainer').show();
            } else {
                $('#timerSettingsContainer').hide();
            }

            // Set action details
            $('#ddlAutomationAction').val(rule.action);

            if (rule.action === 'relay') {
                $('#ddlAutoRelayTarget').val(rule.targetId);
                $('#ddlAutoRelayState').val(rule.targetState);
                $('#pnlAutomationRelayAction').show();
                $('#pnlAutomationSceneAction').hide();
                $('#pnlAutomationNotificationAction').hide();
            } else if (rule.action === 'scene') {
                $('#ddlAutoSceneTarget').val(rule.targetId);
                $('#pnlAutomationRelayAction').hide();
                $('#pnlAutomationSceneAction').show();
                $('#pnlAutomationNotificationAction').hide();
            } else if (rule.action === 'notification') {
                $('#txtAutoNotificationMessage').val(rule.message);
                $('#pnlAutomationRelayAction').hide();
                $('#pnlAutomationSceneAction').hide();
                $('#pnlAutomationNotificationAction').show();
            }
        }

        // Server-side code to open the edit dialog
        function showEditAutomationDialog(ruleId) {
            // AJAX call to get rule data
            $.ajax({
                type: "POST",
                url: "Default.aspx/GetAutomationRule",
                data: JSON.stringify({ 'ruleId': ruleId }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    const rule = JSON.parse(response.d);
                    populateAutomationRuleForm(rule);
                    $('#automationModal').modal('show');
                },
                error: function (xhr, status, error) {
                    console.error("Error fetching automation rule: " + error);
                    alert("Error loading automation rule data.");
                }
            });
        }

        // Function to save the rule
        function saveAutomationRule() {
            const ruleData = collectAutomationRuleData();

            $.ajax({
                type: "POST",
                url: "Default.aspx/SaveAutomationRule",
                data: JSON.stringify({ 'ruleData': JSON.stringify(ruleData) }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    $('#automationModal').modal('hide');
                    // Refresh the automation rules grid
                    __doPostBack('UpdatePanelAutomation', '');
                },
                error: function (xhr, status, error) {
                    console.error("Error saving automation rule: " + error);
                    alert("Error saving automation rule.");
                }
            });
        }
    </script>
</body>
</html>
                                