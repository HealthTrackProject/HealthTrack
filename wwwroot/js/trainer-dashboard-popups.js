(() => {
    'use strict';
    const esc = value => String(value ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    let activeClient = null;

    function closeModal() {
        const modal = document.getElementById('trainerActionModal');
        if (modal) modal.hidden = true;
        document.body.classList.remove('trainer-modal-open');
        activeClient = null;
    }
    function ensureModal() {
        let modal = document.getElementById('trainerActionModal');
        if (modal) return modal;
        modal = document.createElement('div');
        modal.id = 'trainerActionModal';
        modal.className = 'trainer-action-modal';
        modal.hidden = true;
        modal.innerHTML = `<div class="trainer-action-backdrop" data-close></div><section class="trainer-action-dialog" role="dialog" aria-modal="true" aria-labelledby="trainerActionTitle"><button type="button" class="trainer-action-x" aria-label="Close" data-close>&times;</button><div class="trainer-popup-eyebrow">HEALTHTRACK · TRAINER</div><h3 id="trainerActionTitle"></h3><div id="trainerActionBody"></div><div class="trainer-action-footer"><button type="button" class="health-secondary-button" data-close>Close</button></div></section>`;
        document.body.appendChild(modal);
        modal.addEventListener('click', e => { if (e.target.closest('[data-close]')) closeModal(); });
        document.addEventListener('keydown', e => { if (e.key === 'Escape' && !modal.hidden) closeModal(); });
        return modal;
    }
    function showModal(title, html) {
        const modal = ensureModal();
        modal.querySelector('#trainerActionTitle').textContent = title;
        modal.querySelector('#trainerActionBody').innerHTML = html;
        modal.hidden = false;
        document.body.classList.add('trainer-modal-open');
        const first = modal.querySelector('input,select,textarea,button');
        if (first) first.focus();
    }
    function clientFromRow(row) {
        return { id: row.dataset.clientId || '', name: row.dataset.name || '', email: row.dataset.email || '', progress: row.dataset.progress || '0', phone: row.dataset.phone || 'Not provided', goal: row.dataset.goal || 'Not specified', health: row.dataset.health || 'No health record details available.' };
    }
    function showClientProfile(client) {
        showModal('Client profile', `<div class="trainer-client-profile"><div class="trainer-profile-avatar"><i class="bi bi-person-fill"></i></div><div><h4>${esc(client.name)}</h4><p>${esc(client.email)}</p><span class="trainer-progress-pill">${esc(client.progress)}% progress</span></div></div><div class="trainer-popup-detail-grid"><div><span>Phone</span><strong>${esc(client.phone)}</strong></div><div><span>Fitness goal</span><strong>${esc(client.goal)}</strong></div><div><span>Progress</span><strong>${esc(client.progress)}%</strong></div><div><span>Health records</span><strong>Not connected</strong></div></div><div class="trainer-popup-section"><h4><i class="bi bi-heart-pulse"></i> Health records</h4><p>${esc(client.health)} Connect this section to your health-record database before using it for real client care.</p></div><div class="trainer-popup-form-actions"><button type="button" class="health-primary-button" data-client-action="workout-from-modal">Assign workout</button><button type="button" class="health-secondary-button" data-client-action="diet-from-modal">Assign diet plan</button></div>`);
    }
    function showPlanAssignment(client, type) {
        const isWorkout = type === 'workout';
        const title = isWorkout ? 'Assign workout plan' : 'Assign diet plan';
        const plans = isWorkout ? ['Beginner Full Body', 'Strength Training', 'Cardio & Endurance', 'Weight Loss Workout'] : ['Balanced Nutrition', 'Weight Loss Diet', 'Muscle Gain Diet', 'Healthy Lifestyle Diet'];
        showModal(title, `<p class="trainer-popup-intro">Choose an existing ${isWorkout ? 'workout' : 'diet'} plan for <strong>${esc(client.name)}</strong>.</p><form class="trainer-popup-form" id="trainerAssignmentForm"><input type="hidden" name="clientId" value="${esc(client.id)}"><input type="hidden" name="type" value="${type}"><div class="client-form-group"><label for="trainerPlanSelect">${isWorkout ? 'Workout plan' : 'Diet plan'}</label><select id="trainerPlanSelect" name="plan" required><option value="">Select a plan</option>${plans.map(p => `<option value="${esc(p)}">${esc(p)}</option>`).join('')}</select><small>Use the ${isWorkout ? 'Workout Plans' : 'Diet Plans'} sidebar page to create and manage reusable plans.</small></div><div class="client-form-group"><label for="trainerPlanNotes">Instructions (optional)</label><textarea id="trainerPlanNotes" name="notes" placeholder="Add client-specific instructions..."></textarea></div><div class="trainer-popup-form-actions"><button type="submit" class="health-primary-button">Assign plan</button></div></form><p class="trainer-popup-disclaimer">Preview only: this sample form does not save an assignment to the database.</p>`);
    }
    function showAddClient() {
        showModal('Add client', `<form class="trainer-popup-form" id="trainerAddClientForm"><div class="client-form-group"><label for="newClientName">Full name</label><input id="newClientName" name="name" required maxlength="100" placeholder="Enter full name"></div><div class="client-form-group"><label for="newClientEmail">Email address</label><input id="newClientEmail" name="email" type="email" required maxlength="160" placeholder="name@example.com"></div><div class="client-form-group"><label for="newClientProgress">Progress (%)</label><input id="newClientProgress" name="progress" type="number" min="0" max="100" value="0" required></div><div class="trainer-popup-form-actions"><button type="submit" class="health-primary-button">Add to list</button></div></form><p class="trainer-popup-disclaimer">Preview only: connect this form to your client-create controller to save permanently.</p>`);
    }
    document.addEventListener('click', e => {
        const button = e.target.closest('[data-client-action]');
        if (!button) return;
        const action = button.dataset.clientAction;
        if (action === 'add') { showAddClient(); return; }
        if (action === 'view' || action === 'workout' || action === 'diet') {
            const row = button.closest('tr');
            if (!row) return;
            activeClient = clientFromRow(row);
            if (action === 'view') showClientProfile(activeClient);
            else showPlanAssignment(activeClient, action);
            return;
        }
        if (action === 'workout-from-modal' && activeClient) showPlanAssignment(activeClient, 'workout');
        if (action === 'diet-from-modal' && activeClient) showPlanAssignment(activeClient, 'diet');
    });
    document.addEventListener('input', e => {
        if (e.target.id !== 'clientSearch') return;
        const query = e.target.value.trim().toLowerCase();
        const rows = [...document.querySelectorAll('#clientTableBody tr')];
        let shown = 0;
        rows.forEach(row => { const match = row.innerText.toLowerCase().includes(query); row.hidden = !match; if (match) shown++; });
        const empty = document.getElementById('clientsEmptyState');
        if (empty) empty.style.display = shown ? 'none' : 'block';
        const count = document.getElementById('clientCount');
        if (count) count.textContent = shown;
    });
    document.addEventListener('submit', e => {
        if (e.target.id === 'trainerAddClientForm') {
            e.preventDefault();
            const form = new FormData(e.target);
            const name = String(form.get('name') || '').trim();
            const email = String(form.get('email') || '').trim();
            const progress = Math.max(0, Math.min(100, Number(form.get('progress') || 0)));
            if (!name || !email) return;
            const tbody = document.getElementById('clientTableBody');
            if (!tbody) return;
            const id = `preview-${Date.now()}`;
            const row = document.createElement('tr');
            row.dataset.clientId = id; row.dataset.name = name; row.dataset.email = email; row.dataset.progress = String(progress); row.dataset.phone = 'Not provided'; row.dataset.goal = 'Not specified'; row.dataset.health = 'No health records have been added.';
            row.innerHTML = `<td><div class="table-user-info"><div class="table-avatar trainer-initial-avatar">${esc(name.charAt(0).toUpperCase())}</div><div><h4>${esc(name)}</h4><p>Client · Preview</p></div></div></td><td class="client-email">${esc(email)}</td><td><div class="progress-cell"><div class="progress-bar-bg"><div class="progress-bar-fill" style="width:${progress}%"></div></div><span class="progress-text">${progress}%</span></div></td><td><div class="client-actions"><button type="button" class="btn-view-outline" data-client-action="view">View</button><button type="button" class="health-secondary-button" data-client-action="workout">Workout</button><button type="button" class="health-secondary-button" data-client-action="diet">Diet Plan</button></div></td>`;
            tbody.appendChild(row);
            closeModal();
            const count = document.getElementById('clientCount'); if (count) count.textContent = tbody.querySelectorAll('tr:not([hidden])').length;
            showModal('Client added to preview', `<p><strong>${esc(name)}</strong> was added to the visible list. This change is temporary and will disappear after refresh until a database endpoint is connected.</p>`);
            return;
        }
        if (e.target.id === 'trainerAssignmentForm') {
            e.preventDefault();
            const form = new FormData(e.target);
            const plan = String(form.get('plan') || '').trim();
            if (!plan) return;
            const client = activeClient?.name || 'the selected client';
            const type = form.get('type') === 'workout' ? 'Workout' : 'Diet';
            closeModal();
            showModal('Assignment preview', `<p><strong>${esc(plan)}</strong> was selected for <strong>${esc(client)}</strong>.</p><p>This is a front-end preview only. Connect the form to your assignment controller and database to save it permanently.</p>`);
        }
    });
})();
