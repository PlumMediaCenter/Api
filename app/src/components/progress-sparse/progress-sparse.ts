import { Component, Input} from '@angular/core';

@Component({
    selector: 'progress-sparse',
    templateUrl: 'progress-sparse.html'
})
export class ProgressSparseComponent {

    constructor() {
    }
    @Input()
    public set min(value) {
        this._min = value;
        this.calculate();
    }
    public get min() {
        return this._min;
    }
    private _min: number;;

    @Input()
    public set max(value) {
        this._max = value;
        this.calculate();
    }
    public get max() {
        return this._max;
    }
    private _max: number;

    @Input()
    public set lower(value) {
        this._lower = value;
        this.calculate();
    }
    public get lower() {
        return this._lower;
    }
    private _lower: number;

    @Input()
    public set upper(value) {
        this._upper = value;
        this.calculate();
    }
    public get upper() {
        return this._upper;
    }
    private _upper: number;

    @Input()
    public unit?: string;

    public innerLeftLeftboundPercent: number;
    public innerRightRightboundPercent: number;

    public calculate() {
        this.innerLeftLeftboundPercent = this.getInnerLeftLeftboundPercent();
        this.innerRightRightboundPercent = this.getInnerRightLeftboundPercent();
    }

    public getInnerBarCss() {
        var widthPercent = this.innerRightRightboundPercent - this.innerLeftLeftboundPercent;
        return {
            left: `${this.innerLeftLeftboundPercent}%`,
            width: `${widthPercent}%`
        };
    }

    public getInnerLeftLeftboundPercent() {
        var zeroedMax = this.max - this.min;
        var zeroedLower = this.lower - this.min;
        var val = (zeroedLower / zeroedMax) * 100;
        //set a default if val is bogus
        val = !isNaN(val) && val >= 0 ? val : 0;
        //value cannot be larger than 99 so we at least show SOMETHING
        val = val > 99 ? 99 : val;
        //force a whole number
        val = Math.floor(val);

        return val;
    }

    public getInnerRightLeftboundPercent() {
        var zeroedMax = this.max - this.min;
        var zeroedUpper = this.upper - this.min;
        var val = (zeroedUpper / zeroedMax) * 100;
        //if the value is bogus, use a default
        val = !isNaN(val) && val <= 100 ? val : 100;
        //the value cannot be smaller than zero
        val = val < 1 ? 1 : val;
        //force a whole number
        val = Math.ceil(val);
        return val;
    }
}