export class VideoPlayer
{
    constructor(videoElement)
    {
        const _this = this;

        this.video = videoElement;
        this.fullscreen = false;
        this.pipWindow = null;

        // auto play once video stream meta data has been loaded
        this.video.addEventListener('loadedmetadata', function () 
        {
            _this.resizeVideo();
        }, true);

        // Picture-In-Picture events
        this.video.addEventListener('enterpictureinpicture', function (e) 
        {
            _this.pipWindow = e.pictureInPictureWindow;
        }, true);

        this.video.addEventListener('leavepictureinpicture', function (e) 
        {
            _this.pipWindow = null;
        }, true);
    }

    addEventListener(event, handler, flag) { this.video.addEventListener(event, handler, flag); }
    removeEventListener(event, handler, flag) { this.video.removeEventListener(event, handler, flag); }

    async requestPictureInPicture()
    {
        // if ('pictureInPictureEnabled' in document) 
        // {
        //     if (this.video !== document.pictureInPictureElement)
        //     {
        //         await this.video.requestPictureInPicture().catch();
        //     }
        // }
        // else
        // {
             this.video.pause();
        // }
    }

    async exitPictureInPicture()
    { 
        // if ('pictureInPictureEnabled' in document) 
        // {
        //     await document.exitPictureInPicture().catch();
        // }
        this.video.play();
    }

    resizeVideo() 
    {
      const clientRect = this.video.getBoundingClientRect();
      const videoRatio = this.width / this.height;
      const clientRatio = clientRect.width / clientRect.height;

      this.videoScale = videoRatio > clientRatio ? clientRect.width / this.width : clientRect.height / this.height;
      const videoOffsetX = videoRatio > clientRatio ? 0 : (clientRect.width - this.width * this.videoScale) * 0.5;
      const videoOffsetY = videoRatio > clientRatio ? (clientRect.height - this.height * this.videoScale) * 0.5 : 0;
      this.videoOriginX = clientRect.left + videoOffsetX;
      this.videoOriginY = clientRect.top + videoOffsetY;
    }

    changeStream(stream) { this.video.srcObject = stream; }

    play() { this.video.play(); }

    stop() 
    {
        this.video.pause();
        this.video.srcObject = null;
    }

    get width() { return this.video.videoWidth; }
    get height() { return this.video.videoHeight; }
    get originX() { return this.videoOriginX; }
    get originY() { return this.videoOriginY; }
    get scale() { return this.videoScale; }
    get paused() { return this.video.paused; }
}