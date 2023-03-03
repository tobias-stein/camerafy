import Authentication from "./auth/oauth";
import Events from "./events/events";
import User from "./user/user";

class API 
{
    private IsInitialized : boolean;

    public auth : Authentication;
    public events : Events;
    public user : User;

    constructor()
    {
        this.auth = new Authentication();
        this.events = new Events();
        this.user = new User();

        this.IsInitialized = false;
    }
};

const api = new API();

export default api;