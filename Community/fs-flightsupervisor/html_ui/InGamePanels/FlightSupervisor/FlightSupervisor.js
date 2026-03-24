class IngamePanelFlightSupervisor extends TemplateElement {
    constructor() {
        super(...arguments);
        this.panelActive = false;
        this.started = false;
        this.ingameUi = null;
        this.iframeElement = null;
    }
    connectedCallback() {
        super.connectedCallback();
        var self = this;
        this.ingameUi = this.querySelector('ingame-ui');
        this.iframeElement = document.getElementById("FSIframe");

        if (this.ingameUi) {
            this.ingameUi.addEventListener("panelActive", (e) => {
                self.panelActive = true;
                if (self.iframeElement) {
                    self.iframeElement.src = 'http://localhost:5050/';
                }
            });
            this.ingameUi.addEventListener("panelInactive", (e) => {
                self.panelActive = false;
                if (self.iframeElement) {
                    self.iframeElement.src = '';
                }
            });
        }
    }
    disconnectedCallback() {
        super.disconnectedCallback();
    }
}
window.customElements.define("ingamepanel-flightsupervisor", IngamePanelFlightSupervisor);
checkAutoload();
