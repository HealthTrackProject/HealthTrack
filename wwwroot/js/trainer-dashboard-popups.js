(() => {
    "use strict";

    document.addEventListener("DOMContentLoaded", () => {
        const config = document.getElementById("trainerAssignmentConfig");
        const tableBody = document.getElementById("clientTableBody");

        if (!config || !tableBody) {
            console.error("Trainer client table or assignment configuration was not found.");
            return;
        }

        const workoutUrl = config.dataset.workoutUrl;
        const dietUrl = config.dataset.dietUrl;
        const tokenInput = config.querySelector(
            'input[name="__RequestVerificationToken"]'
        );

        // Create the modal once.
        const modal = document.createElement("div");
        modal.id = "trainerClientModal";
        modal.className = "trainer-modal";
        modal.innerHTML = `
            <div class="trainer-modal-backdrop" data-close-modal></div>
            <section class="trainer-modal-card"
                     role="dialog"
                     aria-modal="true"
                     aria-labelledby="trainerModalTitle">
                <header class="trainer-modal-header">
                    <h2 id="trainerModalTitle">Client Details</h2>
                    <button type="button"
                            class="trainer-modal-close"
                            aria-label="Close popup"
                            data-close-modal>&times;</button>
                </header>

                <div class="trainer-modal-content" id="trainerModalContent"></div>
                <div id="trainerModalMessage" role="status" aria-live="polite"></div>
            </section>
        `;
        document.body.appendChild(modal);

        const modalTitle = modal.querySelector("#trainerModalTitle");
        const modalContent = modal.querySelector("#trainerModalContent");
        const modalMessage = modal.querySelector("#trainerModalMessage");

        let activeClient = null;

        function escapeHtml(value) {
            return String(value ?? "").replace(/[&<>"']/g, char => ({
                "&": "&amp;",
                "<": "&lt;",
                ">": "&gt;",
                '"': "&quot;",
                "'": "&#39;"
            })[char]);
        }

        function getClientRow(clientId) {
            return Array.from(
                tableBody.querySelectorAll("tr[data-client-id]")
            ).find(row => row.dataset.clientId === String(clientId));
        }

        function readClient(row) {
            return {
                id: row.dataset.clientId || "",
                name: row.dataset.name || "Client",
                email: row.dataset.email || "Not provided",
                phone: row.dataset.phone || "Not provided",
                progress: row.dataset.progress || "0",
                goal: row.dataset.goal || "Not specified",
                health: row.dataset.health || "No health information available."
            };
        }

        function showModal(title, content) {
            modalTitle.textContent = title;
            modalContent.innerHTML = content;
            modalMessage.textContent = "";
            modalMessage.className = "";
            modal.classList.add("is-open");
            document.body.classList.add("trainer-modal-open");

            const firstInput = modalContent.querySelector(
                "input:not([type=hidden]), textarea, button"
            );

            if (firstInput) firstInput.focus();
        }

        function closeModal() {
            modal.classList.remove("is-open");
            document.body.classList.remove("trainer-modal-open");
            activeClient = null;
        }

        modal.addEventListener("click", event => {
            if (event.target.closest("[data-close-modal]")) {
                closeModal();
            }
        });

        document.addEventListener("keydown", event => {
            if (event.key === "Escape" && modal.classList.contains("is-open")) {
                closeModal();
            }
        });

        function showView(client) {
            showModal("Client Details", `
                <div class="trainer-client-profile">
                    <div class="trainer-client-avatar">
                        <i class="bi bi-person-fill"></i>
                    </div>
                    <h3>${escapeHtml(client.name)}</h3>
                    <p>Client profile</p>
                </div>

                <div class="trainer-detail-list">
                    <div class="trainer-detail-row">
                        <span>Name</span>
                        <strong>${escapeHtml(client.name)}</strong>
                    </div>
                    <div class="trainer-detail-row">
                        <span>Email</span>
                        <strong>${escapeHtml(client.email)}</strong>
                    </div>
                    <div class="trainer-detail-row">
                        <span>Phone</span>
                        <strong>${escapeHtml(client.phone)}</strong>
                    </div>
                    <div class="trainer-detail-row">
                        <span>Workouts completed</span>
                        <strong>${escapeHtml(client.progress)}</strong>
                    </div>
                    <div class="trainer-detail-row">
                        <span>Goal</span>
                        <strong>${escapeHtml(client.goal)}</strong>
                    </div>
                    <div class="trainer-detail-row">
                        <span>Health information</span>
                        <strong>${escapeHtml(client.health)}</strong>
                    </div>
                </div>

                <div class="trainer-modal-actions">
                    <button type="button"
                            class="trainer-btn trainer-btn-primary"
                            data-client-action="workout"
                            data-client-id="${escapeHtml(client.id)}">
                        <i class="bi bi-activity"></i> Assign workout
                    </button>
                    <button type="button"
                            class="trainer-btn trainer-btn-secondary"
                            data-client-action="diet"
                            data-client-id="${escapeHtml(client.id)}">
                        <i class="bi bi-egg-fried"></i> Assign diet plan
                    </button>
                </div>
            `);
        }

        function showAssignment(client, type) {
            const isWorkout = type === "workout";
            const title = isWorkout ? "Assign Workout Plan" : "Assign Diet Plan";

            showModal(title, `
                <div class="trainer-selected-client">
                    <i class="bi bi-person-circle"></i>
                    <div>
                        <strong>${escapeHtml(client.name)}</strong>
                        <span>${escapeHtml(client.email)}</span>
                    </div>
                </div>

                <form id="trainerAssignmentForm">
                    <input type="hidden" name="clientId"
                           value="${escapeHtml(client.id)}">

                    <div class="trainer-form-group">
                        <label for="assignmentPlan">
                            ${isWorkout ? "Workout plan name" : "Diet plan name"}
                        </label>
                        <input id="assignmentPlan"
                               name="plan"
                               type="text"
                               maxlength="150"
                               required
                               placeholder="${isWorkout ? "e.g. Strength Training Plan" : "e.g. Healthy Weight Loss Plan"}">
                    </div>

                    <div class="trainer-form-group">
                        <label for="assignmentNotes">Description / Notes</label>
                        <textarea id="assignmentNotes"
                                  name="notes"
                                  rows="3"
                                  placeholder="Add instructions or notes (optional)"></textarea>
                    </div>

                    <div class="trainer-form-grid">
                        <div class="trainer-form-group">
                            <label for="assignmentStart">Start date</label>
                            <input id="assignmentStart"
                                   name="startDate"
                                   type="date"
                                   required>
                        </div>
                        <div class="trainer-form-group">
                            <label for="assignmentEnd">End date</label>
                            <input id="assignmentEnd"
                                   name="endDate"
                                   type="date"
                                   required>
                        </div>
                    </div>

                    <div class="trainer-modal-actions">
                        <button type="button"
                                class="trainer-btn trainer-btn-secondary"
                                data-close-modal>Cancel</button>
                        <button type="submit"
                                class="trainer-btn trainer-btn-primary"
                                id="saveAssignmentButton">
                            <i class="bi bi-check-circle"></i> Save assignment
                        </button>
                    </div>
                </form>
            `);

            const today = new Date();
            const localToday = new Date(
                today.getTime() - today.getTimezoneOffset() * 60000
            ).toISOString().slice(0, 10);

            const startInput = modalContent.querySelector("#assignmentStart");
            const endInput = modalContent.querySelector("#assignmentEnd");

            startInput.value = localToday;
            endInput.value = localToday;
            startInput.min = localToday;
            endInput.min = localToday;

            startInput.addEventListener("change", () => {
                endInput.min = startInput.value;
                if (endInput.value && endInput.value < startInput.value) {
                    endInput.value = startInput.value;
                }
            });

            const form = modalContent.querySelector("#trainerAssignmentForm");

            form.addEventListener("submit", async event => {
                event.preventDefault();

                const button = form.querySelector("#saveAssignmentButton");
                const formData = new FormData(form);

                if (tokenInput?.value) {
                    formData.append("__RequestVerificationToken", tokenInput.value);
                }

                if (!workoutUrl || !dietUrl) {
                    showMessage("Assignment URL is missing. Check your controller action names.", true);
                    return;
                }

                const url = isWorkout ? workoutUrl : dietUrl;

                button.disabled = true;
                button.innerHTML = '<span class="trainer-spinner"></span> Saving...';
                showMessage("");

                try {
                    const response = await fetch(url, {
                        method: "POST",
                        body: formData,
                        credentials: "same-origin",
                        headers: {
                            "X-Requested-With": "XMLHttpRequest",
                            "Accept": "application/json"
                        }
                    });

                    const result = await response.json().catch(() => null);

                    if (!response.ok || !result || result.success !== true) {
                        throw new Error(
                            result?.message ||
                            `Could not save the assignment (HTTP ${response.status}).`
                        );
                    }

                    showMessage(result.message || "Plan assigned successfully.", false);

                    button.innerHTML = '<i class="bi bi-check-circle"></i> Saved';

                    // Keep the success message visible, then close the popup.
                    window.setTimeout(() => {
                        closeModal();
                        window.location.reload();
                    }, 1200);
                } catch (error) {
                    showMessage(error.message || "An unexpected error occurred.", true);
                    button.disabled = false;
                    button.innerHTML = '<i class="bi bi-check-circle"></i> Save assignment';
                }
            });
        }

        function showMessage(message, isError = false) {
            modalMessage.textContent = message;
            modalMessage.className = message
                ? (isError ? "trainer-message error" : "trainer-message success")
                : "";
        }

        // Event delegation: works for all rows, including filtered rows.
        tableBody.addEventListener("click", event => {
            const button = event.target.closest("[data-client-action]");

            if (!button) return;

            const action = button.dataset.clientAction;
            const clientId = button.dataset.clientId;
            const row = getClientRow(clientId);

            if (!row) {
                showModal("Client Not Found", `
                    <p class="trainer-error-text">
                        The selected client could not be found in the table.
                        Refresh the page and try again.
                    </p>
                `);
                return;
            }

            activeClient = readClient(row);

            if (action === "view") {
                showView(activeClient);
            } else if (action === "workout" || action === "diet") {
                showAssignment(activeClient, action);
            }
        });

        // Also handle assignment buttons opened from inside the View popup.
        modal.addEventListener("click", event => {
            const button = event.target.closest(
                '[data-client-action="workout"], [data-client-action="diet"]'
            );

            if (!button) return;

            event.stopPropagation();

            const clientId = button.dataset.clientId;
            const row = getClientRow(clientId);

            if (!row) {
                showMessage("The selected client row was not found. Refresh the page and try again.", true);
                return;
            }

            const client = readClient(row);
            const action = button.dataset.clientAction;

            showAssignment(client, action);
        });
    });
})();