const state = { snapshot: null, editingId: null, toastTimer: null };
const $ = (selector) => document.querySelector(selector);
const escapeHtml = (value) => String(value ?? "").replace(/[&<>"']/g, (char) => ({
  "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
}[char]));

async function api(path, options = {}) {
  const response = await fetch(path, {
    ...options,
    headers: { ...(options.body ? { "Content-Type": "application/json" } : {}), ...options.headers }
  });
  if (!response.ok) {
    let message = `Request failed (${response.status}).`;
    try { message = (await response.json()).error || message; } catch { /* keep the HTTP status message */ }
    throw new Error(message);
  }
  return response.status === 204 ? null : response.json();
}

function showToast(message) {
  const toast = $("#toast");
  toast.textContent = message;
  toast.classList.add("visible");
  clearTimeout(state.toastTimer);
  state.toastTimer = setTimeout(() => toast.classList.remove("visible"), 3000);
}

function fieldInput(field, value = null) {
  const types = { text: "text", number: "number", date: "date", boolean: "checkbox" };
  const checked = field.type === "boolean" && value === true ? "checked" : "";
  const inputValue = field.type === "boolean" ? "" : `value="${escapeHtml(value ?? "")}"`;
  return `<label class="dynamic-field">${escapeHtml(field.name)}<input data-field-id="${field.id}" type="${types[field.type]}" ${inputValue} ${checked}></label>`;
}

function renderFormOptions() {
  const data = state.snapshot;
  const select = $("#category-select");
  const selectedCategory = select.value;
  select.innerHTML = `<option value="">Choose a category</option>${data.categories.map(category =>
    `<option value="${category.id}">${escapeHtml(category.name)}</option>`).join("")}`;
  if (data.categories.some(category => String(category.id) === selectedCategory)) select.value = selectedCategory;

  $("#tag-options").innerHTML = data.tags.length
    ? data.tags.map(tag => `<label class="tag-choice"><input type="checkbox" name="tagId" value="${tag.id}"><span>${escapeHtml(tag.name)}</span></label>`).join("")
    : `<span class="muted">Create tags in library structure.</span>`;

  $("#custom-inputs").innerHTML = data.customFields.map(field =>
    fieldInput(field, state.editingId ? getEditingItem()?.customValues?.[field.id] : null)).join("");
}

function getEditingItem() {
  return state.snapshot.items.find(item => item.id === state.editingId);
}

function renderStats() {
  const items = state.snapshot.items;
  const rated = items.filter(item => item.rating !== null);
  const average = rated.length
    ? Math.round(rated.reduce((sum, item) => sum + item.score, 0) / rated.length)
    : "—";
  $("#stat-total").textContent = items.length;
  $("#stat-consumed").textContent = items.filter(item => item.status === "Consumed").length;
  $("#stat-score").textContent = average === "—" ? average : `${average}/100`;
  $("#tab-count").textContent = items.length;
}

function renderItems() {
  const query = $("#search").value.trim().toLowerCase();
  const items = state.snapshot.items.filter(item =>
    [item.title, item.category, ...item.tags].some(value => value.toLowerCase().includes(query)));
  $("#visible-count").textContent = `${items.length} ${items.length === 1 ? "item" : "items"}`;
  $("#empty-state").classList.toggle("hidden", items.length > 0);
  if (state.snapshot.items.length > 0 && items.length === 0) {
    $("#empty-state").innerHTML = `<span>⌕</span><h3>No matches this time.</h3><p>Try another title, category, or tag.</p>`;
  } else {
    $("#empty-state").innerHTML = `<span>✳</span><h3>Your next favorite starts here.</h3><p>Add a film, a book, a series, or anything you want to remember.</p>`;
  }
  $("#media-list").innerHTML = items.map(item => {
    const score = item.score === null ? "—" : item.score;
    const tags = item.tags.length
      ? `<div class="media-tags">${item.tags.map(tag => `<span class="media-tag">${escapeHtml(tag)}</span>`).join("")}</div>`
      : "";
    const values = state.snapshot.customFields
      .filter(field => Object.hasOwn(item.customValues, field.id))
      .map(field => `<span><strong>${escapeHtml(field.name)}:</strong> ${escapeHtml(item.customValues[field.id])}</span>`)
      .join("");
    const action = item.status === "Planned"
      ? `<button class="text-button" data-action="consumed" data-id="${item.id}">Mark as experienced</button>`
      : `<form class="rate-form" data-id="${item.id}"><input aria-label="Rating from 1 to 10" name="rating" type="number" min="1" max="10" value="${item.rating ?? ""}" required><button type="submit">Rate</button></form>`;
    return `<article class="media-card">
      <div class="media-main">
        <div class="media-title-row"><h3 class="media-title">${escapeHtml(item.title)}</h3><span class="status ${item.status === "Consumed" ? "consumed" : ""}">${item.status === "Consumed" ? "experienced" : "on your list"}</span></div>
        <div class="media-meta"><span>${escapeHtml(item.category)}</span><span>·</span><span>${item.rating === null ? "Not rated yet" : `Your rating: ${item.rating}/10`}</span></div>
        ${tags}${values ? `<div class="custom-values">${values}</div>` : ""}
        <div class="media-actions">${action}<span class="action-separator">·</span><button class="text-button" data-action="edit" data-id="${item.id}">Edit</button><span class="action-separator">·</span><button class="text-button danger" data-action="delete" data-id="${item.id}">Remove</button></div>
      </div>
      <div class="score-badge"><strong>${score}</strong><span>SCORE</span></div>
    </article>`;
  }).join("");
}

