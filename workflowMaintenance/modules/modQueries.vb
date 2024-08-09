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

WHERE applicationID = @applicationID OR ISNULL(@applicationID, -1) = -1
"
	End Function

	Friend Function ColumnLengths() As String
		Return _
"
SELECT
column_name,
character_maximum_length AS max_length

FROM INFORMATION_SCHEMA.COLUMNS

WHERE table_schema = @schemaName
AND table_name = @tableName
AND data_type = 'varchar'
"
	End Function
End Module
