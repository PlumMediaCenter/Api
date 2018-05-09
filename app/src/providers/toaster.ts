import { Injectable } from '@angular/core';
import 'rxjs/add/operator/map';
import { ToastController, ToastOptions } from 'ionic-angular';

@Injectable()
export class Toaster {
    constructor(public toastCtrl: ToastController) {
    }

    toast(message: string, options: ToastOptions = null) {
        options = Object.assign(<ToastOptions>{
            message: message,
            showCloseButton: true,
            duration: 3000
        }, options);
        let t = this.toastCtrl.create(options);
        t.present();
    }
}
