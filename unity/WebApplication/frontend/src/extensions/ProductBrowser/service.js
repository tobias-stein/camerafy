import backend from '../../services/backend'

class ProductService
{
    async list()
    {
        var next = 'api/models/';
        var models = [];
        while(next != null)
        {
            const response = await backend.get(next);
            models = models.concat(response.results);
            next = response.next;
        } 

        return models;
    }
}

export default new ProductService();