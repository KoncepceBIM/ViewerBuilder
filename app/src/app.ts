import { Viewer, Grid, NavigationCube, ViewType } from "@xbim/viewer";
import { EntityService } from "./services/entity.service";
import { SelectionService } from "./services/selection.service";
import { PropertiesComponent } from "./components/properties/properties.component";

export class Application {
    public viewer = new Viewer('viewer');
    public service = new EntityService();
    public selection = new SelectionService(this.viewer);

    public properties: PropertiesComponent;

    constructor() {
        this.setUpViewer();
        this.setUpPlugins();

        const leftPanel = document.getElementById('left-container');
        this.properties = new PropertiesComponent(leftPanel);

    }


    public start() {
        this.viewer.on('loaded', () => {
            this.viewer.show(ViewType.DEFAULT, undefined, undefined, false);
            this.viewer.start();
        })
        this.viewer.loadAsync('api/model.wexbim');
    }

    private setUpViewer() {
        this.viewer.background = [0, 0, 0, 0];

        this.viewer.on('pick', args => {
            this.selection.select(args.id);

            if (args.id != null) {
                this.service.getProperties(args.id).then(psets => {
                    this.properties.render(psets);
                });
            }
        });
    }

    private setUpPlugins() {
        var grid = new Grid();
        this.viewer.addPlugin(grid);
        var cube = new NavigationCube();
        cube.ratio = 0.05;
        cube.activeAlpha = 0.9;
        cube.passiveAlpha = 0.9;
        this.viewer.addPlugin(cube);
    }
}