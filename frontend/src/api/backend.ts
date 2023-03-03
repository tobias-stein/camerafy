import axios from "axios";

/** 
 * This client class must be used by all APIs that need to to access the backend services.
 */
class Backend
{
    /** This method is called from the oauth.ts when a new access token has been gathered. */
    public SetAuthorization(bearerToken : string)
    {
        // update authorization header
        axios.defaults.headers.common = {'Authorization': `Bearer ${bearerToken}`};
    }
};

export default new Backend();