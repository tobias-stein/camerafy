import backend from '../../services/backend'

class EnvironmentService
{
    async list()
    {
        var next = 'api/environments/';
        var environments = [];
        while(next != null)
        {
            const response = await backend.get(next);
            environments = environments.concat(response.results);
            next = response.next;
        } 

        return environments;
    }
}

export default new EnvironmentService();