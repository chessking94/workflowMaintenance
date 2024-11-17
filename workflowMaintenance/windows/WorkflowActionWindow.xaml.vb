Imports System.Data
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient

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
            command.CommandType = CommandType.Text
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
                    'intentionally allowing inactive actions to be included in the ComboBox; may be nice for initial workflow configuration
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
                stepNumber = nextStepNumber

                command.Parameters.Clear()
                command.CommandType = CommandType.StoredProcedure
                command.CommandText = "Workflow.dbo.insertWorkflowAction"
                command.Parameters.AddWithValue("@workflowName", workflowName)
                command.Parameters.AddWithValue("@stepNumber", nextStepNumber)

                Dim rtnval As New SqlParameter("@ReturnValue", SqlDbType.Int)
                rtnval.Direction = ParameterDirection.ReturnValue
                command.Parameters.Add(rtnval)

                command.ExecuteNonQuery()

                stagingKey = Convert.ToInt32(rtnval.Value)
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
        Dim mainWindow As MainWindow = CType(Application.Current.MainWindow, MainWindow)
        Dim incompleteRecord As Boolean = False  'if new record was created but never saved/updated, delete it
        Using command As New SqlCommand
            command.Connection = MainWindow.db_Connection
            command.CommandText = modQueries.ShowWorkflowActions()
            command.Parameters.AddWithValue("@workflowName", workflowName)
            command.Parameters.AddWithValue("@stepNumber", stepNumber)

            With command.ExecuteReader
                While .Read
                    If .IsDBNull(.GetOrdinal("ActionName")) Then
                        incompleteRecord = True
                    End If
                End While
                .Close()
            End With
        End Using

        If incompleteRecord Then
            DeleteWorkflowAction()
        End If

        'when the window closes, refresh the original DataGrid
        mainWindow.WorkflowActionsWindowClosed()
    End Sub

    Private Sub SaveWorkflowAction() Handles btn_SaveWorkflowAction.Click
        Dim validationFailReason As String = ""
        If validationFailReason = "" Then
            If Not IsNumeric(combo_stepNumber.SelectedValue) Then
                validationFailReason = $"Step Number {combo_stepNumber.SelectedValue} is not an integer"
            End If
        End If

        If validationFailReason = "" Then
            If combo_actionName.SelectedValue Is Nothing Then
                validationFailReason = "No action selected"
            End If
        End If

        If validationFailReason <> "" Then
            MessageBox.Show($"Pre-validation failed: {validationFailReason}", "Result", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Else
            Using command As New SqlCommand
                command.Connection = MainWindow.db_Connection
                command.CommandType = CommandType.StoredProcedure
                command.CommandText = "Workflow.dbo.saveWorkflowAction"
                command.Parameters.AddWithValue("@stagingKey", stagingKey)
                command.Parameters.AddWithValue("@stepNumber", combo_stepNumber.SelectedValue)
                command.Parameters.AddWithValue("@actionName", combo_actionName.SelectedValue)
                command.Parameters.AddWithValue("@eventParameters", tb_eventParameters.Text)
                command.Parameters.AddWithValue("@continueAfterError", If(cb_continueAfterError.IsChecked, 1, 0))
                command.ExecuteNonQuery()
            End Using

            Me.Close()
        End If
    End Sub

    Private Sub DeleteWorkflowAction() Handles btn_DeleteWorkflowAction.Click
        Using command As New SqlCommand
            command.Connection = MainWindow.db_Connection
            command.CommandType = CommandType.StoredProcedure
            command.CommandText = "Workflow.dbo.deleteWorkflowAction"
            command.Parameters.AddWithValue("@stagingKey", stagingKey)
            command.ExecuteNonQuery()
        End Using

        Me.Close()
    End Sub
End Class
