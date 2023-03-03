import Vue from "vue";

export default class Events extends Vue
{
    constructor()
    {
        super();
    }

    public broadcast(eventName : string, eventArgs : any[])
    {
        try
        {
            this.$emit.apply(this, [eventName, eventArgs]);
        }
        catch(ex)
        {
            console.error(`Failed to broadcast event. Reason: ${ex}`);
        }
    }
};