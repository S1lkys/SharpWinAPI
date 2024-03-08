# GetProcessByName

Get process handle(s) from the process name using [NtGetNextProcess](https://processhacker.sourceforge.io/doc/termator_8c_source.html) and [GetProcessImageFileName](https://learn.microsoft.com/en-us/windows/win32/api/psapi/nf-psapi-getprocessimagefilenamea) API calls. 

It returns a list of process handles which you can use for example to get the PIDs using [GetProcessId](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getprocessid):

![img1](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/getprocessbyname/Screenshot_1.png)


## Note

If you prefer Go, you have the same implementation in: [https://github.com/ricardojoserf/go-GetProcessByName/](https://github.com/ricardojoserf/go-GetProcessByName/)
