## Azure Functions durable task scheduler

The durable task scheduler is a fully managed backend for durable execution in Azure. Durable execution is a fault-tolerant approach to running code that handles failures and interruptions through automatic retries and state persistence. Coupled with a developer orchestration framework, like Durable Functions or the Durable Task SDKs (portable sdks), the durable task scheduler enables developers to write stateful orchestrations within compute environments, without the need to architect for fault tolerance. It offers exceptional performance, reliability, and the ease of monitoring stateful orchestrations, regardless of where your applications are hosted in Azure.

Author your orchestrations as code using Durable Functions or Durable Task SDKs. Connect your workloads to the durable task scheduler, which handles orchestrations and task scheduling, persists orchestration state, manages orchestration and task failures, and load balances orchestration execution at scale. These capabilities significantly reduce operational overhead for developers, allowing them to focus on delivering business value. The choice of developer orchestration framework depends on where your applications are hosted: use Durable Functions in Azure Functions, or the Durable Task SDKs (portable SDKS) in Azure Container Apps, Azure Kubernetes Service, App Service, etc.

For more information on how to use the Azure Functions durable task scheduler and to explore its features, please refer to the [official documentation](https://aka.ms/dts-documentation)

![Durable Task Scheduler in all Azure Computes](./media/images/dts-in-all-computes.png)

## Tell us what you think

Your feedback is essential in shaping the future direction of this product. We encourage you to share your experiences, both the good and the bad. If there are any missing features or capabilities that you would like to see supported in the Durable Task Scheduler, we want to hear about them.

> **Note:** This is an early-stage version of the product. You can share your feedback either by dropping us an issue in this repo (other private preview users will see your issue) or send an email to our product managers Nick and Lily ([nicholas.greenfield@microsoft.com](mailto:nicholas.greenfield@microsoft.com); [jiayma@microsoft.com](mailto:jiayma@microsoft.com)).