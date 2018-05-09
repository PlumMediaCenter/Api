import { Injectable } from '@angular/core';
import { AlertController } from 'ionic-angular';

@Injectable()
export class Alerter {
    constructor(
        private alertCtrl: AlertController
    ) {

    }

    /**
     * Show an alert popup
     */
    public alert(message: string, okText = 'Ok', title = "Confirm"): Promise<boolean> {
        return new Promise((resolve, reject) => {
            let confirm = this.alertCtrl.create({
                title: title,
                message: message,
                buttons: [
                    {
                        text: okText,
                        handler: () => {
                            resolve(true);
                        }
                    }
                ]
            });
            confirm.present();
        });
    }

    /**
     * Show a confirm popup
     */
    public confirm(message: string, yesText = 'Yes', noText = 'No', title = "Confirm"): Promise<boolean> {
        return new Promise((resolve, reject) => {
            let confirm = this.alertCtrl.create({
                title: title,
                message: message,
                buttons: [
                    {
                        text: yesText,
                        handler: () => {
                            resolve(true);
                        }
                    },
                    {
                        text: noText,
                        handler: () => {
                            resolve(false);
                        }
                    }
                ]
            });
            confirm.present();
        });
    }


    private static IdCounter = 1;
    /**
     * Prompt the user for input values
     * @param message
     * @param acceptText 
     * @param rejectText 
     * @param inputs 
     */
    public async prompt(message: string, acceptText = 'Save', rejectText = 'Cancel') {
        return new Promise<string>((resolve, reject) => {
            var id = `alerter-prompt-input-` + Alerter.IdCounter++;
            let alert = this.alertCtrl.create({
                title: '',
                message,
                inputs: [{
                    name: 'value',
                    placeholder: 'value',
                    id: id
                }],
                buttons: [
                    {
                        text: rejectText,
                        handler: () => {
                            resolve(undefined);
                        }
                    },
                    {
                        text: acceptText,
                        handler: (data) => {
                            resolve(data.value);
                        }
                    }
                ]
            });

            alert.present().then(() => {
                //focus the first input element
                document.getElementById(id).focus();
            });
        });

    }
}
