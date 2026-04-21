## High level requirement
Create mongoDB performance benchmarking test application for various concurrency settings.
At a very high level, we need ability to show various mongodb usage patterns and how they perform with or without changestream being read/streamed. For now we'll only have 2 use cases mentioned in the requirements below but this should be easily extensible and benchmarkable  and show stats for configured load.

## permitted technology stack
.net 10 C# console app
permitted nuget packages: any microsoft certified nuget packages as well as MongoDB official driver package and any BenchmarkDotNet packages.
Assume docker desktop installed; docker containers, docker-compose

## Environment/Infrastructure setup.

1. Everything should run with simple docker compose build instructions.
2. Always maintain update readme.md file.
3. Ensure GitHub copilot workflow initialized with necessary copilot instruction file providing context of where documents are and context of project. 
4. it should have its docker-compose for resurrecting mongoDB server container and running all other necessary tests from application. Password should be   `N05@ssword`. Container port can be anything but the default port to avoid conflict with local mongoDB server instance. Also, server needs change stream capability so it needs required read-replica option chosen.
5. It should have ability to simulate restricted IOPS defaulting to 3000 IOPS (configurable)
6. mechanism ensuring connection string not committed/pushed to git repo by keeping a connection-setting.local file that is ignored.
7. application should create  connection-setting.local if doesn't exist ; pointing to above created docker mongodb container.


## use cases
We need to demonstrate outbox pattern comparison for ddd aggregate (lets say OrderAggregate). We create order along with domain event OrderCreated (this means Order document inserted and event created as per one of the following pattern). Then Order document is loaded, updated with status ready for fulfillment and a new event OrderReadyForFulfilment created.
OrderAggregate need to maintain version integer on document. Events need to show the aggregate version correlation (i.e. if it was created as part of version 0 or 1 etc.)
Case-1: Outbox pattern that uses two phase commits to save domainevent as well as Order in their corresponding collections; Orders and OrderEvents collections.
Case-2: Outbox pattern using optimistic concurrency by version maintained on document. Document itself has domainevent array; everytime new event is appended in the array. 

## performance load
Default performance load configuration:
Load size: 1000 documents
Concurrency: 5
batchsize: 1 document



## github copilot workflow setup and slash commands
Create standard spec-kit kind of slash commands (but don't use spec-kit) for creating requirement validation based on any file provided as requirement input, then create trackable task plan under pending folder from validated requirements and implement-plan which should implement pending plan's pending task iteratively while any task pending or any implementation plan pending. All github copilot interactions should be saved under context folder and resumed next time from last context (save with datetime in name). Developers should be able to handoff work in team and their copilot should be able to understand last things done.
