import { Directive, Input, HostListener } from '@angular/core';
import { Api } from '../providers/api';
import { Alerter } from '../providers/alerter';
import { Loader } from '../providers/loader';
import { NavController } from 'ionic-angular';

@Directive({ selector: '[mediaItemProcessClick]' })
export class MediaItemProcessClick {
    constructor(
        private api: Api,
        private alerter: Alerter,
        private loader: Loader,
        public navController: NavController
    ) {

    }

    private _mediaId: number;

    @Input("mediaItemProcessClick")
    public set mediaItemId(value: number | { id: number }) {
        if (typeof value === 'object') {
            this._mediaId = value.id;
        } else {
            this._mediaId = value;
        }
    };

    @HostListener("click")
    async click() {
        if (!this._mediaId) {
            throw new Error("mediaItemProcessClick: value cannot be null or undefined");
        }
        var hide = this.loader.show('Processing item');
        try {
            await this.api.library.process(this._mediaId);
            hide();
            await this.alerter.alert('Processing finished');
        } catch (e) {
            hide();
            await this.alerter.alert('There was an error processing item: ' + (e as Error).message);
        }
        //try to refresh the current view
        var component = this.navController.getActive().instance;
        //re-run the view load function if the page has one
        if (component.ionViewDidLoad) {
            component.ionViewDidLoad();
        }
    }
}
