// NativeAPITest.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "ProcessStarter.h"
#include "string"
#include <thread>
#include <iostream>

using namespace std;

int ProcessInformation();
int JobObject();
void IISLibTest();

void Task(int i)
{
    for (int i=0;i<5000;i++)
        cout << "test" << endl;
}


int main()
{
    thread t(Task, 1);
    for (int i = 0; i<5000; i++)
        cout << "main" << endl;
        
    /*
    IISLibTest();
    
    JobObject();
	ProcessInformation();

	wstring processPath = L"c:\\windows\\system32\\notepad.exe";
    ProcessStarter starter(processPath, L"");
    _bstr_t strUser;
    _bstr_t strdomain;
    auto pid = GetCurrentProcessId();
    starter.GetUserFromProcess(pid, strUser, strdomain);

    starter.Run(); */
    t.join();
    return 0;
}

