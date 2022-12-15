import logging
import os
from pathlib import Path
import azure.functions as func
import uuid
import urllib.request
from azure.storage.blob import BlobServiceClient


def main(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')
    
    # Get the parameters from the request body
    req_body = req.get_json()
    number_of_files = req_body.get('files')
    connection_string = req_body.get('connection_string')
    container = req_body.get('container')
    url = req_body.get('url_sample_file')

    # If the number of file or connection string are empty, return a 400 error
    if not number_of_files or not connection_string:
        return func.HttpResponse(
             "Please pass a number of files and connection string in the request body",
             status_code=400
        )
    # Define the name of the container where you want to upload the files
    if not container:
        container = "samplesourcefiles"
    # Download the default sample file:
    if not url:
        url = "https://raw.githubusercontent.com/soferreira/copy-alternatives/main/sample_data/daily.data"
    #urllib.request.urlretrieve(url, "daily.data")

    # Send an HTTP GET request to the file URL
    response = urllib.request.urlopen(url)

    # Write the file to the local file system
    with open("daily.data", "wb") as f:
        f.write(response.read())

    # Create a BlobServiceClient object
    blob_service_client = BlobServiceClient.from_connection_string(connection_string)

    # Create the number of files requested in the container
    for _ in range(number_of_files):
        file_name = f'/{number_of_files}k-unzip/daily-{uuid.uuid4()}.data'
        container_client = blob_service_client.get_container_client(container)
        blob_client = container_client.get_blob_client(blob=file_name)
        with open(file=os.path.join(Path.cwd(), "daily.data"), mode="rb") as data:
            blob_client.upload_blob(data)

    # Return a 200 OK response
    return func.HttpResponse(
            f"This HTTP triggered function executed successfully. {number_of_files} files created in {container} container.",
            status_code=200
    )

