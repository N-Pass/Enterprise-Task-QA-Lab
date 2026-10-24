// UI logic: renders tasks and reacts to user actions. All HTTP calls go through api.js.
import { ApiError, createTask, getTask, listTasks, updateTask } from './api.js';

const STATUS_LABELS = { ToDo: 'To Do', InProgress: 'In Progress', Done: 'Done' };

// Mirrors the server-side rules for immediate feedback; the API remains the source of truth.
const LIMITS = { title: 120, description: 1000, assignedTo: 100 };
const FORM_FIELDS = ['title', 'description', 'priority', 'assignedTo'];

const elements = {
  message: document.getElementById('message'),
  filterStatus: document.getElementById('filter-status'),
  filterPriority: document.getElementById('filter-priority'),
  clearFilters: document.getElementById('clear-filters'),
  taskCount: document.getElementById('task-count'),
  taskList: document.getElementById('task-list'),
  emptyState: document.getElementById('empty-state'),
  taskTemplate: document.getElementById('task-item-template'),
  newTaskButton: document.getElementById('new-task-button'),
  taskDialog: document.getElementById('task-dialog'),
  taskDialogTitle: document.getElementById('task-dialog-title'),
  taskForm: document.getElementById('task-form'),
  formError: document.getElementById('form-error'),
  saveButton: document.getElementById('save-task-button'),
  inputs: {
    title: document.getElementById('task-title'),
    description: document.getElementById('task-description'),
    priority: document.getElementById('task-priority'),
    assignedTo: document.getElementById('task-assignee'),
  },
  fieldErrors: {
    title: document.getElementById('task-title-error'),
    description: document.getElementById('task-description-error'),
    priority: document.getElementById('task-priority-error'),
    assignedTo: document.getElementById('task-assignee-error'),
  },
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

  // Every card has an "Edit" button; the label tells screen reader users which task it acts on.
  item.querySelector('[data-action="edit"]').setAttribute('aria-label', `Edit task: ${task.title}`);

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

// ---- Create / edit form ----

// Id of the task being edited, or null when creating.
let editingTaskId = null;

function readForm() {
  const { title, description, priority, assignedTo } = elements.inputs;
  return {
    title: title.value.trim(),
    description: description.value.trim() || null,
    priority: priority.value,
    assignedTo: assignedTo.value.trim() || null,
  };
}

function validate(task) {
  const errors = {};
  if (!task.title) {
    errors.title = 'Title is required.';
  } else if (task.title.length > LIMITS.title) {
    errors.title = `Title must not exceed ${LIMITS.title} characters.`;
  }
  if (task.description && task.description.length > LIMITS.description) {
    errors.description = `Description must not exceed ${LIMITS.description} characters.`;
  }
  if (task.assignedTo && task.assignedTo.length > LIMITS.assignedTo) {
    errors.assignedTo = `Assigned to must not exceed ${LIMITS.assignedTo} characters.`;
  }
  return errors;
}

function clearFormErrors() {
  elements.formError.hidden = true;
  elements.formError.textContent = '';
  for (const field of FORM_FIELDS) {
    elements.inputs[field].removeAttribute('aria-invalid');
    elements.fieldErrors[field].hidden = true;
    elements.fieldErrors[field].textContent = '';
  }
}

/** Shows messages next to their fields and moves focus to the first invalid one. */
function showFieldErrors(errors) {
  let firstInvalid = null;
  for (const field of FORM_FIELDS) {
    if (!errors[field]) continue;
    elements.inputs[field].setAttribute('aria-invalid', 'true');
    elements.fieldErrors[field].textContent = errors[field];
    elements.fieldErrors[field].hidden = false;
    firstInvalid ??= elements.inputs[field];
  }
  firstInvalid?.focus();
}

function showFormError(text) {
  elements.formError.textContent = text;
  elements.formError.hidden = false;
}

function openTaskForm(task = null) {
  editingTaskId = task?.id ?? null;
  elements.taskForm.reset();
  clearFormErrors();

  elements.taskDialogTitle.textContent = task ? 'Edit task' : 'New task';
  elements.saveButton.textContent = task ? 'Save changes' : 'Create task';

  if (task) {
    elements.inputs.title.value = task.title;
    elements.inputs.description.value = task.description ?? '';
    elements.inputs.priority.value = task.priority;
    elements.inputs.assignedTo.value = task.assignedTo ?? '';
  }

  elements.taskDialog.showModal();
  elements.inputs.title.focus();
}

async function openEditForm(id) {
  try {
    // Load the latest version so the form never edits stale data.
    openTaskForm(await getTask(id));
  } catch (error) {
    showMessage(`Could not open the task. ${errorText(error)}`, 'error');
    if (error instanceof ApiError && error.status === 404) {
      loadTasks();
    }
  }
}

async function handleTaskFormSubmit(event) {
  event.preventDefault();
  clearFormErrors();

  const task = readForm();
  const errors = validate(task);
  if (Object.keys(errors).length > 0) {
    showFieldErrors(errors);
    return;
  }

  elements.saveButton.disabled = true;
  try {
    if (editingTaskId) {
      await updateTask(editingTaskId, task);
      showMessage(`Task "${task.title}" was updated.`);
    } else {
      await createTask(task);
      showMessage(`Task "${task.title}" was created.`);
    }
    elements.taskDialog.close();
    await loadTasks();
  } catch (error) {
    if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
      showFieldErrors(error.fieldErrors);
    }
    showFormError(errorText(error));
  } finally {
    elements.saveButton.disabled = false;
  }
}

// ---- Event wiring ----

elements.newTaskButton.addEventListener('click', () => openTaskForm());
elements.taskForm.addEventListener('submit', handleTaskFormSubmit);

// Any element with data-action="close-dialog" closes the dialog that contains it.
document.addEventListener('click', (event) => {
  const closeButton = event.target.closest('[data-action="close-dialog"]');
  closeButton?.closest('dialog')?.close();
});

// Event delegation: one listener handles the buttons of every task card.
elements.taskList.addEventListener('click', (event) => {
  const button = event.target.closest('button[data-action]');
  const taskId = button?.closest('[data-task-id]')?.dataset.taskId;
  if (!taskId) return;

  if (button.dataset.action === 'edit') {
    openEditForm(taskId);
  }
});

elements.filterStatus.addEventListener('change', loadTasks);
elements.filterPriority.addEventListener('change', loadTasks);
elements.clearFilters.addEventListener('click', () => {
  elements.filterStatus.value = '';
  elements.filterPriority.value = '';
  loadTasks();
});

loadTasks();