function renderStructure() {
  $("#category-list").innerHTML = state.snapshot.categories.length
    ? state.snapshot.categories.map(category => `<span class="pill">${escapeHtml(category.name)} <small>${state.snapshot.items.filter(item => item.categoryId === category.id).length}</small></span>`).join("")
    : `<span class="muted">No categories yet.</span>`;
  $("#tag-list").innerHTML = state.snapshot.tags.length
    ? state.snapshot.tags.map(tag => `<span class="pill">${escapeHtml(tag.name)} <small>${state.snapshot.items.filter(item => item.tagIds.includes(tag.id)).length}</small></span>`).join("")
    : `<span class="muted">No tags yet.</span>`;
  $("#field-count").textContent = state.snapshot.customFields.length;
  $("#field-list").innerHTML = state.snapshot.customFields.length
    ? state.snapshot.customFields.map(field => `<div class="field-row"><strong>${escapeHtml(field.name)}</strong><code>${escapeHtml(field.type)}</code><div class="field-actions"><button data-field-action="rename" data-id="${field.id}">Rename</button><button data-field-action="delete" data-id="${field.id}">Delete</button></div></div>`).join("")
    : `<p class="muted">No custom fields yet — add a detail you want to keep.</p>`;
}

function render() {
  renderStats();
  renderFormOptions();
  renderItems();
  renderStructure();
}

async function refresh() {
  state.snapshot = await api("/api/watchlist");
  render();
}

function customValuesFromForm() {
  const values = {};
  for (const input of $("#custom-inputs").querySelectorAll("[data-field-id]")) {
    let value;
    if (input.type === "checkbox") value = input.checked;
    else if (input.value === "") continue;
    else if (input.type === "number") value = Number(input.value);
    else value = input.value;
    values[input.dataset.fieldId] = value;
  }
  return values;
}

function resetMediaForm() {
  state.editingId = null;
  $("#media-form").reset();
  $("#form-title").textContent = "Add to your library";
  $("#submit-media").innerHTML = "Add to library <span>↗</span>";
  $("#cancel-edit").classList.add("hidden");
  renderFormOptions();
}

async function submitMediaForm(event) {
  event.preventDefault();
  const isEditing = state.editingId !== null;
  const form = new FormData(event.currentTarget);
  const body = {
    title: form.get("title"),
    categoryId: Number(form.get("categoryId")),
    tagIds: form.getAll("tagId").map(Number),
    customValues: customValuesFromForm()
  };
  const path = state.editingId ? `/api/media/${state.editingId}` : "/api/media";
  await api(path, { method: state.editingId ? "PUT" : "POST", body: JSON.stringify(body) });
  resetMediaForm();
  await refresh();
  showToast(isEditing ? "Your changes are saved." : "Added to your library.");
}

$("#media-form").addEventListener("submit", async event => {
  try { await submitMediaForm(event); }
  catch (error) { showToast(error.message); }
});

$("#cancel-edit").addEventListener("click", resetMediaForm);
$("#search").addEventListener("input", renderItems);

