### Portable SDKs
In addition to Azure Functions, the durable task scheduler introduces a managed workflow engine and storage provider, decouping it from any compute serivce. Therefore, the durable task scheduler can be leveraged from other Azure services such as Azure Container Apps (ACA), Azure Kubernetes Service (AKS), and Azure App Service. Developers can write their orchestrations as code using the Durable Task SDKs and connect their workloads directly to the Durable Task Scheduler for scheduling and persistence of orchestration state. The Durable Task SDKs are available in multiple programming languages:

- [.NET](https://github.com/microsoft/durabletask-dotnet)
- [Python](https://github.com/microsoft/durabletask-python)
- [JavaScript](https://github.com/microsoft/durabletask-js) - DTS support coming soon.
- [Java](https://github.com/microsoft/durabletask-java) - DTS support coming soon.

> Note: While these SDKs are available for use, they are not yet officially supported by Microsoft Azure support.
