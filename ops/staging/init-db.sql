-- HRM Staging Database Initialization & Least-Privilege Role Setup
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'HRM_Next_Staging')
BEGIN
    CREATE DATABASE [HRM_Next_Staging];
END;
GO

USE [HRM_Next_Staging];
GO

-- 1. Migration User: Has DDL permissions to create and alter tables/indexes
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'hrm_migrator')
BEGIN
    CREATE LOGIN [hrm_migrator] WITH PASSWORD = '$(MIGRATOR_PASSWORD)', CHECK_POLICY = ON;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'hrm_migrator')
BEGIN
    CREATE USER [hrm_migrator] FOR LOGIN [hrm_migrator];
    ALTER ROLE [db_ddladmin] ADD MEMBER [hrm_migrator];
    ALTER ROLE [db_datareader] ADD MEMBER [hrm_migrator];
    ALTER ROLE [db_datawriter] ADD MEMBER [hrm_migrator];
END;
GO

-- 2. Application Runtime User: LEAST PRIVILEGE (DML only, no DDL/DROP/ALTER)
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'hrm_app_user')
BEGIN
    CREATE LOGIN [hrm_app_user] WITH PASSWORD = '$(APP_DB_PASSWORD)', CHECK_POLICY = ON;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'hrm_app_user')
BEGIN
    CREATE USER [hrm_app_user] FOR LOGIN [hrm_app_user];
    ALTER ROLE [db_datareader] ADD MEMBER [hrm_app_user];
    ALTER ROLE [db_datawriter] ADD MEMBER [hrm_app_user];
    GRANT EXECUTE TO [hrm_app_user];
END;
GO
