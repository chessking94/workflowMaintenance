﻿Imports System.Data
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient

Partial Public Class ApplicationWindow
    Private applicationID As Integer

    Public Sub New(Optional pi_applicationID As Integer = 0)
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        applicationID = pi_applicationID
    End Sub

    Private Sub LoadRecord() Handles Me.Loaded
        Using command As New SqlCommand
            command.Connection = MainWindow.db_Connection
            command.CommandType = CommandType.Text
            command.CommandText = modQueries.ColumnLengths()
            command.Parameters.AddWithValue("@schemaName", "dbo")
            command.Parameters.AddWithValue("@tableName", "Applications")

            With command.ExecuteReader
                While .Read
                    Select Case .GetString("column_name")
                        Case "applicationName"
                            tb_Name.MaxLength = .GetInt32("max_length")
                        Case "applicationDescription"
                            tb_Description.MaxLength = .GetInt32("max_length")
                        Case "applicationFilename"
                            tb_CallName.MaxLength = .GetInt32("max_length")
                        Case "applicationDefaultParameter"
                            tb_DefaultParameter.MaxLength = .GetInt32("max_length")
                    End Select
                End While
                .Close()
            End With

            Dim list_applicationTypes As New List(Of String) From {""}
            command.Parameters.Clear()
            command.CommandText = modQueries.ApplicationTypes()
            With command.ExecuteReader
                While .Read
                    list_applicationTypes.Add(.GetString("applicationType"))
                End While
                .Close()
            End With
            list_applicationTypes.Sort()
            combo_ApplicationType.ItemsSource = list_applicationTypes

            If applicationID = 0 Then
                tb_ID.Text = "(new)"
            Else
                command.Parameters.Clear()
                command.CommandText = modQueries.Applications()
                command.Parameters.AddWithValue("@applicationID", applicationID)

                With command.ExecuteReader
                    While .Read
                        tb_ID.Text = .GetInt32("ID").ToString
                        tb_Name.Text = .GetString("Name")
                        tb_Description.Text = .GetString("Description")
                        tb_CallName.Text = .GetString("Filename")
                        tb_DefaultParameter.Text = If(.IsDBNull(.GetOrdinal("Default_Parameter")), "", .GetString("Default_Parameter"))
                        cb_Active.IsChecked = (.GetBoolean("Active") = True)
                        combo_ApplicationType.SelectedValue = If(.IsDBNull(.GetOrdinal("Type")), "", .Item("Type"))
                    End While
                    .Close()
                End With
            End If
        End Using
    End Sub

    Private Sub WindowClosed() Handles Me.Closed
        'when the window closes, refresh the original DataGrid
        Dim mainWindow As MainWindow = CType(Application.Current.MainWindow, MainWindow)
        mainWindow.RefreshApplications()
    End Sub

    Private Sub ApplicationTypeChanged() Handles combo_ApplicationType.SelectionChanged
        If combo_ApplicationType.SelectedValue IsNot Nothing Then
            Select Case combo_ApplicationType.SelectedValue
                Case "Stored Procedure"
                    label_CallName.Content = "Procedure Name (Database.Schema.Name)"
                Case Else
                    label_CallName.Content = "Filename"
            End Select
        End If
    End Sub

    Private Sub SaveApplication() Handles btn_SaveApp.Click
        Dim validationFailReason As String = ""

        'cleanse data
        tb_Name.Text = tb_Name.Text.Trim()
        tb_Description.Text = tb_Description.Text.Trim()
        tb_CallName.Text = tb_CallName.Text.Trim()
        tb_DefaultParameter.Text = tb_DefaultParameter.Text.Trim()

        'validate data
        If validationFailReason = "" Then
            If String.IsNullOrWhiteSpace(tb_Name.Text) Then
                validationFailReason = "Invalid name"
            End If
        End If

        If validationFailReason = "" Then
            If String.IsNullOrWhiteSpace(tb_Description.Text) Then
                validationFailReason = "Invalid description"
            End If
        End If

        'this is looking on the machine running the app, doesn't work when the server actually running the events is different
        'If validationFailReason = "" Then
        '    If Not IO.File.Exists(tb_CallName.Text) Then
        '        validationFailReason = $"File '{tb_CallName.Text}' does not exist"
        '    End If
        'End If

        If validationFailReason = "" Then
            If String.IsNullOrWhiteSpace(tb_CallName.Text) Then
                validationFailReason = "Invalid filename"
            End If
        End If

        If validationFailReason = "" Then
            If combo_ApplicationType.SelectedValue Is Nothing Then
                validationFailReason = "Type not selected"
            End If
        End If

        'perform create/update
        If validationFailReason <> "" Then
            MessageBox.Show($"Pre-validation failed: {validationFailReason}", "Result", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Else
            Using command As New SqlCommand
                command.Connection = MainWindow.db_Connection
                command.CommandType = CommandType.StoredProcedure
                command.Parameters.AddWithValue("@applicationName", tb_Name.Text)
                command.Parameters.AddWithValue("@applicationDescription", tb_Description.Text)
                command.Parameters.AddWithValue("@applicationFilename", tb_CallName.Text)
                command.Parameters.AddWithValue("@applicationActive", If(cb_Active.IsChecked, 1, 0))
                command.Parameters.AddWithValue("@applicationDefaultParameter", tb_DefaultParameter.Text)
                command.Parameters.AddWithValue("@applicationType", If(combo_ApplicationType.SelectedValue = "", DBNull.Value, combo_ApplicationType.SelectedValue))

                If applicationID = 0 Then
                    'new application
                    command.CommandText = "Workflow.dbo.createApplication"

                    Dim rtnval As New SqlParameter("@ReturnValue", SqlDbType.Int)
                    rtnval.Direction = ParameterDirection.ReturnValue
                    command.Parameters.Add(rtnval)

                    command.ExecuteNonQuery()

                    applicationID = Convert.ToInt32(rtnval.Value)
                    If applicationID > 0 Then
                        MessageBox.Show($"Application {applicationID} successfully created", "Result", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Me.Close()
                    Else
                        'something failed, Select Case the causes returned
                        Select Case applicationID
                        'TODO: highlight the bad fields?
                            Case -1
                                MessageBox.Show("Unable to create application, missing name", "Result", MessageBoxButtons.OK, MessageBoxIcon.Error)
                            Case -2
                                MessageBox.Show("Unable to create application, missing description", "Result", MessageBoxButtons.OK, MessageBoxIcon.Error)
                            Case -3
                                MessageBox.Show("Unable to create application, missing filename", "Result", MessageBoxButtons.OK, MessageBoxIcon.Error)
                            Case -4
                                MessageBox.Show("Unable to create application, missing type", "Result", MessageBoxButtons.OK, MessageBoxIcon.Error)
                            Case Else
                                MessageBox.Show("Unable to create application, unknown error", "Result", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        End Select
                    End If
                Else
                    'updating an existing application
                    command.CommandText = "Workflow.dbo.updateApplication"
                    command.Parameters.AddWithValue("@applicationID", applicationID)

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
