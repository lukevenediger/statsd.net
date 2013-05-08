/* ********************************************** *
 * statsd.net - a stats collector for Graphite 
 * 
 * Creates tables required for SqlServer Backend.
 * See https://github.com/lukevenediger/statsd.net/
 *
 * Creates the following objects:
 *  - TABLE   tb_metrics
 *  - TABLE   tb_metricscontrol
 *
 * Remarks:
 * The script is re-runnable and won't
 * drop any tables.
 * 
 * Project Home (with documentation):
 * https://github.com/lukevenediger/statsd.net/
 * ********************************************** */

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tb_metrics')
BEGIN
    CREATE TABLE [dbo].[tb_metrics](
      [rowid] [bigint] IDENTITY(1,1) NOT NULL,
      [source] [varchar](64) NOT NULL,
      [metric] [varchar](255) NOT NULL,
     CONSTRAINT [PK_tb_metrics] PRIMARY KEY CLUSTERED 
    (
      [rowid] ASC
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