Imports System.Data
Imports System.Data.SqlClient
Imports System.IO
Imports System.Net
Imports System.Net.Http
Imports System.Web.Services
Imports System.Web.UI
Imports System.Web.UI.WebControls
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.IO.Ports

Partial Class _Default
    Inherits System.Web.UI.Page

    ' Database connection string - adjust as needed
    Private Shared ConnectionString As String = "Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\KC868Database.mdf;Integrated Security=True"

    ' API endpoints for communicating with the ESP32
    Private Const ApiBaseUrl As String = "http://{0}/api/"
    Private deviceIp As String = "192.168.1.100" ' Default IP, will be loaded from settings

    ' Serial port for USB communication
    Private Shared serialPort As SerialPort = Nothing
    Private Shared serialEnabled As Boolean = False
    Private Shared serialBaudRate As Integer = 115200
    Private Shared serialPortName As String = "COM3" ' Default, will be loaded from settings

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            ' Load settings
            LoadSettings()

            ' Initialize USB communication if enabled
            InitializeSerialPort()

            ' Load relay data
            LoadRelayData()

            ' Load input data
            LoadInputData()

            ' Load analog input data
            LoadAnalogInputData()

            ' Load schedules
            LoadSchedules()

            ' Load automation rules
            LoadAutomationRules()

            ' Load Alexa settings
            LoadAlexaSettings()

            ' Load I/O configuration
            LoadIOConfiguration()

            ' Initialize timer for auto refresh (this is just for UI, actual data refresh is handled by JavaScript)
            ScriptManager.RegisterStartupScript(Page, Page.GetType(), "refreshScript", "setTimeout(refreshData, 5000);", True)
        End If
    End Sub

    ' Initialize serial port for USB communication
    Private Sub InitializeSerialPort()
        Try
            ' Close existing connection if open
            If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                serialPort.Close()
                serialPort = Nothing
            End If

            ' Create new serial port if USB is enabled
            If serialEnabled Then
                serialPort = New SerialPort()
                serialPort.PortName = serialPortName
                serialPort.BaudRate = serialBaudRate
                serialPort.DataBits = 8
                serialPort.Parity = Parity.None
                serialPort.StopBits = StopBits.One
                serialPort.Handshake = Handshake.None
                serialPort.ReadTimeout = 1000 ' 1 second timeout
                serialPort.WriteTimeout = 1000 ' 1 second timeout

                Try
                    serialPort.Open()
                    If serialPort.IsOpen Then
                        ' Send a test command to verify connection
                        serialPort.WriteLine("STATUS")

                        ' Wait for response
                        Dim response As String = ReadSerialResponse()

                        ' If we got a response, connection is working
                        If Not String.IsNullOrEmpty(response) Then
                            lblConnectionStatus.Text = "Connected (USB)"
                            lblConnectionStatus.CssClass = "connection-status connected"
                        End If
                    End If
                Catch ex As Exception
                    ' Unable to open serial port
                    serialPort = Nothing
                End Try
            End If
        Catch ex As Exception
            ' Handle exception
            Response.Write("Error initializing serial port: " & ex.Message)
        End Try
    End Sub

    ' Read response from serial port
    Private Shared Function ReadSerialResponse() As String
        If serialPort Is Nothing OrElse Not serialPort.IsOpen Then
            Return String.Empty
        End If

        Try
            Dim response As New System.Text.StringBuilder()
            Dim buffer(1024) As Byte
            Dim bytesRead As Integer

            ' Read until we get a separator or timeout
            Dim startTime As DateTime = DateTime.Now

            While (DateTime.Now - startTime).TotalSeconds < 2 ' 2 second max wait
                If serialPort.BytesToRead > 0 Then
                    bytesRead = serialPort.Read(buffer, 0, Math.Min(serialPort.BytesToRead, buffer.Length))
                    response.Append(System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead))

                    ' Check for end of response marker
                    If response.ToString().Contains("-----") Then
                        Exit While
                    End If
                Else
                    ' Small delay to prevent tight loop
                    System.Threading.Thread.Sleep(50)
                End If
            End While

            Return response.ToString()
        Catch ex As Exception
            ' Handle exception
            Return "ERROR: " & ex.Message
        End Try
    End Function

    ' Send command to the device via USB
    Private Shared Function SendSerialCommand(command As String) As String
        If serialPort Is Nothing OrElse Not serialPort.IsOpen Then
            Return "ERROR: Serial port not connected"
        End If

        Try
            ' Clear any pending data
            serialPort.DiscardInBuffer()

            ' Send command
            serialPort.WriteLine(command)

            ' Read response
            Return ReadSerialResponse()
        Catch ex As Exception
            Return "ERROR: " & ex.Message
        End Try
    End Function

    ' Load application settings
    Private Sub LoadSettings()
        Try
            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                Dim cmd As New SqlCommand("SELECT SettingName, SettingValue FROM Settings", conn)
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim settingName As String = reader("SettingName").ToString()
                        Dim settingValue As String = reader("SettingValue").ToString()

                        Select Case settingName
                            Case "DeviceIP"
                                deviceIp = settingValue
                                txtDeviceIP.Text = settingValue
                            Case "ConnectionType"
                                ddlConnectionType.SelectedValue = settingValue
                            Case "WifiSSID"
                                txtWifiSSID.Text = settingValue
                            Case "DeviceName"
                                txtDeviceName.Text = settingValue
                            Case "PollingInterval"
                                txtPollingInterval.Text = settingValue
                            Case "AlexaDeviceName"
                                txtAlexaDeviceName.Text = settingValue
                            Case "AlexaEnabled"
                                chkEnableAlexa.Checked = Boolean.Parse(settingValue)
                            Case "SerialEnabled"
                                serialEnabled = Boolean.Parse(settingValue)
                                chkEnableUsb.Checked = serialEnabled
                            Case "SerialBaudRate"
                                serialBaudRate = Integer.Parse(settingValue)
                                ddlBaudRate.SelectedValue = serialBaudRate.ToString()
                            Case "SerialPortName"
                                serialPortName = settingValue
                                txtComPort.Text = serialPortName
                        End Select
                    End While
                End Using

                ' Add USB settings if they don't exist
                EnsureUsbSettingsExist(conn)
            End Using
        Catch ex As Exception
            ' Handle exception
            Response.Write("Error loading settings: " & ex.Message)
        End Try
    End Sub

    ' Ensure USB settings exist in the database
    Private Sub EnsureUsbSettingsExist(conn As SqlConnection)
        Try
            ' Check if USB settings exist
            Dim cmd As New SqlCommand("SELECT COUNT(*) FROM Settings WHERE SettingName = 'SerialEnabled'", conn)
            Dim settingExists As Integer = Convert.ToInt32(cmd.ExecuteScalar())

            If settingExists = 0 Then
                ' Add USB settings
                cmd = New SqlCommand("INSERT INTO Settings (SettingName, SettingValue) VALUES ('SerialEnabled', 'True')", conn)
                cmd.ExecuteNonQuery()

                cmd = New SqlCommand("INSERT INTO Settings (SettingName, SettingValue) VALUES ('SerialBaudRate', '115200')", conn)
                cmd.ExecuteNonQuery()

                cmd = New SqlCommand("INSERT INTO Settings (SettingName, SettingValue) VALUES ('SerialPortName', 'COM3')", conn)
                cmd.ExecuteNonQuery()
            End If
        Catch ex As Exception
            ' Handle exception
            Response.Write("Error ensuring USB settings: " & ex.Message)
        End Try
    End Sub

    ' Save USB settings
    Protected Sub btnSaveUsbSettings_Click(sender As Object, e As EventArgs)
        Try
            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                ' Update serial enabled status
                Dim cmd As New SqlCommand("UPDATE Settings SET SettingValue = @Value WHERE SettingName = 'SerialEnabled'", conn)
                cmd.Parameters.AddWithValue("@Value", chkEnableUsb.Checked.ToString())
                cmd.ExecuteNonQuery()

                ' Update baud rate
                cmd = New SqlCommand("UPDATE Settings SET SettingValue = @Value WHERE SettingName = 'SerialBaudRate'", conn)
                cmd.Parameters.AddWithValue("@Value", ddlBaudRate.SelectedValue)
                cmd.ExecuteNonQuery()

                ' Update port name
                cmd = New SqlCommand("UPDATE Settings SET SettingValue = @Value WHERE SettingName = 'SerialPortName'", conn)
                cmd.Parameters.AddWithValue("@Value", txtComPort.Text)
                cmd.ExecuteNonQuery()

                ' Update local variables
                serialEnabled = chkEnableUsb.Checked
                serialBaudRate = Integer.Parse(ddlBaudRate.SelectedValue)
                serialPortName = txtComPort.Text

                ' Reinitialize serial port
                InitializeSerialPort()

                ' Send settings to the device if using network connection
                If serialPort Is Nothing OrElse Not serialPort.IsOpen Then
                    SendUsbSettingsToDeviceViaNetwork()
                Else
                    ' Send settings directly via USB
                    Dim command As String = String.Format("CONFIG SET usb {0}", If(serialEnabled, "enabled", "disabled"))
                    SendSerialCommand(command)

                    command = String.Format("CONFIG SET baudrate {0}", serialBaudRate)
                    SendSerialCommand(command)

                    ' Save config
                    SendSerialCommand("CONFIG SAVE")
                End If

                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "usbSaved", "alert('USB settings saved successfully!');", True)
            End Using
        Catch ex As Exception
            ' Handle exception
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "usbError", "alert('Error saving USB settings: " & ex.Message.Replace("'", "\'") & "');", True)
        End Try
    End Sub

    ' Test USB connection
    Protected Sub btnTestUsbConnection_Click(sender As Object, e As EventArgs)
        Try
            ' Update settings
            serialEnabled = chkEnableUsb.Checked
            serialBaudRate = Integer.Parse(ddlBaudRate.SelectedValue)
            serialPortName = txtComPort.Text

            ' Try to connect
            InitializeSerialPort()

            If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                ' Send a test command
                Dim response As String = SendSerialCommand("STATUS")

                If response.StartsWith("ERROR") Then
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "usbTestFailed", "alert('Connection failed: " & response.Replace("'", "\'") & "');", True)
                Else
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "usbTestSuccess", "alert('Connection successful! Device responded with status information.');", True)
                End If
            Else
                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "usbTestFailed", "alert('Could not open COM port " & serialPortName & ". Make sure the device is connected and the correct port is selected.');", True)
            End If
        Catch ex As Exception
            ' Handle exception
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "usbTestError", "alert('Error testing USB connection: " & ex.Message.Replace("'", "\'") & "');", True)
        End Try
    End Sub

    ' Send USB settings to device via network
    Private Sub SendUsbSettingsToDeviceViaNetwork()
        Try
            ' Send settings to the device
            Dim url As String = String.Format(ApiBaseUrl, deviceIp) & "settings/usb"
            Dim postData As String = JsonConvert.SerializeObject(New With {
                .enabled = serialEnabled,
                .baudRate = serialBaudRate
            })

            Using client As New WebClient()
                client.Headers(HttpRequestHeader.ContentType) = "application/json"
                client.UploadString(url, "POST", postData)
            End Using
        Catch ex As Exception
            ' Handle exception
            Throw New Exception("Error sending USB settings to device: " & ex.Message)
        End Try
    End Sub

    ' Load relay data
    Private Sub LoadRelayData()
        Try
            ' Get relay data from the database
            Dim dt As New DataTable()

            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                Dim cmd As New SqlCommand("SELECT Id, Name, State, InvertLogic, RememberState FROM Relays ORDER BY Id", conn)
                Dim adapter As New SqlDataAdapter(cmd)
                adapter.Fill(dt)
            End Using

            ' Bind to repeater
            rptRelays.DataSource = dt
            rptRelays.DataBind()

            ' Also bind to the configuration repeater
            rptRelayConfig.DataSource = dt
            rptRelayConfig.DataBind()

            ' Populate relay dropdown for scheduling
            ddlRelayTarget.Items.Clear()
            For Each row As DataRow In dt.Rows
                ddlRelayTarget.Items.Add(New ListItem(row("Name").ToString(), row("Id").ToString()))
            Next

            ' Populate relay dropdown for automation
            ddlAutoRelayTarget.Items.Clear()
            For Each row As DataRow In dt.Rows
                ddlAutoRelayTarget.Items.Add(New ListItem(row("Name").ToString(), row("Id").ToString()))
            Next

        Catch ex As Exception
            ' Handle exception
            Response.Write("Error loading relay data: " & ex.Message)
        End Try
    End Sub

    ' Load input data
    Private Sub LoadInputData()
        Try
            ' Get input data from the database
            Dim dt As New DataTable()

            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                Dim cmd As New SqlCommand("SELECT Id, Name, State, InvertLogic, Mode FROM Inputs ORDER BY Id", conn)
                Dim adapter As New SqlDataAdapter(cmd)
                adapter.Fill(dt)
            End Using

            ' Bind to repeater
            rptInputs.DataSource = dt
            rptInputs.DataBind()

            ' Also bind to the configuration repeater
            rptInputConfig.DataSource = dt
            rptInputConfig.DataBind()

            ' Populate input dropdown for automation
            ddlDigitalInput1.Items.Clear()
            For Each row As DataRow In dt.Rows
                ddlDigitalInput1.Items.Add(New ListItem(row("Name").ToString(), row("Id").ToString()))
            Next

        Catch ex As Exception
            ' Handle exception
            Response.Write("Error loading input data: " & ex.Message)
        End Try
    End Sub

    ' Load analog input data
    Private Sub LoadAnalogInputData()
        Try
            ' Get analog input data from the database
            Dim dt As New DataTable()

            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                Dim cmd As New SqlCommand("SELECT Id, Name, Value, Mode, Unit FROM AnalogInputs ORDER BY Id", conn)
                Dim adapter As New SqlDataAdapter(cmd)
                adapter.Fill(dt)
            End Using

            ' Bind to repeater
            rptAnalogInputs.DataSource = dt
            rptAnalogInputs.DataBind()

            ' Also bind to the configuration repeater
            rptAnalogConfig.DataSource = dt
            rptAnalogConfig.DataBind()

            ' Populate analog input dropdown for automation
            ddlAnalogInput1.Items.Clear()
            For Each row As DataRow In dt.Rows
                ddlAnalogInput1.Items.Add(New ListItem(row("Name").ToString(), row("Id").ToString()))
            Next

        Catch ex As Exception
            ' Handle exception
            Response.Write("Error loading analog input data: " & ex.Message)
        End Try
    End Sub

    ' Load schedules
    Private Sub LoadSchedules()
        Try
            ' Get schedules from the database
            Dim dt As New DataTable()

            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                Dim cmd As New SqlCommand("SELECT Id, Name, Time, Days, Action, Target, Enabled FROM Schedules ORDER BY Time", conn)
                Dim adapter As New SqlDataAdapter(cmd)
                adapter.Fill(dt)
            End Using

            ' Bind to grid view
            gvSchedules.DataSource = dt
            gvSchedules.DataBind()

        Catch ex As Exception
            ' Handle exception
            Response.Write("Error loading schedules: " & ex.Message)
        End Try
    End Sub

    ' Load automation rules
    Private Sub LoadAutomationRules()
        Try
            ' Get automation rules from the database
            Dim dt As New DataTable()

            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                Dim cmd As New SqlCommand("SELECT Id, Name, Condition, LogicOperator, UseTimer, TimerType, TimerDuration, Action, Enabled FROM Automation ORDER BY Id", conn)
                Dim adapter As New SqlDataAdapter(cmd)
                adapter.Fill(dt)
            End Using

            ' Bind to grid view
            gvAutomation.DataSource = dt
            gvAutomation.DataBind()

        Catch ex As Exception
            ' Handle exception
            Response.Write("Error loading automation rules: " & ex.Message)
        End Try
    End Sub

    ' Load Alexa settings
    Private Sub LoadAlexaSettings()
        Try
            ' Get Alexa-enabled devices from the database
            Dim dt As New DataTable()

            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                Dim cmd As New SqlCommand("SELECT Id, Name, Type, AlexaEnabled FROM AlexaDevices ORDER BY Type, Name", conn)
                Dim adapter As New SqlDataAdapter(cmd)
                adapter.Fill(dt)
            End Using

            ' Bind to repeater
            rptAlexaDevices.DataSource = dt
            rptAlexaDevices.DataBind()

        Catch ex As Exception
            ' Handle exception
            Response.Write("Error loading Alexa settings: " & ex.Message)
        End Try
    End Sub

    ' Load I/O configuration
    Private Sub LoadIOConfiguration()
        ' Relay and input configuration is already loaded in their respective methods
    End Sub

    ' Handle All ON button click
    Protected Sub btnAllOn_Click(sender As Object, e As EventArgs)
        Try
            ' Update all relays in the database
            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                Dim cmd As New SqlCommand("UPDATE Relays SET State = 1", conn)
                cmd.ExecuteNonQuery()
            End Using

            ' Try USB connection first if available
            If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                ' Send command via USB
                Dim command As String = "RELAY ALL ON"
                Dim response As String = SendSerialCommand(command)

                ' Check for errors
                If response.StartsWith("ERROR") Then
                    ' Fall back to network
                    SendAllRelaysCommandViaNetwork(True)
                End If
            Else
                ' Use network connection
                SendAllRelaysCommandViaNetwork(True)
            End If

            ' Reload relay data
            LoadRelayData()
        Catch ex As Exception
            ' Handle exception
            Response.Write("Error turning all relays on: " & ex.Message)
        End Try
    End Sub

    ' Handle All OFF button click
    Protected Sub btnAllOff_Click(sender As Object, e As EventArgs)
        Try
            ' Update all relays in the database
            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                Dim cmd As New SqlCommand("UPDATE Relays SET State = 0", conn)
                cmd.ExecuteNonQuery()
            End Using

            ' Try USB connection first if available
            If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                ' Send command via USB
                Dim command As String = "RELAY ALL OFF"
                Dim response As String = SendSerialCommand(command)

                ' Check for errors
                If response.StartsWith("ERROR") Then
                    ' Fall back to network
                    SendAllRelaysCommandViaNetwork(False)
                End If
            Else
                ' Use network connection
                SendAllRelaysCommandViaNetwork(False)
            End If

            ' Reload relay data
            LoadRelayData()
        Catch ex As Exception
            ' Handle exception
            Response.Write("Error turning all relays off: " & ex.Message)
        End Try
    End Sub

    ' Send all relays command via network
    Private Sub SendAllRelaysCommandViaNetwork(state As Boolean)
        ' Send command to the device
        Dim url As String = String.Format(ApiBaseUrl, deviceIp) & "relay/all/" & (If(state, "on", "off"))

        Using client As New WebClient()
            client.DownloadString(url)
        End Using
    End Sub

    ' Toggle an individual relay
    <WebMethod>
    Public Shared Function ToggleRelay(relayId As Integer, state As Boolean) As Boolean
        Try
            ' Update relay state in the database
            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                Dim cmd As New SqlCommand("UPDATE Relays SET State = @State WHERE Id = @Id", conn)
                cmd.Parameters.AddWithValue("@State", state)
                cmd.Parameters.AddWithValue("@Id", relayId)
                cmd.ExecuteNonQuery()
            End Using

            ' Try USB connection first if available
            If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                ' Send command via USB
                Dim command As String = String.Format("RELAY {0} {1}", relayId + 1, If(state, "ON", "OFF"))
                Dim response As String = SendSerialCommand(command)

                ' Check for errors
                If response.StartsWith("ERROR") Then
                    ' Fall back to network
                    SendRelayCommandViaNetwork(relayId, state)
                End If
            Else
                ' Use network connection
                SendRelayCommandViaNetwork(relayId, state)
            End If

            Return True
        Catch ex As Exception
            ' Handle exception
            Return False
        End Try
    End Function

    ' Send relay command via network
    Private Shared Sub SendRelayCommandViaNetwork(relayId As Integer, state As Boolean)
        ' Get device IP from database
        Dim deviceIp As String
        Using conn As New SqlConnection(ConnectionString)
            conn.Open()

            Dim cmd As New SqlCommand("SELECT SettingValue FROM Settings WHERE SettingName = 'DeviceIP'", conn)
            deviceIp = cmd.ExecuteScalar().ToString()
        End Using

        ' Send command to the device
        Dim url As String = String.Format(ApiBaseUrl, deviceIp) & "relay/" & relayId & "/" & (If(state, "on", "off"))

        Using client As New WebClient()
            client.DownloadString(url)
        End Using
    End Sub

    ' Toggle a schedule's enabled state
    <WebMethod>
    Public Shared Function ToggleSchedule(scheduleId As Integer, enabled As Boolean) As Boolean
        Try
            ' Update schedule state in the database
            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                Dim cmd As New SqlCommand("UPDATE Schedules SET Enabled = @Enabled WHERE Id = @Id", conn)
                cmd.Parameters.AddWithValue("@Enabled", enabled)
                cmd.Parameters.AddWithValue("@Id", scheduleId)
                cmd.ExecuteNonQuery()
            End Using

            ' Try USB connection first if available
            If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                ' Get all schedules and send them via USB
                Dim schedules As String = GetSchedulesJson()
                Dim command As String = "CONFIG SET schedules " & schedules
                Dim response As String = SendSerialCommand(command)

                ' Save config
                SendSerialCommand("CONFIG SAVE")

                If Not response.StartsWith("ERROR") Then
                    Return True
                End If
            End If

            ' Fall back to network or if USB failed
            Dim deviceIp As String
            Using conn As New SqlConnection(ConnectionString)
                conn.Open()
                Dim cmd As New SqlCommand("SELECT SettingValue FROM Settings WHERE SettingName = 'DeviceIP'", conn)
                deviceIp = cmd.ExecuteScalar().ToString()
            End Using

            ' Send command to the device
            Dim url As String = String.Format(ApiBaseUrl, deviceIp) & "schedule/" & scheduleId & "/" & (If(enabled, "enable", "disable"))
            Using client As New WebClient()
                client.DownloadString(url)
            End Using

            Return True
        Catch ex As Exception
            ' Handle exception
            Return False
        End Try
    End Function

    ' Get schedules as JSON
    Private Shared Function GetSchedulesJson() As String
        Dim schedules As New List(Of Object)()

        Using conn As New SqlConnection(ConnectionString)
            conn.Open()
            Dim cmd As New SqlCommand("SELECT Id, Name, Time, Days, Action, Target, Enabled FROM Schedules", conn)
            Using reader As SqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    schedules.Add(New With {
                        .id = Convert.ToInt32(reader("Id")),
                        .name = reader("Name").ToString(),
                        .time = reader("Time").ToString(),
                        .days = reader("Days").ToString().Split(","c),
                        .action = reader("Action").ToString(),
                        .target = reader("Target").ToString(),
                        .enabled = Convert.ToBoolean(reader("Enabled"))
                    })
                End While
            End Using
        End Using

        Return JsonConvert.SerializeObject(schedules)
    End Function

    ' Toggle an automation rule's enabled state
    <WebMethod>
    Public Shared Function ToggleAutomation(automationId As Integer, enabled As Boolean) As Boolean
        Try
            ' Update automation state in the database
            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                Dim cmd As New SqlCommand("UPDATE Automation SET Enabled = @Enabled WHERE Id = @Id", conn)
                cmd.Parameters.AddWithValue("@Enabled", enabled)
                cmd.Parameters.AddWithValue("@Id", automationId)
                cmd.ExecuteNonQuery()
            End Using

            ' Try USB connection first if available
            If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                ' Get all automation rules and send them via USB
                Dim rulesJson As String = GetAutomationRulesJson()
                Dim command As String = "CONFIG SET automation " & rulesJson
                Dim response As String = SendSerialCommand(command)

                ' Save config
                SendSerialCommand("CONFIG SAVE")

                If Not response.StartsWith("ERROR") Then
                    Return True
                End If
            End If

            ' Fall back to network or if USB failed
            Dim deviceIp As String
            Using conn As New SqlConnection(ConnectionString)
                conn.Open()
                Dim cmd As New SqlCommand("SELECT SettingValue FROM Settings WHERE SettingName = 'DeviceIP'", conn)
                deviceIp = cmd.ExecuteScalar().ToString()
            End Using

            ' Send command to the device
            Dim url As String = String.Format(ApiBaseUrl, deviceIp) & "automation/" & automationId & "/" & (If(enabled, "enable", "disable"))
            Using client As New WebClient()
                client.DownloadString(url)
            End Using

            Return True
        Catch ex As Exception
            ' Handle exception
            Return False
        End Try
    End Function

    ' Get automation rules as JSON
    Private Shared Function GetAutomationRulesJson() As String
        Dim rules As New List(Of Object)()

        Using conn As New SqlConnection(ConnectionString)
            conn.Open()
            Dim cmd As New SqlCommand("SELECT Id, Name, Condition, LogicOperator, UseTimer, TimerType, TimerDuration, Action, Enabled FROM Automation", conn)
            Using reader As SqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim rule As New Dictionary(Of String, Object)()
                    rule.Add("id", Convert.ToInt32(reader("Id")))
                    rule.Add("name", reader("Name").ToString())
                    rule.Add("condition", reader("Condition").ToString())

                    If reader("LogicOperator") IsNot DBNull.Value Then
                        rule.Add("logicOperator", reader("LogicOperator").ToString())
                    Else
                        rule.Add("logicOperator", "AND")
                    End If

                    If reader("UseTimer") IsNot DBNull.Value Then
                        rule.Add("useTimer", Convert.ToBoolean(reader("UseTimer")))

                        If Convert.ToBoolean(reader("UseTimer")) Then
                            rule.Add("timerType", reader("TimerType").ToString())
                            rule.Add("timerDuration", Convert.ToInt32(reader("TimerDuration")))
                        End If
                    Else
                        rule.Add("useTimer", False)
                    End If

                    rule.Add("action", reader("Action").ToString())
                    rule.Add("enabled", Convert.ToBoolean(reader("Enabled")))

                    rules.Add(rule)
                End While
            End Using
        End Using

        Return JsonConvert.SerializeObject(rules)
    End Function

    ' Refresh data for UI updates
    <WebMethod>
    Public Shared Function RefreshData() As Object
        Try
            Dim result As New Dictionary(Of String, Object)()

            ' Try to get data via USB first if available
            If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                Dim usbData As Dictionary(Of String, Object) = GetDataViaUsb()
                If usbData IsNot Nothing Then
                    Return usbData
                End If
            End If

            ' Fall back to database/network if USB failed
            Return GetDataViaNetwork()
        Catch ex As Exception
            ' Handle exception
            Return New With {.Error = ex.Message}
        End Try
    End Function

    ' Get status data via USB
    Private Shared Function GetDataViaUsb() As Dictionary(Of String, Object)
        Try
            Dim result As New Dictionary(Of String, Object)()

            ' Get relay states
            Dim relayResponse As String = SendSerialCommand("RELAY STATUS")
            If relayResponse.StartsWith("ERROR") Then
                Return Nothing
            End If

            Dim relays As New List(Of Object)()

            ' Parse relay states from response
            ' Format: "RELAY STATUS: Relay 1 (Relay 1): ON ..."
            Dim relayLines As String() = relayResponse.Split(New String() {Environment.NewLine, "\n"}, StringSplitOptions.RemoveEmptyEntries)
            For i As Integer = 1 To Math.Min(relayLines.Length - 1, 8) ' Skip the first line which is the heading
                Dim line As String = relayLines(i)
                If line.Contains("):") Then
                    Dim namePart As String = line.Substring(line.IndexOf("(") + 1, line.IndexOf(")") - line.IndexOf("(") - 1)
                    Dim state As Boolean = line.Contains("ON")
                    Dim id As Integer = i - 1

                    relays.Add(New With {
                        .Id = id,
                        .Name = namePart,
                        .State = state
                    })
                End If
            Next

            result.Add("Relays", relays)

            ' Get input states
            Dim inputResponse As String = SendSerialCommand("INPUT STATUS")
            If inputResponse.StartsWith("ERROR") Then
                Return Nothing
            End If

            Dim inputs As New List(Of Object)()

            ' Parse input states from response
            ' Format: "INPUT STATUS: Input 1 (Input 1): HIGH ..."
            Dim inputLines As String() = inputResponse.Split(New String() {Environment.NewLine, "\n"}, StringSplitOptions.RemoveEmptyEntries)
            For i As Integer = 1 To Math.Min(inputLines.Length - 1, 8) ' Skip the first line which is the heading
                Dim line As String = inputLines(i)
                If line.Contains("):") Then
                    Dim namePart As String = line.Substring(line.IndexOf("(") + 1, line.IndexOf(")") - line.IndexOf("(") - 1)
                    Dim state As Boolean = line.Contains("HIGH")
                    Dim id As Integer = i - 1

                    inputs.Add(New With {
                        .Id = id,
                        .Name = namePart,
                        .State = state
                    })
                End If
            Next

            result.Add("Inputs", inputs)

            ' Get analog input values
            Dim analogResponse As String = SendSerialCommand("ANALOG STATUS")
            If analogResponse.StartsWith("ERROR") Then
                Return Nothing
            End If

            Dim analogInputs As New List(Of Object)()

            ' Parse analog values from response
            ' Format: "ANALOG STATUS: Analog 1 (Analog 1): 1234 ..."
            Dim analogLines As String() = analogResponse.Split(New String() {Environment.NewLine, "\n"}, StringSplitOptions.RemoveEmptyEntries)
            For i As Integer = 1 To Math.Min(analogLines.Length - 1, 4) ' Skip the first line which is the heading
                Dim line As String = analogLines(i)
                If line.Contains("):") Then
                    Dim namePart As String = line.Substring(line.IndexOf("(") + 1, line.IndexOf(")") - line.IndexOf("(") - 1)
                    Dim valuePart As String = line.Substring(line.IndexOf("):") + 3).Trim()
                    Dim value As Integer = Integer.Parse(valuePart.Split(" "c)(0))
                    Dim id As Integer = i - 1

                    analogInputs.Add(New With {
                        .Id = id,
                        .Name = namePart,
                        .Value = value
                    })
                End If
            Next

            result.Add("AnalogInputs", analogInputs)

            ' Get device status
            Dim statusResponse As String = SendSerialCommand("STATUS")
            If statusResponse.StartsWith("ERROR") Then
                Return Nothing
            End If

            ' Parse status from response
            ' Format: "Device Status: ..."
            Dim statusLines As String() = statusResponse.Split(New String() {Environment.NewLine, "\n"}, StringSplitOptions.RemoveEmptyEntries)
            Dim deviceIp As String = "Unknown"
            Dim connectionType As String = "USB"
            Dim currentTime As String = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")

            For Each line As String In statusLines
                If line.Contains("IP:") Then
                    deviceIp = line.Substring(line.IndexOf("IP:") + 3).Trim()
                End If
                If line.Contains("Connection:") Then
                    connectionType = line.Substring(line.IndexOf("Connection:") + 11).Trim()
                End If
                If line.Contains("Current Time:") Then
                    currentTime = line.Substring(line.IndexOf("Current Time:") + 13).Trim()
                End If
            Next

            result.Add("DeviceIP", deviceIp)
            result.Add("ConnectionType", connectionType)
            result.Add("CurrentTime", currentTime)
            result.Add("Connected", True)

            Return result
        Catch ex As Exception
            ' Return null to fall back to network
            Return Nothing
        End Try
    End Function

    ' Get status data via network
    Private Shared Function GetDataViaNetwork() As Dictionary(Of String, Object)
        Dim result As New Dictionary(Of String, Object)()

        ' Get relay states
        Dim relays As New List(Of Object)()
        Using conn As New SqlConnection(ConnectionString)
            conn.Open()

            Dim cmd As New SqlCommand("SELECT Id, Name, State FROM Relays ORDER BY Id", conn)
            Using reader As SqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    relays.Add(New With {
                    .Id = Convert.ToInt32(reader("Id")),
                    .Name = reader("Name").ToString(),
                    .State = Convert.ToBoolean(reader("State"))
                })
                End While
            End Using
        End Using
        result.Add("Relays", relays)

        ' Get input states
        Dim inputs As New List(Of Object)()
        Using conn As New SqlConnection(ConnectionString)
            conn.Open()

            Dim cmd As New SqlCommand("SELECT Id, Name, State FROM Inputs ORDER BY Id", conn)
            Using reader As SqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    inputs.Add(New With {
                    .Id = Convert.ToInt32(reader("Id")),
                    .Name = reader("Name").ToString(),
                    .State = Convert.ToBoolean(reader("State"))
                })
                End While
            End Using
        End Using
        result.Add("Inputs", inputs)

        ' Get analog input values
        Dim analogInputs As New List(Of Object)()
        Using conn As New SqlConnection(ConnectionString)
            conn.Open()

            Dim cmd As New SqlCommand("SELECT Id, Name, Value FROM AnalogInputs ORDER BY Id", conn)
            Using reader As SqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    analogInputs.Add(New With {
                    .Id = Convert.ToInt32(reader("Id")),
                    .Name = reader("Name").ToString(),
                    .Value = Convert.ToInt32(reader("Value"))
                })
                End While
            End Using
        End Using
        result.Add("AnalogInputs", analogInputs)

        ' Get device status
        Using conn As New SqlConnection(ConnectionString)
            conn.Open()

            Dim cmd As New SqlCommand("SELECT SettingValue FROM Settings WHERE SettingName = 'DeviceIP'", conn)
            Dim deviceIp As String = cmd.ExecuteScalar().ToString()

            cmd = New SqlCommand("SELECT SettingValue FROM Settings WHERE SettingName = 'ConnectionType'", conn)
            Dim connectionType As String = cmd.ExecuteScalar().ToString()

            result.Add("DeviceIP", deviceIp)
            result.Add("ConnectionType", connectionType)
            result.Add("CurrentTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))

            ' Check if device is connected
            Dim isConnected As Boolean = False
            Try
                Dim url As String = String.Format(ApiBaseUrl, deviceIp) & "status"
                Using client As New WebClient()
                    Dim response As String = client.DownloadString(url)
                    isConnected = True
                End Using
            Catch
                isConnected = False
            End Try
            result.Add("Connected", isConnected)
        End Using

        Return result
    End Function

    ' Save network settings
    Protected Sub btnSaveNetwork_Click(sender As Object, e As EventArgs)
        Try
            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                ' Update device IP
                Dim cmd As New SqlCommand("UPDATE Settings SET SettingValue = @Value WHERE SettingName = 'DeviceIP'", conn)
                cmd.Parameters.AddWithValue("@Value", txtDeviceIP.Text)
                cmd.ExecuteNonQuery()

                ' Update connection type
                cmd = New SqlCommand("UPDATE Settings SET SettingValue = @Value WHERE SettingName = 'ConnectionType'", conn)
                cmd.Parameters.AddWithValue("@Value", ddlConnectionType.SelectedValue)
                cmd.ExecuteNonQuery()

                ' Update WiFi SSID
                cmd = New SqlCommand("UPDATE Settings SET SettingValue = @Value WHERE SettingName = 'WifiSSID'", conn)
                cmd.Parameters.AddWithValue("@Value", txtWifiSSID.Text)
                cmd.ExecuteNonQuery()

                ' Update WiFi password if provided
                If Not String.IsNullOrEmpty(txtWifiPassword.Text) Then
                    cmd = New SqlCommand("UPDATE Settings SET SettingValue = @Value WHERE SettingName = 'WifiPassword'", conn)
                    cmd.Parameters.AddWithValue("@Value", txtWifiPassword.Text)
                    cmd.ExecuteNonQuery()
                End If

                ' Update device settings
                deviceIp = txtDeviceIP.Text

                ' Try to send settings via USB first
                If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                    Dim command As String = String.Format("CONFIG SET ssid {0}", txtWifiSSID.Text)
                    SendSerialCommand(command)

                    If Not String.IsNullOrEmpty(txtWifiPassword.Text) Then
                        command = String.Format("CONFIG SET password {0}", txtWifiPassword.Text)
                        SendSerialCommand(command)
                    End If

                    command = String.Format("CONFIG SET ethernet {0}", If(ddlConnectionType.SelectedValue = "ethernet", "enabled", "disabled"))
                    SendSerialCommand(command)

                    ' Save config
                    SendSerialCommand("CONFIG SAVE")
                Else
                    ' Fall back to network
                    SendNetworkSettingsViaNetwork()
                End If

                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "networkSaved", "alert('Network settings saved successfully!');", True)
            End Using
        Catch ex As Exception
            ' Handle exception
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "networkError", "alert('Error saving network settings: " & ex.Message.Replace("'", "\'") & "');", True)
        End Try
    End Sub

    ' Send network settings via network API
    Private Sub SendNetworkSettingsViaNetwork()
        ' Send settings to the device
        Dim url As String = String.Format(ApiBaseUrl, deviceIp) & "settings/network"
        Dim postData As String = JsonConvert.SerializeObject(New With {
            .ip = txtDeviceIP.Text,
            .connectionType = ddlConnectionType.SelectedValue,
            .ssid = txtWifiSSID.Text,
            .password = If(String.IsNullOrEmpty(txtWifiPassword.Text), Nothing, txtWifiPassword.Text)
        })

        Using client As New WebClient()
            client.Headers(HttpRequestHeader.ContentType) = "application/json"
            client.UploadString(url, "POST", postData)
        End Using
    End Sub

    ' Save device settings
    Protected Sub btnSaveDevice_Click(sender As Object, e As EventArgs)
        Try
            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                ' Update device name
                Dim cmd As New SqlCommand("UPDATE Settings SET SettingValue = @Value WHERE SettingName = 'DeviceName'", conn)
                cmd.Parameters.AddWithValue("@Value", txtDeviceName.Text)
                cmd.ExecuteNonQuery()

                ' Update polling interval
                cmd = New SqlCommand("UPDATE Settings SET SettingValue = @Value WHERE SettingName = 'PollingInterval'", conn)
                cmd.Parameters.AddWithValue("@Value", txtPollingInterval.Text)
                cmd.ExecuteNonQuery()

                ' Try to send settings via USB first
                If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                    Dim command As String = String.Format("CONFIG SET devicename {0}", txtDeviceName.Text)
                    SendSerialCommand(command)

                    ' Handle time settings
                    If rbManualTime.Checked AndAlso Not String.IsNullOrEmpty(txtManualDateTime.Text) Then
                        command = String.Format("SYSTEM TIME SET {0}", txtManualDateTime.Text)
                        SendSerialCommand(command)
                    End If

                    ' Save config
                    SendSerialCommand("CONFIG SAVE")
                Else
                    ' Fall back to network
                    SendDeviceSettingsViaNetwork()
                End If

                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "deviceSaved", "alert('Device settings saved successfully!');", True)
            End Using
        Catch ex As Exception
            ' Handle exception
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "deviceError", "alert('Error saving device settings: " & ex.Message.Replace("'", "\'") & "');", True)
        End Try
    End Sub

    ' Send device settings via network API
    Private Sub SendDeviceSettingsViaNetwork()
        ' Send settings to the device
        Dim url As String = String.Format(ApiBaseUrl, deviceIp) & "settings/device"
        Dim postData As String = JsonConvert.SerializeObject(New With {
            .name = txtDeviceName.Text,
            .pollingInterval = Convert.ToInt32(txtPollingInterval.Text),
            .autoTime = rbAutoTime.Checked,
            .manualTime = If(rbManualTime.Checked, txtManualDateTime.Text, Nothing)
        })

        Using client As New WebClient()
            client.Headers(HttpRequestHeader.ContentType) = "application/json"
            client.UploadString(url, "POST", postData)
        End Using
    End Sub

    ' Reboot device
    Protected Sub btnRebootDevice_Click(sender As Object, e As EventArgs)
        Try
            ' Try to send reboot command via USB first
            If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                Dim response As String = SendSerialCommand("SYSTEM REBOOT")

                If Not response.StartsWith("ERROR") Then
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "rebootSuccess", "alert('Device reboot command sent successfully via USB!');", True)
                    Return
                End If
            End If

            ' Fall back to network
            Dim url As String = String.Format(ApiBaseUrl, deviceIp) & "reboot"

            Using client As New WebClient()
                client.DownloadString(url)
            End Using

            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "rebootSuccess", "alert('Device reboot command sent successfully via network!');", True)
        Catch ex As Exception
            ' Handle exception
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "rebootError", "alert('Error rebooting device: " & ex.Message.Replace("'", "\'") & "');", True)
        End Try
    End Sub

    ' Save I/O configuration
    Protected Sub btnSaveIOConfig_Click(sender As Object, e As EventArgs)
        Try
            ' Process relay configuration
            For Each item As RepeaterItem In rptRelayConfig.Items
                Dim relayId As Integer = Convert.ToInt32(CType(item.FindControl("txtRelayName"), TextBox).Attributes("data-relay-id"))
                Dim relayName As String = CType(item.FindControl("txtRelayName"), TextBox).Text
                Dim invertLogic As Boolean = CType(item.FindControl("chkInvertLogic"), CheckBox).Checked
                Dim rememberState As Boolean = CType(item.FindControl("chkRememberState"), CheckBox).Checked

                ' Update in database
                Using conn As New SqlConnection(ConnectionString)
                    conn.Open()

                    Dim cmd As New SqlCommand("UPDATE Relays SET Name = @Name, InvertLogic = @InvertLogic, RememberState = @RememberState WHERE Id = @Id", conn)
                    cmd.Parameters.AddWithValue("@Name", relayName)
                    cmd.Parameters.AddWithValue("@InvertLogic", invertLogic)
                    cmd.Parameters.AddWithValue("@RememberState", rememberState)
                    cmd.Parameters.AddWithValue("@Id", relayId)
                    cmd.ExecuteNonQuery()
                End Using
            Next

            ' Process input configuration
            For Each item As RepeaterItem In rptInputConfig.Items
                Dim inputId As Integer = Convert.ToInt32(CType(item.FindControl("txtInputName"), TextBox).Attributes("data-input-id"))
                Dim inputName As String = CType(item.FindControl("txtInputName"), TextBox).Text
                Dim invertLogic As Boolean = CType(item.FindControl("chkInputInvert"), CheckBox).Checked
                Dim mode As String = CType(item.FindControl("ddlInputMode"), DropDownList).SelectedValue

                ' Update in database
                Using conn As New SqlConnection(ConnectionString)
                    conn.Open()

                    Dim cmd As New SqlCommand("UPDATE Inputs SET Name = @Name, InvertLogic = @InvertLogic, Mode = @Mode WHERE Id = @Id", conn)
                    cmd.Parameters.AddWithValue("@Name", inputName)
                    cmd.Parameters.AddWithValue("@InvertLogic", invertLogic)
                    cmd.Parameters.AddWithValue("@Mode", mode)
                    cmd.Parameters.AddWithValue("@Id", inputId)
                    cmd.ExecuteNonQuery()
                End Using
            Next

            ' Process analog input configuration
            For Each item As RepeaterItem In rptAnalogConfig.Items
                Dim analogId As Integer = Convert.ToInt32(CType(item.FindControl("txtAnalogName"), TextBox).Attributes("data-analog-id"))
                Dim analogName As String = CType(item.FindControl("txtAnalogName"), TextBox).Text
                Dim mode As String = CType(item.FindControl("ddlAnalogMode"), DropDownList).SelectedValue
                Dim unit As String = CType(item.FindControl("txtAnalogUnit"), TextBox).Text

                ' Update in database
                Using conn As New SqlConnection(ConnectionString)
                    conn.Open()

                    Dim cmd As New SqlCommand("UPDATE AnalogInputs SET Name = @Name, Mode = @Mode, Unit = @Unit WHERE Id = @Id", conn)
                    cmd.Parameters.AddWithValue("@Name", analogName)
                    cmd.Parameters.AddWithValue("@Mode", mode)
                    cmd.Parameters.AddWithValue("@Unit", unit)
                    cmd.Parameters.AddWithValue("@Id", analogId)
                    cmd.ExecuteNonQuery()
                End Using
            Next

            ' Try to send configuration via USB first
            If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                Dim ioConfig As String = GetIOConfigJson()
                Dim command As String = "CONFIG SET io " & ioConfig
                Dim response As String = SendSerialCommand(command)

                ' Save config
                SendSerialCommand("CONFIG SAVE")

                If Not response.StartsWith("ERROR") Then
                    ' Reload data
                    LoadRelayData()
                    LoadInputData()
                    LoadAnalogInputData()

                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "ioConfigSaved", "alert('I/O configuration saved successfully via USB!');", True)
                    Return
                End If
            End If

            ' Fall back to network
            SendIOConfigToDeviceViaNetwork()

            ' Reload data
            LoadRelayData()
            LoadInputData()
            LoadAnalogInputData()

            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "ioConfigSaved", "alert('I/O configuration saved successfully via network!');", True)
        Catch ex As Exception
            ' Handle exception
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "ioConfigError", "alert('Error saving I/O configuration: " & ex.Message.Replace("'", "\'") & "');", True)
        End Try
    End Sub

    ' Get I/O configuration as JSON
    Private Function GetIOConfigJson() As String
        ' Prepare relay configuration
        Dim relayConfig As New List(Of Object)()
        Using conn As New SqlConnection(ConnectionString)
            conn.Open()

            Dim cmd As New SqlCommand("SELECT Id, Name, InvertLogic, RememberState FROM Relays ORDER BY Id", conn)
            Using reader As SqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    relayConfig.Add(New With {
                        .id = Convert.ToInt32(reader("Id")),
                        .name = reader("Name").ToString(),
                        .invertLogic = Convert.ToBoolean(reader("InvertLogic")),
                        .rememberState = Convert.ToBoolean(reader("RememberState"))
                    })
                End While
            End Using
        End Using

        ' Prepare input configuration
        Dim inputConfig As New List(Of Object)()
        Using conn As New SqlConnection(ConnectionString)
            conn.Open()

            Dim cmd As New SqlCommand("SELECT Id, Name, InvertLogic, Mode FROM Inputs ORDER BY Id", conn)
            Using reader As SqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    inputConfig.Add(New With {
                        .id = Convert.ToInt32(reader("Id")),
                        .name = reader("Name").ToString(),
                        .invertLogic = Convert.ToBoolean(reader("InvertLogic")),
                        .mode = reader("Mode").ToString()
                    })
                End While
            End Using
        End Using

        ' Prepare analog input configuration
        Dim analogConfig As New List(Of Object)()
        Using conn As New SqlConnection(ConnectionString)
            conn.Open()

            Dim cmd As New SqlCommand("SELECT Id, Name, Mode, Unit FROM AnalogInputs ORDER BY Id", conn)
            Using reader As SqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    analogConfig.Add(New With {
                        .id = Convert.ToInt32(reader("Id")),
                        .name = reader("Name").ToString(),
                        .mode = reader("Mode").ToString(),
                        .unit = reader("Unit").ToString()
                    })
                End While
            End Using
        End Using

        ' Create JSON object
        Dim config As New Dictionary(Of String, Object)()
        config.Add("relays", relayConfig)
        config.Add("inputs", inputConfig)
        config.Add("analogInputs", analogConfig)

        Return JsonConvert.SerializeObject(config)
    End Function

    ' Send I/O configuration to the device
    Private Sub SendIOConfigToDeviceViaNetwork()
        Try
            ' Prepare relay configuration
            Dim relayConfig As New List(Of Object)()
            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                Dim cmd As New SqlCommand("SELECT Id, Name, InvertLogic, RememberState FROM Relays ORDER BY Id", conn)
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        relayConfig.Add(New With {
                            .id = Convert.ToInt32(reader("Id")),
                            .name = reader("Name").ToString(),
                            .invertLogic = Convert.ToBoolean(reader("InvertLogic")),
                            .rememberState = Convert.ToBoolean(reader("RememberState"))
                        })
                    End While
                End Using
            End Using

            ' Prepare input configuration
            Dim inputConfig As New List(Of Object)()
            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                Dim cmd As New SqlCommand("SELECT Id, Name, InvertLogic, Mode FROM Inputs ORDER BY Id", conn)
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        inputConfig.Add(New With {
                            .id = Convert.ToInt32(reader("Id")),
                            .name = reader("Name").ToString(),
                            .invertLogic = Convert.ToBoolean(reader("InvertLogic")),
                            .mode = reader("Mode").ToString()
                        })
                    End While
                End Using
            End Using

            ' Prepare analog input configuration
            Dim analogConfig As New List(Of Object)()
            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                Dim cmd As New SqlCommand("SELECT Id, Name, Mode, Unit FROM AnalogInputs ORDER BY Id", conn)
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        analogConfig.Add(New With {
                            .id = Convert.ToInt32(reader("Id")),
                            .name = reader("Name").ToString(),
                            .mode = reader("Mode").ToString(),
                            .unit = reader("Unit").ToString()
                        })
                    End While
                End Using
            End Using

            ' Send configuration to the device
            Dim url As String = String.Format(ApiBaseUrl, deviceIp) & "settings/io"
            Dim postData As String = JsonConvert.SerializeObject(New With {
                .relays = relayConfig,
                .inputs = inputConfig,
                .analogInputs = analogConfig
            })

            Using client As New WebClient()
                client.Headers(HttpRequestHeader.ContentType) = "application/json"
                client.UploadString(url, "POST", postData)
            End Using
        Catch ex As Exception
            ' Handle exception
            Throw New Exception("Error sending I/O configuration to device: " & ex.Message)
        End Try
    End Sub

    ' Save Alexa settings
    Protected Sub btnSaveAlexa_Click(sender As Object, e As EventArgs)
        Try
            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                ' Update Alexa enabled status
                Dim cmd As New SqlCommand("UPDATE Settings SET SettingValue = @Value WHERE SettingName = 'AlexaEnabled'", conn)
                cmd.Parameters.AddWithValue("@Value", chkEnableAlexa.Checked)
                cmd.ExecuteNonQuery()

                ' Update Alexa device name
                cmd = New SqlCommand("UPDATE Settings SET SettingValue = @Value WHERE SettingName = 'AlexaDeviceName'", conn)
                cmd.Parameters.AddWithValue("@Value", txtAlexaDeviceName.Text)
                cmd.ExecuteNonQuery()

                ' Update Alexa device configuration
                For Each item As RepeaterItem In rptAlexaDevices.Items
                    Dim deviceId As Integer = Convert.ToInt32(CType(item.FindControl("chkAlexaDevice"), CheckBox).Attributes("data-device-id"))
                    Dim alexaEnabled As Boolean = CType(item.FindControl("chkAlexaDevice"), CheckBox).Checked

                    cmd = New SqlCommand("UPDATE AlexaDevices SET AlexaEnabled = @AlexaEnabled WHERE Id = @Id", conn)
                    cmd.Parameters.AddWithValue("@AlexaEnabled", alexaEnabled)
                    cmd.Parameters.AddWithValue("@Id", deviceId)
                    cmd.ExecuteNonQuery()
                Next

                ' Try to send settings via USB first
                If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                    Dim command As String = String.Format("CONFIG SET alexa {0}", If(chkEnableAlexa.Checked, "enabled", "disabled"))
                    SendSerialCommand(command)

                    command = String.Format("CONFIG SET alexaname {0}", txtAlexaDeviceName.Text)
                    SendSerialCommand(command)

                    ' Save config
                    SendSerialCommand("CONFIG SAVE")
                Else
                    ' Fall back to network
                    SendAlexaSettingsToDeviceViaNetwork()
                End If

                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alexaSaved", "alert('Alexa settings saved successfully!');", True)
            End Using
        Catch ex As Exception
            ' Handle exception
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alexaError", "alert('Error saving Alexa settings: " & ex.Message.Replace("'", "\'") & "');", True)
        End Try
    End Sub

    ' Send Alexa settings to device via network
    Private Sub SendAlexaSettingsToDeviceViaNetwork()
        Try
            ' Get Alexa-enabled devices
            Dim alexaDevices As New List(Of Object)()
            Using conn As New SqlConnection(ConnectionString)
                conn.Open()
                Dim cmd As New SqlCommand("SELECT Id, Name, Type FROM AlexaDevices WHERE AlexaEnabled = 1", conn)
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        alexaDevices.Add(New With {
                            .id = Convert.ToInt32(reader("Id")),
                            .name = reader("Name").ToString(),
                            .type = reader("Type").ToString()
                        })
                    End While
                End Using
            End Using

            ' Send settings to the device
            Dim url As String = String.Format(ApiBaseUrl, deviceIp) & "settings/alexa"
            Dim postData As String = JsonConvert.SerializeObject(New With {
                .enabled = chkEnableAlexa.Checked,
                .deviceName = txtAlexaDeviceName.Text,
                .devices = alexaDevices
            })

            Using client As New WebClient()
                client.Headers(HttpRequestHeader.ContentType) = "application/json"
                client.UploadString(url, "POST", postData)
            End Using
        Catch ex As Exception
            ' Handle exception
            Throw New Exception("Error sending Alexa settings to device: " & ex.Message)
        End Try
    End Sub

    ' Test Alexa device discovery
    Protected Sub btnDiscoverDevices_Click(sender As Object, e As EventArgs)
        Try
            ' Try to send discovery command via USB first
            If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                Dim response As String = SendSerialCommand("ALEXA DISCOVER")

                If Not response.StartsWith("ERROR") Then
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "discoverSuccess", "alert('Alexa discovery test initiated successfully via USB!');", True)
                    Return
                End If
            End If

            ' Fall back to network
            Dim url As String = String.Format(ApiBaseUrl, deviceIp) & "alexa/discover"

            Using client As New WebClient()
                client.DownloadString(url)
            End Using

            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "discoverSuccess", "alert('Alexa discovery test initiated successfully via network!');", True)
        Catch ex As Exception
            ' Handle exception
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "discoverError", "alert('Error testing Alexa discovery: " & ex.Message.Replace("'", "\'") & "');", True)
        End Try
    End Sub

    ' Handler for action type dropdown change in schedule modal
    Protected Sub ddlActionType_SelectedIndexChanged(sender As Object, e As EventArgs)
        ' Show/hide appropriate panels based on selected action type
        pnlRelayAction.Visible = (ddlActionType.SelectedValue = "relay")
        pnlSceneAction.Visible = (ddlActionType.SelectedValue = "scene")
        pnlNotificationAction.Visible = (ddlActionType.SelectedValue = "notification")
    End Sub

    ' Handler for trigger type dropdown change in automation modal
    Protected Sub ddlConditionType_SelectedIndexChanged(sender As Object, e As EventArgs)
        ' Get the condition index from the sender control
        Dim ddl As DropDownList = DirectCast(sender, DropDownList)
        Dim conditionIndex As Integer = Convert.ToInt32(ddl.Attributes("data-condition-index"))

        ' Find the corresponding panels
        Dim digitalPanel As Panel = DirectCast(FindControlRecursive(UpdatePanelAutomationModal, "pnlDigitalCondition" & conditionIndex), Panel)
        Dim analogPanel As Panel = DirectCast(FindControlRecursive(UpdatePanelAutomationModal, "pnlAnalogCondition" & conditionIndex), Panel)

        ' Show/hide panels based on condition type
        If ddl.SelectedValue = "digital" Then
            digitalPanel.Visible = True
            analogPanel.Visible = False
        Else
            digitalPanel.Visible = False
            analogPanel.Visible = True
        End If
    End Sub

    ' Helper method to find controls recursively
    Private Function FindControlRecursive(root As Control, id As String) As Control
        If root.ID = id Then
            Return root
        End If

        For Each c As Control In root.Controls
            Dim foundControl As Control = FindControlRecursive(c, id)
            If foundControl IsNot Nothing Then
                Return foundControl
            End If
        Next

        Return Nothing
    End Function

    ' Handler for automation action dropdown change in automation modal
    Protected Sub ddlAutomationAction_SelectedIndexChanged(sender As Object, e As EventArgs)
        ' Show/hide appropriate panels based on selected action type
        pnlAutomationRelayAction.Visible = (ddlAutomationAction.SelectedValue = "relay")
        pnlAutomationSceneAction.Visible = (ddlAutomationAction.SelectedValue = "scene")
        pnlAutomationNotificationAction.Visible = (ddlAutomationAction.SelectedValue = "notification")
    End Sub

    ' Save schedule
    Protected Sub btnSaveSchedule_Click(sender As Object, e As EventArgs)
        Try
            ' Collect days
            Dim days As New List(Of String)()
            If chkMonday.Checked Then days.Add("Mon")
            If chkTuesday.Checked Then days.Add("Tue")
            If chkWednesday.Checked Then days.Add("Wed")
            If chkThursday.Checked Then days.Add("Thu")
            If chkFriday.Checked Then days.Add("Fri")
            If chkSaturday.Checked Then days.Add("Sat")
            If chkSunday.Checked Then days.Add("Sun")

            ' Build action and target based on action type
            Dim action As String = ddlActionType.SelectedValue
            Dim target As String = ""

            Select Case action
                Case "relay"
                    target = ddlRelayTarget.SelectedValue & ":" & ddlRelayState.SelectedValue
                Case "scene"
                    target = ddlSceneTarget.SelectedValue
                Case "notification"
                    target = txtNotificationMessage.Text
            End Select

            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                If String.IsNullOrEmpty(hdnScheduleId.Value) Then
                    ' Insert new schedule
                    Dim cmd As New SqlCommand("INSERT INTO Schedules (Name, Time, Days, Action, Target, Enabled) VALUES (@Name, @Time, @Days, @Action, @Target, @Enabled)", conn)
                    cmd.Parameters.AddWithValue("@Name", txtScheduleName.Text)
                    cmd.Parameters.AddWithValue("@Time", DateTime.Parse(txtScheduleTime.Text).ToString("HH:mm"))
                    cmd.Parameters.AddWithValue("@Days", String.Join(",", days))
                    cmd.Parameters.AddWithValue("@Action", action)
                    cmd.Parameters.AddWithValue("@Target", target)
                    cmd.Parameters.AddWithValue("@Enabled", chkScheduleEnabled.Checked)
                    cmd.ExecuteNonQuery()
                Else
                    ' Update existing schedule
                    Dim cmd As New SqlCommand("UPDATE Schedules SET Name = @Name, Time = @Time, Days = @Days, Action = @Action, Target = @Target, Enabled = @Enabled WHERE Id = @Id", conn)
                    cmd.Parameters.AddWithValue("@Name", txtScheduleName.Text)
                    cmd.Parameters.AddWithValue("@Time", DateTime.Parse(txtScheduleTime.Text).ToString("HH:mm"))
                    cmd.Parameters.AddWithValue("@Days", String.Join(",", days))
                    cmd.Parameters.AddWithValue("@Action", action)
                    cmd.Parameters.AddWithValue("@Target", target)
                    cmd.Parameters.AddWithValue("@Enabled", chkScheduleEnabled.Checked)
                    cmd.Parameters.AddWithValue("@Id", Convert.ToInt32(hdnScheduleId.Value))
                    cmd.ExecuteNonQuery()
                End If
            End Using

            ' Try to send schedules via USB first
            If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                Dim schedules As String = GetSchedulesJson()
                Dim command As String = "CONFIG SET schedules " & schedules
                Dim response As String = SendSerialCommand(command)

                ' Save config
                SendSerialCommand("CONFIG SAVE")

                If Not response.StartsWith("ERROR") Then
                    ' Reload schedules
                    LoadSchedules()

                    ' Close the modal
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "closeScheduleModal", "$('#scheduleModal').modal('hide');", True)
                    Return
                End If
            End If

            ' Fall back to network
            SendSchedulesToDeviceViaNetwork()

            ' Reload schedules
            LoadSchedules()

            ' Close the modal
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "closeScheduleModal", "$('#scheduleModal').modal('hide');", True)
        Catch ex As Exception
            ' Handle exception
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "scheduleError", "alert('Error saving schedule: " & ex.Message.Replace("'", "\'") & "');", True)
        End Try
    End Sub

    ' Send schedules to the device via network
    Private Sub SendSchedulesToDeviceViaNetwork()
        Try
            ' Get all schedules from the database
            Dim schedules As New List(Of Object)()

            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                Dim cmd As New SqlCommand("SELECT Id, Name, Time, Days, Action, Target, Enabled FROM Schedules", conn)
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        schedules.Add(New With {
                            .id = Convert.ToInt32(reader("Id")),
                            .name = reader("Name").ToString(),
                            .time = reader("Time").ToString(),
                            .days = reader("Days").ToString().Split(","c),
                            .action = reader("Action").ToString(),
                            .target = reader("Target").ToString(),
                            .enabled = Convert.ToBoolean(reader("Enabled"))
                        })
                    End While
                End Using
            End Using

            ' Send to the device
            Dim url As String = String.Format(ApiBaseUrl, deviceIp) & "schedules"
            Dim postData As String = JsonConvert.SerializeObject(schedules)

            Using client As New WebClient()
                client.Headers(HttpRequestHeader.ContentType) = "application/json"
                client.UploadString(url, "POST", postData)
            End Using
        Catch ex As Exception
            ' Handle exception
            Throw New Exception("Error sending schedules to device: " & ex.Message)
        End Try
    End Sub

    ' Save automation rule
    Protected Sub btnSaveAutomation_Click(sender As Object, e As EventArgs)
        Try
            ' Collect conditions - this will be a JSON array of condition objects
            Dim conditions As New JArray()

            ' Find all condition groups
            Dim conditionGroups As New List(Of Control)()
            FindControlsByClass(UpdatePanelAutomationModal, "condition-group", conditionGroups)

            For Each group As Control In conditionGroups
                Dim conditionIndex As Integer = 0
                Dim conditionType As String = ""

                ' Find the condition type dropdown
                Dim ddlTypes As New List(Of Control)()
                FindControlsByClass(group, "condition-type", ddlTypes)
                If ddlTypes.Count > 0 Then
                    Dim ddlType As DropDownList = DirectCast(ddlTypes(0), DropDownList)
                    conditionType = ddlType.SelectedValue
                    conditionIndex = Convert.ToInt32(ddlType.Attributes("data-condition-index"))
                End If

                If conditionType = "digital" Then
                    ' Process digital input condition
                    Dim ddlInputs As New List(Of Control)()
                    Dim ddlStates As New List(Of Control)()
                    FindControlsByClass(group, "digital-input", ddlInputs)
                    FindControlsByClass(group, "digital-state", ddlStates)

                    If ddlInputs.Count > 0 And ddlStates.Count > 0 Then
                        Dim ddlInput As DropDownList = DirectCast(ddlInputs(0), DropDownList)
                        Dim ddlState As DropDownList = DirectCast(ddlStates(0), DropDownList)

                        Dim condition As New JObject()
                        condition("type") = "digital"
                        condition("sourceId") = Convert.ToInt32(ddlInput.SelectedValue)
                        condition("condition") = ddlState.SelectedValue

                        conditions.Add(condition)
                    End If
                ElseIf conditionType = "analog" Then
                    ' Process analog input condition
                    Dim ddlInputs As New List(Of Control)()
                    Dim ddlConditions As New List(Of Control)()
                    Dim txtThreshold1s As New List(Of Control)()
                    Dim txtThreshold2s As New List(Of Control)()

                    FindControlsByClass(group, "analog-input", ddlInputs)
                    FindControlsByClass(group, "analog-condition-type", ddlConditions)
                    FindControlsByClass(group, "analog-threshold1", txtThreshold1s)
                    FindControlsByClass(group, "analog-threshold2", txtThreshold2s)

                    If ddlInputs.Count > 0 And ddlConditions.Count > 0 And txtThreshold1s.Count > 0 Then
                        Dim ddlInput As DropDownList = DirectCast(ddlInputs(0), DropDownList)
                        Dim ddlCondition As DropDownList = DirectCast(ddlConditions(0), DropDownList)
                        Dim txtThreshold1 As TextBox = DirectCast(txtThreshold1s(0), TextBox)

                        Dim condition As New JObject()
                        condition("type") = "analog"
                        condition("sourceId") = Convert.ToInt32(ddlInput.SelectedValue)
                        condition("condition") = ddlCondition.SelectedValue
                        condition("threshold1") = Convert.ToInt32(txtThreshold1.Text)

                        If ddlCondition.SelectedValue = "between" And txtThreshold2s.Count > 0 Then
                            Dim txtThreshold2 As TextBox = DirectCast(txtThreshold2s(0), TextBox)
                            condition("threshold2") = Convert.ToInt32(txtThreshold2.Text)
                        End If

                        conditions.Add(condition)
                    End If
                End If
            Next

            ' Logic operator between conditions
            Dim logicOperator As String = "AND" ' Default
            If conditions.Count > 1 Then
                logicOperator = ddlLogicOperator.SelectedValue
            End If

            ' Timer settings
            Dim useTimer As Boolean = chkUseTimer.Checked
            Dim timerType As String = If(useTimer, ddlTimerType.SelectedValue, "ondelay")
            Dim timerDuration As Integer = If(useTimer And Not String.IsNullOrEmpty(txtTimerDuration.Text), Convert.ToInt32(txtTimerDuration.Text), 1000)

            ' Build action based on action type
            Dim action As String = ""

            Select Case ddlAutomationAction.SelectedValue
                Case "relay"
                    action = "relay:" & ddlAutoRelayTarget.SelectedValue & ":" & ddlAutoRelayState.SelectedValue
                Case "scene"
                    action = "scene:" & ddlAutoSceneTarget.SelectedValue
                Case "notification"
                    action = "notification:" & txtAutoNotificationMessage.Text
            End Select

            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                If String.IsNullOrEmpty(hdnAutomationId.Value) Then
                    ' Insert new automation rule
                    Dim cmd As New SqlCommand("INSERT INTO Automation (Name, Condition, LogicOperator, UseTimer, TimerType, TimerDuration, Action, Enabled) VALUES (@Name, @Condition, @LogicOperator, @UseTimer, @TimerType, @TimerDuration, @Action, @Enabled)", conn)
                    cmd.Parameters.AddWithValue("@Name", txtAutomationName.Text)
                    cmd.Parameters.AddWithValue("@Condition", conditions.ToString())
                    cmd.Parameters.AddWithValue("@LogicOperator", logicOperator)
                    cmd.Parameters.AddWithValue("@UseTimer", useTimer)
                    cmd.Parameters.AddWithValue("@TimerType", timerType)
                    cmd.Parameters.AddWithValue("@TimerDuration", timerDuration)
                    cmd.Parameters.AddWithValue("@Action", action)
                    cmd.Parameters.AddWithValue("@Enabled", chkAutomationEnabled.Checked)
                    cmd.ExecuteNonQuery()
                Else
                    ' Update existing automation rule
                    Dim cmd As New SqlCommand("UPDATE Automation SET Name = @Name, Condition = @Condition, LogicOperator = @LogicOperator, UseTimer = @UseTimer, TimerType = @TimerType, TimerDuration = @TimerDuration, Action = @Action, Enabled = @Enabled WHERE Id = @Id", conn)
                    cmd.Parameters.AddWithValue("@Name", txtAutomationName.Text)
                    cmd.Parameters.AddWithValue("@Condition", conditions.ToString())
                    cmd.Parameters.AddWithValue("@LogicOperator", logicOperator)
                    cmd.Parameters.AddWithValue("@UseTimer", useTimer)
                    cmd.Parameters.AddWithValue("@TimerType", timerType)
                    cmd.Parameters.AddWithValue("@TimerDuration", timerDuration)
                    cmd.Parameters.AddWithValue("@Action", action)
                    cmd.Parameters.AddWithValue("@Enabled", chkAutomationEnabled.Checked)
                    cmd.Parameters.AddWithValue("@Id", Convert.ToInt32(hdnAutomationId.Value))
                    cmd.ExecuteNonQuery()
                End If
            End Using

            ' Try to send automation rules via USB first
            If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                Dim rules As String = GetAutomationRulesJson()
                Dim command As String = "CONFIG SET automation " & rules
                Dim response As String = SendSerialCommand(command)

                ' Save config
                SendSerialCommand("CONFIG SAVE")

                If Not response.StartsWith("ERROR") Then
                    ' Reload automation rules
                    LoadAutomationRules()

                    ' Close the modal
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "closeAutomationModal", "$('#automationModal').modal('hide');", True)
                    Return
                End If
            End If

            ' Fall back to network
            SendAutomationRulesToDeviceViaNetwork()

            ' Reload automation rules
            LoadAutomationRules()

            ' Close the modal
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "closeAutomationModal", "$('#automationModal').modal('hide');", True)
        Catch ex As Exception
            ' Handle exception
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "automationError", "alert('Error saving automation rule: " & ex.Message.Replace("'", "\'") & "');", True)
        End Try
    End Sub

    ' Helper method to find controls by CSS class
    Private Sub FindControlsByClass(root As Control, className As String, foundControls As List(Of Control))
        For Each ctrl As Control In root.Controls
            If TypeOf ctrl Is WebControl Then
                Dim webCtrl As WebControl = DirectCast(ctrl, WebControl)
                If Not String.IsNullOrEmpty(webCtrl.CssClass) AndAlso webCtrl.CssClass.Contains(className) Then
                    foundControls.Add(webCtrl)
                End If
            End If

            FindControlsByClass(ctrl, className, foundControls)
        Next
    End Sub

    ' Send automation rules to the device via network
    Private Sub SendAutomationRulesToDeviceViaNetwork()
        Try
            ' Get all automation rules from the database
            Dim rules As New List(Of Object)()

            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                Dim cmd As New SqlCommand("SELECT Id, Name, Condition, LogicOperator, UseTimer, TimerType, TimerDuration, Action, Enabled FROM Automation", conn)
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim rule As New Dictionary(Of String, Object)()
                        rule.Add("id", Convert.ToInt32(reader("Id")))
                        rule.Add("name", reader("Name").ToString())
                        rule.Add("condition", reader("Condition").ToString())

                        If reader("LogicOperator") IsNot DBNull.Value Then
                            rule.Add("logicOperator", reader("LogicOperator").ToString())
                        Else
                            rule.Add("logicOperator", "AND")
                        End If

                        If reader("UseTimer") IsNot DBNull.Value Then
                            rule.Add("useTimer", Convert.ToBoolean(reader("UseTimer")))

                            If Convert.ToBoolean(reader("UseTimer")) Then
                                rule.Add("timerType", reader("TimerType").ToString())
                                rule.Add("timerDuration", Convert.ToInt32(reader("TimerDuration")))
                            End If
                        Else
                            rule.Add("useTimer", False)
                        End If

                        rule.Add("action", reader("Action").ToString())
                        rule.Add("enabled", Convert.ToBoolean(reader("Enabled")))

                        rules.Add(rule)
                    End While
                End Using
            End Using

            ' Send to the device
            Dim url As String = String.Format(ApiBaseUrl, deviceIp) & "automation"
            Dim postData As String = JsonConvert.SerializeObject(rules)

            Using client As New WebClient()
                client.Headers(HttpRequestHeader.ContentType) = "application/json"
                client.UploadString(url, "POST", postData)
            End Using
        Catch ex As Exception
            ' Handle exception
            Throw New Exception("Error sending automation rules to device: " & ex.Message)
        End Try
    End Sub

    ' Handle GridView row commands for schedules
    Protected Sub gvSchedules_RowCommand(sender As Object, e As GridViewCommandEventArgs)
        Try
            Dim index As Integer = Convert.ToInt32(e.CommandArgument)
            Dim scheduleId As Integer = Convert.ToInt32(gvSchedules.DataKeys(index).Value)

            Select Case e.CommandName
                Case "EditSchedule"
                    ' Load schedule data into the modal for editing
                    Using conn As New SqlConnection(ConnectionString)
                        conn.Open()

                        Dim cmd As New SqlCommand("SELECT * FROM Schedules WHERE Id = @Id", conn)
                        cmd.Parameters.AddWithValue("@Id", scheduleId)

                        Using reader As SqlDataReader = cmd.ExecuteReader()
                            If reader.Read() Then
                                hdnScheduleId.Value = scheduleId.ToString()
                                txtScheduleName.Text = reader("Name").ToString()
                                txtScheduleTime.Text = DateTime.Parse(reader("Time").ToString()).ToString("HH:mm")

                                ' Set days
                                Dim days As String() = reader("Days").ToString().Split(","c)
                                chkMonday.Checked = days.Contains("Mon")
                                chkTuesday.Checked = days.Contains("Tue")
                                chkWednesday.Checked = days.Contains("Wed")
                                chkThursday.Checked = days.Contains("Thu")
                                chkFriday.Checked = days.Contains("Fri")
                                chkSaturday.Checked = days.Contains("Sat")
                                chkSunday.Checked = days.Contains("Sun")

                                ' Set action type and details
                                Dim action As String = reader("Action").ToString()
                                Dim target As String = reader("Target").ToString()

                                ddlActionType.SelectedValue = action

                                Select Case action
                                    Case "relay"
                                        Dim parts As String() = target.Split(":"c)
                                        ddlRelayTarget.SelectedValue = parts(0)
                                        ddlRelayState.SelectedValue = parts(1)
                                        pnlRelayAction.Visible = True
                                        pnlSceneAction.Visible = False
                                        pnlNotificationAction.Visible = False
                                    Case "scene"
                                        ddlSceneTarget.SelectedValue = target
                                        pnlRelayAction.Visible = False
                                        pnlSceneAction.Visible = True
                                        pnlNotificationAction.Visible = False
                                    Case "notification"
                                        txtNotificationMessage.Text = target
                                        pnlRelayAction.Visible = False
                                        pnlSceneAction.Visible = False
                                        pnlNotificationAction.Visible = True
                                End Select

                                chkScheduleEnabled.Checked = Convert.ToBoolean(reader("Enabled"))
                            End If
                        End Using
                    End Using

                    ' Show the modal
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "showScheduleModal", "$('#scheduleModal').modal('show');", True)

                Case "DeleteSchedule"
                    ' Delete the schedule
                    Using conn As New SqlConnection(ConnectionString)
                        conn.Open()

                        Dim cmd As New SqlCommand("DELETE FROM Schedules WHERE Id = @Id", conn)
                        cmd.Parameters.AddWithValue("@Id", scheduleId)
                        cmd.ExecuteNonQuery()
                    End Using

                    ' Try to send updated schedules via USB first
                    If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                        Dim schedules As String = GetSchedulesJson()
                        Dim command As String = "CONFIG SET schedules " & schedules
                        Dim response As String = SendSerialCommand(command)

                        ' Save config
                        SendSerialCommand("CONFIG SAVE")

                        If Not response.StartsWith("ERROR") Then
                            ' Reload schedules
                            LoadSchedules()
                            Return
                        End If
                    End If

                    ' Fall back to network
                    SendSchedulesToDeviceViaNetwork()

                    ' Reload schedules
                    LoadSchedules()
            End Select
        Catch ex As Exception
            ' Handle exception
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "rowCommandError", "alert('Error processing command: " & ex.Message.Replace("'", "\'") & "');", True)
        End Try
    End Sub

    ' Handle GridView row commands for automation rules
    Protected Sub gvAutomation_RowCommand(sender As Object, e As GridViewCommandEventArgs)
        Try
            Dim index As Integer = Convert.ToInt32(e.CommandArgument)
            Dim automationId As Integer = Convert.ToInt32(gvAutomation.DataKeys(index).Value)

            Select Case e.CommandName
                Case "EditAutomation"
                    ' Use client-side function to load and show the modal
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "showEditAutomationDialog", "showEditAutomationDialog(" & automationId & ");", True)

                Case "DeleteAutomation"
                    ' Delete the automation rule
                    Using conn As New SqlConnection(ConnectionString)
                        conn.Open()

                        Dim cmd As New SqlCommand("DELETE FROM Automation WHERE Id = @Id", conn)
                        cmd.Parameters.AddWithValue("@Id", automationId)
                        cmd.ExecuteNonQuery()
                    End Using

                    ' Try to send updated automation rules via USB first
                    If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                        Dim rulesJson As String = GetAutomationRulesJson()
                        Dim command As String = "CONFIG SET automation " & rulesJson
                        Dim response As String = SendSerialCommand(command)

                        ' Save config
                        SendSerialCommand("CONFIG SAVE")

                        If Not response.StartsWith("ERROR") Then
                            ' Reload automation rules
                            LoadAutomationRules()
                            Return
                        End If
                    End If

                    ' Fall back to network
                    SendAutomationRulesToDeviceViaNetwork()

                    ' Reload automation rules
                    LoadAutomationRules()
            End Select
        Catch ex As Exception
            ' Handle exception
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "rowCommandError", "alert('Error processing command: " & ex.Message.Replace("'", "\'") & "');", True)
        End Try
    End Sub

    ' Get automation rule details for editing
    <WebMethod>
    Public Shared Function GetAutomationRule(ruleId As Integer) As String
        Try
            Dim rule As New Dictionary(Of String, Object)()

            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                Dim cmd As New SqlCommand("SELECT * FROM Automation WHERE Id = @Id", conn)
                cmd.Parameters.AddWithValue("@Id", ruleId)

                Using reader As SqlDataReader = cmd.ExecuteReader()
                    If reader.Read() Then
                        rule.Add("id", Convert.ToInt32(reader("Id")))
                        rule.Add("name", reader("Name").ToString())
                        rule.Add("enabled", Convert.ToBoolean(reader("Enabled")))

                        ' Parse condition string to extract condition details
                        Dim conditionStr As String = reader("Condition").ToString()
                        Dim conditions As New List(Of Object)()

                        ' Check if it's a JSON array of conditions or legacy format
                        If conditionStr.StartsWith("[") Then
                            ' Parse JSON array of conditions
                            conditions = JsonConvert.DeserializeObject(Of List(Of Object))(conditionStr)
                        Else
                            ' Legacy format - parse a single condition
                            ' Format: "input:1:high" or "analog:2:gt:1000"
                            Dim condParts As String() = conditionStr.Split(":"c)

                            Dim condition As New Dictionary(Of String, Object)()
                            condition.Add("type", condParts(0))
                            condition.Add("sourceId", Convert.ToInt32(condParts(1)))
                            condition.Add("condition", condParts(2))

                            If condParts(0) = "analog" Then
                                condition.Add("threshold1", Convert.ToInt32(condParts(3)))

                                If condParts(2) = "between" And condParts.Length > 4 Then
                                    condition.Add("threshold2", Convert.ToInt32(condParts(4)))
                                End If
                            End If

                            conditions.Add(condition)
                        End If

                        rule.Add("conditions", conditions)

                        ' Logic operator
                        If reader("LogicOperator") IsNot DBNull.Value Then
                            rule.Add("logicOperator", reader("LogicOperator").ToString())
                        Else
                            rule.Add("logicOperator", "AND") ' Default
                        End If

                        ' Timer settings
                        If reader("UseTimer") IsNot DBNull.Value Then
                            rule.Add("useTimer", Convert.ToBoolean(reader("UseTimer")))

                            If Convert.ToBoolean(reader("UseTimer")) Then
                                rule.Add("timerType", reader("TimerType").ToString())
                                rule.Add("timerDuration", Convert.ToInt32(reader("TimerDuration")))
                            End If
                        Else
                            rule.Add("useTimer", False)
                        End If

                        ' Parse action string to extract action details
                        ' Format: "relay:1:on" or "notification:Alert message"
                        Dim actionStr As String = reader("Action").ToString()
                        Dim actionParts As String() = actionStr.Split(":"c)

                        rule.Add("action", actionParts(0))

                        If actionParts(0) = "relay" Then
                            rule.Add("targetId", Convert.ToInt32(actionParts(1)))
                            rule.Add("targetState", actionParts(2))
                        ElseIf actionParts(0) = "scene" Then
                            rule.Add("targetId", Convert.ToInt32(actionParts(1)))
                        ElseIf actionParts(0) = "notification" Then
                            rule.Add("message", actionStr.Substring(actionStr.IndexOf(":") + 1))
                        End If
                    End If
                End Using
            End Using

            Return JsonConvert.SerializeObject(rule)
        Catch ex As Exception
            Return "{""Error"": """ & ex.Message.Replace("""", "\""") & """}"
        End Try
    End Function

    ' Save automation rule from AJAX call
    <WebMethod>
    Public Shared Function SaveAutomationRule(ruleData As String) As Boolean
        Try
            Dim rule As Dictionary(Of String, Object) = JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(ruleData)

            ' Format conditions for storage
            Dim conditions As JArray = JArray.FromObject(rule("conditions"))
            Dim conditionStr As String = conditions.ToString()

            ' Format action for storage
            Dim actionStr As String = ""

            If rule("action").ToString() = "relay" Then
                actionStr = String.Format("relay:{0}:{1}", rule("targetId"), rule("targetState"))
            ElseIf rule("action").ToString() = "scene" Then
                actionStr = String.Format("scene:{0}", rule("targetId"))
            ElseIf rule("action").ToString() = "notification" Then
                actionStr = String.Format("notification:{0}", rule("message"))
            End If

            Using conn As New SqlConnection(ConnectionString)
                conn.Open()

                If rule("id") IsNot Nothing AndAlso Convert.ToInt32(rule("id")) > 0 Then
                    ' Update existing rule
                    Dim cmd As New SqlCommand("UPDATE Automation SET Name = @Name, Condition = @Condition, LogicOperator = @LogicOperator, " &
                                             "UseTimer = @UseTimer, TimerType = @TimerType, TimerDuration = @TimerDuration, " &
                                             "Action = @Action, Enabled = @Enabled WHERE Id = @Id", conn)
                    cmd.Parameters.AddWithValue("@Id", Convert.ToInt32(rule("id")))
                    cmd.Parameters.AddWithValue("@Name", rule("name").ToString())
                    cmd.Parameters.AddWithValue("@Condition", conditionStr)
                    cmd.Parameters.AddWithValue("@LogicOperator", rule("logicOperator").ToString())
                    cmd.Parameters.AddWithValue("@UseTimer", Convert.ToBoolean(rule("useTimer")))
                    cmd.Parameters.AddWithValue("@TimerType", If(rule.ContainsKey("timerType"), rule("timerType").ToString(), "ondelay"))
                    cmd.Parameters.AddWithValue("@TimerDuration", If(rule.ContainsKey("timerDuration"), Convert.ToInt32(rule("timerDuration")), 1000))
                    cmd.Parameters.AddWithValue("@Action", actionStr)
                    cmd.Parameters.AddWithValue("@Enabled", Convert.ToBoolean(rule("enabled")))
                    cmd.ExecuteNonQuery()
                Else
                    ' Insert new rule
                    Dim cmd As New SqlCommand("INSERT INTO Automation (Name, Condition, LogicOperator, UseTimer, TimerType, TimerDuration, Action, Enabled) " &
                                             "VALUES (@Name, @Condition, @LogicOperator, @UseTimer, @TimerType, @TimerDuration, @Action, @Enabled)", conn)
                    cmd.Parameters.AddWithValue("@Name", rule("name").ToString())
                    cmd.Parameters.AddWithValue("@Condition", conditionStr)
                    cmd.Parameters.AddWithValue("@LogicOperator", rule("logicOperator").ToString())
                    cmd.Parameters.AddWithValue("@UseTimer", Convert.ToBoolean(rule("useTimer")))
                    cmd.Parameters.AddWithValue("@TimerType", If(rule.ContainsKey("timerType"), rule("timerType").ToString(), "ondelay"))
                    cmd.Parameters.AddWithValue("@TimerDuration", If(rule.ContainsKey("timerDuration"), Convert.ToInt32(rule("timerDuration")), 1000))
                    cmd.Parameters.AddWithValue("@Action", actionStr)
                    cmd.Parameters.AddWithValue("@Enabled", Convert.ToBoolean(rule("enabled")))
                    cmd.ExecuteNonQuery()
                End If
            End Using

            ' Try to send updated automation rules via USB first
            If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                Dim rulesJson As String = GetAutomationRulesJson()
                Dim command As String = "CONFIG SET automation " & rulesJson
                Dim response As String = SendSerialCommand(command)

                ' Save config
                SendSerialCommand("CONFIG SAVE")

                If Not response.StartsWith("ERROR") Then
                    Return True
                End If
            End If

            ' Fall back to network
            Dim deviceIp As String
            Using conn As New SqlConnection(ConnectionString)
                conn.Open()
                Dim cmd As New SqlCommand("SELECT SettingValue FROM Settings WHERE SettingName = 'DeviceIP'", conn)
                deviceIp = cmd.ExecuteScalar().ToString()
            End Using

            ' Get all automation rules from the database
            Dim allRules As New List(Of Object)()
            Using conn As New SqlConnection(ConnectionString)
                conn.Open()
                Dim cmd As New SqlCommand("SELECT Id, Name, Condition, LogicOperator, UseTimer, TimerType, TimerDuration, Action, Enabled FROM Automation", conn)
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim ruleObj As New Dictionary(Of String, Object)()
                        ruleObj.Add("id", Convert.ToInt32(reader("Id")))
                        ruleObj.Add("name", reader("Name").ToString())
                        ruleObj.Add("condition", reader("Condition").ToString())

                        If reader("LogicOperator") IsNot DBNull.Value Then
                            ruleObj.Add("logicOperator", reader("LogicOperator").ToString())
                        Else
                            ruleObj.Add("logicOperator", "AND")
                        End If

                        If reader("UseTimer") IsNot DBNull.Value Then
                            ruleObj.Add("useTimer", Convert.ToBoolean(reader("UseTimer")))

                            If Convert.ToBoolean(reader("UseTimer")) Then
                                ruleObj.Add("timerType", reader("TimerType").ToString())
                                ruleObj.Add("timerDuration", Convert.ToInt32(reader("TimerDuration")))
                            End If
                        Else
                            ruleObj.Add("useTimer", False)
                        End If

                        ruleObj.Add("action", reader("Action").ToString())
                        ruleObj.Add("enabled", Convert.ToBoolean(reader("Enabled")))

                        allRules.Add(ruleObj)
                    End While
                End Using
            End Using

            ' Send to the device
            Dim url As String = String.Format(ApiBaseUrl, deviceIp) & "automation"
            Dim postData As String = JsonConvert.SerializeObject(allRules)

            Using client As New WebClient()
                client.Headers(HttpRequestHeader.ContentType) = "application/json"
                client.UploadString(url, "POST", postData)
            End Using

            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function
End Class