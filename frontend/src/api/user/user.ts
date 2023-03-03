import { UUID4 } from "@/api/utils/utils"

class User
{
    /** Unique user session id (note: this changes per user per session) */
    public sessionId : string;

    /** Username */
    public username? : string;

    constructor()
    {
        this.sessionId = UUID4();
        console.log(`User session-id: ${this.sessionId}`);
    }
};

export default User;