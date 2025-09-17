-- =============================================
-- SCORM Course Player Database Schema
-- SQL Server DDL Script
-- =============================================

-- Create the main elearning database
USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'elearning')
BEGIN
    CREATE DATABASE elearning;
END
GO

USE elearning;
GO

-- =============================================
-- Core Tables for SCORM Player
-- =============================================

-- ElearningRegistration table - tracks user registrations for courses/assessments
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ElearningRegistration' AND xtype='U')
BEGIN
    CREATE TABLE ElearningRegistration (
        ELN_REG_ID INT IDENTITY(1,1) PRIMARY KEY,
        SESS_REG_ID VARCHAR(15) NOT NULL,  -- FK to CX_SESS_REG.ROW_ID or S_CRSE_TSTRUN.ROW_ID
        HCI_USER_ID VARCHAR(15) NOT NULL,  -- User registration number
        CREATED_DATE DATETIME DEFAULT GETDATE(),
        LAST_UPDATED DATETIME DEFAULT GETDATE(),
        STATUS VARCHAR(10) DEFAULT 'ACTIVE',
        CONSTRAINT UK_ElearningRegistration_SESS_USER UNIQUE (SESS_REG_ID, HCI_USER_ID)
    );
END
GO

-- ElearningApp table - stores course/assessment package information
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ElearningApp' AND xtype='U')
BEGIN
    CREATE TABLE ElearningApp (
        APP_ID INT IDENTITY(1,1) PRIMARY KEY,
        CRSE_ID VARCHAR(15) NOT NULL,      -- Course/Assessment ID
        CRSE_TYPE VARCHAR(2) NOT NULL,     -- 'C' for Course, 'A' for Assessment
        TITLE VARCHAR(300) NOT NULL,       -- Package title
        EXTRACT_PATH VARCHAR(500) NOT NULL, -- Physical path where package is extracted
        WEB_PATH VARCHAR(500) NOT NULL,    -- Web accessible path
        VERSION_ID INT DEFAULT 1,          -- Version number
        IS_ACTIVE BIT DEFAULT 1,           -- Active flag
        CREATED_DATE DATETIME DEFAULT GETDATE(),
        LAST_UPDATED DATETIME DEFAULT GETDATE(),
        CONSTRAINT UK_ElearningApp_CRSE_VERSION UNIQUE (CRSE_ID, CRSE_TYPE, VERSION_ID)
    );
END
GO

-- ElearningAppItem table - stores individual course items/attempts
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ElearningAppItem' AND xtype='U')
BEGIN
    CREATE TABLE ElearningAppItem (
        APP_ITEM_ID INT IDENTITY(1,1) PRIMARY KEY,
        ELN_REG_ID INT NOT NULL,           -- FK to ElearningRegistration
        APP_ID INT NOT NULL,               -- FK to ElearningApp
        PROGRESS_DATA NVARCHAR(MAX),       -- SCORM suspend data
        COMPLETION_STATUS VARCHAR(2),      -- 'Y'=Complete, 'N'=Incomplete, 'E'=Exam
        SCORE_SCALED DECIMAL(10,7),        -- Scaled score (-1 to 1)
        ENTRY_TIME VARCHAR(30),            -- Entry timestamp
        EXIT_TIME VARCHAR(30),             -- Exit timestamp
        LOCATION VARCHAR(30),              -- Current location in course
        EXIT_MODE VARCHAR(2),              -- Exit mode
        SUCCESS_STATUS VARCHAR(2),         -- Success status
        CREATED_DATE DATETIME DEFAULT GETDATE(),
        LAST_UPDATED DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_ElearningAppItem_Registration FOREIGN KEY (ELN_REG_ID) REFERENCES ElearningRegistration(ELN_REG_ID),
        CONSTRAINT FK_ElearningAppItem_App FOREIGN KEY (APP_ID) REFERENCES ElearningApp(APP_ID)
    );
END
GO

-- ElearningAttempt table - tracks individual attempts
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ElearningAttempt' AND xtype='U')
BEGIN
    CREATE TABLE ElearningAttempt (
        ATTEMPT_ID INT IDENTITY(1,1) PRIMARY KEY,
        APP_ITEM_ID INT NOT NULL,          -- FK to ElearningAppItem
        ATTEMPT_NUMBER INT DEFAULT 1,      -- Attempt sequence number
        START_TIME DATETIME DEFAULT GETDATE(),
        END_TIME DATETIME NULL,
        STATUS VARCHAR(20) DEFAULT 'ACTIVE', -- ACTIVE, COMPLETED, ABANDONED
        CREATED_DATE DATETIME DEFAULT GETDATE(),
        LAST_UPDATED DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_ElearningAttempt_AppItem FOREIGN KEY (APP_ITEM_ID) REFERENCES ElearningAppItem(APP_ITEM_ID)
    );
