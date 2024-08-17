Imports Microsoft.Data.SqlClient
Imports System.Data

Partial Public Class WorkflowActionWindow
    Private workflowName As String
    Private stepNumber As Integer
    Private stagingKey As Integer

    Public Sub New(pi_workflowName As String, Optional pi_stepNumber As Integer = 0)
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        workflowName = pi_workflowName
        stepNumber = pi_stepNumber
    End Sub

    Private Sub LoadRecord() Handles Me.Loaded
        Using command As New SqlCommand
            command.Connection = MainWindow.db_Connection
            command.CommandType = Data.CommandType.Text
            command.CommandText = modQueries.ColumnLengths()
            command.Parameters.AddWithValue("@schemaName", "dbo")
            command.Parameters.AddWithValue("@tableName", "WorkflowActions")

            With command.ExecuteReader
                While .Read
                    Select Case .GetString("column_name")
                        Case "eventParameters"
                            tb_eventParameters.MaxLength = .GetInt32("max_length")
                    End Select
                End While
                .Close()
            End With

            Dim list_steps As New List(Of String)
            command.CommandText = modQueries.CountWorkflowSteps()
            command.Parameters.AddWithValue("@workflowName", workflowName)
            Dim nextStepNumber As Integer = command.ExecuteScalar() + 1
            For i = 1 To (nextStepNumber - 1)
                list_steps.Add(i.ToString)
            Next

            Dim list_actions As New List(Of String)
            command.CommandText = modQueries.Actions()
            command.Parameters.Clear()
            command.Parameters.AddWithValue("@actionID", -1)
            With command.ExecuteReader
                While .Read
                    list_actions.Add(.GetString("Name"))
                End While
                .Close()
            End With
            list_actions.Sort()
            combo_actionName.ItemsSource = list_actions

            If stepNumber = 0 Then
                list_steps.Add(nextStepNumber.ToString)
                combo_stepNumber.ItemsSource = list_steps
                combo_stepNumber.SelectedValue = nextStepNumber.ToString  'default new step to last, so previous step count + 1
            Else
                combo_stepNumber.ItemsSource = list_steps

                command.Parameters.Clear()
                command.CommandText = modQueries.ShowWorkflowActions()
                command.Parameters.AddWithValue("@workflowName", workflowName)
                command.Parameters.AddWithValue("@stepNumber", stepNumber)

                With command.ExecuteReader
                    While .Read
                        stagingKey = .GetInt32("StagingKey")
                        combo_stepNumber.SelectedValue = .GetByte("StepNumber").ToString
                        combo_actionName.SelectedValue = .GetString("ActionName")
                        tb_eventParameters.Text = If(.IsDBNull(.GetOrdinal("EventParameters")), "", .GetString("EventParameters"))
                        cb_continueAfterError.IsChecked = (.GetBoolean("ContinueAfterError") = True)
                    End While
                    .Close()
                End With
            End If
        End Using
    End Sub

    Private Sub WindowClosed() Handles Me.Closed
        'when the window closes, refresh the original DataGrid
        Dim mainWindow As MainWindow = CType(Application.Current.MainWindow, MainWindow)
        'mainWindow.RefreshWorkflowActions()
        'TODO: might need a separate call to refresh the window, can't reuse since I can't add parameters
    End Sub

    Private Sub SaveWorkflowAction() Handles btn_SaveWorkflowAction.Click
        'TODO: this will update the values of stepNumber in stage_WorkflowActions for the workflow in question, and reinsert the row for this stagingKey value
    End Sub

    'TODO: also will need a way to delete steps
    'TODO: something to reorder steps when one is inserted/removed
End Class
