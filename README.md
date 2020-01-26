# Atlas

Atlas is a PoC agent for Apfell written in C# and targeted towards Windows 10+ OS's and .NET framework v4.0 (may work on v3.5 but hasn't been throughly tested). The agent has minimal functionality, only allowing 3 commands, `loadassembly`, `runassembly`, `listloaded`, and `exit`. Below is the purpose and uses for each command, followed by instructions for loading the agent into Apfell. Eventually this will be a docker container within Apfell, using Mono to compile the agents, it's a TODO item.

`loadassembly` - This command is used to load any arbitrary .NET assembly file into the agent's AppDomain for future use. Need's more testing with larger files but _should_ chunk files correctly and handle them accordingly. This method uses the `Assembly.Load` function to dynamically the load the assemblies into the agent's AppDomain. Assembly name's are added to a `Modules` list to keep track of loaded assemblies, this should help avoid loaded duplicate assemblies in the same agent.

`runassembly` - This command is used to execute a loaded .NET assembly from he list of loaded assemblies. For successful execution the assembly MUST have a valid `EntryPoint`. Console output from executed assemblies will be redirected and sent back to the Apfell server and command output. This command will only execute assemblies in the `Modules` list of loaded assemblies, ie. you have to use `loadassembly` to load an assembly before you can execute it with `runassembly`.

`listloaded` - This command will get the name's of assemblies currently in the agent's `Modules` list of assemblies loaded with `loadassembly`.

`exit` - This command simply exit's the process for the agent using `Environment.Exit`.

## How to setup with Apfell
Atlas currently is only equipped to be an `External` payload for Apfell. A Payload docker container is in the works. The follow steps describe how to import Atlas into Apfell to use as an agent.

1. Navigate to `Manage Operations` -> `Payload Management`, to the right of `Global Payload Type and Command Information` there should be a blue `import` button. Select this and select the `atlas.json` file from the Atlas project folder.
2. Next, navigate to `Manage Operations` -> `Transform Management`, and copy the contents of `transforms.py` from the Atlas project folder into the displayed input box. You can _optionally_ upload the file and submit the uploaded code if you would not like to copy/paste.
3. Next, navigate to `Manage Operations` -> `C2 Profiles Management`, select the yellow `Edit` button next to the `default` C2 profile and select `Supported Payloads & info`. In this pop out, you will see a `Supported Payloads` section, ensure to click the button next to `atlas` so that it shows green.
4. For the final setup inside of Apfell, navigate to `Create Components` -> `Create Payload`, select `default` from the C2 profile drop down menu, and fill in the data for the agent. Copy the value for the `AES key` for the agent config later IF you are going to use AES encryption or the RSA EKE features of the agent. When finished, select `Select Payload Type`.
>Note: This values will be manually enter into the agent's config, the only required items on the Apfell side are `Base64 32byte AES Key`, `callback host`, `Encrypted Key Exchange (T/F)`, and `Kill Date`.
5. Select `atlas` from the drop down menu and click `Next`.
6. All available commands will be auto-selected for you, simply click `Next`. Then select the `Create Payload` button at the bottom of the screen. Once the payload is created you should see a pop up message in the top right of the browser giving the payload's `UUID`. Copy this where you saved the `AES Key` information to put into the agent's config.
7. Now open the `Atlas.sln` Visual Studio project file (created with Visual Studio 2017). In the `Solution Explorer` pane on the right side, select the `Config.cs` file under the `Atlas` project. Fill out the configuration data in this file. Ensure the `UUID` you got from creating a payload in Apfell is entered into the `PayloadUUID` variable.
>Note: Enter the callback host in the `Servers` array. Atlas allows you to specify multiple callback hosts in this array, but will only select one as a primary C2 domain when a successful connection attempt is made.

>Note: For the `Jitter` config option, enter this number as if it were a percentage, so `30%` jitter would just be `30`
9. At the top of the `Config.cs`, `Http.cs`, and `Crypto.cs` files, you will see a `#define DEFAULT` code line. This let's Atlas determine what code to keep during compilation to reduce extra code. Change this value to fit the communication type you configured in Apfell (`DEFAULT`, `DEFAULT_PSK`, `DEFAULT_EKE`).
10. When all config variables have been set, select the `Build` drop down from the top options menu and select `Build Solution`. This should produce a .NET assembly of your Atlas agent.

## TODO
- Create Payload docker container to fully integrate into Apfell
- Create plumbing for HTTP C2 profile use
- Create `unloadassembly` function to unload assembly from agents AppDomain. @thewover suggested looking into these [APIs](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/debugging/) with examples [here](https://github.com/lowleveldesign/mindbg)
- Add in preprocessing transforms for obfuscation (ConfuserEx? XOR strings? etc..)
