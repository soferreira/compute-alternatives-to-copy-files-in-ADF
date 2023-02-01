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
- [Use cases requiring a copy activity.](#use-cases-requiring-a-copy-activity)
- [Detailed view on the experiment setup.](#experiment-setup)
- [Brief overview on the different compute options.](#compute-options)
- [Detailed view on the experiment results.](#experiment-results)
- [Conclusion](#conclusion)

Ready to get started? Let's go!

## How does Azure Data Factory cost works

The [documentation](https://learn.microsoft.com/en-us/azure/data-factory/pricing-concepts) provides a good overview on how the cost* is calculated through examples. The key points are:

- Orchestrator cost is calculated based on the number of activities and pipelines runs.

- Compute duration called [DIU](https://learn.microsoft.com/en-us/azure/data-factory/copy-activity-performance#data-integration-units)

*Cost - covering the pipeline execution and the compute resources used to run the pipeline.

With number of activities run in mind, it is suggested to try and reduce the number of activities in a pipeline. This is because the cost is calculated based on the number of activities run. The more activities you have in a pipeline, the more you will pay. For example if you need to copy 100 files, you can use a single copy activity to copy all 100 files. This will be cheaper than using 100 copy activities to copy 1 file each.

## Use cases requiring a copy activity

You receive data from multiple sources, all landing on your storage account. You need to move this from the landing zone to your lake. You can use a copy activity to move the data from the landing zone to the lake. You can also use a copy activity to move data from one location to another in the lake. Copy Activity is a very powerful activity that can be used in many scenarios, it is more useful when you have to move data from one location to another in the lake in bulk.

An addition to this use case could be a scenario where data has to be moved into a virtual network. In such scenarios you will have to use either SHIR or Azure Managed VNet Integration Runtime. The Azure Integration Runtime is not supported in a virtual network.

## Experiment setup

The following resources were created:

- Two storage accounts - source and destination

- Azure Data Factory

- Azure Container Apps - hosting a REST API for copy activity

- Two SHIR nodes we used a quickstart template to create the nodes. The template can be found [here](https://github.com/Azure/azure-quickstart-templates/tree/master/quickstarts/microsoft.compute/vms-with-selfhost-integration-runtime). We used Standard _A4 v2 (4 vcpus, 8 GiB memory)_ VMs.

- Managed identity which was used by the Container Apps to access the storage accounts & Key Vault.

- Key Vault - used to store the connection strings for the storage accounts, used by the Container Apps.

While using SHIR, we did not address full network isolation, as it was not part of the scope of this experiment.

We created a sample container app, which can create the sample files. Each file is with the same size of 21KB. We used this to create 1000, 2000, 5000 and 10,000 files containers.

Few pipelines were created to test the different scenarios. We used manual trigger for all pipeline executions. The pipelines are as follows:

- Copy Activity - Copying files from source to destination using Copy Activity. We used parameters to change the source/target of each execution. We have two instances of this pipeline, one is using Azure Integration Runtime and the other is SHIR.

- Copy using ACA - Copying files from source to destination using a call to web activity to call a REST API. We used parameters to change the source/target of each execution.

Results were taken from the pipeline runs.

![consumption](/images/pipeline_consumption.png)
![details](/images/pipeline_run_details.png)

## Compute options

### Azure Integration Runtime

Most common compute option for Azure Data Factory. It is a managed compute, hosted in Azure, that can connect to public datastores. If you need to connect to private datastores, you can whitelist the IP address ranges published for the service, but this is not desirable in many scenarios from a security perspective. It is used to run the copy activity among many other activities. It is a shared resource, which means that multiple pipelines can use the same Azure Integration Runtime.

### Azure Container Apps

In the context of the experiment, we leveraged Azure Container Apps to create a REST API. The REST API was used to copy files from one location to another. The API was implementing using the 202 pattern and the pipeline was configured to ignore the async response. This means that the time taken to copy the files was not considered in the experiment. __This is the reason we did not include the results for this option in the experiment results.__

### Self Hosted Integration Runtime

The same service can be hosted by customers on thier own compute. Users can create the integration runtime service on stand alone compute, or reuse exsiting capacity. We used a dedicated 2 nodes cluster to run the copy activity. We used this [Quickstart](https://github.com/Azure/azure-quickstart-templates/tree/master/quickstarts/microsoft.compute/vms-with-selfhost-integration-runtime) to create all required resources for the SHIR.

## Experiment results

We ran each type with multiple number of files to copy, the number of files per experiment were 1000, 2000, 5000 and 10,000. 

### Cost Calculation

We are shoing both the estimated cost per single pipeline run and the cost for 1000 runs of a pipeline in a month.
All Prices are in USD. We used 'West Europe' as the region for all resources. Pricing details are taken from [here](https://azure.microsoft.com/en-us/pricing/details/data-factory/).

#### Using Azure Integration Runtime

$$ cost = {ActivityRuns * 1.0 + 1000 * DIUHours * 0.25 + Total Time[hours] * 0.005  } $$


|Experiment| DIU | Activity Duration [sec]| Activity Runs| DIU Hour| Total Cost | Cost per 1000| Total Time [sec]|
|----------|-----|------------------------|--------------|---------|------------|--------------|-----------------|
|1000 Files|4|	26|	1|	0.0667|	0.026718056|	17.71805556	|31|
|2000 Files|4|	42|	1|	0.0667|	0.026738889|	17.73888889	|46|
|5000 Files|4|	78|	1|	0.1333|	0.043438889|	34.43888889|	82|
|10000 Files|4|	180|	1	|0.2	|0.060254167	|51.25416667	|183|

#### Using Self Hosted Integration Runtime

$$ cost = {ActivityRuns * 1.0 + 1000 * DIUHours * 0.002 + Total Time[hours] * 0.002 + External Activity Runs * 0.0001 + XComputeTime[min] * 0.01  } $$

XComputeTime is the time taken to run the copy activity on the SHIR nodes. With the VMs we used, the compute time was 0.01 per minute for 2 nodes.

|Experiment|Activity Duration [sec]| Activity Runs| External Activity Runs| SHIR runs|	X-Compute Cost	|Total Cost| Cost per 1000|	Total Time [sec]|
|----------|-----------------------|--------------|-----------------------|----------|-----------------|----------|--------------|-----------------|
|1000 Files|55|	1|	0.0167	|1	|0.01	|0.020032226	|11.53222556	|55|
|2000 Files|96|	1|	0.0333	|1	|0.02	|0.030056663	|21.55666333	|96|
|5000 Files|197|	1|	0.0667	|1	|0.04	|0.050116114	|41.61611444	|197|
|10000 Files|397|	1|	0.1333	|1	|0.05	|0.060151663	|51.65166333	|249|

#### Using Managed VNet Integration Runtime

$$ cost = {ActivityRuns * 1.0 + 1000 * DIUHours * 0.25 + Total Time[hours] * 1  } $$

We have have used the time taken to run the copy activity on the Azure IR.

|Experiment|DIU|	 Activity Duration [sec]|	Activity Runs| 	DIU Hour| Cost per 1000| Total Time [sec]|	Cluster Startup (min)|
|----------|---|---------------------------|----------------|----------|--------------|-----------------|-----------------------|
|1000 Files|4|	26|	1|	0.0667|	42.95277778	|31|	1|
|2000 Files|4|	42|	1|	0.0667|	47.11944444	|46|	1|
|5000 Files|4|	78|	1|	0.1333|	73.76944444|	82|	1|
|10000 Files|4	|180	|1	|0.2	|118.5	|183	|1|

## Conclusion
