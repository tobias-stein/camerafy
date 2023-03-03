import { UUID4 } from "@/api/utils/utils"
import Client from "../backend";

const AUTHENTICATION_RECORD_KEY = "Session_OAuth_Data";

/**
 * This class holds all necessary information for oauth.
 */
class AuthenticationRecord
{
    public RandomState : string;

    public TargetUri : string;

    public AccessToken : string;
    public RefreshToken : string;

    public Expires : Date;

    public Scopes : Array<string>;

    constructor(targetUri : string)
    {
        this.RandomState = UUID4();
        this.TargetUri = targetUri;
        this.AccessToken = "";
        this.RefreshToken = "";
        this.Expires = new Date(1970, 1, 1);
        this.Scopes = new Array<string>();
    }

    public Update(acessToken : string, refreshToken : string, expires : number, scopes : string) : void
    {
        this.AccessToken = acessToken;
        this.RefreshToken = refreshToken;
        this.Scopes = scopes.split(" ");
        this.Expires = new Date();
        this.Expires.setSeconds(this.Expires.getSeconds() + (expires - 1));

        // update session storage data
        localStorage.setItem(AUTHENTICATION_RECORD_KEY, JSON.stringify(this));

        // update clients 'Authorization' header
        Client.SetAuthorization(this.AccessToken);
    }

    public IsValid() : boolean { return (this.AccessToken.length > 0) && (this.RefreshToken.length > 0); }

    public IsAccessExpired() : boolean { return new Date(this.Expires) < new Date(); }
};

/**
 * This class takes care of user authentication with the oauth server.
 */
class Authentication
{
    /** True if client has been authenticated. */
    private AuthRecord : AuthenticationRecord;

    constructor()
    {
        this.AuthRecord = new AuthenticationRecord("");

        /** Check session storage for authentication record */
        const OAuthData = localStorage.getItem(AUTHENTICATION_RECORD_KEY);
        if(OAuthData !== null && OAuthData !== undefined)
        {
            this.AuthRecord = Object.assign(new AuthenticationRecord(""), JSON.parse(OAuthData));
        }
    }

    /**
     * Verifies if user is authenticated.
     */
    public async IsAuthenticated() : Promise<boolean> 
    {
        if(!this.AuthRecord.IsValid())
            return false;

        const headers = new Headers();
        headers.append('Content-Type', 'application/x-www-form-urlencoded');
        headers.append('Authorization', 'Basic ' + btoa(process.env.VUE_APP_OAUTH_CLIENT_ID + ':' + process.env.VUE_APP_OAUTH_CLIENT_SECRET));

        const data = new URLSearchParams();
        data.append("token", this.AuthRecord.AccessToken);

        return fetch(process.env.VUE_APP_OAUTH_INTROSPECTION_URL, { method: "POST", headers: headers, body: data })
            .then((response) => { return response.json(); })
            .then((data) => { return (data.active !== undefined && data.active); })
            .catch((error) => { console.error(error); return false; });
    }

    public AcesssToken() : string { return this.AuthRecord.AccessToken; }
    
    public async Logout() : Promise<void>
    {
        const headers = new Headers();
        headers.append('Content-Type', 'application/x-www-form-urlencoded');

        const data = new URLSearchParams();
        data.append("token", this.AuthRecord.AccessToken);
        data.append("client_id", process.env.VUE_APP_OAUTH_CLIENT_ID);

        try
        {
            await fetch(process.env.VUE_APP_OAUTH_REVOKE_TOKEN_URL, 
            {
                method: "POST",
                headers: headers,
                body: data
            });
        }
        catch(e)
        {
            console.log(e);
        }
    }

    /** Go to authorization server, where user has to authenticate. 
     * Also the user must authorize the application to use his/her data.
     */
    public async Authenticate(targetUri : string) : Promise<void>
    {
        // If authentication data has been gathered before ...
        if(this.AuthRecord.IsValid())
        {
            // Check if expired attempt to refresh access token
            if(this.AuthRecord.IsAccessExpired())
            {
                // try to refresh access token
                if(await this.RefreshAccessToken())
                    return;
            }
        }
        // otherwiese try to acquire an access token, if this was a redirect from a previously initiated authentication attemp
        else
        {
            if(await this.AcquireAccessToken())
                return;
        }

        // If none of the above conditions is true, initiate a new authentication process...

        // Create new authentication record in session cache
        const OAuthData = new AuthenticationRecord(targetUri);
        // create session storage data
        localStorage.setItem(AUTHENTICATION_RECORD_KEY, JSON.stringify(OAuthData));
        // redirect client to authorization server
        window.location.replace(`${process.env.VUE_APP_OAUTH_AUTH_URL}?client_id=${process.env.VUE_APP_OAUTH_CLIENT_ID}&response_type=code&state=${OAuthData.RandomState}`);
    }

    private async AcquireAccessToken() : Promise<boolean>
    {
        // Check query parameter for authentication server parameters
        const query = new URLSearchParams(document.location.search);
        const code  = query.get("code");
        const state = query.get("state");

        // stop right here, if a paremter is missing
        if(code === null || state === null || state !== this.AuthRecord.RandomState)
            return false;

        const GRANT_TYPE = "authorization_code";
        
        const headers = new Headers();
        headers.append('Authorization', 'Basic ' + btoa(process.env.VUE_APP_OAUTH_CLIENT_ID + ':' + process.env.VUE_APP_OAUTH_CLIENT_SECRET));
        headers.append('Content-Type', 'application/x-www-form-urlencoded');

        const data = new URLSearchParams();
        data.append("grant_type", GRANT_TYPE);
        data.append("code", code);

        return fetch(process.env.VUE_APP_OAUTH_TOKEN_URL, { method: "POST", headers: headers, body: data })
        .then((response) => { return response.json(); })
        .then((data) => 
        {
            this.AuthRecord.Update(data.access_token, data.refresh_token, data.expires_in, data.scope);

            if(this.AuthRecord.TargetUri.length > 0)
            {
                window.location.replace(`${window.location.origin}${this.AuthRecord.TargetUri}`);
            }
            else
            {
                // remove query parameter from URL
                window.history.replaceState(null, null, window.location.pathname);
                location.reload();
            }
            
            return true;
        })
        .catch((error) => { console.error(error); return false; });
    }

    private async RefreshAccessToken() : Promise<boolean>
    {
        const REFRESH_TOKEN = this.AuthRecord.RefreshToken;
        const GRANT_TYPE = "refresh_token";
        
        const headers = new Headers();
        headers.append('Authorization', 'Basic ' + btoa(process.env.VUE_APP_OAUTH_CLIENT_ID + ':' + process.env.VUE_APP_OAUTH_CLIENT_SECRET));
        headers.append('Content-Type', 'application/x-www-form-urlencoded');

        const data = new URLSearchParams();
        data.append("grant_type", GRANT_TYPE);
        data.append("refresh_token", REFRESH_TOKEN);

        return fetch(process.env.VUE_APP_OAUTH_TOKEN_URL, { method: "POST", headers: headers, body: data })
        .then((response) => { return response.json(); })
        .then((data) => 
        { 
            this.AuthRecord.Update(data.access_token, data.refresh_token, data.expires_in, data.scope); 
            return true; 
        })
        .catch((error) => { console.error(error); return false; });
    }
};

export default Authentication;