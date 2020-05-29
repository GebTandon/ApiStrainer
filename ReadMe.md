
## ToDo ##
- Add Sliding Window Token Repo.
- Use Fody for Aspect Weaving around Flurl, WebClient and such high level API Call objects.
- Create another ITokenRepo implementation that uses sliding window concept.
- The projects have hard references, instead of Nuget package reference among themselves.
- The 'TokenGen.ConsoleApp' project is the example of how inprocess clients can utilize the service. 
- The 'TokenDispenser.ClientLib.ConsoleApp' project is the example of how out of process clients can utilize the service.
- The 'TokenDispnser' is the gRPC server that issues tokens and protects the target APIs from rate/ call overloading.
- The IKeepStats implementation needs some rethinking, so that 
  - it can keep separate counts based on whether the event source is ILimitRate or ILimitWindow
  - this will also require some re-thinking of ITokenRepository interface especially to distribute the events in other marker interfaces (ILimitRate or ILimitWindow).
- Add Unit tests for Validation logic
- Add Integration tests for the server project.
- Add Integration tests for the client project.
- Currently in all these implementations, there is possibility that users can make mistakes in typing servername or client names in different ways.
  - Mistake naming service with different names will be disastrous as the counters will not match and may allow more calls then allowed.
- Improvements needed:
  - Embedded TokenGen
    - Add attributes to Flurl or WebClient where we can automatically get the domain name and set it as servername
    - Similarly, extract client name from the application name.
  - Remote TokenGen
    - Allow different servers to expose diff ports.
    - Allow clients to connect at those diff ports.
- Attribute for remote server too.
- What is there is no DI
- What if client wants to use different DI container.

## How to Use ##
- The TokeGenLib is the library that is core of the project.
  - Keeps track of the APIs, tracks Instantaneous API call counts and Total counts within a time frame.
- The tracking of APIs can be done in process as well as out of process.
  - InProcess - add reference to the TokenGenLib directly in the project
    - See TokenGen.ConsoleApp project, it uses multiple in-process Api tracking code.
    - To track only one API server, use RegisterTokenMonitor.Register calls with that server name.
    - To Track multiple API servers, call RegisterTokenMonitor.Register multiple times for each server tracked.
  - Out of Process - Run the TockenDispenser instance, and the TokenDispenser.ClientLib.ConsoleApp library.
    - To track one server, instantiate only one TockenDispenser server.
    - To track multiple servers, instantiate multiple of such servers.
- The ApiCallTrackerAttribute attribute is single use, this will promote users to keep each API calling methods separate, so the token are released quicker..
- The Reporting APIs are under development, and not fully flushed out.

