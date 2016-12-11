// NativeAPITest.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "ProcessStarter.h"
#include "string"
using namespace std;

int ProcessInformation();
int main()
{
    ProcessInformation();
    wstring processPath = L"c:\\windows\\system32\\notepad.exe";
    ProcessStarter starter(processPath, L"");
    _bstr_t strUser;
    _bstr_t strdomain;
    auto pid = GetCurrentProcessId();
    starter.GetUserFromProcess(pid, strUser, strdomain);

    starter.Run();
    return 0;
}