$("#category-form").addEventListener("submit", async event => {
  event.preventDefault();
  try {
    await api("/api/categories", { method: "POST", body: JSON.stringify({ name: new FormData(event.currentTarget).get("name") }) });
    event.currentTarget.reset();
    await refresh();
    showToast("Category created.");
  } catch (error) { showToast(error.message); }
});

$("#tag-form").addEventListener("submit", async event => {
  event.preventDefault();
  try {
    await api("/api/tags", { method: "POST", body: JSON.stringify({ name: new FormData(event.currentTarget).get("name") }) });
    event.currentTarget.reset();
    await refresh();
    showToast("Tag created.");
  } catch (error) { showToast(error.message); }
});

$("#field-form").addEventListener("submit", async event => {
  event.preventDefault();
  try {
    const data = new FormData(event.currentTarget);
    await api("/api/custom-fields", { method: "POST", body: JSON.stringify({ name: data.get("name"), type: data.get("type") }) });
    event.currentTarget.reset();
    await refresh();
    showToast("Custom field added to your library.");
  } catch (error) { showToast(error.message); }
});

document.addEventListener("click", async event => {
  const tab = event.target.closest("[data-tab]");
  if (tab) {
    document.querySelectorAll(".tab").forEach(button => button.classList.toggle("active", button === tab));
    $("#library-panel").classList.toggle("hidden", tab.dataset.tab !== "library");
    $("#structure-panel").classList.toggle("hidden", tab.dataset.tab !== "structure");
    return;
  }

  const action = event.target.closest("[data-action]");
  if (action) {
    const id = Number(action.dataset.id);
    try {
      if (action.dataset.action === "consumed") {
        await api(`/api/media/${id}/consumed`, { method: "POST" });
        await refresh();
        showToast("Marked as experienced.");
      } else if (action.dataset.action === "edit") {
        const item = state.snapshot.items.find(entry => entry.id === id);
        state.editingId = id;
        renderFormOptions();
        $("#media-form").elements.title.value = item.title;
        $("#category-select").value = item.categoryId;
        $("#media-form").querySelectorAll('input[name="tagId"]').forEach(input => {
          input.checked = item.tagIds.includes(Number(input.value));
        });
        $("#form-title").textContent = "Edit library entry";
        $("#submit-media").innerHTML = "Save changes <span>↗</span>";
        $("#cancel-edit").classList.remove("hidden");
        $("#media-form").scrollIntoView({ behavior: "smooth", block: "center" });
      } else if (action.dataset.action === "delete" &&
          confirm("Remove this item from your library? Its saved custom-field values will also be removed.")) {
        await api(`/api/media/${id}`, { method: "DELETE" });
        await refresh();
        showToast("Removed from your library.");
      }
    } catch (error) { showToast(error.message); }
  }

  const fieldAction = event.target.closest("[data-field-action]");
  if (fieldAction) {
    const field = state.snapshot.customFields.find(item => item.id === Number(fieldAction.dataset.id));
    try {
      if (fieldAction.dataset.fieldAction === "rename") {
        const name = prompt("New name for this field:", field.name);
        if (name === null || !name.trim()) return;
        await api(`/api/custom-fields/${field.id}`, {
          method: "PUT",
          body: JSON.stringify({ name: name.trim(), type: field.type })
        });
        await refresh();
        showToast("Custom field renamed.");
      } else if (confirm(`Delete “${field.name}” and all values stored for it? This cannot be undone.`)) {
        await api(`/api/custom-fields/${field.id}`, { method: "DELETE" });
        await refresh();
        showToast("Custom field and its values deleted.");
      }
    } catch (error) { showToast(error.message); }
  }
});

document.addEventListener("submit", async event => {
  const form = event.target.closest(".rate-form");
  if (!form) return;
  event.preventDefault();
  try {
    const rating = Number(new FormData(form).get("rating"));
    await api(`/api/media/${form.dataset.id}/rating`, { method: "PUT", body: JSON.stringify({ rating }) });
    await refresh();
    showToast("Your rating is saved.");
  } catch (error) { showToast(error.message); }
});

refresh().catch(error => {
  $("#empty-state").classList.remove("hidden");
  $("#empty-state").innerHTML = `<span>!</span><h3>Could not connect to your library.</h3><p>${escapeHtml(error.message)} Start PostgreSQL, then reload this page.</p>`;
  showToast(error.message);
});
