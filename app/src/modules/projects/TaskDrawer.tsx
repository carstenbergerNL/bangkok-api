import { useState, useEffect, useCallback, useRef } from 'react';
import { addToast } from '../../utils/toast';
import { getUsers, searchUsersForMention, type MentionUser } from '../../services/userService';
import { getCurrentUserId } from '../../services/authService';
import { getCommentsByTaskId, createComment, updateComment, deleteComment } from './commentService';
import { getActivitiesByTaskId } from './activityService';
import { getLabels } from './labelService';
import { getTimeLogs, createTimeLog, deleteTimeLog, updateTask } from './taskService';
import type { User } from '../../models/User';
import { TASK_STATUSES, TASK_PRIORITIES } from './types';
import type { Task, UpdateTaskRequest, TaskComment, TaskActivity, TaskTimeLog, Label } from './types';

function CommentContentWithMentions({ content }: { content: string }) {
  if (!content) return null;
  const parts = content.split(/(@\S+)/g);
  return (
    <>
      {parts.map((part, i) =>
        part.startsWith('@') ? (
          <span key={i} className="text-blue-600 dark:text-blue-400 font-medium">
            {part}
          </span>
        ) : (
          <span key={i}>{part}</span>
        )
      )}
    </>
  );
}

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
  const [estimatedHours, setEstimatedHours] = useState<string>('');
  const [selectedLabelIds, setSelectedLabelIds] = useState<string[]>([]);
  const [projectLabels, setProjectLabels] = useState<Label[]>([]);
  const [users, setUsers] = useState<User[]>([]);
  const [saving, setSaving] = useState(false);

  const [comments, setComments] = useState<TaskComment[]>([]);
  const [commentsLoading, setCommentsLoading] = useState(false);
  const [newCommentText, setNewCommentText] = useState('');
  const [postingComment, setPostingComment] = useState(false);
  const [editingCommentId, setEditingCommentId] = useState<string | null>(null);
  const [editCommentText, setEditCommentText] = useState('');

  const [mentionOpen, setMentionOpen] = useState(false);
  const [mentionQuery, setMentionQuery] = useState('');
  const [mentionOptions, setMentionOptions] = useState<MentionUser[]>([]);
  const [mentionStartIndex, setMentionStartIndex] = useState(0);
  const [mentionLoading, setMentionLoading] = useState(false);
  const commentTextareaRef = useRef<HTMLTextAreaElement>(null);
  const commentCursorRef = useRef(0);

  const [activities, setActivities] = useState<TaskActivity[]>([]);
  const [activitiesLoading, setActivitiesLoading] = useState(false);

  const [timeLogs, setTimeLogs] = useState<TaskTimeLog[]>([]);
  const [timeLogsLoading, setTimeLogsLoading] = useState(false);
  const [logHours, setLogHours] = useState('');
  const [logDescription, setLogDescription] = useState('');
  const [postingTimeLog, setPostingTimeLog] = useState(false);

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

  const loadProjectLabels = useCallback((projectId: string) => {
    getLabels(projectId).then((res) => {
      const data = res.data ?? (res as unknown as { Data?: Label[] }).Data;
      setProjectLabels(Array.isArray(data) ? data : []);
    });
  }, []);

  const loadTimeLogs = useCallback((taskId: string) => {
    setTimeLogsLoading(true);
    getTimeLogs(taskId)
      .then((res) => {
        const data = res.data ?? (res as unknown as { Data?: TaskTimeLog[] }).Data;
        setTimeLogs(Array.isArray(data) ? data : []);
      })
      .finally(() => setTimeLogsLoading(false));
  }, []);

  useEffect(() => {
    if (open && task) {
      loadUsers();
      loadProjectLabels(task.projectId);
      setTitle(task.title ?? '');
      setDescription(task.description ?? '');
      setStatus(task.status ?? 'ToDo');
      setPriority(task.priority ?? 'Medium');
      setAssignedToUserId(task.assignedToUserId ?? '');
      setDueDate(task.dueDate ? task.dueDate.slice(0, 10) : '');
      setEstimatedHours(task.estimatedHours != null ? String(task.estimatedHours) : '');
      setSelectedLabelIds(task.labels?.map((l) => l.id) ?? []);
      setActiveTab('details');
      setNewCommentText('');
      setEditingCommentId(null);
      setLogHours('');
      setLogDescription('');
      loadTimeLogs(task.id);
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

  useEffect(() => {
    if (!mentionQuery.trim()) {
      setMentionOptions([]);
      return;
    }
    setMentionLoading(true);
    const t = setTimeout(() => {
      searchUsersForMention(mentionQuery, 15).then((res) => {
        const data = res.data ?? (res as { Data?: MentionUser[] }).Data;
        setMentionOptions(Array.isArray(data) ? data : []);
      }).finally(() => setMentionLoading(false));
    }, 200);
    return () => clearTimeout(t);
  }, [mentionQuery]);

  const handleCommentInputChange = useCallback((e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const value = e.target.value;
    const cursor = e.target.selectionStart ?? 0;
    commentCursorRef.current = cursor;
    setNewCommentText(value);
    const textBeforeCursor = value.slice(0, cursor);
    const lastAt = textBeforeCursor.lastIndexOf('@');
    if (lastAt !== -1) {
      const fromAt = textBeforeCursor.slice(lastAt + 1);
      if (!/\s/.test(fromAt)) {
        setMentionStartIndex(lastAt);
        setMentionQuery(fromAt);
        setMentionOpen(true);
        return;
      }
    }
    setMentionOpen(false);
  }, []);

  const handleSelectMention = useCallback((user: MentionUser) => {
    const d = (user.displayName || '').trim();
    const mention = d && !d.includes(' ') ? d : user.email;
    const start = mentionStartIndex;
    const end = commentCursorRef.current;
    const before = newCommentText.slice(0, start);
    const after = newCommentText.slice(end);
    const insert = `@${mention} `;
    setNewCommentText(before + insert + after);
    setMentionOpen(false);
    setMentionOptions([]);
    setMentionQuery('');
    setTimeout(() => {
      commentTextareaRef.current?.focus();
      const pos = start + insert.length;
      commentTextareaRef.current?.setSelectionRange(pos, pos);
    }, 0);
  }, [newCommentText, mentionStartIndex]);

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
        estimatedHours: (() => {
            const v = estimatedHours.trim();
            if (v === '') return null;
            const n = parseFloat(v);
            return Number.isNaN(n) ? null : n;
          })(),
        labelIds: selectedLabelIds,
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

  const totalLoggedHours = timeLogs.reduce((sum, l) => sum + l.hours, 0);

  const handleAddTimeLog = async () => {
    if (!task || !canEdit) return;
    const hoursNum = parseFloat(logHours);
    if (Number.isNaN(hoursNum) || hoursNum < 0.01 || hoursNum > 999.99) {
      addToast('error', 'Enter hours between 0.01 and 999.99.');
      return;
    }
    setPostingTimeLog(true);
    try {
      const res = await createTimeLog(task.id, { hours: hoursNum, description: logDescription.trim() || null });
      const data = res.data ?? (res as unknown as { Data?: TaskTimeLog }).Data;
      if (res.success && data) {
        setTimeLogs((prev) => [data, ...prev]);
        setLogHours('');
        setLogDescription('');
        addToast('success', 'Time logged.');
      } else {
        addToast('error', res.error?.message ?? 'Failed to log time.');
      }
    } finally {
      setPostingTimeLog(false);
    }
  };

  const handleDeleteTimeLog = async (log: TaskTimeLog) => {
    if (!canEdit) return;
    const res = await deleteTimeLog(log.id);
    if (res.success) {
      setTimeLogs((prev) => prev.filter((x) => x.id !== log.id));
      addToast('success', 'Time log removed.');
    } else {
      addToast('error', res.error?.message ?? 'Failed to remove time log.');
    }
  };

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
              {projectLabels.length > 0 && (
                <div>
                  <span className="block text-sm font-medium mb-2" style={{ color: headerColor }}>
                    Labels
                  </span>
                  <div className="flex flex-wrap gap-2">
                    {projectLabels.map((label) => {
                      const isSelected = selectedLabelIds.includes(label.id);
                      return (
                        <label
                          key={label.id}
                          className={`inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg text-xs font-medium border cursor-pointer transition-colors ${
                            canEdit ? 'hover:opacity-90' : ''
                          } ${isSelected ? 'ring-2 ring-offset-1 ring-blue-500' : 'border-gray-200 dark:border-slate-600'}`}
                          style={{ backgroundColor: isSelected ? label.color : 'var(--dropdown-bg, #ffffff)', color: isSelected ? (label.color === '#ffffff' || label.color === '#fff' ? '#333' : '#fff') : undefined }}
                        >
                          <input
                            type="checkbox"
                            checked={isSelected}
                            onChange={(e) => {
                              if (!canEdit) return;
                              setSelectedLabelIds((prev) =>
                                e.target.checked ? [...prev, label.id] : prev.filter((id) => id !== label.id)
                              );
                            }}
                            disabled={!canEdit}
                            className="sr-only"
                          />
                          <span style={isSelected && (label.color === '#ffffff' || label.color === '#fff') ? { color: '#333' } : undefined}>
                            {label.name}
                          </span>
                        </label>
                      );
                    })}
                  </div>
                </div>
              )}
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
              <div>
                <label htmlFor="drawer-estimated-hours" className="block text-sm font-medium mb-1" style={{ color: headerColor }}>
                  Estimated hours
                </label>
                <input
                  id="drawer-estimated-hours"
                  type="number"
                  min={0}
                  step={0.25}
                  placeholder="Optional"
                  value={estimatedHours}
                  onChange={(e) => setEstimatedHours(e.target.value)}
                  disabled={!canEdit}
                  className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 disabled:opacity-70 disabled:cursor-not-allowed"
                />
              </div>
              <div className="border-t border-gray-200 dark:border-slate-600 pt-4 mt-2">
                <h4 className="text-sm font-semibold mb-3" style={{ color: headerColor }}>
                  Time tracking
                </h4>
                <div className="flex items-end gap-2 mb-3">
                  <span className="text-sm font-medium" style={{ color: descColor }}>
                    Total: {totalLoggedHours.toFixed(2)} h
                    {task?.estimatedHours != null && task.estimatedHours > 0 && (
                      <span className="ml-2">
                        {totalLoggedHours > task.estimatedHours ? (
                          <span className="text-red-600 dark:text-red-400 font-medium">Over budget</span>
                        ) : (
                          <span className="text-gray-500 dark:text-slate-400">
                            / {task.estimatedHours} h estimated
                          </span>
                        )}
                      </span>
                    )}
                  </span>
                </div>
                {canEdit && (
                  <div className="space-y-2 mb-4">
                    <div className="flex gap-2 flex-wrap items-end">
                      <div className="flex-1 min-w-[80px]">
                        <label htmlFor="drawer-log-hours" className="block text-xs font-medium mb-0.5" style={{ color: descColor }}>
                          Hours
                        </label>
                        <input
                          id="drawer-log-hours"
                          type="number"
                          min={0.01}
                          max={999.99}
                          step={0.25}
                          placeholder="0"
                          value={logHours}
                          onChange={(e) => setLogHours(e.target.value)}
                          className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 text-sm"
                        />
                      </div>
                      <div className="flex-[2] min-w-[140px]">
                        <label htmlFor="drawer-log-desc" className="block text-xs font-medium mb-0.5" style={{ color: descColor }}>
                          Description
                        </label>
                        <input
                          id="drawer-log-desc"
                          type="text"
                          placeholder="Optional"
                          value={logDescription}
                          onChange={(e) => setLogDescription(e.target.value)}
                          className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 text-sm"
                        />
                      </div>
                      <button
                        type="button"
                        onClick={handleAddTimeLog}
                        disabled={postingTimeLog || !logHours.trim()}
                        className="px-4 py-2 rounded-lg bg-blue-600 text-white text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
                      >
                        {postingTimeLog ? 'Logging…' : 'Log time'}
                      </button>
                    </div>
                  </div>
                )}
                {timeLogsLoading ? (
                  <div className="animate-pulse space-y-2">
                    {[1, 2, 3].map((i) => (
                      <div key={i} className="h-10 rounded bg-gray-200 dark:bg-slate-600" />
                    ))}
                  </div>
                ) : timeLogs.length === 0 ? (
                  <p className="text-sm py-2" style={{ color: descColor }}>No time logged yet.</p>
                ) : (
                  <ul className="space-y-2 max-h-48 overflow-y-auto">
                    {timeLogs.map((log) => (
                      <li
                        key={log.id}
                        className="flex items-start justify-between gap-2 py-2 px-3 rounded-lg bg-gray-50 dark:bg-slate-800/60 border border-gray-100 dark:border-slate-700"
                      >
                        <div className="min-w-0 flex-1">
                          <div className="flex items-center gap-2 flex-wrap">
                            <span className="text-sm font-medium" style={{ color: headerColor }}>
                              {log.userDisplayName ?? 'Unknown'}
                            </span>
                            <span className="text-sm font-medium text-blue-600 dark:text-blue-400">
                              {log.hours} h
                            </span>
                          </div>
                          {log.description && (
                            <p className="text-sm mt-0.5 truncate" style={{ color: descColor }} title={log.description}>
                              {log.description}
                            </p>
                          )}
                          <p className="text-xs mt-0.5" style={{ color: descColor }}>
                            {log.createdAt ? new Date(log.createdAt).toLocaleString() : ''}
                          </p>
                        </div>
                        {canEdit && (
                          <button
                            type="button"
                            onClick={() => handleDeleteTimeLog(log)}
                            className="shrink-0 p-1.5 rounded text-gray-500 hover:text-red-600 hover:bg-red-50 dark:hover:text-red-400 dark:hover:bg-red-900/20 transition-colors"
                            title="Remove time log"
                          >
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                            </svg>
                          </button>
                        )}
                      </li>
                    ))}
                  </ul>
                )}
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
                <div className="shrink-0 mb-4 relative">
                  <textarea
                    ref={commentTextareaRef}
                    value={newCommentText}
                    onChange={handleCommentInputChange}
                    onBlur={() => setTimeout(() => setMentionOpen(false), 150)}
                    placeholder="Add a comment… Use @ to mention someone."
                    className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 resize-none"
                    rows={2}
                  />
                  {mentionOpen && (
                    <div
                      className="absolute left-0 right-0 top-full mt-0.5 z-10 py-1 rounded-lg border border-gray-200 dark:border-slate-600 shadow-lg bg-white dark:bg-slate-800 max-h-48 overflow-y-auto"
                    >
                      {mentionLoading ? (
                        <div className="px-3 py-2 text-sm text-gray-500 dark:text-slate-400">Searching…</div>
                      ) : mentionOptions.length === 0 ? (
                        <div className="px-3 py-2 text-sm text-gray-500 dark:text-slate-400">No users found.</div>
                      ) : (
                        mentionOptions.map((u) => (
                          <button
                            key={u.id}
                            type="button"
                            onClick={() => handleSelectMention(u)}
                            className="w-full text-left px-3 py-2 text-sm hover:bg-gray-100 dark:hover:bg-slate-700 transition-colors flex flex-col"
                          >
                            <span style={{ color: headerColor }}>{u.displayName || u.email}</span>
                            {u.displayName && <span className="text-xs text-gray-500 dark:text-slate-400">{u.email}</span>}
                          </button>
                        ))
                      )}
                    </div>
                  )}
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
                            <CommentContentWithMentions content={c.content} />
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