END
GO

-- =============================================
-- Supporting Tables (referenced in code)
-- =============================================

-- CX_TRAIN_OFFR_ACCESS table - tracks course access
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CX_TRAIN_OFFR_ACCESS' AND xtype='U')
BEGIN
    CREATE TABLE CX_TRAIN_OFFR_ACCESS (
        ROW_ID VARCHAR(15) PRIMARY KEY,
        CREATED DATETIME DEFAULT GETDATE(),
        CREATED_BY VARCHAR(15) DEFAULT '0-1',
        LAST_UPD DATETIME DEFAULT GETDATE(),
        LAST_UPD_BY VARCHAR(15) DEFAULT '0-1',
        MODIFICATION_NUM INT DEFAULT 0,
        CONFLICT_ID INT DEFAULT 0,
        REG_ID VARCHAR(15) NOT NULL,
        ENTER_FLG VARCHAR(1) DEFAULT 'N',
        EXIT_FLG VARCHAR(1) DEFAULT 'N'
    );
END
GO

-- S_CRSE_TSTRUN_ACCESS table - tracks assessment access
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='S_CRSE_TSTRUN_ACCESS' AND xtype='U')
BEGIN
    CREATE TABLE S_CRSE_TSTRUN_ACCESS (
        ROW_ID VARCHAR(15) PRIMARY KEY,
        CREATED DATETIME DEFAULT GETDATE(),
        CREATED_BY VARCHAR(15) DEFAULT '0-1',
        LAST_UPD DATETIME DEFAULT GETDATE(),
        LAST_UPD_BY VARCHAR(15) DEFAULT '0-1',
        MODIFICATION_NUM INT DEFAULT 0,
        CONFLICT_ID INT DEFAULT 0,
        CRSE_TSTRUN_ID VARCHAR(15) NOT NULL,
        ENTER_FLG VARCHAR(1) DEFAULT 'N',
        EXIT_FLG VARCHAR(1) DEFAULT 'N'
    );
END
GO

-- =============================================
-- Stored Procedures
-- =============================================

-- sp_LaunchElearningApp - launches a course/assessment
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_LaunchElearningApp')
    DROP PROCEDURE sp_LaunchElearningApp;
GO

