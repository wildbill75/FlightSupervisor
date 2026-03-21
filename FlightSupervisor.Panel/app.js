class FlightSupervisorPanel {
    constructor() {
        this.statusElement = document.getElementById('status-indicator');
        this.phaseElement = document.getElementById('val-phase');
        this.zuluElement = document.getElementById('val-zulu');
        this.aobtElement = document.getElementById('val-aobt');
        this.aibtElement = document.getElementById('val-aibt');
        this.feedbacksContainer = document.getElementById('feedbacks-container');
        
        this.simbriefInput = document.getElementById('simbrief-username');
        this.fetchBtn = document.getElementById('btn-fetch-simbrief');
        this.simbriefData = document.getElementById('simbrief-data');
        this.simbriefLoading = document.getElementById('simbrief-loading');
        
        this.isConnected = false;
        
        this.fetchBtn.addEventListener('click', () => this.fetchSimBrief());
        this.init();
    }
    
    init() {
        this.feedbacksContainer.innerHTML = '';
        this.addFeedback("Panel loaded. Awaiting telemetry...", "info", 0);
        
        // Quick URL flag trick allowing the user to view the UI in demo mode outside of MSFS
        if (window.location.search.includes('demo=1')) {
            this.runDemo();
        }
    }
    
    updateStatus(online) {
        this.isConnected = online;
        if (online) {
            this.statusElement.textContent = "Online";
            this.statusElement.className = "status-online";
        } else {
            this.statusElement.textContent = "Offline";
            this.statusElement.className = "status-offline";
        }
    }
    
    updatePhase(phaseName) {
        this.phaseElement.textContent = phaseName;
    }
    
    updateTimetable(zulu, aobt, aibt) {
        if(zulu) this.zuluElement.textContent = zulu;
        if(aobt) this.aobtElement.textContent = aobt;
        if(aibt) this.aibtElement.textContent = aibt;
    }
    
    addFeedback(message, type, points) {
        // Remove default text on first read feedback
        const defaultItem = this.feedbacksContainer.querySelector('.default-feedback');
        if (defaultItem && type !== "info") {
            defaultItem.remove();
        }

        const entry = document.createElement('div');
        
        let className = 'feedback-item default-feedback';
        let pointsHtml = '';

        if (type === 'bonus') {
            className = 'feedback-item feedback-bonus';
            pointsHtml = `<div class="points-badge">+${points} pts</div>`;
        } else if (type === 'penalty') {
            className = 'feedback-item feedback-penalty';
            pointsHtml = `<div class="points-badge">-${Math.abs(points)} pts</div>`;
        } else {
            entry.textContent = message;
        }

        entry.className = className;
        if (type !== 'info') {
            entry.innerHTML = `
                <div class="feedback-text">${message}</div>
                ${pointsHtml}
            `;
        }
        
        this.feedbacksContainer.insertBefore(entry, this.feedbacksContainer.firstChild);
        
        // Purge old events to not overload the CEF browser engine inside MSFS
        if (this.feedbacksContainer.children.length > 20) {
            this.feedbacksContainer.removeChild(this.feedbacksContainer.lastChild);
        }
    }

    async fetchSimBrief() {
        const username = this.simbriefInput.value.trim();
        if (!username) return;
        
        this.simbriefLoading.classList.remove('hidden');
        this.simbriefData.classList.add('hidden');
        
        try {
            // Fetch from local C# app to get parsed data + generated briefing
            const response = await fetch(`http://localhost:5000/api/simbrief?username=${encodeURIComponent(username)}`);
            if (!response.ok) throw new Error("Local Engine unreachable");
            
            const data = await response.json();
            
            document.getElementById('val-departure').textContent = data.departure;
            document.getElementById('val-destination').textContent = data.destination;
            document.getElementById('val-route').textContent = data.route;
            document.getElementById('val-level').textContent = data.level;
            document.getElementById('val-briefing').textContent = data.briefing;
            
            this.simbriefLoading.classList.add('hidden');
            this.simbriefData.classList.remove('hidden');
            this.addFeedback("Flight Plan & Briefing synchronized", "bonus", 50);
            
        } catch (err) {
            console.error(err);
            this.simbriefLoading.classList.add('hidden');
            this.addFeedback("C# Engine unreachable. Run FlightSupervisor.UI.exe", "penalty", 0);
        }
    }

    runDemo() {
        this.updateStatus(true);
        this.updatePhase("TAXI OUT");
        this.updateTimetable("14:32z", "14:28z", "--:--z");
        
        document.getElementById('val-departure').textContent = "LFPG";
        document.getElementById('val-destination').textContent = "KJFK";
        document.getElementById('val-route').textContent = "LGL UT225 XAMAB ...";
        document.getElementById('val-level').textContent = "FL360";
        document.getElementById('val-briefing').textContent = "CURRENT CONDITIONS:\nVisibility is excellent. Winds are moderate.\n\nFORECAST:\nExpect some snow at destination.";
        this.simbriefData.classList.remove('hidden');

        setTimeout(() => {
            this.addFeedback("Smooth Boarding & Pushback", "bonus", 150);
        }, 1000);

        setTimeout(() => {
            this.addFeedback("Taxi Overspeed: Aircraft exceeded 30kts", "penalty", 200);
        }, 3000);
    }
}

// Global scope instantiation for later WebSockets interaction
window.onload = () => {
    window.fsPanel = new FlightSupervisorPanel();
};
