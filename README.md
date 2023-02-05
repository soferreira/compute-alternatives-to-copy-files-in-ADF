# Research on compute alternatives to copy files in Azure Data Factory

In this repository, we will compare the various compute options we can leverage to copy files in __Azure Data Factory__. The compute options we will compare are Azure Integration Runtime, Self Hosted Integration Runtime (SHIR) on Azure VMs, and Managed VNet Integration Runtime. It is important to note that we will be copying files between two cloud data stores. If the data store were located on-premises, install the SHIR on commodity hardware on-premises would be more appropriate.

Our comparison will cover into the following aspects:

- Performance
- Cost
- Operational aspects

The copy alternatives we will cover are:

- Using Azure Data Factory Copy Activity
- Using a Azure Data Factory web activity to call a REST API - we will leverage Azure Container Apps (ACA) to create a REST API.

__What would be covered?__

- [How does Azure Data Factory cost work.](#how-does-azure-data-factory-cost-works)
- [Use cases requiring a copy activity.](#use-case)
- [Detailed view on the experiment setup.](#experiment-setup)
- [Brief overview on the different compute options.](#compute-options)
- [Detailed view on the experiment results.](#experiment-results)
- [Conclusion](#conclusion)

Ready to get started? Let's go!

## How does Azure Data Factory cost works

The [documentation](https://learn.microsoft.com/en-us/azure/data-factory/pricing-concepts) provides a good overview on how the cost* is calculated through examples. The key points are:

- Orchestrator cost is calculated based on the number of activities and pipelines runs.

- Compute duration called [DIU](https://learn.microsoft.com/en-us/azure/data-factory/copy-activity-performance#data-integration-units).

*Cost - covering the pipeline execution and the compute resources used to run the pipeline.

With number of activities run in mind, it is suggested to try and reduce the number of activities in a pipeline. This is because the cost is calculated based on the number of activities executions. The more activities you have in a pipeline, the more you will pay. For example if you need to copy 100 files, you can use a single copy activity to copy all 100 files. This will be cheaper than using a loop over 100 copy activities to copy 1 file each.

## Use case

You receive data from multiple sources, which are all are landing in your storage account. You need to copy this data from the landing zone to your data lake.

## Experiment setup

The following resources were created:

- Two storage accounts - source and destination. In most cases both would be within the same region.

- Sample data - we created a sample container app, which can create the sample files. Each file is with the same size of 21KB. We used this to create 1000, 2000, 5000 and 10,000 files containers in our source storage account.

- Azure Data Factory - we created multiple pipelines to test the different scenarios. We used manual trigger for all pipeline executions. The pipelines are as follows:
  - Pipeline using a Copy Activity- Copying files from source to destination using a Copy Activity. We leveraged pipeline parameters to change the source/target of each execution. We have two instances of this pipeline, one is using Azure Integration Runtime and the other is using Self Hosted Integration Runtime (SHIR).
  - Pipeline using a Web Activity - Copying files from source to destination using a Web  Activity + ACA.

- Azure Container Apps - hosting a REST API to copy files.
    - Copy using ACA - Copying files from source to destination using a Web Activity to call a REST API. We leveraged pipeline parameters to change the source/target of each execution.
    - Using an external call, is cost-effective when looping over large number of items with small size. In the context of the experiment, we leveraged Azure Container Apps to create a REST API. The REST API was used to copy files from one location to another. The API was implemented using the __202 Accepted__ pattern and the pipeline was configured to ignore the async response. This means that the time taken to copy the files is not included on the pipeline duration time and it was not considered in the experiment results. Therefore we __did not__ include it in the experiment results.

- Two SHIR nodes - We used a quickstart template to create the nodes. The template can be found [here](https://github.com/Azure/azure-quickstart-templates/tree/master/quickstarts/microsoft.compute/vms-with-selfhost-integration-runtime). We used Standard _A4 v2 (4 vcpus, 8 GiB memory)_ VMs.
    >__NOTE:__ The experiment does not support full network isolation, as it was not part of the scope of this experiment.

- Managed Identity - used by the Azure Container Apps to access the storage accounts & Key Vault.

- Key Vault - used to store the connection strings for the storage accounts, used by the Container Apps.

## Compute options

### Azure Integration Runtime

Most common compute option for Azure Data Factory. It is a managed compute, hosted in Azure, that can connect to public data stores. If you need to connect to private data stores, you can whitelist the IP address ranges published for the service, but this is not desirable in many scenarios from a security perspective. It is used to run the copy activity among many other activities. It is a shared resource, which means that multiple pipelines can use the same Azure Integration Runtime.

>__NOTE__: If data needs to be copied into (or from) a virtual network, we encourage you to use either SHIR or Azure Managed VNet Integration Runtime. The Azure Integration Runtime is not supported in a virtual network.

### Self Hosted Integration Runtime

The same service can be hosted by you on your own compute. Users can create the integration runtime service on stand alone compute, or reuse existing capacity. In this experience, we used a dedicated 2 nodes cluster to run the copy activity. We used this [Quickstart](https://github.com/Azure/azure-quickstart-templates/tree/master/quickstarts/microsoft.compute/vms-with-selfhost-integration-runtime) to create all required resources for the SHIR.

### Managed VNet Integration Runtime

The Managed VNet Integration Runtime is a compute option that is managed and hosted within your virtual network in Azure. This allows you to create private endpoint between the IR and your data store. When choosing the managed VNet IR you get a secure, fully managed, fully isolated, and highly available compute option.

## Experiment results

Result values show in the tables bellow were taken from the pipeline run details and consumption.

### Cost Calculation

We are showing both the estimated cost per single pipeline run and the cost for 1000 runs of a pipeline in a month.
All Prices are in USD. We used 'West Europe' as the region for all resources. Pricing details are taken from [here](https://azure.microsoft.com/en-us/pricing/details/data-factory/).

#### Using Azure Integration Runtime

##### Pipeline using a Copy Activity

$$ Cost/1000Runs = {ActivityRuns * 1.0 + 1000(DIUHour * 0.25 + Activity Duration[hours] * 0.005)} $$

|Experiment|DIU |Activity Duration [sec]|Activity Runs|DIUHour|Cost/Run|Cost/1000 Runs|
|----------|-----|----------------------|-------------|-------|-----|------------------|
|1000 Files|4|	26|	1|	0.0667|	0.027|	17.71	|
|2000 Files|4|	42|	1|	0.0667|	0.027|	17.73	|
|5000 Files|4|	78|	1|	0.1333|	0.043|	34.43|
|10000 Files|4|	180|1|0.2	|0.06|51.25	|

##### Pipeline using a Web Activity + ACA

$$ Cost/1000Runs = {ActivityRuns[inThousands/month] * 1000( 1.0 + External Activity Runs* 0.00025 + Activity Duration[hours] * 0.005)} $$

|Experiment|Activity Duration [sec]|Activity Runs|External Activity Runs|Cost/Run|Cost/1000 Runs|
|----------|-----------------------|-------------|----------------------|----|------------------|
|1000 Files|	14|	1|0.0167|	0.01|	1.02|
|2000 Files|	14|	1|0.0167|	0.01|	1.02|
|5000 Files|	14|	1|0.0167|	0.01|	1.02|
|10000 Files|	14|	1|0.0167|0.01   |   1.02|

>Note: The cost of the ACA is __not included__ in the experiment results. The pricing calculator can be found [here](https://azure.microsoft.com/en-us/pricing/details/container-apps/).

#### Using Self Hosted Integration Runtime

##### Pipeline using a Copy Activity

$$
Cost/1000Runs = {
ActivityRuns * 1.0 + 1000(DIUHour * 0.002 + Activity Duration[hours] * 0.002 + External Activity Runs * 0.0001 + XComputeTime[min] * 0.01)}
$$

XComputeTime is the time taken to run the copy activity on the SHIR nodes. With the VMs we used, the compute time was 0.01 per minute for 2 nodes.

|Experiment|Activity Duration [sec]| Activity Runs| External Activity Runs| SHIR Runs| X-Compute Cost|Cost/Run| Cost/1000 Runs|
|----------|-----------------------|--------------|-----------------------|----------|---------------|----|-------------------|
|1000 Files| 55|	1|	0.0167	|1	|0.01	|0.02	|11.53	|
|2000 Files|96|	1|	0.0333	|1	|0.02	|0.030	|21.55	|
|5000 Files|197|	1|	0.0667	|1	|0.04	|0.05	|41.61|
|10000 Files|397|	1|	0.1333	|1	|0.05	|0.06	|51.73	|

#### Using Managed VNet Integration Runtime

##### Pipeline using a Copy Activity

$$Cost/1000Runs = {ActivityRuns * 1.0 + 1000(DIUHour * 0.25 + Activity Duration[hours] * 1)} $$

We have have used the time taken to run the copy activity on the Azure IR.

|Experiment|DIU|Activity Duration [sec]|Activity Runs| DIU-Hour| Cost/1000 Runs|Cluster Startup [sec]|
|----------|---|-----------------------|-------------|---------|-------------------|---------------------|
|1000 Files|4|	26|	1|	0.0667|	41.56	|	60|
|2000 Files|4|	42|	1|	0.0667|	46.01|	60|
|5000 Files|4|	78|	1|	0.1333|	72.66|60|
|10000 Files|4	|180	|1	|0.2	|117.67|60|

## Conclusion

The first conclusion is that it is always better to test your hypothesis before reaching to conclusions. Our hypothesis was that using Self-Hosted-Integration-Runtime would be the most cost effective approach. The experiment results shows that this is not always the case.

The second conclusion, is that each workload __must__ be examined individually. When choosing your compute option, you would need to understand the cost elements, extapulate with your data, and choose the best option for your workload.

Yes, in many cases using SHIR is an easy to switch between Azure IR or Managed VNet IR. Using SHIR brings addtional cost factors, and dependant on your company or project, the addtional cost factors are maintaing the SHIR.

In the specific use case of processing large number of individual files, using an activity for that could be less effective than delegating to another compute option such as Azure Container App, or Azure Functions. It does bring coding complexities to the pipeline, but it can be a good option to consider.