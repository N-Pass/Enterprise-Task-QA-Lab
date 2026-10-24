// Thin client for the Tasks REST API. Knows about HTTP and ProblemDetails, nothing about the DOM.

const BASE_URL = '/api/tasks';

/** Error raised for any failed request. `fieldErrors` maps field names to messages. */
export class ApiError extends Error {
  constructor(status, message, fieldErrors = {}) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

/**
 * ProblemDetails "errors" keys come in different shapes: "Title" (DataAnnotations or
 * domain) and "$.priority" (JSON binding). Normalize them to form field names ("title").
 * The generic "request" key carries no field information and is dropped.
 */
function normalizeFieldErrors(errors) {
  const result = {};
  for (const [key, messages] of Object.entries(errors ?? {})) {
    if (key === 'request') continue;
    const name = key.replace(/^\$\./, '');
    const field = name.charAt(0).toLowerCase() + name.slice(1);
    result[field] = messages.join(' ');
  }
  return result;
}

async function request(path, { method = 'GET', body } = {}) {
  let response;
  try {
    response = await fetch(BASE_URL + path, {
      method,
      headers: body ? { 'Content-Type': 'application/json', Accept: 'application/json' } : { Accept: 'application/json' },
      body: body ? JSON.stringify(body) : undefined,
    });
  } catch {
    throw new ApiError(0, 'Could not reach the server. Check your connection and try again.');
  }

  if (response.status === 204) {
    return null;
  }

  const isJson = (response.headers.get('Content-Type') ?? '').includes('json');
  const data = isJson ? await response.json() : null;

  if (!response.ok) {
    const fieldErrors = normalizeFieldErrors(data?.errors);
    const message = Object.keys(fieldErrors).length > 0
      ? 'Please correct the highlighted fields.'
      : data?.detail ?? data?.title ?? `Request failed with status ${response.status}.`;
    throw new ApiError(response.status, message, fieldErrors);
  }

  return data;
}

export function listTasks({ status = '', priority = '' } = {}) {
  const query = new URLSearchParams();
  if (status) query.set('status', status);
  if (priority) query.set('priority', priority);
  const suffix = query.size > 0 ? `?${query}` : '';
  return request(suffix);
}

export const getTask = (id) => request(`/${encodeURIComponent(id)}`);

export const createTask = (task) => request('', { method: 'POST', body: task });

export const updateTask = (id, task) => request(`/${encodeURIComponent(id)}`, { method: 'PUT', body: task });

export const changeTaskStatus = (id, status) =>
  request(`/${encodeURIComponent(id)}/status`, { method: 'PATCH', body: { status } });

export const deleteTask = (id) => request(`/${encodeURIComponent(id)}`, { method: 'DELETE' });
