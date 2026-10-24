// UI logic: renders tasks and reacts to user actions. All HTTP calls go through api.js.
import { ApiError, listTasks } from './api.js';

const STATUS_LABELS = { ToDo: 'To Do', InProgress: 'In Progress', Done: 'Done' };

const elements = {
  message: document.getElementById('message'),
  filterStatus: document.getElementById('filter-status'),
  filterPriority: document.getElementById('filter-priority'),
  clearFilters: document.getElementById('clear-filters'),
  taskCount: document.getElementById('task-count'),
  taskList: document.getElementById('task-list'),
  emptyState: document.getElementById('empty-state'),
  taskTemplate: document.getElementById('task-item-template'),
};

// ---- Messages ----

let messageTimer;

/** Shows a success or error message in the live region (announced by screen readers). */
function showMessage(text, type = 'success') {
  clearTimeout(messageTimer);
  elements.message.textContent = text;
  elements.message.dataset.type = type;
  elements.message.hidden = false;
  if (type === 'success') {
    messageTimer = setTimeout(hideMessage, 5000);
  }
}

function hideMessage() {
  elements.message.hidden = true;
  elements.message.textContent = '';
}

function errorText(error) {
  return error instanceof ApiError ? error.message : 'An unexpected error occurred.';
}

// ---- Task list ----

function renderTask(task) {
  const item = elements.taskTemplate.content.firstElementChild.cloneNode(true);
  item.dataset.taskId = task.id;

  item.querySelector('[data-testid="task-title"]').textContent = task.title;

  const priority = item.querySelector('[data-testid="task-priority"]');
  priority.textContent = `${task.priority} priority`;
  priority.classList.add(`badge-${task.priority.toLowerCase()}`);

  const status = item.querySelector('[data-testid="task-status-label"]');
  status.textContent = STATUS_LABELS[task.status] ?? task.status;
  status.dataset.status = task.status;

  item.querySelector('[data-testid="task-assignee"]').textContent =
    task.assignedTo ? `Assigned to ${task.assignedTo}` : 'Unassigned';

  return item;
}

function renderTasks(tasks) {
  elements.taskList.replaceChildren(...tasks.map(renderTask));
  elements.emptyState.hidden = tasks.length > 0;
  elements.taskCount.textContent = tasks.length === 1 ? '1 task' : `${tasks.length} tasks`;
}

// Filter changes can overlap; only the most recent request may update the list.
let latestRequestId = 0;

async function loadTasks() {
  const requestId = ++latestRequestId;
  elements.taskList.setAttribute('aria-busy', 'true');
  try {
    const tasks = await listTasks({
      status: elements.filterStatus.value,
      priority: elements.filterPriority.value,
    });
    if (requestId === latestRequestId) {
      renderTasks(tasks);
    }
  } catch (error) {
    if (requestId === latestRequestId) {
      showMessage(`Could not load tasks. ${errorText(error)}`, 'error');
    }
  } finally {
    if (requestId === latestRequestId) {
      elements.taskList.setAttribute('aria-busy', 'false');
    }
  }
}

// ---- Event wiring ----

elements.filterStatus.addEventListener('change', loadTasks);
elements.filterPriority.addEventListener('change', loadTasks);
elements.clearFilters.addEventListener('click', () => {
  elements.filterStatus.value = '';
  elements.filterPriority.value = '';
  loadTasks();
});

loadTasks();
