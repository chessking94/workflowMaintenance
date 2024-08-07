Imports Microsoft.Data.SqlClient
Imports System.Data
Imports System.IO

Public Class MainWindow
    Friend Shared myConfig As New Utilities_NetCore.clsConfig
    Friend Shared projectDir As String
    Friend Shared db_Connection As New SqlConnection

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
    Private Sub Hyperlink_ApplicationID()

    End Sub

    Private Sub RefreshApplications() Handles tab_Applications.Loaded, btn_RefreshApp.Click
        Using command As New SqlCommand
            command.Connection = db_Connection
            command.CommandType = Data.CommandType.Text
            command.CommandText = modQueries.Applications()

            Dim dataTable As New DataTable()
            Dim adapter As New SqlDataAdapter(command)
            adapter.Fill(dataTable)

            dg_Applications.ItemsSource = dataTable.DefaultView
        End Using
    End Sub
#End Region
End Class
