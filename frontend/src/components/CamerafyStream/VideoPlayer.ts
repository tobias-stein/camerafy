import EventEmitter from 'events'
import { Keymap, PointerPhase } from './InputTypes'

/**
 * Simple class to stores a normalized 2D coordinate to the video players view rectangle.
 */
export class ClientViewportPosition
{
    public x : number;
    public y : number;
    
    constructor(x : number, y : number) { this.x = x; this.y = y; }
};

/**
 * This is a wrapper class around the 'CamerafyStream' views video element. It 
 * will take care of reporting the normalized cursor/touch position on the video element.
 * note: The video viewports origin is the top-left corner.
 */
class VideoPlayer extends EventEmitter
{
    // Reference to video tag element from 'CamerafyStream' view
    private video : any; 

    // videos top/left corner on page
    private p0 : ClientViewportPosition;
    // video bottom/right corner on page
    private p1 : ClientViewportPosition;

    constructor()
    {
      super();
    }

    public initialize(videoElement : any) : void
    {
        this.p0 = new ClientViewportPosition(0, 0);
        this.p1 = new ClientViewportPosition(0, 0);

        this.video = videoElement;

        // set <video> element properties
        this.video.playsInline = true;

        // initially call resize once we have video meta data to properly set video client corner points
        this.video.onloadedmetadata = () => { this.onVideoResize(); };
        // whenever the window is reseized
        document.body.onresize = () => { this.onVideoResize(); };
        // initially run resize logic once
        this.onVideoResize();

        this.RegisterInputHandler();
    }

    /**
     * Transforms a client coordinate to a normalized video viewport coordinate.
     */
    private toVideoViewport(cx : number, cy : number) : ClientViewportPosition
    {
        let nx = 0;
        let ny = 0;

        // Check if cursor is inside video rectable
        // horizonal
        if(cx < this.p0.x) { nx = 0.0; }
        else if(cx > this.p1.x) {nx = 1.0; }
        else { nx = cx / ((this.p1.x - this.p0.x) + this.p0.x); }
        // vertical
        if(cy < this.p0.y) { ny = 0.0; }
        else if(cy > this.p1.y) {ny = 1.0; }
        else { ny = cy / ((this.p1.y - this.p0.y) + this.p0.y); }

        return new ClientViewportPosition(nx, ny);
    }

    private onVideoResize()
    {
        /*
            pw = page width
            ph = page height
            vw = video width
            vh = video height
            pa = page ascpect ratio
            va = video aspect ratio

            IF pa < va:                               IF pa > va:
            '''''''''''                               '''''''''''
            ph = phHalf + vh + phHalf                 pw = pwHalf + vw + pwHalf
            +-------------------------+               +---p0----------------+---+
            |                         | phHalf        |   |                 |   |
            p0------------------------+               |   |                 |   |
            |                         |               |   |                 |   |
            |          VIDEO          | vh            |   |      VIDEO      |vh | ph 
            |                         |               |   |                 |   |
            +-------------------------p1              |   |                 |   |
            |            vw           |               |   |                 |   |
            +-------------------------+               +---+-----------------p1--+
                         pw                           pwHalf       vw        
        */

        const pw = document.body.clientWidth;
        const ph = document.body.clientHeight;
        const pa = pw / ph; 
        const va = Math.max(this.video.videoWidth, 1) / Math.max(this.video.videoHeight, 1);

        if(pa < va)
        {
            const vh = pw / va;
            const phHalf = (ph - vh) * 0.5;

            this.p0 = new ClientViewportPosition(0, phHalf);
            this.p1 = new ClientViewportPosition(pw, phHalf + vh);
        }
        else // pa > va
        {
            const vw = ph * va;
            const pwHalf = (pw - vw) * 0.5;

            this.p0 = new ClientViewportPosition(pwHalf, 0);
            this.p1 = new ClientViewportPosition(pwHalf + vw, ph);
        }
    }

