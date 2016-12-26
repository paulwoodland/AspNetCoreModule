#include "precomp.hxx"

ALLOC_CACHE_HANDLER *       TEST_HANDLER::sm_pAlloc = NULL;
TRACE_LOG *                 TEST_HANDLER::sm_pTraceLog = NULL;
MyThread * m_pThread = NULL;
HANDLE hWakeUpEvent = NULL;
HANDLE hPipe = NULL;
std::mutex access_AsynchTasks;
VOID * WorkItems[255];
int  WorkItemIndex = -1;

void Asynch_Task()
{
    while (true)
    {
        access_AsynchTasks.lock();
        VOID * handler = NULL;
        if (WorkItemIndex != -1)
        {
            handler = WorkItems[WorkItemIndex];
            WorkItemIndex--;
        }
        access_AsynchTasks.unlock();
        if (handler == NULL) //the queue is empty
        {
            //assume that the interruption occurs here (*)
            WaitForSingleObject(hWakeUpEvent, INFINITE);
            continue;
        }
        else
        {
            ((TEST_HANDLER *)handler)->m_pW3Context->PostCompletion(0);
            //((TEST_HANDLER *)handler)->m_pW3Context->IndicateCompletion(REQUEST_NOTIFICATION_STATUS::RQ_NOTIFICATION_PENDING);
            if (hPipe != INVALID_HANDLE_VALUE)
            {
                DWORD dwWritten;
                WriteFile(hPipe,
                    "Hello Pipe\n",
                    12,   // = length of string + terminating '\0' !!!
                    &dwWritten,
                    NULL);
            }
        }
    }

}