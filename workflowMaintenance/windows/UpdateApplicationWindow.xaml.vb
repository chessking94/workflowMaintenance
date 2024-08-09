Imports Microsoft.Data.SqlClient
Imports System.Data

Public Class UpdateApplicationWindow
    Private applicationID As Integer

    Public Sub New(pi_applicationID As Integer)
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        applicationID = pi_applicationID
    End Sub

    Private Sub LoadRecord() Handles Me.Loaded
        Using command As New SqlCommand
            command.Connection = MainWindow.db_Connection
            command.CommandType = Data.CommandType.Text
            command.CommandText = modQueries.Applications()
            command.Parameters.AddWithValue("@applicationID", applicationID)

            With command.ExecuteReader
                While .Read
                    tb_ID.Text = .GetInt32("ID").ToString
                    tb_Name.Text = .GetString("Name")
                    tb_Description.Text = .GetString("Description")
                    tb_Filename.Text = .GetString("Filename")
                    tb_DefaultParameter.Text = If(.IsDBNull(.GetOrdinal("Default_Parameter")), "", .GetString("Default_Parameter"))
                    cb_Active.IsChecked = (.GetBoolean("Active") = True)
                End While
                .Close()
            End With

            command.Parameters.Clear()
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
                            tb_Filename.MaxLength = .GetInt32("max_length")
                        Case "applicationDefaultParameter"
                            tb_DefaultParameter.MaxLength = .GetInt32("max_length")
                    End Select
                End While
                .Close()
            End With
        End Using
    End Sub

    Private Sub WindowClosed() Handles Me.Closed
        'TODO: when the window closes, I want to refresh the original data
        'MainWindow.RefreshApplications()
    End Sub

    Private Sub UpdateApplication() Handles btn_UpdateApp.Click
        'TODO: validate fields before passing to the query

        Using command As New SqlCommand
            command.Connection = MainWindow.db_Connection
            command.CommandType = Data.CommandType.StoredProcedure
            command.CommandText = "dbo.UpdateApplication"
            command.Parameters.AddWithValue("@applicationID", applicationID)
            command.Parameters.AddWithValue("@applicationName", tb_Name.Text)
            command.Parameters.AddWithValue("@applicationDescription", tb_Description.Text)
            command.Parameters.AddWithValue("@applicationFilename", tb_Filename.Text)
            command.Parameters.AddWithValue("@applicationActive", If(cb_Active.IsChecked, 1, 0))
            command.Parameters.AddWithValue("@applicationDefaultParameter", tb_DefaultParameter.Text)

            Dim rtnval As New SqlParameter("@ReturnValue", SqlDbType.Int)
            rtnval.Direction = ParameterDirection.ReturnValue
            command.Parameters.Add(rtnval)

            command.ExecuteNonQuery()

            Dim result As Integer = Convert.ToInt32(rtnval.Value)
            Select Case result
                Case 0
                    MessageBox.Show("Update successful", "Result", MessageBoxButton.OK, MessageBoxImage.Information)
                Case 1
                    MessageBox.Show("Update failed, nothing updated", "Result", MessageBoxButton.OK, MessageBoxImage.Warning)
                Case Else
                    MessageBox.Show("Update failed, unknown error", "Result", MessageBoxButton.OK, MessageBoxImage.Error)
            End Select
        End Using
    End Sub
End Class
