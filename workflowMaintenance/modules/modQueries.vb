Friend Module modQueries
    Friend Function ActiveEvents() As String
		Return _
"
SELECT
eventID AS [Event ID],
applicationName AS [Application Name],
workflowName AS [Workflow Name],
stepNumber AS [Workflow Step],
actionName AS [Action Name],
eventStatus AS [Event Status],
eventStatusDate AS [Status Date],
eventStartDate AS [Start Date]

FROM dbo.vwActiveEvents
"
	End Function
End Module
