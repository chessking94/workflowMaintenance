Imports Microsoft.Data.SqlClient
Imports System.Collections.ObjectModel
Imports System.Data
Imports System.IO

Partial Public Class MainWindow
    Friend Shared myConfig As New Utilities_NetCore.clsConfig
    Friend Shared projectDir As String
    Friend Shared db_Connection As New SqlConnection

    Friend Property collectionWorkflowActions As New ObservableCollection(Of clsWorkflowAction)

#Region "Window Events"
    Private Sub WindowLoaded() Handles Me.Loaded
        projectDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\.."))
        Dim configFile As String = Path.Combine(projectDir, "appsettings.json")
        myConfig.configFile = configFile

#If DEBUG Then
        Dim connectionString As String = myConfig.getConfig("connectionStringDev")
#Else
        Dim connectionString As String = myConfig.getConfig("connectionStringProd")
#End If

        db_Connection = Utilities_NetCore.Connection(connectionString)
    End Sub

    Private Sub WindowClosed() Handles Me.Closed
        Try
            db_Connection.Close()
        Catch ex As Exception

        End Try
    End Sub
#End Region

#Region "Home"
    Private Sub RefreshHome() Handles tab_Home.Loaded, btn_RefreshHome.Click
        Using command As New SqlCommand
            command.Connection = db_Connection
            command.CommandType = Data.CommandType.Text
            command.CommandText = modQueries.ActiveEvents()

            Dim dataTable As New DataTable()
            Dim adapter As New SqlDataAdapter(command)
            adapter.Fill(dataTable)

            dg_ActiveEvents.ItemsSource = dataTable.DefaultView
        End Using
    End Sub
#End Region

#Region "Application"
    Private Sub CreateApplication() Handles btn_AddApp.Click
        Dim appWindow As New ApplicationWindow()
        appWindow.Show()
    End Sub

    Private Sub Hyperlink_ApplicationID(sender As Object, e As RoutedEventArgs)
        Dim hyperlink As Hyperlink = CType(sender, Hyperlink)
        Dim run As Run = CType(hyperlink.Inlines.FirstInline, Run)
        Dim applicationID As Integer = Convert.ToInt32(run.Text)

        Dim appWindow As New ApplicationWindow(applicationID)
        appWindow.Show()
    End Sub

    Friend Sub RefreshApplications() Handles tab_Applications.Loaded, btn_RefreshApp.Click
        Using command As New SqlCommand
            command.Connection = db_Connection
            command.CommandType = Data.CommandType.Text
            command.CommandText = modQueries.Applications()
            command.Parameters.AddWithValue("@applicationID", -1)

            Dim dataTable As New DataTable()
            Dim adapter As New SqlDataAdapter(command)
            adapter.Fill(dataTable)

            dg_Applications.ItemsSource = dataTable.DefaultView
        End Using
    End Sub
#End Region

#Region "Action"
    Private Sub CreateAction() Handles btn_AddAction.Click
        Dim actionWindow As New ActionWindow()
        actionWindow.Show()
    End Sub

    Private Sub Hyperlink_ActionID(sender As Object, e As RoutedEventArgs)
        Dim hyperlink As Hyperlink = CType(sender, Hyperlink)
        Dim run As Run = CType(hyperlink.Inlines.FirstInline, Run)
        Dim actionID As Integer = Convert.ToInt32(run.Text)

        Dim actionWindow As New ActionWindow(actionID)
        actionWindow.Show()
    End Sub

    Friend Sub RefreshActions() Handles tab_Actions.Loaded, btn_RefreshAction.Click
        Using command As New SqlCommand
            command.Connection = db_Connection
            command.CommandType = Data.CommandType.Text
            command.CommandText = modQueries.Actions()
            command.Parameters.AddWithValue("@actionID", -1)

            Dim dataTable As New DataTable()
            Dim adapter As New SqlDataAdapter(command)
            adapter.Fill(dataTable)

            dg_Actions.ItemsSource = dataTable.DefaultView
        End Using
    End Sub
#End Region

