import { Pipe, PipeTransform } from '@angular/core';
/*
 * Raise the value exponentially
 * Takes an exponent argument that defaults to 1.
 * Usage:
 *   value | exponentialStrength:exponent
 * Example:
 *   {{ 2 | exponentialStrength:10 }}
 *   formats to: 1024
*/
@Pipe({ name: 'properCaseSpace' })
export class ProperCaseSpacePipe implements PipeTransform {
    public transform(input: string): string {
        return ProperCaseSpacePipe.transform(input);
    }
    
    public static transform(input: string): string {
        if (!input) {
            return '';
        } else {
            input = input.replace(/([A-Z])/g, " $1");
            input = input.charAt(0).toUpperCase() + input.slice(1);
            return input;
        }
    }
}