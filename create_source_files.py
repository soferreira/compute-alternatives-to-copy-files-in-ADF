import shutil
import os
import uuid
import os, uuid
from azure.identity import DefaultAzureCredential
from azure.storage.blob import BlobServiceClient, BlobClient, ContainerClient


storage_account_name = None
container_name = None

account_url = f"https://{storage_account_name}.blob.core.windows.net"
default_credential = DefaultAzureCredential()

# Create the BlobServiceClient object
blob_service_client = BlobServiceClient(account_url, credential=default_credential)

# Create the container
container_client = blob_service_client.create_container(container_name)

# Create a file in the local data directory to upload and download
local_file_name = str(uuid.uuid4()) + ".data"

# Write text to the file
file = open(file=local_file_name, mode='w')
for _ in range(140): # will create files of 20kb  
    file.write('{"dataModelName":"data_model_1","operation":"U","factory":1354010702,"lineId":14069,"date":"2022-06-23T00:00:00","feature1":2,"dim":55,"yield":38605}')
file.close()

# Create a blob client using the local file name as the name for the blob
blob_client = blob_service_client.get_blob_client(container=container_name, blob=local_file_name)

print("\nUploading to Azure Storage as blob:\n\t" + local_file_name)

# Upload the created file
with open(file=local_file_name, mode="rb") as data:
    blob_client.upload_blob(data)

TOTAL_OF_FILES = [1, 2, 5, 10]

for i in TOTAL_OF_FILES:
    if not os.path.exists(dir):
        os.makedirs('{total_files}k-unzip')
        os.makedirs('{total_files}k-zip')
    for _ in range(i * 1000):
        shutil.copyfile('daily.data', f'./{i}k-unzip/daily-{uuid.uuid4()}.data')
    for _ in range(i):
        shutil.copyfile('daily.zip', f'./{i}k-zip/daily-{uuid.uuid4()}.zip')
