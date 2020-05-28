import { PropertySet } from "../../models/property-set";

export class PropertiesComponent {
    /**
     *
     */
    constructor(private element: HTMLElement) {

    }

    public render(psets: PropertySet[]) {
        // clear
        this.element.innerHTML = null;

        const table = document.createElement('table');
        psets.forEach(pset => {
            // header 
            const headerRow = document.createElement('tr');
            const header = document.createElement('th');
            headerRow.appendChild(header);
            header.colSpan = 2;
            header.innerText = pset.name
            table.appendChild(headerRow);

            pset.properties.forEach(prop => {
                const propRow = document.createElement('tr');;
                const propName = document.createElement('td');
                propName.innerText = prop.name;
                const propValue = document.createElement('td');
                propValue.innerText = prop.value;
                propRow.appendChild(propName);
                propRow.appendChild(propValue);

                table.appendChild(propRow);
            });
        })

        this.element.appendChild(table);
    }
}