USE [WebTesterDB]
GO
/****** Object:  Table [dbo].[inputUrl]    Script Date: 2/2/2015 12:00:37 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[inputUrl](
	[id] [uniqueidentifier] NOT NULL,
	[title] [nvarchar](50) NOT NULL,
	[url] [nvarchar](max) NOT NULL,
	[frequency] [int] NOT NULL,
	[engaged] [int] NOT NULL,
	[codefile] [nvarchar](max) NULL,
	[email] [nvarchar](50) NULL,
	[failed] [int] NOT NULL,
	[Category] [nvarchar](200) NULL,
	[IEBrowser] [int] NULL,	
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[testResults]    Script Date: 2/2/2015 12:00:37 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[testResults](
	[urlid] [nvarchar](50) NULL,
	[loadTimeUncached] [numeric](18, 2) NULL,
	[loadTimeCached] [numeric](18, 2) NULL,
	[harfile] [nvarchar](max) NULL,
	[timestamp] [datetime] NOT NULL,
	[screenshot] [nvarchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
ALTER TABLE [dbo].[inputUrl] ADD  CONSTRAINT [DF_inputUrl_failed]  DEFAULT ((0)) FOR [failed]
GO
ALTER TABLE [dbo].[testResults] ADD  CONSTRAINT [DF_inputUrl_loadTimeUncached]  DEFAULT ((0)) FOR [loadTimeUncached]
GO
ALTER TABLE [dbo].[testResults] ADD  CONSTRAINT [DF_inputUrl_loadTimeCached]  DEFAULT ((0)) FOR [loadTimeCached]
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'1: report run in I.E else reports run in Chrome' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'inputUrl', @level2type=N'COLUMN',@level2name=N'IEBrowser'
GO
