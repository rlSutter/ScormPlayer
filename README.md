# SCORM Course Player

A comprehensive ASP.NET Web Forms application for delivering and tracking SCORM-compliant e-learning courses and assessments. This system provides a complete learning management solution with course delivery, progress tracking, and assessment capabilities.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [System Requirements](#system-requirements)
- [Installation](#installation)
- [Configuration](#configuration)
- [Database Setup](#database-setup)
- [Usage](#usage)
- [API Reference](#api-reference)
- [File Structure](#file-structure)
- [Security](#security)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)

## Overview

The SCORM Course Player is a robust e-learning platform built on ASP.NET Web Forms (.NET Framework 4.5) that supports:

- **SCORM 1.2 and 2004** compliance
- **Course and Assessment delivery**
- **Progress tracking and suspend/resume functionality**
- **Real-time data synchronization**
- **Multi-user support with session management**
- **Comprehensive logging and monitoring**

The system integrates with external web services for learner information management and provides a seamless learning experience across different browsers and devices.

## Features

### Core Functionality
- **Course Launch**: Secure course and assessment launching with parameter validation
- **Progress Tracking**: Real-time progress data storage and retrieval
- **Suspend/Resume**: SCORM-compliant suspend data management
- **Completion Tracking**: Automatic completion status updates
- **Score Management**: Scaled score tracking and reporting
- **Session Management**: Secure user session handling

### Administrative Features
- **Package Import**: ZIP-based course package import system
- **Version Management**: Support for multiple course versions
- **User Management**: Integration with existing user systems
- **Access Control**: Registration status validation
- **Error Handling**: Comprehensive error logging and notification

### Technical Features
- **Web Service Integration**: SOAP-based external service communication
- **Database Integration**: SQL Server with stored procedures
- **Logging**: log4net-based comprehensive logging
- **Retry Logic**: Automatic retry mechanisms for external services
- **Performance Monitoring**: Built-in performance tracking

## System Requirements

### Server Requirements
- **Operating System**: Windows Server 2012 R2 or later
- **Web Server**: IIS 8.0 or later
- **.NET Framework**: 4.5 or later
- **Database**: SQL Server 2012 or later
- **Memory**: Minimum 4GB RAM (8GB recommended)
- **Storage**: Minimum 10GB free space for course packages

### Client Requirements
- **Browsers**: Internet Explorer 11+, Chrome 60+, Firefox 55+, Safari 12+
- **JavaScript**: Enabled
- **Cookies**: Enabled
- **Screen Resolution**: Minimum 1024x768

### Dependencies
- **log4net**: 2.0.8
- **System.IO.Compression**: For ZIP file handling
- **System.Web.Services**: For SOAP web service communication

## Installation

### 1. Prerequisites
Ensure the following are installed on your server:
- IIS with ASP.NET 4.5 support
- SQL Server with appropriate permissions
- .NET Framework 4.5 or later

### 2. Application Deployment
1. Copy the application files to your web server directory (e.g., `C:\inetpub\wwwroot\eLearningPlayer\`)
2. Create the following directories:
   - `C:\inetpub\wwwroot\courses\` (for course packages)
   - `C:\Logs\` (for application logs)
   - `C:\ProgramData\Melissa DATA\MatchUP\` (for temporary files)

### 3. IIS Configuration
1. Create a new Application Pool targeting .NET Framework 4.5
2. Create a new Web Application pointing to the application directory
3. Set appropriate permissions for the application pool identity

### 4. Database Setup
Run the provided SQL script (`SCORM_Player_Database_Schema.sql`) to create the database schema and stored procedures.

## Configuration

### Web.config Settings

#### Connection Strings
```xml
<connectionStrings>
  <add name="siebeldb" connectionString="server=YOUR_SERVER;uid=YOUR_USER;pwd=YOUR_PASSWORD;database=siebeldb;Connect Timeout=20;Min Pool Size=3;Max Pool Size=5" providerName="System.Data.SqlClient"/>
  <add name="elearningdb" connectionString="server=YOUR_SERVER;uid=YOUR_USER;pwd=YOUR_PASSWORD;database=elearning;Min Pool Size=3;Max Pool Size=5" providerName="System.Data.SqlClient"/>
</connectionStrings>
```

#### Application Settings
```xml
<appSettings>
  <add key="ExtractedCoursePath" value="c:\inetpub\wwwroot\courses\"/>
  <add key="Import_TempPath" value="C:\ProgramData\Melissa DATA\MatchUP"/>
  <add key="Retry_Number" value="4"/>
  <add key="Retry_Pause" value="1"/>
</appSettings>
```

#### Logging Configuration
The application uses log4net for logging. Configure log files in the `<log4net>` section:
- **RemoteSyslogAppender**: For centralized logging
- **SPLogFileAppender**: For service-specific logs
- **GMLogFileAppender**: For general application logs

### External Service Configuration
Configure web service endpoints in the application settings:
```xml
<applicationSettings>
  <eLearningPlayer.Properties.Settings>
    <setting name="eLearningPlayer_com_certegrity_cloudsvc_Service" serializeAs="String">
      <value>https://your-cloud-service.com/basic/service.asmx</value>
    </setting>
    <setting name="eLearningPlayer_com_certegrity_hciscormsvc_Service" serializeAs="String">
      <value>https://your-scorm-service.com/Scorm/service.asmx</value>
    </setting>
  </eLearningPlayer.Properties.Settings>
</applicationSettings>
```

## Database Setup

### 1. Create Database
Execute the provided SQL script to create the `elearning` database with the following tables:

- **ElearningRegistration**: User course registrations
- **ElearningApp**: Course/assessment package information
- **ElearningAppItem**: Individual course items and attempts
- **ElearningAttempt**: Attempt tracking and status

### 2. Stored Procedures
The script creates the following stored procedures:
- `sp_LaunchElearningApp`: Launches courses/assessments
- `sp_UpdatetElearningAppItemAttempt`: Updates attempt data
- `sp_SaveProgressData`: Saves progress information
- `sp_GetProgressData`: Retrieves progress data
- `sp_InsertElearningApp`: Inserts new courses/assessments

### 3. Permissions
Ensure the application's database user has the following permissions:
- `db_datareader` and `db_datawriter` on the `elearning` database
- `db_datareader` on the `siebeldb` database (for user validation)
- Execute permissions on all stored procedures

## Usage

### Course Launch
Courses are launched via the `HCIlaunch.aspx` page with the following parameters:
- `RegId`: Registration ID
- `UserId`: User identifier
- `CrseId`: Course ID
- `CrseType`: Course type ('C' for course, 'A' for assessment)
- `VersionId`: Version number

Example URL:
```
https://your-domain.com/HCIlaunch.aspx?RegId=12345&UserId=USER001&CrseId=COURSE001&CrseType=C&VersionId=1
```

### Package Import
Use the `import.aspx` page to import new course packages:
1. Navigate to `import.aspx?course=COURSE_ID&type=C`
2. Select a ZIP file containing the SCORM package
3. Click "Import" to extract and register the package

### Progress Tracking
The system automatically tracks:
- Course progress and completion status
- Suspend data for resume functionality
- Assessment scores and results
- Time spent in courses
- Exit modes and success status

## API Reference

### Web Methods

#### ExitPlayer
Handles course/assessment exit and data saving.
```csharp
[WebMethod]
public static string ExitPlayer(
    string normal_exit,
    int app_item_id,
    int attempt_id,
    string progress_data,
    string location,
    string completion_status,
    string exit_mode,
    string success_status,
    string enter_time,
    string exit_time,
    string encoded_user_id,
    string reg_id,
    string type,
    decimal? score_scaled
)
```

#### StoreSuspendData2
Saves progress data during course execution.
```csharp
[WebMethod]
public static string StoreSuspendData2(
    int app_item_id,
    int attempt_id,
    string progress_data,
    string reg_id,
    string type
)
```

#### GetSuspendData2
Retrieves previously saved progress data.
```csharp
[WebMethod]
public static string GetSuspendData2(
    int app_item_id,
    string reg_id,
    string type
)
```

### JavaScript API
The system provides JavaScript functions for SCORM communication:

#### hciSaveSuspendData
Saves suspend data to the server.
```javascript
hciSaveSuspendData(progress_data, OnSuccessCallback, OnFailureCallback);
```

#### hciGetSuspendData
Retrieves suspend data from the server.
```javascript
hciGetSuspendData(OnSuccessCallback, OnFailureCallback);
```

#### hciNormalExit
Handles normal course completion.
```javascript
hciNormalExit(progress_data, location, completion_status, exit_mode, success_status, enter_time, exit_time, encoded_user_id, reg_id, type, score_scaled, OnSuccessExit, OnFailureCallback);
```

#### hciExit
Handles course exit without completion.
```javascript
hciExit(progress_data, location, exit_mode, success_status, enter_time, exit_time, encoded_user_id, reg_id, type, OnSuccessExit, OnFailureCallback);
```

## File Structure

```
eLearningPlayer/
├── HCIlaunch.aspx              # Main course launch page
├── HCIlaunch.aspx.cs           # Launch page code-behind
├── import.aspx                 # Package import page
├── import.aspx.cs              # Import page code-behind
├── service.asmx                # Web service endpoint
├── service.asmx.cs             # Web service implementation
├── Global.asax                 # Application events
├── Global.asax.cs              # Application event handlers
├── Web.config                  # Application configuration
├── packages.config             # NuGet package references
├── Properties/
│   ├── AssemblyInfo.cs         # Assembly information
│   ├── Settings.settings       # Application settings
│   └── PublishProfiles/        # Deployment profiles
├── Web References/
│   ├── com.certegrity.cloudsvc.basic/    # Cloud service reference
│   └── com.certegrity.hciscormsvc/       # SCORM service reference
└── bin/                        # Compiled assemblies
```

## Security

### Authentication
- Session-based authentication using cookies
- User validation against external systems
- Registration status verification

### Data Protection
- Base64 encoding for sensitive user data
- SQL parameter binding to prevent injection
- Secure connection strings (consider encryption)

### Access Control
- Course access validation
- Registration status checking
- Session timeout handling

### Recommendations
1. Use HTTPS for all communications
2. Implement proper input validation
3. Regular security updates
4. Monitor access logs
5. Use encrypted connection strings

## Troubleshooting

### Common Issues

#### Course Launch Failures
- **Symptom**: Course fails to launch with parameter errors
- **Solution**: Verify all required parameters are provided and valid
- **Check**: Database connectivity and user permissions

#### Progress Data Not Saving
- **Symptom**: Progress is lost between sessions
- **Solution**: Check database permissions and stored procedure execution
- **Check**: Web service connectivity and retry settings

#### Import Failures
- **Symptom**: ZIP packages fail to import
- **Solution**: Verify file permissions and directory structure
- **Check**: Available disk space and ZIP file integrity

#### Performance Issues
- **Symptom**: Slow course loading or timeouts
- **Solution**: Check database performance and connection pooling
- **Check**: Web service response times and retry settings

### Log Files
Monitor the following log files for troubleshooting:
- `C:\Logs\HCILaunch.log` - Course launch activities
- `C:\Logs\HCILaunch_ExitPlayer.log` - Exit operations
- `C:\Logs\HCILaunch_StoreSuspData.log` - Progress data storage
- `C:\Logs\HCILaunch_GetSuspData.log` - Progress data retrieval
- `C:\Logs\HCIPlayer_ImportPackage.log` - Package import activities

### Error Codes
Common error codes and their meanings:
- `HCIPLYR_01`: General launch failure
- `completion error`: Course completion processing error
- `deadlocked`: Database deadlock (automatic retry)

## Contributing

### Development Setup
1. Install Visual Studio 2019 or later
2. Install .NET Framework 4.5 SDK
3. Clone the repository
4. Restore NuGet packages
5. Configure local database connection

### Code Standards
- Follow C# coding conventions
- Use meaningful variable and method names
- Include XML documentation for public methods
- Implement proper error handling
- Add logging for important operations

### Testing
- Test with various SCORM packages
- Verify cross-browser compatibility
- Test error scenarios and recovery
- Validate database operations
- Test web service integrations

## License

This project is proprietary software. All rights reserved.

---

## Support

For technical support or questions:
- Check the troubleshooting section
- Review log files for error details
- Contact your system administrator
- Refer to SCORM documentation for content-related issues

## Version History

- **v1.0**: Initial release with basic SCORM support
- **v1.1**: Added assessment support and improved error handling
- **v1.2**: Enhanced logging and performance monitoring
- **v1.3**: Added retry logic and improved web service integration

---

*Last updated: [Current Date]*
