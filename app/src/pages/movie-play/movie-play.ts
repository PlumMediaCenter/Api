import { Component } from '@angular/core';
import { NavController, NavParams } from 'ionic-angular';
import { Api } from '../../providers/api';
import { Movie } from '../../interfaces/movie';

@Component({
    selector: 'page-movie-play',
    templateUrl: 'movie-play.html'
})
export class MoviePlayPage {

    constructor(
        public navCtrl: NavController,
        public navParams: NavParams,
        public api: Api
    ) {

    }
    public movie: Movie;
    public resumeSeconds: number;
    async ionViewDidLoad() {
        var movieId = this.navParams.data.movieId;
        var movie = await this.api.movies.getById(movieId);
        //get the current movie progress
        this.resumeSeconds = await this.api.mediaItems.getResumeSeconds(movieId);
        this.movie = movie;


    }
}
