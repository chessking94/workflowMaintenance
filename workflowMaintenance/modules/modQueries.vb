﻿Friend Module modQueries
#Region "Events"
	Friend Function ActiveEvents() As String
		Return _
"
SELECT
eventID AS [Event_ID],
applicationName AS [Application_Name],
workflowName AS [Workflow_Name],
stepNumber AS [Workflow_Step],
actionName AS [Action_Name],
eventParameters AS [Event_Parameters],
eventStatus AS [Event_Status],
eventStatusDate AS [Status_Date],
eventStartDate AS [Start_Date]

FROM dbo.vwActiveEvents

WHERE eventID = @eventID OR ISNULL(@eventID, -1) = -1
"
	End Function
#End Region

#Region "Applications"
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
#End Region

#Region "Actions"
	Friend Function Actions() As String
		Return _
"
SELECT
actionID AS [ID],
actionName AS [Name],
actionDescription AS [Description],
actionActive AS [Active],
actionRequireParameters AS [Require_Parameters],
actionConcurrency AS [Concurrency],
actionLogOutput AS [Log_Output],
applicationID AS [Application_ID]

FROM vwActions

WHERE actionID = @actionID OR ISNULL(@actionID, -1) = -1
"
	End Function
#End Region

#Region "Workflows"
	Friend Function Workflows() As String
		Return _
"
SELECT
workflowID AS [ID],
workflowName AS [Name],
workflowDescription AS [Description],
workflowActive AS [Active]

FROM vwWorkflows

WHERE workflowID = @workflowID OR ISNULL(@workflowID, -1) = -1
"
	End Function
#End Region

#Region "Workflow Actions"
	Friend Function ShowWorkflowActions() As String
		Return _
"
SELECT
stg.stagingKey AS [StagingKey],
stg.stepNumber AS [StepNumber],
a.actionName AS [ActionName],
stg.eventParameters AS [EventParameters],
stg.continueAfterError AS [ContinueAfterError]

FROM dbo.stage_WorkflowActions stg
JOIN dbo.Workflows wf ON
	stg.workflowID = wf.workflowID
LEFT JOIN dbo.Actions a ON
	stg.actionID = a.actionID

WHERE wf.workflowName = @workflowName
AND (stepNumber = @stepNumber OR ISNULL(@stepNumber, -1) = -1)

ORDER BY stg.stepNumber
"
	End Function

	Friend Function CountWorkflowSteps() As String
		Return _
"
SELECT
COUNT(stg.stepNumber) AS [stepCount]

FROM dbo.stage_WorkflowActions stg
JOIN dbo.Workflows w ON
	stg.workflowID = w.workflowID

WHERE w.workflowName = @workflowName
"
	End Function
#End Region

#Region "Reusables"
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
#End Region
End Module
