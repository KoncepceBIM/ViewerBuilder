import { Property } from "./property";

export class PropertySet{
    public properties: Property[] = [];
    constructor(public name: string = null) {
    }
}