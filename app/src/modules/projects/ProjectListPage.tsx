import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { usePermissions } from '../../hooks/usePermissions';
import { PERMISSIONS } from '../../constants/permissions';
import { addToast } from '../../utils/toast';
import { getProjects, createProject, updateProject } from './projectService';
import type { Project } from './types';
import { ProjectFormModal } from './ProjectFormModal';

function getStatusBadgeClass(status: string): string {
  const s = status?.toLowerCase();
  if (s === 'active') return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300';
  if (s === 'archived') return 'bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300';
  return 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300';
}

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleDateString(undefined, { dateStyle: 'medium' });
  } catch {
    return iso;
  }
}

export function ProjectListPage() {
  const navigate = useNavigate();
  const { hasPermission } = usePermissions();
  const [projects, setProjects] = useState<Project[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingProject, setEditingProject] = useState<Project | null>(null);

  const loadProjects = useCallback(() => {
    setLoading(true);
    setError(null);
    getProjects()
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
  }, []);

  useEffect(() => {
    loadProjects();
  }, [loadProjects]);

  const handleCreate = () => {
    setEditingProject(null);
    setModalOpen(true);
  };

  const handleEdit = (p: Project) => {
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

  const canCreate = hasPermission(PERMISSIONS.ProjectCreate);
  const canEdit = hasPermission(PERMISSIONS.ProjectEdit);

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div className="page-header">
            <h1>Projects</h1>
            <p>Loading…</p>
          </div>
        </div>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div key={i} className="card card-body rounded-xl shadow-sm animate-pulse">
              <div className="h-5 w-3/4 bg-gray-200 dark:bg-slate-700 rounded" />
              <div className="mt-2 h-4 w-full bg-gray-100 dark:bg-slate-600 rounded" />
              <div className="mt-2 h-4 w-1/2 bg-gray-100 dark:bg-slate-600 rounded" />
              <div className="mt-3 h-4 w-24 bg-gray-100 dark:bg-slate-600 rounded" />
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="space-y-6">
        <div className="page-header">
          <h1>Projects</h1>
          <p>Something went wrong.</p>
        </div>
        <div className="card card-body rounded-xl shadow-sm border border-red-200 dark:border-red-900/50">
          <p className="text-red-600 dark:text-red-400">{error}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="page-header">
          <h1>Projects</h1>
          <p>Manage your project planning.</p>
        </div>
        {canCreate && (
          <button
            type="button"
            onClick={handleCreate}
            className="inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-blue-600 text-white text-sm font-medium shadow-sm hover:bg-blue-700 transition-colors"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            Create project
          </button>
        )}
      </div>

      {projects.length === 0 ? (
        <div className="card card-body rounded-xl shadow-sm text-center py-12">
          <div className="mx-auto w-16 h-16 rounded-full bg-slate-100 dark:bg-slate-700 flex items-center justify-center text-slate-400 dark:text-slate-500 mb-4">
            <svg className="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" />
            </svg>
          </div>
          <h2 className="text-lg font-semibold mb-1" style={{ color: 'var(--card-header-color, #323130)' }}>No projects yet</h2>
          <p className="text-sm mb-4" style={{ color: 'var(--card-description-color, #605e5c)' }}>Create your first project to get started.</p>
          {canCreate && (
            <button type="button" onClick={handleCreate} className="inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-blue-600 text-white text-sm font-medium hover:bg-blue-700 transition-colors">
              Create project
            </button>
          )}
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {projects.map((p) => (
            <div
              key={p.id}
              role="button"
              tabIndex={0}
              onClick={() => navigate(`/projects/${p.id}`)}
              onKeyDown={(e) => e.key === 'Enter' && navigate(`/projects/${p.id}`)}
              className="card card-body rounded-xl shadow-sm hover:shadow-md transition-shadow cursor-pointer border border-gray-100 dark:border-slate-700/50"
            >
              <div className="flex items-start justify-between gap-2">
                <h3 className="font-semibold truncate flex-1" style={{ color: 'var(--card-header-color, #323130)' }}>{p.name}</h3>
                {canEdit && (
                  <button
                    type="button"
                    onClick={(e) => { e.stopPropagation(); handleEdit(p); }}
                    className="shrink-0 p-1 rounded hover:bg-gray-100 dark:hover:bg-slate-700 text-gray-500 dark:text-slate-400"
                    aria-label="Edit project"
                  >
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </button>
                )}
              </div>
              {p.description && (
                <p className="text-sm line-clamp-2 mt-1" style={{ color: 'var(--card-description-color, #605e5c)' }}>{p.description}</p>
              )}
              <div className="mt-3 flex flex-wrap items-center gap-2">
                <span className={`inline-flex px-2 py-0.5 rounded-full text-xs font-medium ${getStatusBadgeClass(p.status)}`}>
                  {p.status}
                </span>
                <span className="text-xs" style={{ color: 'var(--card-description-color, #605e5c)' }}>{formatDate(p.createdAt)}</span>
              </div>
            </div>
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
      />
    </div>
  );
}
