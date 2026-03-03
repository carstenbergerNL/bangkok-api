import { useState, useEffect, useCallback } from 'react';
import { addToast } from '../../utils/toast';
import { getUsers } from '../../services/userService';
import { getCurrentUserId } from '../../services/authService';
import { getCommentsByTaskId, createComment, updateComment, deleteComment } from './commentService';
import { getActivitiesByTaskId } from './activityService';
import type { User } from '../../models/User';
import { TASK_STATUSES, TASK_PRIORITIES } from './types';
import type { Task, UpdateTaskRequest, TaskComment, TaskActivity } from './types';

function formatRelativeTime(iso: string): string {
  const d = new Date(iso);
  const now = new Date();
  const s = Math.floor((now.getTime() - d.getTime()) / 1000);
  if (s < 60) return 'just now';
  if (s < 3600) return `${Math.floor(s / 60)} min ago`;
  if (s < 86400) return `${Math.floor(s / 3600)} h ago`;
  if (s < 604800) return `${Math.floor(s / 86400)} d ago`;
  return d.toLocaleDateString();
}

interface TaskDrawerProps {
  open: boolean;
  onClose: () => void;
  onSaved: () => void;
  task: Task | null;
  save: (id: string, data: UpdateTaskRequest) => Promise<{ success: boolean; error?: { message?: string } }>;
  canEdit: boolean;
  canDelete?: boolean;
  canComment?: boolean;
  canViewActivity?: boolean;
  onDelete?: (task: Task) => void;
}

type TabId = 'details' | 'comments' | 'activity';

