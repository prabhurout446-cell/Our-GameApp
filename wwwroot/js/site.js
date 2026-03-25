// ─── Our11 Main JavaScript ─────────────────────────────────────────────────

// Tabs
function initTabs() {
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const tab = this.dataset.tab;
            const parent = this.closest('[data-tabs-container]') || document;
            parent.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
            parent.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
            this.classList.add('active');
            const content = parent.querySelector(`[data-tab-content="${tab}"]`);
            if (content) content.classList.add('active');
        });
    });
}

// Countdown timer for matches
function initCountdowns() {
    document.querySelectorAll('[data-countdown]').forEach(el => {
        const target = new Date(el.dataset.countdown).getTime();
        function update() {
            const now = Date.now();
            const diff = target - now;
            if (diff <= 0) { el.textContent = 'Started'; el.style.color = 'var(--danger)'; return; }
            const h = Math.floor(diff / 3600000);
            const m = Math.floor((diff % 3600000) / 60000);
            const s = Math.floor((diff % 60000) / 1000);
            if (h > 24) {
                const d = Math.floor(h / 24);
                el.textContent = `${d}d ${h % 24}h left`;
            } else {
                el.textContent = `${String(h).padStart(2,'0')}:${String(m).padStart(2,'0')}:${String(s).padStart(2,'0')}`;
            }
        }
        update();
        setInterval(update, 1000);
    });
}

// Team Builder
const TeamBuilder = {
    selected: [],
    captain: null,
    viceCaptain: null,
    maxCredits: 100,

    init() {
        this.selected = [];
        this.captain = null;
        this.viceCaptain = null;
        this.render();
    },

    getCredits() {
        return this.selected.reduce((sum, id) => {
            const card = document.querySelector(`[data-player-id="${id}"]`);
            return sum + (card ? parseFloat(card.dataset.credits) : 0);
        }, 0);
    },

    getCountByRole(role) {
        return this.selected.filter(id => {
            const card = document.querySelector(`[data-player-id="${id}"]`);
            return card && card.dataset.role === role;
        }).length;
    },

    getCountByTeam(team) {
        return this.selected.filter(id => {
            const card = document.querySelector(`[data-player-id="${id}"]`);
            return card && card.dataset.team === team;
        }).length;
    },

    toggle(id) {
        const idx = this.selected.indexOf(id);
        const card = document.querySelector(`[data-player-id="${id}"]`);
        if (!card) return;
        const role = card.dataset.role;
        const team = card.dataset.team;
        const credits = parseFloat(card.dataset.credits);

        if (idx === -1) {
            if (this.selected.length >= 11) { showToast('Maximum 11 players allowed', 'error'); return; }
            if (this.getCredits() + credits > this.maxCredits) { showToast('Credit limit exceeded (max 100)', 'error'); return; }
            if (this.getCountByTeam(team) >= 7) { showToast('Max 7 players from one team', 'error'); return; }
            this.selected.push(id);
        } else {
            if (this.captain === id) this.captain = null;
            if (this.viceCaptain === id) this.viceCaptain = null;
            this.selected.splice(idx, 1);
        }
        this.render();
    },

    setCaptain(id) {
        if (!this.selected.includes(id)) { showToast('Select this player first', 'error'); return; }
        if (this.viceCaptain === id) this.viceCaptain = null;
        this.captain = (this.captain === id) ? null : id;
        this.render();
    },

    setViceCaptain(id) {
        if (!this.selected.includes(id)) { showToast('Select this player first', 'error'); return; }
        if (this.captain === id) this.captain = null;
        this.viceCaptain = (this.viceCaptain === id) ? null : id;
        this.render();
    },

    validate() {
        if (this.selected.length !== 11) return 'Select exactly 11 players';
        if (!this.captain) return 'Select a captain';
        if (!this.viceCaptain) return 'Select a vice-captain';
        const wk = this.getCountByRole('WK');
        if (wk < 1 || wk > 4) return 'Need 1-4 wicket keepers';
        const bat = this.getCountByRole('BAT');
        if (bat < 3 || bat > 6) return 'Need 3-6 batsmen';
        const bowl = this.getCountByRole('BOWL');
        if (bowl < 3 || bowl > 6) return 'Need 3-6 bowlers';
        const all = this.getCountByRole('ALL');
        if (all < 1) return 'Need at least 1 all-rounder';
        return null;
    },

    render() {
        // Update count displays
        const totalEl = document.getElementById('totalCount');
        const creditsEl = document.getElementById('creditsLeft');
        const wkEl = document.getElementById('wkCount');
        const batEl = document.getElementById('batCount');
        const allEl = document.getElementById('allCount');
        const bowlEl = document.getElementById('bowlCount');

        const credits = this.getCredits();
        const left = this.maxCredits - credits;

        if (totalEl) totalEl.textContent = this.selected.length + '/11';
        if (creditsEl) { creditsEl.textContent = left.toFixed(1); creditsEl.style.color = left < 5 ? 'var(--danger)' : 'var(--primary)'; }
        if (wkEl) wkEl.textContent = this.getCountByRole('WK');
        if (batEl) batEl.textContent = this.getCountByRole('BAT');
        if (allEl) allEl.textContent = this.getCountByRole('ALL');
        if (bowlEl) bowlEl.textContent = this.getCountByRole('BOWL');

        // Update submit button
        const submitBtn = document.getElementById('submitTeamBtn');
        if (submitBtn) {
            const err = this.validate();
            submitBtn.disabled = !!err;
            submitBtn.title = err || '';
        }

        // Update player card styles
        document.querySelectorAll('[data-player-id]').forEach(card => {
            const id = parseInt(card.dataset.playerId);
            card.classList.toggle('selected', this.selected.includes(id));
            card.classList.toggle('captain-pick', this.captain === id);
            card.classList.toggle('vc-pick', this.viceCaptain === id);
        });

        // Update hidden inputs
        const pidsInput = document.getElementById('playerIdsInput');
        const capInput = document.getElementById('captainInput');
        const vcInput = document.getElementById('vcInput');
        if (pidsInput) pidsInput.value = this.selected.join(',');
        if (capInput) capInput.value = this.captain || '';
        if (vcInput) vcInput.value = this.viceCaptain || '';
    }
};

