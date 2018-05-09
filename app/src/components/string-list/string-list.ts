import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Alerter } from '../../providers/alerter';

@Component({
    selector: 'string-list',
    templateUrl: 'string-list.html'
})
export class StringListComponent {

    constructor(
        private alerter: Alerter
    ) {
    }
    @Input()
    public items: string[];

    @Input()
    public comparisonItems: string[];

    @Input()
    public buttonType: 'add' | 'remove';
    @Output()
    public buttonClick = new EventEmitter<string>();

    @Input()
    public showAdd?: boolean = false;

    public click(url: string) {
        this.buttonClick.emit(url);
    }

    public async addNewItem() {
        var value = await this.alerter.prompt('Enter a new value', 'Accept', 'Cancel');
        if (value) {
            this.items.push(value);
        }
    }

    public isInUse(value: string) {
        return this.comparisonItems && this.comparisonItems.indexOf(value) > -1;
    }
}