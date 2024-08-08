Public Class UpdateApplicationWindow
    Private applicationID As Integer

    Public Sub New(pi_applicationID As Integer)
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        applicationID = pi_applicationID
    End Sub

    Private Sub FormLoaded() Handles Me.Loaded

    End Sub
End Class
