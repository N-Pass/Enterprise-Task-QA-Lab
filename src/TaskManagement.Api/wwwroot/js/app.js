// UI logic: renders tasks and reacts to user actions. All HTTP calls go through api.js.
import { ApiError, changeTaskStatus, createTask, deleteTask, getTask, listTasks, updateTask } from './api.js';

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
  detailsDialog: document.getElementById('details-dialog'),
  detailsEditButton: document.getElementById('details-edit-button'),
  deleteDialog: document.getElementById('delete-dialog'),
  deleteDescription: document.getElementById('delete-description'),
  confirmDeleteButton: document.getElementById('confirm-delete-button'),
};

const dateFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' });

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

const isNotFound = (error) => error instanceof ApiError && error.status === 404;

function errorText(error) {
  if (isNotFound(error)) {
    // The API's 404 detail contains the raw id, which means nothing to a user.
    return 'This task no longer exists. It may have been deleted. The list has been refreshed.';
  }
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

  const statusSelect = item.querySelector('[data-action="change-status"]');
  statusSelect.value = task.status;

  // Every card repeats the same controls; labels tell screen reader users which task they act on.
  statusSelect.setAttribute('aria-label', `Status of task: ${task.title}`);
  item.querySelector('[data-action="details"]').setAttribute('aria-label', `Details of task: ${task.title}`);
  item.querySelector('[data-action="edit"]').setAttribute('aria-label', `Edit task: ${task.title}`);
  item.querySelector('[data-action="delete"]').setAttribute('aria-label', `Delete task: ${task.title}`);

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
    if (isNotFound(error)) {
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
    if (isNotFound(error)) {
      await loadTasks();
    }
  } finally {
    elements.saveButton.disabled = false;
  }
}

// ---- Details ----

let detailsTaskId = null;

function setDate(element, isoValue) {
  element.dateTime = isoValue;
  element.textContent = dateFormat.format(new Date(isoValue));
}

async function openDetails(id) {
  try {
    const task = await getTask(id);
    detailsTaskId = task.id;
    const field = (name) => elements.detailsDialog.querySelector(`[data-testid="details-${name}"]`);
    field('title').textContent = task.title;
    field('description').textContent = task.description ?? 'No description';
    field('status').textContent = STATUS_LABELS[task.status] ?? task.status;
    field('priority').textContent = task.priority;
    field('assignee').textContent = task.assignedTo ?? 'Unassigned';
    setDate(field('created'), task.createdAtUtc);
    setDate(field('updated'), task.updatedAtUtc);
    elements.detailsDialog.showModal();
  } catch (error) {
    showMessage(`Could not load the task details. ${errorText(error)}`, 'error');
    if (isNotFound(error)) {
      loadTasks();
    }
  }
}

// ---- Status change ----

async function handleStatusChange(select, taskId) {
  const card = select.closest('[data-task-id]');
  const title = card.querySelector('[data-testid="task-title"]').textContent;
  const previousStatus = card.querySelector('[data-testid="task-status-label"]').dataset.status;

  select.disabled = true;
  try {
    const updated = await changeTaskStatus(taskId, select.value);
    showMessage(`Status of "${title}" changed to ${STATUS_LABELS[updated.status]}.`);
    await loadTasks();
  } catch (error) {
    // Revert so the screen never shows a status that was not saved.
    select.value = previousStatus;
    showMessage(`Could not change the status. ${errorText(error)}`, 'error');
    if (isNotFound(error)) {
      await loadTasks();
    }
  } finally {
    select.disabled = false;
  }
}

// ---- Delete ----

let pendingDelete = null;

function openDeleteConfirmation(taskId, title) {
  pendingDelete = { taskId, title };
  elements.deleteDescription.textContent = `"${title}" will be permanently deleted. This cannot be undone.`;
  elements.deleteDialog.showModal();
}

async function confirmDelete() {
  if (!pendingDelete) return;
  const { taskId, title } = pendingDelete;

  elements.confirmDeleteButton.disabled = true;
  try {
    await deleteTask(taskId);
    showMessage(`Task "${title}" was deleted.`);
  } catch (error) {
    showMessage(`Could not delete the task. ${errorText(error)}`, 'error');
  } finally {
    elements.confirmDeleteButton.disabled = false;
    pendingDelete = null;
    elements.deleteDialog.close();
    // Reload in both cases: after a 404 the task is gone anyway.
    await loadTasks();
  }
}

// ---- Event wiring ----

elements.newTaskButton.addEventListener('click', () => openTaskForm());
elements.confirmDeleteButton.addEventListener('click', confirmDelete);
elements.detailsEditButton.addEventListener('click', () => {
  elements.detailsDialog.close();
  openEditForm(detailsTaskId);
});
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

  const title = button.closest('[data-task-id]').querySelector('[data-testid="task-title"]').textContent;

  switch (button.dataset.action) {
    case 'details':
      openDetails(taskId);
      break;
    case 'edit':
      openEditForm(taskId);
      break;
    case 'delete':
      openDeleteConfirmation(taskId, title);
      break;
  }
});

elements.taskList.addEventListener('change', (event) => {
  const select = event.target.closest('select[data-action="change-status"]');
  const taskId = select?.closest('[data-task-id]')?.dataset.taskId;
  if (taskId) {
    handleStatusChange(select, taskId);
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
