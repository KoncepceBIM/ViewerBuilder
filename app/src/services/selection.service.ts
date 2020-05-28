import { Viewer, State } from "@xbim/viewer";

export class SelectionService {

    constructor(private viewer: Viewer) {

    }

    private selection: number;

    public select(id: number) {
        // deselect any current selection
        if (this.selection != null) {
            this.viewer.setState(State.UNDEFINED, [ this.selection ]);
        }

        this.selection = id;
        if (id == null) {
            return;
        }

        this.viewer.setState(State.HIGHLIGHTED, [id]);
    }
}