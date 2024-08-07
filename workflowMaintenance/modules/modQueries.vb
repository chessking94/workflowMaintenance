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

	Friend Function Applications() As String
		Return _
"
SELECT
applicationID AS [ID],
applicationName AS [Name],
applicationDescription AS [Description],
applicationFilename AS [Filename],
applicationDefaultParameter AS [Default_Parameter],
applicationActive AS [Active]

FROM vwApplications
"
	End Function
End Module
