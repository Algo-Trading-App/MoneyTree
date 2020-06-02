CREATE DATABASE [userDB]
GO

USE [userDB]
GO

CREATE TABLE [dbo].[User] (
    [Id]               INT IDENTITY (1, 1) NOT NULL,
    [Email]            NVARCHAR (128)      NOT NULL,
    [FirstName]        NVARCHAR (64)       Not Null,
    [LastName]         NVARCHAR (64)       NOT NULL,
    [BrokerageAccount] NVARCHAR (64),
    PRIMARY KEY CLUSTERED ([id] ASC)  
);
 GO

 CREATE TABLE [dbo].[Portfolio] (
    [Id]           INT IDENTITY (1, 1) NOT NULL,
    [OwnerID]      INT                 NOT NULL,
    [Active]       BIT                 NOT NULL, 
    [Generated]    DATETIME2           NOT NULL,
    [InitialValue] MONEY               NOT NULL,
    [StopValue]    MONEY               NOT NULL,
    [DesiredRisk]  DECIMAL(5, 4)       NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    FOREIGN KEY ([OwnerId]) REFERENCES [dbo].[User] ([id])
);
GO

CREATE TABLE [dbo].[Holding] (
    [Id]            INT IDENTITY (1, 1) NOT NULL,
    [PortfolioId]   INT                 NOT NULL,
    [Name]          NVARCHAR (64)       NOT NULL,
    [Abbreviation]  NVARCHAR (16)       NOT NULL,
    [Description]   NVARCHAR (64)       NOT NULL,
    [Quantity]      INT                 NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    FOREIGN KEY ([PortfolioId]) REFERENCES [dbo].[Portfolio] ([Id])
);
GO

CREATE TABLE [dbo].[PortfolioActions] (
    [Id]   INT IDENTITY (1, 1) NOT NULL,
    [Name] NVARCHAR (64)       NOT NULL,
    [Code] NVARCHAR (1)        NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
);
GO

CREATE TABLE [dbo].[PortfolioHistory] (
    [Id]            INT IDENTITY (1, 1) NOT NULL,
    [PortfolioId]   INT                 NOT NULL,
    [Date]          DATETIME2           NOT NULL,
    [Valuation]     MONEY               NOT NULL,
    [Risk]          DECIMAL (5,4)       NOT NULL,
    [ActionTakenId] INT                 NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    FOREIGN KEY ([PortfolioId]) REFERENCES [dbo].[Portfolio] ([Id]),
    FOREIGN KEY ([ActionTakenId]) REFERENCES [dbo].[PortfolioActions] ([Id])
);
GO