CREATE PROCEDURE sp_LaunchElearningApp
    @crseid VARCHAR(15),
    @crsetype VARCHAR(2),
    @regid VARCHAR(15),
    @userid VARCHAR(15),
    @versionid INT,
    @ret VARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @eln_reg_id INT;
    DECLARE @app_item_id INT;
    DECLARE @active_attempt_id INT;
    DECLARE @cur_attempt_id INT;
    DECLARE @packname VARCHAR(300);
    DECLARE @initurl VARCHAR(500);
    DECLARE @reg_status_cd VARCHAR(10);
    DECLARE @crse_title VARCHAR(300);
    
    BEGIN TRY
        -- Check for active attempts
        SELECT @active_attempt_id = ea.ATTEMPT_ID
        FROM ElearningAttempt ea
        INNER JOIN ElearningAppItem eai ON eai.APP_ITEM_ID = ea.APP_ITEM_ID
        INNER JOIN ElearningRegistration er ON er.ELN_REG_ID = eai.ELN_REG_ID
        WHERE er.SESS_REG_ID = @regid 
        AND er.HCI_USER_ID = @userid
        AND ea.STATUS = 'ACTIVE';
        
        IF @active_attempt_id IS NOT NULL
        BEGIN
            SET @ret = 'error: Active attempt exists';
            RETURN;
        END
        
        -- Get or create registration
        SELECT @eln_reg_id = ELN_REG_ID
        FROM ElearningRegistration
        WHERE SESS_REG_ID = @regid AND HCI_USER_ID = @userid;
        
        IF @eln_reg_id IS NULL
        BEGIN
            INSERT INTO ElearningRegistration (SESS_REG_ID, HCI_USER_ID)
            VALUES (@regid, @userid);
            SET @eln_reg_id = SCOPE_IDENTITY();
        END
        
        -- Get course information
        SELECT @packname = ea.TITLE, @initurl = ea.WEB_PATH, @reg_status_cd = 'ACTIVE', @crse_title = ea.TITLE
        FROM ElearningApp ea
        WHERE ea.CRSE_ID = @crseid 
        AND ea.CRSE_TYPE = @crsetype 
        AND ea.VERSION_ID = @versionid
        AND ea.IS_ACTIVE = 1;
        
        IF @packname IS NULL
        BEGIN
            SET @ret = 'error: Course not found';
            RETURN;
        END
        
        -- Create app item
        INSERT INTO ElearningAppItem (ELN_REG_ID, APP_ID, COMPLETION_STATUS, ENTRY_TIME)
        SELECT @eln_reg_id, ea.APP_ID, 'N', CONVERT(VARCHAR(30), GETDATE(), 120)
        FROM ElearningApp ea
        WHERE ea.CRSE_ID = @crseid AND ea.CRSE_TYPE = @crsetype AND ea.VERSION_ID = @versionid;
        
        SET @app_item_id = SCOPE_IDENTITY();
        
        -- Create attempt
        INSERT INTO ElearningAttempt (APP_ITEM_ID, STATUS)
        VALUES (@app_item_id, 'ACTIVE');
        
        SET @cur_attempt_id = SCOPE_IDENTITY();
        
        -- Return results
        SELECT 
            'success' as Result,
            @eln_reg_id as ELN_REG_ID,
            @app_item_id as APP_ITEM_ID,
            @active_attempt_id as ACTIVE_ATTEMPT_ID,
            @cur_attempt_id as CUR_ATTEMPT_ID,
            @packname as PACK_NAME,
            @initurl as INIT_URL,
            @reg_status_cd as REG_STATUS_CD,
            @crse_title as CRSE_TITLE;
            
        SET @ret = 'success';
        
    END TRY
    BEGIN CATCH
        SET @ret = 'error: ' + ERROR_MESSAGE();
    END CATCH
END
GO

-- sp_UpdatetElearningAppItemAttempt - updates attempt data
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_UpdatetElearningAppItemAttempt')
    DROP PROCEDURE sp_UpdatetElearningAppItemAttempt;
GO

CREATE PROCEDURE sp_UpdatetElearningAppItemAttempt
    @app_item_id INT,
    @attempt_id INT,
    @progress_data NVARCHAR(MAX),
    @location VARCHAR(30),
    @completion_status VARCHAR(2),
    @exit_mode VARCHAR(2),
    @success_status VARCHAR(2),
    @enter_time VARCHAR(30),
    @exit_time VARCHAR(30),
    @score_scaled DECIMAL(10,7),
    @ret VARCHAR(300) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Update app item
        UPDATE ElearningAppItem
        SET PROGRESS_DATA = @progress_data,
            COMPLETION_STATUS = @completion_status,
            SCORE_SCALED = @score_scaled,
            ENTRY_TIME = @enter_time,
            EXIT_TIME = @exit_time,
            LOCATION = @location,
            EXIT_MODE = @exit_mode,
            SUCCESS_STATUS = @success_status,
            LAST_UPDATED = GETDATE()
        WHERE APP_ITEM_ID = @app_item_id;
        
        -- Update attempt
        UPDATE ElearningAttempt
        SET END_TIME = CASE WHEN @exit_time IS NOT NULL THEN CONVERT(DATETIME, @exit_time, 120) ELSE NULL END,
            STATUS = CASE WHEN @completion_status = 'Y' THEN 'COMPLETED' ELSE 'ABANDONED' END,
            LAST_UPDATED = GETDATE()
        WHERE ATTEMPT_ID = @attempt_id;
        
        SET @ret = 'success';
        
    END TRY
    BEGIN CATCH
        SET @ret = 'error: ' + ERROR_MESSAGE();
    END CATCH
END
GO

-- sp_SaveProgressData - saves progress data
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_SaveProgressData')
    DROP PROCEDURE sp_SaveProgressData;
GO

CREATE PROCEDURE sp_SaveProgressData
    @app_item_id INT,
    @attempt_id INT,
    @progress_data NVARCHAR(MAX),
    @ret VARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        UPDATE ElearningAppItem
        SET PROGRESS_DATA = @progress_data,
            LAST_UPDATED = GETDATE()
        WHERE APP_ITEM_ID = @app_item_id;
        
        SET @ret = 'success';
        
    END TRY
    BEGIN CATCH
        SET @ret = 'error: ' + ERROR_MESSAGE();
    END CATCH
