### Azure Durable Functions
[Durable Functions](https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-overview) is an extension of Azure Functions that lets you write stateful functions in a serverless compute environment. It allows developers to define stateful operations (durable executions) by writing orchestrations and stateful entities using the Azure Functions programming model. 

Durable Functions works with all Azure Functions programming languages, including .NET, JavaScript, Python, PowerShell, and Java. It supports multiple application patterns such as function chaining, fan-out/fan-in, async HTTP APIs, human interaction, and more.

Durable Functions leverages a backend component, which refers to the storage provider that is used to schedule orchestrations and tasks, as well as persist the state of orchestrations and entities. The samples in this repository for Durable Functions illustrate how to effectively utilize the durable task scheduler within Azure Functions for durable function applications.
