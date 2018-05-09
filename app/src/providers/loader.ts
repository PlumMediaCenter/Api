import { Injectable } from '@angular/core';
import 'rxjs/add/operator/map';
import { LoadingController } from 'ionic-angular';

@Injectable()
export class Loader {
    constructor(
        public loadingCtrl: LoadingController
    ) {
    }

    public minLoadDuration = 1000;

    /**
     * Show a loading message.
     * @return a function that can be called to hide the loader
     */
    show(message: string) {
        let loader = this.loadingCtrl.create({
            content: message
        });
        loader.present();
        var wasDismissed = false;
        return function () {
            if (wasDismissed) {
                return;
            } else {
                wasDismissed = true;
                loader.dismiss();
            }
        }
    }

    /**
     * Shows a message for at least minimum amount of time.
     * 
     * @return Returns a function that should be called when the calling function is finished with the long operation. 
     *         That returned function, when called, returns a promise, which will be resolved once the minimum duration has been met.
     * 
     */
    showForMinimum(message: string, durationMilliseconds: number = undefined) {
        durationMilliseconds = durationMilliseconds ? durationMilliseconds : this.minLoadDuration
        var startTime = new Date();
        var remove = this.show(message);

        var hide = function () {
            return new Promise((resolve) => {
                var timeWaited = new Date().getTime() - startTime.getTime();
                //if remove was called shorter than allowed duration, timeout until the full duration
                if (timeWaited < durationMilliseconds) {
                    var remainingTime = durationMilliseconds - timeWaited;
                    setTimeout(function () {
                        remove();
                        resolve();
                    }, remainingTime);
                } else {
                    //the time waited was long enough. remove now.
                    remove();
                    resolve();
                }
            });
        }
        return hide;
    }

    /**
     * Show a loading message for a short duration.
     * @return a promise that is resolved when the loading message is dismissed
     */
    showForDuration(message: string, durationMilliseconds: number = undefined) {
        durationMilliseconds = durationMilliseconds ? durationMilliseconds : this.minLoadDuration
        var dismiss = this.show(message);
        return new Promise((resolve, reject) => {
            setTimeout(function () {
                dismiss();
                resolve();
            }, durationMilliseconds);
        });
    }
}
