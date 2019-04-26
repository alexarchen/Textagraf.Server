# Installing

## IIS
 
 Configure IIS 

 appcmd.exe set config -section:system.webserver/serverruntime /uploadreadaheadsize:1048576 /commit:apphost