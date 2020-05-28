import { Entity } from "../models/entity";
import { PropertySet } from "../models/property-set";
import { Property } from "../models/property";

export class EntityService {
    private cache: { [id: number]: Entity } = {};

    public getEntity(id: number): Entity {
        if (this.cache[id] != null)
            return this.cache[id];
    }

    public getProperties(id: number): Promise<PropertySet[]> {
        return new Promise((resolve, reject) => {
            var entity = this.cache[id];
            if (entity != null && entity.properties != null) {
                resolve(entity.properties);
                return;
            }

            this.get<{ [ps: string]: { [p: string]: string } }>(`api/${id}/properties.json`).then(data => {
                var psets: PropertySet[] = [];
                Object.getOwnPropertyNames(data).forEach(psetName => {
                    const psetData = data[psetName];
                    const pset = new PropertySet(psetName);
                    Object.getOwnPropertyNames(psetData).forEach(propName => {
                        const value = psetData[propName];
                        const prop = new Property(propName, value);
                        pset.properties.push(prop);
                    });
                    
                    // only add if there are any properties in it
                    if (pset.properties.length > 0)
                        psets.push(pset);
                });

                // create if not existing
                if (entity == null) {
                    entity = new Entity();
                }

                // keep in cache
                entity.properties = psets;
                this.cache[id] = entity;
                resolve(psets);
            }).catch(err => {
                reject(err);
            });
        });

    }

    private get<T>(url: string): Promise<T> {
        return new Promise<T>((accept, reject) => {
            const request = new XMLHttpRequest();
            request.onreadystatechange = () => {
                if (request.readyState == 4) {
                    if (request.status >= 200 && request.status < 300) {
                        var result = JSON.parse(request.responseText);
                        accept(result);
                        return;
                    } 

                    reject(`Failed to request ${url}. Code: ${request.status} (${request.statusText}). Result: ${request.responseText}`)
                }
            };
            request.open("GET", url, true);
            request.send();
        });
    }
}