# Atlas
![](Atlas.png?style=centerme)

Atlas is a lightweight Windows Apfell agent written in C#. It was intended to be used as an initial access agent with a focus on stability and flexibility in its configuration. I created the agent in an attempt to learn more about coding in C#, so the code is not optimized or written in the "proper" way. It was a fun project and I hope it may be useful to people for red team assessments and penetration testing.

## Features
- 3.5/4.0 .NET compatibility
- Small assembly size: <54Kb
- Dynamic .NET assembly loading and execution
	- Loaded assemblies are tracked so they only need to be loaded once
- Upload/Download files
- AMSI bypass when loading assemblies
- Proxy support
- Dynamic agent configuration
	- Add/remove C2 servers
	- Change sleep time and jitter
	- Change user-agent and host header
	- Change kill date
	- Modify query parameter for GET requests
	- Modify Proxy settings
- Intelligent C2 server selection
	- Keeps track of failed connections to servers and always chooses server with least failed connections
	- Tracking of failed connections is display with configuration data

## Build Instructions
Then navigate to `Manage Operations > Payload Management` on the Apfell server, and import the `atlas.json` file. This registers the payload with the Apfell server as an externally hosted payload.

You can then register the payload with the Apfell server via `Create Components > Create Payload` on the Apfell server, and stuff the PayloadUUID and other relevant information into `Config.cs`

Additional Atlas specific configuration options in `Config.cs`
```
Param			specify the query parameter to use for GET requests
ChunkSize		specify the chunking size to use for upload/download and command output
UseDefaultProxy		true or false, specify whether to use the system default proxy settings or use below manual settings 
ProxyAddress		specify proxy server address to use for web requests
ProxyUser		specify username to use for authenticating to proxy server
ProxyPassword		specify password to use for authenticating to proxy server
```

Then build the agent on a windows system with visual studio or via Mono on a *nix system. 

Once the agent is built, all that's left is to execute.

## Supported Commands
```
config			dynamically change agent settings during runtime (see below for more detail)
download		download files from target system to apfell server
exit			exit atlas instance via Environment.Exit
jobkill			kill a long running job
jobs			list current running jobs
listloaded		list assemblies loaded via the loadassembly command
loadassembly		load an arbitrary .NET assembly via Assembly.Load and keep track of assembly FullName for later execution
runassembly		execute the entrypoint of a .NET assembly loaded via loadassembly command
upload			upload a file from the apfell server to the target system
```

## Dynamic Configuration

Atlas allows a degree of dynamic configuration of the agent's settings after initial execution. These settings can be changed via the `config` command. below are the options to this command as there are many.
```
config				base command

options:
info				display current agent configuration
domain				option to add/remove C2 domain
	add			add a C2 domain to list of domains
	remove			remove a C2 domain from list of domains (will not let list be less then one domain)
sleep				sleep time between taskings in seconds
jitter				variation in sleep time, specify as a percentage
kill_date			date for agent to exit itself
host_header			host header to use for domain fronting
user_agent			user-agent header for web requests
param				option for query parameter used in GET requests
proxy				option to modify proxy settings
	use_default		true/false, choose whether to use system default settings or manual settings specified in config
	address			address of proxy server
	username		username to authenticate to proxy server
	password		password to authenticate to proxy server

Examples:
config info
config domain add http://hello.world
config sleep 60
config jitter 20
config kill_date 2020-03-01
config host_header cdn.cloudfront.com
config user_agent Mozilla 5.0 IE blah blah blah
config param order
config proxy use_default false
config proxy address 192.168.1.100
config proxy username harm.j0y
config proxy password Liv3F0rTh3Tw!ts
```

## Support Assemblies
Since functionality in Atlas is loaded via .NET assemblies, I have provided three support .NET projects that give Atlas basic RAT capability. Your welcome to use whatever fits your needs, but these are a good start.

`processlist` - This is a basic process listing application based off of [@cobbr's](https://twitter.com/cobbr_io?lang=en) code from [SharpSploit](https://github.com/cobbr/SharpSploit), it will retrieve current running process's PID, PPID, Arch, Name, and Owner.

`run` - This is slightly modified code from [@_RastaMouse](https://twitter.com/_rastamouse?lang=en) which he was kind enough to share after my struggles with named pipes redirection. This assembly executes shell commands while spoofing parent process ID and blocking non microsoft dlls from the process space. Neat little code and seems useful. (this does not execute via `cmd.exe`, if you would like to execute commands through there, start the command with `cmd.exe /c`)
```
Usage:
  run.exe <PPID> <command>
Example:
  run.exe 4343 ipconfig /all
Note: PPID is required for command
```

`apcinject` - Again, not my code. This is a slightly modified version of process injection using QueueUserAPC with a new process and suspended thread created by [@0xthirteen](https://twitter.com/0xthirteen).
```
Usage:
  apcinject.exe <path to target application> <base64 shellcode>
Example:
  apcinject.exe C:\Windows\System32\svchost.exe SGFjayB0aGUgUGxhbmV0IQ==
```

## TODO
- HTTP Profile
- File Browser Commands (ls & rm)
- Process listing hook
- unloadassembly

##  Acknowledgments
This project would not of been possible without the help and support of my coworkers

- [@its_a_feature_](https://twitter.com/its_a_feature_)
- [@djhohnstein](https://twitter.com/djhohnstein)
- [@cobbr_io](https://twitter.com/cobbr_io)
- [@001SPARTaN](https://twitter.com/001spartan)
