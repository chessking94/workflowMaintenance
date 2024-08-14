﻿Imports System.Data
Imports Microsoft.Data.SqlClient

Partial Public Class WorkflowWindow
    Private workflowID As Integer

    Public Sub New(Optional pi_workflowID As Integer = 0)
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        workflowID = pi_workflowID
    End Sub

    Private Sub LoadRecord() Handles Me.Loaded
        Using command As New SqlCommand
            command.Connection = MainWindow.db_Connection
            command.CommandType = Data.CommandType.Text
            command.CommandText = modQueries.ColumnLengths()
            command.Parameters.AddWithValue("@schemaName", "dbo")
            command.Parameters.AddWithValue("@tableName", "Workflows")

            With command.ExecuteReader
                While .Read
                    Select Case .GetString("column_name")
                        Case "workflowName"
                            tb_Name.MaxLength = .GetInt32("max_length")
                        Case "workflowDescription"
                            tb_Description.MaxLength = .GetInt32("max_length")
                    End Select
                End While
                .Close()
            End With

            If workflowID = 0 Then
                tb_ID.Text = "(new)"
            Else
                command.Parameters.Clear()
                command.CommandText = modQueries.Workflows()
                command.Parameters.AddWithValue("@workflowID", workflowID)

                With command.ExecuteReader
                    While .Read
                        tb_ID.Text = .GetInt16("ID").ToString
                        tb_Name.Text = .GetString("Name")
                        tb_Description.Text = .GetString("Description")
                        cb_Active.IsChecked = (.GetBoolean("Active") = True)
                    End While
                    .Close()
                End With
            End If
        End Using
    End Sub

    Private Sub WindowClosed() Handles Me.Closed
        'when the window closes, refresh the original DataGrid
        Dim mainWindow As MainWindow = CType(Application.Current.MainWindow, MainWindow)
        mainWindow.RefreshWorkflows()
    End Sub

    Private Sub SaveWorkflow() Handles btn_SaveWorkflow.Click
        Dim validationFailReason As String = ""

        'cleanse data
        tb_Name.Text = tb_Name.Text.Trim()
        tb_Description.Text = tb_Description.Text.Trim()

        'validate data
        'nothing to validate here

        'perform create/update
        If validationFailReason <> "" Then
            MessageBox.Show($"Pre-validation failed: {validationFailReason}", "Result", MessageBoxButton.OK, MessageBoxImage.Error)
        Else
            Using command As New SqlCommand
                command.Connection = MainWindow.db_Connection
                command.CommandType = Data.CommandType.StoredProcedure
                command.Parameters.AddWithValue("@workflowName", tb_Name.Text)
                command.Parameters.AddWithValue("@workflowDescription", tb_Description.Text)
                command.Parameters.AddWithValue("@workflowActive", If(cb_Active.IsChecked, 1, 0))

                If workflowID = 0 Then
                    'new application
                    command.CommandText = "dbo.createWorkflow"

                    Dim rtnval As New SqlParameter("@ReturnValue", SqlDbType.Int)
                    rtnval.Direction = ParameterDirection.ReturnValue
                    command.Parameters.Add(rtnval)

                    command.ExecuteNonQuery()

                    workflowID = Convert.ToInt32(rtnval.Value)
                    If workflowID > 0 Then
                        MessageBox.Show($"Workflow {workflowID} successfully created", "Result", MessageBoxButton.OK, MessageBoxImage.Information)
                        Me.Close()
                    Else
                        'something failed, Select Case the causes returned
                        Select Case workflowID
                        'TODO: highlight the bad fields?
                            Case -1
                                MessageBox.Show("Unable to create workflow, missing name", "Result", MessageBoxButton.OK, MessageBoxImage.Error)
                            Case -2
                                MessageBox.Show("Unable to create workflow, missing description", "Result", MessageBoxButton.OK, MessageBoxImage.Error)
                            Case Else
                                MessageBox.Show("Unable to create workflow, unknown error", "Result", MessageBoxButton.OK, MessageBoxImage.Error)
                        End Select
                    End If
                Else
                    'updating an existing application
                    command.CommandText = "dbo.updateWorkflow"
                    command.Parameters.AddWithValue("@workflowID", workflowID)

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