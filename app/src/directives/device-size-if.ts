import { Directive, Input, TemplateRef, ViewContainerRef } from '@angular/core';
import { Platform } from 'ionic-angular';
@Directive({
    selector: '[deviceSizeIf]'
})
export class DeviceSizeIfDirective {

    constructor(
        private templateRef: TemplateRef<any>,
        private viewContainer: ViewContainerRef,
        public platform: Platform
    ) { }

    private _deviceSizes: DeviceSize[];
    @Input('deviceSizeIf') set deviceSizes(deviceSizes: DeviceSize | DeviceSize[]) {
        if (typeof deviceSizes === 'string') {
            deviceSizes = [deviceSizes];
        }
        this._deviceSizes = deviceSizes;
        this.process();
    }
    private currentDeviceSize: DeviceSize;
    ngDoCheck() {
        var deviceSize = this.getDeviceSize();
        if (deviceSize != this.currentDeviceSize) {
            this.currentDeviceSize = deviceSize;
        }
        this.process();
    }

    process() {
        //if the current device size matches 
        if (this._deviceSizes.indexOf(this.currentDeviceSize) > -1) {
            // If condition is true add template to DOM
            this.viewContainer.createEmbeddedView(this.templateRef);
        } else {
            // Else remove template from DOM
            this.viewContainer.clear();
        }
    }

    getDeviceSize(): DeviceSize {
        //if the device is in portrait mode, use width. otherwise, use height.
        var width = this.platform.isPortrait() ? this.platform.width() : this.platform.height();
        if (width < 768) {
            return 'xs';
        } else if (width < 992) {
            return 'sm';
        } else if (width < 1200) {
            return 'md';
        } else {
            return 'lg';
        }
    }
}

export type DeviceSize = 'xs' | 'sm' | 'md' | 'lg';