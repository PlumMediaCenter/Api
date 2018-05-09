import { Component } from '@angular/core';
import { NavController, NavParams } from 'ionic-angular';
import { Api } from '../../providers/api';
import { Movie } from '../../interfaces/movie';

@Component({
    selector: 'page-movie-info',
    templateUrl: 'movie-info.html'
})
export class MovieInfoPage {
    public Math = Math;
    constructor(
        public navCtrl: NavController,
        public navParams: NavParams,
        public api: Api
    ) {
    }

    /**
     * Run all page initialization in this method so that this page can be refreshed simply by re-calling this function
     */
    ionViewDidLoad() {
        this.movie = undefined;
        //access any parameters provided to the page through navParams. 
        var movieId = this.navParams.data.movieId;
        this.api.movies.getById(movieId).then((movie) => {
            this.movie = movie;
        });
    }
    ionViewDidEnter() {
        //refresh this page anytime it is displayed
        this.ionViewDidLoad();
    }
    public movie: Movie;
}