export function TaskDrawer({
  open,
  onClose,
  onSaved,
  task,
  save,
  canEdit,
  canDelete,
  canComment,
  canViewActivity,
  onDelete,
}: TaskDrawerProps) {
  const [activeTab, setActiveTab] = useState<TabId>('details');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [status, setStatus] = useState('ToDo');
  const [priority, setPriority] = useState('Medium');
  const [assignedToUserId, setAssignedToUserId] = useState('');
  const [dueDate, setDueDate] = useState('');
  const [users, setUsers] = useState<User[]>([]);
  const [saving, setSaving] = useState(false);

  const [comments, setComments] = useState<TaskComment[]>([]);
  const [commentsLoading, setCommentsLoading] = useState(false);
  const [newCommentText, setNewCommentText] = useState('');
  const [postingComment, setPostingComment] = useState(false);
  const [editingCommentId, setEditingCommentId] = useState<string | null>(null);
  const [editCommentText, setEditCommentText] = useState('');

  const [activities, setActivities] = useState<TaskActivity[]>([]);
  const [activitiesLoading, setActivitiesLoading] = useState(false);

  const currentUserId = getCurrentUserId();

  const loadUsers = useCallback(() => {
    getUsers(1, 500, false).then((res) => {
      const data = res.data ?? (res as unknown as { Data?: { items?: User[] } }).Data;
      const items = data?.items ?? [];
      setUsers(Array.isArray(items) ? items.filter((u) => !u.isDeleted) : []);
    });
  }, []);

  const loadComments = useCallback((taskId: string) => {
    setCommentsLoading(true);
    getCommentsByTaskId(taskId)
      .then((res) => {
        const data = res.data ?? (res as unknown as { Data?: TaskComment[] }).Data;
        setComments(Array.isArray(data) ? data : []);
      })
      .finally(() => setCommentsLoading(false));
  }, []);

  const loadActivities = useCallback((taskId: string) => {
    if (!canViewActivity) return;
    setActivitiesLoading(true);
    getActivitiesByTaskId(taskId)
      .then((res) => {
        const data = res.data ?? (res as unknown as { Data?: TaskActivity[] }).Data;
        setActivities(Array.isArray(data) ? data : []);
      })
      .finally(() => setActivitiesLoading(false));
  }, [canViewActivity]);

  useEffect(() => {
    if (open && task) {
      loadUsers();
      setTitle(task.title ?? '');
      setDescription(task.description ?? '');
      setStatus(task.status ?? 'ToDo');
      setPriority(task.priority ?? 'Medium');
      setAssignedToUserId(task.assignedToUserId ?? '');
      setDueDate(task.dueDate ? task.dueDate.slice(0, 10) : '');
      setActiveTab('details');
      setNewCommentText('');
      setEditingCommentId(null);
      if (activeTab === 'comments') loadComments(task.id);
      if (activeTab === 'activity') loadActivities(task.id);
    }
  }, [open, task?.id]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (open && task) {
      if (activeTab === 'comments') loadComments(task.id);
      if (activeTab === 'activity') loadActivities(task.id);
    }
  }, [open, task?.id, activeTab, loadComments, loadActivities]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!task || !canEdit) return;
    const trimmed = title.trim();
    if (!trimmed) return;
    setSaving(true);
    try {
      const res = await save(task.id, {
        title: trimmed,
        description: description.trim() || null,
        status,
        priority,
        assignedToUserId: assignedToUserId || null,
        dueDate: dueDate || null,
      });
      if (res.success) {
        addToast('success', 'Task updated.');
        onSaved();
        onClose();
      } else {
        addToast('error', res.error?.message ?? 'Failed to update task.');
      }
    } finally {
      setSaving(false);
    }
  };

  const handleAddComment = async () => {
    if (!task || !canComment || !newCommentText.trim()) return;
    setPostingComment(true);
    const text = newCommentText.trim();
    setNewCommentText('');
    try {
      const res = await createComment(task.id, { content: text });
      const data = res.data ?? (res as unknown as { Data?: TaskComment }).Data;
      if (res.success && data) {
        setComments((prev) => [data, ...prev]);
        addToast('success', 'Comment added.');
      } else {
        setNewCommentText(text);
        addToast('error', res.error?.message ?? 'Failed to add comment.');
      }
    } finally {
      setPostingComment(false);
    }
  };

  const handleStartEditComment = (c: TaskComment) => {
    setEditingCommentId(c.id);
    setEditCommentText(c.content);
  };

  const handleSaveEditComment = async () => {
    if (!editingCommentId || !editCommentText.trim()) return;
    const res = await updateComment(editingCommentId, { content: editCommentText.trim() });
    if (res.success) {
      setComments((prev) => prev.map((c) => (c.id === editingCommentId ? { ...c, content: editCommentText.trim(), updatedAt: new Date().toISOString() } : c)));
      setEditingCommentId(null);
      setEditCommentText('');
      addToast('success', 'Comment updated.');
    } else {
      addToast('error', res.error?.message ?? 'Failed to update comment.');
    }
  };

  const handleCancelEditComment = () => {
    setEditingCommentId(null);
    setEditCommentText('');
  };

  const handleDeleteComment = async (c: TaskComment) => {
    const isOwn = currentUserId && c.userId === currentUserId;
    if (!isOwn && !canDelete) return;
    const res = await deleteComment(c.id);
    if (res.success) {
      setComments((prev) => prev.filter((x) => x.id !== c.id));
      addToast('success', 'Comment deleted.');
    } else {
      addToast('error', res.error?.message ?? 'Failed to delete comment.');
    }
  };

  const canEditComment = (c: TaskComment) => canComment && currentUserId && c.userId === currentUserId;
  const canDeleteComment = (c: TaskComment) => (canComment && currentUserId && c.userId === currentUserId) || (canDelete === true);

  if (!open) return null;

  const borderColor = 'var(--sidebar-border, #edebe9)';
  const headerColor = 'var(--card-header-color, #323130)';
  const descColor = 'var(--card-description-color, #605e5c)';

  return (
    <aside
      className="w-full h-full flex flex-col min-w-0 border-l"
      style={{ backgroundColor: 'var(--sidebar-bg, #faf9f8)', borderLeft: `1px solid ${borderColor}` }}
      role="complementary"
      aria-label="Task details"
    >
        <div className="flex items-center justify-between border-b px-6 py-4 shrink-0" style={{ borderColor }}>
          <h2 className="text-lg font-semibold" style={{ color: headerColor }}>
            {task ? 'Task details' : 'Task'}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-slate-700 text-gray-500 dark:text-slate-400 transition-colors"
            aria-label="Close"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {task && (
          <div className="flex border-b shrink-0" style={{ borderColor }}>
            {['details', 'comments', 'activity'].map((tab) => {
              const id = tab as TabId;
              const label = id === 'details' ? 'Details' : id === 'comments' ? 'Comments' : 'Activity';
              const show = id === 'details' || id === 'comments' || (id === 'activity' && canViewActivity);
              if (!show) return null;
              return (
                <button
                  key={id}
                  type="button"
                  onClick={() => setActiveTab(id)}
                  className={`px-4 py-3 text-sm font-medium transition-colors ${
                    activeTab === id
                      ? 'border-b-2 text-blue-600 dark:text-blue-400'
                      : 'text-gray-600 dark:text-slate-400 hover:text-gray-900 dark:hover:text-slate-200'
                  }`}
                  style={activeTab === id ? { borderBottomColor: 'var(--primary, #2563eb)' } : undefined}
                >
                  {label}
                </button>
              );
            })}
          </div>
        )}

        <div className="flex-1 overflow-y-auto px-6 py-5">
          {!task ? (
            <p className="text-sm" style={{ color: descColor }}>Select a task.</p>
          ) : activeTab === 'details' ? (
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label htmlFor="drawer-title" className="block text-sm font-medium mb-1" style={{ color: headerColor }}>
                  Title <span className="text-red-500">*</span>
                </label>
                <input
                  id="drawer-title"
                  type="text"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  disabled={!canEdit}
                  className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 focus:border-transparent disabled:opacity-70 disabled:cursor-not-allowed"
                  placeholder="Task title"
                />
              </div>
              <div>
                <label htmlFor="drawer-desc" className="block text-sm font-medium mb-1" style={{ color: headerColor }}>
                  Description
                </label>
                <textarea
                  id="drawer-desc"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  disabled={!canEdit}
                  className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 resize-none disabled:opacity-70 disabled:cursor-not-allowed"
                  placeholder="Description"
                  rows={4}
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label htmlFor="drawer-status" className="block text-sm font-medium mb-1" style={{ color: headerColor }}>
                    Status
                  </label>
                  <select
                    id="drawer-status"
                    value={status}
                    onChange={(e) => setStatus(e.target.value)}
                    disabled={!canEdit}
                    className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 disabled:opacity-70 disabled:cursor-not-allowed"
                  >
                    {TASK_STATUSES.map((s) => (
                      <option key={s} value={s}>{s === 'ToDo' ? 'Todo' : s === 'InProgress' ? 'In Progress' : s}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label htmlFor="drawer-priority" className="block text-sm font-medium mb-1" style={{ color: headerColor }}>
                    Priority
                  </label>
                  <select
                    id="drawer-priority"
                    value={priority}
                    onChange={(e) => setPriority(e.target.value)}
                    disabled={!canEdit}
                    className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 disabled:opacity-70 disabled:cursor-not-allowed"
                  >
                    {TASK_PRIORITIES.map((p) => (
                      <option key={p} value={p}>{p}</option>
                    ))}
                  </select>
                </div>
              </div>
              <div>
                <label htmlFor="drawer-assignee" className="block text-sm font-medium mb-1" style={{ color: headerColor }}>
                  Assigned to
                </label>
                <select
                  id="drawer-assignee"
                  value={assignedToUserId}
                  onChange={(e) => setAssignedToUserId(e.target.value)}
                  disabled={!canEdit}
                  className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 disabled:opacity-70 disabled:cursor-not-allowed"
                >
                  <option value="">Unassigned</option>
                  {users.map((u) => (
                    <option key={u.id} value={u.id}>{u.displayName || u.email}</option>
                  ))}
                </select>
              </div>
              <div>
                <label htmlFor="drawer-due" className="block text-sm font-medium mb-1" style={{ color: headerColor }}>
                  Due date
                </label>
                <input
                  id="drawer-due"
                  type="date"
                  value={dueDate}
                  onChange={(e) => setDueDate(e.target.value)}
                  disabled={!canEdit}
                  className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 disabled:opacity-70 disabled:cursor-not-allowed"
                />
              </div>
              <div className="pt-4 space-y-2">
                {canEdit && (
                  <button
                    type="submit"
                    disabled={saving || !title.trim()}
                    className="w-full px-4 py-2.5 rounded-xl bg-blue-600 text-white font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
                  >
                    {saving ? 'Saving…' : 'Save'}
                  </button>
                )}
                {canDelete && onDelete && (
                  <button
                    type="button"
                    onClick={() => { onDelete(task); onClose(); }}
                    className="w-full px-4 py-2.5 rounded-xl border border-red-200 dark:border-red-900/50 text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
                  >
                    Delete task
                  </button>
                )}
              </div>
            </form>
          ) : activeTab === 'comments' ? (
            <div className="flex flex-col h-full min-h-0">
              {canComment && (
                <div className="shrink-0 mb-4">
                  <textarea
                    value={newCommentText}
                    onChange={(e) => setNewCommentText(e.target.value)}
                    placeholder="Add a comment…"
                    className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 resize-none"
                    rows={2}
                  />
                  <button
                    type="button"
                    onClick={handleAddComment}
                    disabled={postingComment || !newCommentText.trim()}
                    className="mt-2 px-4 py-2 rounded-lg bg-blue-600 text-white text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
                  >
                    {postingComment ? 'Adding…' : 'Add Comment'}
                  </button>
                </div>
              )}
              <div className="flex-1 overflow-y-auto min-h-0 space-y-4">
                {commentsLoading ? (
                  <div className="animate-pulse space-y-3">
                    {[1, 2, 3].map((i) => (
                      <div key={i} className="flex gap-3">
                        <div className="w-8 h-8 rounded-full bg-gray-200 dark:bg-slate-600 shrink-0" />
                        <div className="flex-1 space-y-2">
                          <div className="h-3 w-1/3 rounded bg-gray-200 dark:bg-slate-600" />
                          <div className="h-4 w-full rounded bg-gray-200 dark:bg-slate-600" />
                        </div>
                      </div>
                    ))}
                  </div>
                ) : comments.length === 0 ? (
                  <p className="text-sm py-4" style={{ color: descColor }}>No comments yet.</p>
                ) : (
                  comments.map((c) => (
                    <div key={c.id} className="flex gap-3">
                      <div
                        className="w-8 h-8 rounded-full shrink-0 flex items-center justify-center text-white text-sm font-medium"
                        style={{ backgroundColor: 'var(--primary, #2563eb)' }}
                      >
                        {(c.userDisplayName || '?').charAt(0).toUpperCase()}
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <span className="text-sm font-medium" style={{ color: headerColor }}>
                            {c.userDisplayName || 'Unknown'}
                          </span>
                          <span className="text-xs" style={{ color: descColor }}>
                            {formatRelativeTime(c.createdAt)}
                          </span>
                          {(canEditComment(c) || canDeleteComment(c)) && editingCommentId !== c.id && (
                            <span className="flex gap-1 ml-auto">
                              {canEditComment(c) && (
                                <button
                                  type="button"
                                  onClick={() => handleStartEditComment(c)}
                                  className="text-xs text-blue-600 dark:text-blue-400 hover:underline"
                                >
                                  Edit
                                </button>
                              )}
                              {canDeleteComment(c) && (
                                <button
                                  type="button"
                                  onClick={() => handleDeleteComment(c)}
                                  className="text-xs text-red-600 dark:text-red-400 hover:underline"
                                >
                                  Delete
                                </button>
                              )}
                            </span>
                          )}
                        </div>
                        {editingCommentId === c.id ? (
                          <div className="mt-1">
                            <textarea
                              value={editCommentText}
                              onChange={(e) => setEditCommentText(e.target.value)}
                              className="w-full px-2 py-1 text-sm rounded border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800"
                              rows={2}
                            />
                            <div className="flex gap-2 mt-1">
                              <button
                                type="button"
                                onClick={handleSaveEditComment}
                                className="text-xs px-2 py-1 rounded bg-blue-600 text-white hover:bg-blue-700"
                              >
                                Save
                              </button>
                              <button
                                type="button"
                                onClick={handleCancelEditComment}
                                className="text-xs px-2 py-1 rounded border border-gray-200 dark:border-slate-600 hover:bg-gray-50 dark:hover:bg-slate-700"
                              >
                                Cancel
                              </button>
                            </div>
                          </div>
                        ) : (
                          <p className="text-sm mt-0.5 whitespace-pre-wrap break-words" style={{ color: descColor }}>
                            {c.content}
                          </p>
                        )}
                      </div>
                    </div>
                  ))
                )}
              </div>
            </div>
          ) : activeTab === 'activity' ? (
            <div className="space-y-0">
              {activitiesLoading ? (
                <div className="animate-pulse space-y-4">
                  {[1, 2, 3, 4].map((i) => (
                    <div key={i} className="flex gap-3">
                      <div className="w-8 h-8 rounded-full bg-gray-200 dark:bg-slate-600 shrink-0" />
                      <div className="flex-1 space-y-2">
                        <div className="h-4 w-full rounded bg-gray-200 dark:bg-slate-600" />
                        <div className="h-3 w-1/4 rounded bg-gray-200 dark:bg-slate-600" />
                      </div>
                    </div>
                  ))}
                </div>
              ) : activities.length === 0 ? (
                <p className="text-sm py-4" style={{ color: descColor }}>No activity yet.</p>
              ) : (
                <ul className="relative space-y-4">
                  {activities.map((a, idx) => (
                    <li key={a.id} className="flex gap-3 relative">
                      {idx < activities.length - 1 && (
                        <div
                          className="absolute left-4 top-8 bottom-0 w-0.5 -mb-4"
                          style={{ backgroundColor: 'var(--dropdown-border, #edebe9)' }}
                        />
                      )}
                      <div
                        className="w-8 h-8 rounded-full shrink-0 flex items-center justify-center bg-gray-100 dark:bg-slate-700"
                        style={{ color: headerColor }}
                        aria-hidden
                      >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                      </div>
                      <div className="flex-1 min-w-0 pb-4">
                        <p className="text-sm" style={{ color: headerColor }}>
                          {a.description}
                        </p>
                        <p className="text-xs mt-0.5" style={{ color: descColor }}>
                          {formatRelativeTime(a.createdAt)}
                          {a.userDisplayName ? ` · ${a.userDisplayName}` : ''}
                        </p>
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          ) : null}
        </div>
      </aside>
  );
}
