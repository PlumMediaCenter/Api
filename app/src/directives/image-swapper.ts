import { Directive, Input, ElementRef, OnDestroy, OnInit } from '@angular/core';

@Directive({ selector: '[imageSwapper]' })
export class ImageSwapperDirective implements OnDestroy, OnInit {

    constructor(el: ElementRef) {
        this.img = el.nativeElement;
    }
    private intervalHandle;
    private img: HTMLImageElement;

    private currentIndex = -1;
    private nextImage() {
        this.currentIndex++;
        if (this.currentIndex >= this.urls.length) {
            this.currentIndex = 0;
        }
        var url = this.urls[this.currentIndex];
        if (url) {
            this.img.src = url;
        }
    }

    ngOnInit(): void {
        this.intervalHandle = setInterval(() => {
            this.nextImage();
        }, 5000);
        //run the first nextImage call so we get an image right away
        this.nextImage();
    }

    //stop the interval once this directive is destroyed
    ngOnDestroy(): void {
        clearInterval(this.intervalHandle);
    }

    @Input("imageSwapper")
    public set urls(value: string[]) {
        this._urls = value;
    }
    public get urls() {
        return this._urls ? this._urls : [];
    }
    private _urls: string[];
}