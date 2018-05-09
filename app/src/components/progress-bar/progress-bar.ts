import { Component, Input } from '@angular/core';

@Component({
    selector: 'progress-bar',
    templateUrl: 'progress-bar.html'
})
export class ProgressBarComponent {

    constructor() {
    }
    @Input()
    public percentage: number;

    public getStyle() {
        var right = 0;
        if (this.percentage > 100) {
            right = 100;
        } else if (this.percentage < 3) {
            right = 3;
        } else {
            right = 100 - this.percentage;
        }
        return { 'right': `${right}%` };
    }

}