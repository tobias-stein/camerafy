import EventBus, { UserLoginEvent, UserLogoutEvent } from './EventBus'
import { UUID } from './uuid';

class User
{
    constructor()
    {
        this.UUID = UUID();
        this.name = null;
        this.id = null;
        this.token = null;
        this.permission_groups = [];
    }

    login(name, id, token, groups)
    {
        this.name = name;
        this.id = id;
        this.token = token;
        this.permission_groups = groups;
        EventBus.$emit(UserLoginEvent);
    }

    logout()
    {
        this.name = null;
        this.id = null;
        this.token = null;
        this.permission_groups = [];
        
        EventBus.$emit(UserLogoutEvent);
    }

    get isLoggedIn() { return this.token !== null; }

    get ULD() 
    {
        return JSON.stringify(
        {
            UUID: this.UUID,
            UserId: this.id,
            UserName: this.name,
            Groups: this.permission_groups
        });
    }
}

/** Global shared user object */
export default new User();