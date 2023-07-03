# process_tracker
Console application that creates and tracks process sessions on a device. This application was created as a "learn to program" tutorial I streamed several years ago. 

This application reads running processes on the device and creates a process session. The purpose of the session is to log the amount of time the machine is using various processes. In the demonstration, the goal was to show the audience how coding could be used to track the amount of time playing certain games like Escape from Tarkov, PUBG, and more. We essentially recreated the "Time Played" feature of Steam.  The application logs these sessions to a local sqlite database for persistence. The application database was subsequently used to demonstrate basic usage of SQL. 
