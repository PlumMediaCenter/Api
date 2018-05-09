import { Component } from '@angular/core';
import { NavController } from 'ionic-angular';
import { Api } from '../../providers/api';
import { Loader } from '../../providers/loader';
import { Alerter } from '../../providers/alerter';
import { HomePage } from '../home/home';
import { Toaster } from '../../providers/toaster';

@Component({
    selector: 'page-install-database',
    templateUrl: 'install-database.html'
})
export class InstallDatabasePage {

    constructor(
        public navCtrl: NavController,
        public api: Api,
        public loader: Loader,
        public alerter: Alerter,
        public toaster: Toaster
    ) {

    }
    public rootUsername: string;
    public rootPassword: string;

    public async install() {
        var stop = this.loader.showForMinimum('Installing database');
        try {
            await this.api.database.install(this.rootUsername, this.rootPassword);
            await stop();
            this.navCtrl.setRoot(HomePage);
            this.toaster.toast("Database successfully installed");
        } catch (e) {
            if (e) {
                this.alerter.alert('An error occured: ' + JSON.stringify(e));
            }
            await stop();
        }
    }

}
