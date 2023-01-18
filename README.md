# Copy Alternatives - Research

In this repository, we will compare the various options to __Azure Data Factory__ copy activities. As there are few scenarios where using default values can create challenges with running these pipelines in scale. for each of the options we will run using Azure Integration Runtime, Self Hosted Integration Runtime and Azure Data Factory Managed Integration Runtime. We will review:

- Performance

- Cost

- Operational aspects

The alternatives we will cover are:

- Using Azure Data Factory Copy Activity 
- Using a call to web activity to call a REST API - we will leverage Azure container apps to create a REST API.

__What would be covered?__

- The use cases requiring a copy activity.

- Detailed view on the experiment setup.

- Brief overview on the different compute options. (Azure Data Factory Managed Integration Runtime, Self Hosted Integration Runtime, Azure Integration Runtime)

- Detailed view on the experiment results.

- Conclusion

Ready to get started? Let's go!

## How does Azure Data Factory cost work?

The [documentation](https://learn.microsoft.com/en-us/azure/data-factory/pricing-concepts) provides a good overview on how the cost* is calculated through examples. The key points are:

- Orchestrator cost is calculated based on the number of activities and pipelines runs.

- Compute duration called [DIU](https://learn.microsoft.com/en-us/azure/data-factory/copy-activity-performance#data-integration-units)


*Cost - covering the pipeline execution and the compute resources used to run the pipeline.

With number of activities run in mind, it is suggested to try and reduce the number of activities in a pipeline. This is because the cost is calculated based on the number of activities run. The more activities you have in a pipeline, the more you will pay. For example if you need to copy 100 files, you can use a single copy activity to copy all 100 files. This will be cheaper than using 100 copy activities to copy 1 file each.

## Use cases requiring a copy activity


You receive data from multiple sources, all landing on your storage account. You need to move this from the landing zone to your lake. You can use a copy activity to move the data from the landing zone to the lake. You can also use a copy activity to move data from one location to another in the lake. Copy Activity is a very powerful activity that can be used in many scenarios, it is more useful when you have to move data from one location to another in the lake in bulk. 

An addtion to this use case could be a scenario where data has to be mnoved into a virtual network. In such scenarios you will to use either Self Hosted Integration Runtime or Azure Managed Integration Runtime. The Azure Integration Runtime is not supported in a virtual network.


## Experiment setup

The following resources were created:

- Two storage accounts - source and destination

- Azure Data Factory

- Azure Container Apps - hosting a REST API for copy activity

- Two Azure Data Factory Self-Hosted Integration Runtime nodes we used a quickstart template to create the nodes. The template can be found [here](https://github.com/Azure/azure-quickstart-templates/tree/master/quickstarts/microsoft.compute/vms-with-selfhost-integration-runtime). We used Standard _A4 v2 (4 vcpus, 8 GiB memory)_ VMs.

- Managed identity which was used by the Container Apps to access the storage accounts & Key Vault.

- Key Vault - used to store the connection strings for the storage accounts, used by the Container Apps.

While using Self-Hosted Integration Runtime, we did not address full network isolation, as it was not part of the scope of this experiment. 

We created a sample container app, which creates can create the sample files. each file is with the same size of 21KB. We used this to create 1000, 2000, 5000 and 10,000 files containers.

Few pipelines were created to test the different scenarios. We usede manual trigger for all pipeline executions. The pipelines are as follows:

- Copy Activity - Copying files from source to destination using Copy Activity. We used parameters to change the source/target of each execution. We have two instances of this pipeline, one is using Azure Integration Runtime and the other is using Azure Data Factory Self-Hosted Integration Runtime.

- Copy using ACA - Copying files from source to destination using a call to web activity to call a REST API. We used parameters to change the source/target of each execution. 

Results were taken from the pipeline runs. 

## Compute options

### Azure Integration Runtime

### Azure Container Apps

### Self Hosted Integration Runtime

## Experiment results (place holder!)

We ran each type with multiple number of files to copy. The results are as follows: 

### 1000 files

| Experiment Description | DIU  | Activity Runs | External Activity Runs | Total Cost | Total Time |
|------------------------|------|---------------|------------------------|------------|------------|
| Azure IR + Copy   | 35   | Male   | 40   | 40   | 40   |
| Azure IR + ACA | 29   | Female | 30   | 30   | 30   |
| SHIR + Copy | 42   | Male   | 20   | 20   | 20   |
| SHIR + ACA | 42   | Male   | 20   | 20   | 20   |
| Managed IR + Copy | 42   | Male   | 20   | 20   | 20   |
| Managed IR + ACA | 42   | Male   | 20   | 20   | 20   |

### 2000 files

| Experiment Description | DIU  | Activity Runs | External Activity Runs | Total Cost | Total Time |
|------------------------|------|---------------|------------------------|------------|------------|
| Azure IR + Copy   | 35   | Male   | 40   | 40   | 40   |
| Azure IR + ACA | 29   | Female | 30   | 30   | 30   |
| SHIR + Copy | 42   | Male   | 20   | 20   | 20   |
| SHIR + ACA | 42   | Male   | 20   | 20   | 20   |
| Managed IR + Copy | 42   | Male   | 20   | 20   | 20   |
| Managed IR + ACA | 42   | Male   | 20   | 20   | 20   |


### 5000 files

| Experiment Description | DIU  | Activity Runs | External Activity Runs | Total Cost | Total Time |
|------------------------|------|---------------|------------------------|------------|------------|
| Azure IR + Copy   | 35   | Male   | 40   | 40   | 40   |
| Azure IR + ACA | 29   | Female | 30   | 30   | 30   |
| SHIR + Copy | 42   | Male   | 20   | 20   | 20   |
| SHIR + ACA | 42   | Male   | 20   | 20   | 20   |
| Managed IR + Copy | 42   | Male   | 20   | 20   | 20   |
| Managed IR + ACA | 42   | Male   | 20   | 20   | 20   |

### 10000 files

| Experiment Description | DIU  | Activity Runs | External Activity Runs | Total Cost | Total Time |
|------------------------|------|---------------|------------------------|------------|------------|
| Azure IR + Copy   | 35   | Male   | 40   | 40   | 40   |
| Azure IR + ACA | 29   | Female | 30   | 30   | 30   |
| SHIR + Copy | 42   | Male   | 20   | 20   | 20   |
| SHIR + ACA | 42   | Male   | 20   | 20   | 20   |
| Managed IR + Copy | 42   | Male   | 20   | 20   | 20   |
| Managed IR + ACA | 42   | Male   | 20   | 20   | 20   |


## Conclusion
