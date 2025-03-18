## Azure Functions durable task scheduler

The durable task scheduler is a solution for durable execution in Azure. Durable execution is a fault-tolerant approach to running code that handles failures and interruptions through automatic retries and state persistence. Scenarios where durable execution is required include distributed transactions, multi-agent orchestration, data processing, infrastructure management, etc. Coupled with a developer orchestration framework like Durable Functions or the Durable Task SDKs, the durable task scheduler enables developers to author stateful apps that run on any compute environment without the need to architect for fault tolerance. 

Developer can use the durable task scheduler with the following orchestration frameworks: 
- [Durable Functions](https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-overview) 
- [Durable Task Framework](https://github.com/Azure/durabletask) 
- Durable Task SDKs, also called "portable SDKs"

### Use with Durable Functions and Durable Task Framework
When used with Durable Functions, the durable task scheduler plays the role the "backend provider", where state data is persisted as the app runs. While other backend providers are supported, only the durable task scheduler offers a fully managed experience which removes operational overhead from users. Additionally, the scheduler offers exceptional performance, reliability, and the ease of monitoring orchestrations. Apps that use Durable Functions must be run on the Azure Functions compute platform to have official support. 

The durable task scheduler plays a similar role in the Durable Task Framework as Durable Functions. 

### Use with Durable Task or portable SDKs
The Durable Task SDKs provide a lightweight client library for the durable task scheduler. when running orchestrations, apps using these SDKs would make a connection to the scheduler's orchestration engine in Azure. These SDKs are called "portable" because apps that leverage them can be hosted in various compute environments, such as Azure Container Apps, Azure Kubernetes Service, and Azure App Service. 

![Durable Task Scheduler in all Azure Computes](./media/images/dts-in-all-computes.png)

For more information on how to use the Azure Functions durable task scheduler and to explore its features, please refer to the [official documentation](https://aka.ms/dts-documentation)

## Choosing your orchestration framework
This repo contains samples for all orchestration frameworks that you can use the durable task scheduler with. The following table provides some considerations to help you choose a framework for your scenario:

|Consideration | Portable SDKs | Durable Functions | Durable Task Framework|
|--------------| --------------| ------------------| --------------------- | 
|Hosting option| Any compute environment | Azure Functions | Any compute environment |
|Language support | [.NET](https://github.com/microsoft/durabletask-dotnet/), [Python](https://github.com/microsoft/durabletask-python), Java (coming soon) | [.NET](https://github.com/Azure/azure-functions-durable-extension), [Python](https://github.com/Azure/azure-functions-durable-python), [Java](https://github.com/microsoft/durabletask-java), [JavaScript](https://github.com/Azure/azure-functions-durable-js), [PowerShell](https://github.com/Azure/azure-functions-powershell-worker/tree/dev/examples/durable) | [.NET](https://github.com/Azure/durabletask) |
|Official support| Yes | Yes | No |
|Durable task scheduler emulator| Available | Available |Available |
|Durable task scheduler dashboard| Available | Available* | Available*|
|[Durable Entities](https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-entities)| Not supported | Supported | Not supported|
|Other supported feature(s)| Scheduler| Azure Functions triggers and bindings ||



## Tell us what you think

Your feedback is essential in shaping the future direction of this product. We encourage you to share your experiences, both the good and the bad. If there are any missing features or capabilities that you would like to see supported in the Durable Task Scheduler, we want to hear about them.

> **Note:** This is an early-stage version of the product. You can share your feedback either by dropping us an issue in this repo (other private preview users will see your issue) or send an email to our product managers Nick and Lily ([nicholas.greenfield@microsoft.com](mailto:nicholas.greenfield@microsoft.com); [jiayma@microsoft.com](mailto:jiayma@microsoft.com)).