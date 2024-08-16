Imports Microsoft.Data.SqlClient
Imports System.Data

Partial Public Class WorkflowActionWindow
    Private workflowName As String
    Private stepNumber As Integer

    Public Sub New(pi_workflowName As String, pi_stepNumber As Integer)
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

            'TODO: would prefer for this to be dynamic based on information_schema.columns
            tb_actionName.MaxLength = 20
            tb_eventParameters.MaxLength = 250

            If stepNumber = 0 Then
                'TODO: default new steps to last, so previous step count + 1
                tb_stepNumber.Text = ""
            Else
                command.Parameters.Clear()
                command.CommandText = modQueries.ShowWorkflowActions()
                command.Parameters.AddWithValue("@workflowName", workflowName)
                command.Parameters.AddWithValue("@stepNumber", stepNumber)

                With command.ExecuteReader
                    While .Read
                        tb_stepNumber.Text = .GetByte("StepNumber").ToString
                        tb_actionName.Text = .GetString("ActionName")
                        tb_eventParameters.Text = If(.IsDBNull(.GetOrdinal("EventParameters")), "", .GetString("EventParameters"))
                        cb_continueAfterError.IsChecked = (.GetBoolean("ContinueAfterError") = True)
                    End While
                    .Close()
                End With
            End If

            'TODO: limit tb_stepNumber and tb_actionName to certain values, may need to convert to comboboxes
        End Using
    End Sub

    Private Sub WindowClosed() Handles Me.Closed
        'when the window closes, refresh the original DataGrid
        Dim mainWindow As MainWindow = CType(Application.Current.MainWindow, MainWindow)
        mainWindow.RefreshWorkflowActions()
    End Sub

    'Private Sub SaveWorkflowAction() Handles btn_SaveWorkflowAction.Click
    '    'TODO: this will clear out stage_WorkflowActions for the workflow In question, and reinsert the DataGrid entries
    'End Sub

    'TODO: also will need a way to delete steps
    'TODO: something to reorder steps when one is inserted/removed
    'TODO: do not allow steps outside range of 1 to (step count + 1)
End Class
