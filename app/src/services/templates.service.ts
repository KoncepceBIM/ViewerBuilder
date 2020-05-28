export class TemplatesService {
    private cache: { [key: string]: ((data: any) => string) } = {};

    public tmpl<T>(key: string, str: string): (data: T) => string {
        if (this.cache[key] != null)
            return this.cache[key];

        // Generate a reusable function that will serve as a template
        // generator (and which will be cached).
        const t = new Function("obj",
            "var p=[],print=function(){p.push.apply(p,arguments);};" +

            // Introduce the data as local variables using with(){}
            "with(obj){p.push('" +

            // Convert the template into pure JavaScript
            str
                .replace(/[\r\t\n]/g, " ")
                .split("{{").join("\t")
                .replace(/((^|%>)[^\t]*)'/g, "$1\r")
                .replace(/\t=(.*?)%>/g, "',$1,'")
                .split("\t").join("');")
                .split("}}").join("p.push('")
                .split("\r").join("\\'")
            + "');}return p.join('');") as any;
        this.cache[key] = t;

        return t
    };
}