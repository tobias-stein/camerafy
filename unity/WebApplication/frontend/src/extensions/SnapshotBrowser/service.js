import backend from '../../services/backend'

class SnapshotService
{
    async list()
    {
        return await backend.get('api/snapshots/');
    }

    async retrieve(id) 
    {
        return await backend.get(`api/snapshots/${id}/`);
    }

    async download(id) 
    {
        return await backend.get(`api/snapshots/${id}/download/`);
    }

    async delete(id)
    {
        return await backend.delete(`api/snapshots/${id}/`);
    }
}

export default new SnapshotService();