    public play() { this.video.play(); }
    public stop() { this.video.pause(); }

    public setStream(stream : any) { this.video.srcObject = stream; }

    /** INPUT HANDLING */

    private RegisterInputHandler() : void
    {
        // keypoard events
        document.addEventListener('keyup', (e) => { this.sendKeyUp(e); }, false);
        document.addEventListener('keydown', (e) => { this.sendKeyDown(e); }, false);

        // mouse events
        this.video.addEventListener('click', (e) => { this.sendMouse(e); }, false);
        this.video.addEventListener('mousedown', (e) => { this.sendMouse(e); }, false);
        this.video.addEventListener('mouseup', (e) => { this.sendMouse(e); }, false);
        this.video.addEventListener('mousemove', (e) => { this.sendMouse(e); }, false);
        this.video.addEventListener('wheel', (e) => { this.sendMouseWheel(e); }, false);

        // touch events
        this.video.addEventListener('touchend', (e) => { this.sendTouchEnd(e); }, false);
        this.video.addEventListener('touchstart', (e) => { this.sendTouchStart(e); }, false);
        this.video.addEventListener('touchcancel', (e) => { this.sendTouchCancel(e); }, false);
        this.video.addEventListener('touchmove', (e) => { this.sendTouchMove(e); }, false);
    }

    private sendKeyUp(e : KeyboardEvent) : void { this.sendKey(e, false); }
    private sendKeyDown(e : KeyboardEvent) : void { this.sendKey(e, true); }

    private sendKey(e : KeyboardEvent, isDown : boolean) : void 
    {
      const key = Keymap[e.code];
      const character = e.key.length === 1 ? e.key.charCodeAt(0) : 0;
      this.emit("onKeyboardInput", isDown, e.repeat, key, character);
    }

    private sendTouch(e : TouchEvent , phase : PointerPhase) : void 
    {
      const changedTouches = Array.from(e.changedTouches);
      const touches = Array.from(e.touches);
      
      const touchIds = [];
      const phrases = [];
      const xs = [];
      const ys = [];
      const forces = [];

      for (let i = 0; i < changedTouches.length; i++) {
        if (touches.find((t) => { return t.identifier === changedTouches[i].identifier }) === undefined) { touches.push(changedTouches[i]); }
      }

      for (let i = 0; i < touches.length; i++) {
        touches[i].identifier;
        phrases[i] = changedTouches.find((e) => { return e.identifier === touches[i].identifier }) === undefined ? PointerPhase.Stationary : phase;
      }

      for (let i = 0; i < touches.length; i++) 
      {
        const pos = this.toVideoViewport(e.touches[i].pageX, e.touches[i].pageY);

        touchIds.push(touches[i].identifier);
        xs.push(pos.x); 
        ys.push(pos.y);
        forces.push(touches[i].force);
      }

      this.emit("onTouchInput", touches.length, touchIds, phrases, xs, ys, forces);
    }

    private sendTouchMove(e : TouchEvent) : void { this.sendTouch(e, PointerPhase.Moved); e.preventDefault(); }
    private sendTouchStart(e : TouchEvent) : void { this.sendTouch(e, PointerPhase.Began); e.preventDefault(); }
    private sendTouchEnd(e : TouchEvent) : void { this.sendTouch(e, PointerPhase.Ended); e.preventDefault(); }
    private sendTouchCancel(e : TouchEvent) : void { this.sendTouch(e, PointerPhase.Canceled); e.preventDefault(); }

    private sendMouse(e : MouseEvent) : void 
    {
        const pos = this.toVideoViewport(e.clientX, e.clientY);
        this.emit("onMouseInput", pos.x, pos.y, e.buttons);
    }

    private sendMouseWheel(e : MouseWheelEvent) : void { this.emit("onMouseWheelInput", e.deltaX, e.deltaY); }
};

export default VideoPlayer;
