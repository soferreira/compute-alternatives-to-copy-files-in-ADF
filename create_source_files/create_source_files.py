from pathlib import Path
import shutil
import os, uuid
from zipfile import ZipFile
import urllib.request
#from azure.identity import DefaultAzureCredential
from azure.storage.blob import BlobServiceClient, BlobClient, ContainerClient

NUMBER_OF_FILES = 5
# CONNECTION_STRING = req_body.get('connection_string')
CONNECTION_STRING = 'DefaultEndpointsProtocol=https;AccountName=soferreirastorage;AccountKey=sflIyHtagKkRq5fRvoy5pKShdb5gyG0+yNraQkZ1OpBO8GHcG2eH2QS6YGagHK/1bVcE2Oz6twKe+AStty9uNg==;EndpointSuffix=core.windows.net'

# Download the file from `url` and save it locally:
url = "https://raw.githubusercontent.com/soferreira/copy-alternatives/main/create_source_files/daily.data"
urllib.request.urlretrieve(url, "daily.data")

# Create a BlobServiceClient object
blob_service_client = BlobServiceClient.from_connection_string(CONNECTION_STRING)
# Set the name of the container where you want to upload the files
container_name = "samplesourcefiles"
# Set the local directory where the files are located
local_directory = Path.cwd()
file_to_upload = os.path.join(local_directory, "daily.data")


# Create the files
os.makedirs(f'{NUMBER_OF_FILES}k-unzip', exist_ok=True)
for _ in range(NUMBER_OF_FILES):
    # token = 'sp=racwdl&st=2022-12-14T13:59:29Z&se=2022-12-15T21:59:29Z&spr=https&sv=2021-06-08&sr=c&sig=t1EJFhXS6nRMyRF%2FCani%2FRoVUFDAYA2SukOQLw6Sk2g%3D'
    file_name = f'./{NUMBER_OF_FILES}k-unzip/daily-{uuid.uuid4()}.data'
    # shutil.copyfile('daily.data', file_name)
    blob_client = blob_service_client.get_blob_client(container=container_name, blob=file_name)
    with open(file=file_to_upload, mode="rb") as data:
        blob_client.upload_blob(data)