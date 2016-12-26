#pragma once
#include <thread>
#include <string>
using namespace std;

class MyThread : public thread
{
public:
    MyThread()
    {
    }
    ~MyThread()
    {
    }
};

void Asynch_Task();
extern MyThread * m_pThread;
extern HANDLE hWakeUpEvent;
extern HANDLE hPipe;
extern std::mutex access_AsynchTasks;
extern VOID * WorkItems[255];
extern int  WorkItemIndex;

class TEST_HANDLER
{
public:

    IHttpContext *                      m_pW3Context;
    IHttpContext *                      m_pChildRequestContext;
    SRWLOCK                             m_RequestLock;
    static ALLOC_CACHE_HANDLER *        sm_pAlloc;
    mutable LONG                        m_cRefs;
    static TRACE_LOG *                  sm_pTraceLog;
    
    TEST_HANDLER(__in IHttpContext * pW3Context) : m_pW3Context(pW3Context), m_cRefs(1)
    {
        InitializeSRWLock(&m_RequestLock);
    }

    virtual ~TEST_HANDLER()
    {
        m_pW3Context = NULL;
    }

    static VOID StaticTerminate()
    {
        if (sm_pAlloc != NULL)
        {
            delete sm_pAlloc;
            sm_pAlloc = NULL;
        }

        if (m_pThread != NULL)
        {
            m_pThread->join();
            delete m_pThread;
            m_pThread = NULL;
        }
        if (hWakeUpEvent != INVALID_HANDLE_VALUE)
        {
            CloseHandle(hWakeUpEvent);
        }

        if (hPipe != INVALID_HANDLE_VALUE)
        {
            CloseHandle(hPipe);
        }
    }
    
    static HRESULT StaticInitialize(BOOL fEnableReferenceCountTracing)
    {
        HRESULT                         hr = S_OK;

        sm_pAlloc = new ALLOC_CACHE_HANDLER;
        if (sm_pAlloc == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Failure;
        }

        hr = sm_pAlloc->Initialize(sizeof(FORWARDING_HANDLER),
            64); // nThreshold
        if (FAILED(hr))
        {
            goto Failure;
        }

        m_pThread = (MyThread *) new thread(Asynch_Task);
        hWakeUpEvent = CreateEvent(NULL, FALSE, FALSE, L"AsynchTask");
        if (FAILED(hr))
        {
            goto Failure;
        }

        hPipe = CreateFile(TEXT("\\\\.\\pipe\\Pipe"),
            GENERIC_READ | GENERIC_WRITE,
            0,
            NULL,
            OPEN_EXISTING,
            0,
            NULL);
        if (FAILED(hr))
        {
            goto Failure;
        }

        return S_OK;

    Failure:

        StaticTerminate();

        return hr;
    }       

    static void * operator new(size_t size)
    {
        DBG_ASSERT(sm_pAlloc != NULL);
        if (sm_pAlloc == NULL)
        {
            return NULL;
        }
        return sm_pAlloc->Alloc();
    }

    static void operator delete(void * pMemory)
    {
        DBG_ASSERT(sm_pAlloc != NULL);
        if (sm_pAlloc != NULL)
        {
            sm_pAlloc->Free(pMemory);
        }
    }

    VOID ReferenceForwardingHandler() const
    {
        LONG cRefs = InterlockedIncrement(&m_cRefs);
        if (sm_pTraceLog != NULL)
        {
            WriteRefTraceLog(sm_pTraceLog,
                cRefs,
                this);
        }
    }

    VOID DereferenceForwardingHandler() const
    {
        DBG_ASSERT(m_cRefs != 0);

        LONG cRefs = 0;
        if ((cRefs = InterlockedDecrement(&m_cRefs)) == 0)
        {
            delete this;
        }

        if (sm_pTraceLog != NULL)
        {
            WriteRefTraceLog(sm_pTraceLog,
                cRefs,
                this);
        }
    }

    REQUEST_NOTIFICATION_STATUS OnExecuteRequestHandler()
    {
        REQUEST_NOTIFICATION_STATUS retVal = RQ_NOTIFICATION_CONTINUE;
        HRESULT                     hr = S_OK;
        bool                        fRequestLocked = FALSE;
        ASPNETCORE_CONFIG          *pAspNetCoreConfig = NULL;
        FORWARDER_CONNECTION       *pConnection = NULL;
        STACK_STRU(strDestination, 32);
        STACK_STRU(strUrl, 2048);
        STACK_STRU(struEscapedUrl, 2048);
        STACK_STRU(strDescription, 128);
        HINTERNET                   hConnect = NULL;
        IHttpRequest               *pRequest = m_pW3Context->GetRequest();
        IHttpResponse              *pResponse = m_pW3Context->GetResponse();
        SERVER_PROCESS             *pServerProcess = NULL;
        USHORT                      cchHostName = 0;
        BOOL                        fSecure = FALSE;
        BOOL                        fProcessStartFailure = FALSE;
        HTTP_DATA_CHUNK            *pDataChunk = NULL;

        DBG_ASSERT(m_RequestStatus == FORWARDER_START);
        
        //
        // Take a reference so that object does not go away as a result of
        // async completion.
        //
        ReferenceForwardingHandler();

        hr = AppendLineToResponse("OnExecuteRequestHandler()<BR>");

    Finished:
        DereferenceForwardingHandler();

        access_AsynchTasks.lock();
        WorkItemIndex++;
        WorkItems[WorkItemIndex] = (VOID *) this;
        access_AsynchTasks.unlock();
        SetEvent(hWakeUpEvent);
        
        retVal = RQ_NOTIFICATION_PENDING;

        return retVal;
    }

    REQUEST_NOTIFICATION_STATUS OnAsyncCompletion(DWORD cbCompletion, HRESULT hrCompletionStatus)        
    {
        HRESULT                     hr = S_OK;
        REQUEST_NOTIFICATION_STATUS retVal = RQ_NOTIFICATION_CONTINUE;
        BOOL                        fLocked = FALSE;
        bool                        fClientError = FALSE;
        DBG_ASSERT(m_pW3Context != NULL);
        __analysis_assume(m_pW3Context != NULL);

        ReferenceForwardingHandler();
        hr = AppendLineToResponse("OnAsyncCompletion()<BR>");

    Finished:
        DereferenceForwardingHandler();

        //
        // Do not use this object after dereferencing it, it may be gone.
        //

        return retVal;
    }

private:

    HRESULT AppendLineToResponse(PCSTR szBuffer)
    {
        return WriteResponse(szBuffer, false);
    }

    HRESULT WriteResponse(PCSTR szBuffer, bool clearPage)
    {
        HRESULT hr = S_OK;
        IHttpResponse * pHttpResponse = m_pW3Context->GetResponse();

        HTTP_DATA_CHUNK dataChunk;
        if (clearPage)
        {
            pHttpResponse->Clear();
        }
        dataChunk.DataChunkType = HttpDataChunkFromMemory;
        dataChunk.FromMemory.pBuffer = (PVOID)szBuffer;
        dataChunk.FromMemory.BufferLength = (ULONG)strlen(szBuffer);
        hr = pHttpResponse->WriteEntityChunkByReference(&dataChunk, -1);
        if (FAILED(hr))
        {
            return -1;
        }
        return 0;
    }
};