// Toast notification
function showToast(msg, type = 'success') {
    const toast = document.createElement('div');
    toast.className = `alert alert-${type === 'error' ? 'danger' : 'success'}`;
    toast.style.cssText = 'position:fixed;bottom:80px;left:50%;transform:translateX(-50%);z-index:9999;min-width:260px;text-align:center;';
    toast.innerHTML = `<i class="fas fa-${type === 'error' ? 'exclamation-circle' : 'check-circle'}"></i> ${msg}`;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 3000);
}

// Prize calculator for contest creation
function updatePrize() {
    const maxTeams = parseInt(document.getElementById('maxTeams')?.value) || 0;
    const entryFee = parseFloat(document.getElementById('entryFee')?.value) || 0;
    const commPct = parseFloat(document.getElementById('commissionPct')?.value) || 25;
    const gross = maxTeams * entryFee;
    const commission = gross * commPct / 100;
    const net = gross - commission;
    const grossEl = document.getElementById('grossPrize');
    const commEl = document.getElementById('commissionAmt');
    const netEl = document.getElementById('netPrize');
    if (grossEl) grossEl.textContent = '₹' + gross.toLocaleString('en-IN', {minimumFractionDigits: 2});
    if (commEl) commEl.textContent = '₹' + commission.toLocaleString('en-IN', {minimumFractionDigits: 2});
    if (netEl) netEl.textContent = '₹' + net.toLocaleString('en-IN', {minimumFractionDigits: 2});
}

// Live score poll
function pollLiveScores() {
    document.querySelectorAll('[data-live-match-id]').forEach(el => {
        const id = el.dataset.liveMatchId;
        setInterval(() => {
            fetch(`/Match/LiveScore/${id}`)
                .then(r => r.json())
                .then(data => { if (data.score) el.textContent = data.score; })
                .catch(() => {});
        }, 30000);
    });
}

// Copy to clipboard
function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(() => showToast('Copied to clipboard!'));
}

// Init
document.addEventListener('DOMContentLoaded', () => {
    initTabs();
    initCountdowns();
    pollLiveScores();
    updatePrize();

    document.getElementById('maxTeams')?.addEventListener('input', updatePrize);
    document.getElementById('entryFee')?.addEventListener('input', updatePrize);
});
