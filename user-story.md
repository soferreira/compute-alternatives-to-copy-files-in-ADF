# Cost optimization of a data processing pipeline  

In this blog we will attempt to optimize, both the cost and the performance of a data processing pipelines, more specifically its data movement step. The pipeline was originally created using Azure Data Factory or Synapse Pipelines.

## Scenario

A company needs to process new files from different customers they have. Each customer has their own container. Files initially land into a 'Raw Source' Storage Account, and are then copied and processed in a different Storage account in the following layers Bronze -> Silver -> Gold.

We will use 1k, 2k, 5k and 10k files per iteration. There will also be a validation iteration that replaces each 1k of files with a single zipped (not compressed) file, so 1, 2, 5 & 10 files. We want to see what will be the impact of a single file versus multiple files on the copy step.

We will attempt the optimization with:

- different languages/frameworks: AzCopy, REST API, .NET SDK, Copy Activity, Spark.
- different compute: Azure IR and SHIR. For the SHIR scenario, we will start & stop the SHIR with web calls to save on VMs cost.
- we will also examine if we can rethink the way the pipeline was built to preserve the processing outcome and save cost.
- 
- 


```math
Cost/1000Runs = ActivityRuns[month] * 1.0 + 1000(DIUHour * 0.002 + Activity Duration[hours] * 0.002 + 

External Activity Runs * 0.0001 + XComputeTime[min] * 0.01) 
```