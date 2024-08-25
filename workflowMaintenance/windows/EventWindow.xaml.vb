Imports Microsoft.Data.SqlClient
Imports System.Data

Partial Public Class EventWindow
    Private actionRequiresParameters As New Dictionary(Of String, Boolean)
    Private runNowTime As String = "ASAP"  'choice for combo_StartTime to indicate an event should be run right away

    Public Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
    End Sub

    Private Sub LoadWindow() Handles Me.Loaded
        Using command As New SqlCommand
            actionRequiresParameters.Clear()

            command.Connection = MainWindow.db_Connection
            command.CommandType = Data.CommandType.Text
            command.CommandText = modQueries.ColumnLengths()
            command.Parameters.AddWithValue("@schemaName", "dbo")
            command.Parameters.AddWithValue("@tableName", "Events")

            With command.ExecuteReader
                While .Read
                    Select Case .GetString("column_name")
                        Case "eventParameters"
                            tb_EventParameters.MaxLength = .GetInt32("max_length")
                    End Select
                End While
                .Close()
            End With

            'populate item source for ActionName
            Dim list_actions As New List(Of String)
            command.CommandText = modQueries.Actions()
            command.Parameters.Clear()
            command.Parameters.AddWithValue("@actionID", -1)
            With command.ExecuteReader
                While .Read
                    If .GetBoolean("Active") Then list_actions.Add(.GetString("Name"))
                    actionRequiresParameters.Add(.GetString("Name"), .GetBoolean("Require_Parameters"))
                End While
                .Close()
            End With
            list_actions.Sort()
            combo_ActionName.ItemsSource = list_actions
        End Using

        'populate item source for StartTime
        'TODO: Do I want these values to come from a database table?
        Dim list_StartTimes As New List(Of String) From {runNowTime}  'initialize with the "run now" option, since it is not a time
        Dim iterableTime As DateTime = DateTime.Parse("12:00 AM")
        While iterableTime < DateTime.Parse("11:55 PM")
            list_StartTimes.Add(iterableTime.ToString("hh:mm tt"))
            iterableTime = iterableTime.AddMinutes(5)
        End While
        list_StartTimes.Add(iterableTime.ToString("hh:mm tt"))  'need to add 11:55 PM manually
        combo_StartTime.ItemsSource = list_StartTimes

        dp_StartDate.IsEnabled = False  'open window with this disabled
    End Sub

    Private Sub StartTimeChanged() Handles combo_StartTime.SelectionChanged
        If combo_StartTime.SelectedValue <> runNowTime Then
            dp_StartDate.IsEnabled = True  'not running immediately, allow choosing a date
        Else
            dp_StartDate.IsEnabled = False  'reset this if needed
        End If
    End Sub

    Private Sub WindowClosed() Handles Me.Closed
        'when the window closes, refresh the original DataGrid
        Dim mainWindow As MainWindow = CType(Application.Current.MainWindow, MainWindow)
        mainWindow.RefreshEvents()
    End Sub

    Private Sub SaveEvent() Handles btn_SaveEvent.Click
        Dim validationFailReason As String = ""

        'cleanse data
        tb_EventParameters.Text = tb_EventParameters.Text.Trim()

        'validation
        If validationFailReason = "" Then
            If combo_ActionName.SelectedValue = Nothing Then
                validationFailReason = "No action selected"
            End If
        End If

        If validationFailReason = "" Then
            If actionRequiresParameters(combo_ActionName.SelectedValue) AndAlso String.IsNullOrWhiteSpace(tb_EventParameters.Text) Then
                validationFailReason = "Action requires parameters"
            End If
        End If

        If validationFailReason = "" Then
            If combo_StartTime.SelectedValue Is Nothing Then
                validationFailReason = "No time selected"
            End If
        End If

        If validationFailReason = "" Then
            If combo_StartTime.SelectedValue IsNot Nothing AndAlso combo_StartTime.SelectedValue <> runNowTime AndAlso dp_StartDate.SelectedDate Is Nothing Then
                validationFailReason = "No date selected"
            End If
        End If

        If validationFailReason <> "" Then
            MessageBox.Show($"Pre-validation failed: {validationFailReason}", "Result", MessageBoxButton.OK, MessageBoxImage.Error)
        Else
            Using command As New SqlCommand
                command.Connection = MainWindow.db_Connection
                command.CommandType = Data.CommandType.StoredProcedure
                command.Parameters.AddWithValue("@actionName", combo_ActionName.SelectedValue)
                command.Parameters.AddWithValue("@eventParameters", If(String.IsNullOrWhiteSpace(tb_EventParameters.Text), DBNull.Value, tb_EventParameters.Text))
                If combo_StartTime.SelectedValue <> runNowTime Then
                    Dim dtetme As DateTime = DateTime.Parse(dp_StartDate.SelectedDate.Value.ToString("MM/dd/yyyy") & " " & combo_StartTime.SelectedValue)
                    command.Parameters.AddWithValue("@eventStartDate", dtetme)
                End If

                command.CommandText = "dbo.createEvent"

                Dim rtnval As New SqlParameter("@ReturnValue", SqlDbType.Int)
                rtnval.Direction = ParameterDirection.ReturnValue
                command.Parameters.Add(rtnval)

                command.ExecuteNonQuery()

                Dim eventID As Integer = Convert.ToInt32(rtnval.Value)
                If eventID > 0 Then
                    MessageBox.Show($"Event {eventID} successfully created", "Result", MessageBoxButton.OK, MessageBoxImage.Information)
                    Me.Close()
                Else
                    'something failed, Select Case the causes returned
                    Select Case eventID
                    'TODO: highlight the bad fields?
                        Case -1
                            MessageBox.Show("Unable to create event, invalid workflow/step number combination", "Result", MessageBoxButton.OK, MessageBoxImage.Error)
                        Case -2
                            MessageBox.Show("Unable to create event, actionName parameter not a key or action name", "Result", MessageBoxButton.OK, MessageBoxImage.Error)
                        Case Else
                            MessageBox.Show("Unable to create event, unknown error", "Result", MessageBoxButton.OK, MessageBoxImage.Error)
                    End Select
                End If
            End Using
        End If
    End Sub
End Class
