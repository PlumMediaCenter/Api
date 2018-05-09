import { Component, Input } from '@angular/core';
import { MovieMetadataComparison } from '../../interfaces/movie-metadata-comparison';
import { ProperCaseSpacePipe } from '../../pipes/proper-case-space-pipe';

@Component({
    selector: 'metadata-compare-row',
    templateUrl: 'metadata-compare-row.html'
})
export class MetadataCompareRowComponent {

    constructor() {

    }

    @Input('label')
    public labelAttr: string;

    @Input()
    public property: string;

    @Input()
    public type: string;

    @Input()
    public set comparison(value) {
        this._comparison = value;
        this.sortStringLists();
    }
    public get comparison() {
        return this._comparison;
    }
    public _comparison: MovieMetadataComparison;

    /**
     * The label for the item. If label was specified in attributes, that is used. 
     * Otherwise, a proper case spaced version of the property name is used
     */
    public get label() {
        return this.labelAttr ? this.labelAttr : ProperCaseSpacePipe.transform(this.property);
    }

    public addImageUrl(propertyName, url) {
        var arr: string[] = this.comparison.current[propertyName];
        if (arr.indexOf(url) === -1) {
            arr.push(url)
        }
    }

    public removeImageUrl(propertyName, url) {
        var arr: string[] = this.comparison.current[propertyName];
        var idx = arr.indexOf(url);
        if (idx > -1) {
            arr.splice(idx, 1);
        }
    }

    public addStringItem(propertyName, item) {
        var list = (this.comparison.current[propertyName] as string[]);
        if (list.indexOf(item) === -1) {
            this.comparison.current[propertyName].push(item);
            this.sortStringLists();
        }
    }

    public removeStringItem(propertyName, item) {
        var list = (this.comparison.current[propertyName] as string[]);
        if (list.indexOf(item) > -1) {
            list.splice(list.indexOf(item), 1);
            this.sortStringLists();
        }
    }

    private sortStringLists() {
        var propertyNames = ['genres', 'keywords'];
        for (var i = 0; i < propertyNames.length; i++) {
            var propertyName = propertyNames[i];
            if (this.comparison && this.comparison.current && this.comparison.current[propertyName]) {
                this.comparison.current[propertyName] = (this.comparison.current[propertyName] as string[]).sort(function (a, b) {
                    return a.toLowerCase().localeCompare(b.toLowerCase());
                });
            }
            if (this.comparison && this.comparison.incoming && this.comparison.incoming[propertyName]) {
                this.comparison.incoming[propertyName] = (this.comparison.incoming[propertyName] as string[]).sort(function (a, b) {
                    return a.toLowerCase().localeCompare(b.toLowerCase());
                });
            }
        }
    }
}
