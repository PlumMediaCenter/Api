import { Component } from '@angular/core';
import { NavController, NavParams, LoadingController, AlertController } from 'ionic-angular';
import { Api } from '../../providers/api';
import { Movie } from '../../interfaces/movie';
import { MovieMetadataComparison } from '../../interfaces/movie-metadata-comparison';
import { MovieMetadataSearchResult } from '../../interfaces/movie-metadata-search-result';

@Component({
    selector: 'page-movie-metadata',
    templateUrl: 'movie-metadata.html'
})
export class MovieMetadataPage {
    constructor(
        public navCtrl: NavController,
        public navParams: NavParams,
        public api: Api,
        public loadingCtrl: LoadingController,
        private alertCtrl: AlertController
    ) {
        this.init();
    }

    public async init() {
        var movieId = this.navParams.data.movieId;
        //movieId = 3;
        this.movie = await this.api.movies.getById(movieId);
        this.tmdbId = this.movie.tmdbId;
        this.searchText = this.movie.title;
        if (!this.tmdbId) {
            //auto-search by title
            this.search();
        }
    }

    public movie: Movie;
    public changeIncoming() {
        this.tmdbId = undefined;
    }

    public get movieFolderAndFile() {
        if (!this.movie) {
            return null;
        }
        var pattern = /(.*\/source\d+)/g;
        return this.movie.videoUrl.replace(pattern, '');
    }

    /**
     * The tmdb id that will be used to search for metadata. Id from movie is used if available
     */
    public set tmdbId(value) {
        this._tmdbId = value;
        if (this._tmdbId) {
            this.loadComparison();
        }
    };
    public get tmdbId() {
        return this._tmdbId;
    }
    private _tmdbId: number;

    /**
     * Name of the simple properties that should be used in ngFor to reduce code duplication
     */
    public simpleProperties = [
        'title',
        'summary',
        'description',
        'collection',
        'collectionOrder',
        'rating',
        'releaseDate',
        'runtime',
        'tmdbId'
    ];

    /**
     * The text to use to search for a movie. This is pre-populated with the movie title (or file name without extension)
     */
    public searchText: string;

    public searchResults: MovieMetadataSearchResult[];

    public comparison: MovieMetadataComparison;

    /**
     * Determines if the ID picker should be shown
     */
    public get idPickerIsVisible() {
        return this.movie && !this.tmdbId;
    }

    /**
     * Determines if the comparison editor should be shown
     */
    public get comparisonEditorIsVisible() {
        return this.movie && this.tmdbId && this.comparison;
    }



    public async search() {
        this.searchResults = null;
        let loading = this.loadingCtrl.create({
            content: 'Searching online for matches'
        });
        loading.present();
        this.searchResults = await this.api.metadata.getMovieSearchResults(this.searchText);
        loading.dismiss();
    }

    public async loadComparison() {
        this.comparison = await this.api.metadata.compareMovie(this.movie.id, this._tmdbId);
    }

    public useAllIncoming() {
        this.comparison.current = Object.assign({}, this.comparison.incoming);

        //copy each array
        for (var key in this.comparison.incoming) {
            var value = this.comparison.incoming[key];
            if (Array.isArray(value)) {
                this.comparison.current[key] = value.slice();
            }
        }
        //only keep the first poster
        this.comparison.current.posterUrls = [this.comparison.current.posterUrls[0]];
    }

    public async save() {
        let loading = this.loadingCtrl.create({
            content: 'Saving metadata'
        });
        loading.present();
        try {
            await this.api.metadata.save(this.movie.id, this.comparison.current);
        } catch (error) {
            let alert = this.alertCtrl.create({
                title: 'Error',
                message: `An error occurred while saving metadata: ${error.message}`,
                buttons: ['Dismiss']
            });
            alert.present();
        }
        loading.dismiss();
    }

}
