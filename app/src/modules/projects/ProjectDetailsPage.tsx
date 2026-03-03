import { useCallback, useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { usePermissions } from '../../hooks/usePermissions';
import { PERMISSIONS } from '../../constants/permissions';
import { getUsers } from '../../services/userService';
import { getProject, updateProject, deleteProject } from './projectService';
import { getMyProjectRole } from './memberService';
import { addToast } from '../../utils/toast';
import { Modal } from '../../components/Modal';
import type { Project, ProjectMemberRole } from './types';
import { ProjectFormModal } from './ProjectFormModal';
import { TaskList } from './TaskList';
import { ProjectMembersTab } from './ProjectMembersTab';
import { ProjectDashboardTab } from './ProjectDashboardTab';
import { ProjectLabelsSettings } from './ProjectLabelsSettings';

function getStatusBadgeClass(status: string): string {
  const s = status?.toLowerCase();
  if (s === 'active') return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300';
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
  const [myRole, setMyRole] = useState<ProjectMemberRole | null>(null);

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
  const showMembersTab = myRole != null && myRole !== 'Viewer';

  if (notFound) {
    return (
      <div className="space-y-6">
        <div className="card card-body rounded-xl shadow-sm text-center py-12">
          <h2 className="text-lg font-semibold mb-1" style={{ color: 'var(--card-header-color, #323130)' }}>Project not found</h2>
          <p className="text-sm mb-4" style={{ color: 'var(--card-description-color, #605e5c)' }}>The project may have been removed or you don&apos;t have access.</p>
          <button type="button" onClick={() => navigate('/projects')} className="text-blue-600 dark:text-blue-400 hover:underline">
            Back to projects
          </button>
        </div>
      </div>
    );
  }

  if (loading || !project) {
    return (
      <div className="space-y-6">
        <div className="animate-pulse">
          <div className="h-8 w-64 bg-gray-200 dark:bg-slate-600 rounded mb-2" />
          <div className="h-4 w-full max-w-md bg-gray-100 dark:bg-slate-700 rounded mb-6" />
          <div className="h-32 bg-gray-100 dark:bg-slate-700 rounded-xl" />
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="card card-body rounded-xl shadow-sm border border-gray-100 dark:border-slate-700/50">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <h1 className="text-xl font-semibold" style={{ color: 'var(--card-header-color, #323130)' }}>{project.name}</h1>
            {project.description && (
              <p className="mt-1 text-sm" style={{ color: 'var(--card-description-color, #605e5c)' }}>{project.description}</p>
            )}
            <div className="mt-3">
              <span className={`inline-flex px-2.5 py-1 rounded-full text-xs font-medium ${getStatusBadgeClass(project.status)}`}>
                {project.status}
              </span>
            </div>
          </div>
          <div className="flex items-center gap-2">
            {canEdit && (
              <button
                type="button"
                onClick={() => setEditModalOpen(true)}
                className="inline-flex items-center gap-2 px-4 py-2 rounded-xl border border-gray-200 dark:border-slate-600 hover:bg-gray-50 dark:hover:bg-slate-700 transition-colors text-sm font-medium"
              >
                Edit
              </button>
            )}
            {canDelete && (
              <button
                type="button"
                onClick={handleDeleteClick}
                className="inline-flex items-center gap-2 px-4 py-2 rounded-xl border border-red-200 dark:border-red-900/50 text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors text-sm font-medium"
              >
                Delete
              </button>
            )}
          </div>
        </div>
      </div>

      <div className="rounded-xl shadow-sm border border-gray-100 dark:border-slate-700/50 overflow-hidden" style={{ backgroundColor: 'var(--card-bg, #ffffff)' }}>
        <div className="flex border-b border-gray-100 dark:border-slate-700/50">
          <button
            type="button"
            onClick={() => setActiveTab('dashboard')}
            className={`px-5 py-3 text-sm font-medium transition-colors ${
              activeTab === 'dashboard'
                ? 'border-b-2 text-blue-600 dark:text-blue-400 border-blue-600 dark:border-blue-400'
                : 'text-gray-600 dark:text-slate-400 hover:text-gray-900 dark:hover:text-slate-200'
            }`}
          >
            Dashboard
          </button>
          <button
            type="button"
            onClick={() => setActiveTab('board')}
            className={`px-5 py-3 text-sm font-medium transition-colors ${
              activeTab === 'board'
                ? 'border-b-2 text-blue-600 dark:text-blue-400 border-blue-600 dark:border-blue-400'
                : 'text-gray-600 dark:text-slate-400 hover:text-gray-900 dark:hover:text-slate-200'
            }`}
          >
            Board
          </button>
          {showMembersTab && (
            <button
              type="button"
              onClick={() => setActiveTab('members')}
              className={`px-5 py-3 text-sm font-medium transition-colors ${
                activeTab === 'members'
                  ? 'border-b-2 text-blue-600 dark:text-blue-400 border-blue-600 dark:border-blue-400'
                  : 'text-gray-600 dark:text-slate-400 hover:text-gray-900 dark:hover:text-slate-200'
              }`}
            >
              Members
            </button>
          )}
          {canEdit && (
            <button
              type="button"
              onClick={() => setActiveTab('settings')}
              className={`px-5 py-3 text-sm font-medium transition-colors ${
                activeTab === 'settings'
                  ? 'border-b-2 text-blue-600 dark:text-blue-400 border-blue-600 dark:border-blue-400'
                  : 'text-gray-600 dark:text-slate-400 hover:text-gray-900 dark:hover:text-slate-200'
              }`}
            >
              Settings
            </button>
          )}
        </div>
        <div className="p-4 md:p-6">
          {activeTab === 'board' && project.id && (
            <TaskList projectId={project.id} userMap={userMap} />
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
            <ProjectLabelsSettings projectId={project.id} />
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
          <p className="text-sm mb-4" style={{ color: 'var(--card-description-color, #605e5c)' }}>
            Delete &quot;{project.name}&quot;? If the project has tasks, you must delete them first.
          </p>
          <div className="flex justify-end gap-2">
            <button type="button" onClick={handleDeleteCancel} className="px-4 py-2 rounded-lg border border-gray-200 dark:border-slate-600 hover:bg-gray-50 dark:hover:bg-slate-700 transition-colors">
              Cancel
            </button>
            <button type="button" onClick={handleDeleteConfirm} disabled={deleting} className="px-4 py-2 rounded-lg bg-red-600 text-white hover:bg-red-700 disabled:opacity-50 transition-colors">
              {deleting ? 'Deleting…' : 'Delete'}
            </button>
          </div>
        </Modal>
      )}
    </div>
  );
}
