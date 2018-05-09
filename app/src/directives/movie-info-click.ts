import { Directive, Input, ElementRef, HostListener } from '@angular/core';
import { Movie } from '../interfaces/movie';
import { NavController } from 'ionic-angular';
import { MovieInfoPage } from '../pages/movie-info/movie-info';

@Directive({ selector: '[movieInfoClick]' })
export class MovieInfoClick {
    constructor(el: ElementRef, public navCtrl: NavController) {

    }

    @Input("movieInfoClick")
    public set movieOrId(value: any) {
        if (typeof value === 'object') {
            this.movieId = (value as Movie).id;
        } else {
            this.movieId = value;
        }
    };
    private movieId: number;

    @HostListener("click")
    click() {
        if (!this.movieId) {
            throw new Error("movieInfoClick: value cannot be null or undefined");
        }
        this.navCtrl.push(MovieInfoPage, { movieId: this.movieId })
    }
}