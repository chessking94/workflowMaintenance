Imports System.Data
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient

Partial Public Class ScheduleWindow
    Private scheduleID As Integer

    Public Sub New(Optional pi_scheduleID As Integer = 0)
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        scheduleID = pi_scheduleID
    End Sub

    Private Sub LoadRecord() Handles Me.Loaded
        Using command As New SqlCommand
            command.Connection = MainWindow.db_Connection
            command.CommandType = CommandType.Text
            command.CommandText = modQueries.ColumnLengths()
            command.Parameters.AddWithValue("@schemaName", "dbo")
            command.Parameters.AddWithValue("@tableName", "Schedules")

            With command.ExecuteReader
                While .Read
                    Select Case .Item("column_name").ToString
                        Case "scheduleName"
                            tb_Name.MaxLength = Convert.ToInt32(.Item("max_length"))
                    End Select
                End While
                .Close()
            End With

            'run time
            Dim list_RunTimes As New List(Of String)
            Dim iterableTime As DateTime = DateTime.Parse("12:00 AM")
            While iterableTime < DateTime.Parse("11:59 PM")
                list_RunTimes.Add(iterableTime.ToString("hh:mm tt"))
                iterableTime = iterableTime.AddMinutes(1)
            End While
            list_RunTimes.Add(iterableTime.ToString("hh:mm tt"))  'need to add 11:59 PM manually
            combo_RunTime.ItemsSource = list_RunTimes

            'recurrence name - I have no idea why I have to use .Item here instead of .GetString, app errors out otherwise
            Dim list_recurName As New List(Of String) From {""}
            command.Parameters.Clear()
            command.CommandText = modQueries.Recurrences()
            With command.ExecuteReader
                While .Read
                    list_recurName.Add(.Item("recurrenceName").ToString)
                End While
                .Close()
            End With
            list_recurName.Sort()
            combo_recurrenceName.ItemsSource = list_recurName

            'recurrence interval
            Dim list_recurInterval As New List(Of String) From {""}
            Dim i As Integer = 1
            While i < 60
                list_recurInterval.Add(i.ToString)
                i += 1
            End While
            combo_recurrenceInterval.ItemsSource = list_recurInterval

            If scheduleID = 0 Then
                tb_ID.Text = "(new)"
            Else
                command.Parameters.Clear()
                command.CommandText = modQueries.Schedules()
                command.Parameters.AddWithValue("@scheduleID", scheduleID)

                With command.ExecuteReader
                    While .Read
                        tb_ID.Text = .Item("ID").ToString
                        tb_Name.Text = .Item("Name")
                        cb_Active.IsChecked = (Convert.ToBoolean(.Item("Active")) = True)
                        dp_StartDate.SelectedDate = Convert.ToDateTime(.Item("Start_Date"))
                        If Not .IsDBNull(.GetOrdinal("End_Date")) Then
                            dp_EndDate.SelectedDate = Convert.ToDateTime(.Item("End_Date"))
                        End If
                        'dp_EndDate.SelectedDate = If(.IsDBNull(.GetOrdinal("End_Date")), Nothing, Convert.ToDateTime(.Item("End_Date")))  'I do not understand why I can't use this instead, populates 01/01/0001 as the date instead of reading the Nothing
                        combo_RunTime.SelectedValue = If(.IsDBNull(.GetOrdinal("Run_Time")), "", Convert.ToDateTime(.Item("Run_Time")).ToString("hh:mm tt"))
                        combo_recurrenceName.SelectedValue = If(.IsDBNull(.GetOrdinal("Recurrence_Name")), "", .Item("Recurrence_Name"))
                        combo_recurrenceInterval.SelectedValue = If(.IsDBNull(.GetOrdinal("Recurrence_Interval")), "", .Item("Recurrence_Interval").ToString)
                    End While
                    .Close()
                End With
            End If
        End Using
    End Sub

    Private Sub WindowClosed() Handles Me.Closed
        'when the window closes, refresh the original DataGrid
        Dim mainWindow As MainWindow = CType(Application.Current.MainWindow, MainWindow)
        mainWindow.RefreshSchedules()
    End Sub

    Private Sub RecurrenceNameChanged() Handles combo_recurrenceName.SelectionChanged
        Dim list_recurInterval As New List(Of String) From {""}
        Dim bypassIteration As Boolean = False
        Dim maxIterations As Integer = 1

        Select Case combo_recurrenceName.SelectedValue
            Case "One-Time"
                combo_recurrenceInterval.ItemsSource = list_recurInterval
                bypassIteration = True
            Case "Minutely"
                maxIterations = 59
            Case "Hourly"
                maxIterations = 23
            Case "Daily"
                maxIterations = 6
            Case "Weekly"
                maxIterations = 4
            Case "Monthly"
                maxIterations = 12
            Case "Yearly"
                maxIterations = 1
            Case Else
                maxIterations = 59
        End Select

        If Not bypassIteration Then
            Dim i As Integer = 1
            While i <= maxIterations
                list_recurInterval.Add(i.ToString)
                i += 1
            End While
            combo_recurrenceInterval.ItemsSource = list_recurInterval
        End If
    End Sub

    Private Sub SaveSchedule() Handles btn_SaveSchedule.Click
        Dim validationFailReason As String = ""

        'cleanse data
        tb_Name.Text = tb_Name.Text.Trim()

        'validate data
        If validationFailReason = "" Then
            If String.IsNullOrWhiteSpace(tb_Name.Text) Then
                validationFailReason = "Invalid name"
            End If
        End If

        If validationFailReason = "" Then
            If dp_StartDate.SelectedDate Is Nothing Then
                validationFailReason = "Start date not selected"
            End If
        End If

        If validationFailReason = "" Then
            If combo_RunTime.SelectedValue Is Nothing Then
                validationFailReason = "Run time not selected"
            End If
        End If

        If validationFailReason = "" Then
            If dp_EndDate.SelectedDate IsNot Nothing AndAlso dp_EndDate.SelectedDate < Date.Today Then
                validationFailReason = "End date is in the past"
            End If
        End If

        If validationFailReason = "" Then
            If dp_EndDate.SelectedDate IsNot Nothing AndAlso (dp_EndDate.SelectedDate < dp_StartDate.SelectedDate) Then
                validationFailReason = "End date is before start date"
            End If
        End If

        If validationFailReason = "" Then
            If combo_recurrenceName.SelectedValue <> "" AndAlso (combo_recurrenceInterval.SelectedValue = "" OrElse combo_recurrenceInterval.SelectedValue Is Nothing) Then
                validationFailReason = "Recurrence selected with no interval"
            End If
        End If

        'perform create/update
        If validationFailReason <> "" Then
            MessageBox.Show($"Pre-validation failed: {validationFailReason}", "Result", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Else
            Using command As New SqlCommand
                command.Connection = MainWindow.db_Connection
                command.CommandType = CommandType.StoredProcedure
                command.Parameters.AddWithValue("@scheduleName", tb_Name.Text)
                command.Parameters.AddWithValue("@scheduleActive", If(cb_Active.IsChecked, 1, 0))
                command.Parameters.AddWithValue("@scheduleStartDate", dp_StartDate.SelectedDate)
                command.Parameters.AddWithValue("@scheduleEndDate", If(dp_EndDate.SelectedDate Is Nothing, DBNull.Value, dp_EndDate.SelectedDate))
                command.Parameters.AddWithValue("@scheduleRunTime", If(combo_RunTime.SelectedValue = "", DBNull.Value, combo_RunTime.SelectedValue))
                command.Parameters.AddWithValue("@recurrenceName", If(combo_recurrenceName.SelectedValue = "", DBNull.Value, combo_recurrenceName.SelectedValue))
                command.Parameters.AddWithValue("@recurrenceInterval", If(combo_recurrenceInterval.SelectedValue = "", DBNull.Value, combo_recurrenceInterval.SelectedValue))

                If scheduleID = 0 Then
                    'new schedule
                    command.CommandText = "Workflow.dbo.createSchedule"

                    Dim rtnval As New SqlParameter("@ReturnValue", SqlDbType.Int)
                    rtnval.Direction = ParameterDirection.ReturnValue
                    command.Parameters.Add(rtnval)

                    command.ExecuteNonQuery()

                    scheduleID = Convert.ToInt32(rtnval.Value)
                    If scheduleID > 0 Then
                        MessageBox.Show($"Schedule {scheduleID} successfully created", "Result", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Me.Close()
                    Else
                        'something failed, Select Case the causes returned
                        Select Case scheduleID
                        'TODO: highlight the bad fields?
                            Case -1
                                MessageBox.Show("Unable to create schedule, missing name", "Result", MessageBoxButtons.OK, MessageBoxIcon.Error)
                            Case -2
                                MessageBox.Show("Unable to create schedule, recurrence does not exist", "Result", MessageBoxButtons.OK, MessageBoxIcon.Error)
                            Case -3
                                MessageBox.Show("Unable to create schedule, invalid recurrence interval", "Result", MessageBoxButtons.OK, MessageBoxIcon.Error)
                            Case Else
                                MessageBox.Show("Unable to create schedule, unknown error", "Result", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        End Select
                    End If
                Else
                    'updating an existing schedule
                    command.CommandText = "Workflow.dbo.updateSchedule"
                    command.Parameters.AddWithValue("@scheduleID", scheduleID)

                    Dim rtnval As New SqlParameter("@ReturnValue", SqlDbType.Int)
                    rtnval.Direction = ParameterDirection.ReturnValue
                    command.Parameters.Add(rtnval)

                    command.ExecuteNonQuery()

                    Dim result As Integer = Convert.ToInt32(rtnval.Value)
                    Select Case result
                        Case 0
                            MessageBox.Show("Update successful", "Result", MessageBoxButtons.OK, MessageBoxIcon.Information)
                            Me.Close()
                        Case 1
                            MessageBox.Show("Update failed, nothing updated", "Result", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Case Else
                            MessageBox.Show("Update failed, unknown error", "Result", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Select
                End If
            End Using
        End If
    End Sub
End Class