#Region "Workflow"
    Private Sub CreateWorkflow() Handles btn_AddWorkflow.Click
        Dim workflowWindow As New WorkflowWindow()
        workflowWindow.Show()
    End Sub

    Private Sub Hyperlink_WorkflowID(sender As Object, e As RoutedEventArgs)
        Dim hyperlink As Hyperlink = CType(sender, Hyperlink)
        Dim run As Run = CType(hyperlink.Inlines.FirstInline, Run)
        Dim workflowID As Integer = Convert.ToInt32(run.Text)

        Dim workflowWindow As New WorkflowWindow(workflowID)
        workflowWindow.Show()
    End Sub

    Friend Sub RefreshWorkflows() Handles tab_Workflows.Loaded, btn_RefreshWorkflow.Click
        Using command As New SqlCommand
            command.Connection = db_Connection
            command.CommandType = Data.CommandType.Text
            command.CommandText = modQueries.Workflows()
            command.Parameters.AddWithValue("@workflowID", -1)

            Dim dataTable As New DataTable()
            Dim adapter As New SqlDataAdapter(command)
            adapter.Fill(dataTable)

            dg_Workflows.ItemsSource = dataTable.DefaultView
        End Using
    End Sub
#End Region

#Region "Workflow Actions"
    Private Sub BuildWorkflowList() Handles tab_WorkflowActions.Loaded, btn_ResetWFActions.Click
        combo_workflowName.IsEnabled = True
        btn_AddWFAction.IsEnabled = False
        combo_workflowName.SelectedValue = ""
        dg_WorkflowActions.ItemsSource = Nothing

        Using command As New SqlCommand
            Dim combobox_items As New List(Of String)
            command.Connection = db_Connection
            command.CommandType = Data.CommandType.Text
            command.CommandText = modQueries.Workflows()
            command.Parameters.AddWithValue("@workflowID", -1)

            With command.ExecuteReader
                While .Read
                    combobox_items.Add(.Item("Name"))
                End While
                .Close()
            End With
            combobox_items.Sort()  'want these to be in alphabetical order
            combo_workflowName.ItemsSource = combobox_items
        End Using
    End Sub

    Private Sub Hyperlink_StepNumber(sender As Object, e As RoutedEventArgs)
        Dim hyperlink As Hyperlink = CType(sender, Hyperlink)
        Dim run As Run = CType(hyperlink.Inlines.FirstInline, Run)
        Dim stepNumber As Integer = Convert.ToInt32(run.Text)

        Dim workflowActionWindow As New WorkflowActionWindow(combo_workflowName.SelectedValue, stepNumber)
        workflowActionWindow.Show()
    End Sub

    Friend Sub PresentWorkflowActions() Handles combo_workflowName.SelectionChanged
        If combo_workflowName.SelectedValue <> "" Then
            collectionWorkflowActions.Clear()
            combo_workflowName.IsEnabled = False
            btn_AddWFAction.IsEnabled = True

            Using command As New SqlCommand
                command.Connection = db_Connection
                command.CommandType = Data.CommandType.StoredProcedure
                command.CommandText = "dbo.stageWorkflowActions"
                command.Parameters.AddWithValue("@workflowName", combo_workflowName.SelectedValue)
                command.ExecuteNonQuery()

                command.CommandType = Data.CommandType.Text
                command.CommandText = modQueries.ShowWorkflowActions()
                command.Parameters.AddWithValue("@stepNumber", -1)

                Dim dataTable As New DataTable()
                Dim adapter As New SqlDataAdapter(command)
                adapter.Fill(dataTable)

                For Each row As DataRow In dataTable.Rows
                    Dim wfAction As New clsWorkflowAction
                    With wfAction
                        .stagingKey = CInt(row("StagingKey"))
                        .stepNumber = CInt(row("StepNumber"))
                        .actionName = row("ActionName").ToString()
                        .eventParameters = row("EventParameters").ToString()
                        .continueAfterError = CBool(row("ContinueAfterError"))
                    End With

                    collectionWorkflowActions.Add(wfAction)
                Next

                dg_WorkflowActions.ItemsSource = collectionWorkflowActions
            End Using
        End If
    End Sub

    Private Sub AddWorkflowAction() Handles btn_AddWFAction.Click
        Dim workflowActionWindow As New WorkflowActionWindow(combo_workflowName.SelectedValue)
        workflowActionWindow.Show()
    End Sub

    Private Sub SaveWorkflowActions() Handles btn_SaveWFActions.Click
        'TODO: execute dbo.createWorkflowActions
    End Sub
#End Region
End Class