END
GO

-- sp_GetProgressData - retrieves progress data
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_GetProgressData')
    DROP PROCEDURE sp_GetProgressData;
GO

CREATE PROCEDURE sp_GetProgressData
    @app_item_id INT,
    @progress_data NVARCHAR(MAX) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        SELECT @progress_data = ISNULL(PROGRESS_DATA, 'no record')
        FROM ElearningAppItem
        WHERE APP_ITEM_ID = @app_item_id;
        
    END TRY
    BEGIN CATCH
        SET @progress_data = 'error: ' + ERROR_MESSAGE();
    END CATCH
END
GO

-- sp_InsertElearningApp - inserts new course/assessment
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_InsertElearningApp')
    DROP PROCEDURE sp_InsertElearningApp;
GO

CREATE PROCEDURE sp_InsertElearningApp
    @crse_id VARCHAR(15),
    @crse_type VARCHAR(2),
    @title VARCHAR(300),
    @extract_path VARCHAR(500),
    @web_path VARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        INSERT INTO ElearningApp (CRSE_ID, CRSE_TYPE, TITLE, EXTRACT_PATH, WEB_PATH)
        VALUES (@crse_id, @crse_type, @title, @extract_path, @web_path);
        
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- =============================================
-- Indexes for Performance
-- =============================================

-- Indexes on ElearningRegistration
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ElearningRegistration_SESS_REG_ID')
    CREATE INDEX IX_ElearningRegistration_SESS_REG_ID ON ElearningRegistration(SESS_REG_ID);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ElearningRegistration_HCI_USER_ID')
    CREATE INDEX IX_ElearningRegistration_HCI_USER_ID ON ElearningRegistration(HCI_USER_ID);
GO

-- Indexes on ElearningApp
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ElearningApp_CRSE_ID_TYPE')
    CREATE INDEX IX_ElearningApp_CRSE_ID_TYPE ON ElearningApp(CRSE_ID, CRSE_TYPE);
GO

-- Indexes on ElearningAppItem
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ElearningAppItem_ELN_REG_ID')
    CREATE INDEX IX_ElearningAppItem_ELN_REG_ID ON ElearningAppItem(ELN_REG_ID);
GO

-- Indexes on ElearningAttempt
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ElearningAttempt_APP_ITEM_ID')
    CREATE INDEX IX_ElearningAttempt_APP_ITEM_ID ON ElearningAttempt(APP_ITEM_ID);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ElearningAttempt_STATUS')
    CREATE INDEX IX_ElearningAttempt_STATUS ON ElearningAttempt(STATUS);
GO

-- =============================================
-- Sample Data (Optional)
-- =============================================

-- Insert sample course
IF NOT EXISTS (SELECT 1 FROM ElearningApp WHERE CRSE_ID = 'SAMPLE001' AND CRSE_TYPE = 'C')
BEGIN
    INSERT INTO ElearningApp (CRSE_ID, CRSE_TYPE, TITLE, EXTRACT_PATH, WEB_PATH)
    VALUES ('SAMPLE001', 'C', 'Sample SCORM Course', 'C:\inetpub\wwwroot\courses\sample001', '/courses/sample001');
END
GO

-- Insert sample assessment
IF NOT EXISTS (SELECT 1 FROM ElearningApp WHERE CRSE_ID = 'SAMPLE001' AND CRSE_TYPE = 'A')
BEGIN
    INSERT INTO ElearningApp (CRSE_ID, CRSE_TYPE, TITLE, EXTRACT_PATH, WEB_PATH)
    VALUES ('SAMPLE001', 'A', 'Sample SCORM Assessment', 'C:\inetpub\wwwroot\courses\sample001', '/courses/sample001');
END
GO

PRINT 'SCORM Player Database Schema created successfully!';
PRINT 'Database: elearning';
PRINT 'Tables: ElearningRegistration, ElearningApp, ElearningAppItem, ElearningAttempt';
PRINT 'Stored Procedures: sp_LaunchElearningApp, sp_UpdatetElearningAppItemAttempt, sp_SaveProgressData, sp_GetProgressData, sp_InsertElearningApp';
