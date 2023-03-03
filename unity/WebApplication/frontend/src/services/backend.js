import CamerafyConfig from '../CamerafyConfig'
import CamerafyUser from './user'

class backend
{
    constructor()
    {
        this.backendUrl = `http://${CamerafyConfig.CamerafyBackendUrl == '0.0.0.0' ? window.location.hostname : CamerafyConfig.CamerafyBackendUrl}:${CamerafyConfig.CamerafyBackendPort}`;
    }

    async send(method = 'GET', endpoint, data = null)
    {
        try
        {
            var request = {
                method: method,
                mode: 'cors',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': CamerafyUser.token !== null ? `Token ${CamerafyUser.token}` : ""
                },
            };

            if(data !== null)
                request.body = JSON.stringify(data);
            const response = await fetch(`${this.backendUrl}/${endpoint}`, request);

            if(response.status === 204) /* NO CONTENT */
            {
                return "";
            }

            return await response.json();
        }
        catch(err)
        {
            console.log(err);
            return null;
        }
    }

    async get(endpoint) { return await this.send('GET', endpoint, null); }
    async post(endpoint, data) { return await this.send('POST', endpoint, data); }
    async put(endpoint, data) { return await this.send('PUT', endpoint, data); }
    async patch(endpoint, data) { return await this.send('PATCH', endpoint, data); }
    async delete(endpoint) { return await this.send('DELETE', endpoint, null); }
}

export default new backend();