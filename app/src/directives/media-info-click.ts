import { Directive, Input, HostListener } from '@angular/core';
import { NavController } from 'ionic-angular';
import { MovieInfoPage } from '../pages/movie-info/movie-info';
import { Api } from '../providers/api';
import { MediaTypeId } from '../enums/media-type-id';
import { Loader } from '../providers/loader';

@Directive({ selector: '[mediaInfoClick]' })
export class MediaInfoClick {
    constructor(
        public navCtrl: NavController,
        private api: Api,
        private loader: Loader
    ) {

    }

    @Input("mediaInfoClick")
    public mediaItemId: number;

    @HostListener("click")
    async click() {
        if (!this.mediaItemId) {
            throw new Error("mediaInfoClick: value cannot be null or undefined");
        }
        var hide = this.loader.show('Loading');
        try {
            var mediaItem = await this.api.mediaItems.getMediaItem(this.mediaItemId);

            switch (mediaItem.mediaTypeId) {
                case MediaTypeId.movie:
                    this.navCtrl.push(MovieInfoPage, { movieId: this.mediaItemId });
                    break;
                default:
                    throw new Error('Not implemented');
            }
        } finally {
            hide();
        }
    }
}