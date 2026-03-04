import { useCallback, useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { usePermissions } from '../../hooks/usePermissions';
import { PERMISSIONS } from '../../constants/permissions';
import { getUsers } from '../../services/userService';
import { getProject, updateProject, deleteProject, exportProjectToCsv } from './projectService';
import { getMyProjectRole } from './memberService';
import { addToast } from '../../utils/toast';
import { Modal } from '../../components/Modal';
import type { Project, ProjectMemberRole } from './types';
import { PROJECT_STATUSES } from './types';
import { ProjectFormModal } from './ProjectFormModal';
import { TaskList } from './TaskList';
import { ProjectMembersTab } from './ProjectMembersTab';
import { ProjectDashboardTab } from './ProjectDashboardTab';
import { ProjectLabelsSettings } from './ProjectLabelsSettings';
import { ProjectCustomFieldsSettings } from './ProjectCustomFieldsSettings';
import { ProjectAutomationSettings } from './ProjectAutomationSettings';

function getStatusBadgeClass(status: string): string {
  const s = status?.toLowerCase();
  if (s === 'active') return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300';
  if (s === 'onhold') return 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300';
  if (s === 'completed') return 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300';
  if (s === 'archived') return 'bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300';
  return 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300';
}

export function ProjectDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { hasPermission } = usePermissions();
  const [project, setProject] = useState<Project | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [userMap, setUserMap] = useState<Map<string, string>>(new Map());
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [activeTab, setActiveTab] = useState<'board' | 'members' | 'dashboard' | 'settings'>('dashboard');
  const [settingsSubTab, setSettingsSubTab] = useState<'badges' | 'custom-fields' | 'automation'>('badges');
  const [myRole, setMyRole] = useState<ProjectMemberRole | null>(null);
  const [statusChanging, setStatusChanging] = useState(false);
  const [exporting, setExporting] = useState(false);

  const loadProject = useCallback(() => {
    if (!id) return;
    setLoading(true);
    setNotFound(false);
    getProject(id)
      .then((res) => {
        const data = res.data ?? (res as unknown as { Data?: Project }).Data;
        if (res.success && data) {
          setProject(data);
        } else if (res.error?.message?.toLowerCase().includes('not found') || (res as unknown as { status?: number }).status === 404) {
          setNotFound(true);
        } else {
          addToast('error', res.error?.message ?? 'Failed to load project.');
        }
      })
      .catch(() => {
        addToast('error', 'Failed to load project.');
      })
      .finally(() => setLoading(false));
  }, [id]);

  const loadUsers = useCallback(() => {
    getUsers(1, 500, false).then((res) => {
      const data = res.data ?? (res as unknown as { Data?: { items?: { id: string; displayName?: string; email: string }[] } }).Data;
      const items = data?.items ?? [];
      const map = new Map<string, string>();
      (Array.isArray(items) ? items : []).forEach((u: { id: string; displayName?: string; email: string }) => {
        map.set(u.id, u.displayName || u.email || u.id);
      });
      setUserMap(map);
    });
  }, []);

  const loadMyRole = useCallback(() => {
    if (!id) return;
    getMyProjectRole(id).then((res) => {
      const data = res.data ?? (res as unknown as { data?: { role?: ProjectMemberRole } }).data;
      const role = data?.role ?? null;
      setMyRole(role ?? null);
    });
  }, [id]);

  useEffect(() => {
    loadProject();
    loadUsers();
  }, [loadProject, loadUsers]);

  useEffect(() => {
    if (project?.id) loadMyRole();
  }, [project?.id, loadMyRole]);

  const handleEditSaved = useCallback(() => {
    loadProject();
    setEditModalOpen(false);
  }, [loadProject]);

  const handleDeleteClick = () => setDeleteConfirmOpen(true);
  const handleDeleteCancel = () => setDeleteConfirmOpen(false);

  const handleDeleteConfirm = async () => {
    if (!project) return;
    setDeleting(true);
    try {
      const res = await deleteProject(project.id);
      if (res.success) {
        addToast('success', 'Project deleted.');
        navigate('/projects', { replace: true });
      } else {
        const msg = res.error?.message ?? (res as unknown as { error?: { Message?: string } }).error?.Message ?? 'Failed to delete project.';
        addToast('error', msg);
      }
    } finally {
      setDeleting(false);
    }
  };

  const canEdit = hasPermission(PERMISSIONS.ProjectEdit);
  const canDelete = hasPermission(PERMISSIONS.ProjectDelete);
  const isAdmin = hasPermission(PERMISSIONS.ViewAdminSettings);
  const canManageMembers = myRole === 'Owner' || isAdmin;
  const canChangeStatus = canManageMembers;
  const showMembersTab = myRole != null && myRole !== 'Viewer';
  const isArchived = project?.status?.toLowerCase() === 'archived';

  const handleStatusChange = useCallback(
    async (newStatus: string) => {
      if (!project || statusChanging) return;
      setStatusChanging(true);
      try {
        const res = await updateProject(project.id, { status: newStatus });
        if (res.success) {
          addToast('success', 'Project status updated.');
          loadProject();
        } else {
          addToast('error', res.error?.message ?? 'Failed to update status.');
        }
      } finally {
        setStatusChanging(false);
      }
    },
    [project, statusChanging, loadProject]
  );

  if (notFound) {
    return (
      <div className="space-y-6">
        <div className="rounded-2xl border border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800/80 shadow-sm text-center py-16 px-6">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-slate-100 mb-1">Project not found</h2>
          <p className="text-sm text-gray-500 dark:text-slate-400 mb-5">The project may have been removed or you don&apos;t have access.</p>
          <button
            type="button"
            onClick={() => navigate('/projects')}
            className="text-primary-500 hover:text-primary-600 dark:text-primary-400 dark:hover:text-primary-300 font-medium text-sm"
          >
            ← Back to projects
          </button>
        </div>
      </div>
    );
  }

  if (loading || !project) {
    return (
      <div className="space-y-6">
        <div className="animate-pulse space-y-4">
          <div className="h-8 w-72 bg-gray-200 dark:bg-slate-600 rounded-lg" />
          <div className="h-4 max-w-md bg-gray-100 dark:bg-slate-700 rounded" />
          <div className="h-28 bg-gray-100 dark:bg-slate-700 rounded-xl" />
        </div>
      </div>
    );
  }

  const tabClass = (tab: typeof activeTab) =>
    `px-4 py-3 text-sm font-medium transition-colors border-b-2 -mb-px whitespace-nowrap ${
      activeTab === tab
        ? 'border-primary-500 text-primary-600 dark:text-primary-400 dark:border-primary-400'
        : 'border-transparent text-gray-500 dark:text-slate-400 hover:text-gray-700 dark:hover:text-slate-300 hover:border-gray-200 dark:hover:border-slate-600'
    }`;

  return (
    <div className="space-y-6">
      {/* Project header */}
      <header
        className={`rounded-2xl border border-gray-200/80 dark:border-slate-700/80 bg-white dark:bg-slate-800/80 shadow-sm overflow-hidden ${isArchived ? 'opacity-90' : ''}`}
      >
        <div className="p-5 md:p-6">
          <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
            <div className="min-w-0 flex-1">
              <h1 className="text-xl md:text-2xl font-semibold tracking-tight text-gray-900 dark:text-slate-100">
                {project.name}
              </h1>
              {project.description && (
                <p className="mt-1.5 text-sm text-gray-500 dark:text-slate-400 max-w-2xl">
                  {project.description}
                </p>
              )}
              <div className="mt-4 flex flex-wrap items-center gap-2">
                <span className={`inline-flex px-2.5 py-1 rounded-full text-xs font-medium ${getStatusBadgeClass(project.status)}`}>
                  {project.status}
                </span>
                {canChangeStatus && (
                  <select
                    value={project.status}
                    onChange={(e) => handleStatusChange(e.target.value)}
                    disabled={statusChanging}
                    className="text-sm rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-gray-700 dark:text-slate-300 px-2.5 py-1 focus:ring-2 focus:ring-primary-500 focus:border-primary-500 disabled:opacity-50"
                    aria-label="Change project status"
                  >
                    {PROJECT_STATUSES.map((s) => (
                      <option key={s} value={s}>{s}</option>
                    ))}
                  </select>
                )}
              </div>
            </div>
            <div className="flex flex-wrap items-center gap-2 shrink-0">
              {project.id && (
                <button
                  type="button"
                  onClick={async () => {
                    if (!project.id || exporting) return;
                    setExporting(true);
                    try {
                      await exportProjectToCsv(project.id);
                      addToast('success', 'Export downloaded.');
                    } catch {
                      addToast('error', 'Export failed.');
                    } finally {
                      setExporting(false);
                    }
                  }}
                  disabled={exporting}
                  className="inline-flex items-center gap-2 px-4 py-2 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-gray-700 dark:text-slate-300 text-sm font-medium hover:bg-gray-50 dark:hover:bg-slate-700/80 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {exporting ? 'Exporting…' : 'Export'}
                </button>
              )}
              {canEdit && (
                <button
                  type="button"
                  onClick={() => setEditModalOpen(true)}
                  className="inline-flex items-center gap-2 px-4 py-2 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-gray-700 dark:text-slate-300 text-sm font-medium hover:bg-gray-50 dark:hover:bg-slate-700/80 transition-colors"
                >
                  Edit
                </button>
              )}
              {canDelete && (
                <button
                  type="button"
                  onClick={handleDeleteClick}
                  className="inline-flex items-center gap-2 px-4 py-2 rounded-lg border border-red-200 dark:border-red-900/50 text-red-600 dark:text-red-400 text-sm font-medium hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
                >
                  Delete
                </button>
              )}
            </div>
          </div>
        </div>
      </header>

      {/* Tabs + content */}
      <div className="rounded-2xl border border-gray-200/80 dark:border-slate-700/80 bg-white dark:bg-slate-800/80 shadow-sm overflow-hidden">
        <div className="border-b border-gray-200 dark:border-slate-700 bg-gray-50/50 dark:bg-slate-900/50 px-2">
          <nav className="flex gap-1 overflow-x-auto scrollbar-hide" aria-label="Project sections">
            <button type="button" onClick={() => setActiveTab('dashboard')} className={tabClass('dashboard')}>
              Dashboard
            </button>
            <button type="button" onClick={() => setActiveTab('board')} className={tabClass('board')}>
              Board
            </button>
            {showMembersTab && (
              <button type="button" onClick={() => setActiveTab('members')} className={tabClass('members')}>
                Members
              </button>
            )}
            {canEdit && (
              <button type="button" onClick={() => setActiveTab('settings')} className={tabClass('settings')}>
                Settings
              </button>
            )}
          </nav>
        </div>
        <div className="p-5 md:p-6">
          {activeTab === 'board' && project.id && (
            <TaskList projectId={project.id} userMap={userMap} isProjectArchived={isArchived} />
          )}
          {activeTab === 'members' && project.id && (
            <ProjectMembersTab
              projectId={project.id}
              canManageMembers={canManageMembers}
              onMembersChange={loadMyRole}
            />
          )}
          {activeTab === 'dashboard' && project.id && (
            <ProjectDashboardTab projectId={project.id} />
          )}
          {activeTab === 'settings' && project.id && (
            <div>
              <div className="flex flex-wrap gap-1 border-b border-gray-200 dark:border-slate-600 pb-3 mb-4">
                {(['badges', 'custom-fields', 'automation'] as const).map((tab) => (
                  <button
                    key={tab}
                    type="button"
                    onClick={() => setSettingsSubTab(tab)}
                    className={`px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                      settingsSubTab === tab
                        ? 'bg-primary-50 dark:bg-primary-900/20 text-primary-600 dark:text-primary-400'
                        : 'text-gray-600 dark:text-slate-400 hover:bg-gray-100 dark:hover:bg-slate-700/80 hover:text-gray-900 dark:hover:text-slate-200'
                    }`}
                  >
                    {tab === 'badges' ? 'Badges' : tab === 'custom-fields' ? 'Custom Fields' : 'Automation'}
                  </button>
                ))}
              </div>
              {settingsSubTab === 'badges' && <ProjectLabelsSettings projectId={project.id} />}
              {settingsSubTab === 'custom-fields' && <ProjectCustomFieldsSettings projectId={project.id} />}
              {settingsSubTab === 'automation' && <ProjectAutomationSettings projectId={project.id} />}
            </div>
          )}
        </div>
      </div>

      <ProjectFormModal
        open={editModalOpen}
        onClose={() => setEditModalOpen(false)}
        onSaved={handleEditSaved}
        project={project}
        save={updateProject}
        create={async () => ({ success: false })}
      />

      {deleteConfirmOpen && (
        <Modal open={deleteConfirmOpen} onClose={handleDeleteCancel} title="Delete project">
          <p className="text-sm text-gray-500 dark:text-slate-400 mb-5">
            Delete &quot;{project.name}&quot;? If the project has tasks, you must delete them first.
          </p>
          <div className="flex justify-end gap-3">
            <button type="button" onClick={handleDeleteCancel} className="px-4 py-2 rounded-lg border border-gray-200 dark:border-slate-600 text-gray-700 dark:text-slate-300 hover:bg-gray-50 dark:hover:bg-slate-700 transition-colors font-medium">
              Cancel
            </button>
            <button type="button" onClick={handleDeleteConfirm} disabled={deleting} className="px-4 py-2 rounded-lg bg-red-600 text-white hover:bg-red-700 disabled:opacity-50 transition-colors font-medium">
              {deleting ? 'Deleting…' : 'Delete'}
            </button>
          </div>
        </Modal>
      )}
    </div>
  );
}
