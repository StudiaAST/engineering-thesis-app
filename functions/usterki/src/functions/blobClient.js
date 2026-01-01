const { BlobServiceClient } = require('@azure/storage-blob');

function getBlobClient(connectionString, containerName) {
    const service = BlobServiceClient.fromConnectionString(connectionString);
    const container = service.getContainerClient(containerName);
    return container;
}

async function uploadFile(container, fileName, buffer) {
    const blockBlob = container.getBlockBlobClient(fileName);
    await blockBlob.upload(buffer, buffer.length);
    return blockBlob.url;
}

async function listFiles(container) {
    const files = [];

    for await (const blob of container.listBlobsFlat()) {
        files.push({
            name: blob.name,
            url: `${container.url}/${blob.name}`
        });
    }

    return files;
}


module.exports = {
    getBlobClient,
    uploadFile,
    listFiles
};

