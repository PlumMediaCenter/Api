import { Directive, Input, ElementRef, HostListener } from '@angular/core';
import { Movie } from '../interfaces/movie';
import { NavController } from 'ionic-angular';
import { MovieMetadataPage } from '../pages/movie-metadata/movie-metadata';

@Directive({ selector: '[movieMetadataClick]' })
export class MovieMetadataClick {
    constructor(el: ElementRef, public navCtrl: NavController) {

    }
    
    private movieId: number;

    @Input("movieMetadataClick")
    public set movieOrId(value: any) {
        if (typeof value === 'object') {
            this.movieId = (value as Movie).id;
        } else {
            this.movieId = value;
        }
    };

    @HostListener("click")
    click() {
        if (!this.movieId) {
            throw new Error("movieMetadataClick: value cannot be null or undefined");
        }
        this.navCtrl.push(MovieMetadataPage, { movieId: this.movieId })
    }
}