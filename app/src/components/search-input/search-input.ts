import { Component, Input } from '@angular/core';
import { NavController, NavControllerBase, MenuController } from 'ionic-angular';
import { SearchResultsPage } from '../../pages/search-results/search-results';

@Component({
    selector: 'search-input',
    templateUrl: 'search-input.html'
})
export class SearchInputComponent {

    constructor(
        public menuCtrl: MenuController
    ) {
    }
    public searchText: string;
    @Input()
    private nav: NavControllerBase;

    public search() {
        //if we are already on the search page, scrap the previous search page in favor of this new one
        if (this.nav.getActive().component instanceof SearchResultsPage) {
            this.nav.pop();
        }
        this.nav.push(SearchResultsPage, { searchText: this.searchText });
        this.searchText = undefined;
        this.menuCtrl.close();
    }
}