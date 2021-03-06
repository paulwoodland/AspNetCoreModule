// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#define CS_ROOTWEB_CONFIG                                L"MACHINE/WEBROOT/APPHOST/"
#define CS_ROOTWEB_CONFIG_LEN                            _countof(CS_ROOTWEB_CONFIG)-1
#define CS_ASPNETCORE_SECTION                            L"system.webServer/aspNetCore"
#define CS_ASPNETCORE_PROCESS_EXE_PATH                   L"processPathNoChange"
#define CS_ASPNETCORE_PROCESS_ARGUMENTS                  L"arguments"
#define CS_ASPNETCORE_PROCESS_STARTUP_TIME_LIMIT         L"startupTimeLimitNoChange"
#define CS_ASPNETCORE_PROCESS_SHUTDOWN_TIME_LIMIT        L"shutdownTimeLimitNoChange"
#define CS_ASPNETCORE_WINHTTP_REQUEST_TIMEOUT            L"requestTimeoutNoChange"
#define CS_ASPNETCORE_RAPID_FAILS_PER_MINUTE             L"rapidFailsPerMinuteNoChange"
#define CS_ASPNETCORE_STDOUT_LOG_ENABLED                 L"stdoutLogEnabled"
#define CS_ASPNETCORE_STDOUT_LOG_FILE                    L"stdoutLogFile"
#define CS_ASPNETCORE_ENVIRONMENT_VARIABLES              L"environmentVariables"
#define CS_ASPNETCORE_ENVIRONMENT_VARIABLE               L"environmentVariable"
#define CS_ASPNETCORE_ENVIRONMENT_VARIABLE_NAME          L"name"
#define CS_ASPNETCORE_ENVIRONMENT_VARIABLE_VALUE         L"value"
#define CS_ASPNETCORE_PROCESSES_PER_APPLICATION          L"processesPerApplicationNoChange"
#define CS_ASPNETCORE_FORWARD_WINDOWS_AUTH_TOKEN         L"forwardWindowsAuthToken"
#define CS_ASPNETCORE_DISABLE_START_UP_ERROR_PAGE        L"disableStartUpErrorPage"
#define CS_ASPNETCORE_RECYCLE_ON_FILE_CHANGE             L"recycleOnFileChange"
#define CS_ASPNETCORE_RECYCLE_ON_FILE_CHANGE_FILE        L"file"
#define CS_ASPNETCORE_RECYCLE_ON_FILE_CHANGE_FILE_PATH   L"path"

#define MAX_RAPID_FAILS_PER_MINUTE 100
#define MILLISECONDS_IN_ONE_SECOND 1000
#define MIN_PORT                   1025
#define MAX_PORT                   48000

#define HEX_TO_ASCII(c) ((CHAR)(((c) < 10) ? ((c) + '0') : ((c) + 'a' - 10)))

extern HTTP_MODULE_ID   g_pModuleId;
extern IHttpServer *    g_pHttpServer;


class ASPNETCORE_CONFIG : IHttpStoredContext
{
public:

    virtual
    ~ASPNETCORE_CONFIG();

    VOID
    CleanupStoredContext()
    {
        delete this;
    }

    static
    HRESULT
    GetConfig(
        _In_  IHttpContext           *pHttpContext,
        _Out_ ASPNETCORE_CONFIG  **ppAspNetCoreConfig
    );

    MULTISZ*
    QueryEnvironmentVariables(
        VOID
    )
    {
        return &m_mszEnvironment;
    }

    DWORD
    QueryRapidFailsPerMinute(
        VOID
    )
    {
        return m_dwRapidFailsPerMinute;
    }

    DWORD
    QueryStartupTimeLimitInMS(
        VOID
    )
    {
        return m_dwStartupTimeLimitInMS;
    }

    DWORD
    QueryShutdownTimeLimitInMS(
        VOID
    )
    {
        return m_dwShutdownTimeLimitInMS;
    }

    DWORD
    QueryProcessesPerApplication(
        VOID
    )
    {
        return m_dwProcessesPerApplication;
    }

    DWORD
    QueryRequestTimeoutInMS(
        VOID
    )
    {
        return m_dwRequestTimeoutInMS;
    }

    STRU*
    QueryArguments(
        VOID
    )
    {
        return &m_struArguments;
    }

    STRU*
    QueryApplicationPath(
        VOID
        )
    {
        return &m_struApplication;
    }

    STRU*
    QueryProcessPath(
        VOID
    )
    {
        return &m_struProcessPath;
    }

    BOOL
    QueryStdoutLogEnabled()
    {
        return m_fStdoutLogEnabled;
    }

    BOOL
    QueryForwardWindowsAuthToken()
    {
        return m_fForwardWindowsAuthToken;
    }

    BOOL
    QueryDisableStartUpErrorPage()
    {
        return m_fDisableStartUpErrorPage;
    }

    STRU*
    QueryStdoutLogFile()
    {
        return &m_struStdoutLogFile;
    }

private:

    //
    // private constructor
    //
    
    ASPNETCORE_CONFIG():
        m_fStdoutLogEnabled( FALSE )
    {
    }

    HRESULT
    Populate(
        IHttpContext *pHttpContext
    );

    DWORD           m_dwRequestTimeoutInMS;
    DWORD           m_dwStartupTimeLimitInMS;
    DWORD           m_dwShutdownTimeLimitInMS;
    MULTISZ         m_mszEnvironment;
    DWORD           m_dwRapidFailsPerMinute;
    STRU            m_struApplication;
    STRU            m_struArguments;
    STRU            m_struProcessPath;
    BOOL            m_fStdoutLogEnabled;
    STRU            m_struStdoutLogFile;
    DWORD           m_dwProcessesPerApplication;
    BOOL            m_fForwardWindowsAuthToken;
    BOOL            m_fDisableStartUpErrorPage;
    MULTISZ         m_mszRecycleOnFileChangeFiles;
};
