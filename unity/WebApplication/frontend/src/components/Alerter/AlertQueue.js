class AlertQueue
{
    constructor()
    {
        this.queue = []
    }

    clear() { this.queue = []; }

    info(message, timeout_millsec = 3000) { this.queue.push({message: message, servity: "info", timeout: timeout_millsec}); }
    success(message, timeout_millsec = 4500) { this.queue.push({message: message, servity: "success", timeout: timeout_millsec}); }
    warning(message, timeout_millsec = 6000) { this.queue.push({message: message, servity: "warning", timeout: timeout_millsec}); }
    error(message, timeout_millsec = -1) { this.queue.push({message: message, servity: "error", timeout: timeout_millsec}); }
}

export default new AlertQueue();