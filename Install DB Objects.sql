/* ********************************************** *
 * statsd.net - a stats collector for Graphite 
 * 
 * Creates all DB objects required for 
 * the SqlServer Backend.
 *
 * Creates the following objects:
 *  - TABLE   tb_metrics
 *  - TABLE   tb_metricscontrol
 *  - TYPE    MetricEntriesTableType
 *  - PROC    pr_Metrics_AddMetrics
 *  - PROC    pr_Metrics_DeleteOldData
 *  - PROC    pr_Metrics_GetLatestMetrics
 *  - PROC    pr_Metrics_SetLastRowID
 *
 * Remarks:
 * The script is re-runnable and won't
 * drop any tables. It will drop the stored
 * procs before creating them again It also 
 * doesn't create a user, a role or assign 
 * EXEC permission to the stored procedures.
 * 
 * Project Home (with documentation):
 * https://github.com/lukevenediger/statsd.net/
 * ********************************************** */

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tb_metrics')
BEGIN
    CREATE TABLE [dbo].[tb_metrics](
      [rowid] [bigint] IDENTITY(1,1) NOT NULL,
      [source] [varchar](64) NOT NULL,
      [measure] [varchar](255) NOT NULL,
     CONSTRAINT [PK_tb_metrics] PRIMARY KEY CLUSTERED 
    (
      [rowid] ASC,
      [source] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tb_metricscontrol')
BEGIN
    CREATE TABLE [dbo].[tb_metricscontrol](
      [lastRowID] [bigint] NULL
    ) ON [PRIMARY]
END
GO

IF NOT EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'MetricEntriesTableType')
BEGIN
CREATE TYPE [dbo].[MetricEntriesTableType] AS TABLE(
	[measure] [varchar](255) NULL
)
END
GO

IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE' AND ROUTINE_NAME = 'pr_Metrics_AddMetrics'))
BEGIN 
  DROP PROCEDURE [dbo].[pr_Metrics_AddMetrics]
END
GO
CREATE PROCEDURE [dbo].[pr_Metrics_AddMetrics]
  @metrics MetricEntriesTableType READONLY, 
  @source NVARCHAR (64)
AS
BEGIN
  SET NOCOUNT ON
  INSERT INTO tb_metrics
      (source, measure)
  SELECT 
      @source, measure
  FROM 
      @metrics
END
GO

IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE' AND ROUTINE_NAME = 'pr_Metrics_DeleteOldData'))
BEGIN 
  DROP PROCEDURE [dbo].[pr_Metrics_DeleteOldData]
END
GO
CREATE PROCEDURE [dbo].[pr_Metrics_DeleteOldData]
AS
BEGIN
  DECLARE @lastRowID bigint = 0
  SELECT @lastRowID = ISNULL(lastRowId, -1)
  FROM tb_metricscontrol

  DELETE FROM tb_metrics
  WHERE rowid <= @lastRowID
END
GO 

IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE' AND ROUTINE_NAME = 'pr_Metrics_GetLatestStats'))
BEGIN 
  DROP PROCEDURE [dbo].[pr_Metrics_GetLatestStats]
END
GO
CREATE PROCEDURE [dbo].[pr_Metrics_GetLatestStats]
AS
BEGIN
  DECLARE @lastRowID bigint = 0
  SELECT @lastRowID = ISNULL(lastRowId, -1)
  FROM tb_metricscontrol

  SELECT measure, rowid
  FROM tb_metrics
  WHERE rowid > @lastRowID
  ORDER BY rowid ASC
END
GO

IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE' AND ROUTINE_NAME = 'pr_Metrics_SetLastRowID'))
BEGIN 
  DROP PROCEDURE [dbo].[pr_Metrics_SetLastRowID]
END
GO
CREATE PROCEDURE [dbo].[pr_Metrics_SetLastRowID]
  @lastRowID BIGINT
AS
BEGIN
  IF (EXISTS (SELECT 1 FROM tb_metricscontrol))
    UPDATE tb_metricscontrol
    SET lastRowID = @lastRowID
  ELSE
    INSERT INTO tb_metricscontrol (lastRowID)
    VALUES (@lastRowID)
END
GO