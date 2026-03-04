import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { usePermissions } from '../../hooks/usePermissions';
import { PERMISSIONS } from '../../constants/permissions';
import { addToast } from '../../utils/toast';
import { getProjects, createProject, updateProject, getProjectTemplates, createProjectFromTemplate } from './projectService';
import type { Project, ProjectTemplate } from './types';
import { PROJECT_STATUSES } from './types';
import { ProjectFormModal } from './ProjectFormModal';

function getStatusBadgeClass(status: string): string {
  const s = status?.toLowerCase();
  if (s === 'active') return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300';
  if (s === 'onhold') return 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300';
  if (s === 'completed') return 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300';
  if (s === 'archived') return 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400';
  return 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300';
}

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleDateString(undefined, { dateStyle: 'medium' });
  } catch {
    return iso;
  }
}

const ProjectIcon = () => (
  <svg className="w-5 h-5 text-gray-400 dark:text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" />
  </svg>
);

export function ProjectListPage() {
  const navigate = useNavigate();
  const { hasPermission } = usePermissions();
  const [projects, setProjects] = useState<Project[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingProject, setEditingProject] = useState<Project | null>(null);
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [templates, setTemplates] = useState<ProjectTemplate[]>([]);

  const loadProjects = useCallback(() => {
    setLoading(true);
    setError(null);
    getProjects(statusFilter.trim() || undefined)
      .then((res) => {
        const data = res.data ?? (res as unknown as { Data?: Project[] }).Data;
        if (res.success && Array.isArray(data)) {
          setProjects(data);
        } else {
          const msg = res.error?.message ?? (res as unknown as { error?: { Message?: string } }).error?.Message ?? res.message ?? 'Failed to load projects.';
          setError(msg);
        }
      })
      .catch(() => {
        setError('Network or server error.');
        addToast('error', 'Failed to load projects.');
      })
      .finally(() => setLoading(false));
  }, [statusFilter]);

  useEffect(() => {
    loadProjects();
  }, [loadProjects]);

  const canCreate = hasPermission(PERMISSIONS.ProjectCreate);
  useEffect(() => {
    if (canCreate) {
      getProjectTemplates().then((res) => {
        const data = res.data ?? (res as unknown as { Data?: ProjectTemplate[] }).Data;
        setTemplates(Array.isArray(data) ? data : []);
      });
    }
  }, [canCreate]);

  const handleCreate = () => {
    setEditingProject(null);
    setModalOpen(true);
  };

  const handleEdit = (e: React.MouseEvent, p: Project) => {
    e.stopPropagation();
    setEditingProject(p);
    setModalOpen(true);
  };

  const handleModalSaved = () => {
    loadProjects();
  };

  const handleCloseModal = () => {
    setModalOpen(false);
    setEditingProject(null);
  };

  const canEdit = hasPermission(PERMISSIONS.ProjectEdit);

  if (loading) {
    return (
      <div className="space-y-8">
        <div className="flex flex-col gap-1">
          <h1 className="text-2xl font-semibold tracking-tight text-gray-900 dark:text-slate-100">Projects</h1>
          <p className="text-sm text-gray-500 dark:text-slate-400">Loading your projects…</p>
        </div>
        <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div key={i} className="rounded-xl border border-gray-200/80 dark:border-slate-700/80 bg-white dark:bg-slate-800/80 shadow-sm overflow-hidden animate-pulse">
              <div className="p-5">
                <div className="flex items-start justify-between gap-3">
                  <div className="h-5 w-3/4 bg-gray-200 dark:bg-slate-600 rounded" />
                  <div className="h-8 w-8 bg-gray-100 dark:bg-slate-700 rounded-lg" />
                </div>
                <div className="mt-3 h-4 w-full bg-gray-100 dark:bg-slate-700 rounded" />
                <div className="mt-2 h-4 w-2/3 bg-gray-100 dark:bg-slate-700 rounded" />
                <div className="mt-4 flex gap-2">
                  <div className="h-6 w-16 bg-gray-100 dark:bg-slate-700 rounded-full" />
                  <div className="h-4 w-24 bg-gray-100 dark:bg-slate-700 rounded self-center" />
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="space-y-6">
        <div className="flex flex-col gap-1">
          <h1 className="text-2xl font-semibold tracking-tight text-gray-900 dark:text-slate-100">Projects</h1>
          <p className="text-sm text-gray-500 dark:text-slate-400">Something went wrong.</p>
        </div>
        <div className="rounded-xl border border-red-200 dark:border-red-900/50 bg-red-50/50 dark:bg-red-900/10 p-5">
          <p className="text-sm text-red-700 dark:text-red-300">{error}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      {/* Page header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex flex-col gap-1">
          <h1 className="text-2xl font-semibold tracking-tight text-gray-900 dark:text-slate-100">
            Projects
          </h1>
          <p className="text-sm text-gray-500 dark:text-slate-400">
            Manage and track your project planning and delivery.
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <div className="flex items-center gap-2">
            <label htmlFor="project-status-filter" className="text-sm font-medium text-gray-700 dark:text-slate-300 whitespace-nowrap">
              Status
            </label>
            <select
              id="project-status-filter"
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              className="rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-gray-900 dark:text-slate-200 text-sm px-3 py-2 min-w-[120px] focus:ring-2 focus:ring-primary-500 focus:border-primary-500 transition-shadow"
            >
              <option value="">All statuses</option>
              {PROJECT_STATUSES.map((s) => (
                <option key={s} value={s}>{s}</option>
              ))}
            </select>
          </div>
          {canCreate && (
            <button
              type="button"
              onClick={handleCreate}
              className="inline-flex items-center gap-2 px-4 py-2.5 rounded-lg bg-primary-500 hover:bg-primary-600 text-white text-sm font-medium shadow-sm transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:focus:ring-offset-slate-900"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
              </svg>
              New project
            </button>
          )}
        </div>
      </div>

      {projects.length === 0 ? (
        <div className="rounded-2xl border border-gray-200/80 dark:border-slate-700/80 bg-white dark:bg-slate-800/80 shadow-sm flex flex-col items-center justify-center py-16 px-6 text-center">
          <div className="w-16 h-16 rounded-2xl bg-gray-100 dark:bg-slate-700/80 flex items-center justify-center text-gray-400 dark:text-slate-500 mb-5">
            <ProjectIcon />
          </div>
          <h2 className="text-lg font-semibold text-gray-900 dark:text-slate-100 mb-1">No projects yet</h2>
          <p className="text-sm text-gray-500 dark:text-slate-400 max-w-sm mb-6">
            Create your first project to organize tasks, track progress, and collaborate with your team.
          </p>
          {canCreate && (
            <button
              type="button"
              onClick={handleCreate}
              className="inline-flex items-center gap-2 px-5 py-2.5 rounded-lg bg-primary-500 hover:bg-primary-600 text-white text-sm font-medium shadow-sm transition-colors"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
              </svg>
              Create project
            </button>
          )}
        </div>
      ) : (
        <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
          {projects.map((p) => (
            <article
              key={p.id}
              role="button"
              tabIndex={0}
              onClick={() => navigate(`/projects/${p.id}`)}
              onKeyDown={(e) => e.key === 'Enter' && navigate(`/projects/${p.id}`)}
              className={`group rounded-xl border border-gray-200/80 dark:border-slate-700/80 bg-white dark:bg-slate-800/80 shadow-sm hover:shadow-md transition-all duration-200 cursor-pointer overflow-hidden ${
                p.status?.toLowerCase() === 'archived' ? 'opacity-80' : 'hover:border-gray-300 dark:hover:border-slate-600'
              }`}
            >
              <div className="flex gap-4 p-5">
                <div className="shrink-0 w-10 h-10 rounded-lg bg-gray-100 dark:bg-slate-700/80 flex items-center justify-center text-gray-500 dark:text-slate-400 group-hover:bg-primary-50 dark:group-hover:bg-primary-900/20 transition-colors">
                  <ProjectIcon />
                </div>
                <div className="min-w-0 flex-1">
                  <div className="flex items-start justify-between gap-2">
                    <h3 className="font-semibold text-gray-900 dark:text-slate-100 truncate">
                      {p.name}
                    </h3>
                    {canEdit && (
                      <button
                        type="button"
                        onClick={(e) => handleEdit(e, p)}
                        className="shrink-0 p-1.5 rounded-lg text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:text-slate-500 dark:hover:text-slate-300 dark:hover:bg-slate-700/80 transition-colors"
                        aria-label="Edit project"
                      >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                        </svg>
                      </button>
                    )}
                  </div>
                  {p.description ? (
                    <p className="mt-1.5 text-sm text-gray-500 dark:text-slate-400 line-clamp-2">
                      {p.description}
                    </p>
                  ) : (
                    <p className="mt-1.5 text-sm text-gray-400 dark:text-slate-500 italic">No description</p>
                  )}
                  <div className="mt-4 flex flex-wrap items-center gap-2">
                    <span className={`inline-flex px-2.5 py-1 rounded-full text-xs font-medium ${getStatusBadgeClass(p.status)}`}>
                      {p.status}
                    </span>
                    <span className="text-xs text-gray-400 dark:text-slate-500">
                      Created {formatDate(p.createdAt)}
                    </span>
                  </div>
                </div>
              </div>
            </article>
          ))}
        </div>
      )}

      <ProjectFormModal
        open={modalOpen}
        onClose={handleCloseModal}
        onSaved={handleModalSaved}
        project={editingProject}
        save={async (id, data) => updateProject(id, data)}
        create={createProject}
        templates={templates}
        createFromTemplate={canCreate ? async (templateId, data) => {
          const res = await createProjectFromTemplate(templateId, data);
          return { success: !!res.success, error: res.error };
        } : undefined}
      />
    </div>
  );
}
