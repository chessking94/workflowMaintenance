Imports Microsoft.Data.SqlClient
Imports System.Data

Partial Public Class ActionWindow
    Private actionID As Integer

    Public Sub New(Optional pi_actionID As Integer = 0)
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        actionID = pi_actionID
    End Sub

    Private Sub LoadRecord() Handles Me.Loaded
        Using command As New SqlCommand
            command.Connection = MainWindow.db_Connection
            command.CommandType = Data.CommandType.Text
            command.CommandText = modQueries.ColumnLengths()
            command.Parameters.AddWithValue("@schemaName", "dbo")
            command.Parameters.AddWithValue("@tableName", "Actions")

            With command.ExecuteReader
                While .Read
                    Select Case .GetString("column_name")
                        Case "actionName"
                            tb_Name.MaxLength = .GetInt32("max_length")
                        Case "actionDescription"
                            tb_Description.MaxLength = .GetInt32("max_length")
                    End Select
                End While
                .Close()
            End With

            Dim combobox_items As New List(Of String) From {""}  'initialize with an empty string
            command.Parameters.Clear()
            command.CommandText = modQueries.Applications()
            command.Parameters.AddWithValue("@applicationID", -1)
            With command.ExecuteReader
                While .Read
                    combobox_items.Add(.Item("ID"))
                End While
                .Close()
            End With
            combo_ApplicationID.ItemsSource = combobox_items

            If actionID = 0 Then
                tb_ID.Text = "(new)"
            Else
                command.Parameters.Clear()
                command.CommandText = modQueries.Actions()
                command.Parameters.AddWithValue("@actionID", actionID)

                With command.ExecuteReader
                    While .Read
                        tb_ID.Text = .GetInt32("ID").ToString
                        tb_Name.Text = .GetString("Name")
                        tb_Description.Text = .GetString("Description")
                        cb_Active.IsChecked = (.GetBoolean("Active") = True)
                        cb_RequireParameters.IsChecked = (.GetBoolean("Require_Parameters") = True)
                        tb_Concurrency.Text = .GetByte("Concurrency")
                        cb_LogOutput.IsChecked = (.GetBoolean("Log_Output") = True)
                        combo_ApplicationID.SelectedValue = If(.IsDBNull(.GetOrdinal("Application_ID")), "", .GetInt32("Application_ID").ToString)
                    End While
                    .Close()
                End With
            End If
        End Using
    End Sub

    Private Sub WindowClosed() Handles Me.Closed
        'when the window closes, refresh the original DataGrid
        Dim mainWindow As MainWindow = CType(Application.Current.MainWindow, MainWindow)
        mainWindow.RefreshActions()
    End Sub

    Private Sub SaveApplication() Handles btn_SaveAction.Click
        Dim validationFailReason As String = ""

        'cleanse data
        tb_Name.Text = tb_Name.Text.Trim()
        tb_Description.Text = tb_Description.Text.Trim()
        tb_Concurrency.Text = tb_Concurrency.Text.Trim()

        'validate data
        If validationFailReason = "" Then
            If Not IsNumeric(tb_Concurrency.Text) Then
                validationFailReason = $"'{tb_Concurrency.Text}' is not an integer"
            End If
        End If

        'perform create/update
        If validationFailReason <> "" Then
            MessageBox.Show($"Pre-validation failed: {validationFailReason}", "Result", MessageBoxButton.OK, MessageBoxImage.Error)
        Else
            Using command As New SqlCommand
                command.Connection = MainWindow.db_Connection
                command.CommandType = Data.CommandType.StoredProcedure
                command.Parameters.AddWithValue("@actionName", tb_Name.Text)
                command.Parameters.AddWithValue("@actionDescription", tb_Description.Text)
                command.Parameters.AddWithValue("@actionActive", If(cb_Active.IsChecked, 1, 0))
                command.Parameters.AddWithValue("@actionRequireParameters", If(cb_RequireParameters.IsChecked, 1, 0))
                command.Parameters.AddWithValue("@actionConcurrency", tb_Concurrency.Text)
                command.Parameters.AddWithValue("@actionLogOutput", If(cb_LogOutput.IsChecked, 1, 0))
                command.Parameters.AddWithValue("@applicationID", If(combo_ApplicationID.SelectedValue = "", DBNull.Value, Convert.ToInt32(combo_ApplicationID.SelectedValue)))

                If actionID = 0 Then
                    'new action
                    command.CommandText = "dbo.createAction"

                    Dim rtnval As New SqlParameter("@ReturnValue", SqlDbType.Int)
                    rtnval.Direction = ParameterDirection.ReturnValue
                    command.Parameters.Add(rtnval)

                    command.ExecuteNonQuery()

                    actionID = Convert.ToInt32(rtnval.Value)
                    If actionID > 0 Then
                        MessageBox.Show($"Action {actionID} successfully created", "Result", MessageBoxButton.OK, MessageBoxImage.Information)
                        Me.Close()
                    Else
                        'something failed, Select Case the causes returned
                        Select Case actionID
                        'TODO: highlight the bad fields?
                            Case -1
                                MessageBox.Show("Unable to create action, missing name", "Result", MessageBoxButton.OK, MessageBoxImage.Error)
                            Case -2
                                MessageBox.Show("Unable to create action, missing description", "Result", MessageBoxButton.OK, MessageBoxImage.Error)
                            Case -3
                                MessageBox.Show("Unable to create action, application does not exist", "Result", MessageBoxButton.OK, MessageBoxImage.Error)
                            Case Else
                                MessageBox.Show("Unable to create action, unknown error", "Result", MessageBoxButton.OK, MessageBoxImage.Error)
                        End Select
                    End If
                Else
                    'updating an existing application
                    command.CommandText = "dbo.updateAction"
                    command.Parameters.AddWithValue("@actionID", actionID)

                    Dim rtnval As New SqlParameter("@ReturnValue", SqlDbType.Int)
                    rtnval.Direction = ParameterDirection.ReturnValue
                    command.Parameters.Add(rtnval)

                    command.ExecuteNonQuery()

                    Dim result As Integer = Convert.ToInt32(rtnval.Value)
                    Select Case result
                        Case 0
                            MessageBox.Show("Update successful", "Result", MessageBoxButton.OK, MessageBoxImage.Information)
                            Me.Close()
                        Case 1
                            MessageBox.Show("Update failed, nothing updated", "Result", MessageBoxButton.OK, MessageBoxImage.Warning)
                        Case Else
                            MessageBox.Show("Update failed, unknown error", "Result", MessageBoxButton.OK, MessageBoxImage.Error)
                    End Select
                End If
            End Using
        End If
    End Sub
End Class
