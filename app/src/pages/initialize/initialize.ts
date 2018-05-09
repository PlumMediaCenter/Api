import { Component } from '@angular/core';
import { NavController } from 'ionic-angular';
import { Api } from '../../providers/api';
import { Loader } from '../../providers/loader';
import { Alerter } from '../../providers/alerter';
import { InstallDatabasePage } from '../install-database/install-database';
import { HomePage } from '../home/home';

@Component({
    selector: 'page-initialize',
    templateUrl: 'initialize.html'
})
export class InitializePage {

    constructor(
        public navCtrl: NavController,
        public api: Api,
        public loader: Loader,
        public alerter: Alerter
    ) {
        this.init();
    }
    public rootUsername: string;
    public rootPassword: string;

    public async init() {
        var stop = this.loader.showForMinimum('Initializing PlumMediaCenter');
        var isInstalled = await this.api.database.getIsInstalled();
        await stop();
        if (!isInstalled) {
            this.navCtrl.setRoot(InstallDatabasePage);
        } else {
            this.navCtrl.setRoot(HomePage);
        }
    }

}
