const { app } = require('@azure/functions');
const { getBlobClient, uploadFile, listFiles } = require('./blobClient');

const connectionString = process.env.AZURE_STORAGE_CONNECTION_STRING;
const containerName = process.env.USTERKI_CONTAINER_NAME;

app.http('UsterkiApi', {
    methods: ['GET', 'POST', 'DELETE'],
    authLevel: 'anonymous',
    handler: async (request, context) => {
        context.log('UsterkiApi hit. Method:', request.method);

        // =====================
        // GET
        // =====================
        // - bez parametru: lista plików
        // - ?name=... : redirect do pliku
        if (request.method === 'GET') {
            if (!connectionString || !containerName) {
                return {
                    status: 500,
                    jsonBody: { error: 'Brak konfiguracji storage.' }
                };
            }

            try {
                const container = getBlobClient(connectionString, containerName);

                const name = request.query.get('name');
                if (name) {
                    return {
                        status: 302,
                        headers: { Location: `${container.url}/${name}` }
                    };
                }

                const files = await listFiles(container);

                return {
                    status: 200,
                    jsonBody: {
                        count: files.length,
                        files
                    }
                };
            } catch (err) {
                context.log('GET error:', err);
                return {
                    status: 500,
                    jsonBody: { error: 'Błąd podczas obsługi GET.' }
                };
            }
        }

        // =====================
        // DELETE
        // =====================
        // ?name=... usuwa plik
        if (request.method === 'DELETE') {
            if (!connectionString || !containerName) {
                return {
                    status: 500,
                    jsonBody: { error: 'Brak konfiguracji storage.' }
                };
            }

            const name = request.query.get('name');
            if (!name) {
                return {
                    status: 400,
                    jsonBody: { error: 'Podaj nazwę pliku: ?name=...' }
                };
            }

            try {
                const container = getBlobClient(connectionString, containerName);
                const blobClient = container.getBlobClient(name);

                const result = await blobClient.deleteIfExists();

                return {
                    status: 200,
                    jsonBody: {
                        name,
                        message: result.succeeded
                            ? 'Plik usunięty.'
                            : 'Plik nie istniał.'
                    }
                };
            } catch (err) {
                context.log('DELETE error:', err);
                return {
                    status: 500,
                    jsonBody: { error: 'Błąd podczas usuwania pliku.' }
                };
            }
        }

        // =====================
        // POST
        // =====================
        // upload pliku multipart/form-data
        if (!connectionString || !containerName) {
            return {
                status: 500,
                jsonBody: { error: 'Brak konfiguracji storage.' }
            };
        }

        const contentType = request.headers.get('content-type') || '';
        if (!contentType.startsWith('multipart/form-data')) {
            return {
                status: 400,
                jsonBody: { error: 'Oczekiwano multipart/form-data z polem "file".' }
            };
        }

        try {
            const formData = await request.formData();
            const file = formData.get('file');

            if (!file) {
                return {
                    status: 400,
                    jsonBody: { error: 'Nie znaleziono pliku w polu "file".' }
                };
            }

            const fileName = file.name || `upload_${Date.now()}`;
            const buffer = Buffer.from(await file.arrayBuffer());

            const container = getBlobClient(connectionString, containerName);
            const blobUrl = await uploadFile(container, fileName, buffer);

            return {
                status: 201,
                jsonBody: {
                    message: 'Plik zapisany w Azure Blob Storage.',
                    url: blobUrl
                }
            };
        } catch (err) {
            context.log('POST error:', err);
            return {
                status: 500,
                jsonBody: { error: 'Błąd podczas uploadu pliku.' }
            };
        }
    }
});
